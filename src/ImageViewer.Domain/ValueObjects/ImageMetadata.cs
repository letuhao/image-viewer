using System.Text.Json;

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Image metadata value object
/// </summary>
public class ImageMetadata
{
    public int Quality { get; private set; }
    public string? ColorSpace { get; private set; }
    public string? Compression { get; private set; }
    public DateTime? CreatedDate { get; private set; }
    public DateTime? ModifiedDate { get; private set; }
    public string? Camera { get; private set; }
    public string? Software { get; private set; }
    public Dictionary<string, object> AdditionalMetadata { get; private set; }

    public ImageMetadata(
        int quality = 95,
        string? colorSpace = null,
        string? compression = null,
        DateTime? createdDate = null,
        DateTime? modifiedDate = null,
        string? camera = null,
        string? software = null,
        Dictionary<string, object>? additionalMetadata = null)
    {
        Quality = quality;
        ColorSpace = colorSpace;
        Compression = compression;
        CreatedDate = createdDate;
        ModifiedDate = modifiedDate;
        Camera = camera;
        Software = software;
        AdditionalMetadata = additionalMetadata ?? new Dictionary<string, object>();
    }

    public void UpdateQuality(int quality)
    {
        if (quality < 0 || quality > 100)
            throw new ArgumentException("Quality must be between 0 and 100", nameof(quality));

        Quality = quality;
    }

    public void UpdateColorSpace(string? colorSpace)
    {
        ColorSpace = colorSpace;
    }

    public void UpdateCompression(string? compression)
    {
        Compression = compression;
    }

    public void UpdateCreatedDate(DateTime? createdDate)
    {
        CreatedDate = createdDate;
    }

    public void UpdateModifiedDate(DateTime? modifiedDate)
    {
        ModifiedDate = modifiedDate;
    }

    public void UpdateCamera(string? camera)
    {
        Camera = camera;
    }

    public void UpdateSoftware(string? software)
    {
        Software = software;
    }

    public void SetAdditionalMetadata(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        AdditionalMetadata[key] = value;
    }

    public T? GetAdditionalMetadata<T>(string key)
    {
        if (AdditionalMetadata.TryGetValue(key, out var value))
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

    public static ImageMetadata FromJson(string json)
    {
        return JsonSerializer.Deserialize<ImageMetadata>(json) 
            ?? throw new ArgumentException("Invalid JSON", nameof(json));
    }
}
