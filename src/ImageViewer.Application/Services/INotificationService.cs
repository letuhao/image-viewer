using MongoDB.Bson;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service interface for notification operations
/// </summary>
public interface INotificationService
{
    #region Notification Management
    
    Task<Notification> CreateNotificationAsync(CreateNotificationRequest request);
    Task<Notification> GetNotificationByIdAsync(ObjectId notificationId);
    Task<IEnumerable<Notification>> GetNotificationsByUserIdAsync(ObjectId userId, int page = 1, int pageSize = 20);
    Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(ObjectId userId);
    Task<Notification> MarkAsReadAsync(ObjectId notificationId);
    Task MarkAllAsReadAsync(ObjectId userId);
    Task DeleteNotificationAsync(ObjectId notificationId);
    Task DeleteAllNotificationsAsync(ObjectId userId);
    
    #endregion
    
    #region Real-time Notifications
    
    Task SendRealTimeNotificationAsync(ObjectId userId, NotificationMessage message);
    Task SendBroadcastNotificationAsync(NotificationMessage message);
    Task SendGroupNotificationAsync(List<ObjectId> userIds, NotificationMessage message);
    
    #endregion
    
    #region Notification Templates
    
    Task<NotificationTemplate> CreateTemplateAsync(CreateNotificationTemplateRequest request);
    Task<NotificationTemplate> GetTemplateByIdAsync(ObjectId templateId);
    Task<IEnumerable<NotificationTemplate>> GetTemplatesByTypeAsync(NotificationType type);
    Task<NotificationTemplate> UpdateTemplateAsync(ObjectId templateId, UpdateNotificationTemplateRequest request);
    Task DeleteTemplateAsync(ObjectId templateId);
    
    #endregion
    
    #region Notification Preferences
    
    Task<NotificationPreferences> GetUserPreferencesAsync(ObjectId userId);
    Task<NotificationPreferences> UpdateUserPreferencesAsync(ObjectId userId, UpdateNotificationPreferencesRequest request);
    Task<bool> IsNotificationEnabledAsync(ObjectId userId, NotificationType type);
    
    #endregion
    
    #region Notification Analytics
    
    Task<NotificationAnalytics> GetNotificationAnalyticsAsync(ObjectId? userId = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<NotificationStatistic>> GetNotificationStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    
    #endregion
}

/// <summary>
/// Request model for creating a notification
/// </summary>
public class CreateNotificationRequest
{
    public ObjectId UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public DateTime? ScheduledFor { get; set; }
    public TimeSpan? ExpiresAfter { get; set; }
}

/// <summary>
/// Request model for creating a notification template
/// </summary>
public class CreateNotificationTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ActionUrlTemplate { get; set; }
    public List<string> RequiredVariables { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request model for updating a notification template
/// </summary>
public class UpdateNotificationTemplateRequest
{
    public string? Name { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? ActionUrlTemplate { get; set; }
    public List<string>? RequiredVariables { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Request model for updating notification preferences
/// </summary>
public class UpdateNotificationPreferencesRequest
{
    public bool EmailEnabled { get; set; } = true;
    public bool PushEnabled { get; set; } = true;
    public bool InAppEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = false;
    public Dictionary<NotificationType, bool> TypePreferences { get; set; } = new();
    public TimeSpan QuietHoursStart { get; set; } = TimeSpan.FromHours(22);
    public TimeSpan QuietHoursEnd { get; set; } = TimeSpan.FromHours(8);
    public bool QuietHoursEnabled { get; set; } = true;
    public List<DayOfWeek> QuietDays { get; set; } = new();
}

/// <summary>
/// Notification message for real-time delivery
/// </summary>
public class NotificationMessage
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public string? ActionUrl { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Notification entity
/// </summary>
public class Notification
{
    public ObjectId Id { get; set; }
    public ObjectId UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public NotificationPriority Priority { get; set; }
    public NotificationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<NotificationDelivery> Deliveries { get; set; } = new();
}

/// <summary>
/// Notification template entity
/// </summary>
public class NotificationTemplate
{
    public ObjectId Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ActionUrlTemplate { get; set; }
    public List<string> RequiredVariables { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ObjectId CreatedBy { get; set; }
}

/// <summary>
/// Notification preferences entity
/// </summary>
public class NotificationPreferences
{
    public ObjectId Id { get; set; }
    public ObjectId UserId { get; set; }
    public bool EmailEnabled { get; set; }
    public bool PushEnabled { get; set; }
    public bool InAppEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public Dictionary<NotificationType, bool> TypePreferences { get; set; } = new();
    public TimeSpan QuietHoursStart { get; set; }
    public TimeSpan QuietHoursEnd { get; set; }
    public bool QuietHoursEnabled { get; set; }
    public List<DayOfWeek> QuietDays { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Notification delivery tracking
/// </summary>
public class NotificationDelivery
{
    public NotificationDeliveryMethod Method { get; set; }
    public NotificationDeliveryStatus Status { get; set; }
    public DateTime AttemptedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}

/// <summary>
/// Notification analytics
/// </summary>
public class NotificationAnalytics
{
    public ObjectId? UserId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public long TotalSent { get; set; }
    public long TotalDelivered { get; set; }
    public long TotalRead { get; set; }
    public long TotalClicked { get; set; }
    public double DeliveryRate { get; set; }
    public double ReadRate { get; set; }
    public double ClickThroughRate { get; set; }
    public Dictionary<NotificationType, long> SentByType { get; set; } = new();
    public Dictionary<NotificationDeliveryMethod, long> SentByMethod { get; set; } = new();
    public List<NotificationStatistic> DailyStatistics { get; set; } = new();
}

/// <summary>
/// Notification statistic
/// </summary>
public class NotificationStatistic
{
    public DateTime Date { get; set; }
    public NotificationType Type { get; set; }
    public NotificationDeliveryMethod Method { get; set; }
    public long Sent { get; set; }
    public long Delivered { get; set; }
    public long Read { get; set; }
    public long Clicked { get; set; }
    public double DeliveryRate { get; set; }
    public double ReadRate { get; set; }
    public double ClickThroughRate { get; set; }
}

/// <summary>
/// Enums
/// </summary>
public enum NotificationType
{
    System,
    User,
    Collection,
    MediaItem,
    Library,
    Comment,
    Like,
    Follow,
    Share,
    Download,
    Upload,
    Error,
    Warning,
    Info,
    Success
}

public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Urgent
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Delivered,
    Read,
    Failed,
    Expired
}

public enum NotificationDeliveryMethod
{
    InApp,
    Email,
    Push,
    Sms,
    Webhook
}

public enum NotificationDeliveryStatus
{
    Pending,
    Sent,
    Delivered,
    Failed,
    Bounced,
    Unsubscribed
}
