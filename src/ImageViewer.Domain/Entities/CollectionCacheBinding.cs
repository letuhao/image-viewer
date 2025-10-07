namespace ImageViewer.Domain.Entities;

/// <summary>
/// CollectionCacheBinding entity - represents the relationship between collections and cache folders
/// </summary>
public class CollectionCacheBinding : BaseEntity
{
    public Guid CollectionId { get; private set; }
    public Guid CacheFolderId { get; private set; }

    // Navigation properties
    public Collection Collection { get; private set; } = null!;
    public CacheFolder CacheFolder { get; private set; } = null!;

    // Private constructor for EF Core
    private CollectionCacheBinding() { }

    public CollectionCacheBinding(Guid collectionId, Guid cacheFolderId)
    {
        CollectionId = collectionId;
        CacheFolderId = cacheFolderId;
    }
}
