using Microsoft.AspNetCore.Mvc;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.BackgroundJobs;
using MongoDB.Bson;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Background jobs management controller
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(IBackgroundJobService backgroundJobService, ILogger<JobsController> logger)
    {
        _backgroundJobService = backgroundJobService;
        _logger = logger;
    }

    /// <summary>
    /// Get all background jobs
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BackgroundJobDto>>> GetAllJobs(
        [FromQuery] string? status = null,
        [FromQuery] string? type = null)
    {
        try
        {
            _logger.LogInformation("Getting all background jobs. Status: {Status}, Type: {Type}", status, type);
            var jobs = await _backgroundJobService.GetJobsAsync(status, type);
            return Ok(jobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all background jobs");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get job by ID
    /// </summary>
    [HttpGet("{jobId}")]
    public async Task<ActionResult<BackgroundJobDto>> GetJob(ObjectId jobId)
    {
        try
        {
            _logger.LogInformation("Getting job with ID: {JobId}", jobId);
            var job = await _backgroundJobService.GetJobAsync(jobId);
            return Ok(job);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Job not found: {JobId}", jobId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job with ID: {JobId}", jobId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create new background job
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BackgroundJobDto>> CreateJob([FromBody] CreateBackgroundJobDto dto)
    {
        try
        {
            _logger.LogInformation("Creating background job of type: {Type}", dto.Type);
            var job = await _backgroundJobService.CreateJobAsync(dto);
            return CreatedAtAction(nameof(GetJob), new { jobId = job.JobId }, job);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid job data");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating background job");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update job status
    /// </summary>
    [HttpPut("{jobId}/status")]
    public async Task<ActionResult<BackgroundJobDto>> UpdateJobStatus(ObjectId jobId, [FromBody] UpdateJobStatusDto dto)
    {
        try
        {
            _logger.LogInformation("Updating status for job {JobId} to {Status}", jobId, dto.Status);
            var job = await _backgroundJobService.UpdateJobStatusAsync(jobId, dto.Status, dto.Message);
            return Ok(job);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Job not found: {JobId}", jobId);
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid status data for job: {JobId}", jobId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job status for ID: {JobId}", jobId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update job progress
    /// </summary>
    [HttpPut("{jobId}/progress")]
    public async Task<ActionResult<BackgroundJobDto>> UpdateJobProgress(ObjectId jobId, [FromBody] UpdateJobProgressDto dto)
    {
        try
        {
            _logger.LogInformation("Updating progress for job {JobId}. Completed: {Completed}/{Total}", jobId, dto.Completed, dto.Total);
            var job = await _backgroundJobService.UpdateJobProgressAsync(jobId, dto.Completed, dto.Total, dto.CurrentItem);
            return Ok(job);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Job not found: {JobId}", jobId);
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid progress data for job: {JobId}", jobId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job progress for ID: {JobId}", jobId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Cancel job
    /// </summary>
    [HttpPost("{jobId}/cancel")]
    public async Task<ActionResult> CancelJob(ObjectId jobId)
    {
        try
        {
            _logger.LogInformation("Cancelling job with ID: {JobId}", jobId);
            await _backgroundJobService.CancelJobAsync(jobId);
            return Ok(new { message = "Job cancelled successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Job not found: {JobId}", jobId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling job with ID: {JobId}", jobId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete job
    /// </summary>
    [HttpDelete("{jobId}")]
    public async Task<ActionResult> DeleteJob(ObjectId jobId)
    {
        try
        {
            _logger.LogInformation("Deleting job with ID: {JobId}", jobId);
            await _backgroundJobService.DeleteJobAsync(jobId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Job not found: {JobId}", jobId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting job with ID: {JobId}", jobId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get job statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<JobStatisticsDto>> GetJobStatistics()
    {
        try
        {
            _logger.LogInformation("Getting job statistics");
            var statistics = await _backgroundJobService.GetJobStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job statistics");
            return StatusCode(500, "Internal server error");
        }
    }
}
