using ImageViewer.Domain.Enums;
using ImageViewer.Domain.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

#pragma warning disable CS8618 // MongoDB entities are initialized by the driver

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Archived collection entity - represents a collection that was cleaned up due to non-existent path
/// </summary>
public class CollectionArchive : BaseEntity
{
    [BsonElement("originalId")]
    public ObjectId OriginalId { get; private set; }
    
    [BsonElement("libraryId")]
    public ObjectId? LibraryId { get; private set; }
    
    [BsonElement("name")]
    public string Name { get; private set; }
    
    [BsonElement("description")]
    public string? Description { get; private set; }
    
    [BsonElement("path")]
    public string Path { get; private set; }
    
    [BsonElement("type")]
    public CollectionType Type { get; private set; }
    
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
    
    [BsonElement("images")]
    public List<ImageEmbedded> Images { get; private set; } = new();
    
    [BsonElement("archiveReason")]
    public string ArchiveReason { get; private set; }
    
    [BsonElement("archivedAt")]
    public DateTime ArchivedAt { get; private set; }
    
    [BsonElement("archivedBy")]
    public string? ArchivedBy { get; private set; }

    // Private constructor for MongoDB
    private CollectionArchive() { }

    /// <summary>
    /// Create a new archived collection from an existing collection
    /// </summary>
    public CollectionArchive(Collection originalCollection, string archiveReason, string? archivedBy = null)
    {
        OriginalId = originalCollection.Id;
        LibraryId = originalCollection.LibraryId;
        Name = originalCollection.Name;
        Description = originalCollection.Description;
        Path = originalCollection.Path;
        Type = originalCollection.Type;
        Settings = originalCollection.Settings;
        Metadata = originalCollection.Metadata;
        Statistics = originalCollection.Statistics;
        WatchInfo = originalCollection.WatchInfo;
        SearchIndex = originalCollection.SearchIndex;
        CacheBindings = originalCollection.CacheBindings.ToList();
        Images = originalCollection.Images.ToList();
        ArchiveReason = archiveReason;
        ArchivedAt = DateTime.UtcNow;
        ArchivedBy = archivedBy;
        
        // Set base entity properties
        Id = ObjectId.GenerateNewId();
        CreatedAt = originalCollection.CreatedAt;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    /// <summary>
    /// Get the total number of images in this archived collection
    /// </summary>
    public int GetTotalImageCount()
    {
        return Images.Count;
    }

    /// <summary>
    /// Get the total number of active (non-deleted) images in this archived collection
    /// </summary>
    public int GetActiveImageCount()
    {
        return Images.Count(i => !i.IsDeleted);
    }

    /// <summary>
    /// Get the total size of all images in this archived collection
    /// </summary>
    public long GetTotalSize()
    {
        return Images.Where(i => !i.IsDeleted).Sum(i => i.FileSize);
    }
}
