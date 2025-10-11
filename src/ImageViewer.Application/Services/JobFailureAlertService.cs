using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Application.Services;

/// <summary>
/// Implementation of job failure alert service
/// </summary>
public class JobFailureAlertService : IJobFailureAlertService
{
    private readonly IFileProcessingJobStateRepository _jobStateRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<JobFailureAlertService> _logger;
    private readonly HashSet<string> _alertedJobs = new(); // Track jobs we've already alerted for

    public JobFailureAlertService(
        IFileProcessingJobStateRepository jobStateRepository,
        INotificationService notificationService,
        ILogger<JobFailureAlertService> logger)
    {
        _jobStateRepository = jobStateRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task CheckAndAlertAsync(string jobId, double failureThreshold = 0.1)
    {
        try
        {
            // Don't alert multiple times for same job
            if (_alertedJobs.Contains(jobId))
            {
                return;
            }

            var jobState = await _jobStateRepository.GetByJobIdAsync(jobId);
            if (jobState == null)
            {
                return;
            }

            // Calculate failure rate
            var processedCount = jobState.CompletedImages + jobState.FailedImages + jobState.SkippedImages;
            if (processedCount < 10) // Need at least 10 samples
            {
                return;
            }

            var failureRate = (double)jobState.FailedImages / processedCount;
            
            if (failureRate > failureThreshold)
            {
                _logger.LogWarning("⚠️ High failure rate detected for job {JobId}: {FailureRate:P} ({Failed}/{Total})",
                    jobId, failureRate, jobState.FailedImages, processedCount);

                await SendJobFailureAlertAsync(
                    jobId,
                    jobState.JobType,
                    jobState.CollectionName ?? "Unknown",
                    jobState.FailedImages,
                    jobState.TotalImages,
                    failureRate
                );

                // Mark as alerted
                _alertedJobs.Add(jobId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking failure threshold for job {JobId}", jobId);
        }
    }

    public async Task MonitorAllJobsAsync(double failureThreshold = 0.1)
    {
        try
        {
            _logger.LogDebug("🔍 Monitoring all running jobs for high failure rates...");

            var runningJobs = await _jobStateRepository.GetIncompleteJobsAsync();
            var jobsList = runningJobs.Where(j => j.Status == "Running").ToList();

            if (!jobsList.Any())
            {
                return;
            }

            foreach (var job in jobsList)
            {
                await CheckAndAlertAsync(job.JobId, failureThreshold);
            }

            _logger.LogDebug("✅ Monitored {Count} running jobs", jobsList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring all jobs");
        }
    }

    public async Task SendJobFailureAlertAsync(
        string jobId, 
        string jobType, 
        string collectionName, 
        int failedCount, 
        int totalCount, 
        double failureRate)
    {
        try
        {
            var message = $"⚠️ High failure rate alert!\n\n" +
                         $"Job Type: {jobType}\n" +
                         $"Collection: {collectionName}\n" +
                         $"Job ID: {jobId}\n\n" +
                         $"Failed: {failedCount}/{totalCount} ({failureRate:P})\n\n" +
                         $"Please check worker logs for details.";

            _logger.LogWarning("🚨 ALERT: {Message}", message);

            // Send notification (if notification service is available)
            try
            {
                var notificationRequest = new DTOs.Notifications.CreateNotificationRequest
                {
                    UserId = null, // System notification
                    Title = $"Job Failure Alert: {jobType} - {collectionName}",
                    Message = message,
                    Type = "error",
                    Data = new Dictionary<string, object>
                    {
                        { "jobId", jobId },
                        { "jobType", jobType },
                        { "failureRate", failureRate },
                        { "failedCount", failedCount },
                        { "totalCount", totalCount }
                    }
                };

                await _notificationService.CreateNotificationAsync(notificationRequest);

                _logger.LogInformation("✅ Failure alert sent for job {JobId}", jobId);
            }
            catch (Exception ex)
            {
                // Notification service might not be available, just log
                _logger.LogDebug(ex, "Could not send notification, but alert logged");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending job failure alert for {JobId}", jobId);
        }
    }
}

