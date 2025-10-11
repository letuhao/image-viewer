namespace ImageViewer.Application.DTOs;

/// <summary>
/// DTO for scheduled job information
/// </summary>
public class ScheduledJobDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string JobType { get; set; } = string.Empty;
    public string ScheduleType { get; set; } = string.Empty;
    public string? CronExpression { get; set; }
    public int? IntervalMinutes { get; set; }
    public bool IsEnabled { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? HangfireJobId { get; set; } // Null = orphaned, not bound to Hangfire
    public string? LibraryId { get; set; } // Reference to Library - can be null for non-library jobs
    
    // Execution statistics
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    public TimeSpan? LastRunDuration { get; set; }
    public string? LastRunStatus { get; set; }
    public int RunCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public string? LastErrorMessage { get; set; }
    
    // Settings
    public int Priority { get; set; }
    public int TimeoutMinutes { get; set; }
    public int MaxRetryAttempts { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for scheduled job run history
/// </summary>
public class ScheduledJobRunDto
{
    public string Id { get; set; } = string.Empty;
    public string ScheduledJobId { get; set; } = string.Empty;
    public string ScheduledJobName { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Result { get; set; }
    public string TriggeredBy { get; set; } = string.Empty;
}

/// <summary>
/// Request to create or update a scheduled job
/// </summary>
public class CreateScheduledJobRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string JobType { get; set; } = string.Empty;
    public string ScheduleType { get; set; } = "Cron";
    public string? CronExpression { get; set; }
    public int? IntervalMinutes { get; set; }
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Request to update scheduled job settings
/// </summary>
public class UpdateScheduledJobRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? CronExpression { get; set; }
    public int? IntervalMinutes { get; set; }
    public bool? IsEnabled { get; set; }
    public int? Priority { get; set; }
    public int? TimeoutMinutes { get; set; }
}

