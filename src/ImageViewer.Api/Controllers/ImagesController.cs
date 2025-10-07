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
    public async Task<ActionResult<Image>> GetRandomImage()
    {
        try
        {
            _logger.LogInformation("Getting random image");
            var image = await _imageService.GetRandomImageAsync();
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
    public async Task<ActionResult<Image>> GetRandomImageByCollection(ObjectId collectionId)
    {
        try
        {
            _logger.LogInformation("Getting random image for collection {CollectionId}", collectionId);
            var image = await _imageService.GetRandomImageByCollectionAsync(collectionId);
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
    public async Task<ActionResult<PaginationResponseDto<Image>>> GetImagesByCollection(
        ObjectId collectionId,
        [FromQuery] PaginationRequestDto pagination)
    {
        try
        {
            var images = await _imageService.GetByCollectionIdAsync(collectionId);
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
    /// Get image by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Image>> GetImage(ObjectId id)
    {
        try
        {
            var image = await _imageService.GetByIdAsync(id);
            if (image == null)
            {
                return NotFound();
            }
            return Ok(image);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get image file content
    /// </summary>
    [HttpGet("{id}/file")]
    public async Task<IActionResult> GetImageFile(ObjectId id, [FromQuery] int? width = null, [FromQuery] int? height = null)
    {
        try
        {
            var image = await _imageService.GetByIdAsync(id);
            if (image == null)
            {
                return NotFound();
            }

            var fileBytes = await _imageService.GetImageFileAsync(id);
            if (fileBytes == null)
            {
                return NotFound("Image file not found");
            }

            var contentType = GetContentType(image.Format);
            return File(fileBytes, contentType, image.Filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image file {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get image thumbnail
    /// </summary>
    [HttpGet("{id}/thumbnail")]
    public async Task<IActionResult> GetImageThumbnail(ObjectId id, [FromQuery] int? width = null, [FromQuery] int? height = null)
    {
        try
        {
            var image = await _imageService.GetByIdAsync(id);
            if (image == null)
            {
                return NotFound();
            }

            var thumbnailBytes = await _imageService.GetThumbnailAsync(id, width ?? 200, height ?? 200);
            if (thumbnailBytes == null)
            {
                return NotFound("Thumbnail not found");
            }

            return File(thumbnailBytes, "image/jpeg", $"thumb_{image.Filename}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting thumbnail for image {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete image
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteImage(ObjectId id)
    {
        try
        {
            await _imageService.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {Id}", id);
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
