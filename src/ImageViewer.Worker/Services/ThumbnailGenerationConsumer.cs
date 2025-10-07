using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ImageViewer.Domain.Events;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Application.Services;
using ImageViewer.Infrastructure.Data;
using MongoDB.Bson;

namespace ImageViewer.Worker.Services;

/// <summary>
/// Consumer for thumbnail generation messages
/// </summary>
public class ThumbnailGenerationConsumer : BaseMessageConsumer
{
    private readonly IServiceProvider _serviceProvider;

    public ThumbnailGenerationConsumer(
        IConnection connection,
        IOptions<RabbitMQOptions> options,
        IServiceProvider serviceProvider,
        ILogger<ThumbnailGenerationConsumer> logger)
        : base(connection, options, logger, "thumbnail.generation", "thumbnail-generation-consumer")
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("🖼️ Received thumbnail generation message: {Message}", message);
            
            var thumbnailMessage = JsonSerializer.Deserialize<ThumbnailGenerationMessage>(message);
            if (thumbnailMessage == null)
            {
                _logger.LogWarning("❌ Failed to deserialize ThumbnailGenerationMessage from: {Message}", message);
                return;
            }

            _logger.LogInformation("🖼️ Generating thumbnail for image {ImageId} ({Filename})", 
                thumbnailMessage.ImageId, thumbnailMessage.ImageFilename);

            using var scope = _serviceProvider.CreateScope();
            var imageProcessingService = scope.ServiceProvider.GetRequiredService<IImageProcessingService>();
            var imageService = scope.ServiceProvider.GetRequiredService<IImageService>();

            // Check if image file exists
            if (!File.Exists(thumbnailMessage.ImagePath) && !thumbnailMessage.ImagePath.Contains("#"))
            {
                _logger.LogWarning("❌ Image file {Path} does not exist, skipping thumbnail generation", thumbnailMessage.ImagePath);
                return;
            }

            // Generate thumbnail
            try
            {
                var thumbnailPath = await GenerateThumbnail(
                    thumbnailMessage.ImagePath, 
                    thumbnailMessage.ThumbnailWidth, 
                    thumbnailMessage.ThumbnailHeight,
                    imageProcessingService,
                    cancellationToken);

                if (!string.IsNullOrEmpty(thumbnailPath))
                {
                    // Update database with thumbnail information
                    await UpdateThumbnailInfoInDatabase(thumbnailMessage, thumbnailPath, imageService);
                    
                    _logger.LogInformation("✅ Successfully generated thumbnail for image {ImageId} at {ThumbnailPath}", 
                        thumbnailMessage.ImageId, thumbnailPath);
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing thumbnail generation message");
            throw;
        }
    }

    private async Task<string?> GenerateThumbnail(string imagePath, int width, int height, IImageProcessingService imageProcessingService, CancellationToken cancellationToken = default)
    {
        try
        {
            // Determine thumbnail path
            var thumbnailPath = GetThumbnailPath(imagePath, width, height);
            
            // Ensure thumbnail directory exists
            var thumbnailDir = Path.GetDirectoryName(thumbnailPath);
            if (!string.IsNullOrEmpty(thumbnailDir) && !Directory.Exists(thumbnailDir))
            {
                Directory.CreateDirectory(thumbnailDir);
            }

            // Generate thumbnail using image processing service
            var thumbnailData = await imageProcessingService.GenerateThumbnailAsync(
                imagePath, 
                width, 
                height, 
                cancellationToken);

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

    private static string GetThumbnailPath(string imagePath, int width, int height)
    {
        var fileName = Path.GetFileNameWithoutExtension(imagePath);
        var extension = Path.GetExtension(imagePath);
        var directory = Path.GetDirectoryName(imagePath);
        
        if (string.IsNullOrEmpty(directory))
        {
            directory = ".";
        }

        // Create thumbnail subdirectory
        var thumbnailDir = Path.Combine(directory, "thumbnails");
        var thumbnailFileName = $"{fileName}_{width}x{height}{extension}";
        
        return Path.Combine(thumbnailDir, thumbnailFileName);
    }

    private async Task UpdateThumbnailInfoInDatabase(ThumbnailGenerationMessage thumbnailMessage, string thumbnailPath, IImageService imageService)
    {
        try
        {
            // Convert Guid back to ObjectId for database operations
            var imageId = ObjectId.Parse(thumbnailMessage.ImageId.ToString());
            
            _logger.LogInformation("📝 Updating thumbnail info in database for image {ImageId}", imageId);
            
            // Check if thumbnail info already exists for this image and dimensions
            using var scope = _serviceProvider.CreateScope();
            var thumbnailInfoRepository = scope.ServiceProvider.GetRequiredService<IThumbnailInfoRepository>();
            
            var existingThumbnailInfo = await thumbnailInfoRepository.GetByImageIdAndDimensionsAsync(
                imageId, 
                thumbnailMessage.ThumbnailWidth, 
                thumbnailMessage.ThumbnailHeight);
            
            if (existingThumbnailInfo != null)
            {
                _logger.LogInformation("📝 Updating existing thumbnail info for image {ImageId}", imageId);
                
                // Update existing thumbnail info
                existingThumbnailInfo.UpdateThumbnailPath(thumbnailPath);
                existingThumbnailInfo.UpdateFileSize(new FileInfo(thumbnailPath).Length);
                existingThumbnailInfo.ExtendExpiration(DateTime.UtcNow.AddDays(30));
                existingThumbnailInfo.MarkAsValid();
                
                await thumbnailInfoRepository.UpdateAsync(existingThumbnailInfo);
                _logger.LogInformation("✅ Thumbnail info updated for image {ImageId}: {ThumbnailPath}", 
                    imageId, thumbnailPath);
            }
            else
            {
                _logger.LogInformation("📝 Creating new thumbnail info for image {ImageId}", imageId);
                
                // Get file info for the thumbnail file
                var fileInfo = new FileInfo(thumbnailPath);
                var expiresAt = DateTime.UtcNow.AddDays(30); // Thumbnail expires in 30 days
                
                // Create new thumbnail info entity
                var thumbnailInfo = new ThumbnailInfo(
                    imageId,
                    thumbnailPath,
                    thumbnailMessage.ThumbnailWidth,
                    thumbnailMessage.ThumbnailHeight,
                    fileInfo.Length,
                    expiresAt
                );
                
                // Persist the thumbnail info to the database
                await thumbnailInfoRepository.CreateAsync(thumbnailInfo);
                
                _logger.LogInformation("✅ Thumbnail info created and persisted for image {ImageId}: {ThumbnailPath}", 
                    imageId, thumbnailPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error updating thumbnail info in database for image {ImageId}", thumbnailMessage.ImageId);
        }
    }
}