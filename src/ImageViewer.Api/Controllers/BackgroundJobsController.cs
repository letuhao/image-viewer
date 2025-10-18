using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using ImageViewer.Application.Services;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Controller for background job monitoring and management
/// 中文：后台任务控制器
/// Tiếng Việt: Bộ điều khiển công việc nền
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
// [Authorize] // Uncomment when auth is fully tested
public class BackgroundJobsController : ControllerBase
{
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<BackgroundJobsController> _logger;

    public BackgroundJobsController(
        IBackgroundJobService backgroundJobService,
        ILogger<BackgroundJobsController> logger)
    {
        _backgroundJobService = backgroundJobService ?? throw new ArgumentNullException(nameof(backgroundJobService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all background jobs with pagination and filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllJobs(
        [FromQuery] string? status = null,
        [FromQuery] string? jobType = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        try
        {
            var jobs = await _backgroundJobService.GetJobsAsync(status, jobType);
            var jobsList = jobs.ToList();

            // Filter by status if provided
            if (!string.IsNullOrEmpty(status))
            {
                jobsList = jobsList.Where(j => j.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Filter by jobType if provided
            if (!string.IsNullOrEmpty(jobType))
            {
                jobsList = jobsList.Where(j => j.Type.Equals(jobType, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Order by timing (newest first)
            jobsList = jobsList.OrderByDescending(j => j.Timing?.CreatedAt ?? DateTime.MinValue).ToList();

            // Pagination
            var total = jobsList.Count;
            var totalPages = (int)Math.Ceiling((double)total / limit);
            var paginatedJobs = jobsList.Skip((page - 1) * limit).Take(limit).ToList();

            return Ok(new
            {
                data = paginatedJobs,
                pagination = new
                {
                    page,
                    limit,
                    total,
                    totalPages,
                    hasNext = page < totalPages,
                    hasPrevious = page > 1
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get background jobs");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get job by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetJobById(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var jobId))
                return BadRequest(new { message = "Invalid job ID" });

            var job = await _backgroundJobService.GetJobAsync(jobId);
            if (job == null)
                return NotFound(new { message = "Job not found" });

            return Ok(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get job {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get job statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var stats = await _backgroundJobService.GetJobStatisticsAsync();
            
            // Get all jobs for grouping by type
            var jobs = await _backgroundJobService.GetJobsAsync();
            var jobsList = jobs.ToList();

            return Ok(new
            {
                total = stats.TotalJobs,
                pending = stats.TotalJobs - stats.RunningJobs - stats.CompletedJobs - stats.FailedJobs - stats.CancelledJobs,
                running = stats.RunningJobs,
                completed = stats.CompletedJobs,
                failed = stats.FailedJobs,
                cancelled = stats.CancelledJobs,
                avgDuration = jobsList
                    .Where(j => j.Timing?.Duration != null)
                    .Select(j => j.Timing!.Duration!.Value.TotalSeconds)
                    .DefaultIfEmpty(0)
                    .Average(),
                byType = jobsList.GroupBy(j => j.Type).Select(g => new
                {
                    jobType = g.Key,
                    count = g.Count(),
                    running = g.Count(j => j.Status == "Running"),
                    completed = g.Count(j => j.Status == "Completed"),
                    failed = g.Count(j => j.Status == "Failed")
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get job statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Cancel a job
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelJob(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var jobId))
                return BadRequest(new { message = "Invalid job ID" });

            var job = await _backgroundJobService.GetJobAsync(jobId);
            if (job == null)
                return NotFound(new { message = "Job not found" });

            if (job.Status != "Pending" && job.Status != "Running")
                return BadRequest(new { message = $"Cannot cancel job with status '{job.Status}'" });

            await _backgroundJobService.CancelJobAsync(jobId);

            _logger.LogInformation("Cancelled job {JobId}", jobId);

            return Ok(new { message = "Job cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel job {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete completed/failed jobs (cleanup)
    /// </summary>
    [HttpDelete("cleanup")]
    public async Task<IActionResult> CleanupJobs([FromQuery] int olderThanDays = 7)
    {
        try
        {
            var jobs = await _backgroundJobService.GetJobsAsync();
            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
            
            var jobsToDelete = jobs.Where(j => 
                (j.Status == "Completed" || j.Status == "Failed" || j.Status == "Cancelled") &&
                j.Timing?.CompletedAt != null &&
                j.Timing.CompletedAt < cutoffDate
            ).ToList();

            foreach (var job in jobsToDelete)
            {
                await _backgroundJobService.DeleteJobAsync(job.JobId);
            }

            _logger.LogInformation("Cleaned up {Count} old jobs", jobsToDelete.Count);

            return Ok(new 
            { 
                message = $"Cleaned up {jobsToDelete.Count} jobs",
                deletedCount = jobsToDelete.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup jobs");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

