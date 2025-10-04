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

    public async Task<Collection?> GetByPathAsync(string path, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.Path, path) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Collection>> GetActiveCollectionsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Collection>> GetActiveCollectionsQueryableAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Collection>> GetByTypeAsync(CollectionType type, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.Type, type) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Collection>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.And(
            Builders<Collection>.Filter.Eq(x => x.IsDeleted, false),
            Builders<Collection>.Filter.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"))
        );
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Collection>> GetCollectionsWithImagesAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.And(
            Builders<Collection>.Filter.Eq(x => x.IsDeleted, false),
            Builders<Collection>.Filter.Exists("Images", true)
        );
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByTagAsync(string tagName, CancellationToken cancellationToken = default)
    {
        // This would require a more complex aggregation pipeline in MongoDB
        // For now, we'll get all active collections and filter in memory
        var collections = await GetActiveCollectionsAsync(cancellationToken);
        return collections.Where(c => c.Tags.Any(t => t.TagId.ToString().Contains(tagName)));
    }

    public async Task<long> GetTotalSizeAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        var collections = await _collection.Find(filter).ToListAsync(cancellationToken);
        return collections.Sum(c => c.GetTotalSize());
    }

    public async Task<int> GetTotalImageCountAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        var collections = await _collection.Find(filter).ToListAsync(cancellationToken);
        return collections.Sum(c => c.GetImageCount());
    }

    public async Task<Collection> GetByPathAsync(string path, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.Path, path) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        var result = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return result ?? throw new EntityNotFoundException($"Collection with path '{path}' not found");
    }

    public async Task<IEnumerable<Collection>> GetByLibraryIdAsync(ObjectId libraryId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.LibraryId, libraryId) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Collection>> GetActiveCollectionsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsActive, true) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByTypeAsync(CollectionType type, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.Type, type) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<long> GetCollectionCountAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task<long> GetActiveCollectionCountAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsActive, true) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Collection>> SearchCollectionsAsync(string query, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Or(
            Builders<Collection>.Filter.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(query, "i")),
            Builders<Collection>.Filter.Regex(x => x.Path, new MongoDB.Bson.BsonRegularExpression(query, "i"))
        ) & Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByFilterAsync(CollectionFilter filter, CancellationToken cancellationToken = default)
    {
        var mongoFilter = Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        
        if (filter.Type.HasValue)
            mongoFilter &= Builders<Collection>.Filter.Eq(x => x.Type, filter.Type.Value);
        
        if (filter.IsActive.HasValue)
            mongoFilter &= Builders<Collection>.Filter.Eq(x => x.IsActive, filter.IsActive.Value);
        
        if (!string.IsNullOrEmpty(filter.Name))
            mongoFilter &= Builders<Collection>.Filter.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(filter.Name, "i"));
        
        return await _collection.Find(mongoFilter).ToListAsync(cancellationToken);
    }

    public async Task<ImageViewer.Domain.ValueObjects.CollectionStatistics> GetCollectionStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        var collections = await _collection.Find(filter).ToListAsync(cancellationToken);
        
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

    public async Task<IEnumerable<Collection>> GetTopCollectionsByActivityAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter)
            .Sort(Builders<Collection>.Sort.Descending(x => x.Statistics.LastViewed))
            .Limit(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Collection>> GetRecentCollectionsAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter)
            .Sort(Builders<Collection>.Sort.Descending(x => x.CreatedAt))
            .Limit(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<Collection> GetByPathAsync(string path, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.Path, path) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        var result = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return result ?? throw new EntityNotFoundException($"Collection with path '{path}' not found");
    }

    public async Task<IEnumerable<Collection>> GetByLibraryIdAsync(ObjectId libraryId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.LibraryId, libraryId) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Collection>> GetActiveCollectionsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsActive, true) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByTypeAsync(CollectionType type, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.Type, type) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<long> GetCollectionCountAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }
}
