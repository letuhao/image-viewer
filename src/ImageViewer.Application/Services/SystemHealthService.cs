using Microsoft.Extensions.Logging;
using ImageViewer.Application.DTOs.SystemHealth;
using MongoDB.Bson;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service for system health monitoring operations
/// </summary>
public class SystemHealthService : ISystemHealthService
{
    private readonly IPerformanceService _performanceService;
    private readonly ILogger<SystemHealthService> _logger;

    public SystemHealthService(IPerformanceService performanceService, ILogger<SystemHealthService> logger)
    {
        _performanceService = performanceService;
        _logger = logger;
    }

    public async Task<SystemHealthDto> HealthCheckAsync()
    {
        _logger.LogInformation("Performing system health check");

        try
        {
            var metrics = await _performanceService.GetPerformanceMetricsAsync();
            var health = new SystemHealthDto
            {
                Id = ObjectId.GenerateNewId(),
                CpuUsage = metrics.CpuUsage,
                MemoryUsage = metrics.MemoryUsage,
                DiskUsage = metrics.DiskUsage,
                NetworkUsage = metrics.NetworkUsage,
                ResponseTime = metrics.ResponseTime,
                RequestCount = metrics.RequestCount,
                ErrorRate = metrics.ErrorRate,
                Timestamp = DateTime.UtcNow
            };

            // Determine health status based on thresholds
            var warnings = new List<string>();
            var criticalIssues = new List<string>();

            if (metrics.CpuUsage > 80)
            {
                warnings.Add("High CPU usage detected");
                health.Status = "Warning";
            }
            else if (metrics.CpuUsage > 95)
            {
                criticalIssues.Add("Critical CPU usage detected");
                health.Status = "Critical";
            }

            if (metrics.MemoryUsage > 7L * 1024 * 1024 * 1024) // 7GB
            {
                warnings.Add("High memory usage detected");
                health.Status = "Warning";
            }
            else if (metrics.MemoryUsage > 8L * 1024 * 1024 * 1024) // 8GB
            {
                criticalIssues.Add("Critical memory usage detected");
                health.Status = "Critical";
            }

            if (metrics.ResponseTime > 300)
            {
                warnings.Add("High response time detected");
                health.Status = "Warning";
            }
            else if (metrics.ResponseTime > 500)
            {
                criticalIssues.Add("Critical response time detected");
                health.Status = "Critical";
            }

            if (metrics.ErrorRate > 10)
            {
                criticalIssues.Add("Critical error rate detected");
                health.Status = "Critical";
            }
            else if (metrics.ErrorRate > 5)
            {
                warnings.Add("High error rate detected");
                health.Status = "Warning";
            }

            health.Warnings = warnings;
            health.CriticalIssues = criticalIssues;
            health.IsHealthy = !warnings.Any() && !criticalIssues.Any();
            health.Status = string.IsNullOrEmpty(health.Status) ? "Healthy" : health.Status;

            _logger.LogInformation("System health check completed with status: {Status}", health.Status);
            return health;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing system health check");
            throw;
        }
    }

