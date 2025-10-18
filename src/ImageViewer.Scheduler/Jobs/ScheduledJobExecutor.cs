using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Scheduler.Services;
using MongoDB.Bson;

namespace ImageViewer.Scheduler.Jobs;

/// <summary>
/// Main scheduled job executor - delegates to specific job type handlers
/// 主定时任务执行器 - Executor chính cho công việc định kỳ
/// </summary>
public class ScheduledJobExecutor : IScheduledJobExecutor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IScheduledJobRepository _scheduledJobRepository;
    private readonly IScheduledJobRunRepository _jobRunRepository;
    private readonly ILogger<ScheduledJobExecutor> _logger;

    public ScheduledJobExecutor(
        IServiceProvider serviceProvider,
        IScheduledJobRepository scheduledJobRepository,
        IScheduledJobRunRepository jobRunRepository,
        ILogger<ScheduledJobExecutor> logger)
    {
        _serviceProvider = serviceProvider;
        _scheduledJobRepository = scheduledJobRepository;
        _jobRunRepository = jobRunRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync(ObjectId scheduledJobId, CancellationToken cancellationToken)
    {
        ScheduledJob? job = null;
        ScheduledJobRun? jobRun = null;
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Get job from database
            job = await _scheduledJobRepository.GetByIdAsync(scheduledJobId);
            if (job == null)
            {
                _logger.LogWarning("⚠️ Scheduled job {JobId} not found", scheduledJobId);
                return;
            }

            // Check if job is still enabled
            if (!job.IsEnabled)
            {
                _logger.LogInformation("ℹ️ Job {Name} is disabled, skipping execution", job.Name);
                return;
            }

            _logger.LogInformation("🚀 Executing scheduled job: {Name} ({JobType})", job.Name, job.JobType);
            
            // Create job run record
            jobRun = new ScheduledJobRun(job.Id, job.Name, job.JobType, "Scheduler");
            jobRun = await _jobRunRepository.CreateAsync(jobRun);

            // Get job type handler
            var handler = GetJobTypeHandler(job.JobType);
            if (handler == null)
            {
                throw new InvalidOperationException($"No handler registered for job type: {job.JobType}");
            }

            // Execute with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMinutes(job.TimeoutMinutes));

            Dictionary<string, object>? result = null;
            
            try
            {
                result = await handler.ExecuteAsync(job, cts.Token);
                
                // Mark as completed
                jobRun.Complete(result);
                await _jobRunRepository.UpdateAsync(jobRun);
                
                // Update job statistics
                var endTime = DateTime.UtcNow;
                await _scheduledJobRepository.RecordJobRunAsync(job.Id, startTime, endTime, "Completed");
                
                _logger.LogInformation("✅ Job {Name} completed successfully in {Duration}ms", 
                    job.Name, (endTime - startTime).TotalMilliseconds);
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                _logger.LogWarning("⏱️ Job {Name} timed out after {Timeout} minutes", 
                    job.Name, job.TimeoutMinutes);
                
                jobRun.Timeout();
                await _jobRunRepository.UpdateAsync(jobRun);
                
                await _scheduledJobRepository.RecordJobRunAsync(job.Id, startTime, DateTime.UtcNow, "Timeout", 
                    $"Job execution exceeded {job.TimeoutMinutes} minutes");
                
                throw;
            }
            
            // Calculate and update next run time
            await UpdateNextRunTimeAsync(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error executing scheduled job {JobId}: {Error}", scheduledJobId, ex.Message);
            
            if (jobRun != null)
            {
                jobRun.Fail(ex.Message);
                await _jobRunRepository.UpdateAsync(jobRun);
            }
            
            if (job != null)
            {
                await _scheduledJobRepository.RecordJobRunAsync(job.Id, startTime, DateTime.UtcNow, "Failed", ex.Message);
            }
            
            throw; // Re-throw for Hangfire retry mechanism
        }
    }

    private IScheduledJobTypeHandler? GetJobTypeHandler(string jobType)
    {
        using var scope = _serviceProvider.CreateScope();
        
        return jobType switch
        {
            "LibraryScan" => scope.ServiceProvider.GetService<ILibraryScanJobHandler>(),
            "CacheCleanup" => scope.ServiceProvider.GetService<ICacheCleanupJobHandler>(),
            "StaleJobRecovery" => scope.ServiceProvider.GetService<IStaleJobRecoveryHandler>(),
            "ThumbnailCleanup" => scope.ServiceProvider.GetService<IThumbnailCleanupJobHandler>(),
            _ => null
        };
    }

    private async Task UpdateNextRunTimeAsync(ScheduledJob job)
    {
        var nextRun = await CalculateNextRunTimeAsync(job.CronExpression, job.IntervalMinutes, job.ScheduleType);
        
        if (nextRun.HasValue)
        {
            await _scheduledJobRepository.UpdateNextRunTimeAsync(job.Id, nextRun.Value);
            _logger.LogDebug("📅 Next run time for {Name}: {NextRun}", job.Name, nextRun.Value);
        }
    }

    private async Task<DateTime?> CalculateNextRunTimeAsync(string? cronExpression, int? intervalMinutes, Domain.Enums.ScheduleType scheduleType)
    {
        // Use injected scheduler service for calculation
        using var scope = _serviceProvider.CreateScope();
        var schedulerService = scope.ServiceProvider.GetRequiredService<ISchedulerService>();
        return await schedulerService.CalculateNextRunTimeAsync(cronExpression, intervalMinutes, scheduleType);
    }
}

