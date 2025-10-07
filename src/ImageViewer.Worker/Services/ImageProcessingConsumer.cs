using System.Text.Json;
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
    private readonly IServiceProvider _serviceProvider;

    public ImageProcessingConsumer(
        IConnection connection,
        IOptions<RabbitMQOptions> options,
        IServiceProvider serviceProvider,
        ILogger<ImageProcessingConsumer> logger)
        : base(connection, options, logger, "image.processing", "image-processing-consumer")
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("🖼️ Received image processing message: {Message}", message);
            
            var imageMessage = JsonSerializer.Deserialize<ImageProcessingMessage>(message);
            if (imageMessage == null)
            {
                _logger.LogWarning("❌ Failed to deserialize ImageProcessingMessage from: {Message}", message);
                return;
            }

            _logger.LogInformation("🖼️ Processing image {ImageId} at path {Path}", 
                imageMessage.ImageId, imageMessage.ImagePath);

            using var scope = _serviceProvider.CreateScope();
            var imageService = scope.ServiceProvider.GetRequiredService<IImageService>();
            var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();

            // Check if image file exists
            if (!File.Exists(imageMessage.ImagePath) && !imageMessage.ImagePath.Contains("#"))
            {
                _logger.LogWarning("❌ Image file {Path} does not exist, skipping processing", imageMessage.ImagePath);
                return;
            }

            // Create or update image entity
            var image = await CreateOrUpdateImageEntity(imageMessage, imageService);
            if (image == null)
            {
                _logger.LogWarning("❌ Failed to create/update image entity for {Path}", imageMessage.ImagePath);
                return;
            }

            // Generate thumbnail if requested
            if (imageMessage.GenerateThumbnail)
            {
                try
                {
                var thumbnailMessage = new ThumbnailGenerationMessage
                {
                    ImageId = Guid.Parse(image.Id.ToString()),
                    CollectionId = Guid.Parse(imageMessage.CollectionId.ToString()),
                    ImagePath = imageMessage.ImagePath,
                    ImageFilename = Path.GetFileName(imageMessage.ImagePath),
                    ThumbnailWidth = 300, // Default thumbnail size
                    ThumbnailHeight = 300
                };

                    // Queue the thumbnail generation job
                    await messageQueueService.PublishAsync(thumbnailMessage, "thumbnail.generation");
                    _logger.LogInformation("📋 Queued thumbnail generation job for image {ImageId}", image.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to create thumbnail generation job for image {ImageId}", image.Id);
                }
            }

            // Queue cache generation if needed
            try
            {
                var cacheMessage = new CacheGenerationMessage
                {
                    ImageId = image.Id,
                    CollectionId = imageMessage.CollectionId,
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
                _logger.LogInformation("📋 Queued cache generation job for image {ImageId}", image.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to create cache generation job for image {ImageId}", image.Id);
            }

            _logger.LogInformation("✅ Successfully processed image {ImageId}", image.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing image processing message");
            throw;
        }
    }

    private async Task<Domain.Entities.Image?> CreateOrUpdateImageEntity(ImageProcessingMessage imageMessage, IImageService imageService)
    {
        try
        {
            _logger.LogInformation("➕ Creating/updating image entity for path {Path}", imageMessage.ImagePath);
            
            // Extract actual image metadata if not provided
            var width = imageMessage.Width;
            var height = imageMessage.Height;
            var fileSize = imageMessage.FileSize;
            
            if (width == 0 || height == 0 || fileSize == 0)
            {
                using var scope = _serviceProvider.CreateScope();
                var imageProcessingService = scope.ServiceProvider.GetRequiredService<IImageProcessingService>();
                
                try
                {
                    var metadata = await imageProcessingService.ExtractMetadataAsync(imageMessage.ImagePath);
                    if (metadata != null)
                    {
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
            
            // Create image entity with proper metadata
            var image = new Domain.Entities.Image(
                imageMessage.CollectionId,
                Path.GetFileName(imageMessage.ImagePath),
                GetRelativePath(imageMessage.ImagePath, imageMessage.CollectionId),
                fileSize,
                width,
                height,
                imageMessage.ImageFormat
            );
            
            // Persist the image entity to the database
            using var repositoryScope = _serviceProvider.CreateScope();
            var imageRepository = repositoryScope.ServiceProvider.GetRequiredService<IImageRepository>();
            var persistedImage = await imageRepository.CreateAsync(image);
            
            _logger.LogInformation("✅ Created and persisted image entity {ImageId} for {Path}", persistedImage.Id, imageMessage.ImagePath);
            return persistedImage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creating/updating image entity for path {Path}", imageMessage.ImagePath);
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
}
