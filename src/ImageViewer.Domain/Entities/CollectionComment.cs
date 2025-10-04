using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// CollectionComment - represents comments on collections
/// </summary>
public class CollectionComment : BaseEntity
{
    [BsonElement("collectionId")]
    public ObjectId CollectionId { get; private set; }

    [BsonElement("userId")]
    public ObjectId UserId { get; private set; }

    [BsonElement("content")]
    public string Content { get; private set; } = string.Empty;

    [BsonElement("parentCommentId")]
    public ObjectId? ParentCommentId { get; private set; }

    [BsonElement("isEdited")]
    public bool IsEdited { get; private set; }

    [BsonElement("editedAt")]
    public DateTime? EditedAt { get; private set; }

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; private set; }

    [BsonElement("deletedAt")]
    public DateTime? DeletedAt { get; private set; }

    [BsonElement("likeCount")]
    public int LikeCount { get; private set; }

    [BsonElement("replyCount")]
    public int ReplyCount { get; private set; }

    // Navigation properties
    public Collection Collection { get; private set; } = null!;
    public User User { get; private set; } = null!;
    public CollectionComment? ParentComment { get; private set; }

    // Private constructor for MongoDB
    private CollectionComment() { }

    public CollectionComment(ObjectId collectionId, ObjectId userId, string content, ObjectId? parentCommentId = null)
    {
        CollectionId = collectionId;
        UserId = userId;
        Content = content ?? throw new ArgumentNullException(nameof(content));
        ParentCommentId = parentCommentId;
        IsEdited = false;
        IsDeleted = false;
        LikeCount = 0;
        ReplyCount = 0;
    }

    public void EditContent(string newContent)
    {
        Content = newContent ?? throw new ArgumentNullException(nameof(newContent));
        IsEdited = true;
        EditedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementLikeCount()
    {
        LikeCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DecrementLikeCount()
    {
        if (LikeCount > 0)
        {
            LikeCount--;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void IncrementReplyCount()
    {
        ReplyCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DecrementReplyCount()
    {
        if (ReplyCount > 0)
        {
            ReplyCount--;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
