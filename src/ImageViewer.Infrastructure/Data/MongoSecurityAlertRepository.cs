using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoSecurityAlertRepository : MongoRepository<SecurityAlert>, ISecurityAlertRepository
{
    public MongoSecurityAlertRepository(IMongoDatabase database, ILogger<MongoSecurityAlertRepository> logger)
        : base(database.GetCollection<SecurityAlert>("securityAlerts"), logger)
    {
    }

    public async Task<IEnumerable<SecurityAlert>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(alert => alert.UserId == userId)
                .SortByDescending(alert => alert.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get security alerts for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<SecurityAlert>> GetByAlertTypeAsync(SecurityAlertType alertType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(alert => alert.AlertType == alertType)
                .SortByDescending(alert => alert.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get security alerts for type {AlertType}", alertType);
            throw;
        }
    }

    public async Task<IEnumerable<SecurityAlert>> GetBySeverityAsync(string severity, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(alert => alert.Severity == severity)
                .SortByDescending(alert => alert.Priority)
                .ThenByDescending(alert => alert.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get security alerts for severity {Severity}", severity);
            throw;
        }
    }

    public async Task<IEnumerable<SecurityAlert>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<SecurityAlert>.Filter.And(
                Builders<SecurityAlert>.Filter.Gte(alert => alert.CreatedAt, startDate),
                Builders<SecurityAlert>.Filter.Lte(alert => alert.CreatedAt, endDate)
            );

            return await _collection.Find(filter)
                .SortByDescending(alert => alert.Priority)
                .ThenByDescending(alert => alert.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get security alerts for date range {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }

    public async Task<IEnumerable<SecurityAlert>> GetUnreadAlertsAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(alert => alert.UserId == userId && !alert.IsRead)
                .SortByDescending(alert => alert.Priority)
                .ThenByDescending(alert => alert.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get unread security alerts for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<SecurityAlert>> GetHighPriorityAlertsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(alert => alert.Priority >= 2)
                .SortByDescending(alert => alert.Priority)
                .ThenByDescending(alert => alert.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get high priority security alerts");
            throw;
        }
    }

    public async Task<IEnumerable<SecurityAlert>> GetExpiredAlertsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(alert => alert.ExpiresAt.HasValue && alert.ExpiresAt.Value <= DateTime.UtcNow)
                .SortByDescending(alert => alert.ExpiresAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get expired security alerts");
            throw;
        }
    }

    public async Task<IEnumerable<SecurityAlert>> GetAlertsRequiringActionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(alert => alert.ActionRequired && !alert.IsResolved)
                .SortByDescending(alert => alert.Priority)
                .ThenByDescending(alert => alert.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get security alerts requiring action");
            throw;
        }
    }
}
