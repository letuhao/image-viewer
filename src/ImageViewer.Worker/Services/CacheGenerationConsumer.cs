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

    protected override async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
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

            // Generate cache image using ResizeImageAsync
            var cacheImageData = await imageProcessingService.ResizeImageAsync(
                cacheMessage.ImagePath,
                cacheMessage.CacheWidth,
                cacheMessage.CacheHeight,
                90, // quality
                cancellationToken);

            // Save cache image to file system (this would need to be implemented)
            var cachePath = Path.Combine("cache", $"{cacheMessage.ImageId}_cache.jpg");
            await File.WriteAllBytesAsync(cachePath, cacheImageData, cancellationToken);

            // Note: UpdateImageCacheInfoAsync method needs to be implemented in ICacheService
            // For now, we'll just log the cache generation
            _logger.LogInformation("Cache generated for image {ImageId} at path {CachePath} with dimensions {Width}x{Height}", 
                cacheMessage.ImageId, cachePath, cacheMessage.CacheWidth, cacheMessage.CacheHeight);

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
