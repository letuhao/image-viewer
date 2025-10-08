using MongoDB.Bson;

namespace ImageViewer.Application.DTOs.Cache;

/// <summary>
/// Cache statistics DTO
/// </summary>
public class CacheStatisticsDto
{
    public CacheSummaryDto Summary { get; set; } = null!;
    public IEnumerable<CacheFolderStatisticsDto> CacheFolders { get; set; } = new List<CacheFolderStatisticsDto>();
}

/// <summary>
/// Cache summary DTO
/// </summary>
public class CacheSummaryDto
{
    public int TotalCollections { get; set; }
    public int CollectionsWithCache { get; set; }
    public int TotalImages { get; set; }
    public int CachedImages { get; set; }
    public long TotalCacheSize { get; set; }
    public double CachePercentage { get; set; }
}

/// <summary>
/// Cache folder statistics DTO
/// </summary>
public class CacheFolderStatisticsDto
{
    public ObjectId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int Priority { get; set; }
    public long MaxSize { get; set; }
    public long CurrentSize { get; set; }
    public long FileCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime LastUsed { get; set; }
}

/// <summary>
/// Cache folder DTO
/// </summary>
public class CacheFolderDto
{
    public ObjectId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int Priority { get; set; }
    public long MaxSize { get; set; }
    public long CurrentSize { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Create cache folder DTO
/// </summary>
public class CreateCacheFolderDto
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int Priority { get; set; }
    public long MaxSize { get; set; }
}

/// <summary>
/// Update cache folder DTO
/// </summary>
public class UpdateCacheFolderDto
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int Priority { get; set; }
    public long MaxSize { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Collection cache status DTO
/// </summary>
public class CollectionCacheStatusDto
{
    public ObjectId CollectionId { get; set; }
    public int TotalImages { get; set; }
    public int CachedImages { get; set; }
    public double CachePercentage { get; set; }
    public DateTime? LastCacheUpdate { get; set; }
}
