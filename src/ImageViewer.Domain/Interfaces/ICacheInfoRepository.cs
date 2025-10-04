using ImageViewer.Domain.Entities;
using ImageViewer.Domain.ValueObjects;
using MongoDB.Bson;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Cache info repository interface
/// </summary>
public interface ICacheInfoRepository : IRepository<ImageCacheInfo>
{
    /// <summary>
    /// Get cache info by image ID
    /// </summary>
    Task<ImageCacheInfo?> GetByImageIdAsync(ObjectId imageId);

    /// <summary>
    /// Get cache info by cache folder ID
    /// </summary>
    Task<IEnumerable<ImageCacheInfo>> GetByCacheFolderIdAsync(ObjectId cacheFolderId);

    /// <summary>
    /// Get expired cache entries
    /// </summary>
    Task<IEnumerable<ImageCacheInfo>> GetExpiredAsync();

    /// <summary>
    /// Get cache entries older than specified date
    /// </summary>
    Task<IEnumerable<ImageCacheInfo>> GetOlderThanAsync(DateTime cutoffDate);

    /// <summary>
    /// Get cache statistics
    /// </summary>
    Task<CacheStatistics> GetStatisticsAsync();
}
