using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Comprehensive dashboard statistics stored in Redis for ultra-fast loading
/// 中文：存储在Redis中的综合仪表板统计数据，用于超快速加载
/// Tiếng Việt: Thống kê tổng quan của bảng điều khiển được lưu trữ trong Redis để tải nhanh
/// </summary>
public class DashboardStatistics
{
    [BsonElement("totalCollections")]
    public int TotalCollections { get; set; }

    [BsonElement("activeCollections")]
    public int ActiveCollections { get; set; }

    [BsonElement("totalImages")]
    public long TotalImages { get; set; }

    [BsonElement("totalThumbnails")]
    public long TotalThumbnails { get; set; }

    [BsonElement("totalCacheImages")]
    public long TotalCacheImages { get; set; }

    [BsonElement("totalSize")]
    public long TotalSize { get; set; }

    [BsonElement("totalThumbnailSize")]
    public long TotalThumbnailSize { get; set; }

    [BsonElement("totalCacheSize")]
    public long TotalCacheSize { get; set; }

    [BsonElement("averageImagesPerCollection")]
    public double AverageImagesPerCollection { get; set; }

    [BsonElement("averageSizePerCollection")]
    public double AverageSizePerCollection { get; set; }

    [BsonElement("activeJobs")]
    public int ActiveJobs { get; set; }

    [BsonElement("completedJobsToday")]
    public int CompletedJobsToday { get; set; }

    [BsonElement("failedJobsToday")]
    public int FailedJobsToday { get; set; }

    [BsonElement("lastUpdated")]
    public DateTime LastUpdated { get; set; }

    [BsonElement("cacheFolderStats")]
    public List<CacheFolderStat> CacheFolderStats { get; set; } = new();

    [BsonElement("recentActivity")]
    public List<RecentActivity> RecentActivity { get; set; } = new();

    [BsonElement("topCollections")]
    public List<TopCollection> TopCollections { get; set; } = new();

    [BsonElement("systemHealth")]
    public SystemHealth SystemHealth { get; set; } = new();
}

/// <summary>
/// Cache folder statistics for dashboard
/// </summary>
public class CacheFolderStat
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("path")]
    public string Path { get; set; } = string.Empty;

    [BsonElement("currentSizeBytes")]
    public long CurrentSizeBytes { get; set; }

    [BsonElement("maxSizeBytes")]
    public long MaxSizeBytes { get; set; }

    [BsonElement("totalFiles")]
    public long TotalFiles { get; set; }

    [BsonElement("totalCollections")]
    public int TotalCollections { get; set; }

    [BsonElement("usagePercentage")]
    public double UsagePercentage { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; }
}

/// <summary>
/// Recent activity item for dashboard
/// </summary>
public class RecentActivity
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; set; } = string.Empty; // "collection_created", "scan_completed", "job_failed", etc.

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("collectionId")]
    public string? CollectionId { get; set; }

    [BsonElement("collectionName")]
    public string? CollectionName { get; set; }
}

/// <summary>
/// Top collection for dashboard
/// </summary>
public class TopCollection
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("imageCount")]
    public long ImageCount { get; set; }

    [BsonElement("totalSize")]
    public long TotalSize { get; set; }

    [BsonElement("viewCount")]
    public long ViewCount { get; set; }

    [BsonElement("lastViewed")]
    public DateTime? LastViewed { get; set; }

    [BsonElement("thumbnailPath")]
    public string? ThumbnailPath { get; set; }
}

/// <summary>
/// System health metrics for dashboard
/// </summary>
public class SystemHealth
{
    [BsonElement("redisStatus")]
    public string RedisStatus { get; set; } = "Unknown";

    [BsonElement("mongodbStatus")]
    public string MongoDbStatus { get; set; } = "Unknown";

    [BsonElement("workerStatus")]
    public string WorkerStatus { get; set; } = "Unknown";

    [BsonElement("apiStatus")]
    public string ApiStatus { get; set; } = "Unknown";

    [BsonElement("uptime")]
    public TimeSpan Uptime { get; set; }

    [BsonElement("memoryUsage")]
    public long MemoryUsageBytes { get; set; }

    [BsonElement("diskSpaceFree")]
    public long DiskSpaceFreeBytes { get; set; }

    [BsonElement("lastHealthCheck")]
    public DateTime LastHealthCheck { get; set; }
}
