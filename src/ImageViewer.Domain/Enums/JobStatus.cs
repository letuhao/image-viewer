namespace ImageViewer.Domain.Enums;

/// <summary>
/// Background job status enumeration
/// </summary>
public enum JobStatus
{
    /// <summary>
    /// Job is pending execution
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Job is currently running
    /// </summary>
    Running = 1,

    /// <summary>
    /// Job completed successfully
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Job failed with an error
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Job was cancelled
    /// </summary>
    Cancelled = 4
}
