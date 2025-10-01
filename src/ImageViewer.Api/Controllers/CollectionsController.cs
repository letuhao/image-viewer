using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Application.DTOs.Common;
using ImageViewer.Application.Extensions;

namespace ImageViewer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CollectionsController : ControllerBase
{
    private readonly ICollectionService _collectionService;
    private readonly ILogger<CollectionsController> _logger;

    public CollectionsController(ICollectionService collectionService, ILogger<CollectionsController> logger)
    {
        _collectionService = collectionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all collections with pagination and search
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginationResponseDto<Collection>>> GetCollections(
        [FromQuery] PaginationRequestDto pagination,
        [FromQuery] string? search = null,
        [FromQuery] string? type = null)
    {
        try
        {
            _logger.LogInformation("Getting collections with pagination. Page: {Page}, PageSize: {PageSize}, Search: {Search}", 
                pagination.Page, pagination.PageSize, search);
            
            var collections = await _collectionService.GetCollectionsAsync(pagination, search, type);
            return Ok(collections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collections");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get collection by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Collection>> GetCollection(Guid id)
    {
        try
        {
            var collection = await _collectionService.GetByIdAsync(id);
            if (collection == null)
            {
                return NotFound();
            }
            return Ok(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create new collection
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Collection>> CreateCollection([FromBody] CreateCollectionRequest request)
    {
        try
        {
            var settings = new CollectionSettings();
            settings.UpdateThumbnailSize(request.ThumbnailWidth ?? 200, request.ThumbnailHeight ?? 200);
            settings.UpdateCacheSize(request.CacheWidth ?? 1280, request.CacheHeight ?? 720);
            settings.SetAutoGenerateThumbnails(request.EnableCache ?? true);
            settings.SetAutoGenerateCache(request.AutoScan ?? true);

            var createdCollection = await _collectionService.CreateAsync(
                request.Name,
                request.Path,
                request.Type ?? CollectionType.Folder,
                settings
            );
            return CreatedAtAction(nameof(GetCollection), new { id = createdCollection.Id }, createdCollection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating collection");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update collection
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCollection(Guid id, [FromBody] UpdateCollectionRequest request)
    {
        try
        {
            CollectionSettings? settings = null;
            if (request.Settings != null)
            {
                settings = new CollectionSettings();
                settings.UpdateThumbnailSize(request.Settings.ThumbnailWidth ?? 200, request.Settings.ThumbnailHeight ?? 200);
                settings.UpdateCacheSize(request.Settings.CacheWidth ?? 1280, request.Settings.CacheHeight ?? 720);
                settings.SetAutoGenerateThumbnails(request.Settings.EnableCache ?? true);
                settings.SetAutoGenerateCache(request.Settings.AutoScan ?? true);
            }

            await _collectionService.UpdateAsync(id, request.Name, request.Path, settings);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating collection {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete collection
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCollection(Guid id)
    {
        try
        {
            await _collectionService.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting collection {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Scan collection for images
    /// </summary>
    [HttpPost("{id}/scan")]
    public async Task<IActionResult> ScanCollection(Guid id)
    {
        try
        {
            await _collectionService.ScanCollectionAsync(id);
            return Ok(new { message = "Collection scan started" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning collection {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Search collections
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<SearchResponseDto<Collection>>> SearchCollections(
        [FromQuery] SearchRequestDto searchRequest,
        [FromQuery] PaginationRequestDto pagination)
    {
        try
        {
            var response = await _collectionService.SearchCollectionsAsync(searchRequest, pagination);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching collections");
            return StatusCode(500, "Internal server error");
        }
    }
}

public class CreateCollectionRequest
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public CollectionType? Type { get; set; }
    public int? ThumbnailWidth { get; set; }
    public int? ThumbnailHeight { get; set; }
    public int? CacheWidth { get; set; }
    public int? CacheHeight { get; set; }
    public int? Quality { get; set; }
    public bool? EnableCache { get; set; }
    public bool? AutoScan { get; set; }
}

public class UpdateCollectionRequest
{
    public string? Name { get; set; }
    public string? Path { get; set; }
    public CollectionSettingsRequest? Settings { get; set; }
}

public class CollectionSettingsRequest
{
    public int? ThumbnailWidth { get; set; }
    public int? ThumbnailHeight { get; set; }
    public int? CacheWidth { get; set; }
    public int? CacheHeight { get; set; }
    public int? Quality { get; set; }
    public bool? EnableCache { get; set; }
    public bool? AutoScan { get; set; }
}
