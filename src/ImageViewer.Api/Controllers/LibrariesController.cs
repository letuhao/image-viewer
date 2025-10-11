using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs;
using ImageViewer.Application.Mappings;
using ImageViewer.Domain.Exceptions;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Controller for Library operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]  // Require authentication for all library operations
public class LibrariesController : ControllerBase
{
    private readonly ILibraryService _libraryService;
    private readonly ILogger<LibrariesController> _logger;

    public LibrariesController(ILibraryService libraryService, ILogger<LibrariesController> logger)
    {
        _libraryService = libraryService ?? throw new ArgumentNullException(nameof(libraryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new library
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateLibrary([FromBody] CreateLibraryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!ObjectId.TryParse(request.OwnerId, out var ownerId))
                return BadRequest(new { message = "Invalid owner ID format" });

            var library = await _libraryService.CreateLibraryAsync(
                request.Name, 
                request.Path, 
                ownerId, 
                request.Description,
                request.AutoScan);
            
            var libraryDto = library.ToDto();
            return CreatedAtAction(nameof(GetLibrary), new { id = library.Id.ToString() }, libraryDto);
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
            _logger.LogError(ex, "Failed to create library");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get library by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetLibrary(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var libraryId))
                return BadRequest(new { message = "Invalid library ID format" });

            var library = await _libraryService.GetLibraryByIdAsync(libraryId);
            return Ok(library.ToDto());
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get library with ID {LibraryId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Manually trigger library scan
    /// </summary>
    [HttpPost("{id}/scan")]
    public async Task<IActionResult> TriggerLibraryScan(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var libraryId))
                return BadRequest(new { message = "Invalid library ID format" });

            // Get library to verify it exists
            var library = await _libraryService.GetLibraryByIdAsync(libraryId);
            if (library == null)
                return NotFound(new { message = "Library not found" });

            // Publish scan message directly to RabbitMQ
            var messageQueueService = HttpContext.RequestServices.GetRequiredService<IMessageQueueService>();
            var scanMessage = new ImageViewer.Infrastructure.Messaging.LibraryScanMessage
            {
                LibraryId = libraryId.ToString(),
                LibraryPath = library.Path,
                ScanType = "Manual",
                IncludeSubfolders = true
            };

            _logger.LogInformation("About to publish LibraryScanMessage: LibraryId={LibraryId}, MessageType={MessageType}, MessageId={MessageId}", 
                scanMessage.LibraryId, scanMessage.MessageType, scanMessage.Id);

            // Publish to the exchange (not directly to queue) - queue is bound with routing key pattern
            await messageQueueService.PublishAsync(scanMessage);

            _logger.LogInformation("Published LibraryScanMessage successfully");
            _logger.LogInformation("Manually triggered scan for library {LibraryId}", libraryId);
            
            return Ok(new { 
                message = "Library scan triggered successfully",
                libraryId = id,
                libraryName = library.Name,
                libraryPath = library.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger library scan for {LibraryId}", id);
            return StatusCode(500, new { message = "Failed to trigger library scan" });
        }
    }

    /// <summary>
    /// Get library by path
    /// </summary>
    [HttpGet("path/{path}")]
    public async Task<IActionResult> GetLibraryByPath(string path)
    {
        try
        {
            var library = await _libraryService.GetLibraryByPathAsync(path);
            return Ok(library.ToDto());
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
            _logger.LogError(ex, "Failed to get library at path {Path}", path);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get libraries by owner ID
    /// </summary>
    [HttpGet("owner/{ownerId}")]
    public async Task<IActionResult> GetLibrariesByOwner(string ownerId)
    {
        try
        {
            if (!ObjectId.TryParse(ownerId, out var ownerObjectId))
                return BadRequest(new { message = "Invalid owner ID format" });

            var libraries = await _libraryService.GetLibrariesByOwnerIdAsync(ownerObjectId);
            return Ok(libraries.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get libraries for owner {OwnerId}", ownerId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get public libraries
    /// </summary>
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicLibraries()
    {
        try
        {
            var libraries = await _libraryService.GetPublicLibrariesAsync();
            return Ok(libraries.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get public libraries");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all libraries with pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLibraries([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var libraries = await _libraryService.GetLibrariesAsync(page, pageSize);
            return Ok(libraries.ToDto());
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get libraries for page {Page} with page size {PageSize}", page, pageSize);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update library information
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateLibrary(string id, [FromBody] UpdateLibraryRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var libraryId))
                return BadRequest(new { message = "Invalid library ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var library = await _libraryService.UpdateLibraryAsync(libraryId, request);
            return Ok(library.ToDto());
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
            _logger.LogError(ex, "Failed to update library with ID {LibraryId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete library
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,LibraryManager")]
    public async Task<IActionResult> DeleteLibrary(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var libraryId))
                return BadRequest(new { message = "Invalid library ID format" });

            await _libraryService.DeleteLibraryAsync(libraryId);
            return NoContent();
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete library with ID {LibraryId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update library settings
    /// </summary>
    [HttpPut("{id}/settings")]
    public async Task<IActionResult> UpdateSettings(string id, [FromBody] UpdateLibrarySettingsRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var libraryId))
                return BadRequest(new { message = "Invalid library ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var library = await _libraryService.UpdateSettingsAsync(libraryId, request);
            return Ok(library.ToDto());
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
            _logger.LogError(ex, "Failed to update settings for library with ID {LibraryId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update library metadata
    /// </summary>
    [HttpPut("{id}/metadata")]
    public async Task<IActionResult> UpdateMetadata(string id, [FromBody] UpdateLibraryMetadataRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var libraryId))
                return BadRequest(new { message = "Invalid library ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var library = await _libraryService.UpdateMetadataAsync(libraryId, request);
            return Ok(library);
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
            _logger.LogError(ex, "Failed to update metadata for library with ID {LibraryId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update library statistics
    /// </summary>
    [HttpPut("{id}/statistics")]
    public async Task<IActionResult> UpdateStatistics(string id, [FromBody] UpdateLibraryStatisticsRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var libraryId))
                return BadRequest(new { message = "Invalid library ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var library = await _libraryService.UpdateStatisticsAsync(libraryId, request);
            return Ok(library);
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
            _logger.LogError(ex, "Failed to update statistics for library with ID {LibraryId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Activate library
    /// </summary>
    [HttpPost("{id}/activate")]
    public async Task<IActionResult> ActivateLibrary(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var libraryId))
                return BadRequest(new { message = "Invalid library ID format" });

            var library = await _libraryService.ActivateLibraryAsync(libraryId);
            return Ok(library);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate library with ID {LibraryId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Deactivate library
    /// </summary>
    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> DeactivateLibrary(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var libraryId))
                return BadRequest(new { message = "Invalid library ID format" });

            var library = await _libraryService.DeactivateLibraryAsync(libraryId);
            return Ok(library);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate library with ID {LibraryId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Set library public/private
    /// </summary>
    [HttpPost("{id}/set-public")]
    public async Task<IActionResult> SetPublic(string id, [FromBody] SetPublicRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var libraryId))
                return BadRequest(new { message = "Invalid library ID format" });

            var library = await _libraryService.SetPublicAsync(libraryId, request.IsPublic);
            return Ok(library);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set library visibility for library with ID {LibraryId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Enable library watching
    /// </summary>
    [HttpPost("{id}/enable-watching")]
    public async Task<IActionResult> EnableWatching(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var libraryId))
                return BadRequest(new { message = "Invalid library ID format" });

            var library = await _libraryService.EnableWatchingAsync(libraryId);
            return Ok(library);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable watching for library with ID {LibraryId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Disable library watching
    /// </summary>
    [HttpPost("{id}/disable-watching")]
    public async Task<IActionResult> DisableWatching(string id)
    {
        try
        {
            if (!ObjectId.TryParse(id, out var libraryId))
                return BadRequest(new { message = "Invalid library ID format" });

            var library = await _libraryService.DisableWatchingAsync(libraryId);
            return Ok(library);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable watching for library with ID {LibraryId}", id);
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
            if (!ObjectId.TryParse(id, out var libraryId))
                return BadRequest(new { message = "Invalid library ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var library = await _libraryService.UpdateWatchSettingsAsync(libraryId, request);
            return Ok(library);
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
            _logger.LogError(ex, "Failed to update watch settings for library with ID {LibraryId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Search libraries
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchLibraries([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var libraries = await _libraryService.SearchLibrariesAsync(query, page, pageSize);
            return Ok(libraries);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search libraries with query {Query}", query);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get library statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetLibraryStatistics()
    {
        try
        {
            var statistics = await _libraryService.GetLibraryStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get library statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get top libraries by activity
    /// </summary>
    [HttpGet("top-activity")]
    public async Task<IActionResult> GetTopLibrariesByActivity([FromQuery] int limit = 10)
    {
        try
        {
            var libraries = await _libraryService.GetTopLibrariesByActivityAsync(limit);
            return Ok(libraries);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top libraries by activity");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get recent libraries
    /// </summary>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentLibraries([FromQuery] int limit = 10)
    {
        try
        {
            var libraries = await _libraryService.GetRecentLibrariesAsync(limit);
            return Ok(libraries);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent libraries");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

/// <summary>
/// Request model for creating a library
/// </summary>
public class CreateLibraryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool AutoScan { get; set; } = false;
}

/// <summary>
/// Request model for setting library public/private
/// </summary>
public class SetPublicRequest
{
    public bool IsPublic { get; set; }
}
