using MongoDB.Bson;
using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Application.DTOs.Collections;

/// <summary>
/// Collection detail DTO with full information (including embedded images, thumbnails, cache)
/// Only used when viewing a specific collection
/// </summary>
public class CollectionDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string LibraryId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsNested { get; set; }
    public int Depth { get; set; }
    
    // Settings
    public CollectionSettingsDto Settings { get; set; } = new();
    
    // Metadata
    public CollectionMetadataDto Metadata { get; set; } = new();
    
    // Statistics
    public CollectionStatisticsDto Statistics { get; set; } = new();
    
    // Watch info
    public WatchInfoDto WatchInfo { get; set; } = new();
    
    // Search index
    public SearchIndexDto SearchIndex { get; set; } = new();
    
    // Embedded data (full list)
    public List<ImageEmbedded> Images { get; set; } = new();
    public List<ThumbnailEmbedded> Thumbnails { get; set; } = new();
    public List<CacheImageEmbedded> CacheImages { get; set; } = new();
    public List<CacheBinding> CacheBindings { get; set; } = new();
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CollectionSettingsDto
{
    public bool Enabled { get; set; }
    public bool AutoScan { get; set; }
    public bool GenerateThumbnails { get; set; }
    public bool GenerateCache { get; set; }
    public bool EnableWatching { get; set; }
    public int ScanInterval { get; set; }
    public long MaxFileSize { get; set; }
    public List<string> AllowedFormats { get; set; } = new();
    public List<string> ExcludedPaths { get; set; } = new();
    public bool AutoGenerateCache { get; set; }
}

public class CollectionMetadataDto
{
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public Dictionary<string, string> CustomFields { get; set; } = new();
    public string Version { get; set; } = string.Empty;
    public DateTime? LastModified { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string ModifiedBy { get; set; } = string.Empty;
}

public class CollectionStatisticsDto
{
    public long TotalItems { get; set; }
    public long TotalSize { get; set; }
    public long TotalViews { get; set; }
    public long TotalDownloads { get; set; }
    public long TotalShares { get; set; }
    public long TotalLikes { get; set; }
    public long TotalComments { get; set; }
    public DateTime? LastScanDate { get; set; }
    public long ScanCount { get; set; }
    public DateTime? LastActivity { get; set; }
    public long TotalCollections { get; set; }
    public long ActiveCollections { get; set; }
    public long TotalImages { get; set; }
    public double AverageImagesPerCollection { get; set; }
    public double AverageSizePerCollection { get; set; }
    public DateTime? LastViewed { get; set; }
    
    // Processing progress counts (生成进度统计 / Thống kê tiến độ xử lý)
    public int TotalThumbnails { get; set; }  // Number of thumbnails generated
    public int TotalCached { get; set; }      // Number of cached images generated
}

public class WatchInfoDto
{
    public bool IsWatching { get; set; }
    public string WatchPath { get; set; } = string.Empty;
    public List<string> WatchFilters { get; set; } = new();
    public DateTime? LastWatchDate { get; set; }
    public long WatchCount { get; set; }
    public DateTime? LastChangeDetected { get; set; }
    public long ChangeCount { get; set; }
}

public class SearchIndexDto
{
    public string SearchableText { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public List<string> Keywords { get; set; } = new();
    public DateTime? LastIndexed { get; set; }
    public int IndexVersion { get; set; }
}

