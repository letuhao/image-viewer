using ImageViewer.Domain.Entities;
using MongoDB.Bson;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for FileProcessingJobState entity
/// </summary>
public interface IFileProcessingJobStateRepository : IRepository<FileProcessingJobState>
{
    /// <summary>
    /// Get file processing job state by job ID
    /// </summary>
    Task<FileProcessingJobState?> GetByJobIdAsync(string jobId);
    
    /// <summary>
    /// Get file processing job state by collection ID (most recent)
    /// </summary>
    Task<FileProcessingJobState?> GetByCollectionIdAsync(string collectionId);
    
    /// <summary>
    /// Get file processing job states by job type
    /// </summary>
    Task<IEnumerable<FileProcessingJobState>> GetByJobTypeAsync(string jobType);
    
    /// <summary>
    /// Get all incomplete (resumable) job states
    /// </summary>
    Task<IEnumerable<FileProcessingJobState>> GetIncompleteJobsAsync();
    
    /// <summary>
    /// Get incomplete jobs by job type
    /// </summary>
    Task<IEnumerable<FileProcessingJobState>> GetIncompleteJobsByTypeAsync(string jobType);
    
    /// <summary>
    /// Get all paused job states
    /// </summary>
    Task<IEnumerable<FileProcessingJobState>> GetPausedJobsAsync();
    
    /// <summary>
    /// Get job states that haven't been updated in the specified time period (potentially stale)
    /// </summary>
    Task<IEnumerable<FileProcessingJobState>> GetStaleJobsAsync(TimeSpan stalePeriod);
    
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

