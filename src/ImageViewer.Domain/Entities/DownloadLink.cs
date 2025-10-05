using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Download link entity - represents download links for files and collections
/// </summary>
public class DownloadLink : BaseEntity
{
    [BsonElement("url")]
    public string Url { get; private set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; private set; } = "Direct"; // Direct, Torrent, Magnet, FTP, HTTP, HTTPS

    [BsonElement("fileName")]
    public string FileName { get; private set; } = string.Empty;

    [BsonElement("fileSize")]
    public long? FileSize { get; private set; }

    [BsonElement("description")]
    public string? Description { get; private set; }

    [BsonElement("status")]
    public string Status { get; private set; } = "Active"; // Active, Inactive, Expired, Invalid, Blocked

    [BsonElement("isPublic")]
    public bool IsPublic { get; private set; } = true;

    [BsonElement("expiresAt")]
    public DateTime? ExpiresAt { get; private set; }

    [BsonElement("maxDownloads")]
    public int? MaxDownloads { get; private set; }

    [BsonElement("downloadCount")]
    public int DownloadCount { get; private set; } = 0;

    [BsonElement("lastDownloadedAt")]
    public DateTime? LastDownloadedAt { get; private set; }

    [BsonElement("createdBy")]
    public ObjectId CreatedBy { get; private set; }

    [BsonElement("collectionId")]
    public ObjectId? CollectionId { get; private set; }

    [BsonElement("mediaItemId")]
    public ObjectId? MediaItemId { get; private set; }

    [BsonElement("password")]
    public string? Password { get; private set; }

    [BsonElement("isPasswordProtected")]
    public bool IsPasswordProtected { get; private set; } = false;

    [BsonElement("quality")]
    public string? Quality { get; private set; } // HD, SD, 4K, etc.

    [BsonElement("format")]
    public string? Format { get; private set; } // MP4, AVI, MKV, etc.

    [BsonElement("checksum")]
    public string? Checksum { get; private set; }

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("tags")]
    public List<string> Tags { get; private set; } = new();

    [BsonElement("lastChecked")]
    public DateTime? LastChecked { get; private set; }

    [BsonElement("healthStatus")]
    public string HealthStatus { get; private set; } = "Unknown"; // Healthy, Warning, Error, Unknown

    // Navigation properties
    [BsonIgnore]
    public User Creator { get; private set; } = null!;

    [BsonIgnore]
    public Collection? Collection { get; private set; }

    [BsonIgnore]
    public MediaItem? MediaItem { get; private set; }

    // Private constructor for EF Core
    private DownloadLink() { }

    public static DownloadLink Create(string url, string type, string fileName, ObjectId createdBy, ObjectId? collectionId = null, ObjectId? mediaItemId = null, string? description = null, DateTime? expiresAt = null, int? maxDownloads = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty", nameof(url));

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type cannot be empty", nameof(type));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty", nameof(fileName));

        return new DownloadLink
        {
            Url = url,
            Type = type,
            FileName = fileName,
            Description = description,
            CreatedBy = createdBy,
            CollectionId = collectionId,
            MediaItemId = mediaItemId,
            Status = "Active",
            IsPublic = true,
            ExpiresAt = expiresAt,
            MaxDownloads = maxDownloads,
            DownloadCount = 0,
            IsPasswordProtected = false,
            HealthStatus = "Unknown",
            Metadata = new Dictionary<string, object>(),
            Tags = new List<string>()
        };
    }

    public void UpdateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty", nameof(url));

        Url = url;
        UpdateTimestamp();
    }

    public void UpdateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty", nameof(fileName));

        FileName = fileName;
        UpdateTimestamp();
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        UpdateTimestamp();
    }

    public void SetFileSize(long? fileSize)
    {
        FileSize = fileSize;
        UpdateTimestamp();
    }

    public void SetStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status cannot be empty", nameof(status));

        Status = status;
        UpdateTimestamp();
    }

    public void SetPublic(bool isPublic)
    {
        IsPublic = isPublic;
        UpdateTimestamp();
    }

    public void SetExpiration(DateTime? expiresAt)
    {
        ExpiresAt = expiresAt;
        UpdateTimestamp();
    }

    public void SetMaxDownloads(int? maxDownloads)
    {
        MaxDownloads = maxDownloads;
        UpdateTimestamp();
    }

    public void SetPassword(string? password)
    {
        Password = password;
        IsPasswordProtected = !string.IsNullOrEmpty(password);
        UpdateTimestamp();
    }

    public void SetQuality(string? quality)
    {
        Quality = quality;
        UpdateTimestamp();
    }

    public void SetFormat(string? format)
    {
        Format = format;
        UpdateTimestamp();
    }

    public void SetChecksum(string? checksum)
    {
        Checksum = checksum;
        UpdateTimestamp();
    }

    public void RecordDownload()
    {
        DownloadCount++;
        LastDownloadedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void SetHealthStatus(string healthStatus)
    {
        if (string.IsNullOrWhiteSpace(healthStatus))
            throw new ArgumentException("Health status cannot be empty", nameof(healthStatus));

        HealthStatus = healthStatus;
        LastChecked = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty", nameof(tag));

        if (!Tags.Contains(tag))
        {
            Tags.Add(tag);
            UpdateTimestamp();
        }
    }

    public void RemoveTag(string tag)
    {
        Tags.Remove(tag);
        UpdateTimestamp();
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }

    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    }

    public bool HasReachedMaxDownloads()
    {
        return MaxDownloads.HasValue && DownloadCount >= MaxDownloads.Value;
    }

    public bool IsDownloadable()
    {
        return Status == "Active" && !IsExpired() && !HasReachedMaxDownloads();
    }

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Url) && !IsExpired() && HealthStatus != "Error";
    }
}
