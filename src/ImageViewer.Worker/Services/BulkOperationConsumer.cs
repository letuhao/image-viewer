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
/// Consumer for bulk operation messages
/// </summary>
public class BulkOperationConsumer : BaseMessageConsumer
{
    private readonly IServiceProvider _serviceProvider;

    public BulkOperationConsumer(
        IConnection connection,
        IOptions<RabbitMQOptions> options,
        IServiceProvider serviceProvider,
        ILogger<BulkOperationConsumer> logger)
        : base(connection, options, logger, options.Value.BulkOperationQueue, "bulk-operation-consumer")
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ProcessMessageAsync(string message, BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        try
        {
            var bulkMessage = JsonSerializer.Deserialize<BulkOperationMessage>(message);
            if (bulkMessage == null)
            {
                _logger.LogWarning("Failed to deserialize BulkOperationMessage");
                return;
            }

            _logger.LogInformation("Processing bulk operation {OperationType} for {CollectionCount} collections", 
                bulkMessage.OperationType, bulkMessage.CollectionIds.Count);

            using var scope = _serviceProvider.CreateScope();
            var bulkService = scope.ServiceProvider.GetRequiredService<IBulkService>();

            switch (bulkMessage.OperationType.ToLowerInvariant())
            {
                case "scanall":
                    await bulkService.ScanAllCollectionsAsync(cancellationToken);
                    break;
                case "generateallthumbnails":
                    await bulkService.GenerateAllThumbnailsAsync(cancellationToken);
                    break;
                case "generateallcache":
                    await bulkService.GenerateAllCacheAsync(cancellationToken);
                    break;
                case "scancollections":
                    await bulkService.ScanCollectionsAsync(bulkMessage.CollectionIds, cancellationToken);
                    break;
                case "generatethumbnails":
                    await bulkService.GenerateThumbnailsForCollectionsAsync(bulkMessage.CollectionIds, cancellationToken);
                    break;
                case "generatecache":
                    await bulkService.GenerateCacheForCollectionsAsync(bulkMessage.CollectionIds, cancellationToken);
                    break;
                default:
                    _logger.LogWarning("Unknown bulk operation type: {OperationType}", bulkMessage.OperationType);
                    break;
            }

            _logger.LogInformation("Successfully completed bulk operation {OperationType}", bulkMessage.OperationType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bulk operation message");
            throw;
        }
    }
}
