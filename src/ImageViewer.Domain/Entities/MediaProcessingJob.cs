using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Media processing job entity - represents media processing tasks and workflows
/// </summary>
public class MediaProcessingJob : BaseEntity
{
    [BsonElement("name")]
    public string Name { get; private set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; private set; }

    [BsonElement("jobType")]
    public string JobType { get; private set; } = string.Empty; // Thumbnail, Resize, Convert, Analyze, Extract, etc.

    [BsonElement("status")]
    public string Status { get; private set; } = "Pending"; // Pending, Running, Completed, Failed, Cancelled, Paused

    [BsonElement("priority")]
    public int Priority { get; private set; } = 0; // Higher number = higher priority

    [BsonElement("sourceFileId")]
    public ObjectId? SourceFileId { get; private set; }

    [BsonElement("sourceFilePath")]
    public string? SourceFilePath { get; private set; }

    [BsonElement("targetFileId")]
    public ObjectId? TargetFileId { get; private set; }

    [BsonElement("targetFilePath")]
    public string? TargetFilePath { get; private set; }

    [BsonElement("inputParameters")]
    public Dictionary<string, object> InputParameters { get; private set; } = new();

    [BsonElement("outputParameters")]
    public Dictionary<string, object> OutputParameters { get; private set; } = new();

    [BsonElement("progress")]
    public double Progress { get; private set; } = 0.0; // 0.0 to 100.0

    [BsonElement("estimatedDuration")]
    public TimeSpan? EstimatedDuration { get; private set; }

    [BsonElement("actualDuration")]
    public TimeSpan? ActualDuration { get; private set; }

