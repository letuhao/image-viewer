using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Api.Controllers;

/// <summary>
/// Controller for user-specific settings operations
/// 中文：用户设置控制器
/// Tiếng Việt: Bộ điều khiển cài đặt người dùng
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
// [Authorize] // Uncomment when auth is fully tested
public class UserSettingsController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserSettingsController> _logger;

    public UserSettingsController(
        IUserRepository userRepository,
        ILogger<UserSettingsController> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get current user's settings
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        try
        {
            // TODO: Get userId from authenticated user context
            // var userId = User.FindFirst("sub")?.Value;
            // For now, use admin user ID for testing
            var userId = "68e92fcd1a203b8d769c4560";
            
            if (string.IsNullOrEmpty(userId) || !ObjectId.TryParse(userId, out var userObjectId))
            {
                return Unauthorized();
            }

            var user = await _userRepository.GetByIdAsync(userObjectId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new
            {
                displaySettings = user.Settings.DisplaySettings,
                privacySettings = user.Settings.PrivacySettings,
                notificationSettings = user.Settings.NotificationSettings,
                language = user.Settings.Language,
                timezone = user.Settings.Timezone
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user settings");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update current user's settings
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateUserSettingsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // TODO: Get userId from authenticated user context
            var userId = "68e92fcd1a203b8d769c4560";
            
            if (string.IsNullOrEmpty(userId) || !ObjectId.TryParse(userId, out var userObjectId))
            {
                return Unauthorized();
            }

            var user = await _userRepository.GetByIdAsync(userObjectId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Update display settings
            if (request.DisplaySettings != null)
            {
                if (request.DisplaySettings.Theme != null)
                    user.Settings.DisplaySettings.Theme = request.DisplaySettings.Theme;
                if (request.DisplaySettings.ViewMode != null)
                    user.Settings.DisplaySettings.ViewMode = request.DisplaySettings.ViewMode;
                if (request.DisplaySettings.ItemsPerPage.HasValue)
                    user.Settings.DisplaySettings.ItemsPerPage = request.DisplaySettings.ItemsPerPage.Value;
                if (request.DisplaySettings.CardSize != null)
                    user.Settings.DisplaySettings.CardSize = request.DisplaySettings.CardSize;
                if (request.DisplaySettings.CompactMode.HasValue)
                    user.Settings.DisplaySettings.CompactMode = request.DisplaySettings.CompactMode.Value;
                if (request.DisplaySettings.EnableAnimations.HasValue)
                    user.Settings.DisplaySettings.EnableAnimations = request.DisplaySettings.EnableAnimations.Value;
            }

            // Update notification settings
            if (request.NotificationSettings != null)
            {
                if (request.NotificationSettings.EmailNotifications.HasValue)
                    user.Settings.NotificationSettings.EmailNotifications = request.NotificationSettings.EmailNotifications.Value;
                if (request.NotificationSettings.PushNotifications.HasValue)
                    user.Settings.NotificationSettings.PushNotifications = request.NotificationSettings.PushNotifications.Value;
                if (request.NotificationSettings.DesktopNotifications.HasValue)
                    user.Settings.NotificationSettings.DesktopNotifications = request.NotificationSettings.DesktopNotifications.Value;
            }

            // Update privacy settings
            if (request.PrivacySettings != null)
            {
                if (request.PrivacySettings.ProfilePublic.HasValue)
                    user.Settings.PrivacySettings.ProfilePublic = request.PrivacySettings.ProfilePublic.Value;
                if (request.PrivacySettings.ShowOnlineStatus.HasValue)
                    user.Settings.PrivacySettings.ShowOnlineStatus = request.PrivacySettings.ShowOnlineStatus.Value;
                if (request.PrivacySettings.AllowAnalytics.HasValue)
                    user.Settings.PrivacySettings.AllowAnalytics = request.PrivacySettings.AllowAnalytics.Value;
            }

            // Update language and timezone
            if (request.Language != null)
                user.Settings.Language = request.Language;
            if (request.Timezone != null)
                user.Settings.Timezone = request.Timezone;

            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User {UserId} updated settings", userObjectId);

            return Ok(new
            {
                displaySettings = user.Settings.DisplaySettings,
                privacySettings = user.Settings.PrivacySettings,
                notificationSettings = user.Settings.NotificationSettings,
                language = user.Settings.Language,
                timezone = user.Settings.Timezone
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user settings");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Reset settings to default
    /// </summary>
    [HttpPost("reset")]
    public async Task<IActionResult> ResetSettings()
    {
        try
        {
            // TODO: Get userId from authenticated user context
            var userId = "68e92fcd1a203b8d769c4560";
            
            if (string.IsNullOrEmpty(userId) || !ObjectId.TryParse(userId, out var userObjectId))
            {
                return Unauthorized();
            }

            var user = await _userRepository.GetByIdAsync(userObjectId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Reset to default settings
            user.Settings = new UserSettings();
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User {UserId} reset settings to default", userObjectId);

            return Ok(new
            {
                displaySettings = user.Settings.DisplaySettings,
                privacySettings = user.Settings.PrivacySettings,
                notificationSettings = user.Settings.NotificationSettings,
                language = user.Settings.Language,
                timezone = user.Settings.Timezone
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset user settings");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

/// <summary>
/// Request model for updating user settings
/// </summary>
public class UpdateUserSettingsRequest
{
    public DisplaySettingsUpdate? DisplaySettings { get; set; }
    public NotificationSettingsUpdate? NotificationSettings { get; set; }
    public PrivacySettingsUpdate? PrivacySettings { get; set; }
    public string? Language { get; set; }
    public string? Timezone { get; set; }
}

public class DisplaySettingsUpdate
{
    public string? Theme { get; set; }
    public string? ViewMode { get; set; }
    public int? ItemsPerPage { get; set; }
    public string? CardSize { get; set; }
    public bool? CompactMode { get; set; }
    public bool? EnableAnimations { get; set; }
}

public class NotificationSettingsUpdate
{
    public bool? EmailNotifications { get; set; }
    public bool? PushNotifications { get; set; }
    public bool? DesktopNotifications { get; set; }
}

public class PrivacySettingsUpdate
{
    public bool? ProfilePublic { get; set; }
    public bool? ShowOnlineStatus { get; set; }
    public bool? AllowAnalytics { get; set; }
}

