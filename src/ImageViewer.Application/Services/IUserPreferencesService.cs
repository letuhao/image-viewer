using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service interface for user preferences operations
/// </summary>
public interface IUserPreferencesService
{
    #region User Preferences Management
    
    Task<UserPreferences> GetUserPreferencesAsync(ObjectId userId);
    Task<UserPreferences> UpdateUserPreferencesAsync(ObjectId userId, UpdateUserPreferencesRequest request);
    Task<UserPreferences> ResetUserPreferencesAsync(ObjectId userId);
    Task<bool> ValidatePreferencesAsync(UpdateUserPreferencesRequest request);
    
    #endregion
    
    #region Display Preferences
    
    Task<DisplayPreferences> GetDisplayPreferencesAsync(ObjectId userId);
    Task<DisplayPreferences> UpdateDisplayPreferencesAsync(ObjectId userId, UpdateDisplayPreferencesRequest request);
    
    #endregion
    
    #region Privacy Preferences
    
    Task<PrivacyPreferences> GetPrivacyPreferencesAsync(ObjectId userId);
    Task<PrivacyPreferences> UpdatePrivacyPreferencesAsync(ObjectId userId, UpdatePrivacyPreferencesRequest request);
    
    #endregion
    
    #region Performance Preferences
    
    Task<PerformancePreferences> GetPerformancePreferencesAsync(ObjectId userId);
    Task<PerformancePreferences> UpdatePerformancePreferencesAsync(ObjectId userId, UpdatePerformancePreferencesRequest request);
    
    #endregion
}

/// <summary>
/// Request model for updating user preferences
/// </summary>
public class UpdateUserPreferencesRequest
{
    public DisplayPreferences? Display { get; set; }
    public PrivacyPreferences? Privacy { get; set; }
    public PerformancePreferences? Performance { get; set; }
    public NotificationPreferences? Notifications { get; set; }
}

/// <summary>
/// Request model for updating display preferences
/// </summary>
public class UpdateDisplayPreferencesRequest
{
    public DisplayMode DisplayMode { get; set; } = DisplayMode.Grid;
    public int ItemsPerPage { get; set; } = 20;
    public int ThumbnailSize { get; set; } = 200;
    public bool ShowMetadata { get; set; } = true;
    public bool ShowFileSize { get; set; } = true;
    public bool ShowCreationDate { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string Language { get; set; } = "en";
    public string TimeZone { get; set; } = "UTC";
    public bool EnableAnimations { get; set; } = true;
    public bool EnableTooltips { get; set; } = true;
}

/// <summary>
/// Request model for updating privacy preferences
/// </summary>
public class UpdatePrivacyPreferencesRequest
{
    public bool ProfilePublic { get; set; } = false;
    public bool ShowOnlineStatus { get; set; } = true;
    public bool AllowDirectMessages { get; set; } = true;
    public bool ShowActivity { get; set; } = true;
    public bool AllowSearchIndexing { get; set; } = true;
    public bool ShareUsageData { get; set; } = false;
    public bool AllowAnalytics { get; set; } = true;
    public bool AllowCookies { get; set; } = true;
}

/// <summary>
/// Request model for updating performance preferences
/// </summary>
public class UpdatePerformancePreferencesRequest
{
    public int CacheSize { get; set; } = 100;
    public bool EnableLazyLoading { get; set; } = true;
    public bool EnableImageOptimization { get; set; } = true;
    public int MaxConcurrentDownloads { get; set; } = 3;
    public bool EnableBackgroundSync { get; set; } = true;
    public int AutoSaveInterval { get; set; } = 30;
    public bool EnableCompression { get; set; } = true;
    public bool EnableCaching { get; set; } = true;
}

/// <summary>
/// User preferences entity
/// </summary>
public class UserPreferences
{
    public ObjectId Id { get; set; }
    public ObjectId UserId { get; set; }
    public DisplayPreferences Display { get; set; } = new();
    public PrivacyPreferences Privacy { get; set; } = new();
    public PerformancePreferences Performance { get; set; } = new();
    public NotificationPreferences Notifications { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Display preferences
/// </summary>
public class DisplayPreferences
{
    public DisplayMode DisplayMode { get; set; } = DisplayMode.Grid;
    public int ItemsPerPage { get; set; } = 20;
    public int ThumbnailSize { get; set; } = 200;
    public bool ShowMetadata { get; set; } = true;
    public bool ShowFileSize { get; set; } = true;
    public bool ShowCreationDate { get; set; } = true;
    public string Theme { get; set; } = "light";
    public string Language { get; set; } = "en";
    public string TimeZone { get; set; } = "UTC";
    public bool EnableAnimations { get; set; } = true;
    public bool EnableTooltips { get; set; } = true;
}

/// <summary>
/// Privacy preferences
/// </summary>
public class PrivacyPreferences
{
    public bool ProfilePublic { get; set; } = false;
    public bool ShowOnlineStatus { get; set; } = true;
    public bool AllowDirectMessages { get; set; } = true;
    public bool ShowActivity { get; set; } = true;
    public bool AllowSearchIndexing { get; set; } = true;
    public bool ShareUsageData { get; set; } = false;
    public bool AllowAnalytics { get; set; } = true;
    public bool AllowCookies { get; set; } = true;
}

/// <summary>
/// Performance preferences
/// </summary>
public class PerformancePreferences
{
    public int CacheSize { get; set; } = 100;
    public bool EnableLazyLoading { get; set; } = true;
    public bool EnableImageOptimization { get; set; } = true;
    public int MaxConcurrentDownloads { get; set; } = 3;
    public bool EnableBackgroundSync { get; set; } = true;
    public int AutoSaveInterval { get; set; } = 30;
    public bool EnableCompression { get; set; } = true;
    public bool EnableCaching { get; set; } = true;
}

/// <summary>
/// Display mode enum
/// </summary>
public enum DisplayMode
{
    Grid,
    List,
    Card,
    Compact
}
