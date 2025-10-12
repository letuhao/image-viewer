using ImageViewer.Application.DTOs.BackgroundJobs;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Events;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System.Text.Json;

namespace ImageViewer.Application.Services;

/// <summary>
/// Background job service implementation
/// </summary>
public class BackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobRepository _backgroundJobRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly IBulkService _bulkService;
    private readonly IMessageQueueService _messageQueueService; // Added IMessageQueueService
    private readonly ILogger<BackgroundJobService> _logger;

    public BackgroundJobService(
        IBackgroundJobRepository backgroundJobRepository,
        ICollectionRepository collectionRepository,
        IBulkService bulkService,
        IMessageQueueService messageQueueService, // Added IMessageQueueService to constructor
        ILogger<BackgroundJobService> logger)
    {
        _backgroundJobRepository = backgroundJobRepository;
        _collectionRepository = collectionRepository;
        _bulkService = bulkService;
        _messageQueueService = messageQueueService;
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
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        _logger.LogInformation("Creating job: {Type}", dto.Type);

        // Check if this is a multi-stage job
        bool isMultiStage = dto.Type == "collection-scan";
        
        var job = new BackgroundJob(
            dto.Type,
            dto.Description,
            new Dictionary<string, object>(),
            isMultiStage
        );

        // For collection-scan jobs, initialize stages and set CollectionId
        if (isMultiStage)
        {
            job.AddStage("scan");
            job.AddStage("thumbnail");
            job.AddStage("cache");
            
            if (dto.CollectionId.HasValue)
            {
                job.SetCollectionId(dto.CollectionId.Value);
            }
        }

        await _backgroundJobRepository.CreateAsync(job);

        _logger.LogInformation("Job created with ID: {JobId} (Multi-stage: {IsMultiStage})", job.Id, isMultiStage);

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

        await _backgroundJobRepository.DeleteAsync(job.Id);

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
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

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

        // Send message to RabbitMQ for background processing
        var bulkMessage = new BulkOperationMessage
        {
            OperationType = dto.OperationType,
            CollectionIds = dto.TargetIds?.Select(id => Guid.Parse(id.ToString())).ToList() ?? new List<Guid>(),
            Parameters = dto.Parameters,
            UserId = null, // TODO: Get from current user context
            JobId = job.Id.ToString(), // Link to background job for tracking (convert ObjectId to string)
            Priority = 0, // Default priority
            MaxRetries = 3,
            Timeout = TimeSpan.FromHours(2) // 2 hour timeout for bulk operations
        };

        await _messageQueueService.PublishAsync(bulkMessage, "bulk.operation");

        _logger.LogInformation("Bulk operation message sent to RabbitMQ for job: {JobId}", job.Id);

        return MapToDto(job);
    }

    public async Task IncrementJobStageProgressAsync(ObjectId jobId, string stageName, int incrementBy = 1)
    {
        try
        {
            // Use atomic increment to prevent lost updates from concurrent consumers
            var success = await _backgroundJobRepository.AtomicIncrementStageAsync(jobId, stageName, incrementBy);
            
            if (!success)
            {
                _logger.LogWarning("Failed to atomically increment stage {StageName} for job {JobId}", stageName, jobId);
            }
            
            // NOTE: Status transitions (Pending → InProgress → Completed) are handled by:
            // 1. First increment: Fallback monitor detects completedItems > 0 and sets to InProgress
            // 2. Final increment: Fallback monitor detects completedItems >= totalItems and sets to Completed
            // This separates concerns: consumers update counts, monitor manages states
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to increment job stage progress for {JobId}.{StageName}", jobId, stageName);
            // Don't throw - this is best-effort, fallback monitor will reconcile
        }
    }
    
    public async Task UpdateJobStageAsync(ObjectId jobId, string stageName, string status, int completed = 0, int total = 0, string? message = null)
    {
        var job = await _backgroundJobRepository.GetByIdAsync(jobId);
        if (job == null)
        {
            throw new ArgumentException($"Job with ID {jobId} not found");
        }

        _logger.LogDebug("Updating job {JobId} stage '{StageName}' to {Status}", jobId, stageName, status);

        switch (status.ToLower())
        {
            case "inprogress":
            case "running":
                job.StartStage(stageName, total, message);
                break;
            case "completed":
                // Update progress before completing (to set final counts)
                if (completed > 0 || total > 0)
                {
                    job.UpdateStageProgress(stageName, completed, total, message);
                }
                job.CompleteStage(stageName, message);
                break;
            case "failed":
                job.FailStage(stageName, message ?? "Stage failed");
                break;
            default:
                if (completed > 0 || total > 0)
                {
                    job.UpdateStageProgress(stageName, completed, total, message);
                }
                break;
        }

        await _backgroundJobRepository.UpdateAsync(job);
    }

    private static BackgroundJobDto MapToDto(BackgroundJob job)
    {
        var now = DateTime.UtcNow;
        var duration = job.StartedAt.HasValue ? now - job.StartedAt.Value : (TimeSpan?)null;
        var estimatedTimeRemaining = CalculateEstimatedTimeRemaining(job, now);
        
        return new BackgroundJobDto
        {
            JobId = job.Id,
            Type = job.JobType,
            Status = job.Status.ToString(),
            Progress = new JobProgressDto
            {
                Total = job.TotalItems,
                Completed = job.CompletedItems,
                Failed = job.Errors?.Count ?? 0,
                Skipped = 0, // TODO: Add skipped count to BackgroundJob entity
                Pending = Math.Max(0, job.TotalItems - job.CompletedItems - (job.Errors?.Count ?? 0)),
                Percentage = job.TotalItems > 0 ? (double)job.CompletedItems / job.TotalItems * 100 : 0,
                CurrentItem = job.CurrentItem,
                CurrentStep = GetCurrentStep(job),
                Errors = job.Errors?.ToList() ?? new List<string>(),
                Warnings = new List<string>(), // TODO: Add warnings to BackgroundJob entity
                ItemCounts = new Dictionary<string, int>
                {
                    ["Total"] = job.TotalItems,
                    ["Completed"] = job.CompletedItems,
                    ["Failed"] = job.Errors?.Count ?? 0
                },
                ItemsPerSecond = CalculateItemsPerSecond(job, duration),
                EstimatedTimeRemaining = estimatedTimeRemaining
            },
            Timing = new JobTimingDto
            {
                CreatedAt = job.CreatedAt,
                StartedAt = job.StartedAt,
                CompletedAt = job.Status.ToString() == "Completed" ? now : null,
                Duration = duration,
                EstimatedDuration = job.EstimatedCompletion.HasValue && job.StartedAt.HasValue 
                    ? job.EstimatedCompletion.Value - job.StartedAt.Value 
                    : (TimeSpan?)null,
                EstimatedTimeRemaining = estimatedTimeRemaining,
                AverageStepDuration = CalculateAverageStepDuration(job),
                StepDurations = new Dictionary<string, TimeSpan>() // TODO: Add step tracking
            },
            Metrics = new JobMetricsDto
            {
                MemoryUsageBytes = GC.GetTotalMemory(false),
                CpuUsagePercent = 0, // TODO: Add CPU monitoring
                DiskReadBytes = 0, // TODO: Add disk monitoring
                DiskWriteBytes = 0, // TODO: Add disk monitoring
                NetworkRequests = 0, // TODO: Add network monitoring
                ItemsPerSecond = CalculateItemsPerSecond(job, duration),
                BytesPerSecond = 0, // TODO: Add byte rate monitoring
                RetryCount = 0, // TODO: Add retry tracking
                TimeoutCount = 0, // TODO: Add timeout tracking
                CustomMetrics = new Dictionary<string, double>()
            },
            Health = new JobHealthDto
            {
                Status = GetHealthStatus(job),
                ErrorCount = job.Errors?.Count ?? 0,
                WarningCount = 0, // TODO: Add warning tracking
                LastHeartbeat = now,
                IsStuck = IsJobStuck(job, now),
                IsTimedOut = IsJobTimedOut(job, now),
                HealthIssues = GetHealthIssues(job),
                HealthChecks = new Dictionary<string, object>()
            },
            StartedAt = job.StartedAt,
            EstimatedCompletion = job.EstimatedCompletion,
            Message = job.Message,
            Parameters = job.Parameters != null 
                ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(job.Parameters) ?? new Dictionary<string, object>()
                : new Dictionary<string, object>(),
            Steps = new List<JobStepDto>(), // TODO: Add step tracking
            Dependencies = new JobDependenciesDto
            {
                DependsOn = new List<ObjectId>(),
                Blocks = new List<ObjectId>(),
                ChildJobs = new List<ObjectId>(),
                ParentJob = null,
                DependencyLevel = 0,
                CanStart = true,
                BlockingReasons = new List<string>()
            }
        };
    }

    private static TimeSpan CalculateEstimatedTimeRemaining(BackgroundJob job, DateTime now)
    {
        if (!job.StartedAt.HasValue || job.TotalItems <= 0 || job.CompletedItems <= 0)
            return TimeSpan.Zero;

        var elapsed = now - job.StartedAt.Value;
        var itemsPerSecond = job.CompletedItems / elapsed.TotalSeconds;
        var remainingItems = job.TotalItems - job.CompletedItems;
        
        return TimeSpan.FromSeconds(remainingItems / itemsPerSecond);
    }

    private static double CalculateItemsPerSecond(BackgroundJob job, TimeSpan? duration)
    {
        if (!duration.HasValue || duration.Value.TotalSeconds <= 0 || job.CompletedItems <= 0)
            return 0;

        return job.CompletedItems / duration.Value.TotalSeconds;
    }

    private static double CalculateAverageStepDuration(BackgroundJob job)
    {
        // TODO: Implement step duration calculation
        return 0;
    }

    private static string GetCurrentStep(BackgroundJob job)
    {
        // TODO: Implement current step tracking
        return "Processing";
    }

    private static string GetHealthStatus(BackgroundJob job)
    {
        if (job.Status.ToString() == "Failed")
            return "Failed";
        
        if (job.Errors?.Count > 0)
            return "Warning";
        
        return "Healthy";
    }

    private static bool IsJobStuck(BackgroundJob job, DateTime now)
    {
        if (!job.StartedAt.HasValue || job.Status.ToString() != "Running")
            return false;

        // Consider job stuck if it's been running for more than 1 hour without progress
        var runningTime = now - job.StartedAt.Value;
        return runningTime > TimeSpan.FromHours(1);
    }

    private static bool IsJobTimedOut(BackgroundJob job, DateTime now)
    {
        if (!job.StartedAt.HasValue)
            return false;

        // Consider job timed out if it's been running for more than 2 hours
        var runningTime = now - job.StartedAt.Value;
        return runningTime > TimeSpan.FromHours(2);
    }

    private static List<string> GetHealthIssues(BackgroundJob job)
    {
        var issues = new List<string>();
        
        if (job.Errors?.Count > 0)
            issues.Add($"Has {job.Errors.Count} errors");
        
        if (IsJobStuck(job, DateTime.UtcNow))
            issues.Add("Job appears to be stuck");
        
        if (IsJobTimedOut(job, DateTime.UtcNow))
            issues.Add("Job has timed out");
        
        return issues;
    }

    public async Task UpdateJobErrorStatisticsAsync(ObjectId jobId, int successCount, int errorCount, Dictionary<string, int>? errorSummary = null)
    {
        _logger.LogInformation("Updating job error statistics: {JobId} - {SuccessCount} success, {ErrorCount} errors", 
            jobId, successCount, errorCount);

        var job = await _backgroundJobRepository.GetByIdAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Job not found for error statistics update: {JobId}", jobId);
            return;
        }

        job.UpdateErrorStatistics(successCount, errorCount, errorSummary);
        await _backgroundJobRepository.UpdateAsync(job);

        _logger.LogInformation("Job error statistics updated: {JobId} - {ErrorSummary}", 
            jobId, job.GetErrorSummaryString());
    }
}
