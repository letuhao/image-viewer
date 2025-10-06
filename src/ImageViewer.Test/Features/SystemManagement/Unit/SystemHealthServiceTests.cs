using FluentAssertions;
using Moq;
using Xunit;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.SystemHealth;
using ImageViewer.Domain.Entities;
using MongoDB.Bson;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Test.Features.SystemManagement.Unit;

/// <summary>
/// Unit tests for SystemHealthService - System Health Monitoring features
/// </summary>
public class SystemHealthServiceTests
{
    private readonly Mock<IPerformanceService> _mockPerformanceService;
    private readonly Mock<ILogger<SystemHealthService>> _mockLogger;
    private readonly SystemHealthService _systemHealthService;

    public SystemHealthServiceTests()
    {
        _mockPerformanceService = new Mock<IPerformanceService>();
        _mockLogger = new Mock<ILogger<SystemHealthService>>();
        _systemHealthService = new SystemHealthService(_mockPerformanceService.Object, _mockLogger.Object);
    }

    [Fact]
    public void SystemHealthService_ShouldExist()
    {
        // Arrange & Act
        var service = _systemHealthService;

        // Assert
        service.Should().NotBeNull();
        service.Should().BeOfType<SystemHealthService>();
    }

    [Fact]
    public async Task HealthCheck_WithHealthySystem_ShouldReturnHealthyStatus()
    {
        // Arrange
        var performanceMetrics = new PerformanceMetrics
        {
            Id = ObjectId.GenerateNewId(),
            Timestamp = DateTime.UtcNow,
            CpuUsage = 45.5,
                   MemoryUsage = 1024L * 1024 * 512, // 512MB
                   DiskUsage = 1024L * 1024 * 1024 * 10, // 10GB
                   NetworkUsage = 1024L * 1024 * 5, // 5MB
            ResponseTime = 150.0,
            RequestCount = 1000,
            ErrorRate = 0.5
        };

        _mockPerformanceService.Setup(x => x.GetPerformanceMetricsAsync())
            .ReturnsAsync(performanceMetrics);

        // Act
        var result = await _systemHealthService.HealthCheckAsync();

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Healthy");
        result.IsHealthy.Should().BeTrue();
        result.CpuUsage.Should().Be(45.5);
        result.MemoryUsage.Should().Be(1024L * 1024 * 512);
        result.DiskUsage.Should().Be(1024L * 1024 * 1024 * 10);
        result.NetworkUsage.Should().Be(1024L * 1024 * 5);
        result.ResponseTime.Should().Be(150.0);
        result.RequestCount.Should().Be(1000);
        result.ErrorRate.Should().Be(0.5);
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task HealthCheck_WithHighCpuUsage_ShouldReturnWarningStatus()
    {
        // Arrange
        var performanceMetrics = new PerformanceMetrics
        {
            Id = ObjectId.GenerateNewId(),
            Timestamp = DateTime.UtcNow,
            CpuUsage = 85.0, // High CPU usage
            MemoryUsage = 1024L * 1024 * 512,
            DiskUsage = 1024L * 1024 * 1024 * 10,
            NetworkUsage = 1024L * 1024 * 5,
            ResponseTime = 150.0,
            RequestCount = 1000,
            ErrorRate = 0.5
        };

        _mockPerformanceService.Setup(x => x.GetPerformanceMetricsAsync())
            .ReturnsAsync(performanceMetrics);

        // Act
        var result = await _systemHealthService.HealthCheckAsync();

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Warning");
        result.IsHealthy.Should().BeFalse();
        result.CpuUsage.Should().Be(85.0);
        result.Warnings.Should().Contain("High CPU usage detected");
    }

    [Fact]
    public async Task HealthCheck_WithHighMemoryUsage_ShouldReturnWarningStatus()
    {
        // Arrange
        var performanceMetrics = new PerformanceMetrics
        {
            Id = ObjectId.GenerateNewId(),
            Timestamp = DateTime.UtcNow,
            CpuUsage = 45.5,
                   MemoryUsage = 1024L * 1024 * 1024 * 8, // 8GB - High memory usage
            DiskUsage = 1024L * 1024 * 1024 * 10,
            NetworkUsage = 1024L * 1024 * 5,
            ResponseTime = 150.0,
            RequestCount = 1000,
            ErrorRate = 0.5
        };

        _mockPerformanceService.Setup(x => x.GetPerformanceMetricsAsync())
            .ReturnsAsync(performanceMetrics);

        // Act
        var result = await _systemHealthService.HealthCheckAsync();

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Warning");
        result.IsHealthy.Should().BeFalse();
        result.MemoryUsage.Should().Be(1024L * 1024 * 1024 * 8);
        result.Warnings.Should().Contain("High memory usage detected");
    }

    [Fact]
    public async Task HealthCheck_WithHighErrorRate_ShouldReturnCriticalStatus()
    {
        // Arrange
        var performanceMetrics = new PerformanceMetrics
        {
            Id = ObjectId.GenerateNewId(),
            Timestamp = DateTime.UtcNow,
            CpuUsage = 45.5,
            MemoryUsage = 1024L * 1024 * 512,
            DiskUsage = 1024L * 1024 * 1024 * 10,
            NetworkUsage = 1024L * 1024 * 5,
            ResponseTime = 150.0,
            RequestCount = 1000,
            ErrorRate = 15.0 // High error rate
        };

        _mockPerformanceService.Setup(x => x.GetPerformanceMetricsAsync())
            .ReturnsAsync(performanceMetrics);

        // Act
        var result = await _systemHealthService.HealthCheckAsync();

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Critical");
        result.IsHealthy.Should().BeFalse();
        result.ErrorRate.Should().Be(15.0);
        result.CriticalIssues.Should().Contain("Critical error rate detected");
    }

    [Fact]
    public async Task SystemMetrics_WithValidData_ShouldReturnSystemMetrics()
    {
        // Arrange
        var performanceMetrics = new PerformanceMetrics
        {
            Id = ObjectId.GenerateNewId(),
            Timestamp = DateTime.UtcNow,
            CpuUsage = 45.5,
            MemoryUsage = 1024L * 1024 * 512,
            DiskUsage = 1024L * 1024 * 1024 * 10,
            NetworkUsage = 1024L * 1024 * 5,
            ResponseTime = 150.0,
            RequestCount = 1000,
            ErrorRate = 0.5
        };

        _mockPerformanceService.Setup(x => x.GetPerformanceMetricsAsync())
            .ReturnsAsync(performanceMetrics);

        // Act
        var result = await _systemHealthService.GetSystemMetricsAsync();

        // Assert
        result.Should().NotBeNull();
        result.CpuUsage.Should().Be(45.5);
        result.MemoryUsage.Should().Be(1024L * 1024 * 512);
        result.DiskUsage.Should().Be(1024L * 1024 * 1024 * 10);
        result.NetworkUsage.Should().Be(1024L * 1024 * 5);
        result.ResponseTime.Should().Be(150.0);
        result.RequestCount.Should().Be(1000);
        result.ErrorRate.Should().Be(0.5);
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ResourceMonitoring_WithValidData_ShouldReturnResourceStatus()
    {
        // Arrange
        var performanceMetrics = new PerformanceMetrics
        {
            Id = ObjectId.GenerateNewId(),
            Timestamp = DateTime.UtcNow,
            CpuUsage = 45.5,
            MemoryUsage = 1024L * 1024 * 512,
            DiskUsage = 1024L * 1024 * 1024 * 10,
            NetworkUsage = 1024L * 1024 * 5,
            ResponseTime = 150.0,
            RequestCount = 1000,
            ErrorRate = 0.5
        };

        _mockPerformanceService.Setup(x => x.GetPerformanceMetricsAsync())
            .ReturnsAsync(performanceMetrics);

        // Act
        var result = await _systemHealthService.GetResourceStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.CpuStatus.Should().Be("Normal");
        result.MemoryStatus.Should().Be("Normal");
        result.DiskStatus.Should().Be("Normal");
        result.NetworkStatus.Should().Be("Normal");
        result.OverallStatus.Should().Be("Healthy");
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Alerting_WithThresholdExceeded_ShouldReturnAlerts()
    {
        // Arrange
        var performanceMetrics = new PerformanceMetrics
        {
            Id = ObjectId.GenerateNewId(),
            Timestamp = DateTime.UtcNow,
            CpuUsage = 90.0, // Exceeds threshold
            MemoryUsage = (long)(1024L * 1024 * 1024 * 7.5), // Exceeds threshold but not critical
            DiskUsage = 1024L * 1024 * 1024 * 10,
            NetworkUsage = 1024L * 1024 * 5,
            ResponseTime = 600.0, // Exceeds threshold
            RequestCount = 1000,
            ErrorRate = 15.0 // Exceeds threshold
        };

        _mockPerformanceService.Setup(x => x.GetPerformanceMetricsAsync())
            .ReturnsAsync(performanceMetrics);

        // Act
        var result = await _systemHealthService.GetAlertsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Alerts.Should().NotBeEmpty();
        result.Alerts.Should().Contain(a => a.Type == "CPU" && a.Severity == "High");
        result.Alerts.Should().Contain(a => a.Type == "Memory" && a.Severity == "High");
        result.Alerts.Should().Contain(a => a.Type == "ResponseTime" && a.Severity == "High");
        result.Alerts.Should().Contain(a => a.Type == "ErrorRate" && a.Severity == "Critical");
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Diagnostics_WithValidData_ShouldReturnDiagnosticInfo()
    {
        // Arrange
        var performanceMetrics = new PerformanceMetrics
        {
            Id = ObjectId.GenerateNewId(),
            Timestamp = DateTime.UtcNow,
            CpuUsage = 45.5,
            MemoryUsage = 1024L * 1024 * 512,
            DiskUsage = 1024L * 1024 * 1024 * 10,
            NetworkUsage = 1024L * 1024 * 5,
            ResponseTime = 150.0,
            RequestCount = 1000,
            ErrorRate = 0.5
        };

        _mockPerformanceService.Setup(x => x.GetPerformanceMetricsAsync())
            .ReturnsAsync(performanceMetrics);

        // Act
        var result = await _systemHealthService.RunDiagnosticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.SystemInfo.Should().NotBeNull();
        result.PerformanceMetrics.Should().NotBeNull();
        result.HealthStatus.Should().NotBeNull();
        result.Recommendations.Should().NotBeEmpty();
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Maintenance_WithValidData_ShouldReturnMaintenanceInfo()
    {
        // Arrange
        var performanceMetrics = new PerformanceMetrics
        {
            Id = ObjectId.GenerateNewId(),
            Timestamp = DateTime.UtcNow,
            CpuUsage = 45.5,
            MemoryUsage = 1024L * 1024 * 512,
            DiskUsage = 1024L * 1024 * 1024 * 10,
            NetworkUsage = 1024L * 1024 * 5,
            ResponseTime = 150.0,
            RequestCount = 1000,
            ErrorRate = 0.5
        };

        _mockPerformanceService.Setup(x => x.GetPerformanceMetricsAsync())
            .ReturnsAsync(performanceMetrics);

        // Act
        var result = await _systemHealthService.GetMaintenanceInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result.LastMaintenance.Should().BeCloseTo(DateTime.UtcNow.AddDays(-1), TimeSpan.FromSeconds(5));
        result.NextMaintenance.Should().BeAfter(DateTime.UtcNow);
        result.MaintenanceTasks.Should().NotBeEmpty();
        result.SystemHealth.Should().NotBeNull();
        result.Recommendations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Recovery_WithValidData_ShouldReturnRecoveryInfo()
    {
        // Arrange
        var performanceMetrics = new PerformanceMetrics
        {
            Id = ObjectId.GenerateNewId(),
            Timestamp = DateTime.UtcNow,
            CpuUsage = 45.5,
            MemoryUsage = 1024L * 1024 * 512,
            DiskUsage = 1024L * 1024 * 1024 * 10,
            NetworkUsage = 1024L * 1024 * 5,
            ResponseTime = 150.0,
            RequestCount = 1000,
            ErrorRate = 0.5
        };

        _mockPerformanceService.Setup(x => x.GetPerformanceMetricsAsync())
            .ReturnsAsync(performanceMetrics);

        // Act
        var result = await _systemHealthService.GetRecoveryInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result.RecoveryStatus.Should().Be("Ready");
        result.BackupStatus.Should().Be("Available");
        result.RecoveryPoints.Should().NotBeEmpty();
        result.RecoveryTime.Should().BeGreaterThan(TimeSpan.Zero);
        result.LastBackup.Should().BeCloseTo(DateTime.UtcNow.AddHours(-6), TimeSpan.FromSeconds(5));
    }
}
