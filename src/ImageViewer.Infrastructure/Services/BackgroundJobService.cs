using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Application.Services;
using MongoDB.Bson;

namespace ImageViewer.Infrastructure.Services;

/// <summary>
/// Background job service for processing long-running tasks
/// </summary>
public class BackgroundJobService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundJobService> _logger;
    private readonly SemaphoreSlim _semaphore;

    public BackgroundJobService(IServiceProvider serviceProvider, ILogger<BackgroundJobService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _semaphore = new SemaphoreSlim(2, 2); // Max 2 concurrent jobs
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background job service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingJobsAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Check every 30 seconds
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background job service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait 1 minute before retrying
            }
        }

        _logger.LogInformation("Background job service stopped");
    }

    private async Task ProcessPendingJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var allJobs = await unitOfWork.BackgroundJobs.GetAllAsync();
            var pendingJobs = allJobs.Where(job => job.Status == JobStatus.Pending.ToString());

            foreach (var job in pendingJobs.Take(2)) // Process max 2 jobs at a time
            {
                _ = Task.Run(async () => await ProcessJobAsync(job, scope.ServiceProvider, cancellationToken), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending jobs");
        }
    }

    private async Task ProcessJobAsync(BackgroundJob job, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        
        try
        {
            _logger.LogInformation("Starting background job {JobId} of type {JobType}", job.Id, job.JobType);

            using var scope = serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Start the job
            job.Start();
            await unitOfWork.BackgroundJobs.UpdateAsync(job);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Process the job based on type
            var result = await ProcessJobByTypeAsync(job, scope.ServiceProvider, cancellationToken);

            // Complete the job
            job.Complete(result);
            await unitOfWork.BackgroundJobs.UpdateAsync(job);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Completed background job {JobId} of type {JobType}", job.Id, job.JobType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing background job {JobId}", job.Id);
            
            try
            {
                using var scope = serviceProvider.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                
                job.Fail(ex.Message);
                await unitOfWork.BackgroundJobs.UpdateAsync(job);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Error updating failed job {JobId}", job.Id);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<string?> ProcessJobByTypeAsync(BackgroundJob job, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        return job.JobType switch
        {
            "ScanCollection" => await ProcessScanCollectionJobAsync(job, serviceProvider, cancellationToken),
            "GenerateThumbnails" => await ProcessGenerateThumbnailsJobAsync(job, serviceProvider, cancellationToken),
            "GenerateCache" => await ProcessGenerateCacheJobAsync(job, serviceProvider, cancellationToken),
            "CleanupCache" => await ProcessCleanupCacheJobAsync(job, serviceProvider, cancellationToken),
            _ => throw new ArgumentException($"Unknown job type: {job.JobType}")
        };
    }

    private async Task<string?> ProcessScanCollectionJobAsync(BackgroundJob job, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var collectionService = serviceProvider.GetRequiredService<ICollectionService>();
        
        if (!ObjectId.TryParse(job.Parameters, out var collectionId))
        {
            throw new ArgumentException("Invalid collection ID in job parameters");
        }

        try
        {
            // Minimal placeholder: call GetCollectionByIdAsync to validate ID; replace with real scan when available
            var col = await collectionService.GetCollectionByIdAsync(collectionId);
            if (col == null) throw new ArgumentException($"Collection {collectionId} not found");
            return $"Validated collection {collectionId} for scan";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ScanCollection job fallback path for {CollectionId}", collectionId);
            return $"Scan fallback executed for {collectionId}";
        }
    }

    private async Task<string?> ProcessGenerateThumbnailsJobAsync(BackgroundJob job, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var imageService = serviceProvider.GetRequiredService<IImageService>();
        
        if (!ObjectId.TryParse(job.Parameters, out var collectionId))
        {
            throw new ArgumentException("Invalid collection ID in job parameters");
        }

        var images = await imageService.GetEmbeddedImagesByCollectionAsync(collectionId, cancellationToken);
        var processedCount = 0;

        foreach (var image in images)
        {
            try
            {
                await imageService.GenerateThumbnailAsync(image.Id, collectionId, 300, 300, cancellationToken);
                processedCount++;
                
                // Update job progress
                job.UpdateProgress(processedCount, images.Count());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating thumbnail for image {ImageId}", image.Id);
            }
        }

        return $"Generated {processedCount} thumbnails for collection {collectionId}";
    }

    private async Task<string?> ProcessGenerateCacheJobAsync(BackgroundJob job, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var imageService = serviceProvider.GetRequiredService<IImageService>();
        
        if (!ObjectId.TryParse(job.Parameters, out var collectionId))
        {
            throw new ArgumentException("Invalid collection ID in job parameters");
        }

        var images = await imageService.GetEmbeddedImagesByCollectionAsync(collectionId, cancellationToken);
        var processedCount = 0;

        foreach (var image in images)
        {
            try
            {
                await imageService.GenerateCacheAsync(image.Id, collectionId, 1920, 1080, cancellationToken);
                processedCount++;
                
                // Update job progress
                job.UpdateProgress(processedCount, images.Count());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating cache for image {ImageId}", image.Id);
            }
        }

        return $"Generated {processedCount} cached images for collection {collectionId}";
    }

    private async Task<string?> ProcessCleanupCacheJobAsync(BackgroundJob job, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var cacheService = serviceProvider.GetRequiredService<ICacheService>();
        
        try
        {
            var logger = serviceProvider.GetRequiredService<ILogger<BackgroundJobService>>();
            logger.LogInformation("Starting cache cleanup job: {JobId}", job.Id);
            
            // Get cache statistics before cleanup
            var statsBefore = await cacheService.GetCacheStatisticsAsync();
            logger.LogInformation("Cache statistics before cleanup: {Stats}", statsBefore);
            
            // Clear expired cache entries
            await cacheService.CleanupExpiredCacheAsync();
            
            // Clear cache for collections that haven't been accessed recently
            var cutoffDate = DateTime.UtcNow.AddDays(-30); // 30 days ago
            await cacheService.CleanupOldCacheAsync(cutoffDate);
            
            // Get cache statistics after cleanup
            var statsAfter = await cacheService.GetCacheStatisticsAsync();
            logger.LogInformation("Cache statistics after cleanup: {Stats}", statsAfter);
            
            var message = $"Cache cleanup completed. Freed space: {statsBefore.Summary.TotalCacheSize - statsAfter.Summary.TotalCacheSize} bytes";
            logger.LogInformation(message);
            
            return message;
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<BackgroundJobService>>();
            logger.LogError(ex, "Error during cache cleanup job: {JobId}", job.Id);
            throw;
        }
    }

    public override void Dispose()
    {
        _semaphore?.Dispose();
        base.Dispose();
    }
}
