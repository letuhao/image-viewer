using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ImageViewer.Application.Services;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Bulk operations controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BulkController : ControllerBase
{
    private readonly IBulkService _bulkService;
    private readonly ILogger<BulkController> _logger;

    public BulkController(IBulkService bulkService, ILogger<BulkController> logger)
    {
        _bulkService = bulkService;
        _logger = logger;
    }

    /// <summary>
    /// Bulk add collections from parent directory
    /// </summary>
    [HttpPost("collections")]
    public async Task<ActionResult<BulkOperationResult>> BulkAddCollections([FromBody] BulkAddCollectionsRequest request)
    {
        try
        {
            _logger.LogInformation("Starting bulk add collections from parent path {ParentPath}", request.ParentPath);
            
            var result = await _bulkService.BulkAddCollectionsAsync(request);
            
            _logger.LogInformation("Bulk operation completed. Success: {Success}, Created: {Created}, Updated: {Updated}, Skipped: {Skipped}, Errors: {Errors}, Scanned: {Scanned}", 
                result.SuccessCount, result.CreatedCount, result.UpdatedCount, result.SkippedCount, result.ErrorCount, result.ScannedCount);
            
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request parameters");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk add collections");
            return StatusCode(500, "Internal server error");
        }
    }
}
