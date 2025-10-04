using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB repository implementation for Library
/// </summary>
public class LibraryRepository : MongoRepository<Library>, ILibraryRepository
{
    public LibraryRepository(IMongoCollection<Library> collection, ILogger<LibraryRepository> logger)
        : base(collection, logger)
    {
    }

    public async Task<Library> GetByPathAsync(string path)
    {
        try
        {
            return await _collection.Find(l => l.Path == path).FirstOrDefaultAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get library by path {Path}", path);
            throw new RepositoryException($"Failed to get library by path {path}", ex);
        }
    }

    public async Task<IEnumerable<Library>> GetByOwnerIdAsync(ObjectId ownerId)
    {
        try
        {
            return await _collection.Find(l => l.OwnerId == ownerId).ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get libraries by owner ID {OwnerId}", ownerId);
            throw new RepositoryException($"Failed to get libraries by owner ID {ownerId}", ex);
        }
    }

    public async Task<IEnumerable<Library>> GetPublicLibrariesAsync()
    {
        try
        {
            return await _collection.Find(l => l.IsPublic).ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get public libraries");
            throw new RepositoryException("Failed to get public libraries", ex);
        }
    }

    public async Task<IEnumerable<Library>> GetActiveLibrariesAsync()
    {
        try
        {
            return await _collection.Find(l => l.IsActive).ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get active libraries");
            throw new RepositoryException("Failed to get active libraries", ex);
        }
    }

    public async Task<long> GetLibraryCountAsync()
    {
        try
        {
            return await _collection.CountDocumentsAsync(_ => true);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get library count");
            throw new RepositoryException("Failed to get library count", ex);
        }
    }

    public async Task<long> GetActiveLibraryCountAsync()
    {
        try
        {
            return await _collection.CountDocumentsAsync(l => l.IsActive);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get active library count");
            throw new RepositoryException("Failed to get active library count", ex);
        }
    }

    public async Task<IEnumerable<Library>> SearchLibrariesAsync(string query)
    {
        try
        {
            var filter = Builders<Library>.Filter.Or(
                Builders<Library>.Filter.Regex(l => l.Name, new BsonRegularExpression(query, "i")),
                Builders<Library>.Filter.Regex(l => l.Description, new BsonRegularExpression(query, "i")),
                Builders<Library>.Filter.Regex(l => l.Path, new BsonRegularExpression(query, "i"))
            );
            
            return await _collection.Find(filter).ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to search libraries with query {Query}", query);
            throw new RepositoryException($"Failed to search libraries with query {query}", ex);
        }
    }

    public async Task<IEnumerable<Library>> GetLibrariesByFilterAsync(LibraryFilter filter)
    {
        try
        {
            var builder = Builders<Library>.Filter;
            var filters = new List<FilterDefinition<Library>>();

            if (filter.OwnerId.HasValue)
            {
                filters.Add(builder.Eq(l => l.OwnerId, filter.OwnerId.Value));
            }

            if (filter.IsPublic.HasValue)
            {
                filters.Add(builder.Eq(l => l.IsPublic, filter.IsPublic.Value));
            }

            if (filter.IsActive.HasValue)
            {
                filters.Add(builder.Eq(l => l.IsActive, filter.IsActive.Value));
            }

            if (filter.CreatedAfter.HasValue)
            {
                filters.Add(builder.Gte(l => l.CreatedAt, filter.CreatedAfter.Value));
            }

            if (filter.CreatedBefore.HasValue)
            {
                filters.Add(builder.Lte(l => l.CreatedAt, filter.CreatedBefore.Value));
            }

            if (filter.Path != null)
            {
                filters.Add(builder.Eq(l => l.Path, filter.Path));
            }

            if (filter.Tags != null && filter.Tags.Any())
            {
                filters.Add(builder.In(l => l.Metadata.Tags, filter.Tags));
            }

            if (filter.Categories != null && filter.Categories.Any())
            {
                filters.Add(builder.In(l => l.Metadata.Categories, filter.Categories));
            }

            var combinedFilter = filters.Any() ? builder.And(filters) : builder.Empty;
            return await _collection.Find(combinedFilter).ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get libraries by filter");
            throw new RepositoryException("Failed to get libraries by filter", ex);
        }
    }

    public async Task<LibraryStatistics> GetLibraryStatisticsAsync()
    {
        try
        {
            var totalLibraries = await _collection.CountDocumentsAsync(_ => true);
            var activeLibraries = await _collection.CountDocumentsAsync(l => l.IsActive);
            var publicLibraries = await _collection.CountDocumentsAsync(l => l.IsPublic);
            
            var now = DateTime.UtcNow;
            var newLibrariesThisMonth = await _collection.CountDocumentsAsync(l => l.CreatedAt >= now.AddMonths(-1));
            var newLibrariesThisWeek = await _collection.CountDocumentsAsync(l => l.CreatedAt >= now.AddDays(-7));
            var newLibrariesToday = await _collection.CountDocumentsAsync(l => l.CreatedAt >= now.AddDays(-1));

            return new LibraryStatistics
            {
                TotalLibraries = totalLibraries,
                ActiveLibraries = activeLibraries,
                PublicLibraries = publicLibraries,
                NewLibrariesThisMonth = newLibrariesThisMonth,
                NewLibrariesThisWeek = newLibrariesThisWeek,
                NewLibrariesToday = newLibrariesToday
            };
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get library statistics");
            throw new RepositoryException("Failed to get library statistics", ex);
        }
    }

    public async Task<IEnumerable<Library>> GetTopLibrariesByActivityAsync(int limit = 10)
    {
        try
        {
            return await _collection.Find(_ => true)
                .SortByDescending(l => l.Statistics.LastActivity)
                .Limit(limit)
                .ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get top libraries by activity");
            throw new RepositoryException("Failed to get top libraries by activity", ex);
        }
    }

    public async Task<IEnumerable<Library>> GetRecentLibrariesAsync(int limit = 10)
    {
        try
        {
            return await _collection.Find(_ => true)
                .SortByDescending(l => l.CreatedAt)
                .Limit(limit)
                .ToListAsync();
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get recent libraries");
            throw new RepositoryException("Failed to get recent libraries", ex);
        }
    }
}
