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
/// Consumer for cache generation messages
/// </summary>
public class CacheGenerationConsumer : BaseMessageConsumer
{
    private readonly IServiceProvider _serviceProvider;

    public CacheGenerationConsumer(
        IConnection connection,
        IOptions<RabbitMQOptions> options,
        IServiceProvider serviceProvider,
        ILogger<CacheGenerationConsumer> logger)
        : base(connection, options, logger, options.Value.CacheGenerationQueue, "cache-generation-consumer")
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ProcessMessageAsync(string message, BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        try
        {
            var cacheMessage = JsonSerializer.Deserialize<CacheGenerationMessage>(message);
            if (cacheMessage == null)
            {
                _logger.LogWarning("Failed to deserialize CacheGenerationMessage");
                return;
            }

            _logger.LogInformation("Processing cache generation for image {ImageId} ({Filename})", 
                cacheMessage.ImageId, cacheMessage.ImageFilename);

            using var scope = _serviceProvider.CreateScope();
            var imageProcessingService = scope.ServiceProvider.GetRequiredService<IImageProcessingService>();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            // Generate cache image
            var cachePath = await imageProcessingService.GenerateCacheAsync(
                cacheMessage.ImagePath,
                cacheMessage.CacheWidth,
                cacheMessage.CacheHeight,
                cancellationToken);

            // Update cache info in database
            await cacheService.UpdateImageCacheInfoAsync(
                cacheMessage.ImageId,
                cachePath,
                cacheMessage.CacheWidth,
                cacheMessage.CacheHeight,
                cancellationToken);

            _logger.LogInformation("Successfully generated cache for image {ImageId}", cacheMessage.ImageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing cache generation message for image {ImageId}", 
                JsonSerializer.Deserialize<CacheGenerationMessage>(message)?.ImageId);
            throw;
        }
    }
}
