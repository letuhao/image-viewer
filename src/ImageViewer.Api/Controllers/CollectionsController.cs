using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Application.Mappings;
using ImageViewer.Domain.Exceptions;
using ImageViewer.Domain.Enums;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Controller for Collection operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class CollectionsController : ControllerBase
{
    private readonly ICollectionService _collectionService;
    private readonly ILogger<CollectionsController> _logger;
    private readonly ImageViewer.Domain.Interfaces.IImageCacheService _imageCacheService;
    private readonly ImageViewer.Application.Services.IThumbnailCacheService _thumbnailCacheService;
    private readonly ImageViewer.Domain.Interfaces.ICollectionIndexService _collectionIndexService;

    public CollectionsController(
        ICollectionService collectionService, 
        ILogger<CollectionsController> logger,
        ImageViewer.Domain.Interfaces.IImageCacheService imageCacheService,
        ImageViewer.Application.Services.IThumbnailCacheService thumbnailCacheService,
        ImageViewer.Domain.Interfaces.ICollectionIndexService collectionIndexService)
    {
        _collectionService = collectionService ?? throw new ArgumentNullException(nameof(collectionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _imageCacheService = imageCacheService ?? throw new ArgumentNullException(nameof(imageCacheService));
        _thumbnailCacheService = thumbnailCacheService ?? throw new ArgumentNullException(nameof(thumbnailCacheService));
        _collectionIndexService = collectionIndexService ?? throw new ArgumentNullException(nameof(collectionIndexService));
    }

    /// <summary>
    /// Create a new collection
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCollection([FromBody] CreateCollectionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!ObjectId.TryParse(request.LibraryId, out var libraryId))
                return BadRequest(new { message = "Invalid library ID format" });

            if (!Enum.TryParse<CollectionType>(request.Type, out var collectionType))
                return BadRequest(new { message = "Invalid collection type" });

            var collection = await _collectionService.CreateCollectionAsync(libraryId, request.Name, request.Path, collectionType, request.Description);
            return CreatedAtAction(nameof(GetCollection), new { id = collection.Id }, collection);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DuplicateEntityException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create collection");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get thumbnail image for a collection (with Redis caching)
    /// </summary>
    [HttpGet("{id}/thumbnails/{thumbnailId}")]
    public async Task<IActionResult> GetCollectionThumbnail(string id, string thumbnailId)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var collectionId))
            {
                return BadRequest(new { message = "Invalid collection ID format" });
            }

            if (!ObjectId.TryParse(thumbnailId, out var thumbId))
            {
                return BadRequest(new { message = "Invalid thumbnail ID format" });
            }

            _logger.LogInformation("Getting thumbnail {ThumbnailId} for collection {CollectionId}", thumbnailId, id);

            // Generate cache key
            var cacheKey = _imageCacheService.GetThumbnailCacheKey(id, thumbnailId);

            // Try to get from Redis cache first
            var cachedData = await _imageCacheService.GetCachedImageAsync(cacheKey);
            if (cachedData != null)
            {
                _logger.LogDebug("Serving thumbnail {ThumbnailId} from Redis cache", thumbnailId);
                return base.File(cachedData, "image/jpeg"); // Assume JPEG for cached thumbnails
            }

            // Cache miss - get from database and disk
            _logger.LogDebug("Thumbnail {ThumbnailId} not in cache, loading from disk", thumbnailId);

            // Get the collection to find the thumbnail
            var collection = await _collectionService.GetCollectionByIdAsync(collectionId);
            if (collection == null)
            {
                return NotFound(new { message = "Collection not found" });
            }

            // Find the specific thumbnail
            var thumbnail = collection.Thumbnails?.FirstOrDefault(t => t.Id == thumbId.ToString());
            if (thumbnail == null)
            {
                _logger.LogWarning("Thumbnail {ThumbnailId} not found in collection {CollectionId}", thumbId, collectionId);
                return NotFound(new { message = "Thumbnail not found" });
            }
            
            if (!thumbnail.IsGenerated || !thumbnail.IsValid)
            {
                _logger.LogWarning("Thumbnail {ThumbnailId} is not ready (Generated: {IsGenerated}, Valid: {IsValid})", 
                    thumbId, thumbnail.IsGenerated, thumbnail.IsValid);
                return NotFound(new { message = "Thumbnail not available" });
            }

            // Check if thumbnail file exists
            if (!System.IO.File.Exists(thumbnail.ThumbnailPath))
            {
                _logger.LogWarning("Thumbnail file not found at path: {ThumbnailPath}", thumbnail.ThumbnailPath);
                return NotFound(new { message = "Thumbnail file not found" });
            }

            // Read thumbnail file
            var fileBytes = await System.IO.File.ReadAllBytesAsync(thumbnail.ThumbnailPath);
            var contentType = GetContentType(thumbnail.Format);

            // Cache in Redis for future requests
            await _imageCacheService.SetCachedImageAsync(cacheKey, fileBytes);
            _logger.LogDebug("Cached thumbnail {ThumbnailId} in Redis", thumbnailId);

            // Update access statistics
            thumbnail.UpdateAccess();

            return base.File(fileBytes, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get thumbnail {ThumbnailId} for collection {CollectionId}", thumbnailId, id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get collection by ID (detailed view with all embedded data)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCollection(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var collectionId))
                return BadRequest(new { message = "Invalid collection ID format" });

            var collection = await _collectionService.GetCollectionByIdAsync(collectionId);
            var detailDto = collection.ToDetailDto();
            return Ok(detailDto);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collection with ID {CollectionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
    
    /// <summary>
    /// Get collection overview by ID (lightweight, no embedded data)
    /// </summary>
    [HttpGet("{id}/overview")]
    public async Task<IActionResult> GetCollectionOverview(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var collectionId))
                return BadRequest(new { message = "Invalid collection ID format" });

            var collection = await _collectionService.GetCollectionByIdAsync(collectionId);
            var overviewDto = collection.ToOverviewDto();
            return Ok(overviewDto);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collection overview with ID {CollectionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
    
    /// <summary>
    /// TEST: Get collections list with DTOs
    /// </summary>
    [HttpGet("test-dto")]
    public async Task<IActionResult> GetCollectionsTestDto()
    {
        _logger.LogWarning("TEST ENDPOINT CALLED");
        var collections = await _collectionService.GetCollectionsAsync(1, 2);
        var dtos = collections.Select(c => c.ToOverviewDto()).ToList();
        _logger.LogWarning("Returning {Count} DTOs, first type: {Type}", dtos.Count, dtos.FirstOrDefault()?.GetType().FullName);
        return Ok(dtos);
    }

    /// <summary>
    /// Get collection by path
    /// </summary>
    [HttpGet("path/{path}")]
    public async Task<IActionResult> GetCollectionByPath(string path)
    {
        try
        {
            var collection = await _collectionService.GetCollectionByPathAsync(path);
            return Ok(collection);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collection at path {Path}", path);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get collections by library ID
    /// </summary>
    [HttpGet("library/{libraryId}")]
    public async Task<IActionResult> GetCollectionsByLibrary(string libraryId)
    {
        try
        {
            if (!ObjectId.TryParse(libraryId, out var libraryObjectId))
                return BadRequest(new { message = "Invalid library ID format" });

            var collections = await _collectionService.GetCollectionsByLibraryIdAsync(libraryObjectId);
            return Ok(collections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collections for library {LibraryId}", libraryId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all collections with pagination (returns lightweight overview DTOs)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCollections(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20,
        [FromQuery] int? limit = null, // Support both pageSize and limit for compatibility
        [FromQuery] string sortBy = "updatedAt",
        [FromQuery] string sortDirection = "desc")
    {
        try
        {
            // Use limit if provided, otherwise fall back to pageSize
            var effectivePageSize = limit ?? pageSize;
            var collections = await _collectionService.GetCollectionsAsync(page, effectivePageSize, sortBy, sortDirection);
            var allCollections = collections.ToList();
            var overviewDtos = allCollections.Select(c => c.ToOverviewDto()).ToList();
            
            // Populate base64 thumbnails in parallel for instant display (highest performance)
            var thumbnailTasks = allCollections.Select(async (collection, index) =>
            {
                var thumbnail = collection.GetCollectionThumbnail();
                if (thumbnail != null)
                {
                    var base64 = await _thumbnailCacheService.GetThumbnailAsBase64Async(
                        collection.Id.ToString(), 
                        thumbnail);
                    overviewDtos[index].ThumbnailBase64 = base64;
                }
            });
            
            await Task.WhenAll(thumbnailTasks);
            _logger.LogDebug("Populated {Count} thumbnails as base64", overviewDtos.Count(d => d.ThumbnailBase64 != null));
            
            // Create paginated response
            var totalCount = await _collectionService.GetTotalCollectionsCountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / effectivePageSize);
            
            var response = new
            {
                data = overviewDtos,
                page = page,
                limit = effectivePageSize, // Return the actual limit used
                total = totalCount,
                totalPages = totalPages,
                hasNext = page < totalPages,
                hasPrevious = page > 1
            };
            
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collections for page {Page} with page size {PageSize}", page, pageSize);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update collection information
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCollection(string id, [FromBody] UpdateCollectionRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var collectionId))
                return BadRequest(new { message = "Invalid collection ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var collection = await _collectionService.UpdateCollectionAsync(collectionId, request);
            return Ok(collection);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (DuplicateEntityException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update collection with ID {CollectionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete collection
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCollection(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var collectionId))
                return BadRequest(new { message = "Invalid collection ID format" });

            await _collectionService.DeleteCollectionAsync(collectionId);
            return NoContent();
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete collection with ID {CollectionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update collection settings
    /// </summary>
    [HttpPut("{id}/settings")]
    public async Task<IActionResult> UpdateSettings(string id, [FromBody] UpdateCollectionSettingsRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var collectionId))
                return BadRequest(new { message = "Invalid collection ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var collection = await _collectionService.UpdateSettingsAsync(collectionId, request);
            return Ok(collection);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update settings for collection with ID {CollectionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update collection metadata
    /// </summary>
    [HttpPut("{id}/metadata")]
    public async Task<IActionResult> UpdateMetadata(string id, [FromBody] UpdateCollectionMetadataRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var collectionId))
                return BadRequest(new { message = "Invalid collection ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var collection = await _collectionService.UpdateMetadataAsync(collectionId, request);
            return Ok(collection);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update metadata for collection with ID {CollectionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update collection statistics
    /// </summary>
    [HttpPut("{id}/statistics")]
    public async Task<IActionResult> UpdateStatistics(string id, [FromBody] UpdateCollectionStatisticsRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var collectionId))
                return BadRequest(new { message = "Invalid collection ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var collection = await _collectionService.UpdateStatisticsAsync(collectionId, request);
            return Ok(collection);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update statistics for collection with ID {CollectionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Activate collection
    /// </summary>
    [HttpPost("{id}/activate")]
    public async Task<IActionResult> ActivateCollection(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var collectionId))
                return BadRequest(new { message = "Invalid collection ID format" });

            var collection = await _collectionService.ActivateCollectionAsync(collectionId);
            return Ok(collection);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate collection with ID {CollectionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Deactivate collection
    /// </summary>
    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> DeactivateCollection(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var collectionId))
                return BadRequest(new { message = "Invalid collection ID format" });

            var collection = await _collectionService.DeactivateCollectionAsync(collectionId);
            return Ok(collection);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate collection with ID {CollectionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Enable collection watching
    /// </summary>
    [HttpPost("{id}/enable-watching")]
    public async Task<IActionResult> EnableWatching(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var collectionId))
                return BadRequest(new { message = "Invalid collection ID format" });

            var collection = await _collectionService.EnableWatchingAsync(collectionId);
            return Ok(collection);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable watching for collection with ID {CollectionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Disable collection watching
    /// </summary>
    [HttpPost("{id}/disable-watching")]
    public async Task<IActionResult> DisableWatching(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var collectionId))
                return BadRequest(new { message = "Invalid collection ID format" });

            var collection = await _collectionService.DisableWatchingAsync(collectionId);
            return Ok(collection);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable watching for collection with ID {CollectionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update watch settings
    /// </summary>
    [HttpPut("{id}/watch-settings")]
    public async Task<IActionResult> UpdateWatchSettings(string id, [FromBody] UpdateWatchSettingsRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var collectionId))
                return BadRequest(new { message = "Invalid collection ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var collection = await _collectionService.UpdateWatchSettingsAsync(collectionId, request);
            return Ok(collection);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update watch settings for collection with ID {CollectionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Search collections
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchCollections([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var collections = await _collectionService.SearchCollectionsAsync(query, page, pageSize);
            return Ok(collections);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search collections with query {Query}", query);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get collection statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetCollectionStatistics()
    {
        try
        {
            var statistics = await _collectionService.GetCollectionStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collection statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get top collections by activity
    /// </summary>
    [HttpGet("top-activity")]
    public async Task<IActionResult> GetTopCollectionsByActivity([FromQuery] int limit = 10)
    {
        try
        {
            var collections = await _collectionService.GetTopCollectionsByActivityAsync(limit);
            return Ok(collections);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top collections by activity");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get recent collections
    /// </summary>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentCollections([FromQuery] int limit = 10)
    {
        try
        {
            var collections = await _collectionService.GetRecentCollectionsAsync(limit);
            return Ok(collections);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent collections");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get collections by type
    /// </summary>
    [HttpGet("type/{type}")]
    public async Task<IActionResult> GetCollectionsByType(string type, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            if (!Enum.TryParse<CollectionType>(type, out var collectionType))
                return BadRequest(new { message = "Invalid collection type" });

            var collections = await _collectionService.GetCollectionsByTypeAsync(collectionType, page, pageSize);
            return Ok(collections);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collections by type {Type}", type);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get content type for thumbnail format
    /// </summary>
    private static string GetContentType(string format)
    {
        return format.ToLower() switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "webp" => "image/webp",
            "gif" => "image/gif",
            "bmp" => "image/bmp",
            _ => "image/jpeg" // Default fallback
        };
    }

    #region Collection Navigation

    /// <summary>
    /// Get navigation info for a collection (previous/next IDs and position)
    /// </summary>
    [HttpGet("{id}/navigation")]
    public async Task<IActionResult> GetCollectionNavigation(
        string id,
        [FromQuery] string sortBy = "updatedAt",
        [FromQuery] string sortDirection = "desc")
    {
        try
        {
            if (!ObjectId.TryParse(id, out var collectionId))
            {
                return BadRequest(new { message = "Invalid collection ID format" });
            }

            var navigation = await _collectionService.GetCollectionNavigationAsync(collectionId, sortBy, sortDirection);
            return Ok(navigation);
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Business rule violation getting navigation for collection {CollectionId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get navigation for collection {CollectionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get sibling collections for navigation sidebar
    /// </summary>
    [HttpGet("{id}/siblings")]
    public async Task<IActionResult> GetCollectionSiblings(
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "updatedAt",
        [FromQuery] string sortDirection = "desc")
    {
        try
        {
            if (!ObjectId.TryParse(id, out var collectionId))
            {
                return BadRequest(new { message = "Invalid collection ID format" });
            }

            var siblingsResult = await _collectionService.GetCollectionSiblingsAsync(collectionId, page, pageSize, sortBy, sortDirection);
            
            // Load thumbnails for siblings (same as collection list)
            if (siblingsResult.Siblings.Any())
            {
                var thumbnailTasks = siblingsResult.Siblings.Select(async (sibling, index) =>
                {
                    // Get full collection to access thumbnail data
                    if (ObjectId.TryParse(sibling.Id, out var siblingId))
                    {
                        var collection = await _collectionService.GetCollectionByIdAsync(siblingId);
                        if (collection != null)
                        {
                            var thumbnail = collection.GetCollectionThumbnail();
                            if (thumbnail != null)
                            {
                                var base64 = await _thumbnailCacheService.GetThumbnailAsBase64Async(
                                    collection.Id.ToString(),
                                    thumbnail);
                                siblingsResult.Siblings[index].ThumbnailBase64 = base64;
                            }
                        }
                    }
                });
                
                await Task.WhenAll(thumbnailTasks);
                _logger.LogDebug("Populated {Count} sibling thumbnails", siblingsResult.Siblings.Count(s => s.ThumbnailBase64 != null));
            }
            
            return Ok(siblingsResult);
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Business rule violation getting siblings for collection {CollectionId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get siblings for collection {CollectionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    #endregion

    #region Redis Index Management

    /// <summary>
    /// Get Redis collection index statistics
    /// </summary>
    [HttpGet("index/stats")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetIndexStats()
    {
        try
        {
            var stats = await _collectionIndexService.GetIndexStatsAsync();
            var isValid = await _collectionIndexService.IsIndexValidAsync();
            
            return Ok(new
            {
                totalCollections = stats.TotalCollections,
                lastRebuildTime = stats.LastRebuildTime,
                isValid = isValid,
                redisConnected = true // If we got here, Redis is connected
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Redis index stats");
            return StatusCode(500, new { message = "Failed to get index statistics", error = ex.Message });
        }
    }

    /// <summary>
    /// Validate Redis collection index
    /// </summary>
    [HttpGet("index/validate")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> ValidateIndex()
    {
        try
        {
            var isValid = await _collectionIndexService.IsIndexValidAsync();
            _logger.LogInformation("Redis index validation result: {IsValid}", isValid);
            
            return Ok(new { isValid = isValid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Redis index");
            return StatusCode(500, new { message = "Failed to validate index", error = ex.Message });
        }
    }

    /// <summary>
    /// Rebuild Redis collection index
    /// </summary>
    [HttpPost("index/rebuild")]
    [ProducesResponseType(200)]
    public IActionResult RebuildIndex()
    {
        try
        {
            _logger.LogInformation("🔄 Starting Redis index rebuild (background task)");
            
            // Start rebuild in background task
            _ = Task.Run(async () =>
            {
                try
                {
                    await _collectionIndexService.RebuildIndexAsync();
                    _logger.LogInformation("✅ Redis index rebuild completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Redis index rebuild failed");
                }
            });
            
            return Ok(new { message = "Index rebuild started in background" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Redis index rebuild");
            return StatusCode(500, new { message = "Failed to start index rebuild", error = ex.Message });
        }
    }

    /// <summary>
    /// Clean up collections that no longer exist on disk (archives them for backup)
    /// </summary>
    [HttpPost("cleanup")]
    public async Task<IActionResult> CleanupNonExistentCollections()
    {
        try
        {
            _logger.LogInformation("Starting collection cleanup operation");
            
            var result = await _collectionService.CleanupNonExistentCollectionsAsync();
            
            _logger.LogInformation("Collection cleanup completed. Archived {ArchivedCount} collections", 
                result.CollectionsDeleted);
            
            return Ok(new
            {
                message = "Collection cleanup completed successfully",
                result.TotalCollectionsChecked,
                result.NonExistentCollectionsFound,
                result.CollectionsDeleted,
                result.Errors,
                result.Duration,
                DeletedPaths = result.DeletedCollectionPaths.Take(10), // Show first 10 paths
                ErrorMessages = result.ErrorMessages.Take(5) // Show first 5 errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup non-existent collections");
            return StatusCode(500, new { message = "Collection cleanup failed", error = ex.Message });
        }
    }

    #endregion
}

/// <summary>
/// Request model for creating a collection
/// </summary>
public class CreateCollectionRequest
{
    public string LibraryId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
}