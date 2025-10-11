using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Events;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System.Text.Json;

namespace ImageViewer.Application.Services;

/// <summary>
/// Implementation of file processing job recovery service
/// Handles recovery for cache, thumbnail, and other processing jobs
/// </summary>
public class FileProcessingJobRecoveryService : IFileProcessingJobRecoveryService
{
    private readonly IFileProcessingJobStateRepository _jobStateRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly IMessageQueueService _messageQueueService;
    private readonly IImageProcessingSettingsService _settingsService;
    private readonly ICacheFolderRepository _cacheFolderRepository;
    private readonly ILogger<FileProcessingJobRecoveryService> _logger;

    public FileProcessingJobRecoveryService(
        IFileProcessingJobStateRepository jobStateRepository,
        ICollectionRepository collectionRepository,
        IMessageQueueService messageQueueService,
        IImageProcessingSettingsService settingsService,
        ICacheFolderRepository cacheFolderRepository,
        ILogger<FileProcessingJobRecoveryService> logger)
    {
        _jobStateRepository = jobStateRepository;
        _collectionRepository = collectionRepository;
        _messageQueueService = messageQueueService;
        _settingsService = settingsService;
        _cacheFolderRepository = cacheFolderRepository;
        _logger = logger;
    }

    public async Task RecoverIncompleteJobsAsync()
    {
        try
        {
            _logger.LogInformation("🔄 Starting recovery of all incomplete file processing jobs...");
            
            var incompleteJobs = await _jobStateRepository.GetIncompleteJobsAsync();
            var jobsList = incompleteJobs.ToList();
            
            if (!jobsList.Any())
            {
                _logger.LogInformation("✅ No incomplete jobs found to recover");
                return;
            }
            
            _logger.LogInformation("📋 Found {Count} incomplete file processing jobs to recover", jobsList.Count);
            
            var recoveredCount = 0;
            var failedCount = 0;
            
            foreach (var job in jobsList)
            {
                try
                {
                    var resumed = await ResumeJobAsync(job.JobId);
                    if (resumed)
                    {
                        recoveredCount++;
                    }
                    else
                    {
                        failedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to recover job {JobId}", job.JobId);
                    failedCount++;
                }
            }
            
            _logger.LogInformation("✅ Job recovery complete: {Recovered} recovered, {Failed} failed", 
                recoveredCount, failedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during incomplete job recovery");
        }
    }

    public async Task RecoverIncompleteJobsByTypeAsync(string jobType)
    {
        try
        {
            _logger.LogInformation("🔄 Starting recovery of incomplete {JobType} jobs...", jobType);
            
            var incompleteJobs = await _jobStateRepository.GetIncompleteJobsByTypeAsync(jobType);
            var jobsList = incompleteJobs.ToList();
            
            if (!jobsList.Any())
            {
                _logger.LogInformation("✅ No incomplete {JobType} jobs found to recover", jobType);
                return;
            }
            
            _logger.LogInformation("📋 Found {Count} incomplete {JobType} jobs to recover", jobsList.Count, jobType);
            
            var recoveredCount = 0;
            var failedCount = 0;
            
            foreach (var job in jobsList)
            {
                try
                {
                    var resumed = await ResumeJobAsync(job.JobId);
                    if (resumed)
                    {
                        recoveredCount++;
                    }
                    else
                    {
                        failedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to recover job {JobId}", job.JobId);
                    failedCount++;
                }
            }
            
            _logger.LogInformation("✅ {JobType} job recovery complete: {Recovered} recovered, {Failed} failed", 
                jobType, recoveredCount, failedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during {JobType} job recovery", jobType);
        }
    }

    public async Task<bool> ResumeJobAsync(string jobId)
    {
        try
        {
            _logger.LogInformation("🔄 Resuming file processing job {JobId}...", jobId);
            
            var jobState = await _jobStateRepository.GetByJobIdAsync(jobId);
            if (jobState == null)
            {
                _logger.LogWarning("⚠️ Job {JobId} not found", jobId);
                return false;
            }
            
            if (!jobState.CanResume)
            {
                _logger.LogWarning("⚠️ Job {JobId} is marked as non-resumable", jobId);
                return false;
            }
            
            if (jobState.Status == "Completed")
            {
                _logger.LogInformation("✅ Job {JobId} is already completed", jobId);
                return true;
            }
            
            // Get collection
            var collectionId = ObjectId.Parse(jobState.CollectionId);
            var collection = await _collectionRepository.GetByIdAsync(collectionId);
            if (collection == null)
            {
                _logger.LogWarning("⚠️ Collection {CollectionId} not found for job {JobId}, marking as non-resumable", 
                    jobState.CollectionId, jobId);
                await DisableJobResumptionAsync(jobId, "Collection not found");
                return false;
            }
            
            // Get unprocessed images
            var allImageIds = collection.Images?.Select(img => img.Id).ToList() ?? new List<string>();
            var unprocessedImageIds = allImageIds
                .Where(imgId => !jobState.IsImageProcessed(imgId))
                .ToList();
            
            if (!unprocessedImageIds.Any())
            {
                _logger.LogInformation("✅ All images processed for job {JobId}, marking as completed", jobId);
                await _jobStateRepository.UpdateStatusAsync(jobId, "Completed");
                return true;
            }
            
            _logger.LogInformation("📋 Resuming job {JobId} ({JobType}): {Remaining} images remaining out of {Total}", 
                jobId, jobState.JobType, unprocessedImageIds.Count, jobState.TotalImages);
            
            // Resume job based on type
            await _jobStateRepository.UpdateStatusAsync(jobId, "Running");
            
            bool success = jobState.JobType.ToLowerInvariant() switch
            {
                "cache" => await ResumeCacheJobAsync(jobState, collection, unprocessedImageIds),
                "thumbnail" => await ResumeThumbnailJobAsync(jobState, collection, unprocessedImageIds),
                "both" => await ResumeBothJobAsync(jobState, collection, unprocessedImageIds),
                _ => throw new NotSupportedException($"Job type '{jobState.JobType}' is not supported for resumption")
            };
            
            if (success)
            {
                _logger.LogInformation("✅ Resumed job {JobId}: queued {Count} images for processing", 
                    jobId, unprocessedImageIds.Count);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error resuming job {JobId}", jobId);
            return false;
        }
    }

    private async Task<bool> ResumeCacheJobAsync(FileProcessingJobState jobState, Collection collection, List<string> unprocessedImageIds)
    {
        try
        {
            // Parse job settings
            var settings = JsonSerializer.Deserialize<CacheJobSettings>(jobState.JobSettings) ?? new CacheJobSettings();
            
            var queuedCount = 0;
            foreach (var imageId in unprocessedImageIds)
            {
                var image = collection.Images?.FirstOrDefault(img => img.Id == imageId);
                if (image == null)
                {
                    _logger.LogWarning("⚠️ Image {ImageId} not found in collection {CollectionId}, skipping", 
                        imageId, jobState.CollectionId);
                    await _jobStateRepository.AtomicIncrementSkippedAsync(jobState.JobId, imageId);
                    continue;
                }
                
                var cachePath = DetermineCachePath(
                    jobState.OutputFolderPath ?? string.Empty,
                    jobState.CollectionId,
                    imageId,
                    settings.Width,
                    settings.Height,
                    settings.Format);
                
                var cacheMessage = new CacheGenerationMessage
                {
                    JobId = jobState.JobId,
                    ImageId = imageId,
                    CollectionId = jobState.CollectionId,
                    ImagePath = image.GetFullPath(collection.Path),
                    CachePath = cachePath,
                    CacheWidth = settings.Width,
                    CacheHeight = settings.Height,
                    Quality = settings.Quality,
                    Format = settings.Format,
                    ForceRegenerate = false,
                    CreatedBySystem = $"JobRecovery_{jobState.JobId}"
                };
                
                await _messageQueueService.PublishCacheGenerationMessageAsync(cacheMessage);
                queuedCount++;
            }
            
            return queuedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming cache job {JobId}", jobState.JobId);
            return false;
        }
    }

    private async Task<bool> ResumeThumbnailJobAsync(FileProcessingJobState jobState, Collection collection, List<string> unprocessedImageIds)
    {
        try
        {
            // Parse job settings
            var settings = JsonSerializer.Deserialize<ThumbnailJobSettings>(jobState.JobSettings) ?? new ThumbnailJobSettings();
            
            var queuedCount = 0;
            foreach (var imageId in unprocessedImageIds)
            {
                var image = collection.Images?.FirstOrDefault(img => img.Id == imageId);
                if (image == null)
                {
                    _logger.LogWarning("⚠️ Image {ImageId} not found in collection {CollectionId}, skipping", 
                        imageId, jobState.CollectionId);
                    await _jobStateRepository.AtomicIncrementSkippedAsync(jobState.JobId, imageId);
                    continue;
                }
                
                var thumbnailMessage = new ThumbnailGenerationMessage
                {
                    JobId = jobState.JobId,
                    ImageId = imageId,
                    CollectionId = jobState.CollectionId,
                    ImagePath = image.GetFullPath(collection.Path),
                    ImageFilename = image.Filename,
                    ThumbnailWidth = settings.Width,
                    ThumbnailHeight = settings.Height
                };
                
                await _messageQueueService.PublishThumbnailGenerationMessageAsync(thumbnailMessage);
                queuedCount++;
            }
            
            return queuedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming thumbnail job {JobId}", jobState.JobId);
            return false;
        }
    }

    private async Task<bool> ResumeBothJobAsync(FileProcessingJobState jobState, Collection collection, List<string> unprocessedImageIds)
    {
        // Resume both cache and thumbnail jobs
        var cacheSuccess = await ResumeCacheJobAsync(jobState, collection, unprocessedImageIds);
        var thumbnailSuccess = await ResumeThumbnailJobAsync(jobState, collection, unprocessedImageIds);
        return cacheSuccess && thumbnailSuccess;
    }

    public async Task<IEnumerable<string>> GetResumableJobIdsAsync()
    {
        try
        {
            var incompleteJobs = await _jobStateRepository.GetIncompleteJobsAsync();
            return incompleteJobs.Select(j => j.JobId).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resumable job IDs");
            return Enumerable.Empty<string>();
        }
    }

    public async Task<IEnumerable<string>> GetResumableJobIdsByTypeAsync(string jobType)
    {
        try
        {
            var incompleteJobs = await _jobStateRepository.GetIncompleteJobsByTypeAsync(jobType);
            return incompleteJobs.Select(j => j.JobId).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resumable job IDs for type {JobType}", jobType);
            return Enumerable.Empty<string>();
        }
    }

    public async Task DisableJobResumptionAsync(string jobId, string reason)
    {
        try
        {
            _logger.LogWarning("⚠️ Disabling resumption for job {JobId}: {Reason}", jobId, reason);
            
            var jobState = await _jobStateRepository.GetByJobIdAsync(jobId);
            if (jobState != null)
            {
                jobState.DisableResume();
                await _jobStateRepository.UpdateAsync(jobState);
                await _jobStateRepository.UpdateStatusAsync(jobId, "Failed", reason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling job resumption for {JobId}", jobId);
        }
    }

    public async Task<int> CleanupOldCompletedJobsAsync(int olderThanDays = 30)
    {
        try
        {
            var olderThan = DateTime.UtcNow.AddDays(-olderThanDays);
            _logger.LogInformation("🧹 Cleaning up completed file processing jobs older than {Date}...", olderThan);
            
            var deletedCount = await _jobStateRepository.DeleteOldCompletedJobsAsync(olderThan);
            
            _logger.LogInformation("✅ Cleaned up {Count} old completed jobs", deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old completed jobs");
            return 0;
        }
    }

    private string DetermineCachePath(
        string cacheFolderPath,
        string collectionId,
        string imageId,
        int cacheWidth,
        int cacheHeight,
        string format)
    {
        var extension = format.ToLowerInvariant() switch
        {
            "jpeg" => ".jpg",
            "jpg" => ".jpg",
            "png" => ".png",
            "webp" => ".webp",
            _ => ".jpg"
        };
        
        var cacheDir = Path.Combine(cacheFolderPath, "cache", collectionId);
        var fileName = $"{imageId}_cache_{cacheWidth}x{cacheHeight}{extension}";
        
        return Path.Combine(cacheDir, fileName);
    }
}

// Job settings classes for JSON deserialization
public class CacheJobSettings
{
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int Quality { get; set; } = 85;
    public string Format { get; set; } = "jpeg";
}

public class ThumbnailJobSettings
{
    public int Width { get; set; } = 300;
    public int Height { get; set; } = 300;
    public int Quality { get; set; } = 90;
    public string Format { get; set; } = "jpeg";
}

