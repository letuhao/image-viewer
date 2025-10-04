using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Exceptions;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Controller for user preferences operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class UserPreferencesController : ControllerBase
{
    private readonly IUserPreferencesService _userPreferencesService;
    private readonly ILogger<UserPreferencesController> _logger;

    public UserPreferencesController(IUserPreferencesService userPreferencesService, ILogger<UserPreferencesController> logger)
    {
        _userPreferencesService = userPreferencesService ?? throw new ArgumentNullException(nameof(userPreferencesService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get user preferences
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserPreferences(string userId)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            var preferences = await _userPreferencesService.GetUserPreferencesAsync(userObjectId);
            return Ok(preferences);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user preferences for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update user preferences
    /// </summary>
    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdateUserPreferences(string userId, [FromBody] UpdateUserPreferencesRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var preferences = await _userPreferencesService.UpdateUserPreferencesAsync(userObjectId, request);
            return Ok(preferences);
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
            _logger.LogError(ex, "Failed to update user preferences for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Reset user preferences to defaults
    /// </summary>
    [HttpPost("{userId}/reset")]
    public async Task<IActionResult> ResetUserPreferences(string userId)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            var preferences = await _userPreferencesService.ResetUserPreferencesAsync(userObjectId);
            return Ok(preferences);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset user preferences for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get display preferences
    /// </summary>
    [HttpGet("{userId}/display")]
    public async Task<IActionResult> GetDisplayPreferences(string userId)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            var preferences = await _userPreferencesService.GetDisplayPreferencesAsync(userObjectId);
            return Ok(preferences);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get display preferences for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update display preferences
    /// </summary>
    [HttpPut("{userId}/display")]
    public async Task<IActionResult> UpdateDisplayPreferences(string userId, [FromBody] UpdateDisplayPreferencesRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var preferences = await _userPreferencesService.UpdateDisplayPreferencesAsync(userObjectId, request);
            return Ok(preferences);
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
            _logger.LogError(ex, "Failed to update display preferences for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get privacy preferences
    /// </summary>
    [HttpGet("{userId}/privacy")]
    public async Task<IActionResult> GetPrivacyPreferences(string userId)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            var preferences = await _userPreferencesService.GetPrivacyPreferencesAsync(userObjectId);
            return Ok(preferences);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get privacy preferences for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update privacy preferences
    /// </summary>
    [HttpPut("{userId}/privacy")]
    public async Task<IActionResult> UpdatePrivacyPreferences(string userId, [FromBody] UpdatePrivacyPreferencesRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var preferences = await _userPreferencesService.UpdatePrivacyPreferencesAsync(userObjectId, request);
            return Ok(preferences);
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
            _logger.LogError(ex, "Failed to update privacy preferences for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get performance preferences
    /// </summary>
    [HttpGet("{userId}/performance")]
    public async Task<IActionResult> GetPerformancePreferences(string userId)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            var preferences = await _userPreferencesService.GetPerformancePreferencesAsync(userObjectId);
            return Ok(preferences);
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance preferences for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update performance preferences
    /// </summary>
    [HttpPut("{userId}/performance")]
    public async Task<IActionResult> UpdatePerformancePreferences(string userId, [FromBody] UpdatePerformancePreferencesRequest request)
    {
        try
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
                return BadRequest(new { message = "Invalid user ID format" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var preferences = await _userPreferencesService.UpdatePerformancePreferencesAsync(userObjectId, request);
            return Ok(preferences);
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
            _logger.LogError(ex, "Failed to update performance preferences for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Validate preferences
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidatePreferences([FromBody] UpdateUserPreferencesRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var isValid = await _userPreferencesService.ValidatePreferencesAsync(request);
            return Ok(new { valid = isValid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate preferences");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
