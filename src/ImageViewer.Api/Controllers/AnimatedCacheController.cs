using ImageViewer.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageViewer.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AnimatedCacheController : ControllerBase
{
    private readonly IAnimatedCacheRepairService _repairService;
    private readonly ILogger<AnimatedCacheController> _logger;

    public AnimatedCacheController(
        IAnimatedCacheRepairService repairService,
        ILogger<AnimatedCacheController> logger)
    {
        _repairService = repairService ?? throw new ArgumentNullException(nameof(repairService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Find all animated files that have been incorrectly cached
    /// GET: api/v1/animatedcache/scan
    /// </summary>
    [HttpGet("scan")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> ScanIncorrectlyCachedAnimatedFiles()
    {
        try
        {
            _logger.LogInformation("🔍 API: Scanning for incorrectly cached animated files");
            var result = await _repairService.FindIncorrectlyCachedAnimatedFilesAsync();
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error scanning for incorrectly cached animated files");
            return StatusCode(500, new { error = "Failed to scan for incorrectly cached animated files", details = ex.Message });
        }
    }

    /// <summary>
    /// Repair all incorrectly cached animated files
    /// POST: api/v1/animatedcache/repair
    /// </summary>
    [HttpPost("repair")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RepairIncorrectlyCachedAnimatedFiles([FromQuery] bool forceRegenerate = false)
    {
        try
        {
            _logger.LogInformation("🔧 API: Repairing incorrectly cached animated files (forceRegenerate={ForceRegenerate})", forceRegenerate);
            var result = await _repairService.RepairIncorrectlyCachedAnimatedFilesAsync(forceRegenerate);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error repairing incorrectly cached animated files");
            return StatusCode(500, new { error = "Failed to repair incorrectly cached animated files", details = ex.Message });
        }
    }

    /// <summary>
    /// Regenerate all animated file caches
    /// POST: api/v1/animatedcache/regenerate-all
    /// </summary>
    [HttpPost("regenerate-all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RegenerateAllAnimatedCaches()
    {
        try
        {
            _logger.LogInformation("🔄 API: Regenerating all animated file caches");
            var queuedCount = await _repairService.RegenerateAllAnimatedCachesAsync();
            
            return Ok(new 
            { 
                success = true,
                queuedCount = queuedCount,
                message = $"Successfully queued {queuedCount} animated files for cache regeneration"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error regenerating all animated caches");
            return StatusCode(500, new { error = "Failed to regenerate animated caches", details = ex.Message });
        }
    }
}

