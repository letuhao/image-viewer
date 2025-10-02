using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB implementation of tag repository
/// </summary>
public class MongoTagRepository : MongoRepository<ImageViewer.Domain.Entities.Tag>, ITagRepository
{
    public MongoTagRepository(IMongoDatabase database) : base(database, "tags")
    {
    }

    /// <summary>
    /// Get tag by name
    /// </summary>
    public async Task<ImageViewer.Domain.Entities.Tag?> GetByNameAsync(string name)
    {
        var filter = Builders<ImageViewer.Domain.Entities.Tag>.Filter.Eq(x => x.Name, name);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Search tags by name
    /// </summary>
    public async Task<IEnumerable<ImageViewer.Domain.Entities.Tag>> SearchByNameAsync(string query)
    {
        var filter = Builders<ImageViewer.Domain.Entities.Tag>.Filter.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(query, "i"));
        var sort = Builders<ImageViewer.Domain.Entities.Tag>.Sort.Ascending(x => x.Name);
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }

    /// <summary>
    /// Get popular tags
    /// </summary>
    public async Task<IEnumerable<ImageViewer.Domain.Entities.Tag>> GetPopularTagsAsync(int limit = 20)
    {
        var sort = Builders<ImageViewer.Domain.Entities.Tag>.Sort.Descending(x => x.UsageCount);
        return await _collection.Find(_ => true).Sort(sort).Limit(limit).ToListAsync();
    }

    /// <summary>
    /// Get tags by collection ID
    /// </summary>
    public async Task<IEnumerable<ImageViewer.Domain.Entities.Tag>> GetByCollectionIdAsync(Guid collectionId)
    {
        // Note: Tag entity doesn't have CollectionId directly, this would need to be implemented
        // through the CollectionTag relationship. For now, return empty collection.
        return new List<ImageViewer.Domain.Entities.Tag>();
    }

    /// <summary>
    /// Get tag usage count
    /// </summary>
    public async Task<int> GetUsageCountAsync(Guid tagId)
    {
        var tag = await GetByIdAsync(tagId);
        return tag?.UsageCount ?? 0;
    }
}
