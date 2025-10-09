using System.Text.Json;
using System.IO.Compression;
using SharpCompress.Archives;
using SharpCompress.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ImageViewer.Domain.Events;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Enums;
using ImageViewer.Infrastructure.Data;
using MongoDB.Bson;

namespace ImageViewer.Worker.Services;

/// <summary>
/// Consumer for collection scan messages
/// </summary>
public class CollectionScanConsumer : BaseMessageConsumer
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public CollectionScanConsumer(
        IConnection connection,
        IOptions<RabbitMQOptions> options,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<CollectionScanConsumer> logger)
        : base(connection, options, logger, "collection.scan", "collection-scan-consumer")
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("🔍 Received collection scan message: {Message}", message);
            
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            var scanMessage = JsonSerializer.Deserialize<CollectionScanMessage>(message, options);
            if (scanMessage == null)
            {
                _logger.LogWarning("❌ Failed to deserialize CollectionScanMessage from: {Message}", message);
                return;
            }

            _logger.LogInformation("🔍 Processing collection scan for collection {CollectionId} at path {Path}", 
                scanMessage.CollectionId, scanMessage.CollectionPath);

            // Try to create scope, handle disposal gracefully
            IServiceScope? scope = null;
            try
            {
                scope = _serviceScopeFactory.CreateScope();
            }
            catch (ObjectDisposedException)
            {
                _logger.LogWarning("⚠️ Service provider disposed, worker is shutting down. Skipping collection scan.");
                return;
            }

            using (scope)
            {
            var collectionService = scope.ServiceProvider.GetRequiredService<ICollectionService>();
            var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
            var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();

            // Get the collection (convert string CollectionId back to ObjectId)
            var collectionId = ObjectId.Parse(scanMessage.CollectionId);
            var collection = await collectionService.GetCollectionByIdAsync(collectionId);
            if (collection == null)
            {
                _logger.LogWarning("❌ Collection {CollectionId} not found, skipping scan", scanMessage.CollectionId);
                
                // Update job status to failed if JobId exists
                if (!string.IsNullOrEmpty(scanMessage.JobId))
                {
                    try
                    {
                        await backgroundJobService.UpdateJobStatusAsync(ObjectId.Parse(scanMessage.JobId), "Failed", $"Collection {scanMessage.CollectionId} not found");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update job status for {JobId}", scanMessage.JobId);
                    }
                }
                return;
            }
            
            // Update job stage to InProgress - SCAN stage
            if (!string.IsNullOrEmpty(scanMessage.JobId))
            {
                try
                {
                    await backgroundJobService.UpdateJobStageAsync(
                        ObjectId.Parse(scanMessage.JobId), 
                        "scan", 
                        "InProgress", 
                        0, 
                        0, 
                        $"Scanning collection {collection.Name}");
                    _logger.LogDebug("Updated job {JobId} scan stage to InProgress", scanMessage.JobId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update job stage for {JobId}", scanMessage.JobId);
                }
            }

            // Check if collection path exists (directory for Folder type, file for Zip type)
            bool pathExists = collection.Type == CollectionType.Folder 
                ? Directory.Exists(collection.Path) 
                : File.Exists(collection.Path);
            
            if (!pathExists)
            {
                _logger.LogWarning("❌ Collection path {Path} does not exist, skipping scan", collection.Path);
                return;
            }

            // Scan the collection for media files
            var mediaFiles = ScanCollectionForMediaFiles(collection.Path, collection.Type);
            _logger.LogInformation("📁 Found {FileCount} media files in collection {CollectionId}", 
                mediaFiles.Count, collection.Id);

            // Create image processing jobs for each media file
            foreach (var mediaFile in mediaFiles)
            {
                try
                {
                    // Extract basic metadata for the image processing message
                    var (width, height) = await ExtractImageDimensions(mediaFile.FullPath);
                    
                    var imageProcessingMessage = new ImageProcessingMessage
                    {
                        ImageId = ObjectId.GenerateNewId().ToString(), // Will be set when image is created, convert to string
                        CollectionId = collection.Id.ToString(), // Convert ObjectId to string
                        ImagePath = mediaFile.FullPath,
                        ImageFormat = mediaFile.Extension,
                        Width = width,
                        Height = height,
                        FileSize = mediaFile.FileSize,
                        GenerateThumbnail = true,
                        OptimizeImage = false,
                        CreatedBy = "CollectionScanConsumer",
                        CreatedBySystem = "ImageViewer.Worker",
                        ScanJobId = scanMessage.JobId // Pass scan job ID for tracking
                    };

                    // Queue the image processing job
                    await messageQueueService.PublishAsync(imageProcessingMessage, "image.processing");
                    _logger.LogDebug("📋 Queued image processing job for {ImagePath}", mediaFile.FullPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to create image processing job for {ImagePath}", mediaFile.FullPath);
                }
            }

            _logger.LogInformation("✅ Successfully processed collection scan for {CollectionId}, queued {JobCount} image processing jobs", 
                collection.Id, mediaFiles.Count);
            
            // Update SCAN stage to Completed and initialize THUMBNAIL and CACHE stages
            if (!string.IsNullOrEmpty(scanMessage.JobId))
            {
                try
                {
                    // Complete scan stage
                    await backgroundJobService.UpdateJobStageAsync(
                        ObjectId.Parse(scanMessage.JobId), 
                        "scan", 
                        "Completed", 
                        mediaFiles.Count, 
                        mediaFiles.Count, 
                        $"Found {mediaFiles.Count} media files");
                    
                    // Start thumbnail stage
                    await backgroundJobService.UpdateJobStageAsync(
                        ObjectId.Parse(scanMessage.JobId), 
                        "thumbnail", 
                        "InProgress", 
                        0, 
                        mediaFiles.Count, 
                        $"Generating thumbnails for {mediaFiles.Count} images");
                    
                    // Start cache stage
                    await backgroundJobService.UpdateJobStageAsync(
                        ObjectId.Parse(scanMessage.JobId), 
                        "cache", 
                        "InProgress", 
                        0, 
                        mediaFiles.Count, 
                        $"Generating cache for {mediaFiles.Count} images");
                    
                    _logger.LogInformation("✅ Updated job {JobId} stages: scan=Completed, thumbnail/cache=InProgress", scanMessage.JobId);
                    
                    // Start background monitoring to detect completion
                    _ = Task.Run(async () => await MonitorJobCompletionAsync(
                        ObjectId.Parse(scanMessage.JobId), 
                        collectionId, 
                        mediaFiles.Count,
                        backgroundJobService,
                        collectionService));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update job stages for {JobId}", scanMessage.JobId);
                }
            }
            } // Close using (scope) block
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing collection scan message");
            throw;
        }
    }

    private List<MediaFileInfo> ScanCollectionForMediaFiles(string collectionPath, CollectionType collectionType)
    {
        var mediaFiles = new List<MediaFileInfo>();
        
        try
        {
            if (collectionType == CollectionType.Folder)
            {
                // Handle regular directories
                ScanDirectory(collectionPath, mediaFiles);
            }
            else
            {
                // Handle all compressed archive types (ZIP, 7Z, RAR, TAR, etc.)
                ScanCompressedArchive(collectionPath, mediaFiles);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error scanning collection path {Path}", collectionPath);
        }

        return mediaFiles;
    }

    private void ScanCompressedArchive(string archivePath, List<MediaFileInfo> mediaFiles)
    {
        try
        {
            // Use SharpCompress to support ZIP, 7Z, RAR, TAR, CBZ, CBR, and more
            using var archive = ArchiveFactory.Open(archivePath);
            foreach (var entry in archive.Entries)
            {
                if (!entry.IsDirectory && IsMediaFile(entry.Key))
                {
                    mediaFiles.Add(new MediaFileInfo
                    {
                        FullPath = $"{archivePath}#{entry.Key}",
                        RelativePath = entry.Key,
                        FileName = Path.GetFileName(entry.Key),
                        Extension = Path.GetExtension(entry.Key).ToLowerInvariant(),
                        FileSize = entry.Size
                    });
                }
            }
            
            _logger.LogInformation("📦 Scanned archive {Archive}: found {Count} media files", 
                Path.GetFileName(archivePath), mediaFiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error scanning compressed archive {ArchivePath}", archivePath);
        }
    }

    private void ScanDirectory(string directoryPath, List<MediaFileInfo> mediaFiles)
    {
        try
        {
            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (IsMediaFile(file))
                {
                    var fileInfo = new FileInfo(file);
                    mediaFiles.Add(new MediaFileInfo
                    {
                        FullPath = file,
                        RelativePath = Path.GetRelativePath(directoryPath, file),
                        FileName = fileInfo.Name,
                        Extension = fileInfo.Extension.ToLowerInvariant(),
                        FileSize = fileInfo.Length
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error scanning directory {DirectoryPath}", directoryPath);
        }
    }

    private static bool IsMediaFile(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp", ".svg" };
        return supportedExtensions.Contains(extension);
    }

    private async Task<(int width, int height)> ExtractImageDimensions(string imagePath)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var imageProcessingService = scope.ServiceProvider.GetRequiredService<IImageProcessingService>();
            
            // For ZIP files, we can't easily extract dimensions without extracting the file
            if (imagePath.Contains("#"))
            {
                _logger.LogDebug("📦 ZIP file entry detected, skipping dimension extraction for {Path}", imagePath);
                return (0, 0); // Will be extracted during image processing
            }
            
            // For regular files, try to extract dimensions
            if (File.Exists(imagePath))
            {
                var metadata = await imageProcessingService.ExtractMetadataAsync(imagePath);
                if (metadata != null)
                {
                    // Note: The current ImageMetadata doesn't expose width/height
                    // This would need to be enhanced in the IImageProcessingService
                    _logger.LogDebug("📊 Extracted metadata for {Path}", imagePath);
                    return (0, 0); // Will be extracted during image processing
                }
            }
            
            return (0, 0); // Default to 0, will be determined during processing
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Failed to extract dimensions for {Path}, will be determined during processing", imagePath);
            return (0, 0);
        }
    }

    private async Task MonitorJobCompletionAsync(
        ObjectId jobId, 
        ObjectId collectionId, 
        int expectedCount,
        IBackgroundJobService backgroundJobService,
        ICollectionService collectionService)
    {
        try
        {
            _logger.LogInformation("📊 Starting completion monitor for job {JobId}, expecting {Count} images", jobId, expectedCount);
            
            int thumbnailCount = 0;
            int cacheCount = 0;
            int checksWithoutProgress = 0;
            int maxChecks = 60; // Max 5 minutes (60 * 5 seconds)
            
            for (int i = 0; i < maxChecks; i++)
            {
                await Task.Delay(5000); // Check every 5 seconds
                
                // Get collection and count thumbnails/cache
                var collection = await collectionService.GetCollectionByIdAsync(collectionId);
                if (collection == null) break;
                
                int newThumbnailCount = collection.Thumbnails?.Count ?? 0;
                int newCacheCount = collection.Images?.Count(img => img.CacheInfo != null) ?? 0;
                
                bool thumbnailComplete = newThumbnailCount >= expectedCount;
                bool cacheComplete = newCacheCount >= expectedCount;
                
                // Update thumbnail stage if changed
                if (newThumbnailCount != thumbnailCount)
                {
                    thumbnailCount = newThumbnailCount;
                    
                    if (thumbnailComplete)
                    {
                        await backgroundJobService.UpdateJobStageAsync(jobId, "thumbnail", "Completed", thumbnailCount, expectedCount, $"All {expectedCount} thumbnails generated");
                        _logger.LogInformation("🎊 Thumbnail stage completed for job {JobId}: {Count}/{Total}", jobId, thumbnailCount, expectedCount);
                    }
                    else
                    {
                        await backgroundJobService.UpdateJobStageAsync(jobId, "thumbnail", "InProgress", thumbnailCount, expectedCount, $"Generated {thumbnailCount}/{expectedCount} thumbnails");
                    }
                    checksWithoutProgress = 0;
                }
                
                // Update cache stage if changed
                if (newCacheCount != cacheCount)
                {
                    cacheCount = newCacheCount;
                    
                    if (cacheComplete)
                    {
                        await backgroundJobService.UpdateJobStageAsync(jobId, "cache", "Completed", cacheCount, expectedCount, $"All {expectedCount} cache files generated");
                        _logger.LogInformation("🎊 Cache stage completed for job {JobId}: {Count}/{Total}", jobId, cacheCount, expectedCount);
                    }
                    else
                    {
                        await backgroundJobService.UpdateJobStageAsync(jobId, "cache", "InProgress", cacheCount, expectedCount, $"Generated {cacheCount}/{expectedCount} cache files");
                    }
                    checksWithoutProgress = 0;
                }
                
                // Both complete? Exit monitoring
                if (thumbnailComplete && cacheComplete)
                {
                    _logger.LogInformation("🎉 Job {JobId} fully completed! Thumbnails: {Thumbs}, Cache: {Cache}", jobId, thumbnailCount, cacheCount);
                    break;
                }
                
                // No progress for too long? Exit to prevent infinite loop
                if (newThumbnailCount == thumbnailCount && newCacheCount == cacheCount)
                {
                    checksWithoutProgress++;
                    if (checksWithoutProgress >= 12) // 1 minute without progress
                    {
                        _logger.LogWarning("⚠️ Job {JobId} monitoring stopped - no progress for 1 minute. Thumbnails: {Thumbs}/{Total}, Cache: {Cache}/{Total}", 
                            jobId, thumbnailCount, expectedCount, cacheCount, expectedCount);
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in job completion monitoring for {JobId}", jobId);
        }
    }

    private class MediaFileInfo
    {
        public string FullPath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }
}