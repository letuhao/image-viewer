using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Infrastructure.Messaging;
using MongoDB.Bson;

namespace ImageViewer.Scheduler.Jobs;

/// <summary>
/// Handles library scan scheduled jobs by publishing scan messages to RabbitMQ
/// </summary>
public class LibraryScanJobHandler : ILibraryScanJobHandler
{
    private readonly ILogger<LibraryScanJobHandler> _logger;
    private readonly ILibraryRepository _libraryRepository;
    private readonly IMessageQueueService _messageQueueService;
    private readonly IScheduledJobRunRepository _jobRunRepository;

    public LibraryScanJobHandler(
        ILogger<LibraryScanJobHandler> logger,
        ILibraryRepository libraryRepository,
        IMessageQueueService messageQueueService,
        IScheduledJobRunRepository jobRunRepository)
    {
        _logger = logger;
        _libraryRepository = libraryRepository;
        _messageQueueService = messageQueueService;
        _jobRunRepository = jobRunRepository;
    }

    public async Task<Dictionary<string, object>> ExecuteAsync(ScheduledJob job, CancellationToken cancellationToken = default)
    {
        var runId = ObjectId.GenerateNewId();
        var startedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Starting library scan job. ScheduledJobId: {scheduledJobId}, JobName: {jobName}, RunId: {runId}",
            job.Id,
            job.Name,
            runId);

        try
        {
            // Extract library ID from parameters
            if (!job.Parameters.TryGetValue("LibraryId", out var libraryIdObj) || libraryIdObj == null)
            {
                throw new ArgumentException("LibraryId parameter is required for library scan jobs");
            }

            var libraryId = libraryIdObj switch
            {
                ObjectId oid => oid,
                string str => ObjectId.Parse(str),
                _ => throw new ArgumentException($"Invalid LibraryId type: {libraryIdObj.GetType()}")
            };

            // Verify library exists
            var library = await _libraryRepository.GetByIdAsync(libraryId);
            if (library == null)
            {
                throw new InvalidOperationException($"Library not found: {libraryId}");
            }

            if (library.IsDeleted)
            {
                _logger.LogWarning(
                    "Library {libraryId} is marked as deleted, skipping scan",
                    libraryId);
                
                return new Dictionary<string, object>
                {
                    { "status", "skipped" },
                    { "reason", "Library is deleted" },
                    { "libraryId", libraryId.ToString() }
                };
            }

            _logger.LogInformation(
                "Scanning library: {libraryName} (ID: {libraryId}, Path: {path})",
                library.Name,
                libraryId,
                library.Path);

            // Create job run record
            var jobRun = new ScheduledJobRun(
                job.Id,
                job.Name,
                job.JobType,
                "Scheduler");

            await _jobRunRepository.CreateAsync(jobRun);

            // Publish library scan message to RabbitMQ
            var scanMessage = new LibraryScanMessage
            {
                LibraryId = libraryId.ToString(),
                LibraryPath = library.Path,
                ScheduledJobId = job.Id.ToString(),
                JobRunId = runId.ToString(),
                ScanType = "Full", // Can be "Full" or "Incremental"
                IncludeSubfolders = true
            };

            await _messageQueueService.PublishAsync(scanMessage, "library_scan_queue");

            _logger.LogInformation(
                "Published library scan message for library {libraryId} to RabbitMQ. JobRunId: {runId}",
                libraryId,
                runId);

            // Mark job run as completed (message published successfully)
            jobRun.Complete(new Dictionary<string, object>
            {
                { "message", "Library scan message published to queue successfully" },
                { "libraryId", libraryId.ToString() },
                { "runId", runId.ToString() }
            });
            await _jobRunRepository.UpdateAsync(jobRun);

            var duration = (DateTime.UtcNow - startedAt).TotalMilliseconds;
            
            _logger.LogInformation(
                "Library scan job completed. ScheduledJobId: {scheduledJobId}, RunId: {runId}, Duration: {duration}ms",
                job.Id,
                runId,
                duration);

            return new Dictionary<string, object>
            {
                { "status", "success" },
                { "libraryId", libraryId.ToString() },
                { "libraryName", library.Name },
                { "libraryPath", library.Path },
                { "runId", runId.ToString() },
                { "duration", duration },
                { "message", "Library scan message published successfully" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Library scan job failed. ScheduledJobId: {scheduledJobId}, RunId: {runId}",
                job.Id,
                runId);

            // Try to mark job run as failed
            try
            {
                var failedRun = await _jobRunRepository.GetByIdAsync(runId);
                if (failedRun != null)
                {
                    failedRun.Fail(ex.Message);
                    await _jobRunRepository.UpdateAsync(failedRun);
                }
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Failed to update job run status to failed");
            }

            return new Dictionary<string, object>
            {
                { "status", "failed" },
                { "error", ex.Message },
                { "runId", runId.ToString() },
                { "duration", (DateTime.UtcNow - startedAt).TotalMilliseconds }
            };
        }
    }
}

