using MongoDB.Bson;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service interface for performance optimization operations
/// </summary>
public interface IPerformanceService
{
    #region Caching Operations
    
    Task<CacheInfo> GetCacheInfoAsync();
    Task<CacheInfo> ClearCacheAsync(CacheType? cacheType = null);
    Task<CacheInfo> OptimizeCacheAsync();
    Task<CacheStatistics> GetCacheStatisticsAsync();
    
    #endregion
    
    #region Image Processing Optimization
    
    Task<ImageProcessingInfo> GetImageProcessingInfoAsync();
    Task<ImageProcessingInfo> OptimizeImageProcessingAsync();
    Task<ImageProcessingStatistics> GetImageProcessingStatisticsAsync();
    
    #endregion
    
    #region Database Query Optimization
    
    Task<DatabasePerformanceInfo> GetDatabasePerformanceInfoAsync();
    Task<DatabasePerformanceInfo> OptimizeDatabaseQueriesAsync();
    Task<DatabaseStatistics> GetDatabaseStatisticsAsync();
    
    #endregion
    
    #region CDN Integration
    
    Task<CDNInfo> GetCDNInfoAsync();
    Task<CDNInfo> ConfigureCDNAsync(CDNConfigurationRequest request);
    Task<CDNStatistics> GetCDNStatisticsAsync();
    
    #endregion
    
    #region Lazy Loading
    
    Task<LazyLoadingInfo> GetLazyLoadingInfoAsync();
    Task<LazyLoadingInfo> ConfigureLazyLoadingAsync(LazyLoadingConfigurationRequest request);
    Task<LazyLoadingStatistics> GetLazyLoadingStatisticsAsync();
    
    #endregion
    
    #region Performance Monitoring
    
    Task<PerformanceMetrics> GetPerformanceMetricsAsync();
    Task<PerformanceMetrics> GetPerformanceMetricsByTimeRangeAsync(DateTime fromDate, DateTime toDate);
    Task<PerformanceReport> GeneratePerformanceReportAsync(DateTime? fromDate = null, DateTime? toDate = null);
    
    #endregion
}

