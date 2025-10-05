using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Link health checker entity - represents link validation and health monitoring
/// </summary>
public class LinkHealthChecker : BaseEntity
{
    [BsonElement("url")]
    public string Url { get; private set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; private set; } = string.Empty; // HTTP, HTTPS, FTP, Magnet, Torrent

    [BsonElement("status")]
    public string Status { get; private set; } = "Unknown"; // Healthy, Warning, Error, Unknown

    [BsonElement("lastChecked")]
    public DateTime? LastChecked { get; private set; }

    [BsonElement("nextCheck")]
    public DateTime? NextCheck { get; private set; }

    [BsonElement("checkInterval")]
    public TimeSpan CheckInterval { get; private set; } = TimeSpan.FromHours(1);

    [BsonElement("responseTime")]
    public long? ResponseTimeMs { get; private set; }

    [BsonElement("httpStatusCode")]
    public int? HttpStatusCode { get; private set; }

    [BsonElement("contentLength")]
    public long? ContentLength { get; private set; }

    [BsonElement("contentType")]
    public string? ContentType { get; private set; }

    [BsonElement("isAccessible")]
    public bool IsAccessible { get; private set; } = false;

    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; private set; }

    [BsonElement("retryCount")]
    public int RetryCount { get; private set; } = 0;

    [BsonElement("maxRetries")]
    public int MaxRetries { get; private set; } = 3;

    [BsonElement("consecutiveFailures")]
    public int ConsecutiveFailures { get; private set; } = 0;

    [BsonElement("consecutiveSuccesses")]
    public int ConsecutiveSuccesses { get; private set; } = 0;

    [BsonElement("totalChecks")]
    public int TotalChecks { get; private set; } = 0;

    [BsonElement("successfulChecks")]
    public int SuccessfulChecks { get; private set; } = 0;

    [BsonElement("failedChecks")]
    public int FailedChecks { get; private set; } = 0;

    [BsonElement("lastSuccess")]
    public DateTime? LastSuccess { get; private set; }

    [BsonElement("lastFailure")]
    public DateTime? LastFailure { get; private set; }

    [BsonElement("isActive")]
    public bool IsActive { get; private set; } = true;

    [BsonElement("priority")]
    public int Priority { get; private set; } = 0;

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("checkHistory")]
    public List<HealthCheckResult> CheckHistory { get; private set; } = new();

    [BsonElement("relatedLinkId")]
    public ObjectId? RelatedLinkId { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public DownloadLink? RelatedLink { get; private set; }

    // Private constructor for EF Core
    private LinkHealthChecker() { }

    public static LinkHealthChecker Create(string url, string type, TimeSpan? checkInterval = null, int priority = 0, ObjectId? relatedLinkId = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty", nameof(url));

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type cannot be empty", nameof(type));

        var interval = checkInterval ?? TimeSpan.FromHours(1);

        return new LinkHealthChecker
        {
            Url = url,
            Type = type,
            Status = "Unknown",
            CheckInterval = interval,
            Priority = priority,
            RelatedLinkId = relatedLinkId,
            IsActive = true,
            RetryCount = 0,
            MaxRetries = 3,
            ConsecutiveFailures = 0,
            ConsecutiveSuccesses = 0,
            TotalChecks = 0,
            SuccessfulChecks = 0,
            FailedChecks = 0,
            Metadata = new Dictionary<string, object>(),
            CheckHistory = new List<HealthCheckResult>()
        };
    }

    public void RecordCheck(HealthCheckResult result)
    {
        TotalChecks++;
        LastChecked = DateTime.UtcNow;

        CheckHistory.Add(result);

        // Keep only last 100 results
        if (CheckHistory.Count > 100)
        {
            CheckHistory.RemoveAt(0);
        }

        if (result.IsSuccess)
        {
            IsAccessible = true;
            Status = "Healthy";
            SuccessfulChecks++;
            ConsecutiveSuccesses++;
            ConsecutiveFailures = 0;
            LastSuccess = DateTime.UtcNow;
            ErrorMessage = null;
            RetryCount = 0;
        }
        else
        {
            IsAccessible = false;
            FailedChecks++;
            ConsecutiveFailures++;
            ConsecutiveSuccesses = 0;
            LastFailure = DateTime.UtcNow;
            ErrorMessage = result.ErrorMessage;

            if (ConsecutiveFailures >= 3)
            {
                Status = "Error";
            }
            else if (ConsecutiveFailures >= 2)
            {
                Status = "Warning";
            }
        }

        ResponseTimeMs = result.ResponseTimeMs;
        HttpStatusCode = result.HttpStatusCode;
        ContentLength = result.ContentLength;
        ContentType = result.ContentType;

        SetNextCheckTime();
        UpdateTimestamp();
    }

    public void SetStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status cannot be empty", nameof(status));

        Status = status;
        UpdateTimestamp();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdateTimestamp();
    }

    public void SetPriority(int priority)
    {
        Priority = priority;
        UpdateTimestamp();
    }

    public void SetCheckInterval(TimeSpan interval)
    {
        CheckInterval = interval;
        SetNextCheckTime();
        UpdateTimestamp();
    }

    public void IncrementRetry()
    {
        RetryCount++;
        UpdateTimestamp();
    }

    public void ResetRetryCount()
    {
        RetryCount = 0;
        UpdateTimestamp();
    }

    public void SetNextCheckTime()
    {
        if (IsActive)
        {
            NextCheck = DateTime.UtcNow.Add(CheckInterval);
        }
        else
        {
            NextCheck = null;
        }
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }

    public bool ShouldCheck()
    {
        return IsActive && (!NextCheck.HasValue || NextCheck.Value <= DateTime.UtcNow);
    }

    public bool CanRetry()
    {
        return RetryCount < MaxRetries;
    }

    public double GetSuccessRate()
    {
        if (TotalChecks == 0) return 0;
        return (double)SuccessfulChecks / TotalChecks * 100;
    }

    public bool IsHealthy()
    {
        return Status == "Healthy" && GetSuccessRate() > 80;
    }

    public bool IsStable()
    {
        return ConsecutiveSuccesses >= 5;
    }

    public bool IsUnstable()
    {
        return ConsecutiveFailures >= 3;
    }
}

/// <summary>
/// Health check result entity
/// </summary>
public class HealthCheckResult
{
    [BsonElement("checkedAt")]
    public DateTime CheckedAt { get; set; }

    [BsonElement("isSuccess")]
    public bool IsSuccess { get; set; }

    [BsonElement("responseTimeMs")]
    public long? ResponseTimeMs { get; set; }

    [BsonElement("httpStatusCode")]
    public int? HttpStatusCode { get; set; }

    [BsonElement("contentLength")]
    public long? ContentLength { get; set; }

    [BsonElement("contentType")]
    public string? ContentType { get; set; }

    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; set; }

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static HealthCheckResult Create(bool isSuccess, long? responseTimeMs = null, int? httpStatusCode = null, long? contentLength = null, string? contentType = null, string? errorMessage = null)
    {
        return new HealthCheckResult
        {
            CheckedAt = DateTime.UtcNow,
            IsSuccess = isSuccess,
            ResponseTimeMs = responseTimeMs,
            HttpStatusCode = httpStatusCode,
            ContentLength = contentLength,
            ContentType = contentType,
            ErrorMessage = errorMessage,
            Metadata = new Dictionary<string, object>()
        };
    }
}
