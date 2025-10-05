using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for UserActivityLog entity
/// </summary>
public interface IUserActivityLogRepository : IRepository<UserActivityLog>
{
    /// <summary>
    /// Get activity logs by user ID
    /// </summary>
    Task<IEnumerable<UserActivityLog>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get activity logs by activity type
    /// </summary>
    Task<IEnumerable<UserActivityLog>> GetByActivityTypeAsync(string activityType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get activity logs by session ID
    /// </summary>
    Task<IEnumerable<UserActivityLog>> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get activity logs by date range
    /// </summary>
    Task<IEnumerable<UserActivityLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get activity logs by IP address
    /// </summary>
    Task<IEnumerable<UserActivityLog>> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get activity logs by user agent
    /// </summary>
    Task<IEnumerable<UserActivityLog>> GetByUserAgentAsync(string userAgent, CancellationToken cancellationToken = default);
}
