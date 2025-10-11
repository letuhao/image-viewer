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
using ImageViewer.Application.Services;
using ImageViewer.Infrastructure.Data;
using MongoDB.Bson;

namespace ImageViewer.Worker.Services;

/// <summary>
/// Consumer for thumbnail generation messages
/// </summary>
public class ThumbnailGenerationConsumer : BaseMessageConsumer
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private int _processedCount = 0;
    private readonly object _counterLock = new object();

    public ThumbnailGenerationConsumer(
        IConnection connection,
        IOptions<RabbitMQOptions> options,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ThumbnailGenerationConsumer> logger)
        : base(connection, options, logger, "thumbnail.generation", "thumbnail-generation-consumer")
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("🖼️ Received thumbnail generation message: {Message}", message);
            
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            var thumbnailMessage = JsonSerializer.Deserialize<ThumbnailGenerationMessage>(message, options);
            if (thumbnailMessage == null)
            {
                _logger.LogWarning("❌ Failed to deserialize ThumbnailGenerationMessage from: {Message}", message);
                return;
            }

            _logger.LogDebug("🖼️ Generating thumbnail for image {ImageId} ({Filename})", 
                thumbnailMessage.ImageId, thumbnailMessage.ImageFilename);

            // Try to create scope, handle disposal gracefully
            IServiceScope? scope = null;
            try
            {
                scope = _serviceScopeFactory.CreateScope();
            }
            catch (ObjectDisposedException)
            {
                _logger.LogWarning("⚠️ Service provider disposed, worker is shutting down. Skipping thumbnail generation.");
                return;
            }

            using (scope)
            {
            var imageProcessingService = scope.ServiceProvider.GetRequiredService<IImageProcessingService>();
            var collectionRepository = scope.ServiceProvider.GetRequiredService<ICollectionRepository>();

            // Check if image file exists
            if (!File.Exists(thumbnailMessage.ImagePath) && !thumbnailMessage.ImagePath.Contains("#"))
            {
                _logger.LogWarning("❌ Image file {Path} does not exist, skipping thumbnail generation", thumbnailMessage.ImagePath);
                return;
            }

            // Generate thumbnail
            try
            {
                // Convert string CollectionId back to ObjectId for cache folder selection
                var collectionId = ObjectId.Parse(thumbnailMessage.CollectionId);
                
                var thumbnailPath = await GenerateThumbnail(
                    thumbnailMessage.ImagePath, 
                    thumbnailMessage.ThumbnailWidth, 
                    thumbnailMessage.ThumbnailHeight,
                    imageProcessingService,
                    collectionId,
                    cancellationToken);

                if (!string.IsNullOrEmpty(thumbnailPath))
                {
                    // Update database with thumbnail information
                    await UpdateThumbnailInfoInDatabase(thumbnailMessage, thumbnailPath, collectionRepository);
                    
                    // REAL-TIME JOB TRACKING: Update job stage immediately after each thumbnail
                    if (!string.IsNullOrEmpty(thumbnailMessage.ScanJobId))
                    {
                        try
                        {
                            var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
                            await backgroundJobService.IncrementJobStageProgressAsync(
                                ObjectId.Parse(thumbnailMessage.ScanJobId),
                                "thumbnail",
                                incrementBy: 1);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to update job stage for {JobId}, fallback monitor will handle it", thumbnailMessage.ScanJobId);
                        }
                    }
                    
                    // Track progress in FileProcessingJobState if jobId is present
                    if (!string.IsNullOrEmpty(thumbnailMessage.JobId))
                    {
                        try
                        {
                            var jobStateRepository = scope.ServiceProvider.GetRequiredService<IFileProcessingJobStateRepository>();
                            var fileInfo = new FileInfo(thumbnailPath);
                            await jobStateRepository.AtomicIncrementCompletedAsync(
                                thumbnailMessage.JobId, 
                                thumbnailMessage.ImageId,
                                fileInfo.Length);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to track thumbnail progress for image {ImageId} in job {JobId}", 
                                thumbnailMessage.ImageId, thumbnailMessage.JobId);
                        }
                    }
                    
                    // Batched logging - log every 50 files
                    int currentCount;
                    lock (_counterLock)
                    {
                        _processedCount++;
                        currentCount = _processedCount;
                    }
                    
                    if (currentCount % 50 == 0)
                    {
                        _logger.LogInformation("✅ Generated {Count} thumbnails (latest: {ImageId})", currentCount, thumbnailMessage.ImageId);
                    }
                    else
                    {
                        _logger.LogDebug("✅ Successfully generated thumbnail for image {ImageId} at {ThumbnailPath}", 
                            thumbnailMessage.ImageId, thumbnailPath);
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ Thumbnail generation returned empty path for image {ImageId}", thumbnailMessage.ImageId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to generate thumbnail for image {ImageId}", thumbnailMessage.ImageId);
                
                // Track as failed in FileProcessingJobState
                if (!string.IsNullOrEmpty(thumbnailMessage.JobId))
                {
                    try
                    {
                        var jobStateRepository = scope.ServiceProvider.GetRequiredService<IFileProcessingJobStateRepository>();
                        await jobStateRepository.AtomicIncrementFailedAsync(thumbnailMessage.JobId, thumbnailMessage.ImageId);
                    }
                    catch (Exception trackEx)
                    {
                        _logger.LogWarning(trackEx, "Failed to track thumbnail error for image {ImageId} in job {JobId}", 
                            thumbnailMessage.ImageId, thumbnailMessage.JobId);
                    }
                }
            }
            } // Close using (scope) block
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing thumbnail generation message");
            throw;
        }
    }

    private async Task<string?> GenerateThumbnail(string imagePath, int width, int height, IImageProcessingService imageProcessingService, ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get format from settings FIRST before determining thumbnail path
            using var settingsScope = _serviceScopeFactory.CreateScope();
            var settingsService = settingsScope.ServiceProvider.GetRequiredService<IImageProcessingSettingsService>();
            var format = await settingsService.GetThumbnailFormatAsync();
            
            // Determine thumbnail path (with correct format extension)
            var thumbnailPath = await GetThumbnailPath(imagePath, width, height, collectionId, format);
            
            // Ensure thumbnail directory exists
            var thumbnailDir = Path.GetDirectoryName(thumbnailPath);
            if (!string.IsNullOrEmpty(thumbnailDir) && !Directory.Exists(thumbnailDir))
            {
                Directory.CreateDirectory(thumbnailDir);
            }

            // Generate thumbnail using image processing service
            byte[] thumbnailData;
            
            // Get quality setting (format already retrieved earlier)
            var quality = await settingsService.GetThumbnailQualityAsync();
            
            _logger.LogDebug("🎨 Using thumbnail format: {Format}, quality: {Quality}", format, quality);
            
            // Handle ZIP entries
            if (ArchiveFileHelper.IsZipEntryPath(imagePath))
            {
                // Extract image bytes from ZIP
                var imageBytes = await ArchiveFileHelper.ExtractZipEntryBytes(imagePath, null, cancellationToken);
                if (imageBytes == null || imageBytes.Length == 0)
                {
                    _logger.LogWarning("❌ Failed to extract ZIP entry: {Path}", imagePath);
                    return null;
                }
                
                thumbnailData = await imageProcessingService.GenerateThumbnailFromBytesAsync(
                    imageBytes, 
                    width, 
                    height,
                    format,
                    quality,
                    cancellationToken);
            }
            else
            {
                // Regular file
                thumbnailData = await imageProcessingService.GenerateThumbnailAsync(
                    imagePath, 
                    width, 
                    height,
                    format,
                    quality,
                    cancellationToken);
            }

            if (thumbnailData != null && thumbnailData.Length > 0)
            {
                // Save thumbnail data to file
                await File.WriteAllBytesAsync(thumbnailPath, thumbnailData, cancellationToken);
                _logger.LogInformation("✅ Generated thumbnail: {ThumbnailPath}", thumbnailPath);
                
                // ATOMIC UPDATE: Increment cache folder size to prevent race conditions
                await UpdateCacheFolderSizeAsync(thumbnailPath, thumbnailData.Length, collectionId);
                
                return thumbnailPath;
            }
            else
            {
                _logger.LogWarning("⚠️ Thumbnail generation failed: No data returned");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error generating thumbnail for {ImagePath}", imagePath);
            return null;
        }
    }

    private async Task<string> GetThumbnailPath(string imagePath, int width, int height, ObjectId collectionId, string format)
    {
        // Determine file extension based on format
        var extension = format.ToLowerInvariant() switch
        {
            "jpeg" => ".jpg",
            "jpg" => ".jpg",
            "png" => ".png",
            "webp" => ".webp",
            _ => ".jpg" // Default fallback
        };
        
        // Extract filename only (handle archive entries like "archive.zip#entry.png")
        string fileName;
        
        if (ArchiveFileHelper.IsArchiveEntryPath(imagePath))
        {
            // For archive entries, extract ONLY the entry name (after #)
            var (_, entryName) = ArchiveFileHelper.SplitArchiveEntryPath(imagePath);
            fileName = Path.GetFileNameWithoutExtension(entryName);
        }
        else
        {
            // For regular files, use filename only (not full path)
            fileName = Path.GetFileNameWithoutExtension(imagePath);
        }
        
        // Use cache service to get the appropriate cache folder for thumbnails
        using var scope = _serviceScopeFactory.CreateScope();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        
        // Get all cache folders and select one based on collection ID hash for even distribution
        var cacheFolders = await cacheService.GetCacheFoldersAsync();
        var cacheFoldersList = cacheFolders.ToList();
        
        if (cacheFoldersList.Count == 0)
        {
            throw new InvalidOperationException("No cache folders available");
        }
        
        // Use hash-based distribution to select cache folder
        var hash = collectionId.GetHashCode();
        var selectedIndex = Math.Abs(hash) % cacheFoldersList.Count;
        var selectedCacheFolder = cacheFoldersList[selectedIndex];
        
        // Create proper folder structure: CacheFolder/thumbnails/CollectionId/ImageFileName_WidthxHeight.{ext}
        var collectionIdStr = collectionId.ToString();
        var thumbnailDir = Path.Combine(selectedCacheFolder.Path, "thumbnails", collectionIdStr);
        var thumbnailFileName = $"{fileName}_{width}x{height}{extension}";
        
        _logger.LogDebug("📁 Selected cache folder {CacheFolderName} for thumbnail (format: {Format})", 
            selectedCacheFolder.Name, format);
        
        return Path.Combine(thumbnailDir, thumbnailFileName);
    }

    private async Task UpdateThumbnailInfoInDatabase(ThumbnailGenerationMessage thumbnailMessage, string thumbnailPath, ICollectionRepository collectionRepository)
    {
        try
        {
            _logger.LogInformation("📝 Updating thumbnail info in database for image {ImageId}", thumbnailMessage.ImageId);
            
            // Convert string back to ObjectId for database operations
            var collectionId = ObjectId.Parse(thumbnailMessage.CollectionId);
            
            // Get the collection
            var collection = await collectionRepository.GetByIdAsync(collectionId);
            if (collection == null)
            {
                throw new InvalidOperationException($"Collection {collectionId} not found");
            }
            
            // Create thumbnail embedded object
            var fileInfo = new FileInfo(thumbnailPath);
            var thumbnailEmbedded = new ThumbnailEmbedded(
                thumbnailMessage.ImageId,
                thumbnailPath,
                thumbnailMessage.ThumbnailWidth,
                thumbnailMessage.ThumbnailHeight,
                fileInfo.Length,
                fileInfo.Extension.TrimStart('.').ToUpperInvariant(),
                95 // quality
            );
            
            // Atomically add thumbnail to collection (thread-safe, prevents race conditions!)
            var added = await collectionRepository.AtomicAddThumbnailAsync(collectionId, thumbnailEmbedded);
            if (!added)
            {
                _logger.LogWarning("Failed to add thumbnail to collection {CollectionId} - collection might not exist", collectionId);
                return;
            }
            
            _logger.LogInformation("✅ Thumbnail info created and persisted for image {ImageId}: {ThumbnailPath}", 
                thumbnailMessage.ImageId, thumbnailPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error updating thumbnail info in database for image {ImageId}", thumbnailMessage.ImageId);
            throw;
        }
    }

    /// <summary>
    /// Atomically update cache folder size after saving thumbnail
    /// Uses MongoDB $inc operator to prevent race conditions during concurrent operations
    /// </summary>
    private async Task UpdateCacheFolderSizeAsync(string thumbnailPath, long fileSize, ObjectId collectionId)
    {
        try
        {
            // Determine which cache folder was used based on the path
            using var scope = _serviceScopeFactory.CreateScope();
            var cacheFolderRepository = scope.ServiceProvider.GetRequiredService<ICacheFolderRepository>();
            
            // Extract cache folder path from thumbnail path
            // ThumbnailPath format: {CacheFolderPath}/thumbnails/{CollectionId}/{FileName}
            var thumbnailDir = Path.GetDirectoryName(thumbnailPath);
            if (string.IsNullOrEmpty(thumbnailDir))
            {
                _logger.LogWarning("⚠️ Cannot determine cache folder from path: {Path}", thumbnailPath);
                return;
            }
            
            // Get parent directory (remove CollectionId folder)
            var thumbnailsDir = Path.GetDirectoryName(thumbnailDir);
            if (string.IsNullOrEmpty(thumbnailsDir))
            {
                _logger.LogWarning("⚠️ Cannot determine thumbnails directory from path: {Path}", thumbnailPath);
                return;
            }
            
            // Get cache folder root (remove "thumbnails" folder)
            var cacheFolderPath = Path.GetDirectoryName(thumbnailsDir);
            if (string.IsNullOrEmpty(cacheFolderPath))
            {
                _logger.LogWarning("⚠️ Cannot determine cache folder root from path: {Path}", thumbnailPath);
                return;
            }
            
            // Find cache folder by path
            var cacheFolder = await cacheFolderRepository.GetByPathAsync(cacheFolderPath);
            if (cacheFolder == null)
            {
                _logger.LogWarning("⚠️ Cache folder not found for path: {Path}", cacheFolderPath);
                return;
            }
            
            // ATOMIC INCREMENT: Thread-safe update using MongoDB $inc operator
            await cacheFolderRepository.IncrementSizeAsync(cacheFolder.Id, fileSize);
            await cacheFolderRepository.IncrementFileCountAsync(cacheFolder.Id, 1);
            await cacheFolderRepository.AddCachedCollectionAsync(cacheFolder.Id, collectionId.ToString());
            
            _logger.LogDebug("📊 Atomically incremented cache folder {Name} size by {Size} bytes, file count by 1", 
                cacheFolder.Name, fileSize);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Failed to update cache folder statistics for thumbnail: {Path}", thumbnailPath);
            // Don't throw - thumbnail is already saved, this is just statistics
        }
    }
}