using ImageViewer.Domain.Entities;

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
}
