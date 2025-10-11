using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service for managing scheduled jobs from the API layer
/// Scheduler worker will pick up changes automatically
/// </summary>
public interface IScheduledJobManagementService
{
    /// <summary>
    /// Create or update a library scan scheduled job
    /// </summary>
    Task<ScheduledJob> CreateOrUpdateLibraryScanJobAsync(
        ObjectId libraryId,
        string libraryName,
        string cronExpression,
        bool isEnabled = true);

    /// <summary>
    /// Disable a scheduled job
    /// </summary>
    Task DisableJobAsync(ObjectId jobId);

    /// <summary>
    /// Enable a scheduled job
    /// </summary>
    Task EnableJobAsync(ObjectId jobId);

    /// <summary>
    /// Delete a scheduled job
    /// </summary>
    Task DeleteJobAsync(ObjectId jobId);

    /// <summary>
    /// Get scheduled job by library ID
    /// </summary>
    Task<ScheduledJob?> GetJobByLibraryIdAsync(ObjectId libraryId);

    /// <summary>
    /// Remove orphaned job (job without Hangfire binding)
    /// </summary>
    Task RemoveOrphanedJobAsync(ObjectId jobId);

    /// <summary>
    /// Recreate Hangfire job for scheduled job
    /// </summary>
    Task RecreateHangfireJobAsync(ObjectId jobId);

    /// <summary>
    /// Get all orphaned jobs (jobs without HangfireJobId)
    /// </summary>
    Task<List<ScheduledJob>> GetOrphanedJobsAsync();
}

public class ScheduledJobManagementService : IScheduledJobManagementService
{
    private readonly IScheduledJobRepository _scheduledJobRepository;
    private readonly ILogger<ScheduledJobManagementService> _logger;

    public ScheduledJobManagementService(
        IScheduledJobRepository scheduledJobRepository,
        ILogger<ScheduledJobManagementService> logger)
    {
        _scheduledJobRepository = scheduledJobRepository;
        _logger = logger;
    }

    public async Task<ScheduledJob> CreateOrUpdateLibraryScanJobAsync(
        ObjectId libraryId,
        string libraryName,
        string cronExpression,
        bool isEnabled = true)
    {
        try
        {
            // Validate cron expression BEFORE creating job
            if (!IsValidCronExpression(cronExpression))
            {
                var errorMsg = $"Invalid cron expression: '{cronExpression}'. Please use valid cron format (e.g., '0 2 * * *' for 2 AM daily)";
                _logger.LogError(errorMsg);
                throw new ArgumentException(errorMsg, nameof(cronExpression));
            }

            _logger.LogInformation(
                "Creating/updating library scan job for library {LibraryId} ({LibraryName}) with cron: {Cron}",
                libraryId,
                libraryName,
                cronExpression);

            // Check if job already exists for this library
            var existingJobs = await _scheduledJobRepository.GetAllAsync();
            var existingJob = existingJobs.FirstOrDefault(j =>
                j.JobType == "LibraryScan" &&
                j.Parameters.ContainsKey("LibraryId") &&
                j.Parameters["LibraryId"].ToString() == libraryId.ToString());

            if (existingJob != null)
            {
                // Update existing job
                _logger.LogInformation("Updating existing scheduled job {JobId}", existingJob.Id);
                
                existingJob.UpdateCronExpression(cronExpression);
                if (isEnabled && !existingJob.IsEnabled)
                {
                    existingJob.Enable();
                }
                else if (!isEnabled && existingJob.IsEnabled)
                {
                    existingJob.Disable();
                }

                await _scheduledJobRepository.UpdateAsync(existingJob);
                
                _logger.LogInformation(
                    "⚠️ Job updated. Scheduler will register it with Hangfire within {Minutes} minutes. Check HangfireJobId after {Minutes} minutes.",
                    5, 5);
                
                return existingJob;
            }
            else
            {
                // Create new job
                var job = new ScheduledJob(
                    name: $"Library Scan - {libraryName}",
                    jobType: "LibraryScan",
                    scheduleType: ScheduleType.Cron,
                    cronExpression: cronExpression,
                    intervalMinutes: null,
                    description: $"Automatic scan for library: {libraryName}");

                // Add library ID to parameters
                var parameters = new Dictionary<string, object>
                {
                    { "LibraryId", libraryId.ToString() }
                };
                job.UpdateParameters(parameters);

                if (isEnabled)
                {
                    job.Enable();
                }

                var createdJob = await _scheduledJobRepository.CreateAsync(job);
                
                _logger.LogWarning(
                    "⚠️ Created scheduled job {JobId} for library {LibraryId}. " +
                    "IMPORTANT: Scheduler will register it with Hangfire within 5 minutes. " +
                    "Job will remain orphaned (HangfireJobId = null) until then. " +
                    "Check HangfireJobId after 5 minutes to confirm binding.",
                    createdJob.Id,
                    libraryId);

                return createdJob;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create/update library scan job for library {LibraryId}",
                libraryId);
            throw;
        }
    }

