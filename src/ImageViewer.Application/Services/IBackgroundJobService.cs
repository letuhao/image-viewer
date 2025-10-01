using ImageViewer.Application.DTOs.BackgroundJobs;

namespace ImageViewer.Application.Services;

/// <summary>
/// Background job service interface for managing background jobs
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Get job status by ID
    /// </summary>
    Task<BackgroundJobDto> GetJobAsync(Guid jobId);

    /// <summary>
    /// Get all jobs with optional filtering
    /// </summary>
    Task<IEnumerable<BackgroundJobDto>> GetJobsAsync(string? status = null, string? type = null);

    /// <summary>
    /// Create a new background job
    /// </summary>
    Task<BackgroundJobDto> CreateJobAsync(CreateBackgroundJobDto dto);

    /// <summary>
    /// Update job status
    /// </summary>
    Task<BackgroundJobDto> UpdateJobStatusAsync(Guid jobId, string status, string? message = null);

    /// <summary>
    /// Update job progress
    /// </summary>
    Task<BackgroundJobDto> UpdateJobProgressAsync(Guid jobId, int completed, int total, string? currentItem = null);

    /// <summary>
    /// Cancel a job
    /// </summary>
    Task CancelJobAsync(Guid jobId);

    /// <summary>
    /// Delete a job
    /// </summary>
    Task DeleteJobAsync(Guid jobId);

    /// <summary>
    /// Get job statistics
    /// </summary>
    Task<JobStatisticsDto> GetJobStatisticsAsync();

    /// <summary>
    /// Start cache generation job for collection
    /// </summary>
    Task<BackgroundJobDto> StartCacheGenerationJobAsync(Guid collectionId);

    /// <summary>
    /// Start thumbnail generation job for collection
    /// </summary>
    Task<BackgroundJobDto> StartThumbnailGenerationJobAsync(Guid collectionId);

    /// <summary>
    /// Start bulk operation job
    /// </summary>
    Task<BackgroundJobDto> StartBulkOperationJobAsync(BulkOperationDto dto);
}
