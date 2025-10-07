using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ImageViewer.Application.Services;
using MongoDB.Bson;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Thumbnails controller for advanced thumbnail operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class ThumbnailsController : ControllerBase
{
    private readonly IAdvancedThumbnailService _thumbnailService;
    private readonly ILogger<ThumbnailsController> _logger;

    public ThumbnailsController(IAdvancedThumbnailService thumbnailService, ILogger<ThumbnailsController> logger)
    {
        _thumbnailService = thumbnailService;
        _logger = logger;
    }

    /// <summary>
    /// Generate thumbnail for collection
    /// </summary>
    [HttpPost("collections/{collectionId}/generate")]
    public async Task<ActionResult<ThumbnailGenerationResponse>> GenerateCollectionThumbnail(ObjectId collectionId)
    {
        try
        {
            _logger.LogInformation("Generating thumbnail for collection {CollectionId}", collectionId);
            
            var thumbnailPath = await _thumbnailService.GenerateCollectionThumbnailAsync(collectionId);
            
            if (thumbnailPath == null)
            {
                return NotFound(new { error = "Collection not found or no suitable image for thumbnail" });
            }

            var response = new ThumbnailGenerationResponse
            {
                CollectionId = collectionId,
                ThumbnailPath = thumbnailPath,
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail for collection {CollectionId}", collectionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get collection thumbnail
    /// </summary>
    [HttpGet("collections/{collectionId}")]
    public async Task<IActionResult> GetCollectionThumbnail(
        ObjectId collectionId, 
        [FromQuery] int? width = null, 
        [FromQuery] int? height = null)
    {
        try
        {
            _logger.LogDebug("Getting thumbnail for collection {CollectionId} with size {Width}x{Height}", 
                collectionId, width, height);

            var thumbnailData = await _thumbnailService.GetCollectionThumbnailAsync(collectionId, width, height);
            
            if (thumbnailData == null)
            {
                return NotFound("Thumbnail not found");
            }

            var contentType = "image/jpeg";
            var filename = $"collection_{collectionId}_thumb.jpg";

            // Set cache headers
            Response.Headers["Cache-Control"] = "public, max-age=3600"; // 1 hour cache
            Response.Headers["X-Thumbnail-Source"] = "advanced-service";

            return File(thumbnailData, contentType, filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting thumbnail for collection {CollectionId}", collectionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Batch regenerate thumbnails for multiple collections
    /// </summary>
    [HttpPost("collections/batch-regenerate")]
    public async Task<ActionResult<BatchThumbnailResult>> BatchRegenerateThumbnails([FromBody] BatchThumbnailRequest request)
    {
        try
        {
            if (request.CollectionIds == null || !request.CollectionIds.Any())
            {
                return BadRequest(new { error = "Collection IDs are required" });
            }

            _logger.LogInformation("Starting batch thumbnail regeneration for {Count} collections", 
                request.CollectionIds.Count());

            var result = await _thumbnailService.BatchRegenerateThumbnailsAsync(request.CollectionIds);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch thumbnail regeneration");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete collection thumbnail
    /// </summary>
    [HttpDelete("collections/{collectionId}")]
    public async Task<IActionResult> DeleteCollectionThumbnail(ObjectId collectionId)
    {
        try
        {
            _logger.LogInformation("Deleting thumbnail for collection {CollectionId}", collectionId);
            
            await _thumbnailService.DeleteCollectionThumbnailAsync(collectionId);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting thumbnail for collection {CollectionId}", collectionId);
            return StatusCode(500, "Internal server error");
        }
    }
}

public class ThumbnailGenerationResponse
{
    public ObjectId CollectionId { get; set; }
    public string ThumbnailPath { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}

public class BatchThumbnailRequest
{
    public IEnumerable<ObjectId> CollectionIds { get; set; } = Enumerable.Empty<ObjectId>();
}

