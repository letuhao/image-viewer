using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Collection settings value object
/// </summary>
public class CollectionSettings
{
    [JsonPropertyName("totalImages")]
    public int TotalImages { get; private set; }
    
    [JsonPropertyName("totalSizeBytes")]
    public long TotalSizeBytes { get; private set; }
    
    [JsonPropertyName("thumbnailWidth")]
    public int ThumbnailWidth { get; private set; }
    
    [JsonPropertyName("thumbnailHeight")]
    public int ThumbnailHeight { get; private set; }
    
    [JsonPropertyName("cacheWidth")]
    public int CacheWidth { get; private set; }
    
    [JsonPropertyName("cacheHeight")]
    public int CacheHeight { get; private set; }
    
    [JsonPropertyName("autoGenerateThumbnails")]
    public bool AutoGenerateThumbnails { get; private set; }
    
    [JsonPropertyName("autoGenerateCache")]
    public bool AutoGenerateCache { get; private set; }
    
    [JsonPropertyName("cacheExpiration")]
    public TimeSpan CacheExpiration { get; private set; }
    
    [JsonPropertyName("additionalSettings")]
    public string AdditionalSettingsJson { get; private set; }

    public CollectionSettings()
    {
        TotalImages = 0;
        TotalSizeBytes = 0;
        ThumbnailWidth = 300;
        ThumbnailHeight = 300;
        CacheWidth = 1920;
        CacheHeight = 1080;
        AutoGenerateThumbnails = true;
        AutoGenerateCache = true;
        CacheExpiration = TimeSpan.FromDays(30);
        AdditionalSettingsJson = "{}";
    }

    public CollectionSettings(
        int totalImages,
        long totalSizeBytes,
        int thumbnailWidth,
        int thumbnailHeight,
        int cacheWidth,
        int cacheHeight,
        bool autoGenerateThumbnails,
        bool autoGenerateCache,
        TimeSpan cacheExpiration,
        Dictionary<string, object> additionalSettings)
    {
        TotalImages = totalImages;
        TotalSizeBytes = totalSizeBytes;
        ThumbnailWidth = thumbnailWidth;
        ThumbnailHeight = thumbnailHeight;
        CacheWidth = cacheWidth;
        CacheHeight = cacheHeight;
        AutoGenerateThumbnails = autoGenerateThumbnails;
        AutoGenerateCache = autoGenerateCache;
        CacheExpiration = cacheExpiration;
        AdditionalSettingsJson = JsonSerializer.Serialize(additionalSettings);
    }

    public void UpdateTotalImages(int totalImages)
    {
        if (totalImages < 0)
            throw new ArgumentException("Total images cannot be negative", nameof(totalImages));

        TotalImages = totalImages;
    }

    public void UpdateTotalSize(long totalSizeBytes)
    {
        if (totalSizeBytes < 0)
            throw new ArgumentException("Total size cannot be negative", nameof(totalSizeBytes));

        TotalSizeBytes = totalSizeBytes;
    }

    public void UpdateThumbnailSize(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentException("Thumbnail width must be greater than 0", nameof(width));
        if (height <= 0)
            throw new ArgumentException("Thumbnail height must be greater than 0", nameof(height));

        ThumbnailWidth = width;
        ThumbnailHeight = height;
    }

    public void UpdateCacheSize(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentException("Cache width must be greater than 0", nameof(width));
        if (height <= 0)
            throw new ArgumentException("Cache height must be greater than 0", nameof(height));

        CacheWidth = width;
        CacheHeight = height;
    }

    public void SetAutoGenerateThumbnails(bool enabled)
    {
        AutoGenerateThumbnails = enabled;
    }

    public void SetAutoGenerateCache(bool enabled)
    {
        AutoGenerateCache = enabled;
    }

    public void UpdateCacheExpiration(TimeSpan expiration)
    {
        if (expiration <= TimeSpan.Zero)
            throw new ArgumentException("Cache expiration must be greater than zero", nameof(expiration));

        CacheExpiration = expiration;
    }

    public void SetAdditionalSetting(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(AdditionalSettingsJson) ?? new Dictionary<string, object>();
        settings[key] = value;
        AdditionalSettingsJson = JsonSerializer.Serialize(settings);
    }

    public T? GetAdditionalSetting<T>(string key)
    {
        var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(AdditionalSettingsJson);
        if (settings != null && settings.TryGetValue(key, out var value))
        {
            if (value is JsonElement jsonElement)
            {
                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
            }
            return (T)value;
        }
        return default;
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    public static CollectionSettings FromJson(string json)
    {
        return JsonSerializer.Deserialize<CollectionSettings>(json) 
            ?? throw new ArgumentException("Invalid JSON", nameof(json));
    }
}
