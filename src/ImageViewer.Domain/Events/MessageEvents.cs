using ImageViewer.Domain.Enums;
using MongoDB.Bson;

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
/// Thumbnail generation message event
/// </summary>
public class ThumbnailGenerationMessage : MessageEvent
{
    public string ImageId { get; set; } = string.Empty; // Changed from Guid to string for JSON serialization
    public string CollectionId { get; set; } = string.Empty; // Changed from Guid to string for JSON serialization
    public string ImagePath { get; set; } = string.Empty;
    public string ImageFilename { get; set; } = string.Empty;
    public int ThumbnailWidth { get; set; }
    public int ThumbnailHeight { get; set; }
    public string? UserId { get; set; }
    public string? JobId { get; set; } // Link to background job for tracking
    public string? ScanJobId { get; set; } // Link to parent scan job for multi-stage tracking

    public ThumbnailGenerationMessage()
    {
        MessageType = "ThumbnailGeneration";
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
/// Enhanced bulk operation message event with job tracking
/// </summary>
public class BulkOperationMessage : MessageEvent
{
    public string OperationType { get; set; } = string.Empty; // "BulkAddCollections", "ScanAll", "GenerateAllThumbnails", "GenerateAllCache"
    public List<Guid> CollectionIds { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? UserId { get; set; }
    public string JobId { get; set; } = string.Empty; // Link to background job for tracking (as string for JSON serialization)
    public string? ParentJobId { get; set; } // For job hierarchy (as string for JSON serialization)
    public List<string> ChildJobIds { get; set; } = new(); // For tracking child jobs (as strings for JSON serialization)
    public int Priority { get; set; } = 0; // Job priority (higher = more important)
    public DateTime? ScheduledFor { get; set; } // For delayed execution
    public int MaxRetries { get; set; } = 3;
    public int RetryCount { get; set; } = 0;
    public TimeSpan? Timeout { get; set; } // Job timeout
    public Dictionary<string, object> Metadata { get; set; } = new(); // Additional tracking data

    public BulkOperationMessage()
    {
        MessageType = "BulkOperation";
    }
}

