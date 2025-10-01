namespace ImageViewer.Application.DTOs.BackgroundJobs;

/// <summary>
/// Background job DTO
/// </summary>
public class BackgroundJobDto
{
    public Guid JobId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public JobProgressDto Progress { get; set; } = null!;
    public DateTime? StartedAt { get; set; }
    public DateTime? EstimatedCompletion { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Job progress DTO
/// </summary>
public class JobProgressDto
{
    public int Total { get; set; }
    public int Completed { get; set; }
    public double Percentage { get; set; }
    public string? CurrentItem { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
}

/// <summary>
/// Create background job DTO
/// </summary>
public class CreateBackgroundJobDto
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? CollectionId { get; set; }
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
/// Bulk operation DTO
/// </summary>
public class BulkOperationDto
{
    public string OperationType { get; set; } = string.Empty;
    public List<Guid> TargetIds { get; set; } = new List<Guid>();
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
}
