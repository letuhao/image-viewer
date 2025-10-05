using ImageViewer.Domain.Enums;
using MongoDB.Bson;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// BackgroundJob entity - represents a background processing job
/// </summary>
public class BackgroundJob : BaseEntity
{
    public string JobType { get; private set; }
    public string Status { get; private set; }
    public string? Parameters { get; private set; }
    public string? Result { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int Progress { get; private set; }
    public int TotalItems { get; private set; }
    public int CompletedItems { get; private set; }
    public string? CurrentItem { get; private set; }
    public string? Message { get; private set; }
    public List<string>? Errors { get; private set; }
    public DateTime? EstimatedCompletion { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // Private constructor for EF Core
    private BackgroundJob() { }

    public BackgroundJob(string jobType, string? parameters = null)
    {
        Id = ObjectId.GenerateNewId();
        JobType = jobType ?? throw new ArgumentNullException(nameof(jobType));
        Status = JobStatus.Pending.ToString();
        Parameters = parameters;
        Progress = 0;
        TotalItems = 0;
        CompletedItems = 0;
        Errors = new List<string>();
        CreatedAt = DateTime.UtcNow;
    }

    public BackgroundJob(string jobType, string description, Dictionary<string, object> parameters)
    {
        Id = ObjectId.GenerateNewId();
        JobType = jobType ?? throw new ArgumentNullException(nameof(jobType));
        Status = JobStatus.Pending.ToString();
        Parameters = System.Text.Json.JsonSerializer.Serialize(parameters);
        Progress = 0;
        TotalItems = 0;
        CompletedItems = 0;
        Errors = new List<string>();
        CreatedAt = DateTime.UtcNow;
    }

    public void Start()
    {
        if (Status != JobStatus.Pending.ToString())
            throw new InvalidOperationException($"Cannot start job with status '{Status}'");

        Status = JobStatus.Running.ToString();
        StartedAt = DateTime.UtcNow;
    }

    public void UpdateProgress(int completed, int total)
    {
        if (Status != JobStatus.Running.ToString())
            throw new InvalidOperationException($"Cannot update progress for job with status '{Status}'");

        if (completed < 0)
            throw new ArgumentException("Completed cannot be negative", nameof(completed));
        if (total < 0)
            throw new ArgumentException("Total cannot be negative", nameof(total));
        if (completed > total)
            throw new ArgumentException("Completed cannot exceed total");

        CompletedItems = completed;
        TotalItems = total;
        Progress = total > 0 ? (int)((double)completed / total * 100) : 0;
    }

    public void UpdateStatus(JobStatus status)
    {
        Status = status.ToString();
    }

    public void UpdateMessage(string message)
    {
        Message = message;
    }

    public void UpdateCurrentItem(string currentItem)
    {
        CurrentItem = currentItem;
    }

    public void Complete(string? result = null)
    {
        if (Status != JobStatus.Running.ToString())
            throw new InvalidOperationException($"Cannot complete job with status '{Status}'");

        Status = JobStatus.Completed.ToString();
        Result = result;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        if (Status != JobStatus.Running.ToString())
            throw new InvalidOperationException($"Cannot fail job with status '{Status}'");

        Status = JobStatus.Failed.ToString();
        ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status != JobStatus.Pending.ToString() && Status != JobStatus.Running.ToString())
            throw new InvalidOperationException($"Cannot cancel job with status '{Status}'");

        Status = JobStatus.Cancelled.ToString();
        CompletedAt = DateTime.UtcNow;
    }

    public double GetProgressPercentage()
    {
        return TotalItems > 0 ? (double)Progress / TotalItems * 100 : 0;
    }

    public TimeSpan? GetDuration()
    {
        if (StartedAt == null)
            return null;

        var endTime = CompletedAt ?? DateTime.UtcNow;
        return endTime - StartedAt.Value;
    }

    public bool IsCompleted()
    {
        return Status == JobStatus.Completed.ToString();
    }

    public bool IsFailed()
    {
        return Status == JobStatus.Failed.ToString();
    }

    public bool IsCancelled()
    {
        return Status == JobStatus.Cancelled.ToString();
    }

    public bool IsRunning()
    {
        return Status == JobStatus.Running.ToString();
    }

    public bool IsPending()
    {
        return Status == JobStatus.Pending.ToString();
    }
}
