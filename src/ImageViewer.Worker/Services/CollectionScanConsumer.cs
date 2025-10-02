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
/// Consumer for collection scan messages
/// </summary>
public class CollectionScanConsumer : BaseMessageConsumer
{
    private readonly IServiceProvider _serviceProvider;

    public CollectionScanConsumer(
        IConnection connection,
        IOptions<RabbitMQOptions> options,
        IServiceProvider serviceProvider,
        ILogger<CollectionScanConsumer> logger)
        : base(connection, options, logger, options.Value.CollectionScanQueue, "collection-scan-consumer")
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ProcessMessageAsync(string message, BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        try
        {
            var scanMessage = JsonSerializer.Deserialize<CollectionScanMessage>(message);
            if (scanMessage == null)
            {
                _logger.LogWarning("Failed to deserialize CollectionScanMessage");
                return;
            }

            _logger.LogInformation("Processing collection scan for collection {CollectionId} at path {Path}", 
                scanMessage.CollectionId, scanMessage.CollectionPath);

            using var scope = _serviceProvider.CreateScope();
            var collectionService = scope.ServiceProvider.GetRequiredService<ICollectionService>();
            var fileScannerService = scope.ServiceProvider.GetRequiredService<IFileScannerService>();

            // Perform the collection scan
            await collectionService.ScanCollectionAsync(scanMessage.CollectionId, cancellationToken);

            _logger.LogInformation("Successfully completed collection scan for collection {CollectionId}", scanMessage.CollectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing collection scan message");
            throw;
        }
    }
}
