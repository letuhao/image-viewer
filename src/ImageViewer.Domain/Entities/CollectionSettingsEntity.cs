using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Collection settings entity - represents settings for a collection
/// </summary>
public class CollectionSettingsEntity : BaseEntity
{
    [BsonElement("collectionId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId CollectionId { get; private set; }
    
    [BsonElement("totalImages")]
    public int TotalImages { get; private set; }
    
    [BsonElement("totalSizeBytes")]
    public long TotalSizeBytes { get; private set; }
    
    [BsonElement("thumbnailWidth")]
    public int ThumbnailWidth { get; private set; }
    
    [BsonElement("thumbnailHeight")]
    public int ThumbnailHeight { get; private set; }
    
    [BsonElement("cacheWidth")]
    public int CacheWidth { get; private set; }
    
    [BsonElement("cacheHeight")]
    public int CacheHeight { get; private set; }
    
    [BsonElement("autoGenerateThumbnails")]
    public bool AutoGenerateThumbnails { get; private set; }
    
    [BsonElement("autoGenerateCache")]
    public bool AutoGenerateCache { get; private set; }
    
    [BsonElement("cacheExpiration")]
    public TimeSpan CacheExpiration { get; private set; }
    
    [BsonElement("additionalSettingsJson")]
    public string AdditionalSettingsJson { get; private set; } = "{}";
    
    [BsonElement("deletedAt")]
    public DateTime? DeletedAt { get; private set; }

    // Navigation property
    [BsonIgnore]
    public Collection Collection { get; private set; } = null!;

    // Private constructor for EF Core
    private CollectionSettingsEntity() { }

    public CollectionSettingsEntity(
        ObjectId collectionId,
        int totalImages = 0,
        long totalSizeBytes = 0,
        int thumbnailWidth = 300,
        int thumbnailHeight = 300,
        int cacheWidth = 1920,
        int cacheHeight = 1080,
        bool autoGenerateThumbnails = true,
        bool autoGenerateCache = true,
        TimeSpan? cacheExpiration = null,
        string? additionalSettingsJson = null)
    {
        CollectionId = collectionId;
        TotalImages = totalImages;
        TotalSizeBytes = totalSizeBytes;
        ThumbnailWidth = thumbnailWidth;
        ThumbnailHeight = thumbnailHeight;
        CacheWidth = cacheWidth;
        CacheHeight = cacheHeight;
        AutoGenerateThumbnails = autoGenerateThumbnails;
        AutoGenerateCache = autoGenerateCache;
        CacheExpiration = cacheExpiration ?? TimeSpan.FromDays(30);
        AdditionalSettingsJson = additionalSettingsJson ?? "{}";
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    public void UpdateTotalImages(int totalImages)
    {
        if (totalImages < 0)
            throw new ArgumentException("Total images cannot be negative", nameof(totalImages));

        TotalImages = totalImages;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTotalSize(long totalSizeBytes)
    {
        if (totalSizeBytes < 0)
            throw new ArgumentException("Total size cannot be negative", nameof(totalSizeBytes));

        TotalSizeBytes = totalSizeBytes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateThumbnailSize(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentException("Thumbnail width must be greater than 0", nameof(width));
        if (height <= 0)
            throw new ArgumentException("Thumbnail height must be greater than 0", nameof(height));

        ThumbnailWidth = width;
        ThumbnailHeight = height;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCacheSize(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentException("Cache width must be greater than 0", nameof(width));
        if (height <= 0)
            throw new ArgumentException("Cache height must be greater than 0", nameof(height));

        CacheWidth = width;
        CacheHeight = height;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAutoGenerateThumbnails(bool enabled)
    {
        AutoGenerateThumbnails = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAutoGenerateCache(bool enabled)
    {
        AutoGenerateCache = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCacheExpiration(TimeSpan expiration)
    {
        if (expiration <= TimeSpan.Zero)
            throw new ArgumentException("Cache expiration must be greater than zero", nameof(expiration));

        CacheExpiration = expiration;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAdditionalSettings(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON cannot be null or empty", nameof(json));

        AdditionalSettingsJson = json;
        UpdatedAt = DateTime.UtcNow;
    }
}
