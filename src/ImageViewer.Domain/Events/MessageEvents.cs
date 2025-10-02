using ImageViewer.Domain.Enums;

namespace ImageViewer.Domain.Events;

/// <summary>
/// Base message event for RabbitMQ
/// </summary>
public abstract class MessageEvent : IDomainEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string MessageType { get; set; } = string.Empty;
    public Guid? CorrelationId { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Collection scan message event
/// </summary>
public class CollectionScanMessage : MessageEvent
{
    public Guid CollectionId { get; set; }
    public string CollectionPath { get; set; } = string.Empty;
    public CollectionType CollectionType { get; set; }
    public bool ForceRescan { get; set; } = false;
    public string? UserId { get; set; }

    public CollectionScanMessage()
    {
        MessageType = "CollectionScan";
    }
}

/// <summary>
/// Thumbnail generation message event
/// </summary>
public class ThumbnailGenerationMessage : MessageEvent
{
    public Guid ImageId { get; set; }
    public Guid CollectionId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string ImageFilename { get; set; } = string.Empty;
    public int ThumbnailWidth { get; set; }
    public int ThumbnailHeight { get; set; }
    public string? UserId { get; set; }

    public ThumbnailGenerationMessage()
    {
        MessageType = "ThumbnailGeneration";
    }
}

/// <summary>
/// Cache generation message event
/// </summary>
public class CacheGenerationMessage : MessageEvent
{
    public Guid ImageId { get; set; }
    public Guid CollectionId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string ImageFilename { get; set; } = string.Empty;
    public int CacheWidth { get; set; }
    public int CacheHeight { get; set; }
    public string? UserId { get; set; }

    public CacheGenerationMessage()
    {
        MessageType = "CacheGeneration";
    }
}

/// <summary>
/// Collection creation message event
/// </summary>
public class CollectionCreationMessage : MessageEvent
{
    public string CollectionName { get; set; } = string.Empty;
    public string CollectionPath { get; set; } = string.Empty;
    public CollectionType CollectionType { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();
    public string? UserId { get; set; }

    public CollectionCreationMessage()
    {
        MessageType = "CollectionCreation";
    }
}

/// <summary>
/// Bulk operation message event
/// </summary>
public class BulkOperationMessage : MessageEvent
{
    public string OperationType { get; set; } = string.Empty; // "ScanAll", "GenerateAllThumbnails", "GenerateAllCache"
    public List<Guid> CollectionIds { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? UserId { get; set; }

    public BulkOperationMessage()
    {
        MessageType = "BulkOperation";
    }
}

/// <summary>
/// Image processing message event
/// </summary>
public class ImageProcessingMessage : MessageEvent
{
    public Guid ImageId { get; set; }
    public Guid CollectionId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string ImageFilename { get; set; } = string.Empty;
    public string ProcessingType { get; set; } = string.Empty; // "Thumbnail", "Cache", "Metadata"
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? UserId { get; set; }

    public ImageProcessingMessage()
    {
        MessageType = "ImageProcessing";
    }
}
