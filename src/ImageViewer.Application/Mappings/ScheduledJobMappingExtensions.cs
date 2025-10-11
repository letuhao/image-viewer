using ImageViewer.Application.DTOs;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Application.Mappings;

/// <summary>
/// Mapping extensions for ScheduledJob entities
/// </summary>
public static class ScheduledJobMappingExtensions
{
    public static ScheduledJobDto ToDto(this ScheduledJob job)
    {
        // Convert Parameters, ensuring ObjectIds are serialized as strings
        var parameters = new Dictionary<string, object>();
        foreach (var kvp in job.Parameters)
        {
            if (kvp.Value is MongoDB.Bson.ObjectId objectId)
            {
                parameters[kvp.Key] = objectId.ToString();
            }
            else
            {
                parameters[kvp.Key] = kvp.Value;
            }
        }

        return new ScheduledJobDto
        {
            Id = job.Id.ToString(),
            Name = job.Name,
            Description = job.Description,
            JobType = job.JobType,
            ScheduleType = job.ScheduleType.ToString(),
            CronExpression = job.CronExpression,
            IntervalMinutes = job.IntervalMinutes,
            IsEnabled = job.IsEnabled,
            Parameters = parameters,
            HangfireJobId = job.HangfireJobId, // Null if not bound to Hangfire
            LibraryId = job.LibraryId?.ToString(), // Convert ObjectId to string
            LastRunAt = job.LastRunAt,
            NextRunAt = job.NextRunAt,
            LastRunDuration = job.LastRunDuration,
            LastRunStatus = job.LastRunStatus,
            RunCount = job.RunCount,
            SuccessCount = job.SuccessCount,
            FailureCount = job.FailureCount,
            LastErrorMessage = job.LastErrorMessage,
            Priority = job.Priority,
            TimeoutMinutes = job.TimeoutMinutes,
            MaxRetryAttempts = job.MaxRetryAttempts,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt
        };
    }

    public static ScheduledJobRunDto ToDto(this ScheduledJobRun jobRun)
    {
        return new ScheduledJobRunDto
        {
            Id = jobRun.Id.ToString(),
            ScheduledJobId = jobRun.ScheduledJobId.ToString(),
            ScheduledJobName = jobRun.ScheduledJobName,
            JobType = jobRun.JobType,
            Status = jobRun.Status,
            StartedAt = jobRun.StartedAt,
            CompletedAt = jobRun.CompletedAt,
            Duration = jobRun.Duration,
            ErrorMessage = jobRun.ErrorMessage,
            Result = jobRun.Result != null ? new Dictionary<string, object>(jobRun.Result) : null,
            TriggeredBy = jobRun.TriggeredBy
        };
    }

    public static List<ScheduledJobDto> ToDto(this IEnumerable<ScheduledJob> jobs)
    {
        return jobs.Select(j => j.ToDto()).ToList();
    }

    public static List<ScheduledJobRunDto> ToDto(this IEnumerable<ScheduledJobRun> jobRuns)
    {
        return jobRuns.Select(jr => jr.ToDto()).ToList();
    }
}

