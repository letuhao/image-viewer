using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Exceptions;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Controller for MediaItem operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class MediaItemsController : ControllerBase
{
    private readonly IMediaItemService _mediaItemService;
    private readonly ILogger<MediaItemsController> _logger;

    public MediaItemsController(IMediaItemService mediaItemService, ILogger<MediaItemsController> logger)
    {
        _mediaItemService = mediaItemService ?? throw new ArgumentNullException(nameof(mediaItemService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new media item
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateMediaItem([FromBody] CreateMediaItemRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!ObjectId.TryParse(request.CollectionId, out var collectionId))
                return BadRequest(new { message = "Invalid collection ID format" });

            var mediaItem = await _mediaItemService.CreateMediaItemAsync(
                collectionId, 
                request.Name, 
                request.Filename, 
                request.Path, 
                request.Type, 
                request.Format, 
                request.FileSize, 
                request.Width, 
                request.Height, 
                request.Duration
            );
            
            return CreatedAtAction(nameof(GetMediaItem), new { id = mediaItem.Id }, mediaItem);
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
            _logger.LogError(ex, "Failed to create media item");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get media item by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetMediaItem(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var mediaItemId))
                return BadRequest(new { message = "Invalid media item ID format" });

            var mediaItem = await _mediaItemService.GetMediaItemByIdAsync(mediaItemId);
            return Ok(mediaItem);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get media item with ID {MediaItemId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get media item by path
    /// </summary>
    [HttpGet("path/{path}")]
    public async Task<IActionResult> GetMediaItemByPath(string path)
    {
        try
        {
            var mediaItem = await _mediaItemService.GetMediaItemByPathAsync(path);
            return Ok(mediaItem);
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
            _logger.LogError(ex, "Failed to get media item at path {Path}", path);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get media items by collection ID
    /// </summary>
    [HttpGet("collection/{collectionId}")]
    public async Task<IActionResult> GetMediaItemsByCollection(string collectionId)
    {
        try
        {
            if (!ObjectId.TryParse(collectionId, out var collectionObjectId))
                return BadRequest(new { message = "Invalid collection ID format" });

            var mediaItems = await _mediaItemService.GetMediaItemsByCollectionIdAsync(collectionObjectId);
            return Ok(mediaItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get media items for collection {CollectionId}", collectionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all media items with pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMediaItems([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var mediaItems = await _mediaItemService.GetMediaItemsAsync(page, pageSize);
            return Ok(mediaItems);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get media items for page {Page} with page size {PageSize}", page, pageSize);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update media item information
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMediaItem(string id, [FromBody] UpdateMediaItemRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var mediaItemId))
                return BadRequest(new { message = "Invalid media item ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var mediaItem = await _mediaItemService.UpdateMediaItemAsync(mediaItemId, request);
            return Ok(mediaItem);
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
            _logger.LogError(ex, "Failed to update media item with ID {MediaItemId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete media item
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMediaItem(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var mediaItemId))
                return BadRequest(new { message = "Invalid media item ID format" });

            await _mediaItemService.DeleteMediaItemAsync(mediaItemId);
            return NoContent();
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete media item with ID {MediaItemId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update media item metadata
    /// </summary>
    [HttpPut("{id}/metadata")]
    public async Task<IActionResult> UpdateMetadata(string id, [FromBody] UpdateMediaItemMetadataRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var mediaItemId))
                return BadRequest(new { message = "Invalid media item ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var mediaItem = await _mediaItemService.UpdateMetadataAsync(mediaItemId, request);
            return Ok(mediaItem);
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
            _logger.LogError(ex, "Failed to update metadata for media item with ID {MediaItemId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update media item cache info
    /// </summary>
    [HttpPut("{id}/cache")]
    public async Task<IActionResult> UpdateCacheInfo(string id, [FromBody] UpdateCacheInfoRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var mediaItemId))
                return BadRequest(new { message = "Invalid media item ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var mediaItem = await _mediaItemService.UpdateCacheInfoAsync(mediaItemId, request);
            return Ok(mediaItem);
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
            _logger.LogError(ex, "Failed to update cache info for media item with ID {MediaItemId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update media item statistics
    /// </summary>
    [HttpPut("{id}/statistics")]
    public async Task<IActionResult> UpdateStatistics(string id, [FromBody] UpdateMediaItemStatisticsRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var mediaItemId))
                return BadRequest(new { message = "Invalid media item ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var mediaItem = await _mediaItemService.UpdateStatisticsAsync(mediaItemId, request);
            return Ok(mediaItem);
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
            _logger.LogError(ex, "Failed to update statistics for media item with ID {MediaItemId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Activate media item
    /// </summary>
    [HttpPost("{id}/activate")]
    public async Task<IActionResult> ActivateMediaItem(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var mediaItemId))
                return BadRequest(new { message = "Invalid media item ID format" });

            var mediaItem = await _mediaItemService.ActivateMediaItemAsync(mediaItemId);
            return Ok(mediaItem);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate media item with ID {MediaItemId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Deactivate media item
    /// </summary>
    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> DeactivateMediaItem(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var mediaItemId))
                return BadRequest(new { message = "Invalid media item ID format" });

            var mediaItem = await _mediaItemService.DeactivateMediaItemAsync(mediaItemId);
            return Ok(mediaItem);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate media item with ID {MediaItemId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Search media items
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchMediaItems([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var mediaItems = await _mediaItemService.SearchMediaItemsAsync(query, page, pageSize);
            return Ok(mediaItems);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search media items with query {Query}", query);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get media item statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetMediaItemStatistics()
    {
        try
        {
            var statistics = await _mediaItemService.GetMediaItemStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get media item statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get top media items by activity
    /// </summary>
    [HttpGet("top-activity")]
    public async Task<IActionResult> GetTopMediaItemsByActivity([FromQuery] int limit = 10)
    {
        try
        {
            var mediaItems = await _mediaItemService.GetTopMediaItemsByActivityAsync(limit);
            return Ok(mediaItems);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top media items by activity");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get recent media items
    /// </summary>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentMediaItems([FromQuery] int limit = 10)
    {
        try
        {
            var mediaItems = await _mediaItemService.GetRecentMediaItemsAsync(limit);
            return Ok(mediaItems);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent media items");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get media items by type
    /// </summary>
    [HttpGet("type/{type}")]
    public async Task<IActionResult> GetMediaItemsByType(string type, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var mediaItems = await _mediaItemService.GetMediaItemsByTypeAsync(type, page, pageSize);
            return Ok(mediaItems);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get media items by type {Type}", type);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get media items by format
    /// </summary>
    [HttpGet("format/{format}")]
    public async Task<IActionResult> GetMediaItemsByFormat(string format, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var mediaItems = await _mediaItemService.GetMediaItemsByFormatAsync(format, page, pageSize);
            return Ok(mediaItems);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get media items by format {Format}", format);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

/// <summary>
/// Request model for creating a media item
/// </summary>
public class CreateMediaItemRequest
{
    public string CollectionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public TimeSpan? Duration { get; set; }
}
