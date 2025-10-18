using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;

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
                displayMode = user.Settings.DisplayMode,
                itemsPerPage = user.Settings.ItemsPerPage, // Keep for backward compatibility
                collectionsPageSize = user.Settings.CollectionsPageSize,
                collectionDetailPageSize = user.Settings.CollectionDetailPageSize,
                sidebarPageSize = user.Settings.SidebarPageSize,
                imageViewerPageSize = user.Settings.ImageViewerPageSize,
                theme = user.Settings.Theme,
                language = user.Settings.Language,
                timezone = user.Settings.Timezone,
                notifications = new
                {
                    email = user.Settings.Notifications.Email,
                    push = user.Settings.Notifications.Push,
                    sms = user.Settings.Notifications.Sms,
                    inApp = user.Settings.Notifications.InApp
                },
                privacy = new
                {
                    profileVisibility = user.Settings.Privacy.ProfileVisibility,
                    activityVisibility = user.Settings.Privacy.ActivityVisibility,
                    dataSharing = user.Settings.Privacy.DataSharing,
                    analytics = user.Settings.Privacy.Analytics
                },
                performance = new
                {
                    imageQuality = user.Settings.Performance.ImageQuality,
                    videoQuality = user.Settings.Performance.VideoQuality,
                    cacheSize = user.Settings.Performance.CacheSize,
                    autoOptimize = user.Settings.Performance.AutoOptimize
                },
                pagination = new
                {
                    showFirstLast = user.Settings.Pagination.ShowFirstLast,
                    showPageNumbers = user.Settings.Pagination.ShowPageNumbers,
                    pageNumbersToShow = user.Settings.Pagination.PageNumbersToShow
                }
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

            // Update settings using domain methods
            if (request.DisplayMode != null)
                user.Settings.UpdateDisplayMode(request.DisplayMode);
            
            if (request.ItemsPerPage.HasValue)
                user.Settings.UpdateItemsPerPage(request.ItemsPerPage.Value);
            
            if (request.CollectionsPageSize.HasValue)
                user.Settings.UpdateCollectionsPageSize(request.CollectionsPageSize.Value);
            
            if (request.CollectionDetailPageSize.HasValue)
                user.Settings.UpdateCollectionDetailPageSize(request.CollectionDetailPageSize.Value);
            
            if (request.SidebarPageSize.HasValue)
                user.Settings.UpdateSidebarPageSize(request.SidebarPageSize.Value);
            
            if (request.ImageViewerPageSize.HasValue)
                user.Settings.UpdateImageViewerPageSize(request.ImageViewerPageSize.Value);
            
            if (request.Theme != null)
                user.Settings.UpdateTheme(request.Theme);
            
            if (request.Language != null)
                user.Settings.UpdateLanguage(request.Language);
            
            if (request.Timezone != null)
                user.Settings.UpdateTimezone(request.Timezone);

            // Update notification settings
            if (request.Notifications != null)
            {
                if (request.Notifications.Email.HasValue)
                    user.Settings.Notifications.UpdateEmail(request.Notifications.Email.Value);
                if (request.Notifications.Push.HasValue)
                    user.Settings.Notifications.UpdatePush(request.Notifications.Push.Value);
                if (request.Notifications.Sms.HasValue)
                    user.Settings.Notifications.UpdateSms(request.Notifications.Sms.Value);
                if (request.Notifications.InApp.HasValue)
                    user.Settings.Notifications.UpdateInApp(request.Notifications.InApp.Value);
            }

            // Update privacy settings
            if (request.Privacy != null)
            {
                if (request.Privacy.ProfileVisibility != null)
                    user.Settings.Privacy.UpdateProfileVisibility(request.Privacy.ProfileVisibility);
                if (request.Privacy.ActivityVisibility != null)
                    user.Settings.Privacy.UpdateActivityVisibility(request.Privacy.ActivityVisibility);
                if (request.Privacy.DataSharing.HasValue)
                    user.Settings.Privacy.UpdateDataSharing(request.Privacy.DataSharing.Value);
                if (request.Privacy.Analytics.HasValue)
                    user.Settings.Privacy.UpdateAnalytics(request.Privacy.Analytics.Value);
            }

            // Update performance settings
            if (request.Performance != null)
            {
                if (request.Performance.ImageQuality != null)
                    user.Settings.Performance.UpdateImageQuality(request.Performance.ImageQuality);
                if (request.Performance.VideoQuality != null)
                    user.Settings.Performance.UpdateVideoQuality(request.Performance.VideoQuality);
                if (request.Performance.CacheSize.HasValue)
                    user.Settings.Performance.UpdateCacheSize(request.Performance.CacheSize.Value);
                if (request.Performance.AutoOptimize.HasValue)
                    user.Settings.Performance.UpdateAutoOptimize(request.Performance.AutoOptimize.Value);
            }
            
            // Update pagination settings
            if (request.Pagination != null)
            {
                if (request.Pagination.ShowFirstLast.HasValue)
                    user.Settings.Pagination.UpdateShowFirstLast(request.Pagination.ShowFirstLast.Value);
                if (request.Pagination.ShowPageNumbers.HasValue)
                    user.Settings.Pagination.UpdateShowPageNumbers(request.Pagination.ShowPageNumbers.Value);
                if (request.Pagination.PageNumbersToShow.HasValue)
                    user.Settings.Pagination.UpdatePageNumbersToShow(request.Pagination.PageNumbersToShow.Value);
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User {UserId} updated settings", userObjectId);

            return Ok(new
            {
                displayMode = user.Settings.DisplayMode,
                itemsPerPage = user.Settings.ItemsPerPage, // Keep for backward compatibility
                collectionsPageSize = user.Settings.CollectionsPageSize,
                collectionDetailPageSize = user.Settings.CollectionDetailPageSize,
                sidebarPageSize = user.Settings.SidebarPageSize,
                imageViewerPageSize = user.Settings.ImageViewerPageSize,
                theme = user.Settings.Theme,
                language = user.Settings.Language,
                timezone = user.Settings.Timezone,
                notifications = new
                {
                    email = user.Settings.Notifications.Email,
                    push = user.Settings.Notifications.Push,
                    sms = user.Settings.Notifications.Sms,
                    inApp = user.Settings.Notifications.InApp
                },
                privacy = new
                {
                    profileVisibility = user.Settings.Privacy.ProfileVisibility,
                    activityVisibility = user.Settings.Privacy.ActivityVisibility,
                    dataSharing = user.Settings.Privacy.DataSharing,
                    analytics = user.Settings.Privacy.Analytics
                },
                performance = new
                {
                    imageQuality = user.Settings.Performance.ImageQuality,
                    videoQuality = user.Settings.Performance.VideoQuality,
                    cacheSize = user.Settings.Performance.CacheSize,
                    autoOptimize = user.Settings.Performance.AutoOptimize
                },
                pagination = new
                {
                    showFirstLast = user.Settings.Pagination.ShowFirstLast,
                    showPageNumbers = user.Settings.Pagination.ShowPageNumbers,
                    pageNumbersToShow = user.Settings.Pagination.PageNumbersToShow
                }
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

            // Reset using Update method from User entity
            user.UpdateSettings(new UserSettings());
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User {UserId} reset settings to default", userObjectId);

            return Ok(new
            {
                displayMode = user.Settings.DisplayMode,
                itemsPerPage = user.Settings.ItemsPerPage,
                theme = user.Settings.Theme,
                language = user.Settings.Language,
                timezone = user.Settings.Timezone,
                notifications = new
                {
                    email = user.Settings.Notifications.Email,
                    push = user.Settings.Notifications.Push,
                    sms = user.Settings.Notifications.Sms,
                    inApp = user.Settings.Notifications.InApp
                },
                privacy = new
                {
                    profileVisibility = user.Settings.Privacy.ProfileVisibility,
                    activityVisibility = user.Settings.Privacy.ActivityVisibility,
                    dataSharing = user.Settings.Privacy.DataSharing,
                    analytics = user.Settings.Privacy.Analytics
                },
                performance = new
                {
                    imageQuality = user.Settings.Performance.ImageQuality,
                    videoQuality = user.Settings.Performance.VideoQuality,
                    cacheSize = user.Settings.Performance.CacheSize,
                    autoOptimize = user.Settings.Performance.AutoOptimize
                },
                pagination = new
                {
                    showFirstLast = user.Settings.Pagination.ShowFirstLast,
                    showPageNumbers = user.Settings.Pagination.ShowPageNumbers,
                    pageNumbersToShow = user.Settings.Pagination.PageNumbersToShow
                }
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
    public string? DisplayMode { get; set; }
    public int? ItemsPerPage { get; set; } // Keep for backward compatibility
    public int? CollectionsPageSize { get; set; }
    public int? CollectionDetailPageSize { get; set; }
    public int? SidebarPageSize { get; set; }
    public int? ImageViewerPageSize { get; set; }
    public string? Theme { get; set; }
    public string? Language { get; set; }
    public string? Timezone { get; set; }
    public NotificationSettingsUpdate? Notifications { get; set; }
    public PrivacySettingsUpdate? Privacy { get; set; }
    public PerformanceSettingsUpdate? Performance { get; set; }
    public PaginationSettingsUpdate? Pagination { get; set; }
}

public class NotificationSettingsUpdate
{
    public bool? Email { get; set; }
    public bool? Push { get; set; }
    public bool? Sms { get; set; }
    public bool? InApp { get; set; }
}

public class PrivacySettingsUpdate
{
    public string? ProfileVisibility { get; set; }
    public string? ActivityVisibility { get; set; }
    public bool? DataSharing { get; set; }
    public bool? Analytics { get; set; }
}

public class PerformanceSettingsUpdate
{
    public string? ImageQuality { get; set; }
    public string? VideoQuality { get; set; }
    public long? CacheSize { get; set; }
    public bool? AutoOptimize { get; set; }
}

public class PaginationSettingsUpdate
{
    public bool? ShowFirstLast { get; set; }
    public bool? ShowPageNumbers { get; set; }
    public int? PageNumbersToShow { get; set; }
}
