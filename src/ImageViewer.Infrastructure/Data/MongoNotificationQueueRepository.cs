using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoNotificationQueueRepository : MongoRepository<NotificationQueue>, INotificationQueueRepository
{
    public MongoNotificationQueueRepository(IMongoDatabase database, ILogger<MongoNotificationQueueRepository> logger)
        : base(database.GetCollection<NotificationQueue>("notificationQueue"), logger)
    {
    }

    public async Task<IEnumerable<NotificationQueue>> GetPendingNotificationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(notification => notification.Status == "Pending")
                .SortBy(notification => notification.Priority)
                .ThenBy(notification => notification.ScheduledFor)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get pending notifications");
            throw;
        }
    }

    public async Task<IEnumerable<NotificationQueue>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(notification => notification.UserId == userId)
                .SortByDescending(notification => notification.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get notifications for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<NotificationQueue>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(notification => notification.Status == status)
                .SortByDescending(notification => notification.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get notifications for status {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<NotificationQueue>> GetByChannelAsync(string channel, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(notification => notification.NotificationType == channel)
                .SortByDescending(notification => notification.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get notifications for channel {Channel}", channel);
            throw;
        }
    }
}
