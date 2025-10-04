using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// UserCollection - represents user's personal collections
/// </summary>
public class UserCollection : BaseEntity
{
    [BsonElement("userId")]
    public ObjectId UserId { get; private set; }

    [BsonElement("collectionId")]
    public ObjectId CollectionId { get; private set; }

    [BsonElement("collectionType")]
    public string CollectionType { get; private set; } = string.Empty;

    [BsonElement("isPublic")]
    public bool IsPublic { get; private set; }

    [BsonElement("isFavorite")]
    public bool IsFavorite { get; private set; }

    [BsonElement("accessLevel")]
    public string AccessLevel { get; private set; } = "Read";

    [BsonElement("addedDate")]
    public DateTime AddedDate { get; private set; }

    [BsonElement("lastAccessedDate")]
    public DateTime? LastAccessedDate { get; private set; }

    [BsonElement("accessCount")]
    public long AccessCount { get; private set; }

    [BsonElement("tags")]
    public List<string> Tags { get; private set; } = new();

    [BsonElement("notes")]
    public string? Notes { get; private set; }

    [BsonElement("rating")]
    public int? Rating { get; private set; }

    // Navigation properties
    public User User { get; private set; } = null!;
    public Collection Collection { get; private set; } = null!;

    // Private constructor for MongoDB
    private UserCollection() { }

    public UserCollection(ObjectId userId, ObjectId collectionId, string collectionType)
    {
        UserId = userId;
        CollectionId = collectionId;
        CollectionType = collectionType ?? throw new ArgumentNullException(nameof(collectionType));
        IsPublic = false;
        IsFavorite = false;
        AccessLevel = "Read";
        AddedDate = DateTime.UtcNow;
        AccessCount = 0;
    }

    public void SetPublic(bool isPublic)
    {
        IsPublic = isPublic;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetFavorite(bool isFavorite)
    {
        IsFavorite = isFavorite;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAccessLevel(string accessLevel)
    {
        AccessLevel = accessLevel ?? throw new ArgumentNullException(nameof(accessLevel));
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordAccess()
    {
        AccessCount++;
        LastAccessedDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddTag(string tag)
    {
        if (!Tags.Contains(tag))
        {
            Tags.Add(tag);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveTag(string tag)
    {
        if (Tags.Contains(tag))
        {
            Tags.Remove(tag);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void SetNotes(string? notes)
    {
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRating(int? rating)
    {
        if (rating.HasValue && (rating < 1 || rating > 5))
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5");
        
        Rating = rating;
        UpdatedAt = DateTime.UtcNow;
    }
}
