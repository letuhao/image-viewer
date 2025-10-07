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
    
    [BsonElement("cacheBindings")]
    public List<CacheBinding> CacheBindings { get; private set; } = new();

    // Private constructor for MongoDB
    private Collection() { }

    public Collection(ObjectId libraryId, string name, string path, CollectionType type, string? createdBy = null, string? createdBySystem = null)
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
        
        // Set creator information
        SetCreator(createdBy, createdBySystem);
        
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

    public void UpdateSettings(CollectionSettings settings)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateMetadata(CollectionMetadata metadata)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStatistics(CollectionStatistics statistics)
    {
        Statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void EnableWatching()
    {
        WatchInfo.EnableWatching();
        UpdatedAt = DateTime.UtcNow;
    }

    public void DisableWatching()
    {
        WatchInfo.DisableWatching();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateType(CollectionType newType)
    {
        Type = newType;
        UpdatedAt = DateTime.UtcNow;
    }

    public long GetImageCount()
    {
        return Statistics.TotalItems;
    }

    public long GetTotalSize()
    {
        return Statistics.TotalSize;
    }
}
