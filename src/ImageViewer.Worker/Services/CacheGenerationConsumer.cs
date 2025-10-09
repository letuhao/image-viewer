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

            // Generate cache image using ResizeImageAsync
            byte[] cacheImageData;
            
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
                    cacheMessage.Quality,
                    cancellationToken);
            }
            else
            {
                // Regular file
                cacheImageData = await imageProcessingService.ResizeImageAsync(
                    cacheMessage.ImagePath,
                    cacheMessage.CacheWidth,
                    cacheMessage.CacheHeight,
                    cacheMessage.Quality,
                    cancellationToken);
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
            await UpdateCacheInfoInDatabase(cacheMessage, cachePath, collectionRepository);

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

    private async Task UpdateCacheInfoInDatabase(CacheGenerationMessage cacheMessage, string cachePath, ICollectionRepository collectionRepository)
    {
        try
        {
            _logger.LogInformation("📝 Updating cache info in database for image {ImageId}", cacheMessage.ImageId);
            
            // Convert string back to ObjectId for database operations
            var collectionId = ObjectId.Parse(cacheMessage.CollectionId);
            
            // Get the collection (with retry for race condition)
            Collection? collection = null;
            Domain.ValueObjects.ImageEmbedded? image = null;
            
            for (int attempt = 0; attempt < 3; attempt++)
            {
                collection = await collectionRepository.GetByIdAsync(collectionId);
                if (collection == null)
                {
                    throw new InvalidOperationException($"Collection {collectionId} not found");
                }
                
                // Find the image in the embedded images
                image = collection.Images?.FirstOrDefault(i => i.Id == cacheMessage.ImageId);
                if (image != null)
                {
                    break; // Found it!
                }
                
                // Image not found yet - might be a race condition where image was just created
                if (attempt < 2)
                {
                    _logger.LogDebug("Image {ImageId} not found in collection yet, retrying (attempt {Attempt}/3)...", 
                        cacheMessage.ImageId, attempt + 1);
                    await Task.Delay(500); // Wait 500ms for MongoDB to sync (increased from 100ms)
                }
            }
            
            if (image == null)
            {
                throw new InvalidOperationException($"Image {cacheMessage.ImageId} not found in collection {collectionId} after 3 attempts");
            }
            
            // Update the cache info
            var fileInfo = new FileInfo(cachePath);
            var cacheInfo = new ImageCacheInfoEmbedded(
                cachePath,
                fileInfo.Length,
                fileInfo.Extension.TrimStart('.').ToUpperInvariant(), // e.g., "PNG", "JPEG"
                cacheMessage.CacheWidth,
                cacheMessage.CacheHeight,
                cacheMessage.Quality
            );
            
            image.SetCacheInfo(cacheInfo);
            
            // Save the collection
            await collectionRepository.UpdateAsync(collection);
            
            _logger.LogInformation("✅ Cache info updated for image {ImageId}: {CachePath}", 
                cacheMessage.ImageId, cachePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error updating cache info in database for image {ImageId}", cacheMessage.ImageId);
            throw;
        }
    }
}
