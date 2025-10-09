using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// CollectionCacheBinding entity - represents the relationship between collections and cache folders
/// </summary>
public class CollectionCacheBinding : BaseEntity
{
    [BsonElement("collectionId")]
    public Guid CollectionId { get; private set; }
    
    [BsonElement("cacheFolderId")]
    public Guid CacheFolderId { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public Collection Collection { get; private set; } = null!;
    
    [BsonIgnore]
    public CacheFolder CacheFolder { get; private set; } = null!;

    // Private constructor for EF Core
    private CollectionCacheBinding() { }

    public CollectionCacheBinding(Guid collectionId, Guid cacheFolderId)
    {
        CollectionId = collectionId;
        CacheFolderId = cacheFolderId;
    }
}
