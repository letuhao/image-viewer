using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using MongoDB.Bson;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB implementation of collection tag repository
/// </summary>
public class MongoCollectionTagRepository : MongoRepository<CollectionTag>, ICollectionTagRepository
{
    public MongoCollectionTagRepository(IMongoDatabase database) : base(database, "collection_tags")
    {
    }

    /// <summary>
    /// Get collection tags by collection ID
    /// </summary>
    public async Task<IEnumerable<CollectionTag>> GetByCollectionIdAsync(ObjectId collectionId)
    {
        var filter = Builders<CollectionTag>.Filter.Eq(x => x.CollectionId, collectionId);
        var sort = Builders<CollectionTag>.Sort.Ascending(x => x.CreatedAt);
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }

    /// <summary>
    /// Get collection tags by tag ID
    /// </summary>
    public async Task<IEnumerable<CollectionTag>> GetByTagIdAsync(ObjectId tagId)
    {
        var filter = Builders<CollectionTag>.Filter.Eq(x => x.TagId, tagId);
        var sort = Builders<CollectionTag>.Sort.Ascending(x => x.CreatedAt);
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }

    /// <summary>
    /// Get collection tag by collection and tag IDs
    /// </summary>
    public async Task<CollectionTag?> GetByCollectionAndTagAsync(ObjectId collectionId, ObjectId tagId)
    {
        var filter = Builders<CollectionTag>.Filter.And(
            Builders<CollectionTag>.Filter.Eq(x => x.CollectionId, collectionId),
            Builders<CollectionTag>.Filter.Eq(x => x.TagId, tagId)
        );
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Check if collection has tag
    /// </summary>
    public async Task<bool> HasTagAsync(ObjectId collectionId, ObjectId tagId)
    {
        var filter = Builders<CollectionTag>.Filter.And(
            Builders<CollectionTag>.Filter.Eq(x => x.CollectionId, collectionId),
            Builders<CollectionTag>.Filter.Eq(x => x.TagId, tagId)
        );
        return await _collection.Find(filter).AnyAsync();
    }

    /// <summary>
    /// Get tag usage statistics
    /// </summary>

    /// <summary>
    /// Get collections by tag ID
    /// </summary>
    public async Task<IEnumerable<CollectionTag>> GetCollectionsByTagIdAsync(ObjectId tagId)
    {
        var filter = Builders<CollectionTag>.Filter.Eq(x => x.TagId, tagId);
        var sort = Builders<CollectionTag>.Sort.Ascending(x => x.CreatedAt);
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }

    public async Task<IEnumerable<CollectionTag>> GetByCollectionIdAsync(ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<CollectionTag>.Filter.Eq(x => x.CollectionId, collectionId);
        var sort = Builders<CollectionTag>.Sort.Ascending(x => x.CreatedAt);
        return await _collection.Find(filter).Sort(sort).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CollectionTag>> GetByTagIdAsync(ObjectId tagId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<CollectionTag>.Filter.Eq(x => x.TagId, tagId);
        var sort = Builders<CollectionTag>.Sort.Ascending(x => x.CreatedAt);
        return await _collection.Find(filter).Sort(sort).ToListAsync(cancellationToken);
    }

    public async Task<CollectionTag?> GetByCollectionAndTagAsync(ObjectId collectionId, ObjectId tagId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<CollectionTag>.Filter.And(
            Builders<CollectionTag>.Filter.Eq(x => x.CollectionId, collectionId),
            Builders<CollectionTag>.Filter.Eq(x => x.TagId, tagId)
        );
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasTagAsync(ObjectId collectionId, ObjectId tagId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<CollectionTag>.Filter.And(
            Builders<CollectionTag>.Filter.Eq(x => x.CollectionId, collectionId),
            Builders<CollectionTag>.Filter.Eq(x => x.TagId, tagId)
        );
        var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        return count > 0;
    }


    public async Task<IEnumerable<CollectionTag>> GetCollectionsByTagIdAsync(ObjectId tagId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<CollectionTag>.Filter.Eq(x => x.TagId, tagId);
        var sort = Builders<CollectionTag>.Sort.Ascending(x => x.CreatedAt);
        return await _collection.Find(filter).Sort(sort).ToListAsync(cancellationToken);
    }

    public async Task<CollectionTag?> GetByCollectionIdAndTagIdAsync(ObjectId collectionId, ObjectId tagId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<CollectionTag>.Filter.And(
            Builders<CollectionTag>.Filter.Eq(x => x.CollectionId, collectionId),
            Builders<CollectionTag>.Filter.Eq(x => x.TagId, tagId)
        );
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Dictionary<ObjectId, int>> GetTagUsageCountsAsync()
    {
        var pipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$TagId" },
                { "count", new BsonDocument("$sum", 1) }
            })
        };
        
        var results = await _collection.Aggregate<BsonDocument>(pipeline).ToListAsync();
        
        return results.ToDictionary(
            r => r["_id"].AsObjectId,
            r => r["count"].AsInt32
        );
    }

    public async Task<CollectionTag?> GetByCollectionIdAndTagIdAsync(ObjectId collectionId, ObjectId tagId)
    {
        var filter = Builders<CollectionTag>.Filter.And(
            Builders<CollectionTag>.Filter.Eq(x => x.CollectionId, collectionId),
            Builders<CollectionTag>.Filter.Eq(x => x.TagId, tagId)
        );
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }
}
