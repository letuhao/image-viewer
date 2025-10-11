using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

#pragma warning disable CS8618 // MongoDB entities are initialized by the driver

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Scheduled job run history - tracks execution of scheduled jobs
/// 定时任务运行历史 - Lịch sử chạy công việc định kỳ
/// </summary>
public class ScheduledJobRun : BaseEntity
{
    [BsonElement("scheduledJobId")]
    public ObjectId ScheduledJobId { get; private set; }
    
    [BsonElement("scheduledJobName")]
    public string ScheduledJobName { get; private set; }
    
    [BsonElement("jobType")]
    public string JobType { get; private set; }
    
    [BsonElement("status")]
    public string Status { get; private set; } // Running, Completed, Failed, Timeout
    
    [BsonElement("startedAt")]
    public DateTime StartedAt { get; private set; }
    
    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; private set; }
    
    [BsonElement("duration")]
    public TimeSpan? Duration { get; private set; }
    
    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; private set; }
    
    [BsonElement("result")]
    public Dictionary<string, object>? Result { get; private set; }
    
    [BsonElement("triggeredBy")]
    public string TriggeredBy { get; private set; } // "Scheduler", "Manual", "API"
    
    [BsonElement("hangfireJobId")]
    public string? HangfireJobId { get; private set; }

    // Private constructor for MongoDB
    private ScheduledJobRun() { }

    public ScheduledJobRun(
        ObjectId scheduledJobId,
        string scheduledJobName,
        string jobType,
        string triggeredBy = "Scheduler")
    {
        ScheduledJobId = scheduledJobId;
        ScheduledJobName = scheduledJobName ?? throw new ArgumentNullException(nameof(scheduledJobName));
        JobType = jobType ?? throw new ArgumentNullException(nameof(jobType));
        TriggeredBy = triggeredBy ?? "Scheduler";
        Status = "Running";
        StartedAt = DateTime.UtcNow;
    }

    public void Complete(Dictionary<string, object>? result = null)
    {
        Status = "Completed";
        CompletedAt = DateTime.UtcNow;
        Duration = CompletedAt.Value - StartedAt;
        Result = result;
        UpdateTimestamp();
    }

    public void Fail(string errorMessage)
    {
        Status = "Failed";
        CompletedAt = DateTime.UtcNow;
        Duration = CompletedAt.Value - StartedAt;
        ErrorMessage = errorMessage;
        UpdateTimestamp();
    }

    public void Timeout()
    {
        Status = "Timeout";
        CompletedAt = DateTime.UtcNow;
        Duration = CompletedAt.Value - StartedAt;
        ErrorMessage = "Job execution exceeded timeout limit";
        UpdateTimestamp();
    }

    public void SetHangfireJobId(string hangfireJobId)
    {
        HangfireJobId = hangfireJobId;
        UpdateTimestamp();
    }
}

