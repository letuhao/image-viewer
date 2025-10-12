using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ImageViewer.Worker.Services;

/// <summary>
/// Fallback monitoring service for stuck/failed collection-scan jobs
/// Primary tracking is done by consumers (ThumbnailGenerationConsumer, CacheGenerationConsumer)
/// This service detects and reconciles jobs that are stuck or have lost tracking
/// </summary>
public class JobMonitoringService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<JobMonitoringService> _logger;
    private const int CheckIntervalSeconds = 5; // Check every 5 seconds for status transitions
    private const int StuckThresholdMinutes = 2; // Jobs not updated in 2 minutes need reconciliation
    private const int BatchSize = 500; // Process max 500 jobs per cycle

    public JobMonitoringService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<JobMonitoringService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 JobMonitoringService started - handles status transitions and reconciles stuck jobs (every {Interval}s)", CheckIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(CheckIntervalSeconds), stoppingToken);
                await DetectAndReconcileStuckJobsAsync(stoppingToken);
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

    private async Task DetectAndReconcileStuckJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
        var mongoDatabase = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        
        var jobsCollection = mongoDatabase.GetCollection<Domain.Entities.BackgroundJob>("background_jobs");
        var collectionsCollection = mongoDatabase.GetCollection<Domain.Entities.Collection>("collections");
        
        // Query ALL pending/in-progress collection-scan jobs with CollectionId
        // This handles BOTH: stuck jobs AND jobs that need status transitions
        var filter = MongoDB.Driver.Builders<Domain.Entities.BackgroundJob>.Filter.And(
            MongoDB.Driver.Builders<Domain.Entities.BackgroundJob>.Filter.Eq(j => j.JobType, "collection-scan"),
            MongoDB.Driver.Builders<Domain.Entities.BackgroundJob>.Filter.In(j => j.Status, new[] { "Pending", "InProgress" }),
            MongoDB.Driver.Builders<Domain.Entities.BackgroundJob>.Filter.Ne(j => j.CollectionId, null)
        );
        
        var pendingJobs = await jobsCollection
            .Find(filter)
            .Sort(MongoDB.Driver.Builders<Domain.Entities.BackgroundJob>.Sort.Ascending(j => j.CreatedAt))
            .Limit(BatchSize)
            .ToListAsync(cancellationToken);
        
        if (pendingJobs.Count == 0)
        {
            return; // No pending jobs
        }
        
        _logger.LogDebug("📊 Monitoring {Count} pending collection-scan jobs for status transitions", 
            pendingJobs.Count);
        
        // BATCH query all collections at once (performance optimization)
        var collectionIds = pendingJobs
            .Where(j => j.CollectionId.HasValue)
            .Select(j => j.CollectionId.Value)
            .ToList();
        
        var collections = await collectionsCollection
            .Find(c => collectionIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
        
        var collectionDict = collections.ToDictionary(c => c.Id);
        
        foreach (var job in pendingJobs)
        {
            try
            {
                if (!job.CollectionId.HasValue || 
                    !collectionDict.TryGetValue(job.CollectionId.Value, out var collection))
                {
                    _logger.LogWarning("Collection {CollectionId} not found for stuck job {JobId}", job.CollectionId, job.Id);
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
                    _logger.LogDebug("Job {JobId} has no expected count in scan stage", job.Id);
                    continue;
                }
                
                // Count actual items
                int thumbnailCount = collection.Thumbnails?.Count ?? 0;
                int cacheCount = collection.CacheImages?.Count ?? 0;
                
                _logger.LogDebug("🔍 Checking job {JobId} [{Name}]: Thumbnails={T}/{E}, Cache={C}/{E}", 
                    job.Id, collection.Name, thumbnailCount, expectedCount, cacheCount, expectedCount);
                
                // Reconcile thumbnail stage (force update to actual count)
                bool thumbnailChanged = await UpdateStageIfNeededAsync(
                    backgroundJobService, 
                    job, 
                    "thumbnail", 
                    thumbnailCount, 
                    expectedCount,
                    "thumbnails");
                
                // Reconcile cache stage (force update to actual count)
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

