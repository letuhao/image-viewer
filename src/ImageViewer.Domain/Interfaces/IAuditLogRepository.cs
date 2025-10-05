using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for AuditLog entity
/// </summary>
public interface IAuditLogRepository : IRepository<AuditLog>
{
    /// <summary>
    /// Get audit logs by user ID
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get audit logs by action type
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByActionAsync(string action, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get audit logs by resource type and ID
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByResourceAsync(string resourceType, ObjectId resourceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get audit logs by date range
    /// </summary>
    Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get audit logs by severity level
    /// </summary>
    Task<IEnumerable<AuditLog>> GetBySeverityAsync(string severity, CancellationToken cancellationToken = default);
}
