using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// UserFollow - represents user following relationships
/// </summary>
public class UserFollow : BaseEntity
{
    [BsonElement("followerId")]
    public ObjectId FollowerId { get; private set; }

    [BsonElement("followingId")]
    public ObjectId FollowingId { get; private set; }

    [BsonElement("followDate")]
    public DateTime FollowDate { get; private set; }

    [BsonElement("isActive")]
    public bool IsActive { get; private set; }

    [BsonElement("notificationEnabled")]
    public bool NotificationEnabled { get; private set; }

    [BsonElement("lastNotificationSent")]
    public DateTime? LastNotificationSent { get; private set; }

    // Navigation properties
    public User Follower { get; private set; } = null!;
    public User Following { get; private set; } = null!;

    // Private constructor for MongoDB
    private UserFollow() { }

    public UserFollow(ObjectId followerId, ObjectId followingId)
    {
        FollowerId = followerId;
        FollowingId = followingId;
        FollowDate = DateTime.UtcNow;
        IsActive = true;
        NotificationEnabled = true;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetNotificationEnabled(bool notificationEnabled)
    {
        NotificationEnabled = notificationEnabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordNotificationSent()
    {
        LastNotificationSent = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
