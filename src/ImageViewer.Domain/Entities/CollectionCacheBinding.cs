namespace ImageViewer.Domain.Entities;

/// <summary>
/// CollectionCacheBinding entity - represents the relationship between collections and cache folders
/// </summary>
public class CollectionCacheBinding : BaseEntity
{
    public new Guid Id { get; private set; }
    public Guid CollectionId { get; private set; }
    public Guid CacheFolderId { get; private set; }
    public new DateTime CreatedAt { get; private set; }

    // Navigation properties
    public Collection Collection { get; private set; } = null!;
    public CacheFolder CacheFolder { get; private set; } = null!;

    // Private constructor for EF Core
    private CollectionCacheBinding() { }

    public CollectionCacheBinding(Guid collectionId, Guid cacheFolderId)
    {
        Id = Guid.NewGuid();
        CollectionId = collectionId;
        CacheFolderId = cacheFolderId;
        CreatedAt = DateTime.UtcNow;
    }
}
