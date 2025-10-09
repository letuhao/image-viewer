using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using MongoDB.Bson;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Background job repository interface
/// </summary>
public interface IBackgroundJobRepository : IRepository<BackgroundJob>
{
    /// <summary>
    /// Get jobs by status
    /// </summary>
    Task<IEnumerable<BackgroundJob>> GetByStatusAsync(JobStatus status);

    /// <summary>
    /// Get jobs by type
    /// </summary>
    Task<IEnumerable<BackgroundJob>> GetByTypeAsync(string jobType);

    /// <summary>
    /// Get running jobs
    /// </summary>
    Task<IEnumerable<BackgroundJob>> GetRunningJobsAsync();

    /// <summary>
    /// Get jobs by date range
    /// </summary>
    Task<IEnumerable<BackgroundJob>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Get jobs older than specified date
    /// </summary>
    Task<IEnumerable<BackgroundJob>> GetOlderThanAsync(DateTime date);

    /// <summary>
    /// Get job statistics
    /// </summary>
    Task<Dictionary<JobStatus, int>> GetJobCountsByStatusAsync();
    
    /// <summary>
    /// Atomically increment stage progress (thread-safe for concurrent consumer updates)
    /// </summary>
    Task<bool> AtomicIncrementStageAsync(ObjectId jobId, string stageName, int incrementBy = 1);
}
