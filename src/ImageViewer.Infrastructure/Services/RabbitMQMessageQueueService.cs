using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ImageViewer.Domain.Events;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Infrastructure.Data;

namespace ImageViewer.Infrastructure.Services;

/// <summary>
/// RabbitMQ message queue service implementation
/// </summary>
public class RabbitMQMessageQueueService : IMessageQueueService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly RabbitMQOptions _options;
    private readonly ILogger<RabbitMQMessageQueueService> _logger;
    private bool _disposed = false;

    public RabbitMQMessageQueueService(IOptions<RabbitMQOptions> options, ILogger<RabbitMQMessageQueueService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            RequestedConnectionTimeout = _options.ConnectionTimeout,
            RequestedHeartbeat = TimeSpan.FromSeconds(60)
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        SetupExchangesAndQueues();
    }

    private void SetupExchangesAndQueues()
    {
        // Declare main exchange
        _channel.ExchangeDeclareAsync(_options.DefaultExchange, ExchangeType.Topic, true, false).GetAwaiter().GetResult();

        // Declare dead letter exchange
        _channel.ExchangeDeclareAsync(_options.DeadLetterExchange, ExchangeType.Topic, true, false).GetAwaiter().GetResult();

        // Declare queues with dead letter exchange
        var queueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", _options.DeadLetterExchange },
            { "x-message-ttl", (int)_options.MessageTimeout.TotalMilliseconds }
        };

        _channel.QueueDeclareAsync(_options.CollectionScanQueue, true, false, false, queueArgs).GetAwaiter().GetResult();
        _channel.QueueDeclareAsync(_options.ThumbnailGenerationQueue, true, false, false, queueArgs).GetAwaiter().GetResult();
        _channel.QueueDeclareAsync(_options.CacheGenerationQueue, true, false, false, queueArgs).GetAwaiter().GetResult();
        _channel.QueueDeclareAsync(_options.CollectionCreationQueue, true, false, false, queueArgs).GetAwaiter().GetResult();
        _channel.QueueDeclareAsync(_options.BulkOperationQueue, true, false, false, queueArgs).GetAwaiter().GetResult();
        _channel.QueueDeclareAsync(_options.ImageProcessingQueue, true, false, false, queueArgs).GetAwaiter().GetResult();

        // Bind queues to exchange
        _channel.QueueBindAsync(_options.CollectionScanQueue, _options.DefaultExchange, "collection.scan").GetAwaiter().GetResult();
        _channel.QueueBindAsync(_options.ThumbnailGenerationQueue, _options.DefaultExchange, "thumbnail.generation").GetAwaiter().GetResult();
        _channel.QueueBindAsync(_options.CacheGenerationQueue, _options.DefaultExchange, "cache.generation").GetAwaiter().GetResult();
        _channel.QueueBindAsync(_options.CollectionCreationQueue, _options.DefaultExchange, "collection.creation").GetAwaiter().GetResult();
        _channel.QueueBindAsync(_options.BulkOperationQueue, _options.DefaultExchange, "bulk.operation").GetAwaiter().GetResult();
        _channel.QueueBindAsync(_options.ImageProcessingQueue, _options.DefaultExchange, "image.processing").GetAwaiter().GetResult();

        _logger.LogInformation("RabbitMQ exchanges and queues configured successfully");
    }

    public async Task PublishAsync<T>(T message, string? routingKey = null, CancellationToken cancellationToken = default) where T : MessageEvent
    {
        try
        {
            var queueName = GetQueueName<T>();
            var messageBody = JsonSerializer.SerializeToUtf8Bytes(message);
            var properties = new BasicProperties();
            
            properties.Persistent = true;
            properties.MessageId = message.Id.ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.CorrelationId = message.CorrelationId?.ToString();
            properties.Headers = new Dictionary<string, object>
            {
                { "MessageType", message.MessageType },
                { "Timestamp", message.Timestamp.ToString("O") }
            };

            _channel.BasicPublishAsync(
                exchange: _options.DefaultExchange,
                routingKey: routingKey ?? GetDefaultRoutingKey<T>(),
                mandatory: false,
                basicProperties: properties,
                body: messageBody).GetAwaiter().GetResult();

            _logger.LogDebug("Published message {MessageType} with ID {MessageId}", message.MessageType, message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message {MessageType} with ID {MessageId}", message.MessageType, message.Id);
            throw;
        }
    }

    public async Task PublishDelayedAsync<T>(T message, TimeSpan delay, string? routingKey = null, CancellationToken cancellationToken = default) where T : MessageEvent
    {
        // For delayed messages, we can use RabbitMQ delayed message plugin or implement with TTL
        message.Properties["Delay"] = delay.TotalMilliseconds;
        await PublishAsync(message, routingKey, cancellationToken);
    }

    public async Task PublishWithPriorityAsync<T>(T message, int priority, string? routingKey = null, CancellationToken cancellationToken = default) where T : MessageEvent
    {
        message.Properties["Priority"] = priority;
        await PublishAsync(message, routingKey, cancellationToken);
    }

    private string GetQueueName<T>() where T : MessageEvent
    {
        return typeof(T).Name switch
        {
            nameof(CollectionScanMessage) => _options.CollectionScanQueue,
            nameof(ThumbnailGenerationMessage) => _options.ThumbnailGenerationQueue,
            nameof(CacheGenerationMessage) => _options.CacheGenerationQueue,
            nameof(CollectionCreationMessage) => _options.CollectionCreationQueue,
            nameof(BulkOperationMessage) => _options.BulkOperationQueue,
            nameof(ImageProcessingMessage) => _options.ImageProcessingQueue,
            _ => _options.ImageProcessingQueue
        };
    }

    private string GetDefaultRoutingKey<T>() where T : MessageEvent
    {
        return typeof(T).Name switch
        {
            nameof(CollectionScanMessage) => "collection.scan",
            nameof(ThumbnailGenerationMessage) => "thumbnail.generation",
            nameof(CacheGenerationMessage) => "cache.generation",
            nameof(CollectionCreationMessage) => "collection.creation",
            nameof(BulkOperationMessage) => "bulk.operation",
            nameof(ImageProcessingMessage) => "image.processing",
            _ => "image.processing"
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _channel?.CloseAsync().GetAwaiter().GetResult();
            _channel?.Dispose();
            _connection?.CloseAsync().GetAwaiter().GetResult();
            _connection?.Dispose();
            _disposed = true;
        }
    }
}
