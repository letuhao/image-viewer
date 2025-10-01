using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Collection tag repository interface
/// </summary>
public interface ICollectionTagRepository : IRepository<CollectionTag>
{
    /// <summary>
    /// Get collection tags by collection ID
    /// </summary>
    Task<IEnumerable<CollectionTag>> GetByCollectionIdAsync(Guid collectionId);

    /// <summary>
    /// Get collection tags by tag ID
    /// </summary>
    Task<IEnumerable<CollectionTag>> GetByTagIdAsync(Guid tagId);

    /// <summary>
    /// Get collection tag by collection and tag IDs
    /// </summary>
    Task<CollectionTag?> GetByCollectionAndTagAsync(Guid collectionId, Guid tagId);

    /// <summary>
    /// Check if collection has tag
    /// </summary>
    Task<bool> HasTagAsync(Guid collectionId, Guid tagId);

    /// <summary>
    /// Get tag usage statistics
    /// </summary>
    Task<Dictionary<Guid, int>> GetTagUsageCountsAsync();

    /// <summary>
    /// Get collections by tag ID
    /// </summary>
    Task<IEnumerable<CollectionTag>> GetCollectionsByTagIdAsync(Guid tagId);
}
