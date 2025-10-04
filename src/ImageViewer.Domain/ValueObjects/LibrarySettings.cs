using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Library settings value object
/// </summary>
public class LibrarySettings
{
    [BsonElement("autoScan")]
    public bool AutoScan { get; private set; }
    
    [BsonElement("generateThumbnails")]
    public bool GenerateThumbnails { get; private set; }
    
    [BsonElement("generateCache")]
    public bool GenerateCache { get; private set; }
    
    [BsonElement("enableWatching")]
    public bool EnableWatching { get; private set; }
    
    [BsonElement("scanInterval")]
    public int ScanInterval { get; private set; }
    
    [BsonElement("maxFileSize")]
    public long MaxFileSize { get; private set; }
    
    [BsonElement("allowedFormats")]
    public List<string> AllowedFormats { get; private set; }
    
    [BsonElement("excludedPaths")]
    public List<string> ExcludedPaths { get; private set; }
    
    [BsonElement("thumbnailSettings")]
    public ThumbnailSettings ThumbnailSettings { get; private set; }
    
    [BsonElement("cacheSettings")]
    public CacheSettings CacheSettings { get; private set; }

    public LibrarySettings()
    {
        AutoScan = true;
        GenerateThumbnails = true;
        GenerateCache = true;
        EnableWatching = false;
        ScanInterval = 3600; // 1 hour
        MaxFileSize = 100 * 1024 * 1024; // 100MB
        AllowedFormats = new List<string> { "jpg", "jpeg", "png", "gif", "bmp", "webp", "mp4", "avi", "mov" };
        ExcludedPaths = new List<string>();
        ThumbnailSettings = new ThumbnailSettings();
        CacheSettings = new CacheSettings();
    }

    public void UpdateAutoScan(bool enabled)
    {
        AutoScan = enabled;
    }

    public void UpdateGenerateThumbnails(bool enabled)
    {
        GenerateThumbnails = enabled;
    }

    public void UpdateGenerateCache(bool enabled)
    {
        GenerateCache = enabled;
    }

    public void UpdateEnableWatching(bool enabled)
    {
        EnableWatching = enabled;
    }

    public void UpdateScanInterval(int interval)
    {
        if (interval <= 0)
            throw new ArgumentException("Scan interval must be greater than 0", nameof(interval));
        
        ScanInterval = interval;
    }

    public void UpdateMaxFileSize(long maxSize)
    {
        if (maxSize <= 0)
            throw new ArgumentException("Max file size must be greater than 0", nameof(maxSize));
        
        MaxFileSize = maxSize;
    }

    public void AddAllowedFormat(string format)
    {
        if (!string.IsNullOrWhiteSpace(format) && !AllowedFormats.Contains(format.ToLower()))
        {
            AllowedFormats.Add(format.ToLower());
        }
    }

    public void RemoveAllowedFormat(string format)
    {
        AllowedFormats.Remove(format.ToLower());
    }

    public void AddExcludedPath(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && !ExcludedPaths.Contains(path))
        {
            ExcludedPaths.Add(path);
        }
    }

    public void RemoveExcludedPath(string path)
    {
        ExcludedPaths.Remove(path);
    }

    public void UpdateThumbnailSettings(ThumbnailSettings settings)
    {
        ThumbnailSettings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public void UpdateCacheSettings(CacheSettings settings)
    {
        CacheSettings = settings ?? throw new ArgumentNullException(nameof(settings));
    }
}

/// <summary>
/// Thumbnail settings value object
/// </summary>
public class ThumbnailSettings
{
    [BsonElement("enabled")]
    public bool Enabled { get; private set; }
    
    [BsonElement("width")]
    public int Width { get; private set; }
    
    [BsonElement("height")]
    public int Height { get; private set; }
    
    [BsonElement("quality")]
    public int Quality { get; private set; }
    
    [BsonElement("format")]
    public string Format { get; private set; }

    public ThumbnailSettings()
    {
        Enabled = true;
        Width = 200;
        Height = 200;
        Quality = 85;
        Format = "jpg";
    }

    public void UpdateEnabled(bool enabled)
    {
        Enabled = enabled;
    }

    public void UpdateSize(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentException("Width must be greater than 0", nameof(width));
        
        if (height <= 0)
            throw new ArgumentException("Height must be greater than 0", nameof(height));
        
        Width = width;
        Height = height;
    }

    public void UpdateQuality(int quality)
    {
        if (quality < 1 || quality > 100)
            throw new ArgumentException("Quality must be between 1 and 100", nameof(quality));
        
        Quality = quality;
    }

    public void UpdateFormat(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Format cannot be null or empty", nameof(format));
        
        Format = format;
    }
}

/// <summary>
/// Cache settings value object
/// </summary>
public class CacheSettings
{
    [BsonElement("enabled")]
    public bool Enabled { get; private set; }
    
    [BsonElement("maxSize")]
    public long MaxSize { get; private set; }
    
    [BsonElement("compressionLevel")]
    public int CompressionLevel { get; private set; }
    
    [BsonElement("retentionDays")]
    public int RetentionDays { get; private set; }

    public CacheSettings()
    {
        Enabled = true;
        MaxSize = 1024 * 1024 * 1024; // 1GB
        CompressionLevel = 6;
        RetentionDays = 30;
    }

    public void UpdateEnabled(bool enabled)
    {
        Enabled = enabled;
    }

    public void UpdateMaxSize(long maxSize)
    {
        if (maxSize <= 0)
            throw new ArgumentException("Max size must be greater than 0", nameof(maxSize));
        
        MaxSize = maxSize;
    }

    public void UpdateCompressionLevel(int level)
    {
        if (level < 1 || level > 9)
            throw new ArgumentException("Compression level must be between 1 and 9", nameof(level));
        
        CompressionLevel = level;
    }

    public void UpdateRetentionDays(int days)
    {
        if (days <= 0)
            throw new ArgumentException("Retention days must be greater than 0", nameof(days));
        
        RetentionDays = days;
    }
}
