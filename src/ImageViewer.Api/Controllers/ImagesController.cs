using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Application.DTOs.Common;
using ImageViewer.Application.Extensions;
using MongoDB.Bson;

namespace ImageViewer.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly IImageService _imageService;
    private readonly ILogger<ImagesController> _logger;

    public ImagesController(IImageService imageService, ILogger<ImagesController> logger)
    {
        _imageService = imageService;
        _logger = logger;
    }

    /// <summary>
    /// Get random image
    /// </summary>
    [HttpGet("random")]
    public async Task<ActionResult<Domain.ValueObjects.ImageEmbedded>> GetRandomImage()
    {
        try
        {
            _logger.LogInformation("Getting random image");
            var image = await _imageService.GetRandomEmbeddedImageAsync();
            if (image == null)
            {
                return NotFound();
            }
            return Ok(image);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting random image");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get random image within a collection
    /// </summary>
    [HttpGet("collection/{collectionId}/random")]
    public async Task<ActionResult<Domain.ValueObjects.ImageEmbedded>> GetRandomImageByCollection(ObjectId collectionId)
    {
        try
        {
            _logger.LogInformation("Getting random image for collection {CollectionId}", collectionId);
            var image = await _imageService.GetRandomEmbeddedImageByCollectionAsync(collectionId);
            if (image == null)
            {
                return NotFound();
            }
            return Ok(image);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting random image for collection {CollectionId}", collectionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get images by collection ID with pagination
    /// </summary>
    [HttpGet("collection/{collectionId}")]
    public async Task<ActionResult<PaginationResponseDto<Domain.ValueObjects.ImageEmbedded>>> GetImagesByCollection(
        ObjectId collectionId,
        [FromQuery] PaginationRequestDto pagination)
    {
        try
        {
            var images = await _imageService.GetEmbeddedImagesByCollectionAsync(collectionId);
            var totalCount = images.Count();
            var paginatedImages = images
                .AsQueryable()
                .ApplySorting(pagination.SortBy, pagination.SortDirection)
                .ApplyPagination(pagination);
            
            var response = paginatedImages.ToPaginationResponse(totalCount, pagination);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting images for collection {CollectionId}", collectionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get image by ID - REMOVED: Use GET /api/v1/images/{collectionId}/{imageId} instead
    /// </summary>
    [HttpGet("{id}")]
    [Obsolete("Use GET /api/v1/images/{collectionId}/{imageId} instead")]
    public ActionResult GetImage(ObjectId id)
    {
        return BadRequest("This endpoint is deprecated. Use GET /api/v1/images/{collectionId}/{imageId} instead.");
    }

    /// <summary>
    /// Get image file content
    /// </summary>
    [HttpGet("{collectionId}/{imageId}/file")]
    public async Task<IActionResult> GetImageFile(ObjectId collectionId, string imageId, [FromQuery] int? width = null, [FromQuery] int? height = null)
    {
        try
        {
            var image = await _imageService.GetEmbeddedImageByIdAsync(imageId, collectionId);
            if (image == null)
            {
                return NotFound();
            }

            var fileBytes = await _imageService.GetImageFileAsync(imageId, collectionId);
            if (fileBytes == null)
            {
                return NotFound("Image file not found");
            }

            var contentType = GetContentType(image.Format);
            return File(fileBytes, contentType, image.Filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image file {ImageId} from collection {CollectionId}", imageId, collectionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get image thumbnail
    /// </summary>
    [HttpGet("{collectionId}/{imageId}/thumbnail")]
    public async Task<IActionResult> GetImageThumbnail(ObjectId collectionId, string imageId, [FromQuery] int? width = null, [FromQuery] int? height = null)
    {
        try
        {
            var image = await _imageService.GetEmbeddedImageByIdAsync(imageId, collectionId);
            if (image == null)
            {
                return NotFound();
            }

            var thumbnailBytes = await _imageService.GetThumbnailAsync(imageId, collectionId, width, height);
            if (thumbnailBytes == null)
            {
                return NotFound("Thumbnail not found");
            }

            return File(thumbnailBytes, "image/jpeg", $"thumb_{image.Filename}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting thumbnail for image {ImageId} from collection {CollectionId}", imageId, collectionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete image
    /// </summary>
    [HttpDelete("{collectionId}/{imageId}")]
    public async Task<IActionResult> DeleteImage(ObjectId collectionId, string imageId)
    {
        try
        {
            await _imageService.DeleteEmbeddedImageAsync(imageId, collectionId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {ImageId} from collection {CollectionId}", imageId, collectionId);
            return StatusCode(500, "Internal server error");
        }
    }

    private static string GetContentType(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "gif" => "image/gif",
            "bmp" => "image/bmp",
            "webp" => "image/webp",
            "tiff" or "tif" => "image/tiff",
            _ => "application/octet-stream"
        };
    }
}
