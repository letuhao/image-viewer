using ImageViewer.Domain.Events;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Message queue service interface for RabbitMQ
/// </summary>
public interface IMessageQueueService
{
    /// <summary>
    /// Publish a message to the queue
    /// </summary>
    Task PublishAsync<T>(T message, string? routingKey = null, CancellationToken cancellationToken = default) where T : MessageEvent;

    /// <summary>
    /// Publish a message with delay
    /// </summary>
    Task PublishDelayedAsync<T>(T message, TimeSpan delay, string? routingKey = null, CancellationToken cancellationToken = default) where T : MessageEvent;

    /// <summary>
    /// Publish a message with priority
    /// </summary>
    Task PublishWithPriorityAsync<T>(T message, int priority, string? routingKey = null, CancellationToken cancellationToken = default) where T : MessageEvent;

    /// <summary>
    /// Publish multiple messages in a batch (optimized for performance)
    /// 批量发布消息 - Xuất bản hàng loạt tin nhắn
    /// </summary>
    Task PublishBatchAsync<T>(IEnumerable<T> messages, string? routingKey = null, CancellationToken cancellationToken = default) where T : MessageEvent;
}

/// <summary>
/// Message consumer interface
/// </summary>
public interface IMessageConsumer
{
    /// <summary>
    /// Start consuming messages
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop consuming messages
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Collection scan message consumer
/// </summary>
public interface ICollectionScanConsumer : IMessageConsumer
{
}

/// <summary>
/// Thumbnail generation message consumer
/// </summary>
public interface IThumbnailGenerationConsumer : IMessageConsumer
{
}

/// <summary>
/// Cache generation message consumer
/// </summary>
public interface ICacheGenerationConsumer : IMessageConsumer
{
}

/// <summary>
/// Collection creation message consumer
/// </summary>
public interface ICollectionCreationConsumer : IMessageConsumer
{
}

/// <summary>
/// Bulk operation message consumer
/// </summary>
public interface IBulkOperationConsumer : IMessageConsumer
{
}

/// <summary>
/// Image processing message consumer
/// </summary>
public interface IImageProcessingConsumer : IMessageConsumer
{
}
