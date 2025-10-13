using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ImageViewer.Infrastructure.Data;
using System.Collections.Concurrent;

namespace ImageViewer.Infrastructure.Services;

/// <summary>
/// Disk-aware message batching service
/// Optimizes batching based on disk I/O patterns and queue health
/// </summary>
public interface IDiskAwareMessageBatcher
{
    /// <summary>
    /// Add message to batch queue with disk I/O awareness
    /// </summary>
    Task<bool> AddToBatchAsync<T>(T message, string queueName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get current queue health metrics
    /// </summary>
    Task<QueueHealthMetrics> GetQueueHealthAsync(string queueName);
    
    /// <summary>
    /// Get optimal batch size based on current system load
    /// </summary>
    int GetOptimalBatchSize(string queueName);
}

public class DiskAwareMessageBatcher : IDiskAwareMessageBatcher
{
    private readonly ILogger<DiskAwareMessageBatcher> _logger;
    private readonly RabbitMQOptions _options;
    private readonly ConcurrentDictionary<string, MessageBatchQueue> _batchQueues = new();
    private readonly Timer _batchFlushTimer;
    private readonly object _healthMetricsLock = new();
    private readonly Dictionary<string, QueueHealthMetrics> _healthMetrics = new();

    public DiskAwareMessageBatcher(
        ILogger<DiskAwareMessageBatcher> logger,
        IOptions<RabbitMQOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        
        // Flush batches every 2 seconds to balance throughput vs latency
        _batchFlushTimer = new Timer(FlushAllBatches, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
    }

    public async Task<bool> AddToBatchAsync<T>(T message, string queueName, CancellationToken cancellationToken = default)
    {
        try
        {
            var batchQueue = _batchQueues.GetOrAdd(queueName, name => new MessageBatchQueue(name, _logger));
            
            var added = batchQueue.TryAdd(message);
            if (!added)
            {
                _logger.LogWarning("⚠️ Batch queue {QueueName} is full, dropping message", queueName);
                return false;
            }

            // Check if batch is ready to flush
            var optimalBatchSize = GetOptimalBatchSize(queueName);
            if (batchQueue.Count >= optimalBatchSize)
            {
                await FlushBatchAsync(queueName, cancellationToken);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to add message to batch queue {QueueName}", queueName);
            return false;
        }
    }

    public async Task<QueueHealthMetrics> GetQueueHealthAsync(string queueName)
    {
        lock (_healthMetricsLock)
        {
            if (_healthMetrics.TryGetValue(queueName, out var metrics))
            {
                return metrics;
            }
        }

        // Return default healthy metrics
        return new QueueHealthMetrics
        {
            QueueName = queueName,
            IsHealthy = true,
            RecommendedBatchSize = _options.MessageBatchSize,
            LastUpdated = DateTime.UtcNow
        };
    }

    public int GetOptimalBatchSize(string queueName)
    {
        var healthMetrics = GetQueueHealthAsync(queueName).GetAwaiter().GetResult();
        
        if (!healthMetrics.IsHealthy)
        {
            // Reduce batch size if queue is unhealthy
            return Math.Max(10, _options.MessageBatchSize / 2);
        }

        // Base batch size on queue type and health
        return queueName switch
        {
            "thumbnail.generation" => Math.Min(100, _options.MessageBatchSize), // Smaller batches for I/O intensive
            "cache.generation" => Math.Min(50, _options.MessageBatchSize),      // Even smaller for cache generation
            "image.processing" => _options.MessageBatchSize,                    // Standard size for metadata processing
            _ => _options.MessageBatchSize
        };
    }

    private async void FlushAllBatches(object? state)
    {
        try
        {
            var tasks = _batchQueues.Keys.Select(queueName => FlushBatchAsync(queueName, CancellationToken.None));
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error flushing message batches");
        }
    }

    private async Task FlushBatchAsync(string queueName, CancellationToken cancellationToken)
    {
        try
        {
            if (!_batchQueues.TryGetValue(queueName, out var batchQueue))
                return;

            var messages = batchQueue.TakeAll();
            if (messages.Count == 0)
                return;

            _logger.LogDebug("📦 Flushing batch of {Count} messages to queue {QueueName}", 
                messages.Count, queueName);

            // Here you would publish the batch to RabbitMQ
            // This is a placeholder - implement actual publishing logic
            await PublishBatchAsync(queueName, messages, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to flush batch for queue {QueueName}", queueName);
        }
    }

    private async Task PublishBatchAsync(string queueName, List<object> messages, CancellationToken cancellationToken)
    {
        // TODO: Implement actual batch publishing to RabbitMQ
        // This would use the IMessageQueueService to publish the batch
        await Task.Delay(1, cancellationToken); // Placeholder
    }

    public void Dispose()
    {
        _batchFlushTimer?.Dispose();
    }
}

/// <summary>
/// Thread-safe message batch queue
/// </summary>
public class MessageBatchQueue
{
    private readonly ConcurrentQueue<object> _queue = new();
    private readonly string _queueName;
    private readonly ILogger _logger;
    private readonly int _maxSize;

    public MessageBatchQueue(string queueName, ILogger logger, int maxSize = 1000)
    {
        _queueName = queueName;
        _logger = logger;
        _maxSize = maxSize;
    }

    public bool TryAdd(object message)
    {
        if (_queue.Count >= _maxSize)
            return false;

        _queue.Enqueue(message);
        return true;
    }

    public List<object> TakeAll()
    {
        var messages = new List<object>();
        while (_queue.TryDequeue(out var message))
        {
            messages.Add(message);
        }
        return messages;
    }

    public int Count => _queue.Count;
    public string QueueName => _queueName;
}

/// <summary>
/// Queue health metrics for adaptive batching
/// </summary>
public class QueueHealthMetrics
{
    public string QueueName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; } = true;
    public int RecommendedBatchSize { get; set; } = 100;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public long QueueDepth { get; set; } = 0;
    public int ConsumerCount { get; set; } = 0;
}
