using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for NotificationQueue entity
/// </summary>
public interface INotificationQueueRepository : IRepository<NotificationQueue>
{
    Task<IEnumerable<NotificationQueue>> GetPendingNotificationsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationQueue>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationQueue>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationQueue>> GetByChannelAsync(string channel, CancellationToken cancellationToken = default);
}