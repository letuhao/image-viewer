using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// ImageCacheInfo entity - represents cached image information
/// </summary>
public class ImageCacheInfo : BaseEntity
{
    public new Guid Id { get; private set; }
    public Guid ImageId { get; private set; }
    public string CachePath { get; private set; }
    public string Dimensions { get; private set; }
    public long FileSizeBytes { get; private set; }
    public DateTime CachedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsValid { get; private set; }

    // Navigation properties
    public Image Image { get; private set; } = null!;

    // Private constructor for EF Core
    private ImageCacheInfo() { }

    public ImageCacheInfo(Guid imageId, string cachePath, string dimensions, long fileSizeBytes, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        ImageId = imageId;
        CachePath = cachePath ?? throw new ArgumentNullException(nameof(cachePath));
        Dimensions = dimensions ?? throw new ArgumentNullException(nameof(dimensions));
        FileSizeBytes = fileSizeBytes;
        CachedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
        IsValid = true;
    }

    public void UpdateCachePath(string cachePath)
    {
        CachePath = cachePath ?? throw new ArgumentNullException(nameof(cachePath));
    }

    public void UpdateDimensions(string dimensions)
    {
        Dimensions = dimensions ?? throw new ArgumentNullException(nameof(dimensions));
    }

    public void UpdateFileSize(long fileSizeBytes)
    {
        if (fileSizeBytes < 0)
            throw new ArgumentException("File size cannot be negative", nameof(fileSizeBytes));

        FileSizeBytes = fileSizeBytes;
    }

    public void ExtendExpiration(DateTime newExpiresAt)
    {
        if (newExpiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Expiration date must be in the future", nameof(newExpiresAt));

        ExpiresAt = newExpiresAt;
    }

    public void MarkAsValid()
    {
        IsValid = true;
    }

    public void MarkAsInvalid()
    {
        IsValid = false;
    }

    public bool IsExpired()
    {
        return DateTime.UtcNow > ExpiresAt;
    }

    public bool IsStale(TimeSpan maxAge)
    {
        return DateTime.UtcNow - CachedAt > maxAge;
    }

    public TimeSpan GetAge()
    {
        return DateTime.UtcNow - CachedAt;
    }

    public TimeSpan GetTimeUntilExpiration()
    {
        return ExpiresAt - DateTime.UtcNow;
    }

    public bool ShouldRefresh(TimeSpan refreshThreshold)
    {
        return GetTimeUntilExpiration() <= refreshThreshold;
    }
}
