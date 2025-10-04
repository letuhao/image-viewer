using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB repository implementation for Collection
/// </summary>
public class CollectionRepository : MongoRepository<Collection>, ICollectionRepository
{
    public CollectionRepository(IMongoCollection<Collection> collection, ILogger<CollectionRepository> logger)
        : base(collection, logger)
    {
    }

    public async Task<Collection> GetByPathAsync(string path)
    {
        try
        {
            return await _collection.Find(c => c.Path == path).FirstOrDefaultAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get collection by path {Path}", path);
            throw new RepositoryException($"Failed to get collection by path {path}", ex);
        }
    }

    public async Task<IEnumerable<Collection>> GetByLibraryIdAsync(ObjectId libraryId)
    {
        try
        {
            return await _collection.Find(c => c.LibraryId == libraryId).ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get collections by library ID {LibraryId}", libraryId);
            throw new RepositoryException($"Failed to get collections by library ID {libraryId}", ex);
        }
    }

    public async Task<IEnumerable<Collection>> GetActiveCollectionsAsync()
    {
        try
        {
            return await _collection.Find(c => c.IsActive).ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get active collections");
            throw new RepositoryException("Failed to get active collections", ex);
        }
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByTypeAsync(CollectionType type)
    {
        try
        {
            return await _collection.Find(c => c.Type == type).ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get collections by type {Type}", type);
            throw new RepositoryException($"Failed to get collections by type {type}", ex);
        }
    }

    public async Task<long> GetCollectionCountAsync()
    {
        try
        {
            return await _collection.CountDocumentsAsync(_ => true);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get collection count");
            throw new RepositoryException("Failed to get collection count", ex);
        }
    }

    public async Task<long> GetActiveCollectionCountAsync()
    {
        try
        {
            return await _collection.CountDocumentsAsync(c => c.IsActive);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get active collection count");
            throw new RepositoryException("Failed to get active collection count", ex);
        }
    }

    public async Task<IEnumerable<Collection>> SearchCollectionsAsync(string query)
    {
        try
        {
            var filter = Builders<Collection>.Filter.Or(
                Builders<Collection>.Filter.Regex(c => c.Name, new BsonRegularExpression(query, "i")),
                Builders<Collection>.Filter.Regex(c => c.Path, new BsonRegularExpression(query, "i"))
            );
            
            return await _collection.Find(filter).ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to search collections with query {Query}", query);
            throw new RepositoryException($"Failed to search collections with query {query}", ex);
        }
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByFilterAsync(CollectionFilter filter)
    {
        try
        {
            var builder = Builders<Collection>.Filter;
            var filters = new List<FilterDefinition<Collection>>();

            if (filter.LibraryId.HasValue)
            {
                filters.Add(builder.Eq(c => c.LibraryId, filter.LibraryId.Value));
            }

            if (filter.Type.HasValue)
            {
                filters.Add(builder.Eq(c => c.Type, filter.Type.Value));
            }

            if (filter.IsActive.HasValue)
            {
                filters.Add(builder.Eq(c => c.IsActive, filter.IsActive.Value));
            }

            if (filter.CreatedAfter.HasValue)
            {
                filters.Add(builder.Gte(c => c.CreatedAt, filter.CreatedAfter.Value));
            }

            if (filter.CreatedBefore.HasValue)
            {
                filters.Add(builder.Lte(c => c.CreatedAt, filter.CreatedBefore.Value));
            }

            if (filter.Path != null)
            {
                filters.Add(builder.Eq(c => c.Path, filter.Path));
            }

            if (filter.Tags != null && filter.Tags.Any())
            {
                filters.Add(builder.In(c => c.Metadata.Tags, filter.Tags));
            }

            if (filter.Categories != null && filter.Categories.Any())
            {
                filters.Add(builder.In(c => c.Metadata.Categories, filter.Categories));
            }

            var combinedFilter = filters.Any() ? builder.And(filters) : builder.Empty;
            return await _collection.Find(combinedFilter).ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get collections by filter");
            throw new RepositoryException("Failed to get collections by filter", ex);
        }
    }

    public async Task<CollectionStatistics> GetCollectionStatisticsAsync()
    {
        try
        {
            var totalCollections = await _collection.CountDocumentsAsync(_ => true);
            var activeCollections = await _collection.CountDocumentsAsync(c => c.IsActive);
            
            var now = DateTime.UtcNow;
            var newCollectionsThisMonth = await _collection.CountDocumentsAsync(c => c.CreatedAt >= now.AddMonths(-1));
            var newCollectionsThisWeek = await _collection.CountDocumentsAsync(c => c.CreatedAt >= now.AddDays(-7));
            var newCollectionsToday = await _collection.CountDocumentsAsync(c => c.CreatedAt >= now.AddDays(-1));

            return new CollectionStatistics
            {
                TotalCollections = totalCollections,
                ActiveCollections = activeCollections,
                NewCollectionsThisMonth = newCollectionsThisMonth,
                NewCollectionsThisWeek = newCollectionsThisWeek,
                NewCollectionsToday = newCollectionsToday
            };
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get collection statistics");
            throw new RepositoryException("Failed to get collection statistics", ex);
        }
    }

    public async Task<IEnumerable<Collection>> GetTopCollectionsByActivityAsync(int limit = 10)
    {
        try
        {
            return await _collection.Find(_ => true)
                .SortByDescending(c => c.Statistics.LastActivity)
                .Limit(limit)
                .ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get top collections by activity");
            throw new RepositoryException("Failed to get top collections by activity", ex);
        }
    }

    public async Task<IEnumerable<Collection>> GetRecentCollectionsAsync(int limit = 10)
    {
        try
        {
            return await _collection.Find(_ => true)
                .SortByDescending(c => c.CreatedAt)
                .Limit(limit)
                .ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get recent collections");
            throw new RepositoryException("Failed to get recent collections", ex);
        }
    }
}
