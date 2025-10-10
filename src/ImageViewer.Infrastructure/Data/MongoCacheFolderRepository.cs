using MongoDB.Driver;
using MongoDB.Bson;
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

    /// <summary>
    /// Atomically increment cache folder size using MongoDB $inc operator
    /// Thread-safe for concurrent operations - prevents race conditions
    /// Pattern copied from BackgroundJob atomic updates
    /// </summary>
    public async Task IncrementSizeAsync(ObjectId folderId, long sizeBytes)
    {
        var filter = Builders<CacheFolder>.Filter.Eq(x => x.Id, folderId);
        
        // SINGLE ATOMIC UPDATE: Only increment - don't try to do multiple updates
        var update = Builders<CacheFolder>.Update
            .Inc(x => x.CurrentSizeBytes, sizeBytes)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _collection.UpdateOneAsync(filter, update);
    }

    /// <summary>
    /// Atomically decrement cache folder size using MongoDB $inc operator
    /// Thread-safe for concurrent operations - prevents race conditions
    /// Pattern copied from BackgroundJob atomic updates
    /// </summary>
    public async Task DecrementSizeAsync(ObjectId folderId, long sizeBytes)
    {
        var filter = Builders<CacheFolder>.Filter.And(
            Builders<CacheFolder>.Filter.Eq(x => x.Id, folderId),
            Builders<CacheFolder>.Filter.Gte(x => x.CurrentSizeBytes, sizeBytes) // Only decrement if we have enough
        );
        
        // SINGLE ATOMIC UPDATE: Only decrement - MongoDB handles the operation atomically
        var update = Builders<CacheFolder>.Update
            .Inc(x => x.CurrentSizeBytes, -sizeBytes)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(filter, update);
        
        // If update didn't match (not enough bytes), just set to 0
        if (result.ModifiedCount == 0)
        {
            var resetFilter = Builders<CacheFolder>.Filter.Eq(x => x.Id, folderId);
            var resetUpdate = Builders<CacheFolder>.Update
                .Max(x => x.CurrentSizeBytes, 0L)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);
            
            await _collection.UpdateOneAsync(resetFilter, resetUpdate);
        }
    }
}
