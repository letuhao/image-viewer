using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// File version entity - represents different versions of a file
/// </summary>
public class FileVersion : BaseEntity
{
    [BsonElement("originalFileId")]
    public ObjectId OriginalFileId { get; private set; }

    [BsonElement("versionNumber")]
    public int VersionNumber { get; private set; }

    [BsonElement("filePath")]
    public string FilePath { get; private set; } = string.Empty;

    [BsonElement("fileName")]
    public string FileName { get; private set; } = string.Empty;

    [BsonElement("fileSize")]
    public long FileSize { get; private set; }

    [BsonElement("fileHash")]
    public string FileHash { get; private set; } = string.Empty;

    [BsonElement("mimeType")]
    public string MimeType { get; private set; } = string.Empty;

    [BsonElement("versionType")]
    public string VersionType { get; private set; } = string.Empty; // "original", "thumbnail", "preview", "backup", "processed"

    [BsonElement("createdBy")]
    public new ObjectId? CreatedBy { get; private set; }

    [BsonElement("creationReason")]
    public string CreationReason { get; private set; } = string.Empty; // "upload", "backup", "processing", "manual"

    [BsonElement("isActive")]
    public bool IsActive { get; private set; } = true;

    [BsonElement("isBackup")]
    public bool IsBackup { get; private set; } = false;

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("storageLocation")]
    public string StorageLocation { get; private set; } = string.Empty;

    [BsonElement("compressionLevel")]
    public int CompressionLevel { get; private set; } = 0;

    [BsonElement("quality")]
    public int Quality { get; private set; } = 100;

    [BsonElement("dimensions")]
    public FileDimensions? Dimensions { get; private set; }

    [BsonElement("parentVersionId")]
    public ObjectId? ParentVersionId { get; private set; }

    [BsonElement("childVersionIds")]
    public List<ObjectId> ChildVersionIds { get; private set; } = new();

    [BsonElement("retentionPolicy")]
    public RetentionPolicy? RetentionPolicy { get; private set; }

    [BsonElement("accessCount")]
    public long AccessCount { get; private set; } = 0;

    [BsonElement("lastAccessedAt")]
    public DateTime? LastAccessedAt { get; private set; }

    [BsonElement("downloadCount")]
    public long DownloadCount { get; private set; } = 0;

    [BsonElement("lastDownloadedAt")]
    public DateTime? LastDownloadedAt { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public MediaItem OriginalFile { get; private set; } = null!;

    [BsonIgnore]
    public User? Creator { get; private set; }

    [BsonIgnore]
    public FileVersion? ParentVersion { get; private set; }

    [BsonIgnore]
    public List<FileVersion> ChildVersions { get; private set; } = new();

    // Private constructor for EF Core
    private FileVersion() { }

    public FileVersion(
        ObjectId originalFileId,
        int versionNumber,
        string filePath,
        string fileName,
        long fileSize,
        string fileHash,
        string mimeType,
        string versionType,
        ObjectId? createdBy = null,
        string creationReason = "upload")
    {
        OriginalFileId = originalFileId;
        VersionNumber = versionNumber;
        FilePath = filePath;
        FileName = fileName;
        FileSize = fileSize;
        FileHash = fileHash;
        MimeType = mimeType;
        VersionType = versionType;
        CreatedBy = createdBy;
        CreationReason = creationReason;
        IsActive = true;
        IsBackup = versionType == "backup";
        Metadata = new Dictionary<string, object>();
        ChildVersionIds = new List<ObjectId>();
        AccessCount = 0;
        DownloadCount = 0;
    }

    public void UpdateFileInfo(string filePath, string fileName, long fileSize, string fileHash, string mimeType)
    {
        FilePath = filePath;
        FileName = fileName;
        FileSize = fileSize;
        FileHash = fileHash;
        MimeType = mimeType;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDimensions(int width, int height)
    {
        Dimensions = new FileDimensions { Width = width, Height = height };
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetQuality(int quality)
    {
        Quality = Math.Max(0, Math.Min(100, quality));
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCompressionLevel(int level)
    {
        CompressionLevel = Math.Max(0, Math.Min(9, level));
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStorageLocation(string location)
    {
        StorageLocation = location;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveMetadata(string key)
    {
        if (Metadata.ContainsKey(key))
        {
            Metadata.Remove(key);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void SetParentVersion(ObjectId? parentVersionId)
    {
        ParentVersionId = parentVersionId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddChildVersion(ObjectId childVersionId)
    {
        if (!ChildVersionIds.Contains(childVersionId))
        {
            ChildVersionIds.Add(childVersionId);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveChildVersion(ObjectId childVersionId)
    {
        if (ChildVersionIds.Contains(childVersionId))
        {
            ChildVersionIds.Remove(childVersionId);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void SetRetentionPolicy(RetentionPolicy policy)
    {
        RetentionPolicy = policy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordAccess()
    {
        AccessCount++;
        LastAccessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordDownload()
    {
        DownloadCount++;
        LastDownloadedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsBackup()
    {
        IsBackup = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool ShouldBeRetained()
    {
        if (RetentionPolicy == null) return true;

        var now = DateTime.UtcNow;
        
        if (RetentionPolicy.MaxAge.HasValue && CreatedAt.Add(RetentionPolicy.MaxAge.Value) < now)
            return false;

        if (RetentionPolicy.MaxAccessCount.HasValue && AccessCount > RetentionPolicy.MaxAccessCount.Value)
            return false;

        if (RetentionPolicy.LastAccessThreshold.HasValue && 
            LastAccessedAt.HasValue && 
            LastAccessedAt.Value.Add(RetentionPolicy.LastAccessThreshold.Value) < now)
            return false;

        return true;
    }

    public bool IsExpired()
    {
        return !ShouldBeRetained();
    }
}

/// <summary>
/// File dimensions entity
/// </summary>
public class FileDimensions
{
    [BsonElement("width")]
    public int Width { get; set; }

    [BsonElement("height")]
    public int Height { get; set; }

    [BsonElement("aspectRatio")]
    public double AspectRatio => Height > 0 ? (double)Width / Height : 0;

    [BsonElement("orientation")]
    public string Orientation
    {
        get
        {
            if (Width > Height) return "landscape";
            if (Height > Width) return "portrait";
            return "square";
        }
    }
}

/// <summary>
/// Retention policy entity
/// </summary>
public class RetentionPolicy
{
    [BsonElement("maxAge")]
    public TimeSpan? MaxAge { get; set; }

    [BsonElement("maxAccessCount")]
    public long? MaxAccessCount { get; set; }

    [BsonElement("lastAccessThreshold")]
    public TimeSpan? LastAccessThreshold { get; set; }

    [BsonElement("autoDelete")]
    public bool AutoDelete { get; set; } = false;

    [BsonElement("archiveBeforeDelete")]
    public bool ArchiveBeforeDelete { get; set; } = true;

    [BsonElement("notificationBeforeDelete")]
    public bool NotificationBeforeDelete { get; set; } = true;

    [BsonElement("notificationDaysBefore")]
    public int NotificationDaysBefore { get; set; } = 7;
}
