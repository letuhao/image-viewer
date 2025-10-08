using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// ThumbnailInfo entity - represents thumbnail information for an image
/// OBSOLETE: Use embedded ThumbnailEmbedded in Collection entity instead. This entity is kept only for backward compatibility.
/// </summary>
[Obsolete("Use embedded ThumbnailEmbedded in Collection entity instead. Will be removed in future version.")]
public class ThumbnailInfo : BaseEntity
{
    public ObjectId ImageId { get; private set; }
    public string ThumbnailPath { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public long FileSizeBytes { get; private set; }
    public DateTime GeneratedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsValid { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public Image Image { get; private set; } = null!;

    // Private constructor for EF Core
    private ThumbnailInfo() { }

    public ThumbnailInfo(ObjectId imageId, string thumbnailPath, int width, int height, long fileSizeBytes, DateTime expiresAt)
    {
        if (imageId == ObjectId.Empty)
            throw new ArgumentException("ImageId cannot be empty", nameof(imageId));
        if (string.IsNullOrWhiteSpace(thumbnailPath))
            throw new ArgumentException("ThumbnailPath cannot be null or empty", nameof(thumbnailPath));
        if (width <= 0)
            throw new ArgumentException("Width must be greater than 0", nameof(width));
        if (height <= 0)
            throw new ArgumentException("Height must be greater than 0", nameof(height));
        if (fileSizeBytes < 0)
            throw new ArgumentException("FileSizeBytes cannot be negative", nameof(fileSizeBytes));
        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("ExpiresAt must be in the future", nameof(expiresAt));

        Id = ObjectId.GenerateNewId();
        ImageId = imageId;
        ThumbnailPath = thumbnailPath;
        Width = width;
        Height = height;
        FileSizeBytes = fileSizeBytes;
        GeneratedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
        IsValid = true;
    }

    public void UpdateThumbnailPath(string thumbnailPath)
    {
        ThumbnailPath = thumbnailPath ?? throw new ArgumentNullException(nameof(thumbnailPath));
    }

    public void UpdateDimensions(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentException("Width must be greater than 0", nameof(width));
        if (height <= 0)
            throw new ArgumentException("Height must be greater than 0", nameof(height));

        Width = width;
        Height = height;
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
        return DateTime.UtcNow - GeneratedAt > maxAge;
    }

    public TimeSpan GetAge()
    {
        return DateTime.UtcNow - GeneratedAt;
    }

    public TimeSpan GetTimeUntilExpiration()
    {
        return ExpiresAt - DateTime.UtcNow;
    }

    public bool ShouldRefresh(TimeSpan refreshThreshold)
    {
        return GetTimeUntilExpiration() <= refreshThreshold;
    }

    public string GetDimensionsString()
    {
        return $"{Width}x{Height}";
    }
}
