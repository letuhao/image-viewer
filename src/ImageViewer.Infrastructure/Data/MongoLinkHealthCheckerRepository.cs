using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoLinkHealthCheckerRepository : MongoRepository<LinkHealthChecker>, ILinkHealthCheckerRepository
{
    public MongoLinkHealthCheckerRepository(IMongoDatabase database, ILogger<MongoLinkHealthCheckerRepository> logger)
        : base(database.GetCollection<LinkHealthChecker>("linkHealthCheckers"), logger)
    {
    }

    public async Task<IEnumerable<LinkHealthChecker>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(checker => checker.Status == status)
                .SortByDescending(checker => checker.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get link health checkers for status {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<LinkHealthChecker>> GetByHealthStatusAsync(string healthStatus, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(checker => checker.Status == healthStatus)
                .SortByDescending(checker => checker.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get link health checkers for health status {HealthStatus}", healthStatus);
            throw;
        }
    }

    public async Task<IEnumerable<LinkHealthChecker>> GetByLinkIdAsync(ObjectId linkId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(checker => checker.Url.Contains(linkId.ToString()))
                .SortByDescending(checker => checker.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get link health checkers for link {LinkId}", linkId);
            throw;
        }
    }

    public async Task<IEnumerable<LinkHealthChecker>> GetUnhealthyLinksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(checker => checker.Status == "Error")
                .SortByDescending(checker => checker.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get unhealthy link health checkers");
            throw;
        }
    }

    public async Task<IEnumerable<LinkHealthChecker>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<LinkHealthChecker>.Filter.And(
                Builders<LinkHealthChecker>.Filter.Gte(checker => checker.CreatedAt, startDate),
                Builders<LinkHealthChecker>.Filter.Lte(checker => checker.CreatedAt, endDate)
            );

            return await _collection.Find(filter)
                .SortByDescending(checker => checker.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get link health checkers for date range {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }
}
