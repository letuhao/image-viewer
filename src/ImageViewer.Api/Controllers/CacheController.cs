using Microsoft.AspNetCore.Mvc;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.Cache;
using ImageViewer.Application.Mappings;
using ImageViewer.Domain.Interfaces;
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
    private readonly ICacheFolderRepository _cacheFolderRepository;
    private readonly ICacheJobStateRepository _cacheJobStateRepository;
    private readonly ICacheJobRecoveryService _cacheJobRecoveryService;
    private readonly ILogger<CacheController> _logger;

    public CacheController(
        ICacheService cacheService,
        ICacheFolderRepository cacheFolderRepository,
        ICacheJobStateRepository cacheJobStateRepository,
        ICacheJobRecoveryService cacheJobRecoveryService,
        ILogger<CacheController> logger)
    {
        _cacheService = cacheService;
        _cacheFolderRepository = cacheFolderRepository;
        _cacheJobStateRepository = cacheJobStateRepository;
        _cacheJobRecoveryService = cacheJobRecoveryService;
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

    /// <summary>
    /// Get detailed cache folder statistics (enhanced with collection/file counts)
    /// </summary>
    [HttpGet("folders/statistics")]
    public async Task<ActionResult<IEnumerable<CacheFolderStatisticsDto>>> GetCacheFolderStatistics()
    {
        try
        {
            _logger.LogInformation("Getting detailed cache folder statistics");
            var cacheFolders = await _cacheFolderRepository.GetAllAsync();
            var statistics = cacheFolders.ToStatisticsDtoList();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache folder statistics");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get cache folder statistics by ID
    /// </summary>
    [HttpGet("folders/{id}/statistics")]
    public async Task<ActionResult<CacheFolderStatisticsDto>> GetCacheFolderStatisticsById(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return BadRequest("Invalid cache folder ID");
            }

            var cacheFolder = await _cacheFolderRepository.GetByIdAsync(objectId);
            if (cacheFolder == null)
            {
                return NotFound();
            }

            return Ok(cacheFolder.ToStatisticsDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache folder statistics for {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get all cache job states
    /// </summary>
    [HttpGet("jobs")]
    public async Task<ActionResult<IEnumerable<CacheJobStateDto>>> GetCacheJobStates(
        [FromQuery] string? status = null,
        [FromQuery] bool includeDetails = false)
    {
        try
        {
            _logger.LogInformation("Getting cache job states (status: {Status})", status ?? "all");
            
            IEnumerable<Domain.Entities.CacheJobState> jobs;
            
            if (!string.IsNullOrEmpty(status))
            {
                if (status.Equals("incomplete", StringComparison.OrdinalIgnoreCase))
                {
                    jobs = await _cacheJobStateRepository.GetIncompleteJobsAsync();
                }
                else if (status.Equals("paused", StringComparison.OrdinalIgnoreCase))
                {
                    jobs = await _cacheJobStateRepository.GetPausedJobsAsync();
                }
                else
                {
                    // Get all and filter by status
                    var allJobs = await _cacheJobStateRepository.GetAllAsync();
                    jobs = allJobs.Where(j => j.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
                }
            }
            else
            {
                jobs = await _cacheJobStateRepository.GetAllAsync();
            }

            return Ok(jobs.ToDtoList(includeDetails));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache job states");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get cache job state by job ID
    /// </summary>
    [HttpGet("jobs/{jobId}")]
    public async Task<ActionResult<CacheJobStateDto>> GetCacheJobState(string jobId, [FromQuery] bool includeDetails = true)
    {
        try
        {
            var jobState = await _cacheJobStateRepository.GetByJobIdAsync(jobId);
            if (jobState == null)
            {
                return NotFound();
            }

            return Ok(jobState.ToDto(includeDetails));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache job state for {JobId}", jobId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get cache job state by collection ID
    /// </summary>
    [HttpGet("jobs/collection/{collectionId}")]
    public async Task<ActionResult<CacheJobStateDto>> GetCacheJobStateByCollection(string collectionId, [FromQuery] bool includeDetails = false)
    {
        try
        {
            var jobState = await _cacheJobStateRepository.GetByCollectionIdAsync(collectionId);
            if (jobState == null)
            {
                return NotFound();
            }

            return Ok(jobState.ToDto(includeDetails));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache job state for collection {CollectionId}", collectionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get resumable job IDs
    /// </summary>
    [HttpGet("jobs/resumable")]
    public async Task<ActionResult<IEnumerable<string>>> GetResumableJobs()
    {
        try
        {
            var jobIds = await _cacheJobRecoveryService.GetResumableJobIdsAsync();
            return Ok(jobIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resumable jobs");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Resume a specific cache job
    /// </summary>
    [HttpPost("jobs/{jobId}/resume")]
    public async Task<ActionResult> ResumeJob(string jobId)
    {
        try
        {
            _logger.LogInformation("Resuming cache job {JobId}", jobId);
            var success = await _cacheJobRecoveryService.ResumeJobAsync(jobId);
            
            if (success)
            {
                return Ok(new { message = "Job resumed successfully", jobId });
            }
            else
            {
                return BadRequest(new { message = "Failed to resume job", jobId });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming job {JobId}", jobId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Recover all incomplete jobs
    /// </summary>
    [HttpPost("jobs/recover")]
    public async Task<ActionResult> RecoverIncompleteJobs()
    {
        try
        {
            _logger.LogInformation("Recovering all incomplete cache jobs");
            await _cacheJobRecoveryService.RecoverIncompleteJobsAsync();
            return Ok(new { message = "Job recovery completed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recovering incomplete jobs");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Cleanup old completed jobs
    /// </summary>
    [HttpDelete("jobs/cleanup")]
    public async Task<ActionResult> CleanupOldJobs([FromQuery] int olderThanDays = 30)
    {
        try
        {
            _logger.LogInformation("Cleaning up completed jobs older than {Days} days", olderThanDays);
            var deletedCount = await _cacheJobRecoveryService.CleanupOldCompletedJobsAsync(olderThanDays);
            return Ok(new { message = "Cleanup completed", deletedCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old jobs");
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
