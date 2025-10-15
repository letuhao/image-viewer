using System.Text.Json;
using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ImageViewer.Domain.Events;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Application.Services;
using ImageViewer.Infrastructure.Data;
using MongoDB.Bson;

namespace ImageViewer.Worker.Services;

/// <summary>
/// Consumer for image processing messages
/// </summary>
public class ImageProcessingConsumer : BaseMessageConsumer
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private int _processedCount = 0;
    private readonly object _counterLock = new object();

    public ImageProcessingConsumer(
        IConnection connection,
        IOptions<RabbitMQOptions> options,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ImageProcessingConsumer> logger)
        : base(connection, options, logger, "image.processing", "image-processing-consumer")
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            // Check if cancellation requested before processing
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("⚠️ Cancellation requested, skipping message processing");
                return;
            }

            _logger.LogDebug("🖼️ Received image processing message: {Message}", message);
            
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            var imageMessage = JsonSerializer.Deserialize<ImageProcessingMessage>(message, options);
            if (imageMessage == null)
            {
                _logger.LogWarning("❌ Failed to deserialize ImageProcessingMessage from: {Message}", message);
                return;
            }

            _logger.LogDebug("🖼️ Processing image {ImageId} at path {Path}", 
                imageMessage.ImageId, imageMessage.ImagePath);

            // Try to create scope, handle disposal gracefully
            IServiceScope? scope = null;
            try
            {
                scope = _serviceScopeFactory.CreateScope();
            }
            catch (ObjectDisposedException)
            {
                _logger.LogWarning("⚠️ Service provider disposed, worker is shutting down. Skipping message.");
                return;
            }

            using (scope)
            {
                var serviceProvider = scope.ServiceProvider;
                var imageService = serviceProvider.GetRequiredService<IImageService>();
                var messageQueueService = serviceProvider.GetRequiredService<IMessageQueueService>();

                // Check if image file exists (handle both regular files and ZIP entries)
                // Use new DTO structure directly from message
                if (imageMessage.ArchiveEntry != null)
                {
                    // This is an archive entry - validate the archive file exists
                    if (!File.Exists(imageMessage.ArchiveEntry.ArchivePath))
                    {
                        _logger.LogWarning("❌ Archive file {Path} does not exist, skipping processing", imageMessage.ArchiveEntry.ArchivePath);
                        return;
                    }
                }
                else if (!File.Exists(imageMessage.ImagePath))
                {
                    // Regular file - check if it exists
                    _logger.LogWarning("❌ Image file {Path} does not exist, skipping processing", imageMessage.ImagePath);
                    return;
                }

                // Create or update embedded image (handles both regular files and ZIP entries)
                var embeddedImage = await CreateOrUpdateEmbeddedImage(imageMessage, imageService, scope.ServiceProvider, cancellationToken);
            if (embeddedImage == null)
            {
                _logger.LogWarning("❌ Failed to create/update embedded image for {Path}", imageMessage.ImagePath);
                return;
            }

            // Generate thumbnail if requested
            if (imageMessage.GenerateThumbnail)
            {
                try
                {
                    // Load thumbnail settings from system settings
                    var imageProcessingSettingsService = serviceProvider.GetService<IImageProcessingSettingsService>();
                    int thumbnailWidth = 300; // Default fallback
                    int thumbnailHeight = 300; // Default fallback
                    
                    if (imageProcessingSettingsService != null)
                    {
                        try
                        {
                            thumbnailWidth = await imageProcessingSettingsService.GetThumbnailSizeAsync();
                            thumbnailHeight = thumbnailWidth; // Use same size for width and height
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Failed to load thumbnail size from ImageProcessingSettingsService, using default 300x300");
                        }
                    }
                    
                    var thumbnailMessage = new ThumbnailGenerationMessage
                    {
                        ImageId = embeddedImage.Id, // Already a string
                        CollectionId = imageMessage.CollectionId, // Already a string
                        ImagePath = imageMessage.ImagePath,
                        ImageFilename = Path.GetFileName(imageMessage.ImagePath),
                        ThumbnailWidth = thumbnailWidth, // Loaded from system settings
                        ThumbnailHeight = thumbnailHeight, // Loaded from system settings
                        ScanJobId = imageMessage.ScanJobId // Pass scan job ID for tracking
                    };

                    // Queue the thumbnail generation job
                    await messageQueueService.PublishAsync(thumbnailMessage, "thumbnail.generation");
                    _logger.LogDebug("📋 Queued thumbnail generation job for image {ImageId}", embeddedImage.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to create thumbnail generation job for image {ImageId}", embeddedImage.Id);
                }
            }

            // Queue cache generation if needed
            try
            {
                // Load cache settings from system settings (if available)
                var systemSettingService = serviceProvider.GetService<ISystemSettingService>();
                var imageProcessingSettingsService = serviceProvider.GetService<IImageProcessingSettingsService>();
                var cacheFolderSelectionService = serviceProvider.GetService<ICacheFolderSelectionService>();
                
                int cacheQuality = 85; // Optimized for web (default)
                string cacheFormat = "jpeg"; // Default
                int cacheWidth = 1920; // Default
                int cacheHeight = 1080; // Default
                bool preserveOriginal = false; // Default

                // Get settings from IImageProcessingSettingsService (prioritized) or fallback to SystemSettingService
                _logger.LogDebug("🔧 ImageProcessingConsumer: imageProcessingSettingsService is {Status}", 
                    imageProcessingSettingsService != null ? "available" : "null");
                    
                if (imageProcessingSettingsService != null)
                {
                    try
                    {
                        cacheFormat = await imageProcessingSettingsService.GetCacheFormatAsync();
                        cacheQuality = await imageProcessingSettingsService.GetCacheQualityAsync();
                        _logger.LogDebug("🔧 ImageProcessingConsumer: Loaded cache settings - Format: {Format}, Quality: {Quality}", 
                            cacheFormat, cacheQuality);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to load settings from ImageProcessingSettingsService, trying SystemSettingService");
                    }
                }
                else if (systemSettingService != null)
                {
                    try
                    {
                        cacheQuality = await systemSettingService.GetDefaultCacheQualityAsync();
                        cacheFormat = await systemSettingService.GetDefaultCacheFormatAsync();
                        (cacheWidth, cacheHeight) = await systemSettingService.GetDefaultCacheDimensionsAsync();
                        preserveOriginal = await systemSettingService.GetCachePreserveOriginalAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to load cache settings from system settings, using defaults");
                    }
                }

                // **SMART CACHE FOLDER DISTRIBUTION**: Select cache folder BEFORE enqueueing
                var collectionObjectId = ObjectId.Parse(imageMessage.CollectionId);
                string? cachePath = null;
                
                if (cacheFolderSelectionService != null)
                {
                    _logger.LogDebug("🔧 ImageProcessingConsumer: Calling SelectCacheFolderForCacheAsync with format: {Format}", cacheFormat);
                    cachePath = await cacheFolderSelectionService.SelectCacheFolderForCacheAsync(
                        collectionObjectId,
                        embeddedImage.Id,
                        cacheWidth,
                        cacheHeight,
                        cacheFormat);
                    _logger.LogDebug("🔧 ImageProcessingConsumer: Generated cache path: {CachePath}", cachePath);
                }

                if (string.IsNullOrEmpty(cachePath))
                {
                    _logger.LogWarning("⚠️ Failed to select cache folder for image {ImageId}, job will determine path", embeddedImage.Id);
                    cachePath = ""; // Fallback: consumer will determine
                }

                _logger.LogDebug("🔧 ImageProcessingConsumer: Creating cache message with format: {Format}", cacheFormat);
                
                var cacheMessage = new CacheGenerationMessage
                {
                    ImageId = embeddedImage.Id, // Already a string
                    CollectionId = imageMessage.CollectionId, // Already a string
                    ImagePath = imageMessage.ImagePath,
                    CachePath = cachePath, // PRE-DETERMINED cache path for distribution
                    CacheWidth = cacheWidth,
                    CacheHeight = cacheHeight,
                    Quality = cacheQuality,
                    Format = cacheFormat,
                    PreserveOriginal = preserveOriginal,
                    ForceRegenerate = false,
                    CreatedBy = "ImageProcessingConsumer",
                    CreatedBySystem = "ImageViewer.Worker",
                    ScanJobId = imageMessage.ScanJobId // Pass scan job ID for tracking
                };

                // Queue the cache generation job
                await messageQueueService.PublishAsync(cacheMessage, "cache.generation");
                _logger.LogDebug("📋 Queued cache generation job for image {ImageId} → {CachePath}", embeddedImage.Id, cachePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to create cache generation job for image {ImageId}", embeddedImage.Id);
            }

            // Batched logging - log every 50 files to reduce log size by 50x
            int currentCount;
            lock (_counterLock)
            {
                _processedCount++;
                currentCount = _processedCount;
            }

            if (currentCount % 50 == 0)
            {
                _logger.LogInformation("✅ Processed {Count} images (latest: {ImageId})", currentCount, embeddedImage.Id);
            }
            else
            {
                _logger.LogDebug("✅ Successfully processed image {ImageId}", embeddedImage.Id);
            }
            } // Close the using (scope) block
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing image processing message");
            throw;
        }
    }

    private async Task<Domain.ValueObjects.ImageEmbedded?> CreateOrUpdateEmbeddedImage(ImageProcessingMessage imageMessage, IImageService imageService, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("➕ Creating/updating embedded image for path {Path}", imageMessage.ImagePath);
            
            // Extract actual image metadata if not provided
            var width = imageMessage.Width;
            var height = imageMessage.Height;
            var fileSize = imageMessage.FileSize;
            
            if (width == 0 || height == 0 || fileSize == 0)
            {
                // Check if this is a ZIP entry using new DTO structure
                if (imageMessage.ArchiveEntry != null)
                {
                    // Extract dimensions from ZIP entry
                    var (zipWidth, zipHeight, zipSize) = await ExtractZipEntryMetadata(imageMessage.ArchiveEntry, cancellationToken);
                    width = zipWidth;
                    height = zipHeight;
                    fileSize = zipSize > 0 ? zipSize : imageMessage.FileSize;
                }
                else
                {
                    // Regular file - use image processing service
                    var imageProcessingService = serviceProvider.GetRequiredService<IImageProcessingService>();
                    
                    try
                    {
                        var dimensions = await imageProcessingService.GetImageDimensionsAsync(imageMessage.ImagePath, cancellationToken);
                    if (dimensions != null)
                    {
                        width = dimensions.Width;
                        height = dimensions.Height;
                        
                        // Get file info for size if not provided
                        if (fileSize == 0 && File.Exists(imageMessage.ImagePath))
                        {
                            var fileInfo = new FileInfo(imageMessage.ImagePath);
                            fileSize = fileInfo.Length;
                        }
                        
                        _logger.LogDebug("📊 Extracted metadata: {Width}x{Height}, {FileSize} bytes", 
                            width, height, fileSize);
                    }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Failed to extract metadata for {Path}, using provided values", imageMessage.ImagePath);
                    }
                }
            }
            
            // Create embedded image using the new service
            var collectionId = ObjectId.Parse(imageMessage.CollectionId); // Convert string back to ObjectId
            
            // Extract filename properly (handle archive entries)
            string filename;
            string relativePath;
            
            // Use new DTO structure directly from message
            if (imageMessage.ArchiveEntry != null)
            {
                // This is an archive entry - extract the entry name
                filename = Path.GetFileName(imageMessage.ArchiveEntry.EntryName); // Just the entry filename
                relativePath = imageMessage.ArchiveEntry.EntryName; // Use entry path as relative path
            }
            else
            {
                // Regular file
                filename = Path.GetFileName(imageMessage.ImagePath);
                relativePath = GetRelativePath(imageMessage.ImagePath, collectionId);
            }
            
            var embeddedImage = await imageService.CreateEmbeddedImageAsync(
                collectionId,
                filename,
                relativePath,
                fileSize,
                width,
                height,
                imageMessage.ImageFormat
            );
            
            _logger.LogDebug("✅ Created embedded image {ImageId} for {Path}", embeddedImage.Id, imageMessage.ImagePath);
            return embeddedImage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creating/updating embedded image for path {Path}", imageMessage.ImagePath);
            return null;
        }
    }
    
    private string GetRelativePath(string fullPath, ObjectId collectionId)
    {
        // This method is only called for regular files now (when ArchiveEntry is null)
        // For regular files, return just the filename for now
        // In a real implementation, you'd want to store the full relative path
        return Path.GetFileName(fullPath);
    }

    /// <summary>
    /// Extract metadata from a ZIP entry using ArchiveEntryInfo DTO
    /// </summary>
    private async Task<(int width, int height, long fileSize)> ExtractZipEntryMetadata(ArchiveEntryInfo archiveEntry, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use shared ZIP helper to extract bytes
            var imageBytes = await ArchiveFileHelper.ExtractZipEntryBytes(archiveEntry.GetFullPath(), _logger, cancellationToken);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                _logger.LogWarning("Failed to extract bytes from ZIP entry: {Path}", archiveEntry.GetFullPath());
                return (0, 0, 0);
            }

            // Use SkiaSharp to get dimensions from bytes
            using var data = SkiaSharp.SKData.CreateCopy(imageBytes);
            using var codec = SkiaSharp.SKCodec.Create(data);
            
            if (codec == null)
            {
                _logger.LogWarning("Failed to decode image from ZIP entry: {Path}", archiveEntry.GetFullPath());
                return (0, 0, imageBytes.Length);
            }

            var info = codec.Info;
            // Use DTO directly - no need for string parsing
            var entryName = archiveEntry.EntryName;
            _logger.LogDebug("ZIP entry {Entry}: {Width}x{Height}, {Size} bytes", entryName, info.Width, info.Height, imageBytes.Length);
            
            return (info.Width, info.Height, imageBytes.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting metadata from ZIP entry: {Path}", archiveEntry.GetFullPath());
            return (0, 0, 0);
        }
    }
}
