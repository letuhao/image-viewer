using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoBackupHistoryRepository : MongoRepository<BackupHistory>, IBackupHistoryRepository
{
    public MongoBackupHistoryRepository(IMongoDatabase database, ILogger<MongoBackupHistoryRepository> logger)
        : base(database.GetCollection<BackupHistory>("backupHistory"), logger)
    {
    }

    public async Task<IEnumerable<BackupHistory>> GetByBackupTypeAsync(string backupType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(backup => backup.BackupType == backupType)
                .SortByDescending(backup => backup.StartTime)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get backup history for type {BackupType}", backupType);
            throw;
        }
    }

    public async Task<IEnumerable<BackupHistory>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(backup => backup.Status == status)
                .SortByDescending(backup => backup.StartTime)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get backup history for status {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<BackupHistory>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<BackupHistory>.Filter.And(
                Builders<BackupHistory>.Filter.Gte(backup => backup.StartTime, startDate),
                Builders<BackupHistory>.Filter.Lte(backup => backup.StartTime, endDate)
            );

            return await _collection.Find(filter)
                .SortByDescending(backup => backup.StartTime)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get backup history for date range {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }

    public async Task<IEnumerable<BackupHistory>> GetSuccessfulBackupsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(backup => backup.Status == "Success")
                .SortByDescending(backup => backup.StartTime)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get successful backup history");
            throw;
        }
    }

    public async Task<IEnumerable<BackupHistory>> GetFailedBackupsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(backup => backup.Status == "Failed")
                .SortByDescending(backup => backup.StartTime)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get failed backup history");
            throw;
        }
    }
}
