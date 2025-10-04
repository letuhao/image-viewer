using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB image repository implementation
/// </summary>
public class MongoImageRepository : MongoRepository<Image>, IImageRepository
{
    public MongoImageRepository(IMongoDatabase database) : base(database, "images")
    {
    }

    public async Task<IEnumerable<Image>> GetByCollectionIdAsync(ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Image>.Filter.Eq(x => x.CollectionId, collectionId) & 
                    Builders<Image>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<Image?> GetByCollectionIdAndFilenameAsync(ObjectId collectionId, string filename, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Image>.Filter.And(
            Builders<Image>.Filter.Eq(x => x.CollectionId, collectionId),
            Builders<Image>.Filter.Eq(x => x.Filename, filename),
            Builders<Image>.Filter.Eq(x => x.IsDeleted, false)
        );
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Image?> GetRandomImageByCollectionAsync(ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Image>.Filter.Eq(x => x.CollectionId, collectionId) & 
                    Builders<Image>.Filter.Eq(x => x.IsDeleted, false);
        
        var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        if (count == 0) return null;
        
        var random = new Random();
        var skip = random.Next(0, (int)count);
        
        return await _collection.Find(filter).Skip(skip).Limit(1).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Image?> GetNextImageAsync(ObjectId currentImageId, CancellationToken cancellationToken = default)
    {
        var currentImage = await GetByIdAsync(currentImageId);
        if (currentImage == null) return null;
        
        var filter = Builders<Image>.Filter.And(
            Builders<Image>.Filter.Eq(x => x.CollectionId, currentImage.CollectionId),
            Builders<Image>.Filter.Gt(x => x.CreatedAt, currentImage.CreatedAt),
            Builders<Image>.Filter.Eq(x => x.IsDeleted, false)
        );
        
        return await _collection.Find(filter).Sort(Builders<Image>.Sort.Ascending(x => x.CreatedAt)).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Image?> GetPreviousImageAsync(ObjectId currentImageId, CancellationToken cancellationToken = default)
    {
        var currentImage = await GetByIdAsync(currentImageId);
        if (currentImage == null) return null;
        
        var filter = Builders<Image>.Filter.And(
            Builders<Image>.Filter.Eq(x => x.CollectionId, currentImage.CollectionId),
            Builders<Image>.Filter.Lt(x => x.CreatedAt, currentImage.CreatedAt),
            Builders<Image>.Filter.Eq(x => x.IsDeleted, false)
        );
        
        return await _collection.Find(filter).Sort(Builders<Image>.Sort.Descending(x => x.CreatedAt)).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<long> GetTotalSizeByCollectionAsync(ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Image>.Filter.Eq(x => x.CollectionId, collectionId) & 
                    Builders<Image>.Filter.Eq(x => x.IsDeleted, false);
        var images = await _collection.Find(filter).ToListAsync(cancellationToken);
        return images.Sum(i => i.FileSize);
    }

    public async Task<int> GetCountByCollectionAsync(ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Image>.Filter.Eq(x => x.CollectionId, collectionId) & 
                    Builders<Image>.Filter.Eq(x => x.IsDeleted, false);
        return (int)await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task IncrementViewCountAsync(ObjectId imageId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Image>.Filter.Eq(x => x.Id, imageId);
        var update = Builders<Image>.Update.Inc(x => x.ViewCount, 1).Set(x => x.UpdatedAt, DateTime.UtcNow);
        await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Image>> GetByFormatAsync(string format, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Image>.Filter.Eq(x => x.Format, format) & 
                    Builders<Image>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Image>> GetBySizeRangeAsync(int minWidth, int minHeight, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Image>.Filter.And(
            Builders<Image>.Filter.Gte(x => x.Width, minWidth),
            Builders<Image>.Filter.Gte(x => x.Height, minHeight),
            Builders<Image>.Filter.Eq(x => x.IsDeleted, false)
        );
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Image>> GetLargeImagesAsync(long minSizeBytes, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Image>.Filter.And(
            Builders<Image>.Filter.Gte(x => x.FileSize, minSizeBytes),
            Builders<Image>.Filter.Eq(x => x.IsDeleted, false)
        );
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Image>> GetHighResolutionImagesAsync(int minWidth, int minHeight, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Image>.Filter.And(
            Builders<Image>.Filter.Gte(x => x.Width, minWidth),
            Builders<Image>.Filter.Gte(x => x.Height, minHeight),
            Builders<Image>.Filter.Eq(x => x.IsDeleted, false)
        );
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<Image?> GetRandomImageAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Image>.Filter.Eq(x => x.IsDeleted, false);
        var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        if (count == 0) return null;
        
        var random = new Random();
        var skip = random.Next(0, (int)count);
        
        return await _collection.Find(filter).Skip(skip).Limit(1).FirstOrDefaultAsync(cancellationToken);
    }
}