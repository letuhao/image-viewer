using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB repository implementation for AuditLog entity
/// </summary>
public class MongoAuditLogRepository : MongoRepository<AuditLog>, IAuditLogRepository
{
    public MongoAuditLogRepository(IMongoDatabase database, ILogger<MongoAuditLogRepository> logger) 
        : base(database.GetCollection<AuditLog>("auditLogs"), logger)
    {
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(log => log.UserId == userId)
                .SortByDescending(log => log.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get audit logs for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetByActionAsync(string action, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(log => log.Action == action)
                .SortByDescending(log => log.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get audit logs for action {Action}", action);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetByResourceAsync(string resourceType, ObjectId resourceId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(log => log.EntityType == resourceType && log.EntityId == resourceId)
                .SortByDescending(log => log.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get audit logs for resource {ResourceType} with ID {ResourceId}", resourceType, resourceId);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<AuditLog>.Filter.And(
                Builders<AuditLog>.Filter.Gte(log => log.CreatedAt, startDate),
                Builders<AuditLog>.Filter.Lte(log => log.CreatedAt, endDate)
            );
            
            return await _collection.Find(filter)
                .SortByDescending(log => log.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get audit logs for date range {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetBySeverityAsync(string severity, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(log => log.Severity == severity)
                .SortByDescending(log => log.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get audit logs for severity {Severity}", severity);
            throw;
        }
    }
}
