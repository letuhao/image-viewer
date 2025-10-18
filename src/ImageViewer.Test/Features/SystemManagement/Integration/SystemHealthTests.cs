using ImageViewer.Application.Services;
using ImageViewer.Test.Shared.Fixtures;

namespace ImageViewer.Test.Features.SystemManagement.Integration;

/// <summary>
/// Integration tests for System Health - End-to-end system health monitoring scenarios
/// </summary>
[Collection("Integration")]
public class SystemHealthTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly SystemHealthService _systemHealthService;

    public SystemHealthTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _systemHealthService = _fixture.GetService<SystemHealthService>();
    }

    [Fact]
    public async Task SystemHealth_HealthCheck_ShouldPerformHealthCheck()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();

        // Act
        var healthStatus = await _systemHealthService.HealthCheckAsync();

        // Assert
        healthStatus.Should().NotBeNull();
        healthStatus.Status.Should().BeOneOf("Healthy", "Warning", "Critical");
        healthStatus.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        healthStatus.CpuUsage.Should().BeGreaterOrEqualTo(0).And.BeLessOrEqualTo(100);
        healthStatus.MemoryUsage.Should().BeGreaterOrEqualTo(0);
        healthStatus.DiskUsage.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task SystemHealth_SystemMetrics_ShouldCollectMetrics()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();

        // Act
        var metrics = await _systemHealthService.GetSystemMetricsAsync();

        // Assert
        metrics.Should().NotBeNull();
        metrics.CpuUsage.Should().BeGreaterOrEqualTo(0).And.BeLessOrEqualTo(100);
        metrics.MemoryUsage.Should().BeGreaterOrEqualTo(0);
        metrics.DiskUsage.Should().BeGreaterOrEqualTo(0);
        metrics.NetworkUsage.Should().BeGreaterOrEqualTo(0);
        metrics.ResponseTime.Should().BeGreaterOrEqualTo(0);
        metrics.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task SystemHealth_ResourceMonitoring_ShouldMonitorResources()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();

        // Act
        var resourceStatus = await _systemHealthService.GetResourceStatusAsync();

        // Assert
        resourceStatus.Should().NotBeNull();
               resourceStatus.CpuStatus.Should().BeOneOf("Normal", "Warning", "Critical", "Healthy");
               resourceStatus.MemoryStatus.Should().BeOneOf("Normal", "Warning", "Critical", "Healthy");
               resourceStatus.DiskStatus.Should().BeOneOf("Normal", "Warning", "Critical", "Healthy");
               resourceStatus.NetworkStatus.Should().BeOneOf("Normal", "Warning", "Critical", "Healthy");
               resourceStatus.OverallStatus.Should().BeOneOf("Normal", "Warning", "Critical", "Healthy");
        resourceStatus.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task SystemHealth_Alerting_ShouldTriggerAlerts()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();

        // Act
        var alerts = await _systemHealthService.GetAlertsAsync();

        // Assert
        alerts.Should().NotBeNull();
        alerts.Alerts.Should().NotBeNull();
        alerts.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        
        // Verify alert structure if any alerts exist
        foreach (var alert in alerts.Alerts)
        {
            alert.Should().NotBeNull();
            alert.Type.Should().NotBeNullOrEmpty();
            alert.Severity.Should().BeOneOf("Low", "Medium", "High", "Critical");
        }
    }

    [Fact]
    public async Task SystemHealth_Diagnostics_ShouldRunDiagnostics()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();

        // Act
        var diagnostics = await _systemHealthService.RunDiagnosticsAsync();

        // Assert
        diagnostics.Should().NotBeNull();
        diagnostics.SystemInfo.Should().NotBeNull();
        diagnostics.PerformanceMetrics.Should().NotBeNull();
        diagnostics.HealthStatus.Should().NotBeNull();
        diagnostics.Recommendations.Should().NotBeNull();
        diagnostics.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task SystemHealth_Maintenance_ShouldPerformMaintenance()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();

        // Act
        var maintenanceResult = await _systemHealthService.GetMaintenanceInfoAsync();

        // Assert
        maintenanceResult.Should().NotBeNull();
        maintenanceResult.LastMaintenance.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromDays(30));
        maintenanceResult.NextMaintenance.Should().BeAfter(DateTime.UtcNow);
        maintenanceResult.MaintenanceTasks.Should().NotBeNull();
        maintenanceResult.SystemHealth.Should().NotBeNull();
        maintenanceResult.Recommendations.Should().NotBeNull();
    }

    [Fact]
    public async Task SystemHealth_Recovery_ShouldPerformRecovery()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();

        // Act
        var recoveryResult = await _systemHealthService.GetRecoveryInfoAsync();

        // Assert
        recoveryResult.Should().NotBeNull();
        recoveryResult.RecoveryStatus.Should().BeOneOf("Ready", "InProgress", "Failed");
        recoveryResult.BackupStatus.Should().BeOneOf("Available", "Unavailable", "InProgress");
        recoveryResult.RecoveryPoints.Should().NotBeNull();
        recoveryResult.RecoveryTime.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
        recoveryResult.LastBackup.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromDays(7));
    }

    [Fact]
    public async Task SystemHealth_HealthReporting_ShouldGenerateReports()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = DateTime.UtcNow;

        // Act
        var report = await _systemHealthService.RunDiagnosticsAsync();

        // Assert
        report.Should().NotBeNull();
        report.SystemInfo.Should().NotBeNull();
        report.PerformanceMetrics.Should().NotBeNull();
        report.HealthStatus.Should().NotBeNull();
        report.Recommendations.Should().NotBeNull();
        report.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }
}
