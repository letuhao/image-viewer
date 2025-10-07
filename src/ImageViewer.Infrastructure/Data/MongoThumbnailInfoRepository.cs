using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB thumbnail info repository implementation
/// </summary>
public class MongoThumbnailInfoRepository : MongoRepository<ThumbnailInfo>, IThumbnailInfoRepository
{
    public MongoThumbnailInfoRepository(IMongoDatabase database) : base(database, "thumbnail_info")
    {
        // Create indexes for better performance
        CreateIndexes();
    }

    private void CreateIndexes()
    {
        try
        {
            // Index for ImageId lookups
            var imageIdIndex = Builders<ThumbnailInfo>.IndexKeys.Ascending(x => x.ImageId);
            var imageIdIndexModel = new CreateIndexModel<ThumbnailInfo>(imageIdIndex, new CreateIndexOptions
            {
                Name = "idx_imageId",
                Background = true
            });

            // Compound index for ImageId + Dimensions lookups
            var compoundIndex = Builders<ThumbnailInfo>.IndexKeys
                .Ascending(x => x.ImageId)
                .Ascending(x => x.Width)
                .Ascending(x => x.Height);
            var compoundIndexModel = new CreateIndexModel<ThumbnailInfo>(compoundIndex, new CreateIndexOptions
            {
                Name = "idx_imageId_dimensions",
                Background = true
            });

            // Index for expiration cleanup
            var expirationIndex = Builders<ThumbnailInfo>.IndexKeys.Ascending(x => x.ExpiresAt);
            var expirationIndexModel = new CreateIndexModel<ThumbnailInfo>(expirationIndex, new CreateIndexOptions
            {
                Name = "idx_expiresAt",
                Background = true
            });

            // Index for soft delete filtering
            var softDeleteIndex = Builders<ThumbnailInfo>.IndexKeys.Ascending(x => x.IsDeleted);
            var softDeleteIndexModel = new CreateIndexModel<ThumbnailInfo>(softDeleteIndex, new CreateIndexOptions
            {
                Name = "idx_isDeleted",
                Background = true
            });

            _collection.Indexes.CreateMany(new[] { imageIdIndexModel, compoundIndexModel, expirationIndexModel, softDeleteIndexModel });
        }
        catch (Exception ex)
        {
            // Log error but don't fail - indexes might already exist
            Console.WriteLine($"Warning: Failed to create indexes for ThumbnailInfo: {ex.Message}");
        }
    }

    public async Task<ThumbnailInfo?> GetByImageIdAsync(ObjectId imageId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ThumbnailInfo>.Filter.Eq(x => x.ImageId, imageId) & 
                    Builders<ThumbnailInfo>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ThumbnailInfo?> GetByImageIdAndDimensionsAsync(ObjectId imageId, int width, int height, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ThumbnailInfo>.Filter.And(
            Builders<ThumbnailInfo>.Filter.Eq(x => x.ImageId, imageId),
            Builders<ThumbnailInfo>.Filter.Eq(x => x.Width, width),
            Builders<ThumbnailInfo>.Filter.Eq(x => x.Height, height),
            Builders<ThumbnailInfo>.Filter.Eq(x => x.IsDeleted, false)
        );
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<ThumbnailInfo>> GetByImageIdsAsync(IEnumerable<ObjectId> imageIds, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ThumbnailInfo>.Filter.In(x => x.ImageId, imageIds) & 
                    Builders<ThumbnailInfo>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ThumbnailInfo>> GetExpiredThumbnailsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<ThumbnailInfo>.Filter.Lt(x => x.ExpiresAt, DateTime.UtcNow) & 
                    Builders<ThumbnailInfo>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ThumbnailInfo>> GetStaleThumbnailsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow - maxAge;
        var filter = Builders<ThumbnailInfo>.Filter.Lt(x => x.GeneratedAt, cutoffDate) & 
                    Builders<ThumbnailInfo>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<long> GetTotalSizeAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<ThumbnailInfo>.Filter.Eq(x => x.IsDeleted, false);
        var pipeline = new[]
        {
            new BsonDocument("$match", filter.ToBsonDocument()),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "totalSize", new BsonDocument("$sum", "$FileSizeBytes") }
            })
        };

        var result = await _collection.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync(cancellationToken);
        return result?["totalSize"]?.AsInt64 ?? 0;
    }

    public async Task<int> GetCountByImageIdAsync(ObjectId imageId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ThumbnailInfo>.Filter.Eq(x => x.ImageId, imageId) & 
                    Builders<ThumbnailInfo>.Filter.Eq(x => x.IsDeleted, false);
        return (int)await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task DeleteByImageIdAsync(ObjectId imageId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ThumbnailInfo>.Filter.Eq(x => x.ImageId, imageId);
        var update = Builders<ThumbnailInfo>.Update
            .Set(x => x.IsDeleted, true);
        
        await _collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task DeleteExpiredThumbnailsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<ThumbnailInfo>.Filter.Lt(x => x.ExpiresAt, DateTime.UtcNow);
        var update = Builders<ThumbnailInfo>.Update
            .Set(x => x.IsDeleted, true);
        
        await _collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
    }
}
