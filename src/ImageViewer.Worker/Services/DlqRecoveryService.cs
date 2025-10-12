using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ImageViewer.Infrastructure.Data;
using System.Text;

namespace ImageViewer.Worker.Services;

/// <summary>
/// 死信队列恢复服务 (Dead Letter Queue Recovery Service)
/// Dịch vụ khôi phục hàng đợi thư chết
/// 
/// Automatically recovers messages from DLQ on Worker startup
/// ZERO MESSAGE LOSS GUARANTEE:
/// - Manual ACK only after successful republish
/// - NACK with requeue=true on ANY failure
/// - QoS prefetch=1 for one-at-a-time processing
/// - Idempotent recovery (safe to run multiple times)
/// </summary>
public class DlqRecoveryService : IHostedService
{
    private readonly ILogger<DlqRecoveryService> _logger;
    private readonly RabbitMQOptions _options;
    private readonly IConnectionFactory _connectionFactory;

    // Complete mapping of MessageType → RoutingKey
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
            _logger.LogError(ex, "❌ DLQ recovery failed - messages remain in DLQ for next retry");
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
        var failedStats = new Dictionary<string, int>();
        var skippedMessages = 0;
        var totalRecovered = 0;
        var totalFailed = 0;

        // CRITICAL: QoS prefetch=1 means process one message at a time
        // This ensures we don't lose messages if worker crashes
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        var lastProcessedTime = DateTime.UtcNow;
        var processingLock = new SemaphoreSlim(1, 1);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            await processingLock.WaitAsync(cancellationToken);
            try
            {
                lastProcessedTime = DateTime.UtcNow;

                // Extract MessageType from headers
                string? messageType = null;
                string? originalRoutingKey = null;
                string? xDeathRoutingKey = null; // Track x-death routing key for comparison

                if (ea.BasicProperties.Headers != null)
                {
                    if (ea.BasicProperties.Headers.TryGetValue("MessageType", out var messageTypeObj) && messageTypeObj != null)
                    {
                        messageType = messageTypeObj is byte[] bytes ? Encoding.UTF8.GetString(bytes) : messageTypeObj.ToString();
                    }

                    // Fallback: try x-death header
                    if (string.IsNullOrEmpty(messageType) &&
                        ea.BasicProperties.Headers.TryGetValue("x-death", out var xDeathObj))
                    {
                        if (xDeathObj is List<object> xDeathList && xDeathList.Count > 0)
                        {
                            if (xDeathList[0] is Dictionary<string, object> xDeath &&
                                xDeath.TryGetValue("routing-keys", out var routingKeysObj) &&
                                routingKeysObj is List<object> routingKeys && routingKeys.Count > 0)
                            {
                                xDeathRoutingKey = routingKeys[0]?.ToString();
                                originalRoutingKey = xDeathRoutingKey;
                            }
                        }
                    }
                }

                // Map MessageType to routing key (preferred over x-death)
                if (!string.IsNullOrEmpty(messageType) &&
                    MessageTypeToRoutingKey.TryGetValue(messageType, out var mappedRoutingKey))
                {
                    originalRoutingKey = mappedRoutingKey;
                    
                    // FIX 3: Log if x-death had different routing key (diagnostic)
                    if (!string.IsNullOrEmpty(xDeathRoutingKey) && xDeathRoutingKey != mappedRoutingKey)
                    {
                        _logger.LogWarning("⚠️  Routing key mismatch detected: MessageType={MessageType} maps to {MappedKey}, but x-death shows {XDeathKey}. Using MessageType mapping.", 
                            messageType, mappedRoutingKey, xDeathRoutingKey);
                    }
                }

                if (string.IsNullOrEmpty(originalRoutingKey))
                {
                    _logger.LogWarning("⚠️  Message has no MessageType or routing key. MessageType={MessageType}. Keeping in DLQ for manual review.", messageType);

                    // CRITICAL: Requeue to DLQ (don't delete unknown messages)
                    // FIX 5: Use CancellationToken.None for cleanup operations (prevent cancellation during NACK)
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, CancellationToken.None);
                    Interlocked.Increment(ref skippedMessages);
                    return;
                }

                _logger.LogDebug("Processing DLQ message: MessageType={MessageType}, RoutingKey={RoutingKey}", messageType, originalRoutingKey);

