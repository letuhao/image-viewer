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
                    { "LibraryId", libraryId }
                };
                job.UpdateParameters(parameters);

                if (isEnabled)
                {
                    job.Enable();
                }

                var createdJob = await _scheduledJobRepository.CreateAsync(job);
                
                _logger.LogInformation(
                    "Created new scheduled job {JobId} for library {LibraryId}",
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
}

