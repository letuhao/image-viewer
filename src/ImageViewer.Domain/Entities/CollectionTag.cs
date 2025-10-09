using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// CollectionTag entity - represents the relationship between collections and tags
/// </summary>
public class CollectionTag : BaseEntity
{
    [BsonElement("collectionId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId CollectionId { get; private set; }
    
    [BsonElement("tagId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId TagId { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public Collection Collection { get; private set; } = null!;
    
    [BsonIgnore]
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
