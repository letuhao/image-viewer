using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using ImageViewer.Application.Services;

namespace ImageViewer.Application.Services;

/// <summary>
/// Real-time notification service implementation
/// </summary>
public class RealTimeNotificationService : IRealTimeNotificationService
{
    private readonly ILogger<RealTimeNotificationService> _logger;
    private readonly Dictionary<string, ObjectId> _connections = new();
    private readonly Dictionary<ObjectId, HashSet<string>> _userConnections = new();
    private readonly Dictionary<ObjectId, UserPresenceStatus> _userPresence = new();
    private readonly List<RealTimeNotification> _notificationHistory = new();
    private readonly object _lock = new();

    public RealTimeNotificationService(ILogger<RealTimeNotificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Connection Management

    public async Task ConnectAsync(ObjectId userId, string connectionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(connectionId))
            throw new ArgumentException("Connection ID cannot be null or empty", nameof(connectionId));

        _logger.LogInformation("User {UserId} connecting with connection ID {ConnectionId}", userId, connectionId);

        lock (_lock)
        {
            _connections[connectionId] = userId;
            
            if (!_userConnections.ContainsKey(userId))
                _userConnections[userId] = new HashSet<string>();
            
            _userConnections[userId].Add(connectionId);
            
            // Set user as online when they connect
            _userPresence[userId] = UserPresenceStatus.Online;
        }

        _logger.LogInformation("User {UserId} connected successfully with connection ID {ConnectionId}", userId, connectionId);
        await Task.CompletedTask;
    }

