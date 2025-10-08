using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB implementation of image cache info repository
/// OBSOLETE: Use embedded ImageCacheInfoEmbedded in Collection entity instead. This implementation is kept only for backward compatibility.
/// </summary>
[Obsolete("Use embedded ImageCacheInfoEmbedded in Collection entity instead. Will be removed in future version.")]
public class MongoImageCacheInfoRepository : MongoRepository<ImageCacheInfo>, IImageCacheInfoRepository
{
    public MongoImageCacheInfoRepository(IMongoDatabase database) : base(database, "image_cache_info")
    {
    }
}