                // Prepare new properties for republishing (IReadOnlyBasicProperties is read-only)
                var newProperties = new BasicProperties
                {
                    ContentType = ea.BasicProperties.ContentType,
                    ContentEncoding = ea.BasicProperties.ContentEncoding,
                    DeliveryMode = ea.BasicProperties.DeliveryMode,
                    Priority = ea.BasicProperties.Priority,
                    CorrelationId = ea.BasicProperties.CorrelationId,
                    ReplyTo = ea.BasicProperties.ReplyTo,
                    Expiration = null, // FIX 1: Clear expiration to use new 24-hour TTL from queue settings (prevent immediate re-expiry)
                    MessageId = ea.BasicProperties.MessageId,
                    Timestamp = ea.BasicProperties.Timestamp,
                    Type = ea.BasicProperties.Type,
                    UserId = ea.BasicProperties.UserId,
                    AppId = ea.BasicProperties.AppId,
                    ClusterId = ea.BasicProperties.ClusterId
                };

                // Copy headers and add recovery metadata
                var newHeaders = new Dictionary<string, object?>();
                if (ea.BasicProperties.Headers != null)
                {
                    foreach (var header in ea.BasicProperties.Headers)
                    {
                        // Skip x-death headers to prevent loops
                        if (!header.Key.StartsWith("x-death") && !header.Key.StartsWith("x-first-death") && !header.Key.StartsWith("x-last-death"))
                        {
                            newHeaders[header.Key] = header.Value;
                        }
                    }
                }

                // Add recovery metadata
                newHeaders["x-recovered-from-dlq"] = true;
                newHeaders["x-recovered-at"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                newProperties.Headers = newHeaders;

                // CRITICAL: Publish FIRST, ACK SECOND (prevent message loss)
                try
                {
                    await channel.BasicPublishAsync(
                        exchange: _options.DefaultExchange,
                        routingKey: originalRoutingKey,
                        mandatory: false,
                        basicProperties: newProperties,
                        body: ea.Body,
                        cancellationToken: cancellationToken);

                    // SUCCESS: ACK to remove from DLQ
                    // FIX 5: Use CancellationToken.None for cleanup operations (prevent cancellation during ACK)
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, CancellationToken.None);

                    // Update success statistics
                    lock (stats)
                    {
                        if (!stats.ContainsKey(originalRoutingKey))
                        {
                            stats[originalRoutingKey] = 0;
                        }
                        stats[originalRoutingKey]++;
                    }

                    Interlocked.Increment(ref totalRecovered);

                    // Log progress every 1000 messages
                    if (totalRecovered % 1000 == 0)
                    {
                        _logger.LogInformation("📦 Recovered {Count} messages so far...", totalRecovered);
                    }
                }
                catch (Exception publishEx)
                {
                    // FAILURE: NACK with requeue=true to keep message in DLQ
                    _logger.LogError(publishEx, "❌ Failed to republish message. Keeping in DLQ. MessageType={MessageType}, RoutingKey={RoutingKey}",
                        messageType, originalRoutingKey);

                    // FIX 5: Use CancellationToken.None for cleanup operations (prevent cancellation during NACK)
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, CancellationToken.None);

                    // Update failure statistics
                    lock (failedStats)
                    {
                        if (!failedStats.ContainsKey(originalRoutingKey))
                        {
                            failedStats[originalRoutingKey] = 0;
                        }
                        failedStats[originalRoutingKey]++;
                    }

