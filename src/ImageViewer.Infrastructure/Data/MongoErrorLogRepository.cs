using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB repository implementation for ErrorLog entity
/// </summary>
public class MongoErrorLogRepository : MongoRepository<ErrorLog>, IErrorLogRepository
{
    public MongoErrorLogRepository(IMongoDatabase database, ILogger<MongoErrorLogRepository> logger) 
        : base(database.GetCollection<ErrorLog>("errorLogs"), logger)
    {
    }

    public async Task<IEnumerable<ErrorLog>> GetByTypeAsync(string errorType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(log => log.ErrorType == errorType)
                .SortByDescending(log => log.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get error logs for type {ErrorType}", errorType);
            throw;
        }
    }

    public async Task<IEnumerable<ErrorLog>> GetBySeverityAsync(string severity, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(log => log.Severity == severity)
                .SortByDescending(log => log.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get error logs for severity {Severity}", severity);
            throw;
        }
    }

    public async Task<IEnumerable<ErrorLog>> GetUnresolvedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(log => log.IsResolved == false)
                .SortByDescending(log => log.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get unresolved error logs");
            throw;
        }
    }

    public async Task<IEnumerable<ErrorLog>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(log => log.UserId == userId)
                .SortByDescending(log => log.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get error logs for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<ErrorLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<ErrorLog>.Filter.And(
                Builders<ErrorLog>.Filter.Gte(log => log.CreatedAt, startDate),
                Builders<ErrorLog>.Filter.Lte(log => log.CreatedAt, endDate)
            );
            
            return await _collection.Find(filter)
                .SortByDescending(log => log.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get error logs for date range {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }

    public async Task<IEnumerable<ErrorLog>> GetByResolutionStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(log => log.IsResolved == (status == "resolved"))
                .SortByDescending(log => log.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get error logs for resolution status {Status}", status);
            throw;
        }
    }
}