    public async Task DisableJobAsync(ObjectId jobId)
    {
        try
        {
            var job = await _scheduledJobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                _logger.LogWarning("Scheduled job {JobId} not found", jobId);
                return;
            }

            job.Disable();
            await _scheduledJobRepository.UpdateAsync(job);

            _logger.LogInformation("Disabled scheduled job {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable scheduled job {JobId}", jobId);
            throw;
        }
    }

    public async Task EnableJobAsync(ObjectId jobId)
    {
        try
        {
            var job = await _scheduledJobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                _logger.LogWarning("Scheduled job {JobId} not found", jobId);
                return;
            }

            job.Enable();
            await _scheduledJobRepository.UpdateAsync(job);

            _logger.LogInformation("Enabled scheduled job {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable scheduled job {JobId}", jobId);
            throw;
        }
    }

    public async Task DeleteJobAsync(ObjectId jobId)
    {
        try
        {
            var job = await _scheduledJobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                _logger.LogWarning("Scheduled job {JobId} not found", jobId);
                return;
            }

            await _scheduledJobRepository.DeleteAsync(jobId);

            _logger.LogInformation("Deleted scheduled job {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete scheduled job {JobId}", jobId);
            throw;
        }
    }

    public async Task<ScheduledJob?> GetJobByLibraryIdAsync(ObjectId libraryId)
    {
        try
        {
            var allJobs = await _scheduledJobRepository.GetAllAsync();
            return allJobs.FirstOrDefault(j =>
                j.JobType == "LibraryScan" &&
                j.Parameters.ContainsKey("LibraryId") &&
                j.Parameters["LibraryId"].ToString() == libraryId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get scheduled job for library {LibraryId}", libraryId);
            throw;
        }
    }


    public async Task RemoveOrphanedJobAsync(ObjectId jobId)
    {
        try
        {
            var job = await _scheduledJobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                _logger.LogWarning("Scheduled job {JobId} not found", jobId);
                return;
            }

            if (!string.IsNullOrEmpty(job.HangfireJobId))
            {
                _logger.LogWarning("Job {JobId} has Hangfire binding {HangfireJobId}, not orphaned", jobId, job.HangfireJobId);
                throw new InvalidOperationException($"Job {jobId} is not orphaned - it has Hangfire binding: {job.HangfireJobId}");
            }

            await _scheduledJobRepository.DeleteAsync(jobId);
            _logger.LogInformation("Removed orphaned job {JobId} ({JobName})", jobId, job.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove orphaned job {JobId}", jobId);
            throw;
        }
    }

    public async Task RecreateHangfireJobAsync(ObjectId jobId)
    {
        try
        {
            var job = await _scheduledJobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                _logger.LogWarning("Scheduled job {JobId} not found", jobId);
                throw new InvalidOperationException($"Job {jobId} not found");
            }

            // Disable and re-enable to force Hangfire registration
            // This will trigger the scheduler to pick it up and bind it
            job.Disable();
            await _scheduledJobRepository.UpdateAsync(job);
            
            _logger.LogInformation("Disabled job {JobId} for recreation", jobId);
            
            // Wait a moment for the scheduler to process the disable
            await Task.Delay(500);
            
            job.Enable();
            await _scheduledJobRepository.UpdateAsync(job);
            
            _logger.LogInformation("Re-enabled job {JobId} to trigger Hangfire binding", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recreate Hangfire job for {JobId}", jobId);
            throw;
        }
    }

    public async Task<List<ScheduledJob>> GetOrphanedJobsAsync()
    {
        try
        {
            var allJobs = await _scheduledJobRepository.GetAllAsync();
            var orphanedJobs = allJobs
                .Where(j => !j.IsDeleted && string.IsNullOrEmpty(j.HangfireJobId))
                .ToList();

            _logger.LogInformation("Found {Count} orphaned jobs", orphanedJobs.Count);
            return orphanedJobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get orphaned jobs");
            throw;
        }
    
    }

    private bool IsValidCronExpression(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            return false;

        try
        {
            // Basic cron validation: should have 5 parts (minute hour day month dayOfWeek)
            var parts = cronExpression.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5)
            {
                _logger.LogWarning("Cron expression '{Cron}' has {Count} parts, expected 5", cronExpression, parts.Length);
                return false;
            }

            // Validate each part contains only allowed characters
            foreach (var part in parts)
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(part, @"^[\d\*\/\-,]+$"))
                {
                    _logger.LogWarning("Cron expression part '{Part}' contains invalid characters", part);
                    return false;
                }
            }

            _logger.LogDebug("Cron expression '{Cron}' is valid", cronExpression);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating cron expression '{Cron}'", cronExpression);
            return false;
        }
    }
}

