using MongoDB.Bson;

namespace ImageViewer.Application.DTOs.SystemHealth;

/// <summary>
/// System health check result
/// </summary>
public class SystemHealthDto
{
    public ObjectId Id { get; set; }
    public string Status { get; set; } = string.Empty; // Healthy, Warning, Critical
    public bool IsHealthy { get; set; }
    public double CpuUsage { get; set; }
    public long MemoryUsage { get; set; }
    public long DiskUsage { get; set; }
    public long NetworkUsage { get; set; }
    public double ResponseTime { get; set; }
    public long RequestCount { get; set; }
    public double ErrorRate { get; set; }
    public DateTime Timestamp { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> CriticalIssues { get; set; } = new();
}

/// <summary>
/// System metrics information
/// </summary>
public class SystemMetricsDto
{
    public ObjectId Id { get; set; }
    public double CpuUsage { get; set; }
    public long MemoryUsage { get; set; }
    public long DiskUsage { get; set; }
    public long NetworkUsage { get; set; }
    public double ResponseTime { get; set; }
    public long RequestCount { get; set; }
    public double ErrorRate { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Resource status information
/// </summary>
public class ResourceStatusDto
{
    public ObjectId Id { get; set; }
    public string CpuStatus { get; set; } = string.Empty; // Normal, Warning, Critical
    public string MemoryStatus { get; set; } = string.Empty;
    public string DiskStatus { get; set; } = string.Empty;
    public string NetworkStatus { get; set; } = string.Empty;
    public string OverallStatus { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// System alert information
/// </summary>
public class SystemAlertDto
{
    public ObjectId Id { get; set; }
    public string Type { get; set; } = string.Empty; // CPU, Memory, Disk, Network, ResponseTime, ErrorRate
    public string Severity { get; set; } = string.Empty; // Low, Medium, High, Critical
    public string Message { get; set; } = string.Empty;
    public double Value { get; set; }
    public double Threshold { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// System alerts collection
/// </summary>
public class SystemAlertsDto
{
    public ObjectId Id { get; set; }
    public List<SystemAlertDto> Alerts { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// System diagnostic information
/// </summary>
public class SystemDiagnosticsDto
{
    public ObjectId Id { get; set; }
    public SystemInfoDto SystemInfo { get; set; } = new();
    public SystemMetricsDto PerformanceMetrics { get; set; } = new();
    public SystemHealthDto HealthStatus { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// System information
/// </summary>
public class SystemInfoDto
{
    public ObjectId Id { get; set; }
    public string OperatingSystem { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
    public long TotalMemory { get; set; }
    public long AvailableMemory { get; set; }
    public long TotalDiskSpace { get; set; }
    public long AvailableDiskSpace { get; set; }
    public int ProcessorCount { get; set; }
    public string ProcessorName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Maintenance information
/// </summary>
public class MaintenanceInfoDto
{
    public ObjectId Id { get; set; }
    public DateTime LastMaintenance { get; set; }
    public DateTime NextMaintenance { get; set; }
    public List<string> MaintenanceTasks { get; set; } = new();
    public SystemHealthDto SystemHealth { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Recovery information
/// </summary>
public class RecoveryInfoDto
{
    public ObjectId Id { get; set; }
    public string RecoveryStatus { get; set; } = string.Empty; // Ready, InProgress, Failed
    public string BackupStatus { get; set; } = string.Empty; // Available, Unavailable, InProgress
    public List<string> RecoveryPoints { get; set; } = new();
    public TimeSpan RecoveryTime { get; set; }
    public DateTime LastBackup { get; set; }
}
