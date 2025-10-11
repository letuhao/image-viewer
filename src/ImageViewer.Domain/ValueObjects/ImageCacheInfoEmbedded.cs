using MongoDB.Bson.Serialization.Attributes;

#pragma warning disable CS8618 // MongoDB entities/value objects are initialized by the driver

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Embedded image cache info value object for MongoDB
/// </summary>
public class ImageCacheInfoEmbedded
{
    [BsonElement("cachePath")]
    public string CachePath { get; private set; }
    
    [BsonElement("cacheSize")]
    public long CacheSize { get; private set; }
    
    [BsonElement("cacheFormat")]
    public string CacheFormat { get; private set; }
    
    [BsonElement("cacheWidth")]
    public int CacheWidth { get; private set; }
    
    [BsonElement("cacheHeight")]
    public int CacheHeight { get; private set; }
    
    [BsonElement("quality")]
    public int Quality { get; private set; }
    
    [BsonElement("isGenerated")]
    public bool IsGenerated { get; private set; }
    
    [BsonElement("generatedAt")]
    public DateTime? GeneratedAt { get; private set; }
    
    [BsonElement("lastAccessed")]
    public DateTime? LastAccessed { get; private set; }
    
    [BsonElement("accessCount")]
    public int AccessCount { get; private set; }

    // Private constructor for MongoDB
    private ImageCacheInfoEmbedded() { }

    public ImageCacheInfoEmbedded(string cachePath, long cacheSize, string cacheFormat, 
        int cacheWidth, int cacheHeight, int quality)
    {
        CachePath = cachePath ?? throw new ArgumentNullException(nameof(cachePath));
        CacheSize = cacheSize;
        CacheFormat = cacheFormat ?? throw new ArgumentNullException(nameof(cacheFormat));
        CacheWidth = cacheWidth;
        CacheHeight = cacheHeight;
        Quality = quality;
        IsGenerated = true;
        GeneratedAt = DateTime.UtcNow;
        AccessCount = 0;
    }

    public void UpdateAccess()
    {
        LastAccessed = DateTime.UtcNow;
        AccessCount++;
    }

    public void UpdateCacheInfo(string cachePath, long cacheSize, int cacheWidth, int cacheHeight)
    {
        CachePath = cachePath;
        CacheSize = cacheSize;
        CacheWidth = cacheWidth;
        CacheHeight = cacheHeight;
        GeneratedAt = DateTime.UtcNow;
    }
}