namespace ImageViewer.Api.DTOs;

/// <summary>
/// Cache folder data transfer object
/// </summary>
public class CacheFolderDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long MaxSizeBytes { get; set; }
    public long CurrentSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Cache statistics data transfer object
/// </summary>
public class CacheStatisticsDto
{
    public int TotalImages { get; set; }
    public int CachedImages { get; set; }
    public int ExpiredImages { get; set; }
    public long TotalCacheSize { get; set; }
    public long AvailableSpace { get; set; }
    public double CacheHitRate { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Cache generation request DTO
/// </summary>
public class CacheGenerationRequest
{
    public Guid CollectionId { get; set; }
    public bool ForceRegenerate { get; set; }
    public bool GenerateThumbnails { get; set; }
    public bool GeneratePreviews { get; set; }
    public int ThumbnailSize { get; set; }
    public int PreviewSize { get; set; }
    public int PreviewQuality { get; set; }
}

/// <summary>
/// Cache generation response DTO
/// </summary>
public class CacheGenerationResponse
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
}

/// <summary>
/// Cache cleanup request DTO
/// </summary>
public class CacheCleanupRequest
{
    public Guid? CollectionId { get; set; }
    public bool RemoveExpired { get; set; }
    public bool RemoveUnused { get; set; }
    public long? MaxAgeDays { get; set; }
}

/// <summary>
/// Cache cleanup response DTO
/// </summary>
public class CacheCleanupResponse
{
    public int RemovedImages { get; set; }
    public long FreedSpace { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
}

