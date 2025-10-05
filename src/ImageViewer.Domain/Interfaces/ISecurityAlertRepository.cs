using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for SecurityAlert entity
/// </summary>
public interface ISecurityAlertRepository : IRepository<SecurityAlert>
{
    Task<IEnumerable<SecurityAlert>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SecurityAlert>> GetByAlertTypeAsync(SecurityAlertType alertType, CancellationToken cancellationToken = default);
    Task<IEnumerable<SecurityAlert>> GetBySeverityAsync(string severity, CancellationToken cancellationToken = default);
    Task<IEnumerable<SecurityAlert>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<SecurityAlert>> GetUnreadAlertsAsync(ObjectId userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SecurityAlert>> GetHighPriorityAlertsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SecurityAlert>> GetExpiredAlertsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SecurityAlert>> GetAlertsRequiringActionAsync(CancellationToken cancellationToken = default);
}
