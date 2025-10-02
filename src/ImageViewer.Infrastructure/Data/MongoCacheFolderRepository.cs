using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB implementation of cache folder repository
/// </summary>
public class MongoCacheFolderRepository : MongoRepository<CacheFolder>, ICacheFolderRepository
{
    public MongoCacheFolderRepository(IMongoDatabase database) : base(database, "cache_folders")
    {
    }

    /// <summary>
    /// Get cache folder by path
    /// </summary>
    public async Task<CacheFolder?> GetByPathAsync(string path)
    {
        var filter = Builders<CacheFolder>.Filter.Eq(x => x.Path, path);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Get active cache folders ordered by priority
    /// </summary>
    public async Task<IEnumerable<CacheFolder>> GetActiveOrderedByPriorityAsync()
    {
        var filter = Builders<CacheFolder>.Filter.Eq(x => x.IsActive, true);
        var sort = Builders<CacheFolder>.Sort.Ascending(x => x.Priority);
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }

    /// <summary>
    /// Get cache folders by priority range
    /// </summary>
    public async Task<IEnumerable<CacheFolder>> GetByPriorityRangeAsync(int minPriority, int maxPriority)
    {
        var filter = Builders<CacheFolder>.Filter.And(
            Builders<CacheFolder>.Filter.Gte(x => x.Priority, minPriority),
            Builders<CacheFolder>.Filter.Lte(x => x.Priority, maxPriority)
        );
        var sort = Builders<CacheFolder>.Sort.Ascending(x => x.Priority);
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }
}
