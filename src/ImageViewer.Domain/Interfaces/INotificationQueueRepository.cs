using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for NotificationQueue entity
/// </summary>
public interface INotificationQueueRepository : IRepository<NotificationQueue>
{
    /// <summary>
    /// Get notifications by user ID
    /// </summary>
    Task<IEnumerable<NotificationQueue>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get notifications by status
    /// </summary>
    Task<IEnumerable<NotificationQueue>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get notifications by type
    /// </summary>
    Task<IEnumerable<NotificationQueue>> GetByTypeAsync(string type, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get pending notifications
    /// </summary>
    Task<IEnumerable<NotificationQueue>> GetPendingAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get notifications by priority
    /// </summary>
    Task<IEnumerable<NotificationQueue>> GetByPriorityAsync(string priority, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get notifications by date range
    /// </summary>
    Task<IEnumerable<NotificationQueue>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
