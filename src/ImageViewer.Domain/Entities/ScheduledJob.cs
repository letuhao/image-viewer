using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ImageViewer.Domain.Enums;

#pragma warning disable CS8618 // MongoDB entities are initialized by the driver

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Scheduled job entity - represents a recurring or one-time scheduled task
/// 定时任务实体 - Thực thể công việc được lên lịch
/// </summary>
public class ScheduledJob : BaseEntity
{
    [BsonElement("name")]
    public string Name { get; private set; }
    
    [BsonElement("description")]
    public string? Description { get; private set; }
    
    [BsonElement("jobType")]
    public string JobType { get; private set; }
    
    [BsonElement("scheduleType")]
    public ScheduleType ScheduleType { get; private set; }
    
    [BsonElement("cronExpression")]
    public string? CronExpression { get; private set; }
    
    [BsonElement("intervalMinutes")]
    public int? IntervalMinutes { get; private set; }
    
    [BsonElement("isEnabled")]
    public bool IsEnabled { get; private set; }
    
    [BsonElement("parameters")]
    public Dictionary<string, object> Parameters { get; private set; } = new();
    
    [BsonElement("lastRunAt")]
    public DateTime? LastRunAt { get; private set; }
    
    [BsonElement("nextRunAt")]
    public DateTime? NextRunAt { get; private set; }
    
    [BsonElement("lastRunDuration")]
    public TimeSpan? LastRunDuration { get; private set; }
    
    [BsonElement("lastRunStatus")]
    public string? LastRunStatus { get; private set; }
    
    [BsonElement("runCount")]
    public int RunCount { get; private set; }
    
    [BsonElement("successCount")]
    public int SuccessCount { get; private set; }
    
    [BsonElement("failureCount")]
    public int FailureCount { get; private set; }
    
    [BsonElement("lastErrorMessage")]
    public string? LastErrorMessage { get; private set; }
    
    [BsonElement("priority")]
    public int Priority { get; private set; } = 5; // 1-10, higher = more important
    
    [BsonElement("timeoutMinutes")]
    public int TimeoutMinutes { get; private set; } = 60; // Max execution time
    
    [BsonElement("maxRetryAttempts")]
    public int MaxRetryAttempts { get; private set; } = 3;
    
    [BsonElement("hangfireJobId")]
    public string? HangfireJobId { get; private set; } // Reference to Hangfire's internal job ID

    // Private constructor for MongoDB
    private ScheduledJob() { }

    public ScheduledJob(
        string name,
        string jobType,
        ScheduleType scheduleType,
        string? cronExpression = null,
        int? intervalMinutes = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Job name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(jobType))
            throw new ArgumentException("Job type cannot be empty", nameof(jobType));

        // Validate schedule configuration
        if (scheduleType == ScheduleType.Cron && string.IsNullOrWhiteSpace(cronExpression))
            throw new ArgumentException("Cron expression required for Cron schedule type", nameof(cronExpression));
        if (scheduleType == ScheduleType.Interval && (!intervalMinutes.HasValue || intervalMinutes.Value <= 0))
            throw new ArgumentException("Interval minutes required for Interval schedule type", nameof(intervalMinutes));

        Name = name;
        JobType = jobType;
        ScheduleType = scheduleType;
        CronExpression = cronExpression;
        IntervalMinutes = intervalMinutes;
        Description = description;
        IsEnabled = false; // Start disabled by default
        RunCount = 0;
        SuccessCount = 0;
        FailureCount = 0;
    }

    public void Enable()
    {
        if (!IsEnabled)
        {
            IsEnabled = true;
            UpdateTimestamp();
        }
    }

    public void Disable()
    {
        if (IsEnabled)
        {
            IsEnabled = false;
            UpdateTimestamp();
        }
    }

    public void UpdateNextRunTime(DateTime nextRun)
    {
        NextRunAt = nextRun;
        UpdateTimestamp();
    }

    public void RecordJobRun(DateTime startTime, DateTime endTime, string status, string? errorMessage = null)
    {
        LastRunAt = startTime;
        LastRunDuration = endTime - startTime;
        LastRunStatus = status;
        LastErrorMessage = errorMessage;
        RunCount++;
        
        if (status == "Completed" || status == "Succeeded")
        {
            SuccessCount++;
        }
        else if (status == "Failed")
        {
            FailureCount++;
        }
        
        UpdateTimestamp();
    }

    public void SetHangfireJobId(string hangfireJobId)
    {
        HangfireJobId = hangfireJobId;
        UpdateTimestamp();
    }

    public void UpdateCronExpression(string cronExpression)
    {
        if (ScheduleType != ScheduleType.Cron)
            throw new InvalidOperationException("Cannot set cron expression for non-cron schedule type");
        
        CronExpression = cronExpression;
        UpdateTimestamp();
    }

    public void UpdateInterval(int intervalMinutes)
    {
        if (ScheduleType != ScheduleType.Interval)
            throw new InvalidOperationException("Cannot set interval for non-interval schedule type");
        
        if (intervalMinutes <= 0)
            throw new ArgumentException("Interval must be greater than 0", nameof(intervalMinutes));
        
        IntervalMinutes = intervalMinutes;
        UpdateTimestamp();
    }

    public void UpdateParameters(Dictionary<string, object> parameters)
    {
        Parameters = parameters ?? new Dictionary<string, object>();
        UpdateTimestamp();
    }

    public void UpdatePriority(int priority)
    {
        if (priority < 1 || priority > 10)
            throw new ArgumentException("Priority must be between 1 and 10", nameof(priority));
        
        Priority = priority;
        UpdateTimestamp();
    }
}

