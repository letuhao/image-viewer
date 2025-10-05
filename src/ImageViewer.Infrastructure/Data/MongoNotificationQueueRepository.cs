using MongoDB.Bson;
using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB implementation of NotificationQueue repository
/// </summary>
public class MongoNotificationQueueRepository : MongoRepository<NotificationQueue>, INotificationQueueRepository
{
    public MongoNotificationQueueRepository(MongoDbContext context, ILogger<MongoNotificationQueueRepository> logger) 
        : base(context.NotificationQueue, logger)
    {
    }

    public async Task<IEnumerable<NotificationQueue>> GetPendingNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<NotificationQueue>.Filter.Eq(n => n.Status, "Pending");
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationQueue>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<NotificationQueue>.Filter.Eq(n => n.UserId, userId);
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationQueue>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        var filter = Builders<NotificationQueue>.Filter.Eq(n => n.Status, status);
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<NotificationQueue>> GetByChannelAsync(string channel, CancellationToken cancellationToken = default)
    {
        var filter = Builders<NotificationQueue>.Filter.Eq(n => n.NotificationType, channel);
        var cursor = await _collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }
}