    [BsonElement("startedAt")]
    public DateTime? StartedAt { get; private set; }

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; private set; }

    [BsonElement("scheduledAt")]
    public DateTime? ScheduledAt { get; private set; }

    [BsonElement("expiresAt")]
    public DateTime? ExpiresAt { get; private set; }

    [BsonElement("retryCount")]
    public int RetryCount { get; private set; } = 0;

    [BsonElement("maxRetries")]
    public int MaxRetries { get; private set; } = 3;

    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; private set; }

    [BsonElement("errorDetails")]
    public string? ErrorDetails { get; private set; }

    [BsonElement("workerId")]
    public string? WorkerId { get; private set; }

    [BsonElement("workerNode")]
    public string? WorkerNode { get; private set; }

    [BsonElement("queueName")]
    public string? QueueName { get; private set; }

    [BsonElement("batchId")]
    public ObjectId? BatchId { get; private set; }

    [BsonElement("parentJobId")]
    public ObjectId? ParentJobId { get; private set; }

    [BsonElement("childJobs")]
    public List<ObjectId> ChildJobs { get; private set; } = new();

    [BsonElement("dependencies")]
    public List<ObjectId> Dependencies { get; private set; } = new();

    [BsonElement("tags")]
    public List<string> Tags { get; private set; } = new();

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("createdBy")]
    public new ObjectId? CreatedBy { get; private set; }

    [BsonElement("userId")]
    public ObjectId? UserId { get; private set; }

    [BsonElement("collectionId")]
    public ObjectId? CollectionId { get; private set; }

    [BsonElement("resourceUsage")]
    public ResourceUsage ResourceUsage { get; private set; } = new();

    [BsonElement("qualitySettings")]
    public QualitySettings QualitySettings { get; private set; } = new();

    // Navigation properties
    [BsonIgnore]
    public MediaItem? SourceFile { get; private set; }

    [BsonIgnore]
    public MediaItem? TargetFile { get; private set; }

    [BsonIgnore]
    public User? Creator { get; private set; }

    [BsonIgnore]
    public User? User { get; private set; }

    [BsonIgnore]
    public Collection? Collection { get; private set; }

    [BsonIgnore]
    public MediaProcessingJob? ParentJob { get; private set; }

    // Private constructor for EF Core
    private MediaProcessingJob() { }

    public static MediaProcessingJob Create(string name, string jobType, ObjectId? createdBy = null, ObjectId? userId = null, ObjectId? collectionId = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(jobType))
            throw new ArgumentException("Job type cannot be empty", nameof(jobType));

        return new MediaProcessingJob
        {
            Name = name,
            JobType = jobType,
            Description = description,
            CreatedBy = createdBy,
            UserId = userId,
            CollectionId = collectionId,
            Status = "Pending",
            Priority = 0,
            Progress = 0.0,
            RetryCount = 0,
            MaxRetries = 3,
            InputParameters = new Dictionary<string, object>(),
            OutputParameters = new Dictionary<string, object>(),
            ChildJobs = new List<ObjectId>(),
            Dependencies = new List<ObjectId>(),
            Tags = new List<string>(),
            Metadata = new Dictionary<string, object>(),
            ResourceUsage = new ResourceUsage(),
            QualitySettings = new QualitySettings()
        };
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name;
        UpdateTimestamp();
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        UpdateTimestamp();
    }

    public void SetPriority(int priority)
    {
        Priority = priority;
        UpdateTimestamp();
    }

    public void SetSourceFile(ObjectId? sourceFileId, string? sourceFilePath)
    {
        SourceFileId = sourceFileId;
        SourceFilePath = sourceFilePath;
        UpdateTimestamp();
    }

    public void SetTargetFile(ObjectId? targetFileId, string? targetFilePath)
    {
        TargetFileId = targetFileId;
        TargetFilePath = targetFilePath;
        UpdateTimestamp();
    }

    public void AddInputParameter(string key, object value)
    {
        InputParameters[key] = value;
        UpdateTimestamp();
    }

    public void RemoveInputParameter(string key)
    {
        InputParameters.Remove(key);
        UpdateTimestamp();
    }

    public void AddOutputParameter(string key, object value)
    {
        OutputParameters[key] = value;
        UpdateTimestamp();
    }

    public void RemoveOutputParameter(string key)
    {
        OutputParameters.Remove(key);
        UpdateTimestamp();
    }

    public void UpdateProgress(double progress)
    {
        if (progress < 0.0 || progress > 100.0)
            throw new ArgumentException("Progress must be between 0.0 and 100.0", nameof(progress));

        Progress = progress;
        UpdateTimestamp();
    }

    public void SetEstimatedDuration(TimeSpan? estimatedDuration)
    {
        EstimatedDuration = estimatedDuration;
        UpdateTimestamp();
    }

    public void Start(string? workerId = null, string? workerNode = null)
    {
        Status = "Running";
        StartedAt = DateTime.UtcNow;
        WorkerId = workerId;
        WorkerNode = workerNode;
        Progress = 0.0;
        UpdateTimestamp();
    }

    public void Complete()
    {
        Status = "Completed";
        CompletedAt = DateTime.UtcNow;
        Progress = 100.0;
        
        if (StartedAt.HasValue)
        {
            ActualDuration = CompletedAt.Value - StartedAt.Value;
        }
        
        UpdateTimestamp();
    }

    public void Fail(string errorMessage, string? errorDetails = null)
    {
        Status = "Failed";
        ErrorMessage = errorMessage;
        ErrorDetails = errorDetails;
        CompletedAt = DateTime.UtcNow;
        
        if (StartedAt.HasValue)
        {
            ActualDuration = CompletedAt.Value - StartedAt.Value;
        }
        
        UpdateTimestamp();
    }

    public void Cancel(string? reason = null)
    {
        Status = "Cancelled";
        ErrorMessage = reason ?? "Job cancelled by user";
        CompletedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void Pause(string? reason = null)
    {
        Status = "Paused";
        ErrorMessage = reason ?? "Job paused";
        UpdateTimestamp();
    }

    public void Resume()
    {
        if (Status == "Paused")
        {
            Status = "Running";
            ErrorMessage = null;
            UpdateTimestamp();
        }
    }

    public void Schedule(DateTime scheduledAt)
    {
        ScheduledAt = scheduledAt;
        Status = "Pending";
        UpdateTimestamp();
    }

    public void SetExpiration(DateTime? expiresAt)
    {
        ExpiresAt = expiresAt;
        UpdateTimestamp();
    }

    public void SetMaxRetries(int maxRetries)
    {
        if (maxRetries < 0)
            throw new ArgumentException("Max retries cannot be negative", nameof(maxRetries));

        MaxRetries = maxRetries;
        UpdateTimestamp();
    }

    public void IncrementRetry()
    {
        RetryCount++;
        UpdateTimestamp();
    }

    public void ResetRetry()
    {
        RetryCount = 0;
        ErrorMessage = null;
        ErrorDetails = null;
        UpdateTimestamp();
    }

    public void SetQueue(string? queueName)
    {
        QueueName = queueName;
        UpdateTimestamp();
    }

    public void SetBatch(ObjectId? batchId)
    {
        BatchId = batchId;
        UpdateTimestamp();
    }

    public void SetParentJob(ObjectId? parentJobId)
    {
        ParentJobId = parentJobId;
        UpdateTimestamp();
    }

    public void AddChildJob(ObjectId childJobId)
    {
        if (!ChildJobs.Contains(childJobId))
        {
            ChildJobs.Add(childJobId);
            UpdateTimestamp();
        }
    }

    public void RemoveChildJob(ObjectId childJobId)
    {
        ChildJobs.Remove(childJobId);
        UpdateTimestamp();
    }

    public void AddDependency(ObjectId dependencyId)
    {
        if (!Dependencies.Contains(dependencyId))
        {
            Dependencies.Add(dependencyId);
            UpdateTimestamp();
        }
    }

    public void RemoveDependency(ObjectId dependencyId)
    {
        Dependencies.Remove(dependencyId);
        UpdateTimestamp();
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty", nameof(tag));

        if (!Tags.Contains(tag))
        {
            Tags.Add(tag);
            UpdateTimestamp();
        }
    }

    public void RemoveTag(string tag)
    {
        Tags.Remove(tag);
        UpdateTimestamp();
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }

    public void RemoveMetadata(string key)
    {
        Metadata.Remove(key);
        UpdateTimestamp();
    }

    public void UpdateResourceUsage(ResourceUsage resourceUsage)
    {
        ResourceUsage = resourceUsage ?? new ResourceUsage();
        UpdateTimestamp();
    }

    public void UpdateQualitySettings(QualitySettings qualitySettings)
    {
        QualitySettings = qualitySettings ?? new QualitySettings();
        UpdateTimestamp();
    }

    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    }

    public bool CanRetry()
    {
        return Status == "Failed" && RetryCount < MaxRetries;
    }

    public bool IsScheduled()
    {
        return ScheduledAt.HasValue && ScheduledAt.Value > DateTime.UtcNow;
    }

    public bool IsRunning()
    {
        return Status == "Running";
    }

    public bool IsCompleted()
    {
        return Status == "Completed";
    }

    public bool IsFailed()
    {
        return Status == "Failed";
    }

    public bool IsCancelled()
    {
        return Status == "Cancelled";
    }

    public bool IsPending()
    {
        return Status == "Pending";
    }

    public bool IsPaused()
    {
        return Status == "Paused";
    }

    public TimeSpan GetElapsedTime()
    {
        if (!StartedAt.HasValue)
            return TimeSpan.Zero;

        var endTime = CompletedAt ?? DateTime.UtcNow;
        return endTime - StartedAt.Value;
    }

    public double GetEfficiency()
    {
        if (!EstimatedDuration.HasValue || !ActualDuration.HasValue)
            return 0.0;

        return (double)EstimatedDuration.Value.TotalSeconds / ActualDuration.Value.TotalSeconds;
    }

    public bool IsHighPriority()
    {
        return Priority >= 8;
    }

    public bool IsLowPriority()
    {
        return Priority <= 2;
    }
}

