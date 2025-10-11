using ImageViewer.Domain.Entities;
using MongoDB.Bson;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Cache folder repository interface
/// </summary>
public interface ICacheFolderRepository : IRepository<CacheFolder>
{
    /// <summary>
    /// Get cache folder by path
    /// </summary>
    Task<CacheFolder?> GetByPathAsync(string path);

    /// <summary>
    /// Get active cache folders ordered by priority
    /// </summary>
    Task<IEnumerable<CacheFolder>> GetActiveOrderedByPriorityAsync();

    /// <summary>
    /// Get cache folders by priority range
    /// </summary>
    Task<IEnumerable<CacheFolder>> GetByPriorityRangeAsync(int minPriority, int maxPriority);

    /// <summary>
    /// Atomically increment cache folder size (thread-safe for concurrent operations)
    /// </summary>
    Task IncrementSizeAsync(ObjectId folderId, long sizeBytes);

    /// <summary>
    /// Atomically decrement cache folder size (thread-safe for concurrent operations)
    /// </summary>
    Task DecrementSizeAsync(ObjectId folderId, long sizeBytes);

    /// <summary>
    /// Atomically increment file count
    /// </summary>
    Task IncrementFileCountAsync(ObjectId folderId, int count = 1);

    /// <summary>
    /// Atomically decrement file count
    /// </summary>
    Task DecrementFileCountAsync(ObjectId folderId, int count = 1);

    /// <summary>
    /// Atomically increment cache statistics (size and file count) in single transaction
    /// 在单个事务中原子增加缓存统计信息（大小和文件数） - Tăng thống kê bộ nhớ cache nguyên tử (kích thước và số tệp) trong một giao dịch
    /// </summary>
    Task IncrementCacheStatisticsAsync(ObjectId folderId, long sizeBytes, int fileCount = 1);

    /// <summary>
    /// Add a collection to the cached collections list (atomically updates TotalCollections)
    /// 添加集合到缓存集合列表（原子更新集合总数） - Thêm bộ sưu tập vào danh sách bộ sưu tập được lưu trong bộ nhớ cache (cập nhật tổng số nguyên tử)
    /// </summary>
    Task AddCachedCollectionAsync(ObjectId folderId, string collectionId);

    /// <summary>
    /// Remove a collection from the cached collections list
    /// </summary>
    Task RemoveCachedCollectionAsync(ObjectId folderId, string collectionId);

    /// <summary>
    /// Update last cache generated timestamp
    /// </summary>
    Task UpdateLastCacheGeneratedAsync(ObjectId folderId);

    /// <summary>
    /// Update last cleanup timestamp
    /// </summary>
    Task UpdateLastCleanupAsync(ObjectId folderId);
}