/// <summary>
/// Request model for CDN configuration
/// </summary>
public class CDNConfigurationRequest
{
    public string Provider { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public bool EnableCompression { get; set; } = true;
    public bool EnableCaching { get; set; } = true;
    public int CacheExpiration { get; set; } = 3600;
    public List<string> AllowedFileTypes { get; set; } = new();
}

/// <summary>
/// Request model for lazy loading configuration
/// </summary>
public class LazyLoadingConfigurationRequest
{
    public bool EnableLazyLoading { get; set; } = true;
    public int BatchSize { get; set; } = 20;
    public int PreloadCount { get; set; } = 5;
    public int MaxConcurrentRequests { get; set; } = 3;
    public bool EnableImagePreloading { get; set; } = true;
    public bool EnableMetadataPreloading { get; set; } = true;
    public int PreloadTimeout { get; set; } = 5000;
}

/// <summary>
/// Cache information
/// </summary>
public class CacheInfo
{
    public ObjectId Id { get; set; }
    public CacheType Type { get; set; }
    public long Size { get; set; }
    public int ItemCount { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsOptimized { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Cache statistics
/// </summary>
public class CacheStatistics
{
    public long TotalSize { get; set; }
    public int TotalItems { get; set; }
    public double HitRate { get; set; }
    public double MissRate { get; set; }
    public long TotalHits { get; set; }
    public long TotalMisses { get; set; }
    public DateTime LastReset { get; set; }
    public Dictionary<CacheType, CacheInfo> CacheByType { get; set; } = new();
}

/// <summary>
/// Image processing information
/// </summary>
public class ImageProcessingInfo
{
    public ObjectId Id { get; set; }
    public bool IsOptimized { get; set; }
    public int MaxConcurrentProcesses { get; set; }
    public int QueueSize { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastOptimized { get; set; }
    public List<string> SupportedFormats { get; set; } = new();
    public List<string> OptimizationSettings { get; set; } = new();
}

/// <summary>
/// Image processing statistics
/// </summary>
public class ImageProcessingStatistics
{
    public long TotalProcessed { get; set; }
    public long TotalFailed { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan AverageProcessingTime { get; set; }
    public long TotalProcessingTime { get; set; }
    public DateTime LastProcessed { get; set; }
    public Dictionary<string, long> ProcessedByFormat { get; set; } = new();
    public Dictionary<string, long> ProcessedBySize { get; set; } = new();
}

/// <summary>
/// Database performance information
/// </summary>
public class DatabasePerformanceInfo
{
    public ObjectId Id { get; set; }
    public bool IsOptimized { get; set; }
    public int ActiveConnections { get; set; }
    public int MaxConnections { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastOptimized { get; set; }
    public List<string> OptimizedQueries { get; set; } = new();
    public List<string> Indexes { get; set; } = new();
}

/// <summary>
/// Database statistics
/// </summary>
public class DatabaseStatistics
{
    public long TotalQueries { get; set; }
    public long SlowQueries { get; set; }
    public double AverageQueryTime { get; set; }
    public TimeSpan TotalQueryTime { get; set; }
    public DateTime LastOptimized { get; set; }
    public Dictionary<string, long> QueriesByType { get; set; } = new();
    public Dictionary<string, double> QueryTimesByType { get; set; } = new();
}

/// <summary>
/// CDN information
/// </summary>
public class CDNInfo
{
    public ObjectId Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool EnableCompression { get; set; }
    public bool EnableCaching { get; set; }
    public int CacheExpiration { get; set; }
    public List<string> AllowedFileTypes { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public DateTime LastConfigured { get; set; }
}

/// <summary>
/// CDN statistics
/// </summary>
public class CDNStatistics
{
    public long TotalRequests { get; set; }
    public long TotalBytesServed { get; set; }
    public double AverageResponseTime { get; set; }
    public double CacheHitRate { get; set; }
    public DateTime LastRequest { get; set; }
    public Dictionary<string, long> RequestsByFileType { get; set; } = new();
    public Dictionary<string, long> RequestsByRegion { get; set; } = new();
}

/// <summary>
/// Lazy loading information
/// </summary>
public class LazyLoadingInfo
{
    public ObjectId Id { get; set; }
    public bool IsEnabled { get; set; }
    public int BatchSize { get; set; }
    public int PreloadCount { get; set; }
    public int MaxConcurrentRequests { get; set; }
    public bool EnableImagePreloading { get; set; }
    public bool EnableMetadataPreloading { get; set; }
    public int PreloadTimeout { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastConfigured { get; set; }
}

/// <summary>
/// Lazy loading statistics
/// </summary>
public class LazyLoadingStatistics
{
    public long TotalRequests { get; set; }
    public long TotalPreloaded { get; set; }
    public double PreloadSuccessRate { get; set; }
    public TimeSpan AveragePreloadTime { get; set; }
    public DateTime LastPreload { get; set; }
    public Dictionary<string, long> PreloadedByType { get; set; } = new();
    public Dictionary<string, double> PreloadTimesByType { get; set; } = new();
}

/// <summary>
/// Performance metrics
/// </summary>
public class PerformanceMetrics
{
    public ObjectId Id { get; set; }
    public DateTime Timestamp { get; set; }
    public double CpuUsage { get; set; }
    public long MemoryUsage { get; set; }
    public long DiskUsage { get; set; }
    public long NetworkUsage { get; set; }
    public double ResponseTime { get; set; }
    public long RequestCount { get; set; }
    public double ErrorRate { get; set; }
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
}

/// <summary>
/// Performance report
/// </summary>
public class PerformanceReport
{
    public ObjectId Id { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public PerformanceSummary Summary { get; set; } = new();
    public List<PerformanceMetrics> Metrics { get; set; } = new();
    public List<PerformanceRecommendation> Recommendations { get; set; } = new();
}

/// <summary>
/// Performance summary
/// </summary>
public class PerformanceSummary
{
    public double AverageCpuUsage { get; set; }
    public long AverageMemoryUsage { get; set; }
    public long AverageDiskUsage { get; set; }
    public long AverageNetworkUsage { get; set; }
    public double AverageResponseTime { get; set; }
    public long TotalRequests { get; set; }
    public double AverageErrorRate { get; set; }
    public string OverallStatus { get; set; } = string.Empty;
}

/// <summary>
/// Performance recommendation
/// </summary>
public class PerformanceRecommendation
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public List<string> Actions { get; set; } = new();
}

/// <summary>
/// Cache type enum
/// </summary>
public enum CacheType
{
    Image,
    Thumbnail,
    Metadata,
    Search,
    User,
    System
}
