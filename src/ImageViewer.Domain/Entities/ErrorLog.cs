using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Error log entity - represents system error logging and tracking
/// </summary>
public class ErrorLog : BaseEntity
{
    [BsonElement("userId")]
    public ObjectId? UserId { get; private set; }

    [BsonElement("errorType")]
    public string ErrorType { get; private set; } = string.Empty;

    [BsonElement("errorMessage")]
    public string ErrorMessage { get; private set; } = string.Empty;

    [BsonElement("stackTrace")]
    public string? StackTrace { get; private set; }

    [BsonElement("source")]
    public string Source { get; private set; } = string.Empty;

    [BsonElement("method")]
    public string? Method { get; private set; }

    [BsonElement("lineNumber")]
    public int? LineNumber { get; private set; }

    [BsonElement("severity")]
    public string Severity { get; private set; } = "Error"; // Info, Warning, Error, Critical, Fatal

    [BsonElement("category")]
    public string Category { get; private set; } = "Application"; // Application, Database, Network, Security, etc.

    [BsonElement("requestId")]
    public string? RequestId { get; private set; }

    [BsonElement("sessionId")]
    public string? SessionId { get; private set; }

    [BsonElement("ipAddress")]
    public string? IpAddress { get; private set; }

    [BsonElement("userAgent")]
    public string? UserAgent { get; private set; }

    [BsonElement("exceptionData")]
    public Dictionary<string, object> ExceptionData { get; private set; } = new();

    [BsonElement("isResolved")]
    public bool IsResolved { get; private set; } = false;

    [BsonElement("resolvedAt")]
    public DateTime? ResolvedAt { get; private set; }

    [BsonElement("resolvedBy")]
    public ObjectId? ResolvedBy { get; private set; }

    [BsonElement("resolutionNotes")]
    public string? ResolutionNotes { get; private set; }

    [BsonElement("occurrenceCount")]
    public int OccurrenceCount { get; private set; } = 1;

    [BsonElement("lastOccurrence")]
    public DateTime LastOccurrence { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public User? User { get; private set; }

    [BsonIgnore]
    public User? ResolvedByUser { get; private set; }

    // Private constructor for EF Core
    private ErrorLog() { }

    public static ErrorLog Create(string errorType, string errorMessage, string source, string? stackTrace = null, ObjectId? userId = null, string? method = null, int? lineNumber = null, string severity = "Error", string category = "Application")
    {
        if (string.IsNullOrWhiteSpace(errorType))
            throw new ArgumentException("Error type cannot be empty", nameof(errorType));

        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message cannot be empty", nameof(errorMessage));

        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be empty", nameof(source));

        return new ErrorLog
        {
            ErrorType = errorType,
            ErrorMessage = errorMessage,
            StackTrace = stackTrace,
            Source = source,
            Method = method,
            LineNumber = lineNumber,
            UserId = userId,
            Severity = severity,
            Category = category,
            IsResolved = false,
            OccurrenceCount = 1,
            LastOccurrence = DateTime.UtcNow,
            ExceptionData = new Dictionary<string, object>()
        };
    }

    public void IncrementOccurrence()
    {
        OccurrenceCount++;
        LastOccurrence = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void SetRequestInfo(string? requestId, string? sessionId, string? ipAddress, string? userAgent)
    {
        RequestId = requestId;
        SessionId = sessionId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        UpdateTimestamp();
    }

    public void AddExceptionData(string key, object value)
    {
        ExceptionData[key] = value;
        UpdateTimestamp();
    }

    public void Resolve(ObjectId resolvedBy, string? resolutionNotes = null)
    {
        IsResolved = true;
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = resolvedBy;
        ResolutionNotes = resolutionNotes;
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
