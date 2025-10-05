using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for CollectionRating entity
/// </summary>
public interface ICollectionRatingRepository : IRepository<CollectionRating>
{
    /// <summary>
    /// Get ratings by collection ID
    /// </summary>
    Task<IEnumerable<CollectionRating>> GetByCollectionIdAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get ratings by user ID
    /// </summary>
    Task<IEnumerable<CollectionRating>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get ratings by rating value
    /// </summary>
    Task<IEnumerable<CollectionRating>> GetByRatingAsync(int rating, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get average rating for a collection
    /// </summary>
    Task<double> GetAverageRatingAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get rating count for a collection
    /// </summary>
    Task<int> GetRatingCountAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
}
