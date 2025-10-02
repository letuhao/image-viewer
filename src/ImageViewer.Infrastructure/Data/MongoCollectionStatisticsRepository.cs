using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB implementation of collection statistics repository
/// </summary>
public class MongoCollectionStatisticsRepository : MongoRepository<CollectionStatistics>, ICollectionStatisticsRepository
{
    public MongoCollectionStatisticsRepository(IMongoDatabase database) : base(database, "collection_statistics")
    {
    }
}
