using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using MongoDB.Bson;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB implementation of cache info repository
/// </summary>
public class MongoCacheInfoRepository : MongoRepository<ImageCacheInfo>, ICacheInfoRepository
{
    public MongoCacheInfoRepository(IMongoDatabase database) : base(database, "image_cache_info")
    {
    }

    /// <summary>
    /// Get cache info by image ID
    /// </summary>
    public async Task<ImageCacheInfo?> GetByImageIdAsync(Guid imageId)
    {
        var filter = Builders<ImageCacheInfo>.Filter.Eq(x => x.ImageId, imageId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Get cache info by cache folder ID
    /// </summary>
    public async Task<IEnumerable<ImageCacheInfo>> GetByCacheFolderIdAsync(Guid cacheFolderId)
    {
        // Note: ImageCacheInfo doesn't have CacheFolderId property
        // This would need to be implemented based on the actual cache folder structure
        return new List<ImageCacheInfo>();
    }

    /// <summary>
    /// Get expired cache entries
    /// </summary>
    public async Task<IEnumerable<ImageCacheInfo>> GetExpiredAsync()
    {
        var now = DateTime.UtcNow;
        var filter = Builders<ImageCacheInfo>.Filter.Lt(x => x.ExpiresAt, now);
        var sort = Builders<ImageCacheInfo>.Sort.Ascending(x => x.ExpiresAt);
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }

    /// <summary>
    /// Get cache entries older than specified date
    /// </summary>
    public async Task<IEnumerable<ImageCacheInfo>> GetOlderThanAsync(DateTime cutoffDate)
    {
        var filter = Builders<ImageCacheInfo>.Filter.Lt(x => x.CachedAt, cutoffDate);
        var sort = Builders<ImageCacheInfo>.Sort.Ascending(x => x.CachedAt);
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        var pipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", null },
                { "totalEntries", new BsonDocument("$sum", 1) },
                { "totalSize", new BsonDocument("$sum", "$FileSize") },
                { "expiredEntries", new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray
                {
                    new BsonDocument("$lt", new BsonArray { "$ExpiresAt", DateTime.UtcNow }),
                    1,
                    0
                })) },
                { "activeEntries", new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray
                {
                    new BsonDocument("$gte", new BsonArray { "$ExpiresAt", DateTime.UtcNow }),
                    1,
                    0
                })) }
            })
        };

        var result = await _collection.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
        
        if (result == null)
        {
            return new CacheStatistics();
        }

        return new CacheStatistics
        {
            TotalCacheEntries = result.GetValue("totalEntries", 0).ToInt32(),
            TotalCacheSize = result.GetValue("totalSize", 0L).ToInt64(),
            ExpiredCacheEntries = result.GetValue("expiredEntries", 0).ToInt32(),
            ValidCacheEntries = result.GetValue("activeEntries", 0).ToInt32(),
            AverageCacheSize = result.GetValue("totalEntries", 0).ToInt32() > 0 ? 
                (double)result.GetValue("totalSize", 0L).ToInt64() / result.GetValue("totalEntries", 0).ToInt32() : 0
        };
    }
}
