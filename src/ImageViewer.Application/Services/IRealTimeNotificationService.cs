using MongoDB.Bson;
using ImageViewer.Application.Services;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service interface for real-time notification operations
/// </summary>
public interface IRealTimeNotificationService
{
    #region Connection Management
    
    /// <summary>
    /// Establishes a real-time connection for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="connectionId">Connection ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task ConnectAsync(ObjectId userId, string connectionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Disconnects a user from real-time notifications
    /// </summary>
    /// <param name="connectionId">Connection ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task DisconnectAsync(string connectionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all active connections for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active connection IDs</returns>
    Task<IEnumerable<string>> GetUserConnectionsAsync(ObjectId userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the user ID for a connection
    /// </summary>
    /// <param name="connectionId">Connection ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User ID or null if not found</returns>
    Task<ObjectId?> GetUserIdByConnectionAsync(string connectionId, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Real-time Delivery
    
    /// <summary>
    /// Sends a real-time notification to a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="message">Notification message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task SendToUserAsync(ObjectId userId, NotificationMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a real-time notification to a specific connection
    /// </summary>
    /// <param name="connectionId">Connection ID</param>
    /// <param name="message">Notification message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task SendToConnectionAsync(string connectionId, NotificationMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Broadcasts a real-time notification to all connected users
    /// </summary>
    /// <param name="message">Notification message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task BroadcastAsync(NotificationMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a real-time notification to a group of users
    /// </summary>
    /// <param name="userIds">List of user IDs</param>
    /// <param name="message">Notification message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task SendToGroupAsync(IEnumerable<ObjectId> userIds, NotificationMessage message, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region User Presence
    
    /// <summary>
    /// Updates user presence status
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="status">Presence status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task UpdateUserPresenceAsync(ObjectId userId, UserPresenceStatus status, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets user presence status
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User presence status</returns>
    Task<UserPresenceStatus> GetUserPresenceAsync(ObjectId userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all online users
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of online user IDs</returns>
    Task<IEnumerable<ObjectId>> GetOnlineUsersAsync(CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Notification History
    
    /// <summary>
    /// Gets real-time notification history for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="limit">Maximum number of notifications to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of real-time notifications</returns>
    Task<IEnumerable<RealTimeNotification>> GetNotificationHistoryAsync(ObjectId userId, int limit = 50, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Marks a real-time notification as read
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task MarkAsReadAsync(ObjectId notificationId, ObjectId userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clears notification history for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task ClearHistoryAsync(ObjectId userId, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Statistics
    
    /// <summary>
    /// Gets real-time notification statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Real-time notification statistics</returns>
    Task<RealTimeNotificationStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    
    #endregion
}

/// <summary>
/// Real-time notification entity
/// </summary>
public class RealTimeNotification
{
    public ObjectId Id { get; set; }
    public ObjectId UserId { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
    public NotificationMessage Message { get; set; } = new();
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// User presence status
/// </summary>
public enum UserPresenceStatus
{
    Offline,
    Online,
    Away,
    Busy,
    Invisible
}

/// <summary>
/// Real-time notification statistics
/// </summary>
public class RealTimeNotificationStatistics
{
    public int TotalConnections { get; set; }
    public int OnlineUsers { get; set; }
    public long NotificationsSent { get; set; }
    public long NotificationsDelivered { get; set; }
    public long NotificationsRead { get; set; }
    public double DeliveryRate { get; set; }
    public double ReadRate { get; set; }
    public Dictionary<UserPresenceStatus, int> PresenceDistribution { get; set; } = new();
    public Dictionary<NotificationType, long> NotificationsByType { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}
