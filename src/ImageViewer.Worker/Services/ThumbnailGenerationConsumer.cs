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
                    
                    // Batched logging - log every 50 files
                    int currentCount;
                    lock (_counterLock)
                    {
                        _processedCount++;
                        currentCount = _processedCount;
                    }

                    // Note: Job completion tracking is handled by CollectionScanConsumer.MonitorJobCompletionAsync()
                    // which polls MongoDB to check when all thumbnails/cache are generated
                    
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
            // Determine thumbnail path
            var thumbnailPath = await GetThumbnailPath(imagePath, width, height, collectionId);
            
            // Ensure thumbnail directory exists
            var thumbnailDir = Path.GetDirectoryName(thumbnailPath);
            if (!string.IsNullOrEmpty(thumbnailDir) && !Directory.Exists(thumbnailDir))
            {
                Directory.CreateDirectory(thumbnailDir);
            }

            // Generate thumbnail using image processing service
            byte[] thumbnailData;
            
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
                    cancellationToken);
            }
            else
            {
                // Regular file
                thumbnailData = await imageProcessingService.GenerateThumbnailAsync(
                    imagePath, 
                    width, 
                    height, 
                    cancellationToken);
            }

            if (thumbnailData != null && thumbnailData.Length > 0)
            {
                // Save thumbnail data to file
                await File.WriteAllBytesAsync(thumbnailPath, thumbnailData, cancellationToken);
                _logger.LogInformation("✅ Generated thumbnail: {ThumbnailPath}", thumbnailPath);
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

    private async Task<string> GetThumbnailPath(string imagePath, int width, int height, ObjectId collectionId)
    {
        // Extract filename only (handle archive entries like "archive.zip#entry.png")
        string fileName;
        string extension;
        
        if (ArchiveFileHelper.IsArchiveEntryPath(imagePath))
        {
            // For archive entries, extract ONLY the entry name (after #)
            var (_, entryName) = ArchiveFileHelper.SplitArchiveEntryPath(imagePath);
            fileName = Path.GetFileNameWithoutExtension(entryName);
            extension = Path.GetExtension(entryName);
        }
        else
        {
            // For regular files, use filename only (not full path)
            fileName = Path.GetFileNameWithoutExtension(imagePath);
            extension = Path.GetExtension(imagePath);
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
        
        // Create proper folder structure: CacheFolder/thumbnails/CollectionId/ImageFileName_WidthxHeight.ext
        var collectionIdStr = collectionId.ToString();
        var thumbnailDir = Path.Combine(selectedCacheFolder.Path, "thumbnails", collectionIdStr);
        var thumbnailFileName = $"{fileName}_{width}x{height}{extension}";
        
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
}