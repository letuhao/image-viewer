using ImageViewer.Application.DTOs.Cache;
using MongoDB.Bson;

namespace ImageViewer.Application.DTOs.Statistics;

/// <summary>
/// Collection statistics DTO
/// </summary>
public class CollectionStatisticsDto
{
    public ObjectId CollectionId { get; set; }
    public int ViewCount { get; set; }
    public double TotalViewTime { get; set; }
    public int SearchCount { get; set; }
    public DateTime? LastViewed { get; set; }
    public DateTime? LastSearched { get; set; }
    public double AverageViewTime { get; set; }
    public int TotalImages { get; set; }
    public long TotalSize { get; set; }
    public long AverageFileSize { get; set; }
    public int CachedImages { get; set; }
    public double CachePercentage { get; set; }
    public IEnumerable<PopularImageDto> PopularImages { get; set; } = new List<PopularImageDto>();
}

/// <summary>
/// System statistics DTO
/// </summary>
public class SystemStatisticsDto
{
    public int TotalCollections { get; set; }
    public int TotalImages { get; set; }
    public long TotalSize { get; set; }
    public long TotalCacheSize { get; set; }
    public int TotalViewSessions { get; set; }
    public double TotalViewTime { get; set; }
    public double AverageImagesPerCollection { get; set; }
    public double AverageViewTimePerSession { get; set; }
}

/// <summary>
/// Image statistics DTO
/// </summary>
public class ImageStatisticsDto
{
    public int TotalImages { get; set; }
    public long TotalSize { get; set; }
    public long AverageFileSize { get; set; }
    public int CachedImages { get; set; }
    public double CachePercentage { get; set; }
    public IEnumerable<FormatStatisticsDto> FormatStatistics { get; set; } = new List<FormatStatisticsDto>();
}

/// <summary>
/// Format statistics DTO
/// </summary>
public class FormatStatisticsDto
{
    public string Format { get; set; } = string.Empty;
    public int Count { get; set; }
    public long TotalSize { get; set; }
    public double AverageSize { get; set; }
}

/// <summary>
/// User activity statistics DTO
/// </summary>
public class UserActivityStatisticsDto
{
    public int TotalSessions { get; set; }
    public double TotalViewTime { get; set; }
    public double AverageViewTime { get; set; }
    public IEnumerable<DailyActivityDto> DailyActivity { get; set; } = new List<DailyActivityDto>();
}

/// <summary>
/// Daily activity DTO
/// </summary>
public class DailyActivityDto
{
    public DateTime Date { get; set; }
    public int Sessions { get; set; }
    public double TotalViewTime { get; set; }
}

/// <summary>
/// Performance statistics DTO
/// </summary>
public class PerformanceStatisticsDto
{
    public double AverageResponseTime { get; set; }
    public double AverageImageLoadTime { get; set; }
    public double AverageThumbnailGenerationTime { get; set; }
    public double AverageCacheHitRate { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public double SuccessRate { get; set; }
}

/// <summary>
/// Storage statistics DTO
/// </summary>
public class StorageStatisticsDto
{
    public long TotalImageSize { get; set; }
    public long TotalCacheSize { get; set; }
    public long TotalStorageSize { get; set; }
    public IEnumerable<CacheFolderStorageDto> CacheFolders { get; set; } = new List<CacheFolderStorageDto>();
}

/// <summary>
/// Cache folder storage DTO
/// </summary>
public class CacheFolderStorageDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long MaxSize { get; set; }
    public long CurrentSize { get; set; }
    public double UsagePercentage { get; set; }
}

/// <summary>
/// Popular image DTO
/// </summary>
public class PopularImageDto
{
    public ObjectId Id { get; set; }
    public string Filename { get; set; } = string.Empty;
    public int ViewCount { get; set; }
}

/// <summary>
/// Recent activity DTO
/// </summary>
public class RecentActivityDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public double Duration { get; set; }
}

/// <summary>
/// Statistics summary DTO
/// </summary>
public class StatisticsSummaryDto
{
    public SystemStatisticsDto System { get; set; } = null!;
    public ImageStatisticsDto Images { get; set; } = null!;
    public CacheStatisticsDto Cache { get; set; } = null!;
    public PerformanceStatisticsDto Performance { get; set; } = null!;
    public DateTime LastUpdated { get; set; }
}
