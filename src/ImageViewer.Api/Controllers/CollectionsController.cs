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

    public CollectionsController(ICollectionService collectionService, ILogger<CollectionsController> logger)
    {
        _collectionService = collectionService ?? throw new ArgumentNullException(nameof(collectionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
    /// Get thumbnail image for a collection
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
            _logger.LogDebug("Thumbnail request: CollectionId={CollectionId}, ThumbnailId={ThumbnailId}", collectionId, thumbId);

            // Get the collection to find the thumbnail
            var collection = await _collectionService.GetCollectionByIdAsync(collectionId);
            if (collection == null)
            {
                return NotFound(new { message = "Collection not found" });
            }

            // Find the specific thumbnail
            _logger.LogDebug("Collection has {ThumbnailCount} thumbnails", collection.Thumbnails?.Count ?? 0);
            
            var thumbnail = collection.Thumbnails?.FirstOrDefault(t => t.Id == thumbId.ToString());
            if (thumbnail == null)
            {
                _logger.LogWarning("Thumbnail {ThumbnailId} not found in collection {CollectionId}", thumbId, collectionId);
                return NotFound(new { message = "Thumbnail not found" });
            }
            
            if (!thumbnail.IsGenerated)
            {
                _logger.LogWarning("Thumbnail {ThumbnailId} is not generated", thumbId);
                return NotFound(new { message = "Thumbnail not generated" });
            }
            
            if (!thumbnail.IsValid)
            {
                _logger.LogWarning("Thumbnail {ThumbnailId} is invalid", thumbId);
                return NotFound(new { message = "Thumbnail is invalid" });
            }
            
            _logger.LogDebug("Found thumbnail {ThumbnailId} at path: {ThumbnailPath}", thumbId, thumbnail.ThumbnailPath);

            // Check if thumbnail file exists
            if (!System.IO.File.Exists(thumbnail.ThumbnailPath))
            {
                _logger.LogWarning("Thumbnail file not found at path: {ThumbnailPath}", thumbnail.ThumbnailPath);
                return NotFound(new { message = "Thumbnail file not found" });
            }

            // Read and return the thumbnail file
            var fileBytes = await System.IO.File.ReadAllBytesAsync(thumbnail.ThumbnailPath);
            var contentType = GetContentType(thumbnail.Format);

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
    public async Task<IActionResult> GetCollections([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var collections = await _collectionService.GetCollectionsAsync(page, pageSize);
            var allCollections = collections.ToList();
            var overviewDtos = allCollections.Select(c => c.ToOverviewDto()).ToList();
            
            // Create paginated response
            var totalCount = await _collectionService.GetTotalCollectionsCountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            
            var response = new
            {
                data = overviewDtos,
                page = page,
                limit = pageSize,
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