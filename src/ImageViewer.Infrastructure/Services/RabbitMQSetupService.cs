using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ImageViewer.Infrastructure.Data;

namespace ImageViewer.Infrastructure.Services;

/// <summary>
/// Service for setting up RabbitMQ queues and exchanges
/// </summary>
public class RabbitMQSetupService
{
    private readonly IConnection _connection;
    private readonly RabbitMQOptions _options;
    private readonly ILogger<RabbitMQSetupService> _logger;

    public RabbitMQSetupService(
        IConnection connection,
        IOptions<RabbitMQOptions> options,
        ILogger<RabbitMQSetupService> logger)
    {
        _connection = connection;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Set up all required queues and exchanges
    /// </summary>
    public async Task SetupQueuesAndExchangesAsync()
    {
        try
        {
            _logger.LogInformation("Setting up RabbitMQ queues and exchanges...");

            var channel = await _connection.CreateChannelAsync();

            // Declare exchanges
            await DeclareExchangesAsync(channel);

            // Declare queues
            await DeclareQueuesAsync(channel);

            // Bind queues to exchanges
            await BindQueuesAsync(channel);

            _logger.LogInformation("Successfully set up RabbitMQ queues and exchanges");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set up RabbitMQ queues and exchanges");
            throw;
        }
    }

    /// <summary>
    /// Declare all required exchanges
    /// </summary>
    private async Task DeclareExchangesAsync(IChannel channel)
    {
        _logger.LogDebug("Declaring exchanges...");

        // Main exchange
        await channel.ExchangeDeclareAsync(
            exchange: _options.DefaultExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        // Dead letter exchange
        await channel.ExchangeDeclareAsync(
            exchange: _options.DeadLetterExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        _logger.LogDebug("Exchanges declared successfully");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Declare all required queues
    /// </summary>
    private async Task DeclareQueuesAsync(IChannel channel)
    {
        _logger.LogDebug("Declaring queues...");

        // Get all queue names from configuration
        var queues = GetConfiguredQueues();

        foreach (var queueName in queues)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                _logger.LogWarning("Skipping empty queue name");
                continue;
            }

            var arguments = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", _options.DeadLetterExchange },
                { "x-message-ttl", (int)_options.MessageTimeout.TotalMilliseconds },
                { "x-max-length", _options.MaxQueueLength }, // Limit queue to prevent unbounded growth
                { "x-overflow", "reject-publish" } // Reject new messages when queue is full
            };

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: arguments);

            _logger.LogDebug("Declared queue: {QueueName}", queueName);
        }

        // Declare dead letter queue
        await channel.QueueDeclareAsync(
            queue: "imageviewer.dlq",
            durable: true,
            exclusive: false,
            autoDelete: false);

        _logger.LogDebug("Queues declared successfully");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Get all configured queue names from options
    /// </summary>
    private IEnumerable<string> GetConfiguredQueues()
    {
        var queues = new List<string>();

        if (!string.IsNullOrEmpty(_options.CollectionScanQueue))
            queues.Add(_options.CollectionScanQueue);
        
        if (!string.IsNullOrEmpty(_options.ThumbnailGenerationQueue))
            queues.Add(_options.ThumbnailGenerationQueue);
        
        if (!string.IsNullOrEmpty(_options.CacheGenerationQueue))
            queues.Add(_options.CacheGenerationQueue);
        
        if (!string.IsNullOrEmpty(_options.CollectionCreationQueue))
            queues.Add(_options.CollectionCreationQueue);
        
        if (!string.IsNullOrEmpty(_options.BulkOperationQueue))
            queues.Add(_options.BulkOperationQueue);
        
        if (!string.IsNullOrEmpty(_options.ImageProcessingQueue))
            queues.Add(_options.ImageProcessingQueue);

        return queues;
    }

    /// <summary>
    /// Bind queues to exchanges
    /// </summary>
    private async Task BindQueuesAsync(IChannel channel)
    {
        _logger.LogDebug("Binding queues to exchanges...");

        // Bind main queues to default exchange
        var queueBindings = new Dictionary<string, string>
        {
            { _options.CollectionScanQueue, "collection.scan.*" },
            { _options.ThumbnailGenerationQueue, "thumbnail.generation.*" },
            { _options.CacheGenerationQueue, "cache.generation.*" },
            { _options.CollectionCreationQueue, "collection.creation.*" },
            { _options.BulkOperationQueue, "bulk.operation.*" },
            { _options.ImageProcessingQueue, "image.processing.*" }
        };

        foreach (var binding in queueBindings)
        {
            await channel.QueueBindAsync(
                queue: binding.Key,
                exchange: _options.DefaultExchange,
                routingKey: binding.Value);

            _logger.LogDebug("Bound queue {QueueName} to exchange {ExchangeName} with routing key {RoutingKey}",
                binding.Key, _options.DefaultExchange, binding.Value);
        }

        // Bind dead letter queue
        await channel.QueueBindAsync(
            queue: "imageviewer.dlq",
            exchange: _options.DeadLetterExchange,
            routingKey: "#");

        _logger.LogDebug("Queue bindings completed successfully");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Check if queues exist
    /// </summary>
    public async Task<bool> CheckQueuesExistAsync()
    {
        try
        {
            var channel = await _connection.CreateChannelAsync();

            var queues = GetConfiguredQueues();

            foreach (var queueName in queues)
            {
                if (string.IsNullOrEmpty(queueName))
                    continue;

                try
                {
                    await channel.QueueDeclarePassiveAsync(queueName);
                }
                catch (Exception)
                {
                    _logger.LogDebug("Queue {QueueName} does not exist", queueName);
                    return false;
                }
            }

            _logger.LogDebug("All required queues exist");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking queue existence");
            return false;
        }
    }
}
