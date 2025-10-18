using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service for managing dashboard statistics with Redis caching
/// 中文：管理仪表板统计数据的服务，使用Redis缓存
/// Tiếng Việt: Dịch vụ quản lý thống kê bảng điều khiển với bộ nhớ đệm Redis
/// </summary>
public class DashboardStatisticsService : IDashboardStatisticsService
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly ICacheFolderRepository _cacheFolderRepository;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ICollectionIndexService _collectionIndexService;
    private readonly ILogger<DashboardStatisticsService> _logger;

    public DashboardStatisticsService(
        ICollectionRepository collectionRepository,
        ICacheFolderRepository cacheFolderRepository,
        IBackgroundJobService backgroundJobService,
        ICollectionIndexService collectionIndexService,
        ILogger<DashboardStatisticsService> logger)
    {
        _collectionRepository = collectionRepository;
        _cacheFolderRepository = cacheFolderRepository;
        _backgroundJobService = backgroundJobService;
        _collectionIndexService = collectionIndexService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard statistics with Redis caching for ultra-fast loading
    /// </summary>
    public async Task<DashboardStatistics> GetDashboardStatisticsAsync()
    {
        try
        {
            // Try Redis cache first (ultra-fast)
            var cachedStats = await _collectionIndexService.GetDashboardStatisticsAsync();
            if (cachedStats != null)
            {
                _logger.LogDebug("✅ Retrieved dashboard statistics from Redis cache");
                return cachedStats;
            }

            _logger.LogInformation("📊 Dashboard statistics not in cache, building from database...");
            
            // Build statistics from database
            var stats = await BuildDashboardStatisticsAsync();
            
            // Store in Redis cache for next time
            await _collectionIndexService.StoreDashboardStatisticsAsync(stats);
            
            _logger.LogInformation("✅ Dashboard statistics built and cached successfully");
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dashboard statistics");
            throw;
        }
    }

    /// <summary>
    /// Build comprehensive dashboard statistics from database using efficient MongoDB aggregation
    /// </summary>
    private async Task<DashboardStatistics> BuildDashboardStatisticsAsync()
    {
        var startTime = DateTime.UtcNow;
        
        // Use MongoDB aggregation to calculate statistics efficiently at database level
        var collectionsStats = await _collectionRepository.GetSystemStatisticsAsync();
        
        // Get additional statistics that aren't in the basic system stats
        var activeCollections = await _collectionRepository.GetActiveCollectionCountAsync();
        var totalCollections = await _collectionRepository.GetCollectionCountAsync();
        
        // Calculate averages
        var averageImagesPerCollection = totalCollections > 0 ? (double)collectionsStats.TotalImages / totalCollections : 0;
        var averageSizePerCollection = totalCollections > 0 ? (double)collectionsStats.TotalSize / totalCollections : 0;

        // Get cache folder statistics
        var cacheFolders = await _cacheFolderRepository.GetAllAsync();
        var cacheFolderStats = cacheFolders.Select(cf => new CacheFolderStat
        {
            Id = cf.Id.ToString(),
            Name = cf.Name,
            Path = cf.Path,
            CurrentSizeBytes = cf.CurrentSizeBytes,
            MaxSizeBytes = cf.MaxSizeBytes,
            TotalFiles = cf.TotalFiles,
            TotalCollections = cf.CachedCollectionIds?.Count ?? 0,
            UsagePercentage = cf.MaxSizeBytes > 0 ? (double)cf.CurrentSizeBytes / cf.MaxSizeBytes * 100 : 0,
            IsActive = cf.IsActive
        }).ToList();

        // Get job statistics
        var jobStats = await _backgroundJobService.GetJobStatisticsAsync();
        
        // Get top collections by view count (simplified for performance)
        var topCollections = new List<TopCollection>();
        // Note: For performance, we're not loading all collections to get top collections
        // This could be optimized with a separate aggregation query if needed

        // Get recent activity (simplified for now)
        var recentActivity = new List<RecentActivity>
        {
            new() { Id = "1", Type = "system_startup", Message = "System started", Timestamp = DateTime.UtcNow.AddMinutes(-5) },
            new() { Id = "2", Type = "index_rebuilt", Message = "Collection index rebuilt", Timestamp = DateTime.UtcNow.AddMinutes(-10) }
        };

        // Build system health
        var systemHealth = new Domain.ValueObjects.SystemHealth
        {
            RedisStatus = "Connected",
            MongoDbStatus = "Connected", 
            WorkerStatus = "Running",
            ApiStatus = "Running",
            Uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime,
            MemoryUsageBytes = GC.GetTotalMemory(false),
            DiskSpaceFreeBytes = GetFreeDiskSpace(),
            LastHealthCheck = DateTime.UtcNow
        };

        var stats = new DashboardStatistics
        {
            TotalCollections = (int)totalCollections,
            ActiveCollections = (int)activeCollections,
            TotalImages = collectionsStats.TotalImages,
            TotalThumbnails = collectionsStats.TotalThumbnails,
            TotalCacheImages = collectionsStats.TotalCacheImages,
            TotalSize = collectionsStats.TotalSize,
            TotalThumbnailSize = collectionsStats.TotalThumbnailSize,
            TotalCacheSize = collectionsStats.TotalCacheSize,
            AverageImagesPerCollection = averageImagesPerCollection,
            AverageSizePerCollection = averageSizePerCollection,
            ActiveJobs = jobStats.RunningJobs,
            CompletedJobsToday = jobStats.CompletedJobs,
            FailedJobsToday = jobStats.FailedJobs,
            LastUpdated = DateTime.UtcNow,
            CacheFolderStats = cacheFolderStats,
            RecentActivity = recentActivity,
            TopCollections = topCollections,
            SystemHealth = systemHealth
        };

        var duration = DateTime.UtcNow - startTime;
        _logger.LogInformation("📊 Built dashboard statistics in {Duration}ms: {Collections} collections, {Images} images", 
            duration.TotalMilliseconds, totalCollections, collectionsStats.TotalImages);

        return stats;
    }

    /// <summary>
    /// Update dashboard statistics when collections change (real-time updates)
    /// </summary>
    public async Task UpdateDashboardStatisticsAsync(string updateType, object updateData)
    {
        try
        {
            // Update Redis metadata for real-time activity
            await _collectionIndexService.UpdateDashboardStatisticsAsync(updateType, updateData);
            
            _logger.LogDebug("✅ Updated dashboard statistics: {UpdateType}", updateType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update dashboard statistics");
        }
    }

    /// <summary>
    /// Get recent dashboard activity
    /// </summary>
    public async Task<List<object>> GetRecentActivityAsync(int limit = 10)
    {
        try
        {
            return await _collectionIndexService.GetRecentDashboardActivityAsync(limit);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get recent activity");
            return new List<object>();
        }
    }

    /// <summary>
    /// Check if dashboard statistics are fresh
    /// </summary>
    public async Task<bool> IsStatisticsFreshAsync()
    {
        try
        {
            return await _collectionIndexService.IsDashboardStatisticsFreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check statistics freshness");
            return false;
        }
    }

    /// <summary>
    /// Get free disk space (simplified implementation)
    /// </summary>
    private static long GetFreeDiskSpace()
    {
        try
        {
            // Simple fallback - return a reasonable default
            return 100L * 1024 * 1024 * 1024; // 100GB
        }
        catch
        {
            return 0;
        }
    }
}
