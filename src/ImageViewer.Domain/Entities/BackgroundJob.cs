using ImageViewer.Domain.Enums;
using ImageViewer.Domain.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

#pragma warning disable CS8618 // MongoDB entities/value objects are initialized by the driver

namespace ImageViewer.Domain.Entities;

/// <summary>
/// BackgroundJob entity - represents a background processing job
/// </summary>
public class BackgroundJob : BaseEntity
{
    [BsonElement("jobType")]
    public string JobType { get; private set; }
    
    [BsonElement("status")]
    public string Status { get; private set; }
    
    [BsonElement("parameters")]
    public string? Parameters { get; private set; }
    
    [BsonElement("result")]
    public string? Result { get; private set; }
    
    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; private set; }
    
    [BsonElement("progress")]
    public int Progress { get; private set; }
    
    [BsonElement("totalItems")]
    public int TotalItems { get; private set; }
    
    [BsonElement("completedItems")]
    public int CompletedItems { get; private set; }
    
    [BsonElement("currentItem")]
    public string? CurrentItem { get; private set; }
    
    [BsonElement("message")]
    public string? Message { get; private set; }
    
    [BsonElement("errors")]
    public List<string>? Errors { get; private set; }
    
    [BsonElement("estimatedCompletion")]
    public DateTime? EstimatedCompletion { get; private set; }
    
    [BsonElement("startedAt")]
    public DateTime? StartedAt { get; private set; }
    
    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; private set; }
    
    // Multi-stage job tracking (for complex jobs like collection-scan)
    [BsonElement("stages")]
    public Dictionary<string, JobStageInfo>? Stages { get; private set; }
    
    [BsonElement("currentStage")]
    public string? CurrentStage { get; private set; }
    
    // Reference to collection being processed (for collection-scan jobs)
    [BsonElement("collectionId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId? CollectionId { get; private set; }
    
    // Error tracking for jobs that complete with errors
    [BsonElement("hasErrors")]
    public bool HasErrors { get; private set; }
    
    [BsonElement("errorCount")]
    public int ErrorCount { get; private set; }
    
    [BsonElement("successCount")]
    public int SuccessCount { get; private set; }
    
    [BsonElement("errorSummary")]
    public Dictionary<string, int>? ErrorSummary { get; private set; } // Error type -> count

    // Private constructor for EF Core
    private BackgroundJob() { }

    public BackgroundJob(string jobType, string? parameters = null, bool isMultiStage = false)
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
        
        if (isMultiStage)
        {
            Stages = new Dictionary<string, JobStageInfo>();
        }
    }

    public BackgroundJob(string jobType, string description, Dictionary<string, object> parameters, bool isMultiStage = false)
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
        
        if (isMultiStage)
        {
            Stages = new Dictionary<string, JobStageInfo>();
        }
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
    
    public void SetCollectionId(ObjectId collectionId)
    {
        CollectionId = collectionId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete(string? result = null)
    {
        if (Status != JobStatus.Running.ToString())
            throw new InvalidOperationException($"Cannot complete job with status '{Status}'");

        Status = JobStatus.Completed.ToString();
        Result = result;
        CompletedAt = DateTime.UtcNow;
    }

    public void CompleteWithErrors(string? result = null)
    {
        if (Status != JobStatus.Running.ToString())
            throw new InvalidOperationException($"Cannot complete job with status '{Status}'");

        Status = JobStatus.Completed.ToString();
        Result = result;
        CompletedAt = DateTime.UtcNow;
        HasErrors = true;
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

    // Multi-stage job management
    public void AddStage(string stageName)
    {
        if (Stages == null)
        {
            Stages = new Dictionary<string, JobStageInfo>();
        }
        
        if (!Stages.ContainsKey(stageName))
        {
            Stages[stageName] = new JobStageInfo(stageName);
        }
    }

    public void StartStage(string stageName, int totalItems = 0, string? message = null)
    {
        if (Stages == null || !Stages.ContainsKey(stageName))
        {
            AddStage(stageName);
        }
        
        Stages![stageName].Start(totalItems, message);
        CurrentStage = stageName;
    }

    public void UpdateStageProgress(string stageName, int completed, int total, string? message = null)
    {
        if (Stages != null && Stages.ContainsKey(stageName))
        {
            Stages[stageName].UpdateProgress(completed, total, message);
            
            // Update overall job progress based on all stages
            RecalculateOverallProgress();
        }
    }

    public void CompleteStage(string stageName, string? message = null)
    {
        if (Stages != null && Stages.ContainsKey(stageName))
        {
            Stages[stageName].Complete(message);
            
            // Check if all stages are complete
            if (Stages.Values.All(s => s.Status == "Completed"))
            {
                Status = JobStatus.Completed.ToString();
                CompletedAt = DateTime.UtcNow;
                Message = "All stages completed successfully";
            }
        }
    }

    public void FailStage(string stageName, string errorMessage)
    {
        if (Stages != null && Stages.ContainsKey(stageName))
        {
            Stages[stageName].Fail(errorMessage);
            Status = JobStatus.Failed.ToString();
            ErrorMessage = $"Stage '{stageName}' failed: {errorMessage}";
            CompletedAt = DateTime.UtcNow;
        }
    }

    private void RecalculateOverallProgress()
    {
        if (Stages == null || Stages.Count == 0)
        {
            return;
        }

        // Calculate average progress across all stages
        var totalProgress = Stages.Values.Sum(s => s.Progress);
        Progress = totalProgress / Stages.Count;
        
        // For multi-stage jobs, use the FIRST stage's totals (all stages process the same items)
        // Example: scan finds 39 images, then thumbnail processes 39, cache processes 39
        // Don't sum them (would be 117), use the scan total (39)
        var scanStage = Stages.ContainsKey("scan") ? Stages["scan"] : Stages.Values.FirstOrDefault();
        if (scanStage != null)
        {
            CompletedItems = scanStage.CompletedItems;
            TotalItems = scanStage.TotalItems;
        }
    }

    /// <summary>
    /// Update error tracking statistics for jobs that complete with errors
    /// </summary>
    public void UpdateErrorStatistics(int successCount, int errorCount, Dictionary<string, int>? errorSummary = null)
    {
        SuccessCount = successCount;
        ErrorCount = errorCount;
        HasErrors = errorCount > 0;
        ErrorSummary = errorSummary ?? new Dictionary<string, int>();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add an error to the error summary
    /// </summary>
    public void AddError(string errorType)
    {
        if (ErrorSummary == null)
        {
            ErrorSummary = new Dictionary<string, int>();
        }

        if (ErrorSummary.ContainsKey(errorType))
        {
            ErrorSummary[errorType]++;
        }
        else
        {
            ErrorSummary[errorType] = 1;
        }

        ErrorCount++;
        HasErrors = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get error summary as formatted string
    /// </summary>
    public string GetErrorSummaryString()
    {
        if (!HasErrors || ErrorSummary == null || ErrorSummary.Count == 0)
        {
            return "No errors";
        }

        return string.Join(", ", ErrorSummary.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
    }
}