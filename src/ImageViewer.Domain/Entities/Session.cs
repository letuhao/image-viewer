using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Session entity for user authentication sessions
/// </summary>
public class Session : BaseEntity
{
    [BsonElement("userId")]
    public ObjectId UserId { get; private set; }

    [BsonElement("deviceId")]
    public ObjectId DeviceId { get; private set; }

    [BsonElement("sessionToken")]
    public string SessionToken { get; private set; }

    [BsonElement("userAgent")]
    public string UserAgent { get; private set; }

    [BsonElement("ipAddress")]
    public string IpAddress { get; private set; }

    [BsonElement("location")]
    public string? Location { get; private set; }

    [BsonElement("isActive")]
    public bool IsActive { get; private set; }

    [BsonElement("lastActivity")]
    public DateTime LastActivity { get; private set; }

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; private set; }

    [BsonElement("terminatedAt")]
    public DateTime? TerminatedAt { get; private set; }

    [BsonElement("terminatedBy")]
    public ObjectId? TerminatedBy { get; private set; }

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; }

    // Private constructor for MongoDB
    private Session() 
    {
        Metadata = new Dictionary<string, object>();
    }

    public Session(ObjectId userId, ObjectId deviceId, string sessionToken, string userAgent, string ipAddress, 
                  string? location = null, DateTime? expiresAt = null)
    {
        UserId = userId;
        DeviceId = deviceId;
        SessionToken = sessionToken ?? throw new ArgumentNullException(nameof(sessionToken));
        UserAgent = userAgent ?? throw new ArgumentNullException(nameof(userAgent));
        IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
        Location = location;
        IsActive = true;
        LastActivity = DateTime.UtcNow;
        ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(30);
        TerminatedAt = null;
        TerminatedBy = null;
        Metadata = new Dictionary<string, object>();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateActivity()
    {
        LastActivity = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLocation(string? location)
    {
        Location = location;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Terminate(ObjectId? terminatedBy = null)
    {
        IsActive = false;
        TerminatedAt = DateTime.UtcNow;
        TerminatedBy = terminatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ExtendExpiry(DateTime newExpiryDate)
    {
        ExpiresAt = newExpiryDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsValid => IsActive && !IsExpired;
}
