using Microsoft.AspNetCore.Mvc;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Exceptions;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Controller for performance optimization operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class PerformanceController : ControllerBase
{
    private readonly IPerformanceService _performanceService;
    private readonly ILogger<PerformanceController> _logger;

    public PerformanceController(IPerformanceService performanceService, ILogger<PerformanceController> logger)
    {
        _performanceService = performanceService ?? throw new ArgumentNullException(nameof(performanceService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get cache information
    /// </summary>
    [HttpGet("cache")]
    public async Task<IActionResult> GetCacheInfo()
    {
        try
        {
            var cacheInfo = await _performanceService.GetCacheInfoAsync();
            return Ok(cacheInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cache info");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Clear cache
    /// </summary>
    [HttpPost("cache/clear")]
    public async Task<IActionResult> ClearCache([FromQuery] string? cacheType = null)
    {
        try
        {
            CacheType? type = null;
            if (!string.IsNullOrEmpty(cacheType) && Enum.TryParse<CacheType>(cacheType, out var parsedType))
            {
                type = parsedType;
            }

            var cacheInfo = await _performanceService.ClearCacheAsync(type);
            return Ok(cacheInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear cache");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Optimize cache
    /// </summary>
    [HttpPost("cache/optimize")]
    public async Task<IActionResult> OptimizeCache()
    {
        try
        {
            var cacheInfo = await _performanceService.OptimizeCacheAsync();
            return Ok(cacheInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to optimize cache");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    [HttpGet("cache/statistics")]
    public async Task<IActionResult> GetCacheStatistics()
    {
        try
        {
            var statistics = await _performanceService.GetCacheStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cache statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get image processing information
    /// </summary>
    [HttpGet("image-processing")]
    public async Task<IActionResult> GetImageProcessingInfo()
    {
        try
        {
            var info = await _performanceService.GetImageProcessingInfoAsync();
            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get image processing info");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Optimize image processing
    /// </summary>
    [HttpPost("image-processing/optimize")]
    public async Task<IActionResult> OptimizeImageProcessing()
    {
        try
        {
            var info = await _performanceService.OptimizeImageProcessingAsync();
            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to optimize image processing");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get image processing statistics
    /// </summary>
    [HttpGet("image-processing/statistics")]
    public async Task<IActionResult> GetImageProcessingStatistics()
    {
        try
        {
            var statistics = await _performanceService.GetImageProcessingStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get image processing statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get database performance information
    /// </summary>
    [HttpGet("database")]
    public async Task<IActionResult> GetDatabasePerformanceInfo()
    {
        try
        {
            var info = await _performanceService.GetDatabasePerformanceInfoAsync();
            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database performance info");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Optimize database queries
    /// </summary>
    [HttpPost("database/optimize")]
    public async Task<IActionResult> OptimizeDatabaseQueries()
    {
        try
        {
            var info = await _performanceService.OptimizeDatabaseQueriesAsync();
            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to optimize database queries");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get database statistics
    /// </summary>
    [HttpGet("database/statistics")]
    public async Task<IActionResult> GetDatabaseStatistics()
    {
        try
        {
            var statistics = await _performanceService.GetDatabaseStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get CDN information
    /// </summary>
    [HttpGet("cdn")]
    public async Task<IActionResult> GetCDNInfo()
    {
        try
        {
            var info = await _performanceService.GetCDNInfoAsync();
            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get CDN info");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Configure CDN
    /// </summary>
    [HttpPost("cdn/configure")]
    public async Task<IActionResult> ConfigureCDN([FromBody] CDNConfigurationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var info = await _performanceService.ConfigureCDNAsync(request);
            return Ok(info);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure CDN");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get CDN statistics
    /// </summary>
    [HttpGet("cdn/statistics")]
    public async Task<IActionResult> GetCDNStatistics()
    {
        try
        {
            var statistics = await _performanceService.GetCDNStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get CDN statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get lazy loading information
    /// </summary>
    [HttpGet("lazy-loading")]
    public async Task<IActionResult> GetLazyLoadingInfo()
    {
        try
        {
            var info = await _performanceService.GetLazyLoadingInfoAsync();
            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get lazy loading info");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Configure lazy loading
    /// </summary>
    [HttpPost("lazy-loading/configure")]
    public async Task<IActionResult> ConfigureLazyLoading([FromBody] LazyLoadingConfigurationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var info = await _performanceService.ConfigureLazyLoadingAsync(request);
            return Ok(info);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure lazy loading");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get lazy loading statistics
    /// </summary>
    [HttpGet("lazy-loading/statistics")]
    public async Task<IActionResult> GetLazyLoadingStatistics()
    {
        try
        {
            var statistics = await _performanceService.GetLazyLoadingStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get lazy loading statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get performance metrics
    /// </summary>
    [HttpGet("metrics")]
    public async Task<IActionResult> GetPerformanceMetrics()
    {
        try
        {
            var metrics = await _performanceService.GetPerformanceMetricsAsync();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance metrics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get performance metrics by time range
    /// </summary>
    [HttpGet("metrics/range")]
    public async Task<IActionResult> GetPerformanceMetricsByTimeRange([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
    {
        try
        {
            var metrics = await _performanceService.GetPerformanceMetricsByTimeRangeAsync(fromDate, toDate);
            return Ok(metrics);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance metrics for time range");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Generate performance report
    /// </summary>
    [HttpPost("report")]
    public async Task<IActionResult> GeneratePerformanceReport([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var report = await _performanceService.GeneratePerformanceReportAsync(fromDate, toDate);
            return Ok(report);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate performance report");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
