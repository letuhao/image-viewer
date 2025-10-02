using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB implementation of image metadata repository
/// </summary>
public class MongoImageMetadataRepository : MongoRepository<ImageMetadataEntity>, IImageMetadataRepository
{
    public MongoImageMetadataRepository(IMongoDatabase database) : base(database, "image_metadata")
    {
    }
}
