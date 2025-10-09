using MongoDB.Bson;

namespace ImageViewer.Application.DTOs.BackgroundJobs;

/// <summary>
/// Background job DTO with comprehensive monitoring
/// </summary>
public class BackgroundJobDto
{
    public ObjectId JobId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public JobProgressDto Progress { get; set; } = null!;
    public JobTimingDto Timing { get; set; } = null!;
    public JobMetricsDto Metrics { get; set; } = null!;
    public JobHealthDto Health { get; set; } = null!;
    public DateTime? StartedAt { get; set; }
    public DateTime? EstimatedCompletion { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    public List<JobStepDto> Steps { get; set; } = new List<JobStepDto>();
    public JobDependenciesDto Dependencies { get; set; } = null!;
}

/// <summary>
/// Enhanced job progress DTO with detailed tracking
/// </summary>
public class JobProgressDto
{
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public int Pending { get; set; }
    public double Percentage { get; set; }
    public string? CurrentItem { get; set; }
    public string? CurrentStep { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
    public List<string> Warnings { get; set; } = new List<string>();
    public Dictionary<string, int> ItemCounts { get; set; } = new Dictionary<string, int>();
    public double ItemsPerSecond { get; set; }
    public TimeSpan EstimatedTimeRemaining { get; set; }
}

/// <summary>
/// Create background job DTO
/// </summary>
public class CreateBackgroundJobDto
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ObjectId? CollectionId { get; set; }
}

/// <summary>
/// Update job status DTO
/// </summary>
public class UpdateJobStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
}

/// <summary>
/// Update job progress DTO
/// </summary>
public class UpdateJobProgressDto
{
    public int Completed { get; set; }
    public int Total { get; set; }
    public string? CurrentItem { get; set; }
    public List<string>? Errors { get; set; }
}

/// <summary>
/// Job statistics DTO
/// </summary>
public class JobStatisticsDto
{
    public int TotalJobs { get; set; }
    public int RunningJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int FailedJobs { get; set; }
    public int CancelledJobs { get; set; }
    public double SuccessRate { get; set; }
}

/// <summary>
/// Job timing information DTO
/// </summary>
public class JobTimingDto
{
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public TimeSpan? EstimatedDuration { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public double AverageStepDuration { get; set; }
    public Dictionary<string, TimeSpan> StepDurations { get; set; } = new Dictionary<string, TimeSpan>();
}

/// <summary>
/// Job metrics DTO for performance monitoring
/// </summary>
public class JobMetricsDto
{
    public long MemoryUsageBytes { get; set; }
    public double CpuUsagePercent { get; set; }
    public long DiskReadBytes { get; set; }
    public long DiskWriteBytes { get; set; }
    public int NetworkRequests { get; set; }
    public double ItemsPerSecond { get; set; }
    public double BytesPerSecond { get; set; }
    public int RetryCount { get; set; }
    public int TimeoutCount { get; set; }
    public Dictionary<string, double> CustomMetrics { get; set; } = new Dictionary<string, double>();
}

/// <summary>
/// Job health status DTO
/// </summary>
public class JobHealthDto
{
    public string Status { get; set; } = "Healthy"; // Healthy, Warning, Critical, Failed
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public bool IsStuck { get; set; }
    public bool IsTimedOut { get; set; }
    public List<string> HealthIssues { get; set; } = new List<string>();
    public Dictionary<string, object> HealthChecks { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Individual job step DTO
/// </summary>
public class JobStepDto
{
    public string StepName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Pending, Running, Completed, Failed, Skipped
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public int ItemsProcessed { get; set; }
    public int ItemsTotal { get; set; }
    public double Percentage { get; set; }
    public string? CurrentItem { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
    public List<string> Warnings { get; set; } = new List<string>();
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Job dependencies DTO
/// </summary>
public class JobDependenciesDto
{
    public List<ObjectId> DependsOn { get; set; } = new List<ObjectId>();
    public List<ObjectId> Blocks { get; set; } = new List<ObjectId>();
    public List<ObjectId> ChildJobs { get; set; } = new List<ObjectId>();
    public ObjectId? ParentJob { get; set; }
    public int DependencyLevel { get; set; }
    public bool CanStart { get; set; }
    public List<string> BlockingReasons { get; set; } = new List<string>();
}

/// <summary>
/// Enhanced bulk operation DTO
/// </summary>
public class BulkOperationDto
{
    public string OperationType { get; set; } = string.Empty;
    public List<Guid> TargetIds { get; set; } = new List<Guid>();
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    public BulkOperationProgressDto Progress { get; set; } = null!;
    public List<BulkOperationStepDto> Steps { get; set; } = new List<BulkOperationStepDto>();
}

/// <summary>
/// Bulk operation progress DTO
/// </summary>
public class BulkOperationProgressDto
{
    public int TotalCollections { get; set; }
    public int ProcessedCollections { get; set; }
    public int CreatedCollections { get; set; }
    public int UpdatedCollections { get; set; }
    public int SkippedCollections { get; set; }
    public int FailedCollections { get; set; }
    public int TotalImages { get; set; }
    public int ProcessedImages { get; set; }
    public int GeneratedThumbnails { get; set; }
    public int GeneratedCache { get; set; }
    public double Percentage { get; set; }
    public TimeSpan EstimatedTimeRemaining { get; set; }
    public double ItemsPerSecond { get; set; }
}

/// <summary>
/// Bulk operation step DTO
/// </summary>
public class BulkOperationStepDto
{
    public string StepName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Failed { get; set; }
    public double Percentage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
}
