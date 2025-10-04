using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ImageViewer.Domain.Events;
using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// MediaItem aggregate root - represents a media file (image, video, etc.)
/// </summary>
public class MediaItem : BaseEntity
{
    [BsonElement("collectionId")]
    public ObjectId CollectionId { get; private set; }
    
    [BsonElement("name")]
    public string Name { get; private set; }
    
    [BsonElement("filename")]
    public string Filename { get; private set; }
    
    [BsonElement("path")]
    public string Path { get; private set; }
    
    [BsonElement("type")]
    public string Type { get; private set; }
    
    [BsonElement("format")]
    public string Format { get; private set; }
    
    [BsonElement("fileSize")]
    public long FileSize { get; private set; }
    
    [BsonElement("width")]
    public int Width { get; private set; }
    
    [BsonElement("height")]
    public int Height { get; private set; }
    
    [BsonElement("duration")]
    public TimeSpan? Duration { get; private set; }
    
    [BsonElement("isActive")]
    public bool IsActive { get; private set; }
    
    [BsonElement("metadata")]
    public MediaMetadata Metadata { get; private set; }
    
    [BsonElement("cacheInfo")]
    public CacheInfo CacheInfo { get; private set; }
    
    [BsonElement("statistics")]
    public MediaStatistics Statistics { get; private set; }
    
    [BsonElement("searchIndex")]
    public SearchIndex SearchIndex { get; private set; }

    // Private constructor for MongoDB
    private MediaItem() { }

    public MediaItem(ObjectId collectionId, string name, string filename, string path, string type, string format, 
        long fileSize, int width, int height, TimeSpan? duration = null)
    {
        CollectionId = collectionId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Filename = filename ?? throw new ArgumentNullException(nameof(filename));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Format = format ?? throw new ArgumentNullException(nameof(format));
        FileSize = fileSize;
        Width = width;
        Height = height;
        Duration = duration;
        IsActive = true;
        
        Metadata = new MediaMetadata();
        CacheInfo = new CacheInfo();
        Statistics = new MediaStatistics();
        SearchIndex = new SearchIndex();
        
        AddDomainEvent(new MediaItemCreatedEvent(Id, Name, CollectionId));
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name cannot be null or empty", nameof(newName));
        
        Name = newName;
        UpdateTimestamp();
        
        AddDomainEvent(new MediaItemNameChangedEvent(Id, newName));
    }

    public void UpdateFilename(string newFilename)
    {
        if (string.IsNullOrWhiteSpace(newFilename))
            throw new ArgumentException("Filename cannot be null or empty", nameof(newFilename));
        
        Filename = newFilename;
        UpdateTimestamp();
        
        AddDomainEvent(new MediaItemFilenameChangedEvent(Id, newFilename));
    }

    public void UpdatePath(string newPath)
    {
        if (string.IsNullOrWhiteSpace(newPath))
            throw new ArgumentException("Path cannot be null or empty", nameof(newPath));
        
        Path = newPath;
        UpdateTimestamp();
        
        AddDomainEvent(new MediaItemPathChangedEvent(Id, newPath));
    }

    public void UpdateDimensions(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentException("Width must be greater than 0", nameof(width));
        
        if (height <= 0)
            throw new ArgumentException("Height must be greater than 0", nameof(height));
        
        Width = width;
        Height = height;
        UpdateTimestamp();
        
        AddDomainEvent(new MediaItemDimensionsChangedEvent(Id, width, height));
    }

    public void UpdateDuration(TimeSpan? duration)
    {
        Duration = duration;
        UpdateTimestamp();
        
        AddDomainEvent(new MediaItemDurationChangedEvent(Id, duration));
    }

    public void Activate()
    {
        if (!IsActive)
        {
            IsActive = true;
            UpdateTimestamp();
            
            AddDomainEvent(new MediaItemActivatedEvent(Id));
        }
    }

    public void Deactivate()
    {
        if (IsActive)
        {
            IsActive = false;
            UpdateTimestamp();
            
            AddDomainEvent(new MediaItemDeactivatedEvent(Id));
        }
    }

    public void UpdateMetadata(MediaMetadata newMetadata)
    {
        Metadata = newMetadata ?? throw new ArgumentNullException(nameof(newMetadata));
        UpdateTimestamp();
        
        AddDomainEvent(new MediaItemMetadataUpdatedEvent(Id));
    }

    public void UpdateCacheInfo(CacheInfo newCacheInfo)
    {
        CacheInfo = newCacheInfo ?? throw new ArgumentNullException(nameof(newCacheInfo));
        UpdateTimestamp();
        
        AddDomainEvent(new MediaItemCacheInfoUpdatedEvent(Id));
    }

    public void UpdateStatistics(MediaStatistics newStatistics)
    {
        Statistics = newStatistics ?? throw new ArgumentNullException(nameof(newStatistics));
        UpdateTimestamp();
    }

    public void UpdateSearchIndex(SearchIndex newSearchIndex)
    {
        SearchIndex = newSearchIndex ?? throw new ArgumentNullException(nameof(newSearchIndex));
        UpdateTimestamp();
    }
}
