using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Events;
using ImageViewer.Domain.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Collection aggregate root - represents a collection of media items
/// </summary>
public class Collection : BaseEntity
{
    [BsonElement("libraryId")]
    public ObjectId LibraryId { get; private set; }
    
    [BsonElement("name")]
    public string Name { get; private set; }
    
    [BsonElement("path")]
    public string Path { get; private set; }
    
    [BsonElement("type")]
    public CollectionType Type { get; private set; }
    
    [BsonElement("isActive")]
    public bool IsActive { get; private set; }
    
    [BsonElement("settings")]
    public CollectionSettings Settings { get; private set; }
    
    [BsonElement("metadata")]
    public CollectionMetadata Metadata { get; private set; }
    
    [BsonElement("statistics")]
    public CollectionStatistics Statistics { get; private set; }
    
    [BsonElement("watchInfo")]
    public WatchInfo WatchInfo { get; private set; }
    
    [BsonElement("searchIndex")]
    public SearchIndex SearchIndex { get; private set; }

    // Private constructor for MongoDB
    private Collection() { }

    public Collection(ObjectId libraryId, string name, string path, CollectionType type)
    {
        LibraryId = libraryId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Type = type;
        IsActive = true;
        
        Settings = new CollectionSettings();
        Metadata = new CollectionMetadata();
        Statistics = new CollectionStatistics();
        WatchInfo = new WatchInfo();
        SearchIndex = new SearchIndex();
        
        AddDomainEvent(new CollectionCreatedEvent(Id, Name, LibraryId));
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
