using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Cache binding value object for collection cache settings
/// </summary>
public class CacheBinding
{
    [BsonElement("cachePath")]
    public string CachePath { get; private set; }
    
    [BsonElement("cacheFormat")]
    public string CacheFormat { get; private set; }
    
    [BsonElement("cacheQuality")]
    public int CacheQuality { get; private set; }
    
    [BsonElement("cacheWidth")]
    public int CacheWidth { get; private set; }
    
    [BsonElement("cacheHeight")]
    public int CacheHeight { get; private set; }
    
    [BsonElement("isEnabled")]
    public bool IsEnabled { get; private set; }
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; private set; }
    
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; private set; }
    
    [BsonElement("cacheFolder")]
    public string CacheFolder { get; private set; } = string.Empty;

    public CacheBinding(string cachePath, string cacheFormat, int cacheQuality, int cacheWidth, int cacheHeight, string cacheFolder = "")
    {
        CachePath = cachePath ?? throw new ArgumentNullException(nameof(cachePath));
        CacheFormat = cacheFormat ?? throw new ArgumentNullException(nameof(cacheFormat));
        CacheQuality = cacheQuality;
        CacheWidth = cacheWidth;
        CacheHeight = cacheHeight;
        CacheFolder = cacheFolder;
        IsEnabled = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSettings(string cachePath, string cacheFormat, int cacheQuality, int cacheWidth, int cacheHeight)
    {
        CachePath = cachePath ?? throw new ArgumentNullException(nameof(cachePath));
        CacheFormat = cacheFormat ?? throw new ArgumentNullException(nameof(cacheFormat));
        CacheQuality = cacheQuality;
        CacheWidth = cacheWidth;
        CacheHeight = cacheHeight;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
