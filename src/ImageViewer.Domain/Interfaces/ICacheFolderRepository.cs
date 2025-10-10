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
}
