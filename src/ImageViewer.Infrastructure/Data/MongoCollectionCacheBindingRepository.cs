using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB implementation of collection cache binding repository
/// </summary>
public class MongoCollectionCacheBindingRepository : MongoRepository<CollectionCacheBinding>, ICollectionCacheBindingRepository
{
    public MongoCollectionCacheBindingRepository(IMongoDatabase database) : base(database, "collection_cache_bindings")
    {
    }
}
