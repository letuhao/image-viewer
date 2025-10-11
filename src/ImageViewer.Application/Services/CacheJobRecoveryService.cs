using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Events;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace ImageViewer.Application.Services;

/// <summary>
/// Implementation of cache job recovery service
/// </summary>
public class CacheJobRecoveryService : ICacheJobRecoveryService
{
    private readonly ICacheJobStateRepository _cacheJobStateRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly IMessageQueueService _messageQueueService;
    private readonly IImageProcessingSettingsService _settingsService;
    private readonly ICacheFolderRepository _cacheFolderRepository;
    private readonly ILogger<CacheJobRecoveryService> _logger;

    public CacheJobRecoveryService(
        ICacheJobStateRepository cacheJobStateRepository,
        ICollectionRepository collectionRepository,
        IMessageQueueService messageQueueService,
        IImageProcessingSettingsService settingsService,
        ICacheFolderRepository cacheFolderRepository,
        ILogger<CacheJobRecoveryService> logger)
    {
        _cacheJobStateRepository = cacheJobStateRepository;
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
            _logger.LogInformation("🔄 Starting recovery of incomplete cache jobs...");
            
            // Get all incomplete jobs
            var incompleteJobs = await _cacheJobStateRepository.GetIncompleteJobsAsync();
            var jobsList = incompleteJobs.ToList();
            
            if (!jobsList.Any())
            {
                _logger.LogInformation("✅ No incomplete jobs found to recover");
                return;
            }
            
            _logger.LogInformation("📋 Found {Count} incomplete cache jobs to recover", jobsList.Count);
            
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

    public async Task<bool> ResumeJobAsync(string jobId)
    {
        try
        {
            _logger.LogInformation("🔄 Resuming cache job {JobId}...", jobId);
            
            // Get job state
            var jobState = await _cacheJobStateRepository.GetByJobIdAsync(jobId);
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
                await _cacheJobStateRepository.UpdateStatusAsync(jobId, "Completed");
                return true;
            }
            
            _logger.LogInformation("📋 Resuming job {JobId}: {Remaining} images remaining out of {Total}", 
                jobId, unprocessedImageIds.Count, jobState.TotalImages);
            
            // Get settings
            var cacheWidth = jobState.CacheWidth;
            var cacheHeight = jobState.CacheHeight;
            var quality = jobState.Quality;
            var format = jobState.Format;
            
            // Resume job
            await _cacheJobStateRepository.UpdateStatusAsync(jobId, "Running");
            
            // Re-queue unprocessed images
            var queuedCount = 0;
            foreach (var imageId in unprocessedImageIds)
            {
                var image = collection.Images?.FirstOrDefault(img => img.Id == imageId);
                if (image == null)
                {
                    _logger.LogWarning("⚠️ Image {ImageId} not found in collection {CollectionId}, skipping", 
                        imageId, jobState.CollectionId);
                    await _cacheJobStateRepository.AtomicIncrementSkippedAsync(jobId, imageId);
                    continue;
                }
                
                // Determine cache path (same logic as original job creation)
                var cachePath = DetermineCachePath(
                    jobState.CacheFolderPath ?? string.Empty,
                    jobState.CollectionId,
                    imageId,
                    cacheWidth,
                    cacheHeight,
                    format);
                
                // Create cache generation message
                var cacheMessage = new CacheGenerationMessage
                {
                    JobId = jobId,
                    ImageId = imageId,
                    CollectionId = jobState.CollectionId,
                    ImagePath = image.GetFullPath(collection.Path),
                    CachePath = cachePath,
                    CacheWidth = cacheWidth,
                    CacheHeight = cacheHeight,
                    Quality = quality,
                    Format = format,
                    ForceRegenerate = false, // Don't force regenerate during resume
                    CreatedBySystem = $"JobRecovery_{jobId}"
                };
                
                // Queue message
                await _messageQueueService.PublishCacheGenerationMessageAsync(cacheMessage);
                queuedCount++;
            }
            
            _logger.LogInformation("✅ Resumed job {JobId}: queued {Count} images for processing", 
                jobId, queuedCount);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error resuming job {JobId}", jobId);
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetResumableJobIdsAsync()
    {
        try
        {
            var incompleteJobs = await _cacheJobStateRepository.GetIncompleteJobsAsync();
            return incompleteJobs.Select(j => j.JobId).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resumable job IDs");
            return Enumerable.Empty<string>();
        }
    }

    public async Task DisableJobResumptionAsync(string jobId, string reason)
    {
        try
        {
            _logger.LogWarning("⚠️ Disabling resumption for job {JobId}: {Reason}", jobId, reason);
            
            var jobState = await _cacheJobStateRepository.GetByJobIdAsync(jobId);
            if (jobState != null)
            {
                jobState.DisableResume();
                await _cacheJobStateRepository.UpdateAsync(jobState);
                await _cacheJobStateRepository.UpdateStatusAsync(jobId, "Failed", reason);
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
            _logger.LogInformation("🧹 Cleaning up completed cache jobs older than {Date}...", olderThan);
            
            var deletedCount = await _cacheJobStateRepository.DeleteOldCompletedJobsAsync(olderThan);
            
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
        // Determine file extension based on format
        var extension = format.ToLowerInvariant() switch
        {
            "jpeg" => ".jpg",
            "jpg" => ".jpg",
            "png" => ".png",
            "webp" => ".webp",
            _ => ".jpg"
        };
        
        // Create cache path: {CacheFolderPath}/cache/{CollectionId}/{ImageId}_cache_{Width}x{Height}.{ext}
        var cacheDir = Path.Combine(cacheFolderPath, "cache", collectionId);
        var fileName = $"{imageId}_cache_{cacheWidth}x{cacheHeight}{extension}";
        
        return Path.Combine(cacheDir, fileName);
    }
}

