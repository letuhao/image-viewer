using Microsoft.AspNetCore.Mvc;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.Tags;
using MongoDB.Bson;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Tag management controller
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;
    private readonly ILogger<TagsController> _logger;

    public TagsController(ITagService tagService, ILogger<TagsController> logger)
    {
        _tagService = tagService;
        _logger = logger;
    }

    /// <summary>
    /// Get all tags
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TagDto>>> GetAllTags()
    {
        try
        {
            _logger.LogInformation("Getting all tags");
            var tags = await _tagService.GetAllTagsAsync();
            return Ok(tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tags");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get tags for specific collection
    /// </summary>
    [HttpGet("collections/{collectionId}")]
    public async Task<ActionResult<IEnumerable<CollectionTagDto>>> GetCollectionTags(ObjectId collectionId)
    {
        try
        {
            _logger.LogInformation("Getting tags for collection: {CollectionId}", collectionId);
            var tags = await _tagService.GetCollectionTagsAsync(collectionId);
            return Ok(tags);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Collection not found: {CollectionId}", collectionId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tags for collection: {CollectionId}", collectionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Add tag to collection
    /// </summary>
    [HttpPost("collections/{collectionId}")]
    public async Task<ActionResult<CollectionTagDto>> AddTagToCollection(ObjectId collectionId, [FromBody] AddTagToCollectionDto dto)
    {
        try
        {
            _logger.LogInformation("Adding tag '{TagName}' to collection: {CollectionId}", dto.TagName, collectionId);
            var tag = await _tagService.AddTagToCollectionAsync(collectionId, dto);
            return CreatedAtAction(nameof(GetAllTags), new { }, tag);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Collection not found: {CollectionId}", collectionId);
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid tag data for collection: {CollectionId}", collectionId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding tag to collection: {CollectionId}", collectionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update tag
    /// </summary>
    [HttpPut("{tagId}")]
    public async Task<ActionResult<TagDto>> UpdateTag(ObjectId tagId, [FromBody] UpdateTagDto dto)
    {
        try
        {
            _logger.LogInformation("Updating tag with ID: {TagId}", tagId);
            var tag = await _tagService.UpdateTagAsync(tagId, dto);
            return Ok(tag);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Tag not found: {TagId}", tagId);
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid tag data for ID: {TagId}", tagId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tag with ID: {TagId}", tagId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Remove tag from collection
    /// </summary>
    [HttpDelete("collections/{collectionId}/tags/{tagName}")]
    public async Task<ActionResult> RemoveTagFromCollection(ObjectId collectionId, string tagName)
    {
        try
        {
            _logger.LogInformation("Removing tag {TagName} from collection: {CollectionId}", tagName, collectionId);
            await _tagService.RemoveTagFromCollectionAsync(collectionId, tagName);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Tag or collection not found: {TagName}, {CollectionId}", tagName, collectionId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tag from collection: {TagName}, {CollectionId}", tagName, collectionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete tag completely
    /// </summary>
    [HttpDelete("{tagId}")]
    public async Task<ActionResult> DeleteTag(ObjectId tagId)
    {
        try
        {
            _logger.LogInformation("Deleting tag with ID: {TagId}", tagId);
            await _tagService.DeleteTagAsync(tagId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Tag not found: {TagId}", tagId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tag with ID: {TagId}", tagId);
            return StatusCode(500, "Internal server error");
        }
    }
}
