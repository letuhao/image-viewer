using ImageViewer.Application.DTOs.Statistics;
using ImageViewer.Application.DTOs.Cache;

namespace ImageViewer.Application.Services;

/// <summary>
/// Statistics service interface for managing statistics operations
/// </summary>
public interface IStatisticsService
{
    /// <summary>
    /// Get collection statistics
    /// </summary>
    Task<CollectionStatisticsDto> GetCollectionStatisticsAsync(Guid collectionId);

    /// <summary>
    /// Get system statistics
    /// </summary>
    Task<SystemStatisticsDto> GetSystemStatisticsAsync();

    /// <summary>
    /// Get image statistics
    /// </summary>
    Task<ImageStatisticsDto> GetImageStatisticsAsync();

    /// <summary>
    /// Get cache statistics
    /// </summary>
    Task<CacheStatisticsDto> GetCacheStatisticsAsync();

    /// <summary>
    /// Get user activity statistics
    /// </summary>
    Task<UserActivityStatisticsDto> GetUserActivityStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Get performance statistics
    /// </summary>
    Task<PerformanceStatisticsDto> GetPerformanceStatisticsAsync();

    /// <summary>
    /// Get storage statistics
    /// </summary>
    Task<StorageStatisticsDto> GetStorageStatisticsAsync();

    /// <summary>
    /// Get popular images for collection
    /// </summary>
    Task<IEnumerable<PopularImageDto>> GetPopularImagesAsync(Guid collectionId, int limit = 10);

    /// <summary>
    /// Get recent activity
    /// </summary>
    Task<IEnumerable<RecentActivityDto>> GetRecentActivityAsync(int limit = 20);

    /// <summary>
    /// Get statistics summary
    /// </summary>
    Task<StatisticsSummaryDto> GetStatisticsSummaryAsync();
}
