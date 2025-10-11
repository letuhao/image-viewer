using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Storage location entity - represents different storage locations for files
/// </summary>
public class StorageLocation : BaseEntity
{
    [BsonElement("name")]
    public string Name { get; private set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; private set; }

    [BsonElement("type")]
    public string Type { get; private set; } = "Local"; // Local, Network, Cloud, CDN, Archive

    [BsonElement("path")]
    public string Path { get; private set; } = string.Empty;

    [BsonElement("connectionString")]
    public string? ConnectionString { get; private set; }

    [BsonElement("credentials")]
    public Dictionary<string, object> Credentials { get; private set; } = new();

    [BsonElement("isActive")]
    public bool IsActive { get; private set; } = true;

    [BsonElement("isDefault")]
    public bool IsDefault { get; private set; } = false;

    [BsonElement("capacityBytes")]
    public long? CapacityBytes { get; private set; }

    [BsonElement("usedBytes")]
    public long UsedBytes { get; private set; } = 0;

    [BsonElement("freeBytes")]
    public long? FreeBytes { get; private set; }

    [BsonElement("maxFileSize")]
    public long? MaxFileSize { get; private set; }

    [BsonElement("allowedExtensions")]
    public List<string> AllowedExtensions { get; private set; } = new();

    [BsonElement("blockedExtensions")]
    public List<string> BlockedExtensions { get; private set; } = new();

    [BsonElement("settings")]
    public Dictionary<string, object> Settings { get; private set; } = new();

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("lastChecked")]
    public DateTime? LastChecked { get; private set; }

    [BsonElement("healthStatus")]
    public string HealthStatus { get; private set; } = "Unknown"; // Healthy, Warning, Error, Unknown

    [BsonElement("priority")]
    public int Priority { get; private set; } = 0; // Higher number = higher priority

    [BsonElement("createdBy")]
    public new ObjectId CreatedBy { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public User Creator { get; private set; } = null!;

    // Private constructor for EF Core
    private StorageLocation() { }

    public static StorageLocation Create(string name, string type, string path, ObjectId createdBy, string? description = null, bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type cannot be empty", nameof(type));

        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be empty", nameof(path));

        return new StorageLocation
        {
            Name = name,
            Type = type,
            Path = path,
            Description = description,
            CreatedBy = createdBy,
            IsActive = true,
            IsDefault = isDefault,
            UsedBytes = 0,
            HealthStatus = "Unknown",
            Priority = 0,
            AllowedExtensions = new List<string>(),
            BlockedExtensions = new List<string>(),
            Settings = new Dictionary<string, object>(),
            Metadata = new Dictionary<string, object>(),
            Credentials = new Dictionary<string, object>()
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

    public void UpdatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be empty", nameof(path));

        Path = path;
        UpdateTimestamp();
    }

    public void SetConnectionString(string? connectionString)
    {
        ConnectionString = connectionString;
        UpdateTimestamp();
    }

    public void SetCredentials(Dictionary<string, object> credentials)
    {
        Credentials = credentials ?? new Dictionary<string, object>();
        UpdateTimestamp();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdateTimestamp();
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
        UpdateTimestamp();
    }

    public void SetCapacity(long? capacityBytes)
    {
        CapacityBytes = capacityBytes;
        if (capacityBytes.HasValue)
        {
            FreeBytes = capacityBytes.Value - UsedBytes;
        }
        UpdateTimestamp();
    }

    public void UpdateUsedBytes(long usedBytes)
    {
        UsedBytes = usedBytes;
        if (CapacityBytes.HasValue)
        {
            FreeBytes = CapacityBytes.Value - UsedBytes;
        }
        UpdateTimestamp();
    }

    public void SetMaxFileSize(long? maxFileSize)
    {
        MaxFileSize = maxFileSize;
        UpdateTimestamp();
    }

    public void AddAllowedExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            throw new ArgumentException("Extension cannot be empty", nameof(extension));

        if (!AllowedExtensions.Contains(extension.ToLowerInvariant()))
        {
            AllowedExtensions.Add(extension.ToLowerInvariant());
            UpdateTimestamp();
        }
    }

    public void RemoveAllowedExtension(string extension)
    {
        AllowedExtensions.Remove(extension.ToLowerInvariant());
        UpdateTimestamp();
    }

    public void AddBlockedExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            throw new ArgumentException("Extension cannot be empty", nameof(extension));

        if (!BlockedExtensions.Contains(extension.ToLowerInvariant()))
        {
            BlockedExtensions.Add(extension.ToLowerInvariant());
            UpdateTimestamp();
        }
    }

    public void RemoveBlockedExtension(string extension)
    {
        BlockedExtensions.Remove(extension.ToLowerInvariant());
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

    public void SetPriority(int priority)
    {
        Priority = priority;
        UpdateTimestamp();
    }

    public void AddSetting(string key, object value)
    {
        Settings[key] = value;
        UpdateTimestamp();
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }

    public bool CanStoreFile(string extension, long fileSize)
    {
        if (!IsActive || HealthStatus == "Error")
            return false;

        if (MaxFileSize.HasValue && fileSize > MaxFileSize.Value)
            return false;

        var ext = extension.ToLowerInvariant().TrimStart('.');
        
        if (BlockedExtensions.Contains(ext))
            return false;

        if (AllowedExtensions.Any() && !AllowedExtensions.Contains(ext))
            return false;

        return true;
    }

    public double GetUsagePercentage()
    {
        if (!CapacityBytes.HasValue || CapacityBytes.Value == 0)
            return 0;

        return (double)UsedBytes / CapacityBytes.Value * 100;
    }
}
