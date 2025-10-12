using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ImageViewer.Infrastructure.Data;
using System.Text;
using System.Text.Json;

namespace ImageViewer.Worker.Services;

/// <summary>
/// 死信队列恢复服务 (Dead Letter Queue Recovery Service)
/// Dịch vụ khôi phục hàng đợi thư chết
/// 
/// Automatically recovers messages from DLQ on Worker startup
/// Maps all message types to their correct routing keys
/// </summary>
public class DlqRecoveryService : IHostedService
{
    private readonly ILogger<DlqRecoveryService> _logger;
    private readonly RabbitMQOptions _options;
    private readonly IConnectionFactory _connectionFactory;

    // Complete mapping of MessageType → RoutingKey
    // MessageType是从消息的headers中获取，而不是从x-death中获取
    private static readonly Dictionary<string, string> MessageTypeToRoutingKey = new()
    {
        { "CollectionScan", "collection.scan" },
        { "ThumbnailGeneration", "thumbnail.generation" },
        { "CacheGeneration", "cache.generation" },
        { "CollectionCreation", "collection.creation" },
        { "BulkOperation", "bulk.operation" },
        { "ImageProcessing", "image.processing" },
        { "LibraryScan", "library_scan_queue" }
    };

    public DlqRecoveryService(
        ILogger<DlqRecoveryService> logger,
        IOptions<RabbitMQOptions> options,
        IConnectionFactory connectionFactory)
    {
        _logger = logger;
        _options = options.Value;
        _connectionFactory = connectionFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔄 Starting DLQ Recovery Service...");

        try
        {
            await RecoverDlqMessagesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ DLQ recovery failed");
        }

        return;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DLQ Recovery Service stopped");
        return Task.CompletedTask;
    }

    private async Task RecoverDlqMessagesAsync(CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        const string dlqName = "imageviewer.dlq";

        // Get DLQ message count
        var queueInfo = await channel.QueueDeclarePassiveAsync(dlqName, cancellationToken);
        var messageCount = queueInfo.MessageCount;

        if (messageCount == 0)
        {
            _logger.LogInformation("✅ DLQ is empty. No messages to recover.");
            return;
        }

        _logger.LogWarning("⚠️  Found {MessageCount} messages in DLQ. Starting recovery...", messageCount);

        var stats = new Dictionary<string, int>();
        var totalRecovered = 0;
        var batchSize = 100;

        while (messageCount > 0 && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Get message from DLQ
                var result = await channel.BasicGetAsync(dlqName, autoAck: false, cancellationToken);

                if (result == null)
                {
                    // No more messages
                    break;
                }

                // Extract MessageType from headers to determine correct routing key
                string? messageType = null;
                string? originalRoutingKey = null;

                if (result.BasicProperties.Headers != null)
                {
                    // Try to get MessageType from headers (this is how we originally published)
                    if (result.BasicProperties.Headers.TryGetValue("MessageType", out var messageTypeObj))
                    {
                        messageType = messageTypeObj?.ToString();
                    }

                    // If MessageType not in headers, try x-death as fallback
                    if (string.IsNullOrEmpty(messageType) && 
                        result.BasicProperties.Headers.TryGetValue("x-death", out var xDeathObj))
                    {
                        if (xDeathObj is List<object> xDeathList && xDeathList.Count > 0)
                        {
                            if (xDeathList[0] is Dictionary<string, object> xDeath)
                            {
                                // Get original routing key from x-death
                                if (xDeath.TryGetValue("routing-keys", out var routingKeysObj))
                                {
                                    if (routingKeysObj is List<object> routingKeys && routingKeys.Count > 0)
                                    {
                                        originalRoutingKey = routingKeys[0]?.ToString();
                                    }
                                }
                            }
                        }
                    }
                }

                // Map MessageType to routing key
                if (!string.IsNullOrEmpty(messageType) && MessageTypeToRoutingKey.TryGetValue(messageType, out var mappedRoutingKey))
                {
                    originalRoutingKey = mappedRoutingKey;
                }

                if (string.IsNullOrEmpty(originalRoutingKey))
                {
                    _logger.LogWarning("⚠️  Message has no MessageType or routing key. MessageType={MessageType}. Skipping...", messageType);
                    await channel.BasicNackAsync(result.DeliveryTag, multiple: false, requeue: false, cancellationToken);
                    continue;
                }

                _logger.LogDebug("Processing DLQ message: MessageType={MessageType}, RoutingKey={RoutingKey}", messageType, originalRoutingKey);

                // Republish to original queue via exchange
                var properties = result.BasicProperties;
                
                // Remove x-death and x-first-death headers to prevent loops
                if (properties.Headers != null)
                {
                    properties.Headers.Remove("x-death");
                    properties.Headers.Remove("x-first-death-exchange");
                    properties.Headers.Remove("x-first-death-queue");
                    properties.Headers.Remove("x-first-death-reason");
                    properties.Headers.Remove("x-last-death-exchange");
                    properties.Headers.Remove("x-last-death-queue");
                    properties.Headers.Remove("x-last-death-reason");
                }

                await channel.BasicPublishAsync(
                    exchange: _options.DefaultExchange,
                    routingKey: originalRoutingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: result.Body,
                    cancellationToken: cancellationToken);

                // Acknowledge removal from DLQ
                await channel.BasicAckAsync(result.DeliveryTag, multiple: false, cancellationToken);

                // Update statistics
                if (!stats.ContainsKey(originalRoutingKey))
                {
                    stats[originalRoutingKey] = 0;
                }
                stats[originalRoutingKey]++;
                totalRecovered++;

                // Log progress every 1000 messages
                if (totalRecovered % 1000 == 0)
                {
                    _logger.LogInformation("📦 Recovered {Count} messages so far...", totalRecovered);
                }

                messageCount--;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to recover message from DLQ");
                break;
            }
        }

        // Log summary
        _logger.LogInformation("================================");
        _logger.LogInformation("📊 DLQ RECOVERY SUMMARY");
        _logger.LogInformation("================================");
        _logger.LogInformation("✅ Total Recovered: {TotalRecovered} messages", totalRecovered);
        _logger.LogInformation("");
        _logger.LogInformation("By Queue:");
        foreach (var kvp in stats.OrderByDescending(x => x.Value))
        {
            _logger.LogInformation("   {Queue}: {Count}", kvp.Key, kvp.Value);
        }
        _logger.LogInformation("================================");

        // Check remaining DLQ count
        var finalQueueInfo = await channel.QueueDeclarePassiveAsync(dlqName, cancellationToken);
        var remainingCount = finalQueueInfo.MessageCount;

        if (remainingCount > 0)
        {
            _logger.LogWarning("⚠️  {RemainingCount} messages still in DLQ", remainingCount);
        }
        else
        {
            _logger.LogInformation("✅ DLQ is now empty!");
        }
    }
}

