using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ImageViewer.Domain.Events;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// UserBehaviorEvent - represents user behavior tracking events
/// </summary>
public class UserBehaviorEvent : BaseEntity
{
    [BsonElement("userId")]
    public ObjectId UserId { get; private set; }

    [BsonElement("eventType")]
    public string EventType { get; private set; } = string.Empty;

    [BsonElement("eventData")]
    public Dictionary<string, object> EventData { get; private set; } = new();

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; private set; }

    [BsonElement("sessionId")]
    public string? SessionId { get; private set; }

    [BsonElement("ipAddress")]
    public string? IpAddress { get; private set; }

    [BsonElement("userAgent")]
    public string? UserAgent { get; private set; }

    // Navigation properties
    public User User { get; private set; } = null!;

    // Private constructor for MongoDB
    private UserBehaviorEvent() { }

    public UserBehaviorEvent(ObjectId userId, string eventType, Dictionary<string, object> eventData)
    {
        UserId = userId;
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        EventData = eventData ?? throw new ArgumentNullException(nameof(eventData));
        Timestamp = DateTime.UtcNow;
    }

    public void SetSessionInfo(string sessionId, string? ipAddress, string? userAgent)
    {
        SessionId = sessionId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    public void AddEventData(string key, object value)
    {
        EventData[key] = value;
    }
}
