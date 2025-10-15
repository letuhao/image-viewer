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
            _logger.LogWarning("⚠️ Continuing with application startup despite RabbitMQ setup failure - some features may not work properly");
            // Don't throw - let the application continue so batch consumers can start
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

            // Try to declare queue with new settings, but handle conflicts gracefully
            try
            {
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
            catch (RabbitMQ.Client.Exceptions.OperationInterruptedException ex) when (ex.Message.Contains("PRECONDITION_FAILED"))
            {
                _logger.LogWarning("Queue {QueueName} already exists with different settings. Skipping declaration. Error: {Error}", 
                    queueName, ex.Message);
                
                // Queue exists with different settings - this is okay, we'll use the existing queue
                // The existing queue will work fine, just with different limits
                continue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to declare queue: {QueueName}", queueName);
                _logger.LogWarning("⚠️ Continuing with other queues despite failure to declare {QueueName}", queueName);
                // Don't throw - continue with other queues
            }
        }

        // Declare dead letter queue
        try
        {
            await channel.QueueDeclareAsync(
                queue: "imageviewer.dlq",
                durable: true,
                exclusive: false,
                autoDelete: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Failed to declare DLQ, continuing without it");
        }

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
        
        if (!string.IsNullOrEmpty(_options.LibraryScanQueue))
            queues.Add(_options.LibraryScanQueue);

        return queues;
    }

    /// <summary>
    /// Bind queues to exchanges
    /// </summary>
    private async Task BindQueuesAsync(IChannel channel)
    {
        _logger.LogDebug("Binding queues to exchanges...");

        // Bind main queues to default exchange
        // Use exact match routing keys - no wildcards needed for simplicity
        var queueBindings = new Dictionary<string, string>
        {
            { _options.CollectionScanQueue, "collection.scan" },
            { _options.ThumbnailGenerationQueue, "thumbnail.generation" },
            { _options.CacheGenerationQueue, "cache.generation" },
            { _options.CollectionCreationQueue, "collection.creation" },
            { _options.BulkOperationQueue, "bulk.operation" },
            { _options.ImageProcessingQueue, "image.processing" },
            { _options.LibraryScanQueue, "library_scan_queue" } // Use queue name as routing key for consistency
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

    /// <summary>
    /// Delete all existing queues (use with caution - will lose all messages!)
    /// Only use this if you need to reset queue configurations
    /// </summary>
    public async Task DeleteAllQueuesAsync()
    {
        try
        {
            _logger.LogWarning("⚠️ DELETING ALL EXISTING QUEUES - THIS WILL LOSE ALL MESSAGES!");
            
            var channel = await _connection.CreateChannelAsync();
            var queues = GetConfiguredQueues();

            foreach (var queueName in queues)
            {
                if (string.IsNullOrEmpty(queueName))
                    continue;

                try
                {
                    await channel.QueueDeleteAsync(queueName);
                    _logger.LogWarning("Deleted queue: {QueueName}", queueName);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Could not delete queue {QueueName}: {Error}", queueName, ex.Message);
                }
            }

            // Delete dead letter queue too
            try
            {
                await channel.QueueDeleteAsync("imageviewer.dlq");
                _logger.LogWarning("Deleted dead letter queue");
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Could not delete dead letter queue: {Error}", ex.Message);
            }

            _logger.LogWarning("All queues deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting queues");
            throw;
        }
    }
}
