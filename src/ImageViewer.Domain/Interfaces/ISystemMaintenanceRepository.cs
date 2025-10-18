using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for SystemMaintenance entity
/// </summary>
public interface ISystemMaintenanceRepository : IRepository<SystemMaintenance>
{
    Task<IEnumerable<SystemMaintenance>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<SystemMaintenance>> GetByTypeAsync(string maintenanceType, CancellationToken cancellationToken = default);
    Task<IEnumerable<SystemMaintenance>> GetScheduledAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SystemMaintenance>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
