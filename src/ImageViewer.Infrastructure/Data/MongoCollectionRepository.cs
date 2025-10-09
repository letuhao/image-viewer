using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Exceptions;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB collection repository implementation
/// </summary>
public class MongoCollectionRepository : MongoRepository<Collection>, ICollectionRepository
{
    public MongoCollectionRepository(IMongoDatabase database) : base(database, "collections")
    {
    }

    public async Task<Collection?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.Name, name) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Collection> GetByPathAsync(string path)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.Path, path) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        var result = await _collection.Find(filter).FirstOrDefaultAsync();
        return result ?? throw new EntityNotFoundException($"Collection with path '{path}' not found");
    }

    public async Task<IEnumerable<Collection>> GetByLibraryIdAsync(ObjectId libraryId)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.LibraryId, libraryId) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<Collection>> GetActiveCollectionsAsync()
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsActive, true) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByTypeAsync(CollectionType type)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.Type, type) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<long> GetCollectionCountAsync()
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.CountDocumentsAsync(filter);
    }

    public async Task<long> GetActiveCollectionCountAsync()
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsActive, true) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.CountDocumentsAsync(filter);
    }

    public async Task<IEnumerable<Collection>> SearchCollectionsAsync(string query)
    {
        var filter = Builders<Collection>.Filter.Or(
            Builders<Collection>.Filter.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(query, "i")),
            Builders<Collection>.Filter.Regex(x => x.Path, new MongoDB.Bson.BsonRegularExpression(query, "i"))
        ) & Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByFilterAsync(CollectionFilter filter)
    {
        var mongoFilter = Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        
        if (filter.Type.HasValue)
            mongoFilter &= Builders<Collection>.Filter.Eq(x => x.Type, filter.Type.Value);
        
        if (filter.IsActive.HasValue)
            mongoFilter &= Builders<Collection>.Filter.Eq(x => x.IsActive, filter.IsActive.Value);
        
        if (!string.IsNullOrEmpty(filter.Name))
            mongoFilter &= Builders<Collection>.Filter.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(filter.Name, "i"));
        
        return await _collection.Find(mongoFilter).ToListAsync();
    }

    public async Task<ImageViewer.Domain.ValueObjects.CollectionStatistics> GetCollectionStatisticsAsync()
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        var collections = await _collection.Find(filter).ToListAsync();
        
        return new ImageViewer.Domain.ValueObjects.CollectionStatistics
        {
            TotalCollections = collections.Count,
            ActiveCollections = collections.Count(c => c.IsActive),
            TotalImages = collections.Sum(c => c.GetImageCount()),
            TotalSize = collections.Sum(c => c.GetTotalSize()),
            AverageImagesPerCollection = collections.Count > 0 ? (double)collections.Sum(c => c.GetImageCount()) / collections.Count : 0,
            AverageSizePerCollection = collections.Count > 0 ? (double)collections.Sum(c => c.GetTotalSize()) / collections.Count : 0
        };
    }

    public async Task<IEnumerable<Collection>> GetTopCollectionsByActivityAsync(int limit = 10)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter)
            .Sort(Builders<Collection>.Sort.Descending(x => x.Statistics.LastViewed))
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Collection>> GetRecentCollectionsAsync(int limit = 10)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter)
            .Sort(Builders<Collection>.Sort.Descending(x => x.CreatedAt))
            .Limit(limit)
            .ToListAsync();
    }

    #region Atomic Array Operations

    public async Task<bool> AtomicAddImageAsync(ObjectId collectionId, Domain.ValueObjects.ImageEmbedded image)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.Id, collectionId);
        var update = Builders<Collection>.Update
            .Push(x => x.Images, image)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        
        var result = await _collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> AtomicAddThumbnailAsync(ObjectId collectionId, Domain.ValueObjects.ThumbnailEmbedded thumbnail)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.Id, collectionId);
        var update = Builders<Collection>.Update
            .Push(x => x.Thumbnails, thumbnail)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        
        var result = await _collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> AtomicAddCacheImageAsync(ObjectId collectionId, Domain.ValueObjects.CacheImageEmbedded cacheImage)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.Id, collectionId);
        var update = Builders<Collection>.Update
            .Push(x => x.CacheImages, cacheImage)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        
        var result = await _collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    #endregion
}