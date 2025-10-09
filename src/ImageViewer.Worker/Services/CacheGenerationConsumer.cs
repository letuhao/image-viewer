using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ImageViewer.Domain.Events;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Infrastructure.Data;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.Cache;
using MongoDB.Bson;

namespace ImageViewer.Worker.Services;

/// <summary>
/// Consumer for cache generation messages
/// </summary>
public class CacheGenerationConsumer : BaseMessageConsumer
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private int _processedCount = 0;
    private readonly object _counterLock = new object();

    public CacheGenerationConsumer(
        IConnection connection,
        IOptions<RabbitMQOptions> options,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<CacheGenerationConsumer> logger)
        : base(connection, options, logger, "cache.generation", "cache-generation-consumer")
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            // Check if cancellation requested
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("⚠️ Cancellation requested, skipping cache generation");
                return;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            var cacheMessage = JsonSerializer.Deserialize<CacheGenerationMessage>(message, options);
            if (cacheMessage == null)
            {
                _logger.LogWarning("Failed to deserialize CacheGenerationMessage");
                return;
            }

            _logger.LogDebug("Processing cache generation for image {ImageId} ({Path})", 
                cacheMessage.ImageId, cacheMessage.ImagePath);

            // Try to create scope, handle disposal gracefully
            IServiceScope? scope = null;
            try
            {
                scope = _serviceScopeFactory.CreateScope();
            }
            catch (ObjectDisposedException)
            {
                _logger.LogWarning("⚠️ Service provider disposed, worker is shutting down. Skipping cache generation.");
                return;
            }

            using (scope)
            {
            var imageProcessingService = scope.ServiceProvider.GetRequiredService<IImageProcessingService>();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var collectionRepository = scope.ServiceProvider.GetRequiredService<ICollectionRepository>();

            // Determine proper cache path using cache service
            var cachePath = await DetermineCachePath(cacheMessage, cacheService);
            if (string.IsNullOrEmpty(cachePath))
            {
                _logger.LogWarning("❌ Could not determine cache path for image {ImageId}", cacheMessage.ImageId);
                return;
            }

            // Check if cache already exists and force regeneration is disabled
            if (!cacheMessage.ForceRegenerate && File.Exists(cachePath))
            {
                _logger.LogInformation("📁 Cache already exists for image {ImageId}, skipping generation", cacheMessage.ImageId);
                return;
            }

            // Smart quality adjustment: avoid degrading low-quality source images
            int adjustedQuality = await DetermineOptimalCacheQuality(
                cacheMessage, 
                imageProcessingService, 
                cancellationToken);
            
            if (adjustedQuality != cacheMessage.Quality)
            {
                _logger.LogInformation("🎨 Adjusted cache quality from {RequestedQuality} to {AdjustedQuality} based on source image analysis", 
                    cacheMessage.Quality, adjustedQuality);
            }
            
            // Generate cache image
            byte[] cacheImageData;
            
            // Check if we should preserve original (no resize)
            if (cacheMessage.PreserveOriginal || cacheMessage.Format == "original")
            {
                _logger.LogDebug("Preserving original quality for image {ImageId} (no resize)", cacheMessage.ImageId);
                
                // Handle ZIP entries - extract bytes
                if (ArchiveFileHelper.IsZipEntryPath(cacheMessage.ImagePath))
                {
                    var imageBytes = await ArchiveFileHelper.ExtractZipEntryBytes(cacheMessage.ImagePath, null, cancellationToken);
                    if (imageBytes == null || imageBytes.Length == 0)
                    {
                        _logger.LogWarning("❌ Failed to extract ZIP entry for cache: {Path}", cacheMessage.ImagePath);
                        return;
                    }
                    cacheImageData = imageBytes; // Use original bytes, no resize
                }
                else
                {
                    // Regular file - read original file
                    cacheImageData = await File.ReadAllBytesAsync(cacheMessage.ImagePath, cancellationToken);
                }
            }
            else
            {
                // Resize to cache dimensions with smart quality
                // Handle ZIP entries
                if (ArchiveFileHelper.IsZipEntryPath(cacheMessage.ImagePath))
                {
                    // Extract image bytes from ZIP
                    var imageBytes = await ArchiveFileHelper.ExtractZipEntryBytes(cacheMessage.ImagePath, null, cancellationToken);
                    if (imageBytes == null || imageBytes.Length == 0)
                    {
                        _logger.LogWarning("❌ Failed to extract ZIP entry for cache: {Path}", cacheMessage.ImagePath);
                        return;
                    }
                    
                    cacheImageData = await imageProcessingService.ResizeImageFromBytesAsync(
                        imageBytes,
                        cacheMessage.CacheWidth,
                        cacheMessage.CacheHeight,
                        adjustedQuality, // Use adjusted quality!
                        cancellationToken);
                }
                else
                {
                    // Regular file
                    cacheImageData = await imageProcessingService.ResizeImageAsync(
                cacheMessage.ImagePath,
                cacheMessage.CacheWidth,
                cacheMessage.CacheHeight,
                        adjustedQuality, // Use adjusted quality!
                cancellationToken);
                }
            }

            // Ensure cache directory exists
            var cacheDir = Path.GetDirectoryName(cachePath);
            if (!string.IsNullOrEmpty(cacheDir) && !Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            // Save cache image to file system
            await File.WriteAllBytesAsync(cachePath, cacheImageData, cancellationToken);

            // Update cache info in database
            var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
            await UpdateCacheInfoInDatabase(cacheMessage, cachePath, collectionRepository, backgroundJobService);

            _logger.LogDebug("✅ Cache generated for image {ImageId} at path {CachePath} with dimensions {Width}x{Height}", 
                cacheMessage.ImageId, cachePath, cacheMessage.CacheWidth, cacheMessage.CacheHeight);

            // Batched logging - log every 50 files
            int currentCount;
            lock (_counterLock)
            {
                _processedCount++;
                currentCount = _processedCount;
            }

            if (currentCount % 50 == 0)
            {
                _logger.LogInformation("✅ Generated {Count} cache files (latest: {ImageId})", currentCount, cacheMessage.ImageId);
            }
            else
            {
                _logger.LogDebug("Successfully generated cache for image {ImageId}", cacheMessage.ImageId);
            }
            } // Close using (scope) block
        }
        catch (Exception ex)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            _logger.LogError(ex, "Error processing cache generation message for image {ImageId}", 
                JsonSerializer.Deserialize<CacheGenerationMessage>(message, options)?.ImageId);
            throw;
        }
    }

    private async Task<string?> DetermineCachePath(CacheGenerationMessage cacheMessage, ICacheService cacheService)
    {
        try
        {
            // Use cache service to determine the proper cache path
            var cacheFolders = await cacheService.GetCacheFoldersAsync();
            if (!cacheFolders.Any())
            {
                _logger.LogWarning("⚠️ No cache folders configured, using default cache directory");
                return Path.Combine("cache", $"{cacheMessage.ImageId}_cache_{cacheMessage.CacheWidth}x{cacheMessage.CacheHeight}.jpg");
            }

            // Select cache folder using hash-based distribution for equal load balancing
            var collectionId = ObjectId.Parse(cacheMessage.CollectionId); // Convert string back to ObjectId
            var cacheFolder = SelectCacheFolderForEqualDistribution(cacheFolders, collectionId);
            
            // Create proper folder structure: CacheFolder/cache/CollectionId/ImageId_CacheWidthxCacheHeight.jpg
            var collectionIdStr = cacheMessage.CollectionId; // Already a string
            var cacheDir = Path.Combine(cacheFolder.Path, "cache", collectionIdStr);
            var fileName = $"{cacheMessage.ImageId}_cache_{cacheMessage.CacheWidth}x{cacheMessage.CacheHeight}.jpg";
            
            _logger.LogDebug("📁 Selected cache folder {CacheFolderName} for collection {CollectionId}, image {ImageId}", 
                cacheFolder.Name, collectionIdStr, cacheMessage.ImageId);
            return Path.Combine(cacheDir, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error determining cache path for image {ImageId}", cacheMessage.ImageId);
            return null;
        }
    }

    private CacheFolderDto SelectCacheFolderForEqualDistribution(IEnumerable<CacheFolderDto> cacheFolders, ObjectId collectionId)
    {
        // Filter to only active cache folders
        var activeCacheFolders = cacheFolders.Where(cf => cf.IsActive).ToList();
        
        if (!activeCacheFolders.Any())
        {
            throw new InvalidOperationException("No active cache folders available");
        }

        // Use hash-based distribution to ensure equal distribution across cache folders
        // This ensures the same collection always goes to the same cache folder (for consistency)
        // while distributing collections evenly across all available cache folders
        var hash = Math.Abs(collectionId.GetHashCode());
        var selectedIndex = hash % activeCacheFolders.Count;
        var selectedFolder = activeCacheFolders[selectedIndex];
        
        _logger.LogDebug("🎯 Hash-based cache folder selection: CollectionId={CollectionId}, Hash={Hash}, Index={Index}, SelectedFolder={FolderName}", 
            collectionId, hash, selectedIndex, selectedFolder.Name);
        
        return selectedFolder;
    }

    private async Task UpdateCacheInfoInDatabase(CacheGenerationMessage cacheMessage, string cachePath, ICollectionRepository collectionRepository, IBackgroundJobService backgroundJobService)
    {
        try
        {
            _logger.LogInformation("📝 Updating cache info in database for image {ImageId}", cacheMessage.ImageId);
            
            // Convert string back to ObjectId for database operations
            var collectionId = ObjectId.Parse(cacheMessage.CollectionId);
            
            // Get the collection
            var collection = await collectionRepository.GetByIdAsync(collectionId);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection {collectionId} not found");
            }
            
            // Check if cache already exists for this image (prevent duplicates)
            var existingCache = collection.CacheImages?.FirstOrDefault(c => c.ImageId == cacheMessage.ImageId);
            if (existingCache != null)
            {
                _logger.LogDebug("Cache already exists for image {ImageId}, skipping", cacheMessage.ImageId);
                return;
            }
            
            // Create cache image embedded object
            var fileInfo = new FileInfo(cachePath);
            var cacheImage = new CacheImageEmbedded(
                cacheMessage.ImageId,
                cachePath,
                cacheMessage.CacheWidth,
                cacheMessage.CacheHeight,
                fileInfo.Length,
                fileInfo.Extension.TrimStart('.').ToUpperInvariant(),
                cacheMessage.Quality
            );
            
            // Atomically add cache image to collection (thread-safe, prevents race conditions!)
            var added = await collectionRepository.AtomicAddCacheImageAsync(collectionId, cacheImage);
            if (!added)
            {
                _logger.LogWarning("Failed to add cache image to collection {CollectionId} - collection might not exist", collectionId);
                return;
            }
            
            // REAL-TIME JOB TRACKING: Update job stage immediately after each cache
            if (!string.IsNullOrEmpty(cacheMessage.ScanJobId))
            {
                try
                {
                    await backgroundJobService.IncrementJobStageProgressAsync(
                        ObjectId.Parse(cacheMessage.ScanJobId),
                        "cache",
                        incrementBy: 1);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update job stage for {JobId}, fallback monitor will handle it", cacheMessage.ScanJobId);
                }
            }
            
            _logger.LogInformation("✅ Cache info updated for image {ImageId}: {CachePath}", 
                cacheMessage.ImageId, cachePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error updating cache info in database for image {ImageId}", cacheMessage.ImageId);
            throw;
        }
    }

    /// <summary>
    /// Determines optimal cache quality to avoid degrading low-quality source images
    /// </summary>
    private async Task<int> DetermineOptimalCacheQuality(
        CacheGenerationMessage cacheMessage, 
        IImageProcessingService imageProcessingService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract image bytes for analysis
            byte[]? imageBytes = null;
            long fileSize = 0;
            int requestedQuality = cacheMessage.Quality;
            
            if (ArchiveFileHelper.IsArchiveEntryPath(cacheMessage.ImagePath))
            {
                imageBytes = await ArchiveFileHelper.ExtractZipEntryBytes(cacheMessage.ImagePath, null, cancellationToken);
                fileSize = imageBytes?.Length ?? 0;
            }
            else if (File.Exists(cacheMessage.ImagePath))
            {
                var fileInfo = new FileInfo(cacheMessage.ImagePath);
                fileSize = fileInfo.Length;
            }
            
            // Get image dimensions from metadata (we should have this from ImageProcessingConsumer)
            // For now, use file size as a proxy for quality estimation
            
            // Rule 1: If source is very small (likely low quality or highly compressed), don't use high quality
            // File size per pixel ratio estimation
            if (fileSize > 0 && imageBytes != null)
            {
                // Use SkiaSharp to analyze the image
                using var skImage = SkiaSharp.SKBitmap.Decode(imageBytes);
                if (skImage != null)
                {
                    var totalPixels = skImage.Width * skImage.Height;
                    var bytesPerPixel = (double)fileSize / totalPixels;
                    
                    // Estimate source quality based on bytes per pixel
                    // High quality JPEGs: > 2 bytes/pixel
                    // Medium quality: 1-2 bytes/pixel
                    // Low quality: < 1 byte/pixel
                    // Very low quality: < 0.5 bytes/pixel
                    
                    int estimatedSourceQuality;
                    if (bytesPerPixel >= 2.0)
                    {
                        estimatedSourceQuality = 95; // High quality source
                    }
                    else if (bytesPerPixel >= 1.0)
                    {
                        estimatedSourceQuality = 85; // Medium-high quality
                    }
                    else if (bytesPerPixel >= 0.5)
                    {
                        estimatedSourceQuality = 75; // Medium quality
                    }
                    else
                    {
                        estimatedSourceQuality = 60; // Low quality source
                    }
                    
                    // Don't use cache quality higher than source quality
                    // (no point compressing at 95% when source is already 60%)
                    if (requestedQuality > estimatedSourceQuality)
                    {
                        _logger.LogDebug("Source image appears to be {EstimatedQuality}% quality ({BytesPerPixel:F2} bytes/pixel), " +
                            "adjusting cache quality from {RequestedQuality}% to {AdjustedQuality}%",
                            estimatedSourceQuality, bytesPerPixel, requestedQuality, estimatedSourceQuality);
                        return estimatedSourceQuality;
                    }
                    
                    // Rule 2: If image is already smaller than cache target, preserve original quality
                    if (skImage.Width <= cacheMessage.CacheWidth && skImage.Height <= cacheMessage.CacheHeight)
                    {
                        _logger.LogDebug("Source image ({Width}x{Height}) is smaller than cache target ({CacheWidth}x{CacheHeight}), " +
                            "using quality 100 to preserve original",
                            skImage.Width, skImage.Height, cacheMessage.CacheWidth, cacheMessage.CacheHeight);
                        return 100; // Preserve original quality for small images
                    }
                }
            }
            
            // Default: use requested quality
            return requestedQuality;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to analyze image quality for {ImagePath}, using requested quality {Quality}", 
                cacheMessage.ImagePath, cacheMessage.Quality);
            return cacheMessage.Quality; // Fallback to requested quality
        }
    }
}
