using MongoDB.Driver;
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

    public async Task<IEnumerable<Image>> GetByCollectionIdAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Image>.Filter.Eq(x => x.CollectionId, collectionId) & 
                    Builders<Image>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
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
            Builders<Image>.Filter.Eq(x => x.IsDeleted, false),
            Builders<Image>.Filter.Gte(x => x.Width, minWidth),
            Builders<Image>.Filter.Gte(x => x.Height, minHeight)
        );
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Image>> GetMostViewedAsync(int count, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Image>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter)
            .SortByDescending(x => x.ViewCount)
            .Limit(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Image>> GetRecentlyViewedAsync(int count, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Image>.Filter.And(
            Builders<Image>.Filter.Eq(x => x.IsDeleted, false),
            Builders<Image>.Filter.Gt(x => x.ViewCount, 0)
        );
        return await _collection.Find(filter)
            .SortByDescending(x => x.UpdatedAt)
            .Limit(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetTotalSizeByCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Image>.Filter.Eq(x => x.CollectionId, collectionId) & 
                    Builders<Image>.Filter.Eq(x => x.IsDeleted, false);
        var images = await _collection.Find(filter).ToListAsync(cancellationToken);
        return images.Sum(i => i.FileSize);
    }

    public async Task<int> GetCountByCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Image>.Filter.Eq(x => x.CollectionId, collectionId) & 
                    Builders<Image>.Filter.Eq(x => x.IsDeleted, false);
        return (int)await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task<Image?> GetByCollectionIdAndFilenameAsync(Guid collectionId, string filename, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Image>.Filter.And(
            Builders<Image>.Filter.Eq(x => x.CollectionId, collectionId),
            Builders<Image>.Filter.Eq(x => x.Filename, filename),
            Builders<Image>.Filter.Eq(x => x.IsDeleted, false)
        );
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Image>> GetLargeImagesAsync(long minSizeBytes, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Image>.Filter.And(
            Builders<Image>.Filter.Eq(x => x.IsDeleted, false),
            Builders<Image>.Filter.Gte(x => x.FileSize, minSizeBytes)
        );
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Image>> GetHighResolutionImagesAsync(int minWidth, int minHeight, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Image>.Filter.And(
            Builders<Image>.Filter.Eq(x => x.IsDeleted, false),
            Builders<Image>.Filter.Gte(x => x.Width, minWidth),
            Builders<Image>.Filter.Gte(x => x.Height, minHeight)
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
        return await _collection.Find(filter).Skip(skip).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Image?> GetRandomImageByCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Image>.Filter.And(
            Builders<Image>.Filter.Eq(x => x.CollectionId, collectionId),
            Builders<Image>.Filter.Eq(x => x.IsDeleted, false)
        );
        var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        if (count == 0) return null;
        
        var random = new Random();
        var skip = random.Next(0, (int)count);
        return await _collection.Find(filter).Skip(skip).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Image?> GetNextImageAsync(Guid currentImageId, CancellationToken cancellationToken = default)
    {
        var currentImage = await GetByIdAsync(currentImageId, cancellationToken);
        if (currentImage == null) return null;

        var filter = Builders<Image>.Filter.And(
            Builders<Image>.Filter.Eq(x => x.CollectionId, currentImage.CollectionId),
            Builders<Image>.Filter.Eq(x => x.IsDeleted, false),
            Builders<Image>.Filter.Gt(x => x.CreatedAt, currentImage.CreatedAt)
        );
        return await _collection.Find(filter)
            .SortBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Image?> GetPreviousImageAsync(Guid currentImageId, CancellationToken cancellationToken = default)
    {
        var currentImage = await GetByIdAsync(currentImageId, cancellationToken);
        if (currentImage == null) return null;

        var filter = Builders<Image>.Filter.And(
            Builders<Image>.Filter.Eq(x => x.CollectionId, currentImage.CollectionId),
            Builders<Image>.Filter.Eq(x => x.IsDeleted, false),
            Builders<Image>.Filter.Lt(x => x.CreatedAt, currentImage.CreatedAt)
        );
        return await _collection.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task IncrementViewCountAsync(Guid imageId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Image>.Filter.Eq(x => x.Id, imageId);
        var update = Builders<Image>.Update.Inc(x => x.ViewCount, 1).Set(x => x.UpdatedAt, DateTime.UtcNow);
        await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }
}
