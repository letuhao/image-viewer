using Hangfire;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Enums;
using ImageViewer.Scheduler.Jobs;
using MongoDB.Bson;
using NCrontab;

namespace ImageViewer.Scheduler.Services;

/// <summary>
/// Hangfire-based scheduler service implementation
/// 基于Hangfire的调度服务实现 - Dịch vụ lập lịch dựa trên Hangfire
/// </summary>
public class HangfireSchedulerService : ISchedulerService
{
    private readonly IScheduledJobRepository _scheduledJobRepository;
    private readonly IScheduledJobRunRepository _jobRunRepository;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly ILogger<HangfireSchedulerService> _logger;

    public HangfireSchedulerService(
        IScheduledJobRepository scheduledJobRepository,
        IScheduledJobRunRepository jobRunRepository,
        IBackgroundJobClient backgroundJobClient,
        IRecurringJobManager recurringJobManager,
        ILogger<HangfireSchedulerService> logger)
    {
        _scheduledJobRepository = scheduledJobRepository;
        _jobRunRepository = jobRunRepository;
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
        _logger = logger;
    }

    public async Task<ScheduledJob> CreateScheduledJobAsync(ScheduledJob job)
    {
        _logger.LogInformation("Creating scheduled job: {Name} ({JobType})", job.Name, job.JobType);
        
        // Save to database
        var createdJob = await _scheduledJobRepository.CreateAsync(job);
        
        // If enabled, register with Hangfire
        if (job.IsEnabled)
        {
            await RegisterJobWithHangfireAsync(createdJob);
        }
        
        return createdJob;
    }

    public async Task<bool> EnableJobAsync(ObjectId jobId)
    {
        var job = await _scheduledJobRepository.GetByIdAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Job {JobId} not found", jobId);
            return false;
        }

        job.Enable();
        await _scheduledJobRepository.UpdateAsync(job);
        
        // Register with Hangfire
        await RegisterJobWithHangfireAsync(job);
        
        _logger.LogInformation("✅ Enabled scheduled job: {Name}", job.Name);
        return true;
    }

    public async Task<bool> DisableJobAsync(ObjectId jobId)
    {
        var job = await _scheduledJobRepository.GetByIdAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Job {JobId} not found", jobId);
            return false;
        }

        job.Disable();
        await _scheduledJobRepository.UpdateAsync(job);
        
        // Remove from Hangfire
        if (!string.IsNullOrEmpty(job.HangfireJobId))
        {
            _recurringJobManager.RemoveIfExists(job.HangfireJobId);
            _logger.LogInformation("🗑️ Removed job from Hangfire: {HangfireJobId}", job.HangfireJobId);
        }
        
        _logger.LogInformation("⏸️ Disabled scheduled job: {Name}", job.Name);
        return true;
    }

    public async Task<DateTime?> CalculateNextRunTimeAsync(string? cronExpression, int? intervalMinutes = null, ScheduleType scheduleType = ScheduleType.Cron)
    {
        try
        {
            if (scheduleType == ScheduleType.Cron && !string.IsNullOrWhiteSpace(cronExpression))
            {
                // Parse cron expression (supports standard cron format)
                var schedule = CrontabSchedule.Parse(cronExpression, new CrontabSchedule.ParseOptions { IncludingSeconds = true });
                var nextOccurrence = schedule.GetNextOccurrence(DateTime.UtcNow);
                return nextOccurrence;
            }
            else if (scheduleType == ScheduleType.Interval && intervalMinutes.HasValue && intervalMinutes.Value > 0)
            {
                return DateTime.UtcNow.AddMinutes(intervalMinutes.Value);
            }
            else if (scheduleType == ScheduleType.Once)
            {
                return null; // One-time jobs don't have next run
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating next run time for cron: {CronExpression}", cronExpression);
            return null;
        }
    }

    public async Task<IEnumerable<ScheduledJob>> GetActiveScheduledJobsAsync()
    {
        return await _scheduledJobRepository.GetEnabledJobsAsync();
    }

    public async Task ExecuteJobAsync(ObjectId jobId)
    {
        var job = await _scheduledJobRepository.GetByIdAsync(jobId);
        if (job == null)
        {
            _logger.LogWarning("Job {JobId} not found", jobId);
            return;
        }

        _logger.LogInformation("⏰ Manually executing scheduled job: {Name}", job.Name);
        
        // Enqueue immediate execution via Hangfire
        _backgroundJobClient.Enqueue<IScheduledJobExecutor>(
            executor => executor.ExecuteAsync(jobId, default));
    }

    private async Task RegisterJobWithHangfireAsync(ScheduledJob job)
    {
        var hangfireJobId = $"scheduled-job-{job.Id}";
        
        switch (job.ScheduleType)
        {
            case ScheduleType.Cron:
                if (!string.IsNullOrWhiteSpace(job.CronExpression))
                {
                    _recurringJobManager.AddOrUpdate<IScheduledJobExecutor>(
                        recurringJobId: hangfireJobId,
                        methodCall: executor => executor.ExecuteAsync(job.Id, default),
                        cronExpression: job.CronExpression,
                        options: new RecurringJobOptions
                        {
                            TimeZone = TimeZoneInfo.Local
                        });
                    
                    job.SetHangfireJobId(hangfireJobId);
                    await _scheduledJobRepository.UpdateAsync(job);
                    
                    _logger.LogInformation("✅ Registered cron job with Hangfire: {JobId} - {CronExpression}", 
                        hangfireJobId, job.CronExpression);
                }
                break;
                
            case ScheduleType.Interval:
                if (job.IntervalMinutes.HasValue && job.IntervalMinutes.Value > 0)
                {
                    // Convert interval to cron expression (every X minutes)
                    var cronExpression = $"*/{job.IntervalMinutes} * * * *";
                    
                    _recurringJobManager.AddOrUpdate<IScheduledJobExecutor>(
                        recurringJobId: hangfireJobId,
                        methodCall: executor => executor.ExecuteAsync(job.Id, default),
                        cronExpression: cronExpression,
                        options: new RecurringJobOptions
                        {
                            TimeZone = TimeZoneInfo.Local
                        });
                    
                    job.SetHangfireJobId(hangfireJobId);
                    await _scheduledJobRepository.UpdateAsync(job);
                    
                    _logger.LogInformation("✅ Registered interval job with Hangfire: {JobId} - Every {Minutes} minutes", 
                        hangfireJobId, job.IntervalMinutes.Value);
                }
                break;
                
            case ScheduleType.Once:
                // Schedule for immediate or future execution
                var nextRun = job.NextRunAt ?? DateTime.UtcNow;
                var delay = nextRun - DateTime.UtcNow;
                
                if (delay.TotalSeconds > 0)
                {
                    _backgroundJobClient.Schedule<IScheduledJobExecutor>(
                        executor => executor.ExecuteAsync(job.Id, default),
                        delay);
                    
                    _logger.LogInformation("✅ Scheduled one-time job for {Delay} from now", delay);
                }
                else
                {
                    _backgroundJobClient.Enqueue<IScheduledJobExecutor>(
                        executor => executor.ExecuteAsync(job.Id, default));
                    
                    _logger.LogInformation("✅ Enqueued one-time job for immediate execution");
                }
                break;
        }
    }
}

