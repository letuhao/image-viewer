using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for SystemHealth entity
/// </summary>
public interface ISystemHealthRepository : IRepository<SystemHealth>
{
    Task<IEnumerable<SystemHealth>> GetByComponentAsync(string component, CancellationToken cancellationToken = default);
    Task<IEnumerable<SystemHealth>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<SystemHealth>> GetHealthyComponentsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SystemHealth>> GetUnhealthyComponentsAsync(CancellationToken cancellationToken = default);
    Task<SystemHealth?> GetLatestByComponentAsync(string component, CancellationToken cancellationToken = default);
    Task<IEnumerable<SystemHealth>> GetComponentsWithAlertsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SystemHealth>> GetComponentsWithCriticalAlertsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SystemHealth>> GetByHealthScoreRangeAsync(double minScore, double maxScore, CancellationToken cancellationToken = default);
}
