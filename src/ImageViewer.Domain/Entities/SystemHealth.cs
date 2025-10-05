using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// System health entity - represents system health metrics and status
/// </summary>
public class SystemHealth : BaseEntity
{
    [BsonElement("component")]
    public string Component { get; private set; } = string.Empty; // "database", "cache", "storage", "api", "worker"

    [BsonElement("status")]
    public string Status { get; private set; } = string.Empty; // "healthy", "degraded", "unhealthy", "unknown"

    [BsonElement("healthScore")]
    public double HealthScore { get; private set; } // 0.0 to 1.0

    [BsonElement("metrics")]
    public Dictionary<string, object> Metrics { get; private set; } = new();

    [BsonElement("lastCheck")]
    public DateTime LastCheck { get; private set; }

    [BsonElement("responseTime")]
    public TimeSpan? ResponseTime { get; private set; }

    [BsonElement("errorCount")]
    public long ErrorCount { get; private set; } = 0;

    [BsonElement("warningCount")]
    public long WarningCount { get; private set; } = 0;

    [BsonElement("lastError")]
    public string? LastError { get; private set; }

    [BsonElement("lastErrorAt")]
    public DateTime? LastErrorAt { get; private set; }

    [BsonElement("uptime")]
    public TimeSpan? Uptime { get; private set; }

    [BsonElement("version")]
    public string? Version { get; private set; }

    [BsonElement("dependencies")]
    public List<ComponentDependency> Dependencies { get; private set; } = new();

    [BsonElement("alerts")]
    public List<HealthAlert> Alerts { get; private set; } = new();

    [BsonElement("thresholds")]
    public HealthThresholds Thresholds { get; private set; } = new();

    // Private constructor for EF Core
    private SystemHealth() { }

    public SystemHealth(
        string component,
        string status = "unknown",
        double healthScore = 0.0)
    {
        Component = component;
        Status = status;
        HealthScore = healthScore;
        LastCheck = DateTime.UtcNow;
        Metrics = new Dictionary<string, object>();
        Dependencies = new List<ComponentDependency>();
        Alerts = new List<HealthAlert>();
        Thresholds = new HealthThresholds();
        ErrorCount = 0;
        WarningCount = 0;
    }

    public void UpdateStatus(string status, double healthScore, TimeSpan? responseTime = null)
    {
        Status = status;
        HealthScore = Math.Max(0.0, Math.Min(1.0, healthScore));
        ResponseTime = responseTime;
        LastCheck = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetMetric(string key, object value)
    {
        Metrics[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveMetric(string key)
    {
        if (Metrics.ContainsKey(key))
        {
            Metrics.Remove(key);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RecordError(string error, DateTime? occurredAt = null)
    {
        ErrorCount++;
        LastError = error;
        LastErrorAt = occurredAt ?? DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordWarning()
    {
        WarningCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetUptime(TimeSpan uptime)
    {
        Uptime = uptime;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetVersion(string version)
    {
        Version = version;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddDependency(ComponentDependency dependency)
    {
        var existing = Dependencies.FirstOrDefault(d => d.ComponentName == dependency.ComponentName);
        if (existing != null)
        {
            Dependencies.Remove(existing);
        }
        Dependencies.Add(dependency);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveDependency(string componentName)
    {
        Dependencies.RemoveAll(d => d.ComponentName == componentName);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddAlert(HealthAlert alert)
    {
        Alerts.Add(alert);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveAlert(string alertId)
    {
        Alerts.RemoveAll(a => a.AlertId == alertId);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearResolvedAlerts()
    {
        Alerts.RemoveAll(a => a.Status == "resolved");
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetThresholds(HealthThresholds thresholds)
    {
        Thresholds = thresholds;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsHealthy()
    {
        return Status == "healthy" && HealthScore >= Thresholds.MinHealthScore;
    }

    public bool IsDegraded()
    {
        return Status == "degraded" || (Status == "healthy" && HealthScore < Thresholds.MinHealthScore);
    }

    public bool IsUnhealthy()
    {
        return Status == "unhealthy" || HealthScore < Thresholds.CriticalHealthScore;
    }

    public bool HasActiveAlerts()
    {
        return Alerts.Any(a => a.Status == "active");
    }

    public List<HealthAlert> GetActiveAlerts()
    {
        return Alerts.Where(a => a.Status == "active").ToList();
    }

    public List<HealthAlert> GetCriticalAlerts()
    {
        return Alerts.Where(a => a.Severity == "critical" && a.Status == "active").ToList();
    }

    public bool ShouldAlert()
    {
        return IsUnhealthy() || HasActiveAlerts() || GetCriticalAlerts().Any();
    }
}

/// <summary>
/// Component dependency entity
/// </summary>
public class ComponentDependency
{
    [BsonElement("componentName")]
    public string ComponentName { get; set; } = string.Empty;

    [BsonElement("dependencyType")]
    public string DependencyType { get; set; } = string.Empty; // "required", "optional", "external"

    [BsonElement("status")]
    public string Status { get; set; } = string.Empty; // "available", "unavailable", "degraded"

    [BsonElement("lastChecked")]
    public DateTime LastChecked { get; set; }

    [BsonElement("responseTime")]
    public TimeSpan? ResponseTime { get; set; }

    [BsonElement("version")]
    public string? Version { get; set; }

    [BsonElement("endpoint")]
    public string? Endpoint { get; set; }
}

/// <summary>
/// Health alert entity
/// </summary>
public class HealthAlert
{
    [BsonElement("alertId")]
    public string AlertId { get; set; } = string.Empty;

    [BsonElement("alertType")]
    public string AlertType { get; set; } = string.Empty; // "error", "warning", "info"

    [BsonElement("severity")]
    public string Severity { get; set; } = string.Empty; // "low", "medium", "high", "critical"

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = string.Empty; // "active", "acknowledged", "resolved"

    [BsonElement("triggeredAt")]
    public DateTime TriggeredAt { get; set; }

    [BsonElement("acknowledgedAt")]
    public DateTime? AcknowledgedAt { get; set; }

    [BsonElement("resolvedAt")]
    public DateTime? ResolvedAt { get; set; }

    [BsonElement("acknowledgedBy")]
    public ObjectId? AcknowledgedBy { get; set; }

    [BsonElement("resolvedBy")]
    public ObjectId? ResolvedBy { get; set; }

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Health thresholds entity
/// </summary>
public class HealthThresholds
{
    [BsonElement("minHealthScore")]
    public double MinHealthScore { get; set; } = 0.7;

    [BsonElement("criticalHealthScore")]
    public double CriticalHealthScore { get; set; } = 0.3;

    [BsonElement("maxResponseTime")]
    public TimeSpan MaxResponseTime { get; set; } = TimeSpan.FromSeconds(5);

    [BsonElement("maxErrorRate")]
    public double MaxErrorRate { get; set; } = 0.05; // 5%

    [BsonElement("maxWarningCount")]
    public long MaxWarningCount { get; set; } = 10;

    [BsonElement("maxErrorCount")]
    public long MaxErrorCount { get; set; } = 5;

    [BsonElement("checkInterval")]
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(1);

    [BsonElement("alertCooldown")]
    public TimeSpan AlertCooldown { get; set; } = TimeSpan.FromMinutes(5);
}