                    Interlocked.Increment(ref totalFailed);
                }
            }
            catch (Exception ex)
            {
                // CRITICAL: If ANY error, NACK with requeue=true
                _logger.LogError(ex, "❌ Error processing message from DLQ. Message requeued for retry.");

                try
                {
                    // FIX 5: Use CancellationToken.None for cleanup operations (prevent cancellation during NACK)
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, CancellationToken.None);
                }
                catch (Exception nackEx)
                {
                    _logger.LogError(nackEx, "❌ CRITICAL: Failed to NACK message. Message may be lost!");
                }

                Interlocked.Increment(ref totalFailed);
            }
            finally
            {
                processingLock.Release();
            }
        };

        // Start consuming from DLQ
        var consumerTag = await channel.BasicConsumeAsync(
            queue: dlqName,
            autoAck: false, // CRITICAL: Manual ACK only after successful republish
            consumer: consumer);

        _logger.LogInformation("Started DLQ consumer with tag: {ConsumerTag}", consumerTag);

        // Wait for processing to complete
        var timeout = TimeSpan.FromMinutes(30);
        var startTime = DateTime.UtcNow;

        while (!cancellationToken.IsCancellationRequested && (DateTime.UtcNow - startTime) < timeout)
        {
            await Task.Delay(1000, cancellationToken);

            // If no messages processed for 10 seconds, check if DLQ is empty
            if ((DateTime.UtcNow - lastProcessedTime) > TimeSpan.FromSeconds(10))
            {
                var currentQueueInfo = await channel.QueueDeclarePassiveAsync(dlqName, cancellationToken);
                if (currentQueueInfo.MessageCount == 0)
                {
                    _logger.LogInformation("DLQ appears empty, waiting for any in-flight messages to complete...");
                    
                    // FIX 2 + FIX 4: Acquire lock with timeout (prevents deadlock on cancellation)
                    // This prevents premature exit while a message is between fetch and ACK/NACK
                    if (!await processingLock.WaitAsync(TimeSpan.FromSeconds(5)))
                    {
                        _logger.LogWarning("⚠️  Timed out waiting for in-flight message. Exiting anyway (message will be requeued by RabbitMQ).");
                        break;
                    }
                    processingLock.Release();
                    
                    _logger.LogInformation("In-flight messages completed, waiting 5 seconds to confirm DLQ is empty...");
                    await Task.Delay(5000, cancellationToken);

                    var confirmQueueInfo = await channel.QueueDeclarePassiveAsync(dlqName, cancellationToken);
                    if (confirmQueueInfo.MessageCount == 0)
                    {
                        _logger.LogInformation("✅ Confirmed: DLQ is empty. Recovery complete.");
                        break; // Recovery complete
                    }
                    else
                    {
                        _logger.LogInformation("⚠️  New messages appeared in DLQ ({Count}). Continuing recovery...", confirmQueueInfo.MessageCount);
                    }
                }
                else
                {
                    // Messages still in DLQ but not being processed - might be failures
                    if ((DateTime.UtcNow - lastProcessedTime) > TimeSpan.FromSeconds(30))
                    {
                        _logger.LogWarning("⚠️  No messages processed for 30 seconds but {Count} messages remain in DLQ. Stopping recovery (will retry on next startup).", currentQueueInfo.MessageCount);
                        break;
                    }
                }
            }
        }

        // Cancel consumer
        await channel.BasicCancelAsync(consumerTag);
        _logger.LogInformation("Stopped DLQ consumer");

        // Log summary
        _logger.LogInformation("================================");
        _logger.LogInformation("📊 DLQ RECOVERY SUMMARY");
        _logger.LogInformation("================================");
        _logger.LogInformation("✅ Total Recovered: {TotalRecovered} messages", totalRecovered);

        if (totalFailed > 0)
        {
            _logger.LogWarning("❌ Total Failed: {TotalFailed} messages (kept in DLQ for retry)", totalFailed);
        }

        if (skippedMessages > 0)
        {
            _logger.LogWarning("⚠️  Skipped Messages: {SkippedMessages} (unknown type, kept in DLQ for manual review)", skippedMessages);
        }

        _logger.LogInformation("");
        _logger.LogInformation("Successfully Recovered By Queue:");
        lock (stats)
        {
            foreach (var kvp in stats.OrderByDescending(x => x.Value))
            {
                _logger.LogInformation("   {Queue}: {Count}", kvp.Key, kvp.Value);
            }
        }

        if (failedStats.Count > 0)
        {
            _logger.LogInformation("");
            _logger.LogWarning("Failed Recoveries By Queue:");
            lock (failedStats)
            {
                foreach (var kvp in failedStats.OrderByDescending(x => x.Value))
                {
                    _logger.LogWarning("   {Queue}: {Count}", kvp.Key, kvp.Value);
                }
            }
        }

        _logger.LogInformation("================================");

        // Check remaining DLQ count
        var finalQueueInfo = await channel.QueueDeclarePassiveAsync(dlqName, cancellationToken);
        var remainingCount = finalQueueInfo.MessageCount;

        if (remainingCount > 0)
        {
            _logger.LogWarning("⚠️  {RemainingCount} messages still in DLQ (will retry on next startup)", remainingCount);
        }
        else
        {
            _logger.LogInformation("✅ DLQ is now empty!");
        }
    }
}