/// <summary>
/// Resource usage entity
/// </summary>
public class ResourceUsage
{
    [BsonElement("cpuUsage")]
    public double CpuUsage { get; set; } = 0.0; // percentage

    [BsonElement("memoryUsage")]
    public long MemoryUsage { get; set; } = 0; // bytes

    [BsonElement("diskUsage")]
    public long DiskUsage { get; set; } = 0; // bytes

    [BsonElement("networkUsage")]
    public long NetworkUsage { get; set; } = 0; // bytes

    [BsonElement("processingTime")]
    public TimeSpan ProcessingTime { get; set; } = TimeSpan.Zero;

    [BsonElement("peakMemoryUsage")]
    public long PeakMemoryUsage { get; set; } = 0; // bytes

    public static ResourceUsage Create(double cpuUsage = 0.0, long memoryUsage = 0, long diskUsage = 0, long networkUsage = 0)
    {
        return new ResourceUsage
        {
            CpuUsage = cpuUsage,
            MemoryUsage = memoryUsage,
            DiskUsage = diskUsage,
            NetworkUsage = networkUsage,
            ProcessingTime = TimeSpan.Zero,
            PeakMemoryUsage = 0
        };
    }
}

/// <summary>
/// Quality settings entity
/// </summary>
public class QualitySettings
{
    [BsonElement("quality")]
    public int Quality { get; set; } = 85; // 1-100

    [BsonElement("format")]
    public string Format { get; set; } = "JPEG";

    [BsonElement("compression")]
    public string Compression { get; set; } = "Lossy";

    [BsonElement("resolution")]
    public string? Resolution { get; set; }

    [BsonElement("colorSpace")]
    public string ColorSpace { get; set; } = "sRGB";

    [BsonElement("bitDepth")]
    public int BitDepth { get; set; } = 8;

    public static QualitySettings Create(int quality = 85, string format = "JPEG", string compression = "Lossy")
    {
        return new QualitySettings
        {
            Quality = quality,
            Format = format,
            Compression = compression,
            Resolution = null,
            ColorSpace = "sRGB",
            BitDepth = 8
        };
    }
}
