using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB implementation of collection settings repository
/// </summary>
public class MongoCollectionSettingsRepository : MongoRepository<CollectionSettingsEntity>, ICollectionSettingsRepository
{
    public MongoCollectionSettingsRepository(IMongoDatabase database) : base(database, "collection_settings")
    {
    }
}
