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
    
    [BsonElement("description")]
    public string? Description { get; private set; }
    
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
    
    [BsonElement("images")]
    public List<ImageEmbedded> Images { get; set; } = new();  // Must be public set for MongoDB driver!
    
    [BsonElement("thumbnails")]
    public List<ThumbnailEmbedded> Thumbnails { get; set; } = new();  // Must be public set for MongoDB driver!
    
    [BsonElement("cacheImages")]
    public List<CacheImageEmbedded> CacheImages { get; set; } = new();  // Must be public set for MongoDB driver!

    // Private constructor for MongoDB deserialization
    private Collection()
    {
        // MongoDB will populate these arrays during deserialization
        // Initialize to prevent null reference exceptions if arrays don't exist in DB
        Images = new List<ImageEmbedded>();
        Thumbnails = new List<ThumbnailEmbedded>();
        CacheImages = new List<CacheImageEmbedded>();
    }

    public Collection(ObjectId libraryId, string name, string path, CollectionType type, string? description = null, string? createdBy = null, string? createdBySystem = null)
    {
        LibraryId = libraryId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
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

    public void UpdateDescription(string? description)
    {
        Description = description;
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

    // Image management methods
    public void AddImage(ImageEmbedded image)
    {
        if (image == null) throw new ArgumentNullException(nameof(image));
        
        Images.Add(image);
        UpdateStatistics();
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveImage(string imageId)
    {
        var image = Images.FirstOrDefault(i => i.Id == imageId);
        if (image != null)
        {
            image.MarkAsDeleted();
            UpdateStatistics();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RestoreImage(string imageId)
    {
        var image = Images.FirstOrDefault(i => i.Id == imageId);
        if (image != null)
        {
            image.Restore();
            UpdateStatistics();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public ImageEmbedded? GetImage(string imageId)
    {
        return Images.FirstOrDefault(i => i.Id == imageId && !i.IsDeleted);
    }

    public List<ImageEmbedded> GetActiveImages()
    {
        return Images.Where(i => !i.IsDeleted).ToList();
    }

    public List<ImageEmbedded> GetDeletedImages()
    {
        return Images.Where(i => i.IsDeleted).ToList();
    }

    public void UpdateImageMetadata(string imageId, int width, int height, long fileSize)
    {
        var image = Images.FirstOrDefault(i => i.Id == imageId);
        if (image != null)
        {
            image.UpdateMetadata(width, height, fileSize);
            UpdateStatistics();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void SetImageCacheInfo(string imageId, ImageCacheInfoEmbedded cacheInfo)
    {
        var image = Images.FirstOrDefault(i => i.Id == imageId);
        if (image != null)
        {
            image.SetCacheInfo(cacheInfo);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void SetImageMetadata(string imageId, ImageMetadataEmbedded metadata)
    {
        var image = Images.FirstOrDefault(i => i.Id == imageId);
        if (image != null)
        {
            image.SetMetadata(metadata);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void IncrementImageViewCount(string imageId)
    {
        var image = Images.FirstOrDefault(i => i.Id == imageId);
        if (image != null)
        {
            image.IncrementViewCount();
            Statistics.IncrementViewCount();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    private void UpdateStatistics()
    {
        var activeImages = GetActiveImages();
        Statistics.UpdateItemCount(activeImages.Count);
        Statistics.UpdateTotalSize(activeImages.Sum(i => i.FileSize));
    }

    // Thumbnail management methods
    public void AddThumbnail(ThumbnailEmbedded thumbnail)
    {
        if (thumbnail == null) throw new ArgumentNullException(nameof(thumbnail));
        
        Thumbnails.Add(thumbnail);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveThumbnail(string thumbnailId)
    {
        var thumbnail = Thumbnails.FirstOrDefault(t => t.Id == thumbnailId);
        if (thumbnail != null)
        {
            Thumbnails.Remove(thumbnail);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public ThumbnailEmbedded? GetThumbnail(string thumbnailId)
    {
        return Thumbnails.FirstOrDefault(t => t.Id == thumbnailId);
    }

    public ThumbnailEmbedded? GetThumbnailForImage(string imageId)
    {
        return Thumbnails.FirstOrDefault(t => t.ImageId == imageId);
    }

    // Cache image management methods
    public void AddCacheImage(CacheImageEmbedded cacheImage)
    {
        if (cacheImage == null) throw new ArgumentNullException(nameof(cacheImage));
        
        CacheImages.Add(cacheImage);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveCacheImage(string cacheImageId)
    {
        var cacheImage = CacheImages.FirstOrDefault(c => c.Id == cacheImageId);
        if (cacheImage != null)
        {
            CacheImages.Remove(cacheImage);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public CacheImageEmbedded? GetCacheImage(string cacheImageId)
    {
        return CacheImages.FirstOrDefault(c => c.Id == cacheImageId);
    }

    public CacheImageEmbedded? GetCacheImageForImage(string imageId)
    {
        return CacheImages.FirstOrDefault(c => c.ImageId == imageId);
    }

    public List<ThumbnailEmbedded> GetThumbnailsForImages(List<string> imageIds)
    {
        return Thumbnails.Where(t => imageIds.Contains(t.ImageId)).ToList();
    }

    public void UpdateThumbnailAccess(string thumbnailId)
    {
        var thumbnail = Thumbnails.FirstOrDefault(t => t.Id == thumbnailId);
        if (thumbnail != null)
        {
            thumbnail.UpdateAccess();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void UpdateThumbnailInfo(string thumbnailId, string thumbnailPath, int width, int height, long fileSize)
    {
        var thumbnail = Thumbnails.FirstOrDefault(t => t.Id == thumbnailId);
        if (thumbnail != null)
        {
            thumbnail.UpdateThumbnailInfo(thumbnailPath, width, height, fileSize);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void MarkThumbnailAsInvalid(string thumbnailId)
    {
        var thumbnail = Thumbnails.FirstOrDefault(t => t.Id == thumbnailId);
        if (thumbnail != null)
        {
            thumbnail.MarkAsInvalid();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void MarkThumbnailAsValid(string thumbnailId)
    {
        var thumbnail = Thumbnails.FirstOrDefault(t => t.Id == thumbnailId);
        if (thumbnail != null)
        {
            thumbnail.MarkAsValid();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public List<ThumbnailEmbedded> GetValidThumbnails()
    {
        return Thumbnails.Where(t => t.IsValid).ToList();
    }

    public List<ThumbnailEmbedded> GetInvalidThumbnails()
    {
        return Thumbnails.Where(t => !t.IsValid).ToList();
    }

    public void CleanupInvalidThumbnails()
    {
        Thumbnails.RemoveAll(t => !t.IsValid);
        UpdatedAt = DateTime.UtcNow;
    }
}
