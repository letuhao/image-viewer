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
    /// Add a collection to the cached collections list
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
