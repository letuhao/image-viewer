using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Download quality option entity - represents different quality options for downloads
/// </summary>
public class DownloadQualityOption : BaseEntity
{
    [BsonElement("name")]
    public string Name { get; private set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; private set; }

    [BsonElement("quality")]
    public string Quality { get; private set; } = string.Empty; // HD, SD, 4K, 8K, etc.

    [BsonElement("resolution")]
    public string? Resolution { get; private set; } // 1920x1080, 3840x2160, etc.

    [BsonElement("bitrate")]
    public int? Bitrate { get; private set; } // kbps

    [BsonElement("format")]
    public string Format { get; private set; } = string.Empty; // MP4, AVI, MKV, etc.

    [BsonElement("codec")]
    public string? Codec { get; private set; } // H.264, H.265, VP9, etc.

    [BsonElement("fileSizeEstimate")]
    public long? FileSizeEstimate { get; private set; } // bytes

    [BsonElement("downloadSpeedEstimate")]
    public long? DownloadSpeedEstimate { get; private set; } // bytes per second

    [BsonElement("isDefault")]
    public bool IsDefault { get; private set; } = false;

    [BsonElement("isRecommended")]
    public bool IsRecommended { get; private set; } = false;

    [BsonElement("isPremium")]
    public bool IsPremium { get; private set; } = false;

    [BsonElement("priority")]
    public int Priority { get; private set; } = 0;

    [BsonElement("isActive")]
    public bool IsActive { get; private set; } = true;

    [BsonElement("minBandwidth")]
    public long? MinBandwidth { get; private set; } // bytes per second

    [BsonElement("maxBandwidth")]
    public long? MaxBandwidth { get; private set; } // bytes per second

    [BsonElement("compatibility")]
    public List<string> Compatibility { get; private set; } = new(); // Device types, browsers, etc.

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("createdBy")]
    public ObjectId? CreatedBy { get; private set; }

    [BsonElement("collectionId")]
    public ObjectId? CollectionId { get; private set; }

    [BsonElement("downloadCount")]
    public int DownloadCount { get; private set; } = 0;

    [BsonElement("lastDownloadedAt")]
    public DateTime? LastDownloadedAt { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public User? Creator { get; private set; }

    [BsonIgnore]
    public Collection? Collection { get; private set; }

    // Private constructor for EF Core
    private DownloadQualityOption() { }

    public static DownloadQualityOption Create(string name, string quality, string format, ObjectId? createdBy = null, ObjectId? collectionId = null, string? description = null, string? resolution = null, int? bitrate = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(quality))
            throw new ArgumentException("Quality cannot be empty", nameof(quality));

        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Format cannot be empty", nameof(format));

        return new DownloadQualityOption
        {
            Name = name,
            Quality = quality,
            Format = format,
            Description = description,
            Resolution = resolution,
            Bitrate = bitrate,
            CreatedBy = createdBy,
            CollectionId = collectionId,
            IsDefault = false,
            IsRecommended = false,
            IsPremium = false,
            Priority = 0,
            IsActive = true,
            DownloadCount = 0,
            Compatibility = new List<string>(),
            Metadata = new Dictionary<string, object>()
        };
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name;
        UpdateTimestamp();
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        UpdateTimestamp();
    }

    public void UpdateQuality(string quality)
    {
        if (string.IsNullOrWhiteSpace(quality))
            throw new ArgumentException("Quality cannot be empty", nameof(quality));

        Quality = quality;
        UpdateTimestamp();
    }

    public void UpdateResolution(string? resolution)
    {
        Resolution = resolution;
        UpdateTimestamp();
    }

    public void UpdateBitrate(int? bitrate)
    {
        Bitrate = bitrate;
        UpdateTimestamp();
    }

    public void UpdateFormat(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Format cannot be empty", nameof(format));

        Format = format;
        UpdateTimestamp();
    }

    public void UpdateCodec(string? codec)
    {
        Codec = codec;
        UpdateTimestamp();
    }

    public void SetFileSizeEstimate(long? fileSizeEstimate)
    {
        FileSizeEstimate = fileSizeEstimate;
        UpdateTimestamp();
    }

    public void SetDownloadSpeedEstimate(long? downloadSpeedEstimate)
    {
        DownloadSpeedEstimate = downloadSpeedEstimate;
        UpdateTimestamp();
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
        UpdateTimestamp();
    }

    public void SetRecommended(bool isRecommended)
    {
        IsRecommended = isRecommended;
        UpdateTimestamp();
    }

    public void SetPremium(bool isPremium)
    {
        IsPremium = isPremium;
        UpdateTimestamp();
    }

    public void SetPriority(int priority)
    {
        Priority = priority;
        UpdateTimestamp();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdateTimestamp();
    }

    public void SetBandwidthLimits(long? minBandwidth, long? maxBandwidth)
    {
        MinBandwidth = minBandwidth;
        MaxBandwidth = maxBandwidth;
        UpdateTimestamp();
    }

    public void AddCompatibility(string device)
    {
        if (string.IsNullOrWhiteSpace(device))
            throw new ArgumentException("Device cannot be empty", nameof(device));

        if (!Compatibility.Contains(device))
        {
            Compatibility.Add(device);
            UpdateTimestamp();
        }
    }

    public void RemoveCompatibility(string device)
    {
        Compatibility.Remove(device);
        UpdateTimestamp();
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }

    public void RecordDownload()
    {
        DownloadCount++;
        LastDownloadedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public bool IsCompatibleWithBandwidth(long bandwidth)
    {
        if (MinBandwidth.HasValue && bandwidth < MinBandwidth.Value)
            return false;

        if (MaxBandwidth.HasValue && bandwidth > MaxBandwidth.Value)
            return false;

        return true;
    }

    public bool IsCompatibleWithDevice(string device)
    {
        return Compatibility.Contains(device);
    }

    public bool IsAvailable()
    {
        return IsActive && !IsDeleted;
    }

    public double GetEstimatedDownloadTime(long bandwidth)
    {
        if (!FileSizeEstimate.HasValue || bandwidth <= 0)
            return 0;

        return (double)FileSizeEstimate.Value / bandwidth;
    }
}
