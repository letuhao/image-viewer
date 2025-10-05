using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Audit log entity - represents system audit trail for security and compliance
/// </summary>
public class AuditLog : BaseEntity
{
    [BsonElement("userId")]
    public ObjectId? UserId { get; private set; }

    [BsonElement("action")]
    public string Action { get; private set; } = string.Empty;

    [BsonElement("entityType")]
    public string EntityType { get; private set; } = string.Empty;

    [BsonElement("entityId")]
    public ObjectId? EntityId { get; private set; }

    [BsonElement("oldValues")]
    public Dictionary<string, object>? OldValues { get; private set; }

    [BsonElement("newValues")]
    public Dictionary<string, object>? NewValues { get; private set; }

    [BsonElement("ipAddress")]
    public string? IpAddress { get; private set; }

    [BsonElement("userAgent")]
    public string? UserAgent { get; private set; }

    [BsonElement("sessionId")]
    public string? SessionId { get; private set; }

    [BsonElement("severity")]
    public string Severity { get; private set; } = "Info"; // Info, Warning, Error, Critical

    [BsonElement("category")]
    public string Category { get; private set; } = "General"; // Authentication, Authorization, DataAccess, System, etc.

    [BsonElement("description")]
    public string? Description { get; private set; }

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("isSuccessful")]
    public bool IsSuccessful { get; private set; } = true;

    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; private set; }

    // Private constructor for EF Core
    private AuditLog() { }

    public static AuditLog Create(string action, string entityType, ObjectId? userId = null, ObjectId? entityId = null, string? ipAddress = null, string? userAgent = null, string? sessionId = null, string severity = "Info", string category = "General")
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be empty", nameof(action));

        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("Entity type cannot be empty", nameof(entityType));

        return new AuditLog
        {
            Action = action,
            EntityType = entityType,
            UserId = userId,
            EntityId = entityId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SessionId = sessionId,
            Severity = severity,
            Category = category,
            IsSuccessful = true,
            Metadata = new Dictionary<string, object>()
        };
    }

    public void SetValues(Dictionary<string, object>? oldValues, Dictionary<string, object>? newValues)
    {
        OldValues = oldValues;
        NewValues = newValues;
        UpdateTimestamp();
    }

    public void SetDescription(string description)
    {
        Description = description;
        UpdateTimestamp();
    }

    public void SetError(string errorMessage)
    {
        IsSuccessful = false;
        ErrorMessage = errorMessage;
        Severity = "Error";
        UpdateTimestamp();
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }

    public void SetSeverity(string severity)
    {
        if (string.IsNullOrWhiteSpace(severity))
            throw new ArgumentException("Severity cannot be empty", nameof(severity));

        Severity = severity;
        UpdateTimestamp();
    }

    public void SetCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be empty", nameof(category));

        Category = category;
        UpdateTimestamp();
    }
}
