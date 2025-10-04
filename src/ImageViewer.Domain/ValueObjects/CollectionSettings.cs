using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Collection settings value object
/// </summary>
public class CollectionSettings
{
    [BsonElement("enabled")]
    public bool Enabled { get; private set; }
    
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

    public CollectionSettings()
    {
        Enabled = true;
        AutoScan = true;
        GenerateThumbnails = true;
        GenerateCache = true;
        EnableWatching = false;
        ScanInterval = 3600; // 1 hour
        MaxFileSize = 100 * 1024 * 1024; // 100MB
        AllowedFormats = new List<string> { "jpg", "jpeg", "png", "gif", "bmp", "webp", "mp4", "avi", "mov" };
        ExcludedPaths = new List<string>();
    }

    public void Enable()
    {
        Enabled = true;
    }

    public void Disable()
    {
        Enabled = false;
    }

    public void UpdateScanSettings(bool autoScan, bool generateThumbnails, bool generateCache)
    {
        AutoScan = autoScan;
        GenerateThumbnails = generateThumbnails;
        GenerateCache = generateCache;
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
}