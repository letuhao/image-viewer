using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Node performance metrics entity - represents performance metrics for distribution nodes
/// </summary>
public class NodePerformanceMetrics : BaseEntity
{
    [BsonElement("nodeId")]
    public ObjectId NodeId { get; private set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; private set; }

    [BsonElement("cpuUsage")]
    public double CpuUsage { get; private set; } = 0; // percentage

    [BsonElement("memoryUsage")]
    public double MemoryUsage { get; private set; } = 0; // percentage

    [BsonElement("diskUsage")]
    public double DiskUsage { get; private set; } = 0; // percentage

    [BsonElement("networkIn")]
    public long NetworkIn { get; private set; } = 0; // bytes per second

    [BsonElement("networkOut")]
    public long NetworkOut { get; private set; } = 0; // bytes per second

    [BsonElement("requestsPerSecond")]
    public double RequestsPerSecond { get; private set; } = 0;

    [BsonElement("responseTime")]
    public long ResponseTime { get; private set; } = 0; // milliseconds

    [BsonElement("errorRate")]
    public double ErrorRate { get; private set; } = 0; // percentage

    [BsonElement("activeConnections")]
    public int ActiveConnections { get; private set; } = 0;

    [BsonElement("totalRequests")]
    public long TotalRequests { get; private set; } = 0;

    [BsonElement("successfulRequests")]
    public long SuccessfulRequests { get; private set; } = 0;

    [BsonElement("failedRequests")]
    public long FailedRequests { get; private set; } = 0;

    [BsonElement("cacheHitRate")]
    public double CacheHitRate { get; private set; } = 0; // percentage

    [BsonElement("bandwidthUtilization")]
    public double BandwidthUtilization { get; private set; } = 0; // percentage

    [BsonElement("loadAverage")]
    public double LoadAverage { get; private set; } = 0;

    [BsonElement("temperature")]
    public double? Temperature { get; private set; } // celsius

    [BsonElement("powerConsumption")]
    public double? PowerConsumption { get; private set; } // watts

    [BsonElement("uptime")]
    public TimeSpan Uptime { get; private set; } = TimeSpan.Zero;

    [BsonElement("lastRestart")]
    public DateTime? LastRestart { get; private set; }

    [BsonElement("healthScore")]
    public double HealthScore { get; private set; } = 100; // 0-100

    [BsonElement("performanceScore")]
    public double PerformanceScore { get; private set; } = 100; // 0-100

    [BsonElement("alerts")]
    public List<PerformanceAlert> Alerts { get; private set; } = new();

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    // Navigation properties
    [BsonIgnore]
    public DistributionNode Node { get; private set; } = null!;

    // Private constructor for EF Core
    private NodePerformanceMetrics() { }

    public static NodePerformanceMetrics Create(ObjectId nodeId, DateTime timestamp)
    {
        return new NodePerformanceMetrics
        {
            NodeId = nodeId,
            Timestamp = timestamp,
            CpuUsage = 0,
            MemoryUsage = 0,
            DiskUsage = 0,
            NetworkIn = 0,
            NetworkOut = 0,
            RequestsPerSecond = 0,
            ResponseTime = 0,
            ErrorRate = 0,
            ActiveConnections = 0,
            TotalRequests = 0,
            SuccessfulRequests = 0,
            FailedRequests = 0,
            CacheHitRate = 0,
            BandwidthUtilization = 0,
            LoadAverage = 0,
            Uptime = TimeSpan.Zero,
            HealthScore = 100,
            PerformanceScore = 100,
            Alerts = new List<PerformanceAlert>(),
            Metadata = new Dictionary<string, object>()
        };
    }

    public void UpdateSystemMetrics(double cpuUsage, double memoryUsage, double diskUsage, double? temperature = null, double? powerConsumption = null)
    {
        CpuUsage = Math.Max(0, Math.Min(100, cpuUsage));
        MemoryUsage = Math.Max(0, Math.Min(100, memoryUsage));
        DiskUsage = Math.Max(0, Math.Min(100, diskUsage));
        Temperature = temperature;
        PowerConsumption = powerConsumption;
        UpdateTimestamp();
    }

    public void UpdateNetworkMetrics(long networkIn, long networkOut, double bandwidthUtilization)
    {
        NetworkIn = Math.Max(0, networkIn);
        NetworkOut = Math.Max(0, networkOut);
        BandwidthUtilization = Math.Max(0, Math.Min(100, bandwidthUtilization));
        UpdateTimestamp();
    }

