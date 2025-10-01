namespace ImageViewer.Domain.Entities;

/// <summary>
/// CollectionTag entity - represents the relationship between collections and tags
/// </summary>
public class CollectionTag : BaseEntity
{
    public Guid Id { get; private set; }
    public Guid CollectionId { get; private set; }
    public Guid TagId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public Collection Collection { get; private set; } = null!;
    public Tag Tag { get; private set; } = null!;

    // Private constructor for EF Core
    private CollectionTag() { }

    public CollectionTag(Guid collectionId, Guid tagId)
    {
        Id = Guid.NewGuid();
        CollectionId = collectionId;
        TagId = tagId;
        CreatedAt = DateTime.UtcNow;
    }
}
