using MongoDB.Bson;
using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB implementation of SystemHealth repository
/// </summary>
public class MongoSystemHealthRepository : MongoRepository<SystemHealth>, ISystemHealthRepository
{
    public MongoSystemHealthRepository(MongoDbContext context, ILogger<MongoSystemHealthRepository> logger) 
        : base(context.SystemHealth, logger)
    {
    }

    public async Task<IEnumerable<SystemHealth>> GetByComponentAsync(string component, CancellationToken cancellationToken = default)
    {
        var filter = Builders<SystemHealth>.Filter.Eq(h => h.Component, component);
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SystemHealth>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        var filter = Builders<SystemHealth>.Filter.Eq(h => h.Status, status);
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SystemHealth>> GetHealthyComponentsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<SystemHealth>.Filter.Eq(h => h.Status, "healthy");
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SystemHealth>> GetUnhealthyComponentsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<SystemHealth>.Filter.In(h => h.Status, new[] { "unhealthy", "degraded" });
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task<SystemHealth?> GetLatestByComponentAsync(string component, CancellationToken cancellationToken = default)
    {
        var filter = Builders<SystemHealth>.Filter.Eq(h => h.Component, component);
        var sort = Builders<SystemHealth>.Sort.Descending(h => h.LastCheck);
        var cursor = await _collection.FindAsync(filter, new FindOptions<SystemHealth> { Sort = sort }, cancellationToken: cancellationToken);
        return await cursor.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<SystemHealth>> GetComponentsWithAlertsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<SystemHealth>.Filter.Exists("alerts.0"); // Has at least one alert
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SystemHealth>> GetComponentsWithCriticalAlertsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<SystemHealth>.Filter.ElemMatch(h => h.Alerts, 
            Builders<HealthAlert>.Filter.And(
                Builders<HealthAlert>.Filter.Eq(a => a.Severity, "critical"),
                Builders<HealthAlert>.Filter.Eq(a => a.Status, "active")
            ));
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SystemHealth>> GetByHealthScoreRangeAsync(double minScore, double maxScore, CancellationToken cancellationToken = default)
    {
        var filter = Builders<SystemHealth>.Filter.And(
            Builders<SystemHealth>.Filter.Gte(h => h.HealthScore, minScore),
            Builders<SystemHealth>.Filter.Lte(h => h.HealthScore, maxScore)
        );
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }
}
