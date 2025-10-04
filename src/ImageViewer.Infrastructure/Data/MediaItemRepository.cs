using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB repository implementation for MediaItem
/// </summary>
public class MediaItemRepository : MongoRepository<MediaItem>, IMediaItemRepository
{
    public MediaItemRepository(IMongoCollection<MediaItem> collection, ILogger<MediaItemRepository> logger)
        : base(collection, logger)
    {
    }

    public async Task<MediaItem> GetByPathAsync(string path)
    {
        try
        {
            return await _collection.Find(m => m.Path == path).FirstOrDefaultAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get media item by path {Path}", path);
            throw new RepositoryException($"Failed to get media item by path {path}", ex);
        }
    }

    public async Task<IEnumerable<MediaItem>> GetByCollectionIdAsync(ObjectId collectionId)
    {
        try
        {
            return await _collection.Find(m => m.CollectionId == collectionId).ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get media items by collection ID {CollectionId}", collectionId);
            throw new RepositoryException($"Failed to get media items by collection ID {collectionId}", ex);
        }
    }

    public async Task<IEnumerable<MediaItem>> GetActiveMediaItemsAsync()
    {
        try
        {
            return await _collection.Find(m => m.IsActive).ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get active media items");
            throw new RepositoryException("Failed to get active media items", ex);
        }
    }

    public async Task<IEnumerable<MediaItem>> GetMediaItemsByTypeAsync(string type)
    {
        try
        {
            return await _collection.Find(m => m.Type == type).ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get media items by type {Type}", type);
            throw new RepositoryException($"Failed to get media items by type {type}", ex);
        }
    }

    public async Task<IEnumerable<MediaItem>> GetMediaItemsByFormatAsync(string format)
    {
        try
        {
            return await _collection.Find(m => m.Format == format).ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get media items by format {Format}", format);
            throw new RepositoryException($"Failed to get media items by format {format}", ex);
        }
    }

    public async Task<long> GetMediaItemCountAsync()
    {
        try
        {
            return await _collection.CountDocumentsAsync(_ => true);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get media item count");
            throw new RepositoryException("Failed to get media item count", ex);
        }
    }

    public async Task<long> GetActiveMediaItemCountAsync()
    {
        try
        {
            return await _collection.CountDocumentsAsync(m => m.IsActive);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get active media item count");
            throw new RepositoryException("Failed to get active media item count", ex);
        }
    }

    public async Task<IEnumerable<MediaItem>> SearchMediaItemsAsync(string query)
    {
        try
        {
            var filter = Builders<MediaItem>.Filter.Or(
                Builders<MediaItem>.Filter.Regex(m => m.Name, new BsonRegularExpression(query, "i")),
                Builders<MediaItem>.Filter.Regex(m => m.Filename, new BsonRegularExpression(query, "i")),
                Builders<MediaItem>.Filter.Regex(m => m.Path, new BsonRegularExpression(query, "i"))
            );
            
            return await _collection.Find(filter).ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to search media items with query {Query}", query);
            throw new RepositoryException($"Failed to search media items with query {query}", ex);
        }
    }

