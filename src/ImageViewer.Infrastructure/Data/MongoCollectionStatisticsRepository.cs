using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Exceptions;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB implementation of collection statistics repository
/// </summary>
public class MongoCollectionStatisticsRepository : MongoRepository<CollectionStatisticsEntity>, ICollectionStatisticsRepository
{
    public MongoCollectionStatisticsRepository(IMongoDatabase database) : base(database, "collection_statistics")
    {
    }

    public async Task<CollectionStatisticsEntity> GetByIdAsync(ObjectId id)
    {
        var filter = Builders<CollectionStatisticsEntity>.Filter.Eq(x => x.Id, id);
        var result = await _collection.Find(filter).FirstOrDefaultAsync();
        return result ?? throw new EntityNotFoundException($"CollectionStatisticsEntity with ID {id} not found");
    }

    public async Task<IEnumerable<CollectionStatisticsEntity>> GetAllAsync()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }

    public async Task<CollectionStatisticsEntity> CreateAsync(CollectionStatisticsEntity entity)
    {
        await _collection.InsertOneAsync(entity);
        return entity;
    }

    public async Task<CollectionStatisticsEntity> UpdateAsync(CollectionStatisticsEntity entity)
    {
        var filter = Builders<CollectionStatisticsEntity>.Filter.Eq(x => x.Id, entity.Id);
        await _collection.ReplaceOneAsync(filter, entity);
        return entity;
    }

    public async Task DeleteAsync(ObjectId id)
    {
        var filter = Builders<CollectionStatisticsEntity>.Filter.Eq(x => x.Id, id);
        await _collection.DeleteOneAsync(filter);
    }
}
