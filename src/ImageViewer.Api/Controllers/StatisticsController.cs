using Microsoft.AspNetCore.Mvc;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.Statistics;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Statistics controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(IStatisticsService statisticsService, ILogger<StatisticsController> logger)
    {
        _statisticsService = statisticsService;
        _logger = logger;
    }

    /// <summary>
    /// Get overall system statistics
    /// </summary>
    [HttpGet("overall")]
    public async Task<ActionResult<SystemStatisticsDto>> GetOverallStatistics()
    {
        try
        {
            _logger.LogInformation("Getting overall system statistics");
            var statistics = await _statisticsService.GetSystemStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overall statistics");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get statistics for specific collection
    /// </summary>
    [HttpGet("collections/{collectionId}")]
    public async Task<ActionResult<CollectionStatisticsDto>> GetCollectionStatistics(Guid collectionId)
    {
        try
        {
            _logger.LogInformation("Getting statistics for collection: {CollectionId}", collectionId);
            var statistics = await _statisticsService.GetCollectionStatisticsAsync(collectionId);
            return Ok(statistics);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Collection not found: {CollectionId}", collectionId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics for collection: {CollectionId}", collectionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get statistics for specific image
    /// </summary>
    [HttpGet("images/{imageId}")]
    public async Task<ActionResult<ImageStatisticsDto>> GetImageStatistics(Guid imageId)
    {
        try
        {
            _logger.LogInformation("Getting statistics for image: {ImageId}", imageId);
            var statistics = await _statisticsService.GetImageStatisticsAsync(imageId);
            return Ok(statistics);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Image not found: {ImageId}", imageId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics for image: {ImageId}", imageId);
            return StatusCode(500, "Internal server error");
        }
    }
}
