using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB implementation of collection statistics repository
/// </summary>
public class MongoCollectionStatisticsRepository : MongoRepository<CollectionStatisticsEntity>, ICollectionStatisticsRepository
{
    public MongoCollectionStatisticsRepository(IMongoDatabase database) : base(database, "collection_statistics")
    {
    }

    // All CRUD operations are inherited from MongoRepository<T> base class
    // Additional collection-specific methods can be added here if needed
}
