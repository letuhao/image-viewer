using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// UserMessage - represents private messages between users
/// </summary>
public class UserMessage : BaseEntity
{
    [BsonElement("senderId")]
    public ObjectId SenderId { get; private set; }

    [BsonElement("recipientId")]
    public ObjectId RecipientId { get; private set; }

    [BsonElement("subject")]
    public string? Subject { get; private set; }

    [BsonElement("content")]
    public string Content { get; private set; } = string.Empty;

    [BsonElement("isRead")]
    public bool IsRead { get; private set; }

    [BsonElement("readAt")]
    public DateTime? ReadAt { get; private set; }

    [BsonElement("isDeletedBySender")]
    public bool IsDeletedBySender { get; private set; }

    [BsonElement("isDeletedByRecipient")]
    public bool IsDeletedByRecipient { get; private set; }

    [BsonElement("deletedAt")]
    public DateTime? DeletedAt { get; private set; }

    [BsonElement("replyToMessageId")]
    public ObjectId? ReplyToMessageId { get; private set; }

    [BsonElement("attachmentIds")]
    public List<ObjectId> AttachmentIds { get; private set; } = new();

    // Navigation properties
    public User Sender { get; private set; } = null!;
    public User Recipient { get; private set; } = null!;
    public UserMessage? ReplyToMessage { get; private set; }

    // Private constructor for MongoDB
    private UserMessage() { }

    public UserMessage(ObjectId senderId, ObjectId recipientId, string content, string? subject = null, ObjectId? replyToMessageId = null)
    {
        SenderId = senderId;
        RecipientId = recipientId;
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Subject = subject;
        ReplyToMessageId = replyToMessageId;
        IsRead = false;
        IsDeletedBySender = false;
        IsDeletedByRecipient = false;
    }

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DeleteBySender()
    {
        IsDeletedBySender = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DeleteByRecipient()
    {
        IsDeletedByRecipient = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddAttachment(ObjectId attachmentId)
    {
        if (!AttachmentIds.Contains(attachmentId))
        {
            AttachmentIds.Add(attachmentId);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveAttachment(ObjectId attachmentId)
    {
        if (AttachmentIds.Contains(attachmentId))
        {
            AttachmentIds.Remove(attachmentId);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public bool IsDeleted()
    {
        return IsDeletedBySender || IsDeletedByRecipient;
    }
}
