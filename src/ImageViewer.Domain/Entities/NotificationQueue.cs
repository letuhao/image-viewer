using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Notification queue entity - represents queued notifications for delivery
/// </summary>
public class NotificationQueue : BaseEntity
{
    [BsonElement("userId")]
    public ObjectId UserId { get; private set; }

    [BsonElement("notificationType")]
    public string NotificationType { get; private set; } = string.Empty; // Email, SMS, Push, InApp

    [BsonElement("templateId")]
    public ObjectId? TemplateId { get; private set; }

    [BsonElement("subject")]
    public string Subject { get; private set; } = string.Empty;

    [BsonElement("content")]
    public string Content { get; private set; } = string.Empty;

    [BsonElement("priority")]
    public string Priority { get; private set; } = "Normal"; // Low, Normal, High, Urgent

    [BsonElement("status")]
    public string Status { get; private set; } = "Pending"; // Pending, Processing, Sent, Failed, Cancelled

    [BsonElement("scheduledFor")]
    public DateTime? ScheduledFor { get; private set; }

    [BsonElement("sentAt")]
    public DateTime? SentAt { get; private set; }

    [BsonElement("failedAt")]
    public DateTime? FailedAt { get; private set; }

    [BsonElement("retryCount")]
    public int RetryCount { get; private set; } = 0;

    [BsonElement("maxRetries")]
    public int MaxRetries { get; private set; } = 3;

    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; private set; }

    [BsonElement("recipientInfo")]
    public Dictionary<string, object> RecipientInfo { get; private set; } = new();

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("deliveryAttempts")]
    public List<DeliveryAttempt> DeliveryAttempts { get; private set; } = new();

    [BsonElement("expiresAt")]
    public DateTime? ExpiresAt { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public User User { get; private set; } = null!;

    [BsonIgnore]
    public NotificationTemplate? Template { get; private set; }

    // Private constructor for EF Core
    private NotificationQueue() { }

    public static NotificationQueue Create(ObjectId userId, string notificationType, string subject, string content, string priority = "Normal", ObjectId? templateId = null, DateTime? scheduledFor = null, DateTime? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(notificationType))
            throw new ArgumentException("Notification type cannot be empty", nameof(notificationType));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject cannot be empty", nameof(subject));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be empty", nameof(content));

        if (string.IsNullOrWhiteSpace(priority))
            throw new ArgumentException("Priority cannot be empty", nameof(priority));

        return new NotificationQueue
        {
            UserId = userId,
            NotificationType = notificationType,
            Subject = subject,
            Content = content,
            Priority = priority,
            TemplateId = templateId,
            Status = "Pending",
            ScheduledFor = scheduledFor,
            ExpiresAt = expiresAt,
            RetryCount = 0,
            MaxRetries = 3,
            RecipientInfo = new Dictionary<string, object>(),
            Metadata = new Dictionary<string, object>(),
            DeliveryAttempts = new List<DeliveryAttempt>()
        };
    }

    public void Process()
    {
        Status = "Processing";
        UpdateTimestamp();
    }

    public void MarkAsSent()
    {
        Status = "Sent";
        SentAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = "Failed";
        FailedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        RetryCount++;
        UpdateTimestamp();
    }

    public void Cancel()
    {
        Status = "Cancelled";
        UpdateTimestamp();
    }

    public void AddDeliveryAttempt(DeliveryAttempt attempt)
    {
        DeliveryAttempts.Add(attempt);
        UpdateTimestamp();
    }

    public void SetPriority(string priority)
    {
        if (string.IsNullOrWhiteSpace(priority))
            throw new ArgumentException("Priority cannot be empty", nameof(priority));

        Priority = priority;
        UpdateTimestamp();
    }

    public void ScheduleFor(DateTime scheduledTime)
    {
        ScheduledFor = scheduledTime;
        UpdateTimestamp();
    }

    public void SetExpiration(DateTime expirationTime)
    {
        ExpiresAt = expirationTime;
        UpdateTimestamp();
    }

    public void AddRecipientInfo(string key, object value)
    {
        RecipientInfo[key] = value;
        UpdateTimestamp();
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }

    public bool CanRetry()
    {
        return RetryCount < MaxRetries && Status == "Failed" && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);
    }

    public void IncrementRetry()
    {
        if (CanRetry())
        {
            RetryCount++;
            Status = "Pending";
            UpdateTimestamp();
        }
    }
}

/// <summary>
/// Delivery attempt record for notification queue
/// </summary>
public class DeliveryAttempt
{
    [BsonElement("attemptedAt")]
    public DateTime AttemptedAt { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = string.Empty; // Success, Failed, Timeout

    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; set; }

    [BsonElement("responseCode")]
    public int? ResponseCode { get; set; }

    [BsonElement("responseTime")]
    public long? ResponseTimeMs { get; set; }

    public static DeliveryAttempt Create(string status, string? errorMessage = null, int? responseCode = null, long? responseTimeMs = null)
    {
        return new DeliveryAttempt
        {
            AttemptedAt = DateTime.UtcNow,
            Status = status,
            ErrorMessage = errorMessage,
            ResponseCode = responseCode,
            ResponseTimeMs = responseTimeMs
        };
    }
}
