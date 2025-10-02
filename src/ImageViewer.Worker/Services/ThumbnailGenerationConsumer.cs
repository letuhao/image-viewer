using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ImageViewer.Domain.Events;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Infrastructure.Data;
using ImageViewer.Application.Services;

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
        : base(connection, options, logger, options.Value.ThumbnailGenerationQueue, "thumbnail-generation-consumer")
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ProcessMessageAsync(string message, BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        try
        {
            var thumbnailMessage = JsonSerializer.Deserialize<ThumbnailGenerationMessage>(message);
            if (thumbnailMessage == null)
            {
                _logger.LogWarning("Failed to deserialize ThumbnailGenerationMessage");
                return;
            }

            _logger.LogInformation("Processing thumbnail generation for image {ImageId} ({Filename})", 
                thumbnailMessage.ImageId, thumbnailMessage.ImageFilename);

            using var scope = _serviceProvider.CreateScope();
            var imageProcessingService = scope.ServiceProvider.GetRequiredService<IImageProcessingService>();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            // Generate thumbnail
            var thumbnailData = await imageProcessingService.GenerateThumbnailAsync(
                thumbnailMessage.ImagePath,
                thumbnailMessage.ThumbnailWidth,
                thumbnailMessage.ThumbnailHeight,
                cancellationToken);

            // Save thumbnail to file system (this would need to be implemented)
            var thumbnailPath = Path.Combine("thumbnails", $"{thumbnailMessage.ImageId}_thumb.jpg");
            await File.WriteAllBytesAsync(thumbnailPath, thumbnailData, cancellationToken);

            // Note: UpdateImageCacheInfoAsync method needs to be implemented in ICacheService
            // For now, we'll just log the thumbnail generation
            _logger.LogInformation("Thumbnail generated for image {ImageId} at path {ThumbnailPath} with dimensions {Width}x{Height}", 
                thumbnailMessage.ImageId, thumbnailPath, thumbnailMessage.ThumbnailWidth, thumbnailMessage.ThumbnailHeight);

            _logger.LogInformation("Successfully generated thumbnail for image {ImageId}", thumbnailMessage.ImageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing thumbnail generation message for image {ImageId}", 
                JsonSerializer.Deserialize<ThumbnailGenerationMessage>(message)?.ImageId);
            throw;
        }
    }
}
