using ImageViewer.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Admin controller for system maintenance operations
/// </summary>
[ApiController]
[Route("api/v1/admin")]
[Authorize] // Add proper admin authorization in the future
public class AdminController : ControllerBase
{
    private readonly IMetadataRecalculationService _metadataRecalculationService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IMetadataRecalculationService metadataRecalculationService,
        ILogger<AdminController> logger)
    {
        _metadataRecalculationService = metadataRecalculationService;
        _logger = logger;
    }

    /// <summary>
    /// Recalculate cache folder metadata based on actual files and collection data
    /// </summary>
    [HttpPost("recalculate-cache-folder-metadata")]
    [ProducesResponseType(typeof(MetadataRecalculationResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> RecalculateCacheFolderMetadata()
    {
        try
        {
            _logger.LogInformation("🔄 Admin requested cache folder metadata recalculation");
            
            var result = await _metadataRecalculationService.RecalculateCacheFolderMetadataAsync();
            
            if (result.Success)
            {
                _logger.LogInformation("✅ Cache folder metadata recalculation completed successfully");
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("⚠️ Cache folder metadata recalculation completed with errors: {ErrorCount}", result.Errors.Count);
                return Ok(result); // Still return 200 but with error details
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during cache folder metadata recalculation");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Recalculate library metadata based on collection statistics
    /// </summary>
    [HttpPost("recalculate-library-metadata")]
    [ProducesResponseType(typeof(MetadataRecalculationResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> RecalculateLibraryMetadata()
    {
        try
        {
            _logger.LogInformation("🔄 Admin requested library metadata recalculation");
            
            var result = await _metadataRecalculationService.RecalculateLibraryMetadataAsync();
            
            if (result.Success)
            {
                _logger.LogInformation("✅ Library metadata recalculation completed successfully");
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("⚠️ Library metadata recalculation completed with errors: {ErrorCount}", result.Errors.Count);
                return Ok(result); // Still return 200 but with error details
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during library metadata recalculation");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Recalculate all metadata (cache folders and libraries)
    /// </summary>
    [HttpPost("recalculate-all-metadata")]
    [ProducesResponseType(typeof(MetadataRecalculationResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> RecalculateAllMetadata()
    {
        try
        {
            _logger.LogInformation("🔄 Admin requested complete metadata recalculation");
            
            var result = await _metadataRecalculationService.RecalculateAllMetadataAsync();
            
            if (result.Success)
            {
                _logger.LogInformation("✅ Complete metadata recalculation completed successfully");
                return Ok(result);
            }
            else
            {
                _logger.LogWarning("⚠️ Complete metadata recalculation completed with errors: {ErrorCount}", result.Errors.Count);
                return Ok(result); // Still return 200 but with error details
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during complete metadata recalculation");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Get system health and metadata status
    /// </summary>
    [HttpGet("system-health")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetSystemHealth()
    {
        try
        {
            // This could be expanded to include more system health information
            var health = new
            {
                timestamp = DateTime.UtcNow,
                status = "healthy",
                services = new
                {
                    api = "running",
                    database = "connected",
                    cache = "operational"
                },
                metadata = new
                {
                    lastRecalculation = "N/A", // Could track this in a system settings table
                    needsRecalculation = true // Could implement logic to detect this
                }
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting system health");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }
}
