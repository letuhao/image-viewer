using Microsoft.AspNetCore.Mvc;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.BackgroundJobs;
using MongoDB.Bson;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Bulk operations controller
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class BulkController : ControllerBase
{
    private readonly IBulkService _bulkService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<BulkController> _logger;

    public BulkController(IBulkService bulkService, IBackgroundJobService backgroundJobService, ILogger<BulkController> logger)
    {
        _bulkService = bulkService;
        _backgroundJobService = backgroundJobService;
        _logger = logger;
    }

    /// <summary>
    /// Bulk add collections from parent directory (asynchronous background job)
    /// </summary>
    [HttpPost("collections")]
    public async Task<ActionResult<BackgroundJobDto>> BulkAddCollections([FromBody] BulkAddCollectionsRequest request)
    {
        try
        {
            _logger.LogInformation("Starting bulk add collections from parent path {ParentPath}", request.ParentPath);
            
            // Validate request parameters
            if (string.IsNullOrEmpty(request.ParentPath))
            {
                return BadRequest(new { error = "Parent path is required" });
            }
            
            // Create bulk operation DTO for background job
            var bulkOperationDto = new BulkOperationDto
            {
                OperationType = "BulkAddCollections",
                Parameters = new Dictionary<string, object?>
                {
                    ["ParentPath"] = request.ParentPath,
                    ["CollectionPrefix"] = request.CollectionPrefix ?? "",
                    ["IncludeSubfolders"] = request.IncludeSubfolders,
                    ["AutoAdd"] = request.AutoAdd,
                    ["OverwriteExisting"] = request.OverwriteExisting,
                    ["ProcessCompressedFiles"] = true, // Default to true for compressed files
                    ["MaxConcurrentOperations"] = 5, // Default to 5 concurrent operations
                    ["CreatedAfter"] = request.CreatedAfter,
                    ["CreatedBefore"] = request.CreatedBefore,
                    ["ModifiedAfter"] = request.ModifiedAfter,
                    ["ModifiedBefore"] = request.ModifiedBefore
                }
            };
            
            // Start background job
            var job = await _backgroundJobService.StartBulkOperationJobAsync(bulkOperationDto);
            
            _logger.LogInformation("Bulk operation job started with ID {JobId}", job.JobId);
            
            return Ok(job);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request parameters");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting bulk add collections job");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get bulk operation job status
    /// </summary>
    [HttpGet("jobs/{jobId}")]
    public async Task<ActionResult<BackgroundJobDto>> GetBulkJobStatus(string jobId)
    {
        try
        {
            if (!ObjectId.TryParse(jobId, out var objectId))
            {
                return BadRequest(new { error = "Invalid job ID format" });
            }
            
            var job = await _backgroundJobService.GetJobAsync(objectId);
            return Ok(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bulk job status for ID {JobId}", jobId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Cancel bulk operation job
    /// </summary>
    [HttpPost("jobs/{jobId}/cancel")]
    public async Task<ActionResult> CancelBulkJob(string jobId)
    {
        try
        {
            if (!ObjectId.TryParse(jobId, out var objectId))
            {
                return BadRequest(new { error = "Invalid job ID format" });
            }
            
            await _backgroundJobService.CancelJobAsync(objectId);
            return Ok(new { message = "Job cancellation requested" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling bulk job for ID {JobId}", jobId);
            return StatusCode(500, "Internal server error");
        }
    }
}
