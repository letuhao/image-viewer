using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB repository implementation for CollectionRating entity
/// </summary>
public class MongoCollectionRatingRepository : MongoRepository<CollectionRating>, ICollectionRatingRepository
{
    public MongoCollectionRatingRepository(IMongoDatabase database, ILogger<MongoCollectionRatingRepository> logger) 
        : base(database.GetCollection<CollectionRating>("collectionRatings"), logger)
    {
    }

    public async Task<IEnumerable<CollectionRating>> GetByCollectionIdAsync(ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(rating => rating.CollectionId == collectionId)
                .SortByDescending(rating => rating.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get ratings for collection {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<IEnumerable<CollectionRating>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(rating => rating.UserId == userId)
                .SortByDescending(rating => rating.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get ratings for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<CollectionRating>> GetByRatingAsync(int rating, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(r => r.Rating == rating)
                .SortByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get ratings with value {Rating}", rating);
            throw;
        }
    }

    public async Task<double> GetAverageRatingAsync(ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("CollectionId", collectionId)),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", BsonNull.Value },
                    { "averageRating", new BsonDocument("$avg", "$Rating") }
                })
            };

            var result = await _collection.Aggregate<BsonDocument>(pipeline, cancellationToken: cancellationToken).FirstOrDefaultAsync(cancellationToken);
            return result?["averageRating"].AsDouble ?? 0.0;
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get average rating for collection {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<int> GetRatingCountAsync(ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return (int)await _collection.CountDocumentsAsync(rating => rating.CollectionId == collectionId, cancellationToken: cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get rating count for collection {CollectionId}", collectionId);
            throw;
        }
    }
}