    public async Task<SystemMetricsDto> GetSystemMetricsAsync()
    {
        _logger.LogInformation("Getting system metrics");

        try
        {
            var metrics = await _performanceService.GetPerformanceMetricsAsync();
            return new SystemMetricsDto
            {
                Id = ObjectId.GenerateNewId(),
                CpuUsage = metrics.CpuUsage,
                MemoryUsage = metrics.MemoryUsage,
                DiskUsage = metrics.DiskUsage,
                NetworkUsage = metrics.NetworkUsage,
                ResponseTime = metrics.ResponseTime,
                RequestCount = metrics.RequestCount,
                ErrorRate = metrics.ErrorRate,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system metrics");
            throw;
        }
    }

    public async Task<ResourceStatusDto> GetResourceStatusAsync()
    {
        _logger.LogInformation("Getting resource status");

        try
        {
            var metrics = await _performanceService.GetPerformanceMetricsAsync();
            var status = new ResourceStatusDto
            {
                Id = ObjectId.GenerateNewId(),
                Timestamp = DateTime.UtcNow
            };

            // Determine resource status
            status.CpuStatus = metrics.CpuUsage > 80 ? "Warning" : "Normal";
            status.MemoryStatus = metrics.MemoryUsage > 7L * 1024 * 1024 * 1024 ? "Warning" : "Normal";
            status.DiskStatus = "Normal"; // Would need disk space info
            status.NetworkStatus = "Normal"; // Would need network info

            // Overall status
            if (status.CpuStatus == "Warning" || status.MemoryStatus == "Warning")
            {
                status.OverallStatus = "Warning";
            }
            else
            {
                status.OverallStatus = "Healthy";
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resource status");
            throw;
        }
    }

    public async Task<SystemAlertsDto> GetAlertsAsync()
    {
        _logger.LogInformation("Getting system alerts");

        try
        {
            var metrics = await _performanceService.GetPerformanceMetricsAsync();
            var alerts = new List<SystemAlertDto>();

            // CPU alerts
            if (metrics.CpuUsage > 80)
            {
                alerts.Add(new SystemAlertDto
                {
                    Id = ObjectId.GenerateNewId(),
                    Type = "CPU",
                    Severity = metrics.CpuUsage > 90 ? "Critical" : "High",
                    Message = $"CPU usage is {metrics.CpuUsage:F1}%",
                    Value = metrics.CpuUsage,
                    Threshold = 80,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Memory alerts
            if (metrics.MemoryUsage > 7L * 1024 * 1024 * 1024)
            {
                alerts.Add(new SystemAlertDto
                {
                    Id = ObjectId.GenerateNewId(),
                    Type = "Memory",
                    Severity = metrics.MemoryUsage > 8L * 1024 * 1024 * 1024 ? "Critical" : "High",
                    Message = $"Memory usage is {metrics.MemoryUsage / (1024L * 1024 * 1024):F1}GB",
                    Value = metrics.MemoryUsage,
                    Threshold = 7L * 1024 * 1024 * 1024,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Response time alerts
            if (metrics.ResponseTime > 500)
            {
                alerts.Add(new SystemAlertDto
                {
                    Id = ObjectId.GenerateNewId(),
                    Type = "ResponseTime",
                    Severity = metrics.ResponseTime > 1000 ? "Critical" : "High",
                    Message = $"Response time is {metrics.ResponseTime:F1}ms",
                    Value = metrics.ResponseTime,
                    Threshold = 500,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Error rate alerts
            if (metrics.ErrorRate > 5)
            {
                alerts.Add(new SystemAlertDto
                {
                    Id = ObjectId.GenerateNewId(),
                    Type = "ErrorRate",
                    Severity = metrics.ErrorRate > 10 ? "Critical" : "High",
                    Message = $"Error rate is {metrics.ErrorRate:F1}%",
                    Value = metrics.ErrorRate,
                    Threshold = 5,
                    Timestamp = DateTime.UtcNow
                });
            }

            return new SystemAlertsDto
            {
                Id = ObjectId.GenerateNewId(),
                Alerts = alerts,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system alerts");
            throw;
        }
    }

    public async Task<SystemDiagnosticsDto> RunDiagnosticsAsync()
    {
        _logger.LogInformation("Running system diagnostics");

        try
        {
            var metrics = await _performanceService.GetPerformanceMetricsAsync();
            var health = await HealthCheckAsync();

            var diagnostics = new SystemDiagnosticsDto
            {
                Id = ObjectId.GenerateNewId(),
                SystemInfo = new SystemInfoDto
                {
                    Id = ObjectId.GenerateNewId(),
                    OperatingSystem = Environment.OSVersion.Platform.ToString(),
                    Version = Environment.OSVersion.VersionString,
                    Architecture = Environment.OSVersion.Version.ToString(),
                    TotalMemory = GC.GetTotalMemory(false),
                    AvailableMemory = GC.GetTotalMemory(false),
                    TotalDiskSpace = 0, // Would need actual disk info
                    AvailableDiskSpace = 0,
                    ProcessorCount = Environment.ProcessorCount,
                    ProcessorName = Environment.ProcessorCount.ToString(),
                    Timestamp = DateTime.UtcNow
                },
                PerformanceMetrics = new SystemMetricsDto
                {
                    Id = ObjectId.GenerateNewId(),
                    CpuUsage = metrics.CpuUsage,
                    MemoryUsage = metrics.MemoryUsage,
                    DiskUsage = metrics.DiskUsage,
                    NetworkUsage = metrics.NetworkUsage,
                    ResponseTime = metrics.ResponseTime,
                    RequestCount = metrics.RequestCount,
                    ErrorRate = metrics.ErrorRate,
                    Timestamp = DateTime.UtcNow
                },
                HealthStatus = health,
                Recommendations = GenerateRecommendations(metrics),
                Timestamp = DateTime.UtcNow
            };

            return diagnostics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running system diagnostics");
            throw;
        }
    }

    public async Task<MaintenanceInfoDto> GetMaintenanceInfoAsync()
    {
        _logger.LogInformation("Getting maintenance information");

        try
        {
            var health = await HealthCheckAsync();
            var metrics = await _performanceService.GetPerformanceMetricsAsync();

            var maintenance = new MaintenanceInfoDto
            {
                Id = ObjectId.GenerateNewId(),
                LastMaintenance = DateTime.UtcNow.AddDays(-1),
                NextMaintenance = DateTime.UtcNow.AddDays(7),
                MaintenanceTasks = new List<string>
                {
                    "Clear temporary files",
                    "Update system components",
                    "Optimize database indexes",
                    "Clean up old logs"
                },
                SystemHealth = health,
                Recommendations = GenerateRecommendations(metrics)
            };

            return maintenance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting maintenance information");
            throw;
        }
    }

    public async Task<RecoveryInfoDto> GetRecoveryInfoAsync()
    {
        _logger.LogInformation("Getting recovery information");

        try
        {
            var recovery = new RecoveryInfoDto
            {
                Id = ObjectId.GenerateNewId(),
                RecoveryStatus = "Ready",
                BackupStatus = "Available",
                RecoveryPoints = new List<string>
                {
                    "Daily backup - 2024-01-15",
                    "Weekly backup - 2024-01-14",
                    "Monthly backup - 2024-01-01"
                },
                RecoveryTime = TimeSpan.FromMinutes(30),
                LastBackup = DateTime.UtcNow.AddHours(-6)
            };

            return recovery;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recovery information");
            throw;
        }
    }

    private List<string> GenerateRecommendations(PerformanceMetrics metrics)
    {
        var recommendations = new List<string>();

        if (metrics.CpuUsage > 70)
        {
            recommendations.Add("Consider optimizing CPU-intensive operations");
        }

        if (metrics.MemoryUsage > 6L * 1024 * 1024 * 1024)
        {
            recommendations.Add("Consider implementing memory optimization strategies");
        }

        if (metrics.ResponseTime > 300)
        {
            recommendations.Add("Consider implementing caching to improve response times");
        }

        if (metrics.ErrorRate > 2)
        {
            recommendations.Add("Investigate and fix error sources to reduce error rate");
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("System is performing well, continue monitoring");
        }

        return recommendations;
    }
}
