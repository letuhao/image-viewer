using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Exceptions;
using ImageViewer.Domain.DTOs;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB collection repository implementation
/// </summary>
public class MongoCollectionRepository : MongoRepository<Collection>, ICollectionRepository
{
    public MongoCollectionRepository(IMongoDatabase database) : base(database, "collections")
    {
    }

    public async Task<Collection?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.Name, name) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Collection> GetByPathAsync(string path)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.Path, path) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        var result = await _collection.Find(filter).FirstOrDefaultAsync();
        return result ?? throw new EntityNotFoundException($"Collection with path '{path}' not found");
    }

    public async Task<IEnumerable<Collection>> GetByLibraryIdAsync(ObjectId libraryId)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.LibraryId, libraryId) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<Collection>> GetActiveCollectionsAsync()
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsActive, true) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByTypeAsync(CollectionType type)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.Type, type) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<long> GetCollectionCountAsync()
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.CountDocumentsAsync(filter);
    }

    public async Task<long> GetActiveCollectionCountAsync()
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsActive, true) & 
                    Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.CountDocumentsAsync(filter);
    }

    public async Task<IEnumerable<Collection>> SearchCollectionsAsync(string query)
    {
        var filter = Builders<Collection>.Filter.Or(
            Builders<Collection>.Filter.Regex(x => x.Name, new BsonRegularExpression(query, "i")),
            Builders<Collection>.Filter.Regex(x => x.Path, new BsonRegularExpression(query, "i"))
        ) & Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByFilterAsync(CollectionFilter filter)
    {
        var mongoFilter = Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        
        if (filter.Type.HasValue)
            mongoFilter &= Builders<Collection>.Filter.Eq(x => x.Type, filter.Type.Value);
        
        if (filter.IsActive.HasValue)
            mongoFilter &= Builders<Collection>.Filter.Eq(x => x.IsActive, filter.IsActive.Value);
        
        if (!string.IsNullOrEmpty(filter.Name))
            mongoFilter &= Builders<Collection>.Filter.Regex(x => x.Name, new BsonRegularExpression(filter.Name, "i"));
        
        return await _collection.Find(mongoFilter).ToListAsync();
    }

    public async Task<Domain.ValueObjects.CollectionStatistics> GetCollectionStatisticsAsync()
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        var collections = await _collection.Find(filter).ToListAsync();
        
        return new Domain.ValueObjects.CollectionStatistics
        {
            TotalCollections = collections.Count,
            ActiveCollections = collections.Count(c => c.IsActive),
            TotalImages = collections.Sum(c => c.GetImageCount()),
            TotalSize = collections.Sum(c => c.GetTotalSize()),
            AverageImagesPerCollection = collections.Count > 0 ? (double)collections.Sum(c => c.GetImageCount()) / collections.Count : 0,
            AverageSizePerCollection = collections.Count > 0 ? (double)collections.Sum(c => c.GetTotalSize()) / collections.Count : 0
        };
    }

    public async Task<IEnumerable<Collection>> GetTopCollectionsByActivityAsync(int limit = 10)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter)
            .Sort(Builders<Collection>.Sort.Descending(x => x.Statistics.LastViewed))
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Collection>> GetRecentCollectionsAsync(int limit = 10)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter)
            .Sort(Builders<Collection>.Sort.Descending(x => x.CreatedAt))
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<(int totalImages, int cachedImages, long totalCacheSize, int collectionsWithCache)> GetCacheStatisticsAsync()
    {
        // Use MongoDB aggregation pipeline for optimal performance (10-100x faster than client-side iteration)
        // Updated to use Collection.CacheImages array instead of deprecated Image.CacheInfo
        var pipeline = new BsonDocument[]
        {
            // Stage 1: Match non-deleted collections
            new BsonDocument("$match", new BsonDocument("isDeleted", false)),
            
            // Stage 2: Project to prepare data for statistics
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", 1 },
                { "images", 1 },
                { "cacheImages", 1 }
            }),
            
            // Stage 3: Add computed field for image statistics
            new BsonDocument("$addFields", new BsonDocument
            {
                { "activeImages", new BsonDocument("$filter", new BsonDocument
                    {
                        { "input", "$images" },
                        { "as", "img" },
                        { "cond", new BsonDocument("$eq", new BsonArray { "$$img.isDeleted", false }) }
                    })
                }
            }),
            
            // Stage 4: Unwind active images array
            new BsonDocument("$unwind", new BsonDocument
            {
                { "path", "$activeImages" },
                { "preserveNullAndEmptyArrays", false }
            }),
            
            // Stage 5: Add lookup to check if image has cache entry in CacheImages array
            new BsonDocument("$addFields", new BsonDocument
            {
                { "hasCacheEntry", new BsonDocument("$in", new BsonArray 
                    { 
                        "$activeImages.id", 
                        new BsonDocument("$ifNull", new BsonArray { "$cacheImages.imageId", new BsonArray() })
                    })
                },
                { "cacheEntry", new BsonDocument("$arrayElemAt", new BsonArray
                    {
                        new BsonDocument("$filter", new BsonDocument
                        {
                            { "input", new BsonDocument("$ifNull", new BsonArray { "$cacheImages", new BsonArray() }) },
                            { "as", "cache" },
                            { "cond", new BsonDocument("$eq", new BsonArray { "$$cache.imageId", "$activeImages.id" }) }
                        }),
                        0
                    })
                }
            }),
            
            // Stage 6: Group and calculate final statistics
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "totalImages", new BsonDocument("$sum", 1) },
                { "cachedImages", new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray 
                    { 
                        "$hasCacheEntry",
                        1, 
                        0 
                    })) 
                },
                { "totalCacheSize", new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$cacheEntry.fileSize", 0 })) },
                { "collectionsWithCache", new BsonDocument("$addToSet", new BsonDocument("$cond", new BsonArray
                    {
                        new BsonDocument("$and", new BsonArray
                        {
                            new BsonDocument("$ne", new BsonArray { "$cacheImages", BsonNull.Value }),
                            new BsonDocument("$gt", new BsonArray { new BsonDocument("$size", new BsonDocument("$ifNull", new BsonArray { "$cacheImages", new BsonArray() })), 0 })
                        }),
                        "$_id",
                        BsonNull.Value
                    }))
                }
            }),
            
            // Stage 7: Project final results
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", 0 },
                { "totalImages", 1 },
                { "cachedImages", 1 },
                { "totalCacheSize", 1 },
                { "collectionsWithCache", new BsonDocument("$size", new BsonDocument("$filter", new BsonDocument
                    {
                        { "input", "$collectionsWithCache" },
                        { "cond", new BsonDocument("$ne", new BsonArray { "$$this", BsonNull.Value }) }
                    }))
                }
            })
        };

        var result = await _collection.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
        
        if (result == null)
        {
            return (0, 0, 0, 0);
        }

        return (
            totalImages: result.GetValue("totalImages", 0).ToInt32(),
            cachedImages: result.GetValue("cachedImages", 0).ToInt32(),
            totalCacheSize: result.GetValue("totalCacheSize", 0).ToInt64(),
            collectionsWithCache: result.GetValue("collectionsWithCache", 0).ToInt32()
        );
    }

    #region Atomic Array Operations

    public async Task<bool> AtomicAddImageAsync(ObjectId collectionId, Domain.ValueObjects.ImageEmbedded image)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.Id, collectionId);
        var update = Builders<Collection>.Update
            .Push(x => x.Images, image)
            .Inc(x => x.Statistics.TotalItems, 1)
            .Inc(x => x.Statistics.TotalSize, image.FileSize)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        
        var result = await _collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> AtomicAddThumbnailAsync(ObjectId collectionId, Domain.ValueObjects.ThumbnailEmbedded thumbnail)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.Id, collectionId);
        var update = Builders<Collection>.Update
            .Push(x => x.Thumbnails, thumbnail)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        
        var result = await _collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> AtomicAddThumbnailsAsync(ObjectId collectionId, IEnumerable<Domain.ValueObjects.ThumbnailEmbedded> thumbnails)
    {
        var thumbnailList = thumbnails.ToList();
        if (!thumbnailList.Any())
        {
            return true; // Nothing to add
        }

        var filter = Builders<Collection>.Filter.Eq(x => x.Id, collectionId);
        var update = Builders<Collection>.Update
            .PushEach(x => x.Thumbnails, thumbnailList)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        
        var result = await _collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> AtomicAddCacheImagesAsync(ObjectId collectionId, IEnumerable<Domain.ValueObjects.CacheImageEmbedded> cacheImages)
    {
        var cacheImageList = cacheImages.ToList();
        if (!cacheImageList.Any())
        {
            return true; // Nothing to add
        }

        var filter = Builders<Collection>.Filter.Eq(x => x.Id, collectionId);
        var update = Builders<Collection>.Update
            .PushEach(x => x.CacheImages, cacheImageList)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        
        var result = await _collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> AtomicAddCacheImageAsync(ObjectId collectionId, Domain.ValueObjects.CacheImageEmbedded cacheImage)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.Id, collectionId);
        var update = Builders<Collection>.Update
            .Push(x => x.CacheImages, cacheImage)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        
        var result = await _collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task ClearImageArraysAsync(ObjectId collectionId)
    {
        var filter = Builders<Collection>.Filter.Eq(x => x.Id, collectionId);
        var update = Builders<Collection>.Update
            .Set(x => x.Images, new List<Domain.ValueObjects.ImageEmbedded>())
            .Set(x => x.Thumbnails, new List<Domain.ValueObjects.ThumbnailEmbedded>())
            .Set(x => x.CacheImages, new List<Domain.ValueObjects.CacheImageEmbedded>())
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        
        await _collection.UpdateOneAsync(filter, update);
    }

    public async Task RecalculateCollectionStatisticsAsync(ObjectId collectionId)
    {
        var collection = await GetByIdAsync(collectionId);
        if (collection == null) return;

        var activeImages = collection.GetActiveImages();
        var totalItems = activeImages.Count;
        var totalSize = activeImages.Sum(i => i.FileSize);

        var filter = Builders<Collection>.Filter.Eq(x => x.Id, collectionId);
        var update = Builders<Collection>.Update
            .Set(x => x.Statistics.TotalItems, totalItems)
            .Set(x => x.Statistics.TotalSize, totalSize)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _collection.UpdateOneAsync(filter, update);
    }

    public async Task RecalculateAllCollectionStatisticsAsync()
    {
        var collections = await GetAllAsync();
        foreach (var collection in collections)
        {
            await RecalculateCollectionStatisticsAsync(collection.Id);
        }
    }

    public async Task<SystemStatisticsDto> GetSystemStatisticsAsync()
    {
        // Use MongoDB aggregation pipeline to calculate statistics efficiently at database level
        // This avoids loading all collections and their embedded arrays into memory
        var pipeline = new[]
        {
            // Match only active, non-deleted collections
            new BsonDocument("$match", new BsonDocument
            {
                { "isActive", true },
                { "isDeleted", false }
            }),
            
            // Project only the statistics fields we need
            new BsonDocument("$project", new BsonDocument
            {
                { "statistics.totalItems", 1 },
                { "statistics.totalSize", 1 },
                { "statistics.totalThumbnails", 1 },
                { "statistics.totalThumbnailSize", 1 },
                { "statistics.totalCacheFiles", 1 },
                { "statistics.totalCacheSize", 1 }
            }),
            
            // Group and calculate totals
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "totalCollections", new BsonDocument("$sum", 1) },
                { "totalImages", new BsonDocument("$sum", "$statistics.totalItems") },
                { "totalSize", new BsonDocument("$sum", "$statistics.totalSize") },
                { "totalThumbnails", new BsonDocument("$sum", "$statistics.totalThumbnails") },
                { "totalThumbnailSize", new BsonDocument("$sum", "$statistics.totalThumbnailSize") },
                { "totalCacheFiles", new BsonDocument("$sum", "$statistics.totalCacheFiles") },
                { "totalCacheSize", new BsonDocument("$sum", "$statistics.totalCacheSize") }
            })
        };

        var result = await _collection.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
        
        if (result == null)
        {
            return new SystemStatisticsDto
            {
                TotalCollections = 0,
                TotalImages = 0,
                TotalSize = 0,
                TotalCacheSize = 0,
                TotalViewSessions = 0,
                TotalViewTime = 0,
                AverageImagesPerCollection = 0,
                AverageViewTimePerSession = 0
            };
        }

        return new SystemStatisticsDto
        {
            TotalCollections = result.GetValue("totalCollections", 0).AsInt32,
            TotalImages = result.GetValue("totalImages", 0).AsInt64,
            TotalSize = result.GetValue("totalSize", 0).AsInt64,
            TotalCacheSize = result.GetValue("totalCacheSize", 0).AsInt64,
            TotalViewSessions = 0, // Will be calculated separately for view sessions
            TotalViewTime = 0,     // Will be calculated separately for view sessions
            AverageImagesPerCollection = 0, // Will be calculated in the service
            AverageViewTimePerSession = 0    // Will be calculated in the service
        };
    }

    #endregion
}