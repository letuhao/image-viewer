using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoSystemMaintenanceRepository : MongoRepository<SystemMaintenance>, ISystemMaintenanceRepository
{
    public MongoSystemMaintenanceRepository(IMongoDatabase database, ILogger<MongoSystemMaintenanceRepository> logger)
        : base(database.GetCollection<SystemMaintenance>("systemMaintenance"), logger)
    {
    }

    public async Task<IEnumerable<SystemMaintenance>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(maintenance => maintenance.Status == status)
                .SortByDescending(maintenance => maintenance.ScheduledStart)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get system maintenance for status {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<SystemMaintenance>> GetByTypeAsync(string maintenanceType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(maintenance => maintenance.Type == maintenanceType)
                .SortByDescending(maintenance => maintenance.ScheduledStart)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get system maintenance for type {MaintenanceType}", maintenanceType);
            throw;
        }
    }

    public async Task<IEnumerable<SystemMaintenance>> GetScheduledAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _collection.Find(maintenance => 
                maintenance.Status == "Scheduled" && 
                maintenance.ScheduledStart > now)
                .SortBy(maintenance => maintenance.ScheduledStart)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get scheduled system maintenance");
            throw;
        }
    }

    public async Task<IEnumerable<SystemMaintenance>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<SystemMaintenance>.Filter.And(
                Builders<SystemMaintenance>.Filter.Gte(maintenance => maintenance.ScheduledStart, startDate),
                Builders<SystemMaintenance>.Filter.Lte(maintenance => maintenance.ScheduledStart, endDate)
            );

            return await _collection.Find(filter)
                .SortByDescending(maintenance => maintenance.ScheduledStart)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get system maintenance for date range {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }
}
