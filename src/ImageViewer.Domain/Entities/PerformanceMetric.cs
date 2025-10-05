using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Performance metric entity - represents system performance monitoring and metrics
/// </summary>
public class PerformanceMetric : BaseEntity
{
    [BsonElement("metricName")]
    public string MetricName { get; private set; } = string.Empty;

    [BsonElement("metricType")]
    public string MetricType { get; private set; } = string.Empty; // Counter, Gauge, Histogram, Timer

    [BsonElement("value")]
    public double Value { get; private set; }

    [BsonElement("unit")]
    public string Unit { get; private set; } = string.Empty; // ms, bytes, count, percent, etc.

    [BsonElement("tags")]
    public Dictionary<string, string> Tags { get; private set; } = new();

    [BsonElement("component")]
    public string Component { get; private set; } = string.Empty; // API, Database, Cache, etc.

    [BsonElement("operation")]
    public string? Operation { get; private set; }

    [BsonElement("endpoint")]
    public string? Endpoint { get; private set; }

    [BsonElement("userId")]
    public ObjectId? UserId { get; private set; }

    [BsonElement("sessionId")]
    public string? SessionId { get; private set; }

    [BsonElement("requestId")]
    public string? RequestId { get; private set; }

    [BsonElement("duration")]
    public long? DurationMs { get; private set; }

    [BsonElement("memoryUsage")]
    public long? MemoryUsageBytes { get; private set; }

    [BsonElement("cpuUsage")]
    public double? CpuUsagePercent { get; private set; }

    [BsonElement("status")]
    public string Status { get; private set; } = "Success"; // Success, Warning, Error

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("sampledAt")]
    public DateTime SampledAt { get; private set; }

    // Private constructor for EF Core
    private PerformanceMetric() { }

    public static PerformanceMetric Create(string metricName, string metricType, double value, string unit, string component, string? operation = null, string? endpoint = null, ObjectId? userId = null, string? sessionId = null, string? requestId = null)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name cannot be empty", nameof(metricName));

        if (string.IsNullOrWhiteSpace(metricType))
            throw new ArgumentException("Metric type cannot be empty", nameof(metricType));

        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Unit cannot be empty", nameof(unit));

        if (string.IsNullOrWhiteSpace(component))
            throw new ArgumentException("Component cannot be empty", nameof(component));

        return new PerformanceMetric
        {
            MetricName = metricName,
            MetricType = metricType,
            Value = value,
            Unit = unit,
            Component = component,
            Operation = operation,
            Endpoint = endpoint,
            UserId = userId,
            SessionId = sessionId,
            RequestId = requestId,
            Status = "Success",
            SampledAt = DateTime.UtcNow,
            Tags = new Dictionary<string, string>(),
            Metadata = new Dictionary<string, object>()
        };
    }

    public void SetDuration(long durationMs)
    {
        DurationMs = durationMs;
        UpdateTimestamp();
    }

    public void SetMemoryUsage(long memoryUsageBytes)
    {
        MemoryUsageBytes = memoryUsageBytes;
        UpdateTimestamp();
    }

    public void SetCpuUsage(double cpuUsagePercent)
    {
        CpuUsagePercent = cpuUsagePercent;
        UpdateTimestamp();
    }

    public void SetStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status cannot be empty", nameof(status));

        Status = status;
        UpdateTimestamp();
    }

    public void AddTag(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Tag key cannot be empty", nameof(key));

        Tags[key] = value;
        UpdateTimestamp();
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }

    public void UpdateValue(double newValue)
    {
        Value = newValue;
        UpdateTimestamp();
    }
}
