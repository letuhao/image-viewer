using ImageViewer.Application.DTOs.SystemHealth;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service interface for system health monitoring operations
/// </summary>
public interface ISystemHealthService
{
    /// <summary>
    /// Performs a comprehensive health check of the system
    /// </summary>
    /// <returns>System health status</returns>
    Task<SystemHealthDto> HealthCheckAsync();

    /// <summary>
    /// Gets current system metrics
    /// </summary>
    /// <returns>System metrics information</returns>
    Task<SystemMetricsDto> GetSystemMetricsAsync();

    /// <summary>
    /// Gets resource status information
    /// </summary>
    /// <returns>Resource status</returns>
    Task<ResourceStatusDto> GetResourceStatusAsync();

    /// <summary>
    /// Gets system alerts based on current metrics
    /// </summary>
    /// <returns>System alerts</returns>
    Task<SystemAlertsDto> GetAlertsAsync();

    /// <summary>
    /// Runs comprehensive system diagnostics
    /// </summary>
    /// <returns>Diagnostic information</returns>
    Task<SystemDiagnosticsDto> RunDiagnosticsAsync();

    /// <summary>
    /// Gets maintenance information and recommendations
    /// </summary>
    /// <returns>Maintenance information</returns>
    Task<MaintenanceInfoDto> GetMaintenanceInfoAsync();

    /// <summary>
    /// Gets recovery information and backup status
    /// </summary>
    /// <returns>Recovery information</returns>
    Task<RecoveryInfoDto> GetRecoveryInfoAsync();
}
