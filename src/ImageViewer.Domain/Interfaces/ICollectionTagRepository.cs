using ImageViewer.Domain.Entities;
using MongoDB.Bson;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Collection tag repository interface
/// </summary>
public interface ICollectionTagRepository : IRepository<CollectionTag>
{
    /// <summary>
    /// Get collection tags by collection ID
    /// </summary>
    Task<IEnumerable<CollectionTag>> GetByCollectionIdAsync(ObjectId collectionId);

    /// <summary>
    /// Get collection tags by tag ID
    /// </summary>
    Task<IEnumerable<CollectionTag>> GetByTagIdAsync(ObjectId tagId);

    /// <summary>
    /// Get collection tag by collection and tag IDs
    /// </summary>
    Task<CollectionTag?> GetByCollectionAndTagAsync(ObjectId collectionId, ObjectId tagId);

    /// <summary>
    /// Check if collection has tag
    /// </summary>
    Task<bool> HasTagAsync(ObjectId collectionId, ObjectId tagId);

    /// <summary>
    /// Get tag usage statistics
    /// </summary>
    Task<Dictionary<ObjectId, int>> GetTagUsageCountsAsync();

    /// <summary>
    /// Get collections by tag ID
    /// </summary>
    Task<IEnumerable<CollectionTag>> GetCollectionsByTagIdAsync(ObjectId tagId);

    /// <summary>
    /// Get collection tag by collection ID and tag ID
    /// </summary>
    Task<CollectionTag?> GetByCollectionIdAndTagIdAsync(ObjectId collectionId, ObjectId tagId);
}
