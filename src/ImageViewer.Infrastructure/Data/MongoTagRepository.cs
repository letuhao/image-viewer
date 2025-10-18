using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB implementation of tag repository
/// </summary>
public class MongoTagRepository : MongoRepository<Domain.Entities.Tag>, ITagRepository
{
    public MongoTagRepository(IMongoDatabase database) : base(database, "tags")
    {
    }

    /// <summary>
    /// Get tag by name
    /// </summary>
    public async Task<Domain.Entities.Tag?> GetByNameAsync(string name)
    {
        var filter = Builders<Domain.Entities.Tag>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Search tags by name
    /// </summary>
    public async Task<IEnumerable<Domain.Entities.Tag>> SearchByNameAsync(string query)
    {
        var filter = Builders<Domain.Entities.Tag>.Filter.Regex(x => x.Name, new BsonRegularExpression(query, "i"));
        var sort = Builders<Domain.Entities.Tag>.Sort.Ascending(x => x.Name);
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }

    /// <summary>
    /// Get popular tags
    /// </summary>
    public async Task<IEnumerable<Domain.Entities.Tag>> GetPopularTagsAsync(int limit = 20)
    {
        var sort = Builders<Domain.Entities.Tag>.Sort.Descending(x => x.UsageCount);
        return await _collection.Find(_ => true).Sort(sort).Limit(limit).ToListAsync();
    }

    /// <summary>
    /// Get tags by collection ID
    /// </summary>
    public async Task<IEnumerable<Domain.Entities.Tag>> GetByCollectionIdAsync(Guid collectionId)
    {
        // Note: Tag entity doesn't have CollectionId directly, this would need to be implemented
        // through the CollectionTag relationship. For now, return empty collection.
        return new List<Domain.Entities.Tag>();
    }

    /// <summary>
    /// Get tag usage count
    /// </summary>
    public async Task<int> GetUsageCountAsync(ObjectId tagId)
    {
        var tag = await GetByIdAsync(tagId);
        return tag?.UsageCount ?? 0;
    }

    public async Task<IEnumerable<Domain.Entities.Tag>> GetByCollectionIdAsync(ObjectId collectionId)
    {
        // This is a simplified implementation
        // In a real scenario, you would join with CollectionTag collection
        var filter = Builders<Domain.Entities.Tag>.Filter.Empty;
        return await _collection.Find(filter).ToListAsync();
    }

}
