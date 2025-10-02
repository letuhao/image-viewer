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
    private readonly IModel _channel;
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

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        SetupExchangesAndQueues();
    }

    private void SetupExchangesAndQueues()
    {
        // Declare main exchange
        _channel.ExchangeDeclare(_options.DefaultExchange, ExchangeType.Topic, true, false);

        // Declare dead letter exchange
        _channel.ExchangeDeclare(_options.DeadLetterExchange, ExchangeType.Topic, true, false);

        // Declare queues with dead letter exchange
        var queueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", _options.DeadLetterExchange },
            { "x-message-ttl", (int)_options.MessageTimeout.TotalMilliseconds }
        };

        _channel.QueueDeclare(_options.CollectionScanQueue, true, false, false, queueArgs);
        _channel.QueueDeclare(_options.ThumbnailGenerationQueue, true, false, false, queueArgs);
        _channel.QueueDeclare(_options.CacheGenerationQueue, true, false, false, queueArgs);
        _channel.QueueDeclare(_options.CollectionCreationQueue, true, false, false, queueArgs);
        _channel.QueueDeclare(_options.BulkOperationQueue, true, false, false, queueArgs);
        _channel.QueueDeclare(_options.ImageProcessingQueue, true, false, false, queueArgs);

        // Bind queues to exchange
        _channel.QueueBind(_options.CollectionScanQueue, _options.DefaultExchange, "collection.scan");
        _channel.QueueBind(_options.ThumbnailGenerationQueue, _options.DefaultExchange, "thumbnail.generation");
        _channel.QueueBind(_options.CacheGenerationQueue, _options.DefaultExchange, "cache.generation");
        _channel.QueueBind(_options.CollectionCreationQueue, _options.DefaultExchange, "collection.creation");
        _channel.QueueBind(_options.BulkOperationQueue, _options.DefaultExchange, "bulk.operation");
        _channel.QueueBind(_options.ImageProcessingQueue, _options.DefaultExchange, "image.processing");

        _logger.LogInformation("RabbitMQ exchanges and queues configured successfully");
    }

    public async Task PublishAsync<T>(T message, string? routingKey = null, CancellationToken cancellationToken = default) where T : MessageEvent
    {
        try
        {
            var queueName = GetQueueName<T>();
            var messageBody = JsonSerializer.SerializeToUtf8Bytes(message);
            var properties = _channel.CreateBasicProperties();
            
            properties.Persistent = true;
            properties.MessageId = message.Id.ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.CorrelationId = message.CorrelationId?.ToString();
            properties.Headers = new Dictionary<string, object>
            {
                { "MessageType", message.MessageType },
                { "Timestamp", message.Timestamp.ToString("O") }
            };

            _channel.BasicPublish(
                exchange: _options.DefaultExchange,
                routingKey: routingKey ?? GetDefaultRoutingKey<T>(),
                basicProperties: properties,
                body: messageBody);

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
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
            _disposed = true;
        }
    }
}
