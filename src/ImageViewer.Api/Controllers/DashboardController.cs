using ImageViewer.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Dashboard API Controller with Redis caching for ultra-fast loading
/// 中文：仪表板API控制器，使用Redis缓存实现超快速加载
/// Tiếng Việt: Bộ điều khiển API bảng điều khiển với bộ nhớ đệm Redis để tải nhanh
/// </summary>
[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardStatisticsService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardStatisticsService dashboardService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard statistics (Redis-cached for ultra-fast loading)
    /// </summary>
    /// <returns>Dashboard statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(DashboardStatistics), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetDashboardStatistics()
    {
        try
        {
            _logger.LogInformation("Getting dashboard statistics");
            
            var statistics = await _dashboardService.GetDashboardStatisticsAsync();
            
            _logger.LogDebug("✅ Dashboard statistics retrieved successfully");
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dashboard statistics");
            return StatusCode(500, new { message = "Failed to get dashboard statistics", error = ex.Message });
        }
    }

    /// <summary>
    /// Get recent dashboard activity
    /// </summary>
    /// <param name="limit">Number of activities to return (default: 10)</param>
    /// <returns>Recent activity list</returns>
    [HttpGet("activity")]
    [ProducesResponseType(typeof(List<object>), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetRecentActivity([FromQuery] int limit = 10)
    {
        try
        {
            if (limit < 1 || limit > 100)
            {
                return BadRequest(new { message = "Limit must be between 1 and 100" });
            }

            _logger.LogInformation("Getting recent dashboard activity (limit: {Limit})", limit);
            
            var activity = await _dashboardService.GetRecentActivityAsync(limit);
            
            _logger.LogDebug("✅ Recent activity retrieved successfully");
            return Ok(activity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent activity");
            return StatusCode(500, new { message = "Failed to get recent activity", error = ex.Message });
        }
    }

    /// <summary>
    /// Check if dashboard statistics are fresh
    /// </summary>
    /// <returns>Freshness status</returns>
    [HttpGet("fresh")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> IsStatisticsFresh()
    {
        try
        {
            _logger.LogDebug("Checking dashboard statistics freshness");
            
            var isFresh = await _dashboardService.IsStatisticsFreshAsync();
            
            return Ok(new { isFresh, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check statistics freshness");
            return StatusCode(500, new { message = "Failed to check freshness", error = ex.Message });
        }
    }

    /// <summary>
    /// Force refresh dashboard statistics (admin only)
    /// </summary>
    /// <returns>Refresh status</returns>
    [HttpPost("refresh")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> RefreshStatistics()
    {
        try
        {
            _logger.LogInformation("Force refreshing dashboard statistics");
            
            // Force refresh by updating metadata
            await _dashboardService.UpdateDashboardStatisticsAsync("force_refresh", new { requestedBy = User.Identity?.Name });
            
            _logger.LogInformation("✅ Dashboard statistics refresh requested");
            return Ok(new { message = "Dashboard statistics refresh requested", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh dashboard statistics");
            return StatusCode(500, new { message = "Failed to refresh statistics", error = ex.Message });
        }
    }
}
