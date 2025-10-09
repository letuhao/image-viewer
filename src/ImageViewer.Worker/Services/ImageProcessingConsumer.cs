using System.Text.Json;
using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ImageViewer.Domain.Events;
using ImageViewer.Domain.Interfaces;
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

            _logger.LogInformation("🖼️ Received image processing message: {Message}", message);
            
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

            _logger.LogInformation("🖼️ Processing image {ImageId} at path {Path}", 
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
                var imageService = scope.ServiceProvider.GetRequiredService<IImageService>();
                var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();

                // Check if image file exists (handle both regular files and ZIP entries)
                bool isZipEntry = imageMessage.ImagePath.Contains("#");
                if (!isZipEntry && !File.Exists(imageMessage.ImagePath))
                {
                    _logger.LogWarning("❌ Image file {Path} does not exist, skipping processing", imageMessage.ImagePath);
                    return;
                }

                // For ZIP entries, validate ZIP file exists
                if (isZipEntry)
                {
                    var zipPath = imageMessage.ImagePath.Split('#')[0];
                    if (!File.Exists(zipPath))
                    {
                        _logger.LogWarning("❌ ZIP file {Path} does not exist, skipping processing", zipPath);
                        return;
                    }
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
                var thumbnailMessage = new ThumbnailGenerationMessage
                {
                    ImageId = embeddedImage.Id, // Already a string
                    CollectionId = imageMessage.CollectionId, // Already a string
                    ImagePath = imageMessage.ImagePath,
                    ImageFilename = Path.GetFileName(imageMessage.ImagePath),
                    ThumbnailWidth = 300, // Default thumbnail size
                    ThumbnailHeight = 300
                };

                    // Queue the thumbnail generation job
                    await messageQueueService.PublishAsync(thumbnailMessage, "thumbnail.generation");
                    _logger.LogInformation("📋 Queued thumbnail generation job for image {ImageId}", embeddedImage.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to create thumbnail generation job for image {ImageId}", embeddedImage.Id);
                }
            }

            // Queue cache generation if needed
            try
            {
                var cacheMessage = new CacheGenerationMessage
                {
                    ImageId = embeddedImage.Id, // Already a string
                    CollectionId = imageMessage.CollectionId, // Already a string
                    ImagePath = imageMessage.ImagePath,
                    CachePath = "", // Will be determined by cache service
                    CacheWidth = 1920, // Default cache size
                    CacheHeight = 1080,
                    Quality = 85,
                    ForceRegenerate = false,
                    CreatedBy = "ImageProcessingConsumer",
                    CreatedBySystem = "ImageViewer.Worker"
                };

                // Queue the cache generation job
                await messageQueueService.PublishAsync(cacheMessage, "cache.generation");
                _logger.LogInformation("📋 Queued cache generation job for image {ImageId}", embeddedImage.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to create cache generation job for image {ImageId}", embeddedImage.Id);
            }

            _logger.LogInformation("✅ Successfully processed image {ImageId}", embeddedImage.Id);
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
            _logger.LogInformation("➕ Creating/updating embedded image for path {Path}", imageMessage.ImagePath);
            
            // Extract actual image metadata if not provided
            var width = imageMessage.Width;
            var height = imageMessage.Height;
            var fileSize = imageMessage.FileSize;
            
            if (width == 0 || height == 0 || fileSize == 0)
            {
                // Check if this is a ZIP entry
                bool isZipEntry = imageMessage.ImagePath.Contains("#");
                
                if (isZipEntry)
                {
                    // Extract dimensions from ZIP entry
                    var (zipWidth, zipHeight, zipSize) = await ExtractZipEntryMetadata(imageMessage.ImagePath, cancellationToken);
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
                        
                        _logger.LogInformation("📊 Extracted metadata: {Width}x{Height}, {FileSize} bytes", 
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
            var filename = Path.GetFileName(imageMessage.ImagePath);
            var relativePath = GetRelativePath(imageMessage.ImagePath, collectionId);
            
            var embeddedImage = await imageService.CreateEmbeddedImageAsync(
                collectionId,
                filename,
                relativePath,
                fileSize,
                width,
                height,
                imageMessage.ImageFormat
            );
            
            _logger.LogInformation("✅ Created embedded image {ImageId} for {Path}", embeddedImage.Id, imageMessage.ImagePath);
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
        // For ZIP files, extract the relative path from the ZIP entry
        if (fullPath.Contains("#"))
        {
            var parts = fullPath.Split('#');
            return parts.Length > 1 ? parts[1] : Path.GetFileName(fullPath);
        }
        
        // For regular files, return just the filename for now
        // In a real implementation, you'd want to store the full relative path
        return Path.GetFileName(fullPath);
    }

    /// <summary>
    /// Extract metadata from a ZIP entry (path format: zipfile.zip#entry.png)
    /// </summary>
    private async Task<(int width, int height, long fileSize)> ExtractZipEntryMetadata(string zipEntryPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var parts = zipEntryPath.Split('#');
            if (parts.Length != 2)
            {
                _logger.LogWarning("Invalid ZIP entry path format: {Path}", zipEntryPath);
                return (0, 0, 0);
            }

            var zipPath = parts[0];
            var entryName = parts[1];

            if (!File.Exists(zipPath))
            {
                _logger.LogWarning("ZIP file not found: {Path}", zipPath);
                return (0, 0, 0);
            }

            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.GetEntry(entryName);
            if (entry == null)
            {
                _logger.LogWarning("Entry {Entry} not found in ZIP {Zip}", entryName, zipPath);
                return (0, 0, 0);
            }

            // Extract entry to memory stream and get dimensions
            using var entryStream = entry.Open();
            using var memoryStream = new MemoryStream();
            await entryStream.CopyToAsync(memoryStream, cancellationToken);
            var imageBytes = memoryStream.ToArray();

            // Use SkiaSharp to get dimensions from bytes
            using var data = SkiaSharp.SKData.CreateCopy(imageBytes);
            using var codec = SkiaSharp.SKCodec.Create(data);
            
            if (codec == null)
            {
                _logger.LogWarning("Failed to decode image from ZIP entry: {Path}", zipEntryPath);
                return (0, 0, entry.Length);
            }

            var info = codec.Info;
            _logger.LogDebug("ZIP entry {Entry}: {Width}x{Height}, {Size} bytes", entryName, info.Width, info.Height, entry.Length);
            
            return (info.Width, info.Height, entry.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting metadata from ZIP entry: {Path}", zipEntryPath);
            return (0, 0, 0);
        }
    }
}
