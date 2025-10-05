using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using MongoDB.Bson;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB implementation of view session repository
/// </summary>
public class MongoViewSessionRepository : MongoRepository<ViewSession>, IViewSessionRepository
{
    public MongoViewSessionRepository(IMongoDatabase database) : base(database, "view_sessions")
    {
    }

    /// <summary>
    /// Get view sessions by collection ID
    /// </summary>
    public async Task<IEnumerable<ViewSession>> GetByCollectionIdAsync(ObjectId collectionId)
    {
        var filter = Builders<ViewSession>.Filter.Eq(x => x.CollectionId, collectionId);
        var sort = Builders<ViewSession>.Sort.Descending(x => x.StartedAt);
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }

    /// <summary>
    /// Get view sessions by user ID
    /// </summary>
    public async Task<IEnumerable<ViewSession>> GetByUserIdAsync(string userId)
    {
        // Note: ViewSession doesn't have UserId property directly
        // This would need to be implemented based on the actual user tracking structure
        return new List<ViewSession>();
    }

    /// <summary>
    /// Get view sessions by date range
    /// </summary>
    public async Task<IEnumerable<ViewSession>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        var filter = Builders<ViewSession>.Filter.And(
            Builders<ViewSession>.Filter.Gte(x => x.StartedAt, fromDate),
            Builders<ViewSession>.Filter.Lte(x => x.StartedAt, toDate)
        );
        var sort = Builders<ViewSession>.Sort.Descending(x => x.StartedAt);
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }

    /// <summary>
    /// Get recent view sessions
    /// </summary>
    public async Task<IEnumerable<ViewSession>> GetRecentAsync(int limit = 20)
    {
        var sort = Builders<ViewSession>.Sort.Descending(x => x.StartedAt);
        return await _collection.Find(_ => true).Sort(sort).Limit(limit).ToListAsync();
    }

    /// <summary>
    /// Get view session statistics
    /// </summary>
    public async Task<ViewSessionStatistics> GetStatisticsAsync()
    {
        var pipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", null },
                { "totalSessions", new BsonDocument("$sum", 1) },
                { "totalViewTime", new BsonDocument("$sum", "$Duration") },
                { "uniqueCollections", new BsonDocument("$addToSet", "$CollectionId") },
                { "uniqueUsers", new BsonDocument("$addToSet", "$UserId") }
            }),
            new BsonDocument("$project", new BsonDocument
            {
                { "totalSessions", 1 },
                { "totalViewTime", 1 },
                { "averageViewTime", new BsonDocument("$divide", new BsonArray { "$totalViewTime", "$totalSessions" }) },
                { "uniqueCollections", new BsonDocument("$size", "$uniqueCollections") },
                { "uniqueUsers", new BsonDocument("$size", "$uniqueUsers") }
            })
        };

        var result = await _collection.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
        
        if (result == null)
        {
            return new ViewSessionStatistics();
        }

        return new ViewSessionStatistics
        {
            TotalSessions = result.GetValue("totalSessions", 0).ToInt32(),
            TotalViewTime = result.GetValue("totalViewTime", 0.0).ToDouble(),
            AverageViewTime = result.GetValue("averageViewTime", 0.0).ToDouble(),
            UniqueCollections = result.GetValue("uniqueCollections", 0).ToInt32(),
            UniqueUsers = result.GetValue("uniqueUsers", 0).ToInt32()
        };
    }

    /// <summary>
    /// Get popular collections
    /// </summary>
    public async Task<IEnumerable<PopularCollection>> GetPopularCollectionsAsync(int limit = 10)
    {
        var pipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$CollectionId" },
                { "viewCount", new BsonDocument("$sum", 1) },
                { "totalViewTime", new BsonDocument("$sum", "$Duration") }
            }),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "collections" },
                { "localField", "_id" },
                { "foreignField", "_id" },
                { "as", "collection" }
            }),
            new BsonDocument("$unwind", "$collection"),
            new BsonDocument("$project", new BsonDocument
            {
                { "collectionId", "$_id" },
                { "collectionName", "$collection.Name" },
                { "viewCount", 1 },
                { "totalViewTime", 1 }
            }),
            new BsonDocument("$sort", new BsonDocument("viewCount", -1)),
            new BsonDocument("$limit", limit)
        };

        var results = await _collection.Aggregate<BsonDocument>(pipeline).ToListAsync();
        
        return results.Select(r => new PopularCollection
        {
            CollectionId = r.GetValue("collectionId").AsGuid,
            CollectionName = r.GetValue("collectionName", "").AsString,
            ViewCount = r.GetValue("viewCount", 0).ToInt32(),
            TotalViewTime = r.GetValue("totalViewTime", 0.0).ToDouble()
        });
    }
}
