using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB implementation of image cache info repository
/// </summary>
public class MongoImageCacheInfoRepository : MongoRepository<ImageCacheInfo>, IImageCacheInfoRepository
{
    public MongoImageCacheInfoRepository(IMongoDatabase database) : base(database, "image_cache_info")
    {
    }
}
