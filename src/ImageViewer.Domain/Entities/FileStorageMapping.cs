using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// File storage mapping entity - represents mapping between files and their storage locations
/// </summary>
public class FileStorageMapping : BaseEntity
{
    [BsonElement("fileId")]
    public ObjectId FileId { get; private set; }

    [BsonElement("storageLocationId")]
    public ObjectId StorageLocationId { get; private set; }

    [BsonElement("filePath")]
    public string FilePath { get; private set; } = string.Empty;

    [BsonElement("fileName")]
    public string FileName { get; private set; } = string.Empty;

    [BsonElement("fileSize")]
    public long FileSize { get; private set; }

    [BsonElement("fileHash")]
    public string? FileHash { get; private set; }

    [BsonElement("mimeType")]
    public string? MimeType { get; private set; }

    [BsonElement("isPrimary")]
    public bool IsPrimary { get; private set; } = false;

    [BsonElement("isReplica")]
    public bool IsReplica { get; private set; } = false;

    [BsonElement("replicaCount")]
    public int ReplicaCount { get; private set; } = 0;

    [BsonElement("lastAccessed")]
    public DateTime? LastAccessed { get; private set; }

    [BsonElement("accessCount")]
    public int AccessCount { get; private set; } = 0;

    [BsonElement("isCompressed")]
    public bool IsCompressed { get; private set; } = false;

    [BsonElement("compressionRatio")]
    public double? CompressionRatio { get; private set; }

    [BsonElement("encryptionKey")]
    public string? EncryptionKey { get; private set; }

    [BsonElement("isEncrypted")]
    public bool IsEncrypted { get; private set; } = false;

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("tags")]
    public List<string> Tags { get; private set; } = new();

    [BsonElement("checksum")]
    public string? Checksum { get; private set; }

    [BsonElement("version")]
    public int Version { get; private set; } = 1;

    [BsonElement("parentMappingId")]
    public ObjectId? ParentMappingId { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public StorageLocation StorageLocation { get; private set; } = null!;

    // Private constructor for EF Core
    private FileStorageMapping() { }

    public static FileStorageMapping Create(ObjectId fileId, ObjectId storageLocationId, string filePath, string fileName, long fileSize, string? fileHash = null, string? mimeType = null, bool isPrimary = false, bool isReplica = false)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty", nameof(fileName));

        if (fileSize < 0)
            throw new ArgumentException("File size cannot be negative", nameof(fileSize));

        return new FileStorageMapping
        {
            FileId = fileId,
            StorageLocationId = storageLocationId,
            FilePath = filePath,
            FileName = fileName,
            FileSize = fileSize,
            FileHash = fileHash,
            MimeType = mimeType,
            IsPrimary = isPrimary,
            IsReplica = isReplica,
            ReplicaCount = isReplica ? 1 : 0,
            AccessCount = 0,
            IsCompressed = false,
            IsEncrypted = false,
            Version = 1,
            Metadata = new Dictionary<string, object>(),
            Tags = new List<string>()
        };
    }

    public void UpdateFilePath(string newPath, string newFileName)
    {
        if (string.IsNullOrWhiteSpace(newPath))
            throw new ArgumentException("File path cannot be empty", nameof(newPath));

        if (string.IsNullOrWhiteSpace(newFileName))
            throw new ArgumentException("File name cannot be empty", nameof(newFileName));

        FilePath = newPath;
        FileName = newFileName;
        UpdateTimestamp();
    }

    public void UpdateFileSize(long newSize)
    {
        if (newSize < 0)
            throw new ArgumentException("File size cannot be negative", nameof(newSize));

        FileSize = newSize;
        UpdateTimestamp();
    }

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
        UpdateTimestamp();
    }

    public void SetReplica(bool isReplica, int replicaCount = 1)
    {
        IsReplica = isReplica;
        ReplicaCount = isReplica ? replicaCount : 0;
        UpdateTimestamp();
    }

    public void RecordAccess()
    {
        LastAccessed = DateTime.UtcNow;
        AccessCount++;
        UpdateTimestamp();
    }

    public void SetCompression(bool isCompressed, double? compressionRatio = null)
    {
        IsCompressed = isCompressed;
        CompressionRatio = compressionRatio;
        UpdateTimestamp();
    }

    public void SetEncryption(bool isEncrypted, string? encryptionKey = null)
    {
        IsEncrypted = isEncrypted;
        EncryptionKey = encryptionKey;
        UpdateTimestamp();
    }

    public void UpdateChecksum(string checksum)
    {
        Checksum = checksum;
        UpdateTimestamp();
    }

    public void IncrementVersion()
    {
        Version++;
        UpdateTimestamp();
    }

    public void SetParentMapping(ObjectId? parentMappingId)
    {
        ParentMappingId = parentMappingId;
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

    public void RemoveMetadata(string key)
    {
        Metadata.Remove(key);
        UpdateTimestamp();
    }

    public void UpdateMimeType(string mimeType)
    {
        MimeType = mimeType;
        UpdateTimestamp();
    }

    public bool IsAccessible()
    {
        return !IsDeleted && StorageLocation.IsActive;
    }

    public string GetFullPath()
    {
        return Path.Combine(StorageLocation.Path, FilePath);
    }

    public bool VerifyChecksum(string calculatedChecksum)
    {
        return !string.IsNullOrEmpty(Checksum) && Checksum.Equals(calculatedChecksum, StringComparison.OrdinalIgnoreCase);
    }
}
