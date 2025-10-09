using ImageViewer.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

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
