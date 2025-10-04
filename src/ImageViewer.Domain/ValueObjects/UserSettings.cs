using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// User settings value object
/// </summary>
public class UserSettings
{
    [BsonElement("displayMode")]
    public string DisplayMode { get; private set; }
    
    [BsonElement("itemsPerPage")]
    public int ItemsPerPage { get; private set; }
    
    [BsonElement("theme")]
    public string Theme { get; private set; }
    
    [BsonElement("language")]
    public string Language { get; private set; }
    
    [BsonElement("timezone")]
    public string Timezone { get; private set; }
    
    [BsonElement("notifications")]
    public NotificationSettings Notifications { get; private set; }
    
    [BsonElement("privacy")]
    public PrivacySettings Privacy { get; private set; }
    
    [BsonElement("performance")]
    public PerformanceSettings Performance { get; private set; }

    public UserSettings()
    {
        DisplayMode = "grid";
        ItemsPerPage = 20;
        Theme = "light";
        Language = "en";
        Timezone = "UTC";
        Notifications = new NotificationSettings();
        Privacy = new PrivacySettings();
        Performance = new PerformanceSettings();
    }

    public void UpdateDisplayMode(string displayMode)
    {
        if (string.IsNullOrWhiteSpace(displayMode))
            throw new ArgumentException("Display mode cannot be null or empty", nameof(displayMode));
        
        DisplayMode = displayMode;
    }

    public void UpdateItemsPerPage(int itemsPerPage)
    {
        if (itemsPerPage <= 0)
            throw new ArgumentException("Items per page must be greater than 0", nameof(itemsPerPage));
        
        ItemsPerPage = itemsPerPage;
    }

    public void UpdateTheme(string theme)
    {
        if (string.IsNullOrWhiteSpace(theme))
            throw new ArgumentException("Theme cannot be null or empty", nameof(theme));
        
        Theme = theme;
    }

    public void UpdateLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentException("Language cannot be null or empty", nameof(language));
        
        Language = language;
    }

    public void UpdateTimezone(string timezone)
    {
        if (string.IsNullOrWhiteSpace(timezone))
            throw new ArgumentException("Timezone cannot be null or empty", nameof(timezone));
        
        Timezone = timezone;
    }

    public void UpdateNotifications(NotificationSettings notifications)
    {
        Notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
    }

    public void UpdatePrivacy(PrivacySettings privacy)
    {
        Privacy = privacy ?? throw new ArgumentNullException(nameof(privacy));
    }

    public void UpdatePerformance(PerformanceSettings performance)
    {
        Performance = performance ?? throw new ArgumentNullException(nameof(performance));
    }
}

/// <summary>
/// Notification settings value object
/// </summary>
public class NotificationSettings
{
    [BsonElement("email")]
    public bool Email { get; private set; }
    
    [BsonElement("push")]
    public bool Push { get; private set; }
    
    [BsonElement("sms")]
    public bool Sms { get; private set; }
    
    [BsonElement("inApp")]
    public bool InApp { get; private set; }

    public NotificationSettings()
    {
        Email = true;
        Push = true;
        Sms = false;
        InApp = true;
    }

    public void UpdateEmail(bool enabled)
    {
        Email = enabled;
    }

    public void UpdatePush(bool enabled)
    {
        Push = enabled;
    }

    public void UpdateSms(bool enabled)
    {
        Sms = enabled;
    }

    public void UpdateInApp(bool enabled)
    {
        InApp = enabled;
    }
}

/// <summary>
/// Privacy settings value object
/// </summary>
public class PrivacySettings
{
    [BsonElement("profileVisibility")]
    public string ProfileVisibility { get; private set; }
    
    [BsonElement("activityVisibility")]
    public string ActivityVisibility { get; private set; }
    
    [BsonElement("dataSharing")]
    public bool DataSharing { get; private set; }
    
    [BsonElement("analytics")]
    public bool Analytics { get; private set; }

    public PrivacySettings()
    {
        ProfileVisibility = "public";
        ActivityVisibility = "public";
        DataSharing = false;
        Analytics = true;
    }

    public void UpdateProfileVisibility(string visibility)
    {
        if (string.IsNullOrWhiteSpace(visibility))
            throw new ArgumentException("Profile visibility cannot be null or empty", nameof(visibility));
        
        ProfileVisibility = visibility;
    }

    public void UpdateActivityVisibility(string visibility)
    {
        if (string.IsNullOrWhiteSpace(visibility))
            throw new ArgumentException("Activity visibility cannot be null or empty", nameof(visibility));
        
        ActivityVisibility = visibility;
    }

    public void UpdateDataSharing(bool enabled)
    {
        DataSharing = enabled;
    }

    public void UpdateAnalytics(bool enabled)
    {
        Analytics = enabled;
    }
}

/// <summary>
/// Performance settings value object
/// </summary>
public class PerformanceSettings
{
    [BsonElement("imageQuality")]
    public string ImageQuality { get; private set; }
    
    [BsonElement("videoQuality")]
    public string VideoQuality { get; private set; }
    
    [BsonElement("cacheSize")]
    public long CacheSize { get; private set; }
    
    [BsonElement("autoOptimize")]
    public bool AutoOptimize { get; private set; }

    public PerformanceSettings()
    {
        ImageQuality = "high";
        VideoQuality = "medium";
        CacheSize = 1024 * 1024 * 1024; // 1GB
        AutoOptimize = true;
    }

    public void UpdateImageQuality(string quality)
    {
        if (string.IsNullOrWhiteSpace(quality))
            throw new ArgumentException("Image quality cannot be null or empty", nameof(quality));
        
        ImageQuality = quality;
    }

    public void UpdateVideoQuality(string quality)
    {
        if (string.IsNullOrWhiteSpace(quality))
            throw new ArgumentException("Video quality cannot be null or empty", nameof(quality));
        
        VideoQuality = quality;
    }

    public void UpdateCacheSize(long size)
    {
        if (size < 0)
            throw new ArgumentException("Cache size cannot be negative", nameof(size));
        
        CacheSize = size;
    }

    public void UpdateAutoOptimize(bool enabled)
    {
        AutoOptimize = enabled;
    }
}
