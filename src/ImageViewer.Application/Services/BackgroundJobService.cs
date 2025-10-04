using ImageViewer.Application.DTOs.BackgroundJobs;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace ImageViewer.Application.Services;

/// <summary>
/// Background job service implementation
/// </summary>
public class BackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobRepository _backgroundJobRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly ILogger<BackgroundJobService> _logger;

    public BackgroundJobService(
        IBackgroundJobRepository backgroundJobRepository,
        ICollectionRepository collectionRepository,
        ILogger<BackgroundJobService> logger)
    {
        _backgroundJobRepository = backgroundJobRepository;
        _collectionRepository = collectionRepository;
        _logger = logger;
    }

    public async Task<BackgroundJobDto> GetJobAsync(ObjectId jobId)
    {
        _logger.LogInformation("Getting job: {JobId}", jobId);

        var job = await _backgroundJobRepository.GetByIdAsync(jobId);
        if (job == null)
        {
            throw new ArgumentException($"Job with ID {jobId} not found");
        }

        return MapToDto(job);
    }

    public async Task<IEnumerable<BackgroundJobDto>> GetJobsAsync(string? status = null, string? type = null)
    {
        _logger.LogInformation("Getting jobs with status: {Status}, type: {Type}", status, type);

        var jobs = await _backgroundJobRepository.GetAllAsync();
        
        if (!string.IsNullOrEmpty(status))
        {
            jobs = jobs.Where(j => j.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(type))
        {
            jobs = jobs.Where(j => j.JobType.ToString().Equals(type, StringComparison.OrdinalIgnoreCase));
        }

        return jobs.Select(MapToDto);
    }

    public async Task<BackgroundJobDto> CreateJobAsync(CreateBackgroundJobDto dto)
    {
        _logger.LogInformation("Creating job: {Type}", dto.Type);

        var job = new BackgroundJob(
            dto.Type,
            dto.Description,
            new Dictionary<string, object>()
        );

        await _backgroundJobRepository.CreateAsync(job);

        _logger.LogInformation("Job created with ID: {JobId}", job.Id);

        return MapToDto(job);
    }

    public async Task<BackgroundJobDto> UpdateJobStatusAsync(ObjectId jobId, string status, string? message = null)
    {
        _logger.LogInformation("Updating job status: {JobId} to {Status}", jobId, status);

        var job = await _backgroundJobRepository.GetByIdAsync(jobId);
        if (job == null)
        {
            throw new ArgumentException($"Job with ID {jobId} not found");
        }

        if (Enum.TryParse<JobStatus>(status, true, out var jobStatus))
        {
            job.UpdateStatus(jobStatus);
            if (!string.IsNullOrEmpty(message))
            {
                job.UpdateMessage(message);
            }
        }
        else
        {
            throw new ArgumentException($"Invalid job status: {status}");
        }

        await _backgroundJobRepository.UpdateAsync(job);

        _logger.LogInformation("Job status updated: {JobId}", jobId);

        return MapToDto(job);
    }

    public async Task<BackgroundJobDto> UpdateJobProgressAsync(ObjectId jobId, int completed, int total, string? currentItem = null)
    {
        _logger.LogInformation("Updating job progress: {JobId} - {Completed}/{Total}", jobId, completed, total);

        var job = await _backgroundJobRepository.GetByIdAsync(jobId);
        if (job == null)
        {
            throw new ArgumentException($"Job with ID {jobId} not found");
        }

        job.UpdateProgress(completed, total);
        if (!string.IsNullOrEmpty(currentItem))
        {
            job.UpdateCurrentItem(currentItem);
        }

        await _backgroundJobRepository.UpdateAsync(job);

        _logger.LogInformation("Job progress updated: {JobId}", jobId);

        return MapToDto(job);
    }

    public async Task CancelJobAsync(ObjectId jobId)
    {
        _logger.LogInformation("Cancelling job: {JobId}", jobId);

        var job = await _backgroundJobRepository.GetByIdAsync(jobId);
        if (job == null)
        {
            throw new ArgumentException($"Job with ID {jobId} not found");
        }

        job.Cancel();
        await _backgroundJobRepository.UpdateAsync(job);

        _logger.LogInformation("Job cancelled: {JobId}", jobId);
    }

    public async Task DeleteJobAsync(ObjectId jobId)
    {
        _logger.LogInformation("Deleting job: {JobId}", jobId);

        var job = await _backgroundJobRepository.GetByIdAsync(jobId);
        if (job == null)
        {
            throw new ArgumentException($"Job with ID {jobId} not found");
        }

        await _backgroundJobRepository.DeleteAsync(job);

        _logger.LogInformation("Job deleted: {JobId}", jobId);
    }

    public async Task<JobStatisticsDto> GetJobStatisticsAsync()
    {
        _logger.LogInformation("Getting job statistics");

        var jobs = await _backgroundJobRepository.GetAllAsync();
        var totalJobs = jobs.Count();
        var runningJobs = jobs.Count(j => j.Status == JobStatus.Running.ToString());
        var completedJobs = jobs.Count(j => j.Status == JobStatus.Completed.ToString());
        var failedJobs = jobs.Count(j => j.Status == JobStatus.Failed.ToString());
        var cancelledJobs = jobs.Count(j => j.Status == JobStatus.Cancelled.ToString());

        return new JobStatisticsDto
        {
            TotalJobs = totalJobs,
            RunningJobs = runningJobs,
            CompletedJobs = completedJobs,
            FailedJobs = failedJobs,
            CancelledJobs = cancelledJobs,
            SuccessRate = totalJobs > 0 ? (double)completedJobs / totalJobs * 100 : 0
        };
    }

    public async Task<BackgroundJobDto> StartCacheGenerationJobAsync(ObjectId collectionId)
    {
        _logger.LogInformation("Starting cache generation job for collection: {CollectionId}", collectionId);

        var collection = await _collectionRepository.GetByIdAsync(collectionId);
        if (collection == null)
        {
            throw new ArgumentException($"Collection with ID {collectionId} not found");
        }

        var job = new BackgroundJob(
            "cache-generation",
            $"Generate cache for collection: {collection.Name}",
            new Dictionary<string, object>
            {
                { "collectionId", collectionId },
                { "collectionName", collection.Name }
            }
        );

        await _backgroundJobRepository.CreateAsync(job);

        _logger.LogInformation("Cache generation job started: {JobId}", job.Id);

        return MapToDto(job);
    }

    public async Task<BackgroundJobDto> StartThumbnailGenerationJobAsync(ObjectId collectionId)
    {
        _logger.LogInformation("Starting thumbnail generation job for collection: {CollectionId}", collectionId);

        var collection = await _collectionRepository.GetByIdAsync(collectionId);
        if (collection == null)
        {
            throw new ArgumentException($"Collection with ID {collectionId} not found");
        }

        var job = new BackgroundJob(
            "thumbnail-generation",
            $"Generate thumbnails for collection: {collection.Name}",
            new Dictionary<string, object>
            {
                { "collectionId", collectionId },
                { "collectionName", collection.Name }
            }
        );

        await _backgroundJobRepository.CreateAsync(job);

        _logger.LogInformation("Thumbnail generation job started: {JobId}", job.Id);

        return MapToDto(job);
    }

    public async Task<BackgroundJobDto> StartBulkOperationJobAsync(BulkOperationDto dto)
    {
        _logger.LogInformation("Starting bulk operation job: {OperationType}", dto.OperationType);

        var job = new BackgroundJob(
            "bulk-operation",
            $"Bulk operation: {dto.OperationType}",
            new Dictionary<string, object>
            {
                { "operationType", dto.OperationType },
                { "targetIds", dto.TargetIds },
                { "parameters", dto.Parameters }
            }
        );

        await _backgroundJobRepository.CreateAsync(job);

        _logger.LogInformation("Bulk operation job started: {JobId}", job.Id);

        return MapToDto(job);
    }

    private static BackgroundJobDto MapToDto(BackgroundJob job)
    {
        return new BackgroundJobDto
        {
            JobId = job.Id,
            Type = job.JobType,
            Status = job.Status.ToString(),
            Progress = new JobProgressDto
            {
                Total = job.TotalItems,
                Completed = job.CompletedItems,
                Percentage = job.TotalItems > 0 ? (double)job.CompletedItems / job.TotalItems * 100 : 0,
                CurrentItem = job.CurrentItem,
                Errors = job.Errors?.ToList() ?? new List<string>()
            },
            StartedAt = job.StartedAt,
            EstimatedCompletion = job.EstimatedCompletion,
            Message = job.Message,
            Parameters = job.Parameters != null 
                ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(job.Parameters) ?? new Dictionary<string, object>()
                : new Dictionary<string, object>()
        };
    }
}
