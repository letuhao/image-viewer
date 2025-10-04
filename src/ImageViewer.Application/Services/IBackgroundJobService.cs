using ImageViewer.Application.DTOs.BackgroundJobs;
using MongoDB.Bson;

namespace ImageViewer.Application.Services;

/// <summary>
/// Background job service interface for managing background jobs
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Get job status by ID
    /// </summary>
    Task<BackgroundJobDto> GetJobAsync(ObjectId jobId);

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
    Task<BackgroundJobDto> UpdateJobStatusAsync(ObjectId jobId, string status, string? message = null);

    /// <summary>
    /// Update job progress
    /// </summary>
    Task<BackgroundJobDto> UpdateJobProgressAsync(ObjectId jobId, int completed, int total, string? currentItem = null);

    /// <summary>
    /// Cancel a job
    /// </summary>
    Task CancelJobAsync(ObjectId jobId);

    /// <summary>
    /// Delete a job
    /// </summary>
    Task DeleteJobAsync(ObjectId jobId);

    /// <summary>
    /// Get job statistics
    /// </summary>
    Task<JobStatisticsDto> GetJobStatisticsAsync();

    /// <summary>
    /// Start cache generation job for collection
    /// </summary>
    Task<BackgroundJobDto> StartCacheGenerationJobAsync(ObjectId collectionId);

    /// <summary>
    /// Start thumbnail generation job for collection
    /// </summary>
    Task<BackgroundJobDto> StartThumbnailGenerationJobAsync(ObjectId collectionId);

    /// <summary>
    /// Start bulk operation job
    /// </summary>
    Task<BackgroundJobDto> StartBulkOperationJobAsync(BulkOperationDto dto);
}
