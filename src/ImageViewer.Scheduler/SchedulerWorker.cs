using ImageViewer.Domain.Interfaces;
using ImageViewer.Scheduler.Configuration;
using ImageViewer.Scheduler.Services;
using Microsoft.Extensions.Options;

namespace ImageViewer.Scheduler;

/// <summary>
/// Background worker that manages Hangfire scheduler lifecycle
/// </summary>
public class SchedulerWorker : BackgroundService
{
    private readonly ILogger<SchedulerWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly HangfireOptions _hangfireOptions;

    public SchedulerWorker(
        ILogger<SchedulerWorker> logger,
        IServiceProvider serviceProvider,
        IOptions<HangfireOptions> hangfireOptions)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _hangfireOptions = hangfireOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduler Worker starting at: {time}", DateTimeOffset.Now);

        try
        {
            // Load existing scheduled jobs from database and register them with Hangfire
            await SynchronizeScheduledJobsAsync(stoppingToken);

            // Periodically synchronize jobs with database
            var syncInterval = TimeSpan.FromMinutes(_hangfireOptions.JobSynchronizationInterval);
            _logger.LogInformation(
                "Job synchronization enabled. Interval: {interval} minutes. New/updated/deleted jobs will be detected automatically.",
                _hangfireOptions.JobSynchronizationInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(syncInterval, stoppingToken);
                    
                    _logger.LogDebug("Checking for job updates...");
                    await SynchronizeScheduledJobsAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during job synchronization, will retry in {interval}", syncInterval);
                    // Don't throw - keep the worker running
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Scheduler Worker is stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Scheduler Worker");
            throw;
        }
    }

    private async Task SynchronizeScheduledJobsAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Synchronizing scheduled jobs from database...");

        using var scope = _serviceProvider.CreateScope();
        var scheduledJobRepository = scope.ServiceProvider.GetRequiredService<IScheduledJobRepository>();
        var schedulerService = scope.ServiceProvider.GetRequiredService<ISchedulerService>();

        try
        {
            // Get all scheduled jobs from database
            var allJobs = await scheduledJobRepository.GetAllAsync();
            var dbJobs = allJobs.Where(j => !j.IsDeleted).ToList();
            
            // Get currently registered jobs from Hangfire
            var activeJobs = (await schedulerService.GetActiveScheduledJobsAsync()).ToList();
            var activeJobIds = activeJobs.Select(j => j.Id).ToHashSet();

            int registered = 0;
            int updated = 0;
            int removed = 0;
            int disabled = 0;

            // Process each job from database
            foreach (var job in dbJobs)
            {
                try
                {
                    var isRegistered = activeJobIds.Contains(job.Id);

                    if (job.IsEnabled)
                    {
                        if (!isRegistered)
                        {
                            // NEW JOB: Register with Hangfire
                            await schedulerService.EnableJobAsync(job.Id);
                            registered++;
                            
                            _logger.LogInformation(
                                "✅ Registered new job: {jobName} ({jobType}) with schedule: {cron}",
                                job.Name,
                                job.JobType,
                                job.CronExpression);
                        }
                        else
                        {
                            // EXISTING JOB: Check if needs update (cron expression changed)
                            var existingJob = activeJobs.First(j => j.Id == job.Id);
                            if (existingJob.CronExpression != job.CronExpression ||
                                existingJob.IntervalMinutes != job.IntervalMinutes)
                            {
                                // Re-register to update schedule
                                await schedulerService.DisableJobAsync(job.Id);
                                await schedulerService.EnableJobAsync(job.Id);
                                updated++;
                                
                                _logger.LogInformation(
                                    "🔄 Updated job schedule: {jobName} to {cron}",
                                    job.Name,
                                    job.CronExpression);
                            }
                        }
                    }
                    else
                    {
                        // JOB IS DISABLED in database
                        if (isRegistered)
                        {
                            // Remove from Hangfire
                            await schedulerService.DisableJobAsync(job.Id);
                            disabled++;
                            
                            _logger.LogInformation(
                                "⏸️ Disabled job: {jobName}",
                                job.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Failed to synchronize job: {jobName} ({jobId})", 
                        job.Name, 
                        job.Id);
                }
            }

            // Check for deleted jobs (exist in Hangfire but not in database)
            var dbJobIds = dbJobs.Select(j => j.Id).ToHashSet();
            foreach (var activeJob in activeJobs)
            {
                if (!dbJobIds.Contains(activeJob.Id))
                {
                    try
                    {
                        await schedulerService.DisableJobAsync(activeJob.Id);
                        removed++;
                        
                        _logger.LogInformation(
                            "🗑️ Removed deleted job: {jobName}",
                            activeJob.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, 
                            "Failed to remove job: {jobName} ({jobId})", 
                            activeJob.Name, 
                            activeJob.Id);
                    }
                }
            }

            _logger.LogInformation(
                "Job synchronization complete. Registered: {registered}, Updated: {updated}, Disabled: {disabled}, Removed: {removed}",
                registered,
                updated,
                disabled,
                removed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to synchronize scheduled jobs from database");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduler Worker is stopping at: {time}", DateTimeOffset.Now);
        await base.StopAsync(stoppingToken);
    }
}

