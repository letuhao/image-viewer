using MongoDB.Bson;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// CollectionTag entity - represents the relationship between collections and tags
/// </summary>
public class CollectionTag : BaseEntity
{
    public ObjectId CollectionId { get; private set; }
    public ObjectId TagId { get; private set; }

    // Navigation properties
    public Collection Collection { get; private set; } = null!;
    public Tag Tag { get; private set; } = null!;

    // Private constructor for EF Core
    private CollectionTag() { }

    public CollectionTag(ObjectId collectionId, ObjectId tagId)
    {
        Id = ObjectId.GenerateNewId();
        CollectionId = collectionId;
        TagId = tagId;
        CreatedAt = DateTime.UtcNow;
    }
}
