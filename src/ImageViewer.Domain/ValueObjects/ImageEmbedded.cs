using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

#pragma warning disable CS8618 // MongoDB entities/value objects are initialized by the driver

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Embedded image value object for MongoDB collections
/// </summary>
public class ImageEmbedded
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; private set; } = ObjectId.GenerateNewId().ToString();
    
    [BsonElement("filename")]
    public string Filename { get; private set; }
    
    [BsonElement("relativePath")]
    public string RelativePath { get; private set; }
    
    [BsonElement("fileSize")]
    public long FileSize { get; private set; }
    
    [BsonElement("width")]
    public int Width { get; private set; }
    
    [BsonElement("height")]
    public int Height { get; private set; }
    
    [BsonElement("format")]
    public string Format { get; private set; }
    
    [BsonElement("viewCount")]
    public int ViewCount { get; private set; }
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; private set; }
    
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; private set; }
    
    [BsonElement("isDeleted")]
    public bool IsDeleted { get; private set; }
    
    [BsonElement("deletedAt")]
    public DateTime? DeletedAt { get; private set; }
    
    [BsonElement("cacheInfo")]
    public ImageCacheInfoEmbedded? CacheInfo { get; private set; }
    
    [BsonElement("metadata")]
    public ImageMetadataEmbedded? Metadata { get; private set; }

    // Private constructor for MongoDB
    private ImageEmbedded() { }

    public ImageEmbedded(string filename, string relativePath, long fileSize, 
        int width, int height, string format)
    {
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

    public void UpdateMetadata(int width, int height, long fileSize)
    {
        Width = width;
        Height = height;
        FileSize = fileSize;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementViewCount()
    {
        ViewCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCacheInfo(ImageCacheInfoEmbedded? cacheInfo)
    {
        CacheInfo = cacheInfo;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearCacheInfo()
    {
        CacheInfo = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetMetadata(ImageMetadataEmbedded metadata)
    {
        Metadata = metadata;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsDeleted()
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

    /// <summary>
    /// Get full path for the image (resolves relative paths and handles ZIP entries)
    /// 获取图片的完整路径 - Lấy đường dẫn đầy đủ
    /// </summary>
    public string GetFullPath(string collectionPath)
    {
        if (string.IsNullOrEmpty(collectionPath))
        {
            return RelativePath;
        }

        // Handle ZIP entries (format: "archive.zip#entry.jpg")
        if (RelativePath.Contains("#"))
        {
            var parts = RelativePath.Split('#');
            var zipPath = parts[0];
            var entryName = parts.Length > 1 ? parts[1] : string.Empty;

            // If ZIP path is not rooted, combine with collection path
            if (!Path.IsPathRooted(zipPath))
            {
                zipPath = Path.Combine(collectionPath, zipPath);
            }

            return $"{zipPath}#{entryName}";
        }

        // Handle regular files
        if (!Path.IsPathRooted(RelativePath))
        {
            return Path.Combine(collectionPath, RelativePath);
        }

        return RelativePath;
    }
}