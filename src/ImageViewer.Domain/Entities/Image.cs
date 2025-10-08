using ImageViewer.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Image entity - represents a single image within a collection
/// OBSOLETE: Use embedded ImageEmbedded in Collection entity instead. This entity is kept only for CacheService backward compatibility.
/// </summary>
[Obsolete("Use embedded ImageEmbedded in Collection entity instead. Will be removed in future version.")]
public class Image : BaseEntity
{
    public ObjectId CollectionId { get; private set; }
    public string Filename { get; private set; }
    public string RelativePath { get; private set; }
    public long FileSize { get; private set; }
    public long FileSizeBytes => FileSize;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public string Format { get; private set; }
    public int ViewCount { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public Collection Collection { get; private set; } = null!;
    [BsonIgnore]
    public ImageCacheInfo? CacheInfo { get; private set; }
    [BsonIgnore]
    public ImageMetadataEntity? Metadata { get; private set; }
    
    // Collection of cache info for compatibility
    [BsonIgnore]
    public IEnumerable<ImageCacheInfo> CacheInfoCollection => CacheInfo != null ? new[] { CacheInfo } : Enumerable.Empty<ImageCacheInfo>();

    // Private constructor for EF Core
    private Image() { }

    public Image(ObjectId collectionId, string filename, string relativePath, long fileSize, 
        int width, int height, string format)
    {
        Id = ObjectId.GenerateNewId();
        CollectionId = collectionId;
        Filename = filename ?? throw new ArgumentNullException(nameof(filename));
        RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
        FileSize = fileSize;
        Width = width;
        Height = height;
        Format = format ?? throw new ArgumentNullException(nameof(format));
        ViewCount = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    public void SetMetadata(ImageMetadataEntity metadata)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDimensions(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentException("Width must be greater than 0", nameof(width));
        if (height <= 0)
            throw new ArgumentException("Height must be greater than 0", nameof(height));

        Width = width;
        Height = height;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateFileSize(long fileSize)
    {
        if (fileSize < 0)
            throw new ArgumentException("File size cannot be negative", nameof(fileSize));

        FileSize = fileSize;
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

    public void SetCacheInfo(ImageCacheInfo cacheInfo)
    {
        CacheInfo = cacheInfo ?? throw new ArgumentNullException(nameof(cacheInfo));
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearCacheInfo()
    {
        CacheInfo = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementViewCount()
    {
        ViewCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public double GetAspectRatio()
    {
        return Height > 0 ? (double)Width / Height : 0;
    }

    public bool IsLandscape()
    {
        return Width > Height;
    }

    public bool IsPortrait()
    {
        return Height > Width;
    }

    public bool IsSquare()
    {
        return Width == Height;
    }

    public string GetResolution()
    {
        return $"{Width}x{Height}";
    }

    public bool IsHighResolution()
    {
        return Width >= 1920 || Height >= 1080;
    }

    public bool IsLargeFile()
    {
        return FileSize > 10 * 1024 * 1024; // 10MB
    }

    public bool IsSupportedFormat()
    {
        var supportedFormats = new[] { "jpg", "jpeg", "png", "gif", "bmp", "webp", "tiff" };
        return supportedFormats.Contains(Format.ToLowerInvariant());
    }
}