    public async Task DisconnectAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(connectionId))
            throw new ArgumentException("Connection ID cannot be null or empty", nameof(connectionId));

        _logger.LogInformation("Disconnecting connection ID {ConnectionId}", connectionId);

        lock (_lock)
        {
            if (_connections.TryGetValue(connectionId, out var userId))
            {
                _connections.Remove(connectionId);
                
                if (_userConnections.ContainsKey(userId))
                {
                    _userConnections[userId].Remove(connectionId);
                    
                    // If user has no more connections, set them as offline
                    if (_userConnections[userId].Count == 0)
                    {
                        _userConnections.Remove(userId);
                        _userPresence[userId] = UserPresenceStatus.Offline;
                    }
                }
            }
        }

        _logger.LogInformation("Connection ID {ConnectionId} disconnected successfully", connectionId);
        await Task.CompletedTask;
    }

    public async Task<IEnumerable<string>> GetUserConnectionsAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                return connections.ToList();
            }
        }

        return Enumerable.Empty<string>();
    }

    public async Task<ObjectId?> GetUserIdByConnectionAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(connectionId))
            return null;

        lock (_lock)
        {
            if (_connections.TryGetValue(connectionId, out var userId))
            {
                return userId;
            }
            return null;
        }
    }

    #endregion

    #region Real-time Delivery

    public async Task SendToUserAsync(ObjectId userId, NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        _logger.LogInformation("Sending real-time notification to user {UserId}", userId);

        var connections = await GetUserConnectionsAsync(userId, cancellationToken);
        var connectionList = connections.ToList();

        if (!connectionList.Any())
        {
            _logger.LogWarning("No active connections found for user {UserId}", userId);
            return;
        }

        var tasks = connectionList.Select(connectionId => SendToConnectionAsync(connectionId, message, cancellationToken));
        await Task.WhenAll(tasks);

        _logger.LogInformation("Real-time notification sent to user {UserId} via {ConnectionCount} connections", userId, connectionList.Count);
    }

    public async Task SendToConnectionAsync(string connectionId, NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(connectionId))
            throw new ArgumentException("Connection ID cannot be null or empty", nameof(connectionId));
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        _logger.LogDebug("Sending real-time notification to connection {ConnectionId}", connectionId);

        var userId = await GetUserIdByConnectionAsync(connectionId, cancellationToken);
        if (userId == null)
        {
            _logger.LogWarning("No user found for connection ID {ConnectionId}", connectionId);
            return;
        }

        var notification = new RealTimeNotification
        {
            Id = ObjectId.GenerateNewId(),
            UserId = userId.Value,
            ConnectionId = connectionId,
            Message = message,
            IsRead = false,
            SentAt = DateTime.UtcNow
        };

        lock (_lock)
        {
            _notificationHistory.Add(notification);
        }

        _logger.LogDebug("Real-time notification sent to connection {ConnectionId}", connectionId);
        await Task.CompletedTask;
    }

    public async Task BroadcastAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        _logger.LogInformation("Broadcasting real-time notification to all connected users");

        var onlineUsers = await GetOnlineUsersAsync(cancellationToken);
        await SendToGroupAsync(onlineUsers, message, cancellationToken);

        _logger.LogInformation("Real-time notification broadcasted to {UserCount} online users", onlineUsers.Count());
    }

    public async Task SendToGroupAsync(IEnumerable<ObjectId> userIds, NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (userIds == null)
            throw new ArgumentNullException(nameof(userIds));
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var userIdList = userIds.ToList();
        _logger.LogInformation("Sending real-time notification to group of {UserCount} users", userIdList.Count);

        var tasks = userIdList.Select(userId => SendToUserAsync(userId, message, cancellationToken));
        await Task.WhenAll(tasks);

        _logger.LogInformation("Real-time notification sent to group of {UserCount} users", userIdList.Count);
    }

    #endregion

    #region User Presence

    public async Task UpdateUserPresenceAsync(ObjectId userId, UserPresenceStatus status, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating presence for user {UserId} to {Status}", userId, status);

        lock (_lock)
        {
            _userPresence[userId] = status;
        }

        _logger.LogDebug("Presence updated for user {UserId} to {Status}", userId, status);
        await Task.CompletedTask;
    }

    public async Task<UserPresenceStatus> GetUserPresenceAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return _userPresence.TryGetValue(userId, out var status) ? status : UserPresenceStatus.Offline;
        }
    }

    public async Task<IEnumerable<ObjectId>> GetOnlineUsersAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return _userPresence
                .Where(kvp => kvp.Value == UserPresenceStatus.Online)
                .Select(kvp => kvp.Key)
                .ToList();
        }
    }

    #endregion

    #region Notification History

    public async Task<IEnumerable<RealTimeNotification>> GetNotificationHistoryAsync(ObjectId userId, int limit = 50, CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
            limit = 50;

        lock (_lock)
        {
            return _notificationHistory
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.SentAt)
                .Take(limit)
                .ToList();
        }
    }

    public async Task MarkAsReadAsync(ObjectId notificationId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Marking notification {NotificationId} as read for user {UserId}", notificationId, userId);

        lock (_lock)
        {
            var notification = _notificationHistory.FirstOrDefault(n => n.Id == notificationId && n.UserId == userId);
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }
        }

        _logger.LogDebug("Notification {NotificationId} marked as read for user {UserId}", notificationId, userId);
        await Task.CompletedTask;
    }

    public async Task ClearHistoryAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing notification history for user {UserId}", userId);

        lock (_lock)
        {
            _notificationHistory.RemoveAll(n => n.UserId == userId);
        }

        _logger.LogInformation("Notification history cleared for user {UserId}", userId);
        await Task.CompletedTask;
    }

    #endregion

    #region Statistics

    public async Task<RealTimeNotificationStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var totalConnections = _connections.Count;
            var onlineUsers = _userPresence.Count(kvp => kvp.Value == UserPresenceStatus.Online);
            var notificationsSent = _notificationHistory.Count;
            var notificationsDelivered = _notificationHistory.Count(n => string.IsNullOrEmpty(n.ErrorMessage));
            var notificationsRead = _notificationHistory.Count(n => n.IsRead);

            var deliveryRate = notificationsSent > 0 ? (double)notificationsDelivered / notificationsSent * 100 : 0;
            var readRate = notificationsDelivered > 0 ? (double)notificationsRead / notificationsDelivered * 100 : 0;

            var presenceDistribution = _userPresence.Values
                .GroupBy(status => status)
                .ToDictionary(g => g.Key, g => g.Count());

            var notificationsByType = _notificationHistory
                .GroupBy(n => n.Message.Type)
                .ToDictionary(g => g.Key, g => (long)g.Count());

            return new RealTimeNotificationStatistics
            {
                TotalConnections = totalConnections,
                OnlineUsers = onlineUsers,
                NotificationsSent = notificationsSent,
                NotificationsDelivered = notificationsDelivered,
                NotificationsRead = notificationsRead,
                DeliveryRate = deliveryRate,
                ReadRate = readRate,
                PresenceDistribution = presenceDistribution,
                NotificationsByType = notificationsByType,
                LastUpdated = DateTime.UtcNow
            };
        }
    }

    #endregion
}