    public async Task<IEnumerable<MediaItem>> GetMediaItemsByFilterAsync(MediaItemFilter filter)
    {
        try
        {
            var builder = Builders<MediaItem>.Filter;
            var filters = new List<FilterDefinition<MediaItem>>();

            if (filter.CollectionId.HasValue)
            {
                filters.Add(builder.Eq(m => m.CollectionId, filter.CollectionId.Value));
            }

            if (filter.Type != null)
            {
                filters.Add(builder.Eq(m => m.Type, filter.Type));
            }

            if (filter.Format != null)
            {
                filters.Add(builder.Eq(m => m.Format, filter.Format));
            }

            if (filter.IsActive.HasValue)
            {
                filters.Add(builder.Eq(m => m.IsActive, filter.IsActive.Value));
            }

            if (filter.CreatedAfter.HasValue)
            {
                filters.Add(builder.Gte(m => m.CreatedAt, filter.CreatedAfter.Value));
            }

            if (filter.CreatedBefore.HasValue)
            {
                filters.Add(builder.Lte(m => m.CreatedAt, filter.CreatedBefore.Value));
            }

            if (filter.Path != null)
            {
                filters.Add(builder.Eq(m => m.Path, filter.Path));
            }

            if (filter.Tags != null && filter.Tags.Any())
            {
                filters.Add(builder.In(m => m.Metadata.Tags, filter.Tags));
            }

            if (filter.Categories != null && filter.Categories.Any())
            {
                filters.Add(builder.In(m => m.Metadata.Categories, filter.Categories));
            }

            if (filter.MinWidth.HasValue)
            {
                filters.Add(builder.Gte(m => m.Width, filter.MinWidth.Value));
            }

            if (filter.MaxWidth.HasValue)
            {
                filters.Add(builder.Lte(m => m.Width, filter.MaxWidth.Value));
            }

            if (filter.MinHeight.HasValue)
            {
                filters.Add(builder.Gte(m => m.Height, filter.MinHeight.Value));
            }

            if (filter.MaxHeight.HasValue)
            {
                filters.Add(builder.Lte(m => m.Height, filter.MaxHeight.Value));
            }

            if (filter.MinFileSize.HasValue)
            {
                filters.Add(builder.Gte(m => m.FileSize, filter.MinFileSize.Value));
            }

            if (filter.MaxFileSize.HasValue)
            {
                filters.Add(builder.Lte(m => m.FileSize, filter.MaxFileSize.Value));
            }

            var combinedFilter = filters.Any() ? builder.And(filters) : builder.Empty;
            return await _collection.Find(combinedFilter).ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get media items by filter");
            throw new RepositoryException("Failed to get media items by filter", ex);
        }
    }

    public async Task<MediaItemStatistics> GetMediaItemStatisticsAsync()
    {
        try
        {
            var totalMediaItems = await _collection.CountDocumentsAsync(_ => true);
            var activeMediaItems = await _collection.CountDocumentsAsync(m => m.IsActive);
            
            var now = DateTime.UtcNow;
            var newMediaItemsThisMonth = await _collection.CountDocumentsAsync(m => m.CreatedAt >= now.AddMonths(-1));
            var newMediaItemsThisWeek = await _collection.CountDocumentsAsync(m => m.CreatedAt >= now.AddDays(-7));
            var newMediaItemsToday = await _collection.CountDocumentsAsync(m => m.CreatedAt >= now.AddDays(-1));

            // Get total file size
            var pipeline = new[]
            {
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", BsonNull.Value },
                    { "totalSize", new BsonDocument("$sum", "$fileSize") },
                    { "count", new BsonDocument("$sum", 1) }
                })
            };

            var result = await _collection.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
            var totalFileSize = result?["totalSize"]?.AsInt64 ?? 0;
            var count = result?["count"]?.AsInt32 ?? 0;
            var averageFileSize = count > 0 ? (double)totalFileSize / count : 0;

            return new MediaItemStatistics
            {
                TotalMediaItems = totalMediaItems,
                ActiveMediaItems = activeMediaItems,
                NewMediaItemsThisMonth = newMediaItemsThisMonth,
                NewMediaItemsThisWeek = newMediaItemsThisWeek,
                NewMediaItemsToday = newMediaItemsToday,
                TotalFileSize = totalFileSize,
                AverageFileSize = averageFileSize
            };
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get media item statistics");
            throw new RepositoryException("Failed to get media item statistics", ex);
        }
    }

    public async Task<IEnumerable<MediaItem>> GetTopMediaItemsByActivityAsync(int limit = 10)
    {
        try
        {
            return await _collection.Find(_ => true)
                .SortByDescending(m => m.Statistics.LastViewed)
                .Limit(limit)
                .ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get top media items by activity");
            throw new RepositoryException("Failed to get top media items by activity", ex);
        }
    }

    public async Task<IEnumerable<MediaItem>> GetRecentMediaItemsAsync(int limit = 10)
    {
        try
        {
            return await _collection.Find(_ => true)
                .SortByDescending(m => m.CreatedAt)
                .Limit(limit)
                .ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get recent media items");
            throw new RepositoryException("Failed to get recent media items", ex);
        }
    }
}
