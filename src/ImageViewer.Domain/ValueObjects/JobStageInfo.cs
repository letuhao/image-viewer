using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Represents a stage in a multi-stage background job
/// </summary>
public class JobStageInfo
{
    [BsonElement("stageName")]
    public string StageName { get; private set; }
    
    [BsonElement("status")]
    public string Status { get; private set; } // Pending, InProgress, Completed, Failed
    
    [BsonElement("progress")]
    public int Progress { get; private set; } // 0-100
    
    [BsonElement("totalItems")]
    public int TotalItems { get; private set; }
    
    [BsonElement("completedItems")]
    public int CompletedItems { get; private set; }
    
    [BsonElement("message")]
    public string? Message { get; private set; }
    
    [BsonElement("startedAt")]
    public DateTime? StartedAt { get; private set; }
    
    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; private set; }
    
    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; private set; }

    // Private constructor for MongoDB
    private JobStageInfo() { }

    public JobStageInfo(string stageName)
    {
        StageName = stageName ?? throw new ArgumentNullException(nameof(stageName));
        Status = "Pending";
        Progress = 0;
        TotalItems = 0;
        CompletedItems = 0;
    }

    public void Start(int totalItems = 0, string? message = null)
    {
        Status = "InProgress";
        StartedAt = DateTime.UtcNow;
        TotalItems = totalItems;
        if (message != null) Message = message;
    }

    public void UpdateProgress(int completed, int total, string? message = null)
    {
        CompletedItems = completed;
        TotalItems = total;
        Progress = total > 0 ? (int)((double)completed / total * 100) : 0;
        if (message != null) Message = message;
    }

    public void Complete(string? message = null)
    {
        Status = "Completed";
        CompletedAt = DateTime.UtcNow;
        Progress = 100;
        if (message != null) Message = message;
    }

    public void Fail(string errorMessage)
    {
        Status = "Failed";
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }
}

