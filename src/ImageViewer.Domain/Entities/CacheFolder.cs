using ImageViewer.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

#pragma warning disable CS8618 // MongoDB entities/value objects are initialized by the driver

namespace ImageViewer.Domain.Entities;

/// <summary>
/// CacheFolder entity - represents a cache storage location
/// </summary>
public class CacheFolder : BaseEntity
{
    [BsonElement("name")]
    public string Name { get; private set; }
    
    [BsonElement("path")]
    public string Path { get; private set; }
    
    [BsonElement("maxSizeBytes")]
    public long MaxSizeBytes { get; private set; }
    
    [BsonElement("currentSizeBytes")]
    public long CurrentSizeBytes { get; private set; }
    
    // Alias properties for compatibility
    [BsonIgnore]
    public long MaxSize => MaxSizeBytes;
    
    [BsonIgnore]
    public long CurrentSize => CurrentSizeBytes;
    
    [BsonElement("priority")]
    public int Priority { get; private set; }
    
    [BsonElement("isActive")]
    public bool IsActive { get; private set; }
    
    // Enhanced statistics - 增强的统计信息 - Thống kê nâng cao
    [BsonElement("totalCollections")]
    public int TotalCollections { get; private set; } // Total number of collections using this cache folder
    
    [BsonElement("totalFiles")]
    public int TotalFiles { get; private set; } // Total number of cached files
    
    [BsonElement("cachedCollectionIds")]
    public List<string> CachedCollectionIds { get; private set; } = new(); // List of collection IDs that have cache in this folder
    
    [BsonElement("lastCacheGeneratedAt")]
    public DateTime? LastCacheGeneratedAt { get; private set; } // Last time a cache was generated
    
    [BsonElement("lastCleanupAt")]
    public DateTime? LastCleanupAt { get; private set; } // Last time cache was cleaned up

    // Navigation properties
    private readonly List<CollectionCacheBinding> _bindings = new();
    public IReadOnlyCollection<CollectionCacheBinding> Bindings => _bindings.AsReadOnly();

    // Private constructor for EF Core
    private CacheFolder() { }

    public CacheFolder(string name, string path, long maxSizeBytes, int priority = 0)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        MaxSizeBytes = maxSizeBytes;
        Priority = priority;
        IsActive = true;
        CurrentSizeBytes = 0;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        Name = name;
        UpdateTimestamp();
    }

    public void UpdatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        Path = path;
        UpdateTimestamp();
    }

    public void UpdateMaxSize(long maxSizeBytes)
    {
        if (maxSizeBytes < 0)
            throw new ArgumentException("Max size cannot be negative", nameof(maxSizeBytes));

        MaxSizeBytes = maxSizeBytes;
        UpdateTimestamp();
    }

    public void UpdatePriority(int priority)
    {
        Priority = priority;
        UpdateTimestamp();
    }

    public void Activate()
    {
        IsActive = true;
        UpdateTimestamp();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdateTimestamp();
    }

    public void AddSize(long sizeBytes)
    {
        if (sizeBytes < 0)
            throw new ArgumentException("Size cannot be negative", nameof(sizeBytes));

        CurrentSizeBytes += sizeBytes;
        UpdateTimestamp();
    }

    public void RemoveSize(long sizeBytes)
    {
        if (sizeBytes < 0)
            throw new ArgumentException("Size cannot be negative", nameof(sizeBytes));

        CurrentSizeBytes = Math.Max(0, CurrentSizeBytes - sizeBytes);
        UpdateTimestamp();
    }

    public void AddBinding(CollectionCacheBinding binding)
    {
        if (binding == null)
            throw new ArgumentNullException(nameof(binding));

        if (_bindings.Any(b => b.CollectionId == binding.CollectionId))
            throw new InvalidOperationException($"Collection '{binding.CollectionId}' is already bound to this cache folder");

        _bindings.Add(binding);
        UpdateTimestamp();
    }

    public void RemoveBinding(Guid collectionId)
    {
        var binding = _bindings.FirstOrDefault(b => b.CollectionId == collectionId);
        if (binding == null)
            throw new InvalidOperationException($"Collection '{collectionId}' is not bound to this cache folder");

        _bindings.Remove(binding);
        UpdateTimestamp();
    }

    public bool HasSpace(long requiredBytes)
    {
        return CurrentSizeBytes + requiredBytes <= MaxSizeBytes;
    }

    public long GetAvailableSpace()
    {
        return MaxSizeBytes - CurrentSizeBytes;
    }

    public double GetUsagePercentage()
    {
        return MaxSizeBytes > 0 ? (double)CurrentSizeBytes / MaxSizeBytes * 100 : 0;
    }

    public void UpdateStatistics(long currentSize, int fileCount)
    {
        CurrentSizeBytes = currentSize;
        TotalFiles = fileCount;
        UpdateTimestamp();
    }

    public void IncrementFileCount(int count = 1)
    {
        TotalFiles += count;
        LastCacheGeneratedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void DecrementFileCount(int count = 1)
    {
        TotalFiles = Math.Max(0, TotalFiles - count);
        UpdateTimestamp();
    }

    public void AddCachedCollection(string collectionId)
    {
        if (string.IsNullOrEmpty(collectionId))
            throw new ArgumentNullException(nameof(collectionId));

        if (!CachedCollectionIds.Contains(collectionId))
        {
            CachedCollectionIds.Add(collectionId);
            TotalCollections = CachedCollectionIds.Count;
            UpdateTimestamp();
        }
    }

    public void RemoveCachedCollection(string collectionId)
    {
        if (CachedCollectionIds.Remove(collectionId))
        {
            TotalCollections = CachedCollectionIds.Count;
            UpdateTimestamp();
        }
    }

    public void UpdateLastCacheGeneratedAt()
    {
        LastCacheGeneratedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void UpdateLastCleanupAt()
    {
        LastCleanupAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public bool IsFull()
    {
        return CurrentSizeBytes >= MaxSizeBytes;
    }

    public bool IsNearFull(double threshold = 0.9)
    {
        return GetUsagePercentage() >= threshold * 100;
    }
}