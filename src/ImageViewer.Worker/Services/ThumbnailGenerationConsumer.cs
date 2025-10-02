using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ImageViewer.Domain.Events;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Infrastructure.Data;

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
            var thumbnailPath = await imageProcessingService.GenerateThumbnailAsync(
                thumbnailMessage.ImagePath,
                thumbnailMessage.ThumbnailWidth,
                thumbnailMessage.ThumbnailHeight,
                cancellationToken);

            // Update cache info in database
            await cacheService.UpdateImageCacheInfoAsync(
                thumbnailMessage.ImageId,
                thumbnailPath,
                thumbnailMessage.ThumbnailWidth,
                thumbnailMessage.ThumbnailHeight,
                cancellationToken);

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
