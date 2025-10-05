using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for ErrorLog entity
/// </summary>
public interface IErrorLogRepository : IRepository<ErrorLog>
{
    /// <summary>
    /// Get error logs by error type
    /// </summary>
    Task<IEnumerable<ErrorLog>> GetByTypeAsync(string errorType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get error logs by severity level
    /// </summary>
    Task<IEnumerable<ErrorLog>> GetBySeverityAsync(string severity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get unresolved error logs
    /// </summary>
    Task<IEnumerable<ErrorLog>> GetUnresolvedAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get error logs by user ID
    /// </summary>
    Task<IEnumerable<ErrorLog>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get error logs by date range
    /// </summary>
    Task<IEnumerable<ErrorLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get error logs by resolution status
    /// </summary>
    Task<IEnumerable<ErrorLog>> GetByResolutionStatusAsync(string status, CancellationToken cancellationToken = default);
}