    public void UpdateRequestMetrics(double requestsPerSecond, long responseTime, long totalRequests, long successfulRequests, long failedRequests)
    {
        RequestsPerSecond = Math.Max(0, requestsPerSecond);
        ResponseTime = Math.Max(0, responseTime);
        TotalRequests = Math.Max(0, totalRequests);
        SuccessfulRequests = Math.Max(0, successfulRequests);
        FailedRequests = Math.Max(0, failedRequests);

        if (totalRequests > 0)
        {
            ErrorRate = (double)failedRequests / totalRequests * 100;
        }

        UpdateTimestamp();
    }

    public void UpdateConnectionMetrics(int activeConnections)
    {
        ActiveConnections = Math.Max(0, activeConnections);
        UpdateTimestamp();
    }

    public void UpdateCacheMetrics(double cacheHitRate)
    {
        CacheHitRate = Math.Max(0, Math.Min(100, cacheHitRate));
        UpdateTimestamp();
    }

    public void UpdateLoadAverage(double loadAverage)
    {
        LoadAverage = Math.Max(0, loadAverage);
        UpdateTimestamp();
    }

    public void UpdateUptime(TimeSpan uptime, DateTime? lastRestart = null)
    {
        Uptime = uptime;
        LastRestart = lastRestart;
        UpdateTimestamp();
    }

    public void CalculateHealthScore()
    {
        var cpuScore = Math.Max(0, 100 - CpuUsage);
        var memoryScore = Math.Max(0, 100 - MemoryUsage);
        var diskScore = Math.Max(0, 100 - DiskUsage);
        var errorScore = Math.Max(0, 100 - ErrorRate);
        var responseScore = Math.Max(0, 100 - (ResponseTime / 100.0)); // Normalize response time

        HealthScore = (cpuScore + memoryScore + diskScore + errorScore + responseScore) / 5;
        UpdateTimestamp();
    }

    public void CalculatePerformanceScore()
    {
        var throughputScore = Math.Min(100, RequestsPerSecond * 10); // Scale requests per second
        var cacheScore = CacheHitRate;
        var bandwidthScore = Math.Min(100, BandwidthUtilization);
        var loadScore = Math.Max(0, 100 - (LoadAverage * 10)); // Scale load average

        PerformanceScore = (throughputScore + cacheScore + bandwidthScore + loadScore) / 4;
        UpdateTimestamp();
    }

    public void AddAlert(PerformanceAlert alert)
    {
        Alerts.Add(alert);
        UpdateTimestamp();
    }

    public void ClearAlerts()
    {
        Alerts.Clear();
        UpdateTimestamp();
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }

    public bool IsHealthy()
    {
        return HealthScore >= 80 && ErrorRate < 5 && CpuUsage < 90 && MemoryUsage < 90;
    }

    public bool IsPerformingWell()
    {
        return PerformanceScore >= 70 && ResponseTime < 1000 && CacheHitRate > 50;
    }

    public bool HasAlerts()
    {
        return Alerts.Any(a => a.Severity == "Critical" || a.Severity == "High");
    }

    public List<PerformanceAlert> GetCriticalAlerts()
    {
        return Alerts.Where(a => a.Severity == "Critical").ToList();
    }

    public List<PerformanceAlert> GetHighAlerts()
    {
        return Alerts.Where(a => a.Severity == "High").ToList();
    }
}

/// <summary>
/// Performance alert entity
/// </summary>
public class PerformanceAlert
{
    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    [BsonElement("severity")]
    public string Severity { get; set; } = string.Empty; // Critical, High, Medium, Low

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("threshold")]
    public double? Threshold { get; set; }

    [BsonElement("actualValue")]
    public double? ActualValue { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("isResolved")]
    public bool IsResolved { get; set; } = false;

    [BsonElement("resolvedAt")]
    public DateTime? ResolvedAt { get; set; }

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static PerformanceAlert Create(string type, string severity, string message, double? threshold = null, double? actualValue = null)
    {
        return new PerformanceAlert
        {
            Type = type,
            Severity = severity,
            Message = message,
            Threshold = threshold,
            ActualValue = actualValue,
            Timestamp = DateTime.UtcNow,
            IsResolved = false,
            Metadata = new Dictionary<string, object>()
        };
    }

    public void Resolve()
    {
        IsResolved = true;
        ResolvedAt = DateTime.UtcNow;
    }
}
