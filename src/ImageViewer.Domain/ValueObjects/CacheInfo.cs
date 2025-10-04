using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Cache info value object for media item caching
/// </summary>
public class CacheInfo
{
    [BsonElement("isCached")]
    public bool IsCached { get; private set; }
    
    [BsonElement("cachePath")]
    public string CachePath { get; private set; }
    
    [BsonElement("cacheSize")]
    public long CacheSize { get; private set; }
    
    [BsonElement("cacheFormat")]
    public string CacheFormat { get; private set; }
    
    [BsonElement("cacheQuality")]
    public int CacheQuality { get; private set; }
    
    [BsonElement("cacheWidth")]
    public int CacheWidth { get; private set; }
    
    [BsonElement("cacheHeight")]
    public int CacheHeight { get; private set; }
    
    [BsonElement("lastCached")]
    public DateTime? LastCached { get; private set; }
    
    [BsonElement("cacheVersion")]
    public int CacheVersion { get; private set; }
    
    [BsonElement("compressionLevel")]
    public int CompressionLevel { get; private set; }

    public CacheInfo()
    {
        IsCached = false;
        CachePath = string.Empty;
        CacheSize = 0;
        CacheFormat = "jpg";
        CacheQuality = 85;
        CacheWidth = 0;
        CacheHeight = 0;
        CacheVersion = 1;
        CompressionLevel = 6;
    }

    public void SetCached(string cachePath, long cacheSize, string cacheFormat, int cacheQuality, 
        int cacheWidth, int cacheHeight, int compressionLevel = 6)
    {
        if (string.IsNullOrWhiteSpace(cachePath))
            throw new ArgumentException("Cache path cannot be null or empty", nameof(cachePath));
        
        if (cacheSize < 0)
            throw new ArgumentException("Cache size cannot be negative", nameof(cacheSize));
        
        if (string.IsNullOrWhiteSpace(cacheFormat))
            throw new ArgumentException("Cache format cannot be null or empty", nameof(cacheFormat));
        
        if (cacheQuality < 1 || cacheQuality > 100)
            throw new ArgumentException("Cache quality must be between 1 and 100", nameof(cacheQuality));
        
        if (cacheWidth <= 0)
            throw new ArgumentException("Cache width must be greater than 0", nameof(cacheWidth));
        
        if (cacheHeight <= 0)
            throw new ArgumentException("Cache height must be greater than 0", nameof(cacheHeight));
        
        if (compressionLevel < 1 || compressionLevel > 9)
            throw new ArgumentException("Compression level must be between 1 and 9", nameof(compressionLevel));
        
        IsCached = true;
        CachePath = cachePath;
        CacheSize = cacheSize;
        CacheFormat = cacheFormat;
        CacheQuality = cacheQuality;
        CacheWidth = cacheWidth;
        CacheHeight = cacheHeight;
        CompressionLevel = compressionLevel;
        LastCached = DateTime.UtcNow;
        CacheVersion++;
    }

    public void ClearCache()
    {
        IsCached = false;
        CachePath = string.Empty;
        CacheSize = 0;
        CacheWidth = 0;
        CacheHeight = 0;
        LastCached = null;
        CacheVersion++;
    }

    public void UpdateCachePath(string cachePath)
    {
        if (string.IsNullOrWhiteSpace(cachePath))
            throw new ArgumentException("Cache path cannot be null or empty", nameof(cachePath));
        
        CachePath = cachePath;
    }

    public void UpdateCacheSize(long cacheSize)
    {
        if (cacheSize < 0)
            throw new ArgumentException("Cache size cannot be negative", nameof(cacheSize));
        
        CacheSize = cacheSize;
    }

    public void UpdateCacheFormat(string cacheFormat)
    {
        if (string.IsNullOrWhiteSpace(cacheFormat))
            throw new ArgumentException("Cache format cannot be null or empty", nameof(cacheFormat));
        
        CacheFormat = cacheFormat;
    }

    public void UpdateCacheQuality(int cacheQuality)
    {
        if (cacheQuality < 1 || cacheQuality > 100)
            throw new ArgumentException("Cache quality must be between 1 and 100", nameof(cacheQuality));
        
        CacheQuality = cacheQuality;
    }

    public void UpdateCacheDimensions(int cacheWidth, int cacheHeight)
    {
        if (cacheWidth <= 0)
            throw new ArgumentException("Cache width must be greater than 0", nameof(cacheWidth));
        
        if (cacheHeight <= 0)
            throw new ArgumentException("Cache height must be greater than 0", nameof(cacheHeight));
        
        CacheWidth = cacheWidth;
        CacheHeight = cacheHeight;
    }

    public void UpdateCompressionLevel(int compressionLevel)
    {
        if (compressionLevel < 1 || compressionLevel > 9)
            throw new ArgumentException("Compression level must be between 1 and 9", nameof(compressionLevel));
        
        CompressionLevel = compressionLevel;
    }

    public void UpdateLastCached()
    {
        LastCached = DateTime.UtcNow;
        CacheVersion++;
    }
}
