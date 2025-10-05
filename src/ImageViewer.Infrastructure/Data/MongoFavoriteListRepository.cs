using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB repository implementation for FavoriteList entity
/// </summary>
public class MongoFavoriteListRepository : MongoRepository<FavoriteList>, IFavoriteListRepository
{
    public MongoFavoriteListRepository(IMongoDatabase database, ILogger<MongoFavoriteListRepository> logger) 
        : base(database.GetCollection<FavoriteList>("favoriteLists"), logger)
    {
    }

    public async Task<IEnumerable<FavoriteList>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(list => list.UserId == userId)
                .SortByDescending(list => list.UpdatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get favorite lists for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<FavoriteList>> GetByTypeAsync(string type, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(list => list.ListName == type)
                .SortByDescending(list => list.UpdatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get favorite lists for type {Type}", type);
            throw;
        }
    }

    public async Task<IEnumerable<FavoriteList>> GetPublicAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(list => list.IsPublic == true)
                .SortByDescending(list => list.UpdatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get public favorite lists");
            throw;
        }
    }

    public async Task<IEnumerable<FavoriteList>> GetByMediaIdAsync(ObjectId mediaId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(list => list.Tags.Contains(mediaId.ToString()))
                .SortByDescending(list => list.UpdatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get favorite lists for media {MediaId}", mediaId);
            throw;
        }
    }

    public async Task<IEnumerable<FavoriteList>> GetByCollectionIdAsync(ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(list => list.CollectionId == collectionId)
                .SortByDescending(list => list.UpdatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get favorite lists for collection {CollectionId}", collectionId);
            throw;
        }
    }
}
