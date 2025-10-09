using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ImageViewer.Worker.Services;

/// <summary>
/// Centralized service to monitor ALL collection-scan jobs
/// Replaces the distributed Task.Run approach with a single reliable monitor
/// </summary>
public class JobMonitoringService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<JobMonitoringService> _logger;
    private const int CheckIntervalSeconds = 5;

    public JobMonitoringService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<JobMonitoringService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 JobMonitoringService started - monitoring all collection-scan jobs");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(CheckIntervalSeconds), stoppingToken);
                await MonitorAllPendingJobsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("JobMonitoringService shutting down");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobMonitoringService monitoring loop");
                // Continue monitoring even if one iteration fails
            }
        }
    }

    private async Task MonitorAllPendingJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
        var mongoDatabase = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        
        // Query all Pending collection-scan jobs that have a CollectionId
        var jobsCollection = mongoDatabase.GetCollection<Domain.Entities.BackgroundJob>("background_jobs");
        var collectionsCollection = mongoDatabase.GetCollection<Domain.Entities.Collection>("collections");
        
        var filter = MongoDB.Driver.Builders<Domain.Entities.BackgroundJob>.Filter.And(
            MongoDB.Driver.Builders<Domain.Entities.BackgroundJob>.Filter.Eq(j => j.JobType, "collection-scan"),
            MongoDB.Driver.Builders<Domain.Entities.BackgroundJob>.Filter.Eq(j => j.Status, "Pending"),
            MongoDB.Driver.Builders<Domain.Entities.BackgroundJob>.Filter.Ne(j => j.CollectionId, null)
        );
        
        var pendingJobs = await jobsCollection.Find(filter).ToListAsync(cancellationToken);
        
        if (pendingJobs.Count == 0)
        {
            return; // No pending jobs to monitor
        }
        
        _logger.LogDebug("📊 Monitoring {Count} pending collection-scan jobs", pendingJobs.Count);
        
        foreach (var job in pendingJobs)
        {
            try
            {
                if (!job.CollectionId.HasValue)
                    continue;
                
                // Query collection directly from MongoDB
                var collection = await collectionsCollection
                    .Find(c => c.Id == job.CollectionId.Value)
                    .FirstOrDefaultAsync(cancellationToken);
                
                if (collection == null)
                {
                    _logger.LogWarning("Collection {CollectionId} not found for job {JobId}", job.CollectionId, job.Id);
                    continue;
                }
                
                // Get expected count from scan stage
                int expectedCount = 0;
                if (job.Stages != null && job.Stages.ContainsKey("scan"))
                {
                    expectedCount = job.Stages["scan"].TotalItems;
                }
                
                if (expectedCount == 0)
                {
                    _logger.LogWarning("Job {JobId} has no expected count in scan stage", job.Id);
                    continue;
                }
                
                // Count actual items
                int thumbnailCount = collection.Thumbnails?.Count ?? 0;
                int cacheCount = collection.CacheImages?.Count ?? 0;
                
                _logger.LogDebug("Job {JobId} [{Name}]: Thumbnails={T}/{E}, Cache={C}/{E}", 
                    job.Id, collection.Name, thumbnailCount, expectedCount, cacheCount, expectedCount);
                
                // Update thumbnail stage
                bool thumbnailChanged = await UpdateStageIfNeededAsync(
                    backgroundJobService, 
                    job, 
                    "thumbnail", 
                    thumbnailCount, 
                    expectedCount,
                    "thumbnails");
                
                // Update cache stage
                bool cacheChanged = await UpdateStageIfNeededAsync(
                    backgroundJobService, 
                    job, 
                    "cache", 
                    cacheCount, 
                    expectedCount,
                    "cache files");
                
                if (thumbnailChanged || cacheChanged)
                {
                    _logger.LogInformation("✅ Updated job {JobId}: Thumbnails {T}/{E}, Cache {C}/{E}", 
                        job.Id, thumbnailCount, expectedCount, cacheCount, expectedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring job {JobId}", job.Id);
            }
        }
    }

    private async Task<bool> UpdateStageIfNeededAsync(
        IBackgroundJobService backgroundJobService,
        Domain.Entities.BackgroundJob job,
        string stageName,
        int currentCount,
        int expectedCount,
        string itemName)
    {
        if (job.Stages == null || !job.Stages.ContainsKey(stageName))
            return false;
        
        var stage = job.Stages[stageName];
        bool isComplete = currentCount >= expectedCount;
        bool countChanged = currentCount != stage.CompletedItems;
        
        // Update if count changed OR if complete but not marked as such
        if (countChanged || (isComplete && stage.Status != "Completed"))
        {
            if (isComplete)
            {
                await backgroundJobService.UpdateJobStageAsync(
                    job.Id, 
                    stageName, 
                    "Completed", 
                    currentCount, 
                    expectedCount, 
                    $"All {expectedCount} {itemName} generated");
            }
            else if (currentCount > 0)
            {
                await backgroundJobService.UpdateJobStageAsync(
                    job.Id, 
                    stageName, 
                    "InProgress", 
                    currentCount, 
                    expectedCount, 
                    $"Generated {currentCount}/{expectedCount} {itemName}");
            }
            
            return true;
        }
        
        return false;
    }
}

