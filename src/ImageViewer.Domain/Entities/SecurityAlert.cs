using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ImageViewer.Domain.Enums;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Security alert entity - represents security alerts for users
/// </summary>
public class SecurityAlert : BaseEntity
{
    [BsonElement("userId")]
    public ObjectId UserId { get; private set; }

    [BsonElement("alertType")]
    public SecurityAlertType AlertType { get; private set; }

    [BsonElement("title")]
    public string Title { get; private set; } = string.Empty;

    [BsonElement("message")]
    public string Message { get; private set; } = string.Empty;

    [BsonElement("severity")]
    public string Severity { get; private set; } = "Medium"; // Low, Medium, High, Critical

    [BsonElement("isRead")]
    public bool IsRead { get; private set; } = false;

    [BsonElement("readAt")]
    public DateTime? ReadAt { get; private set; }

    [BsonElement("isResolved")]
    public bool IsResolved { get; private set; } = false;

    [BsonElement("resolvedAt")]
    public DateTime? ResolvedAt { get; private set; }

    [BsonElement("resolvedBy")]
    public ObjectId? ResolvedBy { get; private set; }

    [BsonElement("source")]
    public string Source { get; private set; } = "System"; // System, User, Admin, API

    [BsonElement("ipAddress")]
    public string? IpAddress { get; private set; }

    [BsonElement("userAgent")]
    public string? UserAgent { get; private set; }

    [BsonElement("location")]
    public string? Location { get; private set; }

    [BsonElement("deviceInfo")]
    public string? DeviceInfo { get; private set; }

    [BsonElement("additionalData")]
    public Dictionary<string, object> AdditionalData { get; private set; } = new();

    [BsonElement("tags")]
    public List<string> Tags { get; private set; } = new();

    [BsonElement("priority")]
    public int Priority { get; private set; } = 0; // 0 = Low, 1 = Medium, 2 = High, 3 = Critical

    [BsonElement("expiresAt")]
    public DateTime? ExpiresAt { get; private set; }

    [BsonElement("actionRequired")]
    public bool ActionRequired { get; private set; } = false;

    [BsonElement("actionTaken")]
    public string? ActionTaken { get; private set; }

    [BsonElement("actionTakenAt")]
    public DateTime? ActionTakenAt { get; private set; }

    [BsonElement("actionTakenBy")]
    public ObjectId? ActionTakenBy { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public User User { get; private set; } = null!;

    [BsonIgnore]
    public User? Resolver { get; private set; }

    [BsonIgnore]
    public User? ActionTaker { get; private set; }

    // Private constructor for EF Core
    private SecurityAlert() { }

    public static SecurityAlert Create(ObjectId userId, SecurityAlertType alertType, string title, string message, string severity = "Medium", string source = "System", ObjectId? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty", nameof(message));

        if (string.IsNullOrWhiteSpace(severity))
            throw new ArgumentException("Severity cannot be empty", nameof(severity));

        var priority = severity switch
        {
            "Low" => 0,
            "Medium" => 1,
            "High" => 2,
            "Critical" => 3,
            _ => 1
        };

        return new SecurityAlert
        {
            Id = ObjectId.GenerateNewId(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UserId = userId,
            AlertType = alertType,
            Title = title,
            Message = message,
            Severity = severity,
            Priority = priority,
            Source = source,
            IsRead = false,
            IsResolved = false,
            ActionRequired = priority >= 2, // High and Critical alerts require action
            AdditionalData = new Dictionary<string, object>(),
            Tags = new List<string>()
        };
    }

    public void MarkAsRead(ObjectId? readBy = null)
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
            UpdateTimestamp();
        }
    }

    public void MarkAsUnread()
    {
        if (IsRead)
        {
            IsRead = false;
            ReadAt = null;
            UpdateTimestamp();
        }
    }

    public void Resolve(ObjectId resolvedBy, string? resolution = null)
    {
        if (!IsResolved)
        {
            IsResolved = true;
            ResolvedAt = DateTime.UtcNow;
            ResolvedBy = resolvedBy;
            ActionTaken = resolution;
            ActionTakenAt = DateTime.UtcNow;
            ActionTakenBy = resolvedBy;
            UpdateTimestamp();
        }
    }

    public void Unresolve()
    {
        if (IsResolved)
        {
            IsResolved = false;
            ResolvedAt = null;
            ResolvedBy = null;
            ActionTaken = null;
            ActionTakenAt = null;
            ActionTakenBy = null;
            UpdateTimestamp();
        }
    }

    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));

        Title = title;
        UpdateTimestamp();
    }

    public void UpdateMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty", nameof(message));

        Message = message;
        UpdateTimestamp();
    }

    public void UpdateSeverity(string severity)
    {
        if (string.IsNullOrWhiteSpace(severity))
            throw new ArgumentException("Severity cannot be empty", nameof(severity));

        Severity = severity;
        Priority = severity switch
        {
            "Low" => 0,
            "Medium" => 1,
            "High" => 2,
            "Critical" => 3,
            _ => 1
        };
        ActionRequired = Priority >= 2;
        UpdateTimestamp();
    }

    public void SetExpiration(DateTime? expiresAt)
    {
        ExpiresAt = expiresAt;
        UpdateTimestamp();
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty", nameof(tag));

        if (!Tags.Contains(tag))
        {
            Tags.Add(tag);
            UpdateTimestamp();
        }
    }

    public void RemoveTag(string tag)
    {
        Tags.Remove(tag);
        UpdateTimestamp();
    }

    public void AddAdditionalData(string key, object value)
    {
        AdditionalData[key] = value;
        UpdateTimestamp();
    }

    public void SetLocationInfo(string? ipAddress, string? userAgent, string? location, string? deviceInfo)
    {
        IpAddress = ipAddress;
        UserAgent = userAgent;
        Location = location;
        DeviceInfo = deviceInfo;
        UpdateTimestamp();
    }

    public void TakeAction(string action, ObjectId actionTaker)
    {
        ActionTaken = action;
        ActionTakenAt = DateTime.UtcNow;
        ActionTakenBy = actionTaker;
        UpdateTimestamp();
    }

    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    }

    public bool IsHighPriority()
    {
        return Priority >= 2;
    }

    public bool IsCritical()
    {
        return Priority >= 3;
    }

    public bool NeedsAttention()
    {
        return !IsResolved && (IsHighPriority() || ActionRequired);
    }

    public string GetPriorityText()
    {
        return Priority switch
        {
            0 => "Low",
            1 => "Medium",
            2 => "High",
            3 => "Critical",
            _ => "Unknown"
        };
    }
}
