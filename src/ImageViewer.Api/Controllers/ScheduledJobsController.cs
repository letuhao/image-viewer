using Microsoft.AspNetCore.Mvc;
using ImageViewer.Application.DTOs;
using ImageViewer.Application.Services;
using ImageViewer.Application.Mappings;
using ImageViewer.Domain.Interfaces;
using MongoDB.Bson;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Controller for managing scheduled jobs
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class ScheduledJobsController : ControllerBase
{
    private readonly IScheduledJobManagementService _scheduledJobManagementService;
    private readonly IScheduledJobRepository _scheduledJobRepository;
    private readonly IScheduledJobRunRepository _scheduledJobRunRepository;
    private readonly ILogger<ScheduledJobsController> _logger;

    public ScheduledJobsController(
        IScheduledJobManagementService scheduledJobManagementService,
        IScheduledJobRepository scheduledJobRepository,
        IScheduledJobRunRepository scheduledJobRunRepository,
        ILogger<ScheduledJobsController> logger)
    {
        _scheduledJobManagementService = scheduledJobManagementService;
        _scheduledJobRepository = scheduledJobRepository;
        _scheduledJobRunRepository = scheduledJobRunRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all scheduled jobs
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ScheduledJobDto>>> GetAllScheduledJobs()
    {
        try
        {
            var jobs = await _scheduledJobRepository.GetAllAsync();
            var jobDtos = jobs.ToDto();
            return Ok(jobDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all scheduled jobs");
            return StatusCode(500, new { message = "Failed to retrieve scheduled jobs" });
        }
    }

    /// <summary>
    /// Get scheduled job by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ScheduledJobDto>> GetScheduledJob(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return BadRequest(new { message = "Invalid job ID format" });
            }

            var job = await _scheduledJobRepository.GetByIdAsync(objectId);
            if (job == null)
            {
                return NotFound(new { message = $"Scheduled job with ID {id} not found" });
            }

            return Ok(job.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get scheduled job {JobId}", id);
            return StatusCode(500, new { message = "Failed to retrieve scheduled job" });
        }
    }

    /// <summary>
    /// Get scheduled job by library ID
    /// </summary>
    [HttpGet("library/{libraryId}")]
    public async Task<ActionResult<ScheduledJobDto>> GetScheduledJobByLibrary(string libraryId)
    {
        try
        {
            if (!ObjectId.TryParse(libraryId, out var objectId))
            {
                return BadRequest(new { message = "Invalid library ID format" });
            }

            var job = await _scheduledJobManagementService.GetJobByLibraryIdAsync(objectId);
            if (job == null)
            {
                return NotFound(new { message = $"No scheduled job found for library {libraryId}" });
            }

            return Ok(job.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get scheduled job for library {LibraryId}", libraryId);
            return StatusCode(500, new { message = "Failed to retrieve scheduled job" });
        }
    }

    /// <summary>
    /// Enable a scheduled job
    /// </summary>
    [HttpPost("{id}/enable")]
    public async Task<ActionResult> EnableScheduledJob(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return BadRequest(new { message = "Invalid job ID format" });
            }

            await _scheduledJobManagementService.EnableJobAsync(objectId);
            return Ok(new { message = "Scheduled job enabled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable scheduled job {JobId}", id);
            return StatusCode(500, new { message = "Failed to enable scheduled job" });
        }
    }

    /// <summary>
    /// Disable a scheduled job
    /// </summary>
    [HttpPost("{id}/disable")]
    public async Task<ActionResult> DisableScheduledJob(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return BadRequest(new { message = "Invalid job ID format" });
            }

            await _scheduledJobManagementService.DisableJobAsync(objectId);
            return Ok(new { message = "Scheduled job disabled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable scheduled job {JobId}", id);
            return StatusCode(500, new { message = "Failed to disable scheduled job" });
        }
    }

    /// <summary>
    /// Delete a scheduled job
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteScheduledJob(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return BadRequest(new { message = "Invalid job ID format" });
            }

            await _scheduledJobManagementService.DeleteJobAsync(objectId);
            return Ok(new { message = "Scheduled job deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete scheduled job {JobId}", id);
            return StatusCode(500, new { message = "Failed to delete scheduled job" });
        }
    }

    /// <summary>
    /// Get job execution history
    /// </summary>
    [HttpGet("{id}/runs")]
    public async Task<ActionResult<List<ScheduledJobRunDto>>> GetJobRuns(
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return BadRequest(new { message = "Invalid job ID format" });
            }

            var allRuns = await _scheduledJobRunRepository.GetAllAsync();
            var jobRuns = allRuns
                .Where(r => r.ScheduledJobId == objectId)
                .OrderByDescending(r => r.StartedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var totalCount = allRuns.Count(r => r.ScheduledJobId == objectId);
            var jobRunDtos = jobRuns.ToDto();

            return Ok(new
            {
                data = jobRunDtos,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get job runs for {JobId}", id);
            return StatusCode(500, new { message = "Failed to retrieve job execution history" });
        }
    }

    /// <summary>
    /// Get all active (enabled) scheduled jobs
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<List<ScheduledJobDto>>> GetActiveScheduledJobs()
    {
        try
        {
            var allJobs = await _scheduledJobRepository.GetAllAsync();
            var activeJobs = allJobs.Where(j => j.IsEnabled).ToList();
            var jobDtos = activeJobs.ToDto();
            return Ok(jobDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active scheduled jobs");
            return StatusCode(500, new { message = "Failed to retrieve active scheduled jobs" });
        }
    }

    /// <summary>
    /// Get recent job execution history across all jobs
    /// </summary>
    [HttpGet("runs/recent")]
    public async Task<ActionResult<List<ScheduledJobRunDto>>> GetRecentJobRuns(
        [FromQuery] int limit = 50)
    {
        try
        {
            var allRuns = await _scheduledJobRunRepository.GetAllAsync();
            var recentRuns = allRuns
                .OrderByDescending(r => r.StartedAt)
                .Take(limit)
                .ToList();

            var runDtos = recentRuns.ToDto();
            return Ok(runDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent job runs");
            return StatusCode(500, new { message = "Failed to retrieve recent job executions" });
        }
    }
}

