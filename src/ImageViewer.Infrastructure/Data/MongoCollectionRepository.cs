using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Enums;

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
}
