using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for BackupHistory entity
/// </summary>
public interface IBackupHistoryRepository : IRepository<BackupHistory>
{
    Task<IEnumerable<BackupHistory>> GetByBackupTypeAsync(string backupType, CancellationToken cancellationToken = default);
    Task<IEnumerable<BackupHistory>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<BackupHistory>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<BackupHistory>> GetSuccessfulBackupsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<BackupHistory>> GetFailedBackupsAsync(CancellationToken cancellationToken = default);
}
