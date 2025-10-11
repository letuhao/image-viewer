using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Conversation entity - represents messaging conversations between users
/// </summary>
public class Conversation : BaseEntity
{
    [BsonElement("participants")]
    public List<ObjectId> Participants { get; private set; } = new();

    [BsonElement("title")]
    public string? Title { get; private set; }

    [BsonElement("type")]
    public string Type { get; private set; } = "Direct"; // Direct, Group, Support, System

    [BsonElement("lastMessageId")]
    public ObjectId? LastMessageId { get; private set; }

    [BsonElement("lastMessageAt")]
    public DateTime? LastMessageAt { get; private set; }

    [BsonElement("lastMessagePreview")]
    public string? LastMessagePreview { get; private set; }

    [BsonElement("isArchived")]
    public bool IsArchived { get; private set; } = false;

    [BsonElement("isMuted")]
    public bool IsMuted { get; private set; } = false;

    [BsonElement("isReadOnly")]
    public bool IsReadOnly { get; private set; } = false;

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("messageCount")]
    public int MessageCount { get; private set; } = 0;

    [BsonElement("unreadCount")]
    public Dictionary<ObjectId, int> UnreadCount { get; private set; } = new();

    [BsonElement("createdBy")]
    public new ObjectId CreatedBy { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public List<User> ParticipantUsers { get; private set; } = new();
    
    [BsonIgnore]
    public UserMessage? LastMessage { get; private set; }

    [BsonIgnore]
    public User Creator { get; private set; } = null!;

    // Private constructor for EF Core
    private Conversation() { }

    public static Conversation Create(ObjectId createdBy, List<ObjectId> participants, string? title = null, string type = "Direct")
    {
        if (participants == null || !participants.Any())
            throw new ArgumentException("Participants cannot be empty", nameof(participants));

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type cannot be empty", nameof(type));

        return new Conversation
        {
            CreatedBy = createdBy,
            Participants = participants.ToList(),
            Title = title,
            Type = type,
            IsArchived = false,
            IsMuted = false,
            IsReadOnly = false,
            MessageCount = 0,
            Metadata = new Dictionary<string, object>(),
            UnreadCount = new Dictionary<ObjectId, int>()
        };
    }

    public void UpdateTitle(string? title)
    {
        Title = title;
        UpdateTimestamp();
    }

    public void AddParticipant(ObjectId userId)
    {
        if (!Participants.Contains(userId))
        {
            Participants.Add(userId);
            UnreadCount[userId] = 0;
            UpdateTimestamp();
        }
    }

    public void RemoveParticipant(ObjectId userId)
    {
        Participants.Remove(userId);
        UnreadCount.Remove(userId);
        UpdateTimestamp();
    }

    public void UpdateLastMessage(ObjectId messageId, string? preview)
    {
        LastMessageId = messageId;
        LastMessageAt = DateTime.UtcNow;
        LastMessagePreview = preview;
        MessageCount++;
        UpdateTimestamp();
    }

    public void IncrementUnreadCount(ObjectId userId)
    {
        if (UnreadCount.ContainsKey(userId))
        {
            UnreadCount[userId]++;
        }
        else
        {
            UnreadCount[userId] = 1;
        }
        UpdateTimestamp();
    }

    public void MarkAsRead(ObjectId userId)
    {
        UnreadCount[userId] = 0;
        UpdateTimestamp();
    }

    public void Archive()
    {
        IsArchived = true;
        UpdateTimestamp();
    }

    public void Unarchive()
    {
        IsArchived = false;
        UpdateTimestamp();
    }

    public void Mute()
    {
        IsMuted = true;
        UpdateTimestamp();
    }

    public void Unmute()
    {
        IsMuted = false;
        UpdateTimestamp();
    }

    public void SetReadOnly(bool readOnly)
    {
        IsReadOnly = readOnly;
        UpdateTimestamp();
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }
}
