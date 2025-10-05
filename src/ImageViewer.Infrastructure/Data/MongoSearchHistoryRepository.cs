using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB repository implementation for SearchHistory entity
/// </summary>
public class MongoSearchHistoryRepository : MongoRepository<SearchHistory>, ISearchHistoryRepository
{
    public MongoSearchHistoryRepository(IMongoDatabase database, ILogger<MongoSearchHistoryRepository> logger) 
        : base(database.GetCollection<SearchHistory>("searchHistory"), logger)
    {
    }

    public async Task<IEnumerable<SearchHistory>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(history => history.UserId == userId)
                .SortByDescending(history => history.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get search history for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<SearchHistory>> GetBySearchTypeAsync(string searchType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(history => history.SearchType == searchType)
                .SortByDescending(history => history.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get search history for type {SearchType}", searchType);
            throw;
        }
    }

    public async Task<IEnumerable<SearchHistory>> GetByQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(history => history.SearchQuery.ToLower().Contains(query.ToLower()))
                .SortByDescending(history => history.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get search history for query {Query}", query);
            throw;
        }
    }

    public async Task<IEnumerable<SearchHistory>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<SearchHistory>.Filter.And(
                Builders<SearchHistory>.Filter.Gte(history => history.CreatedAt, startDate),
                Builders<SearchHistory>.Filter.Lte(history => history.CreatedAt, endDate)
            );
            
            return await _collection.Find(filter)
                .SortByDescending(history => history.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get search history for date range {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }

    public async Task<IEnumerable<SearchHistory>> GetPopularQueriesAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            // For now, return recent search queries since we can't easily reconstruct SearchHistory objects
            // This could be improved with a proper aggregation pipeline that returns SearchHistory objects
            return await _collection.Find(_ => true)
                .SortByDescending(history => history.CreatedAt)
                .Limit(limit)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get popular search queries");
            throw;
        }
    }
}
