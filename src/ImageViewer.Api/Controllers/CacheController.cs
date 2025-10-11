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
    private readonly ICacheCleanupService _cacheCleanupService;
    // Unified file processing job state (cache, thumbnail, etc.)
    private readonly IFileProcessingJobStateRepository _fileProcessingJobStateRepository;
    private readonly IFileProcessingJobRecoveryService _fileProcessingJobRecoveryService;
    private readonly ILogger<CacheController> _logger;

    public CacheController(
        ICacheService cacheService,
        ICacheFolderRepository cacheFolderRepository,
        ICacheCleanupService cacheCleanupService,
        IFileProcessingJobStateRepository fileProcessingJobStateRepository,
        IFileProcessingJobRecoveryService fileProcessingJobRecoveryService,
        ILogger<CacheController> logger)
    {
        _cacheService = cacheService;
        _cacheFolderRepository = cacheFolderRepository;
        _cacheCleanupService = cacheCleanupService;
        _fileProcessingJobStateRepository = fileProcessingJobStateRepository;
        _fileProcessingJobRecoveryService = fileProcessingJobRecoveryService;
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

    // ============================================================================
    // UNIFIED FILE PROCESSING JOB ENDPOINTS (cache, thumbnail, etc.)
    // ============================================================================

    /// <summary>
    /// Get all file processing job states with optional filtering by job type
    /// </summary>
    [HttpGet("processing-jobs")]
    public async Task<ActionResult<IEnumerable<FileProcessingJobStateDto>>> GetFileProcessingJobs(
        [FromQuery] string? jobType = null,
        [FromQuery] string? status = null,
        [FromQuery] bool includeDetails = false)
    {
        try
        {
            _logger.LogInformation("Getting file processing jobs (jobType: {JobType}, status: {Status})", 
                jobType ?? "all", status ?? "all");
            
            IEnumerable<Domain.Entities.FileProcessingJobState> jobs;
            
            if (!string.IsNullOrEmpty(status))
            {
                if (status.Equals("incomplete", StringComparison.OrdinalIgnoreCase))
                {
                    jobs = !string.IsNullOrEmpty(jobType)
                        ? await _fileProcessingJobStateRepository.GetIncompleteJobsByTypeAsync(jobType)
                        : await _fileProcessingJobStateRepository.GetIncompleteJobsAsync();
                }
                else if (status.Equals("paused", StringComparison.OrdinalIgnoreCase))
                {
                    var pausedJobs = await _fileProcessingJobStateRepository.GetPausedJobsAsync();
                    jobs = !string.IsNullOrEmpty(jobType)
                        ? pausedJobs.Where(j => j.JobType.Equals(jobType, StringComparison.OrdinalIgnoreCase))
                        : pausedJobs;
                }
                else
                {
                    var allJobs = await _fileProcessingJobStateRepository.GetAllAsync();
                    jobs = allJobs.Where(j => j.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
                    
                    if (!string.IsNullOrEmpty(jobType))
                    {
                        jobs = jobs.Where(j => j.JobType.Equals(jobType, StringComparison.OrdinalIgnoreCase));
                    }
                }
            }
            else if (!string.IsNullOrEmpty(jobType))
            {
                jobs = await _fileProcessingJobStateRepository.GetByJobTypeAsync(jobType);
            }
            else
            {
                jobs = await _fileProcessingJobStateRepository.GetAllAsync();
            }

            return Ok(jobs.ToDtoList(includeDetails));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file processing jobs");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get file processing job state by job ID
    /// </summary>
    [HttpGet("processing-jobs/{jobId}")]
    public async Task<ActionResult<FileProcessingJobStateDto>> GetFileProcessingJob(
        string jobId, 
        [FromQuery] bool includeDetails = true)
    {
        try
        {
            var jobState = await _fileProcessingJobStateRepository.GetByJobIdAsync(jobId);
            if (jobState == null)
            {
                return NotFound();
            }

            return Ok(jobState.ToDto(includeDetails));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file processing job {JobId}", jobId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get file processing job state by collection ID (most recent)
    /// </summary>
    [HttpGet("processing-jobs/collection/{collectionId}")]
    public async Task<ActionResult<FileProcessingJobStateDto>> GetFileProcessingJobByCollection(
        string collectionId, 
        [FromQuery] bool includeDetails = false)
    {
        try
        {
            var jobState = await _fileProcessingJobStateRepository.GetByCollectionIdAsync(collectionId);
            if (jobState == null)
            {
                return NotFound();
            }

            return Ok(jobState.ToDto(includeDetails));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file processing job for collection {CollectionId}", collectionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get resumable file processing job IDs with optional filtering by job type
    /// </summary>
    [HttpGet("processing-jobs/resumable")]
    public async Task<ActionResult<IEnumerable<string>>> GetResumableFileProcessingJobs(
        [FromQuery] string? jobType = null)
    {
        try
        {
            var jobIds = !string.IsNullOrEmpty(jobType)
                ? await _fileProcessingJobRecoveryService.GetResumableJobIdsByTypeAsync(jobType)
                : await _fileProcessingJobRecoveryService.GetResumableJobIdsAsync();
            
            return Ok(jobIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resumable file processing jobs");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Resume a specific file processing job
    /// </summary>
    [HttpPost("processing-jobs/{jobId}/resume")]
    public async Task<ActionResult> ResumeFileProcessingJob(string jobId)
    {
        try
        {
            _logger.LogInformation("Resuming file processing job {JobId}", jobId);
            var success = await _fileProcessingJobRecoveryService.ResumeJobAsync(jobId);
            
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
            _logger.LogError(ex, "Error resuming file processing job {JobId}", jobId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Recover all incomplete file processing jobs with optional filtering by job type
    /// </summary>
    [HttpPost("processing-jobs/recover")]
    public async Task<ActionResult> RecoverFileProcessingJobs([FromQuery] string? jobType = null)
    {
        try
        {
            _logger.LogInformation("Recovering incomplete file processing jobs (jobType: {JobType})", jobType ?? "all");
            
            if (!string.IsNullOrEmpty(jobType))
            {
                await _fileProcessingJobRecoveryService.RecoverIncompleteJobsByTypeAsync(jobType);
            }
            else
            {
                await _fileProcessingJobRecoveryService.RecoverIncompleteJobsAsync();
            }
            
            return Ok(new { message = "Job recovery completed", jobType = jobType ?? "all" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recovering file processing jobs");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Cleanup old completed file processing jobs
    /// </summary>
    [HttpDelete("processing-jobs/cleanup")]
    public async Task<ActionResult> CleanupOldFileProcessingJobs([FromQuery] int olderThanDays = 30)
    {
        try
        {
            _logger.LogInformation("Cleaning up completed file processing jobs older than {Days} days", olderThanDays);
            var deletedCount = await _fileProcessingJobRecoveryService.CleanupOldCompletedJobsAsync(olderThanDays);
            return Ok(new { message = "Cleanup completed", deletedCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old file processing jobs");
            return StatusCode(500, "Internal server error");
        }
    }

    // ============================================================================
    // CACHE FILE CLEANUP ENDPOINTS
    // ============================================================================

    /// <summary>
    /// Cleanup orphaned cache files in a specific cache folder
    /// </summary>
    [HttpPost("folders/{cacheFolderPath}/cleanup/cache")]
    public async Task<ActionResult> CleanupOrphanedCacheFiles(
        string cacheFolderPath,
        [FromQuery] int olderThanDays = 7)
    {
        try
        {
            _logger.LogInformation("Cleaning up orphaned cache files in {Path}", cacheFolderPath);
            var deletedCount = await _cacheCleanupService.CleanupOrphanedCacheFilesAsync(cacheFolderPath, olderThanDays);
            return Ok(new { message = "Cache cleanup completed", deletedCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up orphaned cache files");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Cleanup orphaned thumbnail files in a specific cache folder
    /// </summary>
    [HttpPost("folders/{cacheFolderPath}/cleanup/thumbnails")]
    public async Task<ActionResult> CleanupOrphanedThumbnailFiles(
        string cacheFolderPath,
        [FromQuery] int olderThanDays = 7)
    {
        try
        {
            _logger.LogInformation("Cleaning up orphaned thumbnail files in {Path}", cacheFolderPath);
            var deletedCount = await _cacheCleanupService.CleanupOrphanedThumbnailFilesAsync(cacheFolderPath, olderThanDays);
            return Ok(new { message = "Thumbnail cleanup completed", deletedCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up orphaned thumbnail files");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Cleanup all orphaned files (cache + thumbnails) in a cache folder
    /// </summary>
    [HttpPost("folders/{cacheFolderPath}/cleanup/all")]
    public async Task<ActionResult> CleanupAllOrphanedFiles(
        string cacheFolderPath,
        [FromQuery] int olderThanDays = 7)
    {
        try
        {
            _logger.LogInformation("Cleaning up all orphaned files in {Path}", cacheFolderPath);
            var (cacheFiles, thumbnailFiles) = await _cacheCleanupService.CleanupOrphanedFilesAsync(cacheFolderPath, olderThanDays);
            return Ok(new { 
                message = "Full cleanup completed", 
                cacheFiles, 
                thumbnailFiles,
                totalFiles = cacheFiles + thumbnailFiles
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up orphaned files");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Reconcile cache folder statistics with actual disk usage
    /// </summary>
    [HttpPost("folders/{id}/reconcile")]
    public async Task<ActionResult> ReconcileCacheFolderStatistics(string id)
    {
        try
        {
            _logger.LogInformation("Reconciling cache folder statistics for {Id}", id);
            await _cacheCleanupService.ReconcileCacheFolderStatisticsAsync(id);
            return Ok(new { message = "Reconciliation completed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reconciling cache folder statistics");
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
