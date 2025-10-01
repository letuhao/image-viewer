using ImageViewer.Domain.Enums;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// CacheFolder entity - represents a cache storage location
/// </summary>
public class CacheFolder : BaseEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Path { get; private set; }
    public long MaxSizeBytes { get; private set; }
    public long CurrentSizeBytes { get; private set; }
    
    // Alias properties for compatibility
    public long MaxSize => MaxSizeBytes;
    public long CurrentSize => CurrentSizeBytes;
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    private readonly List<CollectionCacheBinding> _bindings = new();
    public IReadOnlyCollection<CollectionCacheBinding> Bindings => _bindings.AsReadOnly();

    // Private constructor for EF Core
    private CacheFolder() { }

    public CacheFolder(string name, string path, long maxSizeBytes, int priority = 0)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        MaxSizeBytes = maxSizeBytes;
        Priority = priority;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        CurrentSizeBytes = 0;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        Path = path;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateMaxSize(long maxSizeBytes)
    {
        if (maxSizeBytes < 0)
            throw new ArgumentException("Max size cannot be negative", nameof(maxSizeBytes));

        MaxSizeBytes = maxSizeBytes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePriority(int priority)
    {
        Priority = priority;
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

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddSize(long sizeBytes)
    {
        if (sizeBytes < 0)
            throw new ArgumentException("Size cannot be negative", nameof(sizeBytes));

        CurrentSizeBytes += sizeBytes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveSize(long sizeBytes)
    {
        if (sizeBytes < 0)
            throw new ArgumentException("Size cannot be negative", nameof(sizeBytes));

        CurrentSizeBytes = Math.Max(0, CurrentSizeBytes - sizeBytes);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddBinding(CollectionCacheBinding binding)
    {
        if (binding == null)
            throw new ArgumentNullException(nameof(binding));

        if (_bindings.Any(b => b.CollectionId == binding.CollectionId))
            throw new InvalidOperationException($"Collection '{binding.CollectionId}' is already bound to this cache folder");

        _bindings.Add(binding);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveBinding(Guid collectionId)
    {
        var binding = _bindings.FirstOrDefault(b => b.CollectionId == collectionId);
        if (binding == null)
            throw new InvalidOperationException($"Collection '{collectionId}' is not bound to this cache folder");

        _bindings.Remove(binding);
        UpdatedAt = DateTime.UtcNow;
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
        UpdatedAt = DateTime.UtcNow;
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
