using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for FavoriteList entity
/// </summary>
public interface IFavoriteListRepository : IRepository<FavoriteList>
{
    /// <summary>
    /// Get favorite lists by user ID
    /// </summary>
    Task<IEnumerable<FavoriteList>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get favorite lists by type
    /// </summary>
    Task<IEnumerable<FavoriteList>> GetByTypeAsync(string type, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get public favorite lists
    /// </summary>
    Task<IEnumerable<FavoriteList>> GetPublicAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get favorite lists by media ID
    /// </summary>
    Task<IEnumerable<FavoriteList>> GetByMediaIdAsync(ObjectId mediaId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get favorite lists by collection ID
    /// </summary>
    Task<IEnumerable<FavoriteList>> GetByCollectionIdAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
}
