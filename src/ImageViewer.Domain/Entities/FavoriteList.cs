using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Favorite list entity - represents user's favorite collections
/// </summary>
public class FavoriteList : BaseEntity
{
    [BsonElement("userId")]
    public ObjectId UserId { get; private set; }

    [BsonElement("collectionId")]
    public ObjectId CollectionId { get; private set; }

    [BsonElement("listName")]
    public string ListName { get; private set; } = "Favorites";

    [BsonElement("notes")]
    public string? Notes { get; private set; }

    [BsonElement("sortOrder")]
    public int SortOrder { get; private set; }

    [BsonElement("isPublic")]
    public bool IsPublic { get; private set; }

    [BsonElement("tags")]
    public List<string> Tags { get; private set; } = new();

    // Navigation properties
    [BsonIgnore]
    public User User { get; private set; } = null!;
    
    [BsonIgnore]
    public Collection Collection { get; private set; } = null!;

    // Private constructor for EF Core
    private FavoriteList() { }

    public static FavoriteList Create(ObjectId userId, ObjectId collectionId, string? listName = null, string? notes = null, bool isPublic = false)
    {
        return new FavoriteList
        {
            UserId = userId,
            CollectionId = collectionId,
            ListName = listName ?? "Favorites",
            Notes = notes,
            SortOrder = 0,
            IsPublic = isPublic,
            Tags = new List<string>()
        };
    }

    public void UpdateListName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("List name cannot be empty", nameof(newName));

        ListName = newName;
        UpdateTimestamp();
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        UpdateTimestamp();
    }

    public void UpdateSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
        UpdateTimestamp();
    }

    public void ToggleVisibility()
    {
        IsPublic = !IsPublic;
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
}
