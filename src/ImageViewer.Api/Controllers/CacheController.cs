using Microsoft.AspNetCore.Mvc;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.Cache;
using MongoDB.Bson;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Cache management controller
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class CacheController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheController> _logger;

    public CacheController(ICacheService cacheService, ILogger<CacheController> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<CacheStatisticsDto>> GetCacheStatistics()
    {
        try
        {
            _logger.LogInformation("Getting cache statistics");
            var statistics = await _cacheService.GetCacheStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get all cache folders
    /// </summary>
    [HttpGet("folders")]
    public async Task<ActionResult<IEnumerable<CacheFolderDto>>> GetCacheFolders()
    {
        try
        {
            _logger.LogInformation("Getting cache folders");
            var folders = await _cacheService.GetCacheFoldersAsync();
            return Ok(folders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache folders");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get cache folder by ID
    /// </summary>
    [HttpGet("folders/{id}")]
    public async Task<ActionResult<CacheFolderDto>> GetCacheFolder(ObjectId id)
    {
        try
        {
            _logger.LogInformation("Getting cache folder with ID: {Id}", id);
            var folder = await _cacheService.GetCacheFolderAsync(id);
            if (folder == null)
                return NotFound($"Cache folder with ID {id} not found");

            return Ok(folder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache folder with ID: {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create new cache folder
    /// </summary>
    [HttpPost("folders")]
    public async Task<ActionResult<CacheFolderDto>> CreateCacheFolder([FromBody] CreateCacheFolderDto dto)
    {
        try
        {
            _logger.LogInformation("Creating cache folder: {Name}", dto.Name);
            var folder = await _cacheService.CreateCacheFolderAsync(dto);
            return CreatedAtAction(nameof(GetCacheFolder), new { id = folder.Id }, folder);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid cache folder data");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cache folder");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update cache folder
    /// </summary>
    [HttpPut("folders/{id}")]
    public async Task<ActionResult<CacheFolderDto>> UpdateCacheFolder(ObjectId id, [FromBody] UpdateCacheFolderDto dto)
    {
        try
        {
            _logger.LogInformation("Updating cache folder with ID: {Id}", id);
            var folder = await _cacheService.UpdateCacheFolderAsync(id, dto);
            return Ok(folder);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Cache folder not found: {Id}", id);
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid cache folder data for ID: {Id}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cache folder with ID: {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete cache folder
    /// </summary>
    [HttpDelete("folders/{id}")]
    public async Task<ActionResult> DeleteCacheFolder(ObjectId id)
    {
        try
        {
            _logger.LogInformation("Deleting cache folder with ID: {Id}", id);
            await _cacheService.DeleteCacheFolderAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Cache folder not found: {Id}", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cache folder with ID: {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Clear cache for specific collection
    /// </summary>
    [HttpPost("collections/{collectionId}/clear")]
    public async Task<ActionResult> ClearCollectionCache(ObjectId collectionId)
    {
        try
        {
            _logger.LogInformation("Clearing cache for collection: {CollectionId}", collectionId);
            await _cacheService.ClearCollectionCacheAsync(collectionId);
            return Ok(new { message = "Cache cleared successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Collection not found: {CollectionId}", collectionId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache for collection: {CollectionId}", collectionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Clear all cache
    /// </summary>
    [HttpPost("clear-all")]
    public async Task<ActionResult> ClearAllCache()
    {
        try
        {
            _logger.LogInformation("Clearing all cache");
            await _cacheService.ClearAllCacheAsync();
            return Ok(new { message = "All cache cleared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all cache");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get cache status for collection
    /// </summary>
    [HttpGet("collections/{collectionId}/status")]
    public async Task<ActionResult<CollectionCacheStatusDto>> GetCollectionCacheStatus(ObjectId collectionId)
    {
        try
        {
            _logger.LogInformation("Getting cache status for collection: {CollectionId}", collectionId);
            var status = await _cacheService.GetCollectionCacheStatusAsync(collectionId);
            return Ok(status);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Collection not found: {CollectionId}", collectionId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache status for collection: {CollectionId}", collectionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Regenerate cache for collection
    /// </summary>
    [HttpPost("collections/{collectionId}/regenerate")]
    public async Task<ActionResult> RegenerateCollectionCache(ObjectId collectionId, [FromBody] RegenerateCacheRequest? request = null)
    {
        try
        {
            _logger.LogInformation("Regenerating cache for collection: {CollectionId}", collectionId);
            if (request != null)
            {
                var sizes = new List<(int Width, int Height)>();
                if (request.Sizes != null && request.Sizes.Any())
                {
                    sizes.AddRange(request.Sizes.Select(s => (s.Width, s.Height)));
                }

                if (!string.IsNullOrWhiteSpace(request.Preset))
                {
                    // Resolve preset from options via a scoped service
                    var presetsOptions = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<ImageViewer.Application.Options.ImageCachePresetsOptions>>().Value;
                    if (presetsOptions.Presets.TryGetValue(request.Preset, out var presetSizes))
                    {
                        sizes.AddRange(presetSizes.Select(s => (s.Width, s.Height)));
                    }
                }

                if (sizes.Any())
                {
                    await _cacheService.RegenerateCollectionCacheAsync(collectionId, sizes);
                }
                else
                {
                    await _cacheService.RegenerateCollectionCacheAsync(collectionId);
                }
            }
            else
            {
                await _cacheService.RegenerateCollectionCacheAsync(collectionId);
            }
            return Ok(new { message = "Cache regeneration initiated" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Collection not found: {CollectionId}", collectionId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating cache for collection: {CollectionId}", collectionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get cache folder distribution statistics
    /// </summary>
    /// <returns>Cache distribution statistics</returns>
    [HttpGet("distribution")]
    public async Task<ActionResult<CacheDistributionStatisticsDto>> GetCacheDistributionStatistics()
    {
        try
        {
            _logger.LogInformation("Getting cache folder distribution statistics");
            var statistics = await _cacheService.GetCacheDistributionStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache distribution statistics");
            return StatusCode(500, "Internal server error");
        }
    }
}

public class RegenerateCacheRequest
{
    public string? Preset { get; set; }
    public List<CacheSizeDto>? Sizes { get; set; }
}

public class CacheSizeDto
{
    public int Width { get; set; }
    public int Height { get; set; }
}
