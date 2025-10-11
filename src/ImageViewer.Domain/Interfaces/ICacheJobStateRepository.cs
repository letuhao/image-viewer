using ImageViewer.Domain.Entities;
using MongoDB.Bson;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for CacheJobState entity
/// </summary>
public interface ICacheJobStateRepository : IRepository<CacheJobState>
{
    /// <summary>
    /// Get cache job state by job ID
    /// </summary>
    Task<CacheJobState?> GetByJobIdAsync(string jobId);
    
    /// <summary>
    /// Get cache job state by collection ID
    /// </summary>
    Task<CacheJobState?> GetByCollectionIdAsync(string collectionId);
    
    /// <summary>
    /// Get all incomplete (resumable) cache job states
    /// </summary>
    Task<IEnumerable<CacheJobState>> GetIncompleteJobsAsync();
    
    /// <summary>
    /// Get all paused cache job states
    /// </summary>
    Task<IEnumerable<CacheJobState>> GetPausedJobsAsync();
    
    /// <summary>
    /// Get cache job states that haven't been updated in the specified time period (potentially stale)
    /// </summary>
    Task<IEnumerable<CacheJobState>> GetStaleJobsAsync(TimeSpan stalePeriod);
    
    /// <summary>
    /// Check if an image has been processed in a job
    /// </summary>
    Task<bool> IsImageProcessedAsync(string jobId, string imageId);
    
    /// <summary>
    /// Atomically increment completed count and add image to processed list
    /// </summary>
    Task<bool> AtomicIncrementCompletedAsync(string jobId, string imageId, long sizeBytes);
    
    /// <summary>
    /// Atomically increment failed count and add image to failed list
    /// </summary>
    Task<bool> AtomicIncrementFailedAsync(string jobId, string imageId);
    
    /// <summary>
    /// Atomically increment skipped count and add image to processed list
    /// </summary>
    Task<bool> AtomicIncrementSkippedAsync(string jobId, string imageId);
    
    /// <summary>
    /// Update job status
    /// </summary>
    Task<bool> UpdateStatusAsync(string jobId, string status, string? errorMessage = null);
    
    /// <summary>
    /// Delete old completed jobs (cleanup)
    /// </summary>
    Task<int> DeleteOldCompletedJobsAsync(DateTime olderThan);
}

