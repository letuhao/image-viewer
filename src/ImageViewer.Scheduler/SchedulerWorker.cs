using Hangfire;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Scheduler.Services;

namespace ImageViewer.Scheduler;

/// <summary>
/// Background worker that manages Hangfire scheduler lifecycle
/// </summary>
public class SchedulerWorker : BackgroundService
{
    private readonly ILogger<SchedulerWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public SchedulerWorker(
        ILogger<SchedulerWorker> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduler Worker starting at: {time}", DateTimeOffset.Now);

        try
        {
            // Load existing scheduled jobs from database and register them with Hangfire
            await LoadScheduledJobsAsync(stoppingToken);

            // Keep the worker running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
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

    private async Task LoadScheduledJobsAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Loading scheduled jobs from database...");

        using var scope = _serviceProvider.CreateScope();
        var scheduledJobRepository = scope.ServiceProvider.GetRequiredService<IScheduledJobRepository>();
        var schedulerService = scope.ServiceProvider.GetRequiredService<ISchedulerService>();

        try
        {
            // Get all active scheduled jobs
            var scheduledJobs = await scheduledJobRepository.GetAllAsync();
            var activeJobs = scheduledJobs.Where(j => j.IsEnabled).ToList();

            _logger.LogInformation("Found {count} active scheduled jobs", activeJobs.Count);

            // Register each job with Hangfire
            foreach (var job in activeJobs)
            {
                try
                {
                    await schedulerService.ScheduleJobAsync(
                        job.Id.ToString(),
                        job.JobType,
                        job.CronExpression,
                        job.Parameters,
                        stoppingToken);

                    _logger.LogInformation(
                        "Registered job: {jobName} ({jobType}) with schedule: {cron}",
                        job.JobName,
                        job.JobType,
                        job.CronExpression);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Failed to register job: {jobName} ({jobId})", 
                        job.JobName, 
                        job.Id);
                }
            }

            _logger.LogInformation("Scheduled jobs loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load scheduled jobs from database");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduler Worker is stopping at: {time}", DateTimeOffset.Now);
        await base.StopAsync(stoppingToken);
    }
}

