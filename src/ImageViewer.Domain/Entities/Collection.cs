using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Events;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Collection aggregate root - represents a collection of images
/// </summary>
public class Collection : BaseEntity
{
    [BsonId]
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Path { get; private set; }
    public CollectionType Type { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // Navigation properties
    [BsonIgnore]
    private readonly List<Image> _images = new();
    [BsonIgnore]
    public IReadOnlyCollection<Image> Images => _images.AsReadOnly();

    [BsonIgnore]
    private readonly List<CollectionTag> _tags = new();
    [BsonIgnore]
    public IReadOnlyCollection<CollectionTag> Tags => _tags.AsReadOnly();

    [BsonIgnore]
    private readonly List<CollectionCacheBinding> _cacheBindings = new();
    [BsonIgnore]
    public IReadOnlyCollection<CollectionCacheBinding> CacheBindings => _cacheBindings.AsReadOnly();

    public CollectionStatistics? Statistics { get; private set; }
    public CollectionSettingsEntity? Settings { get; private set; }

    // Private constructor for EF Core
    private Collection() { }

    public Collection(string name, string path, CollectionType type)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Type = type;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;
        
        AddDomainEvent(new CollectionCreatedEvent(this));
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        Path = path;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSettings(CollectionSettingsEntity settings)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddImage(Image image)
    {
        if (image == null)
            throw new ArgumentNullException(nameof(image));

        if (_images.Any(i => i.Filename == image.Filename))
            throw new InvalidOperationException($"Image with filename '{image.Filename}' already exists in collection");

        _images.Add(image);
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new ImageAddedEvent(image, this));
    }

    public void RemoveImage(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image == null)
            throw new InvalidOperationException($"Image with ID '{imageId}' not found in collection");

        _images.Remove(image);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddTag(CollectionTag tag)
    {
        if (tag == null)
            throw new ArgumentNullException(nameof(tag));

        if (_tags.Any(t => t.TagId == tag.TagId))
            throw new InvalidOperationException($"Tag with ID '{tag.TagId}' already exists in collection");

        _tags.Add(tag);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveTag(Guid tagId)
    {
        var tag = _tags.FirstOrDefault(t => t.TagId == tagId);
        if (tag == null)
            throw new InvalidOperationException($"Tag with ID '{tagId}' not found in collection");

        _tags.Remove(tag);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public int GetImageCount()
    {
        return _images.Count;
    }

    public long GetTotalSize()
    {
        return _images.Sum(i => i.FileSize);
    }

    public IEnumerable<Image> GetImagesByFormat(string format)
    {
        return _images.Where(i => i.Format.Equals(format, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<Image> GetImagesBySizeRange(int minWidth, int minHeight)
    {
        return _images.Where(i => i.Width >= minWidth && i.Height >= minHeight);
    }
}
