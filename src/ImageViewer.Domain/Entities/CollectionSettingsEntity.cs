using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Collection settings entity - represents settings for a collection
/// </summary>
public class CollectionSettingsEntity : BaseEntity
{
    public new Guid Id { get; private set; }
    public Guid CollectionId { get; private set; }
    public int TotalImages { get; private set; }
    public long TotalSizeBytes { get; private set; }
    public int ThumbnailWidth { get; private set; }
    public int ThumbnailHeight { get; private set; }
    public int CacheWidth { get; private set; }
    public int CacheHeight { get; private set; }
    public bool AutoGenerateThumbnails { get; private set; }
    public bool AutoGenerateCache { get; private set; }
    public TimeSpan CacheExpiration { get; private set; }
    public string AdditionalSettingsJson { get; private set; } = "{}";
    public new DateTime CreatedAt { get; private set; }
    public new DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // Navigation property
    public Collection Collection { get; private set; } = null!;

    // Private constructor for EF Core
    private CollectionSettingsEntity() { }

    public CollectionSettingsEntity(
        Guid collectionId,
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
        Id = Guid.NewGuid();
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
