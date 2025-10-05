using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// User activity log entity - represents user activity tracking and analytics
/// </summary>
public class UserActivityLog : BaseEntity
{
    [BsonElement("userId")]
    public ObjectId UserId { get; private set; }

    [BsonElement("activityType")]
    public string ActivityType { get; private set; } = string.Empty; // Login, Logout, View, Download, Upload, etc.

    [BsonElement("action")]
    public string Action { get; private set; } = string.Empty; // Specific action taken

    [BsonElement("resourceType")]
    public string? ResourceType { get; private set; } // Collection, Image, User, etc.

    [BsonElement("resourceId")]
    public ObjectId? ResourceId { get; private set; }

    [BsonElement("description")]
    public string? Description { get; private set; }

    [BsonElement("ipAddress")]
    public string? IpAddress { get; private set; }

    [BsonElement("userAgent")]
    public string? UserAgent { get; private set; }

    [BsonElement("sessionId")]
    public string? SessionId { get; private set; }

    [BsonElement("requestId")]
    public string? RequestId { get; private set; }

    [BsonElement("duration")]
    public long? DurationMs { get; private set; }

    [BsonElement("success")]
    public bool Success { get; private set; } = true;

    [BsonElement("errorCode")]
    public string? ErrorCode { get; private set; }

    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; private set; }

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("location")]
    public Geolocation? Location { get; private set; }

    [BsonElement("deviceInfo")]
    public DeviceInfo? DeviceInfo { get; private set; }

    [BsonElement("activityDate")]
    public DateTime ActivityDate { get; private set; }

    [BsonElement("severity")]
    public string Severity { get; private set; } = "Info"; // Info, Warning, Error, Critical

    [BsonElement("category")]
    public string Category { get; private set; } = "General"; // Authentication, Navigation, Content, System

    // Navigation properties
    [BsonIgnore]
    public User User { get; private set; } = null!;

    // Private constructor for EF Core
    private UserActivityLog() { }

    public static UserActivityLog Create(ObjectId userId, string activityType, string action, string? resourceType = null, ObjectId? resourceId = null, string? description = null, string? ipAddress = null, string? userAgent = null, string? sessionId = null)
    {
        if (string.IsNullOrWhiteSpace(activityType))
            throw new ArgumentException("Activity type cannot be empty", nameof(activityType));

        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be empty", nameof(action));

        return new UserActivityLog
        {
            UserId = userId,
            ActivityType = activityType,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Description = description,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SessionId = sessionId,
            Success = true,
            ActivityDate = DateTime.UtcNow,
            Severity = "Info",
            Category = "General",
            Metadata = new Dictionary<string, object>()
        };
    }

    public void SetDuration(long durationMs)
    {
        DurationMs = durationMs;
        UpdateTimestamp();
    }

    public void SetError(string errorCode, string errorMessage)
    {
        Success = false;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Severity = "Error";
        UpdateTimestamp();
    }

    public void SetSuccess()
    {
        Success = true;
        ErrorCode = null;
        ErrorMessage = null;
        Severity = "Info";
        UpdateTimestamp();
    }

    public void SetLocation(Geolocation location)
    {
        Location = location;
        UpdateTimestamp();
    }

    public void SetDeviceInfo(DeviceInfo deviceInfo)
    {
        DeviceInfo = deviceInfo;
        UpdateTimestamp();
    }

    public void SetSeverity(string severity)
    {
        if (string.IsNullOrWhiteSpace(severity))
            throw new ArgumentException("Severity cannot be empty", nameof(severity));

        Severity = severity;
        UpdateTimestamp();
    }

    public void SetCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be empty", nameof(category));

        Category = category;
        UpdateTimestamp();
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }

    public void SetRequestInfo(string? requestId, string? sessionId, string? ipAddress, string? userAgent)
    {
        RequestId = requestId;
        SessionId = sessionId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        UpdateTimestamp();
    }
}

/// <summary>
/// Geolocation information for user activity
/// </summary>
public class Geolocation
{
    [BsonElement("country")]
    public string? Country { get; set; }

    [BsonElement("region")]
    public string? Region { get; set; }

    [BsonElement("city")]
    public string? City { get; set; }

    [BsonElement("latitude")]
    public double? Latitude { get; set; }

    [BsonElement("longitude")]
    public double? Longitude { get; set; }

    [BsonElement("timezone")]
    public string? Timezone { get; set; }

    [BsonElement("isp")]
    public string? ISP { get; set; }

    public static Geolocation Create(string? country = null, string? region = null, string? city = null, double? latitude = null, double? longitude = null, string? timezone = null, string? isp = null)
    {
        return new Geolocation
        {
            Country = country,
            Region = region,
            City = city,
            Latitude = latitude,
            Longitude = longitude,
            Timezone = timezone,
            ISP = isp
        };
    }
}

/// <summary>
/// Device information for user activity
/// </summary>
public class DeviceInfo
{
    [BsonElement("deviceType")]
    public string? DeviceType { get; set; } // Desktop, Mobile, Tablet

    [BsonElement("operatingSystem")]
    public string? OperatingSystem { get; set; }

    [BsonElement("browser")]
    public string? Browser { get; set; }

    [BsonElement("browserVersion")]
    public string? BrowserVersion { get; set; }

    [BsonElement("screenResolution")]
    public string? ScreenResolution { get; set; }

    [BsonElement("language")]
    public string? Language { get; set; }

    [BsonElement("isMobile")]
    public bool IsMobile { get; set; }

    [BsonElement("isBot")]
    public bool IsBot { get; set; }

    public static DeviceInfo Create(string? deviceType = null, string? operatingSystem = null, string? browser = null, string? browserVersion = null, string? screenResolution = null, string? language = null, bool isMobile = false, bool isBot = false)
    {
        return new DeviceInfo
        {
            DeviceType = deviceType,
            OperatingSystem = operatingSystem,
            Browser = browser,
            BrowserVersion = browserVersion,
            ScreenResolution = screenResolution,
            Language = language,
            IsMobile = isMobile,
            IsBot = isBot
        };
    }
}
