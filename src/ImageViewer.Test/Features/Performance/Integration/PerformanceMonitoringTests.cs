using ImageViewer.Application.Services;
using ImageViewer.Test.Shared.Fixtures;
using System.Diagnostics;

namespace ImageViewer.Test.Features.Performance.Integration;

/// <summary>
/// Integration tests for Performance Monitoring - End-to-end performance monitoring scenarios
/// </summary>
[Collection("Integration")]
public class PerformanceMonitoringTests : IClassFixture<BasicPerformanceIntegrationTestFixture>
{
    private readonly BasicPerformanceIntegrationTestFixture _fixture;
    private readonly IPerformanceService _performanceService;

    public PerformanceMonitoringTests(BasicPerformanceIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _performanceService = _fixture.GetService<IPerformanceService>();
    }

    [Fact]
    public async Task PerformanceMonitoring_GetPerformanceMetrics_ShouldReturnMetricsEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var metrics = await _performanceService.GetPerformanceMetricsAsync();

        // Assert
        stopwatch.Stop();
        metrics.Should().NotBeNull();
        metrics.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Should complete within 2 seconds
    }

    [Fact]
    public async Task PerformanceMonitoring_GetPerformanceMetricsByTimeRange_ShouldReturnMetricsEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var fromDate = DateTime.UtcNow.AddDays(-1);
        var toDate = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var metrics = await _performanceService.GetPerformanceMetricsByTimeRangeAsync(fromDate, toDate);

        // Assert
        stopwatch.Stop();
        metrics.Should().NotBeNull();
        metrics.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000); // Should complete within 3 seconds
    }

    [Fact]
    public async Task PerformanceMonitoring_GeneratePerformanceReport_ShouldGenerateReportEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var report = await _performanceService.GeneratePerformanceReportAsync(fromDate, toDate);

        // Assert
        stopwatch.Stop();
        report.Should().NotBeNull();
        report.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        report.FromDate.Should().Be(fromDate);
        report.ToDate.Should().Be(toDate);
        report.Summary.Should().NotBeNull();
        report.Metrics.Should().NotBeNull();
        report.Recommendations.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
    }

    [Fact]
    public async Task PerformanceMonitoring_GetCacheInfo_ShouldReturnCacheInfoEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var cacheInfo = await _performanceService.GetCacheInfoAsync();

        // Assert
        stopwatch.Stop();
        cacheInfo.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }

    [Fact]
    public async Task PerformanceMonitoring_GetCacheStatistics_ShouldReturnStatisticsEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var statistics = await _performanceService.GetCacheStatisticsAsync();

        // Assert
        stopwatch.Stop();
        statistics.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }

    [Fact]
    public async Task PerformanceMonitoring_GetImageProcessingInfo_ShouldReturnInfoEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var info = await _performanceService.GetImageProcessingInfoAsync();

        // Assert
        stopwatch.Stop();
        info.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }

    [Fact]
    public async Task PerformanceMonitoring_GetImageProcessingStatistics_ShouldReturnStatisticsEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var statistics = await _performanceService.GetImageProcessingStatisticsAsync();

        // Assert
        stopwatch.Stop();
        statistics.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }

    [Fact]
    public async Task PerformanceMonitoring_GetDatabasePerformanceInfo_ShouldReturnInfoEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var info = await _performanceService.GetDatabasePerformanceInfoAsync();

        // Assert
        stopwatch.Stop();
        info.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Should complete within 2 seconds
    }

    [Fact]
    public async Task PerformanceMonitoring_GetDatabaseStatistics_ShouldReturnStatisticsEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var statistics = await _performanceService.GetDatabaseStatisticsAsync();

        // Assert
        stopwatch.Stop();
        statistics.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Should complete within 2 seconds
    }

    [Fact]
    public async Task PerformanceMonitoring_GetCDNInfo_ShouldReturnInfoEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var info = await _performanceService.GetCDNInfoAsync();

        // Assert
        stopwatch.Stop();
        info.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }

    [Fact]
    public async Task PerformanceMonitoring_GetCDNStatistics_ShouldReturnStatisticsEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var statistics = await _performanceService.GetCDNStatisticsAsync();

        // Assert
        stopwatch.Stop();
        statistics.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }

    [Fact]
    public async Task PerformanceMonitoring_GetLazyLoadingInfo_ShouldReturnInfoEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var info = await _performanceService.GetLazyLoadingInfoAsync();

        // Assert
        stopwatch.Stop();
        info.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }

    [Fact]
    public async Task PerformanceMonitoring_GetLazyLoadingStatistics_ShouldReturnStatisticsEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var statistics = await _performanceService.GetLazyLoadingStatisticsAsync();

        // Assert
        stopwatch.Stop();
        statistics.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }

    [Fact]
    public async Task PerformanceMonitoring_OptimizeCache_ShouldOptimizeEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _performanceService.OptimizeCacheAsync();

        // Assert
        stopwatch.Stop();
        result.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
    }

    [Fact]
    public async Task PerformanceMonitoring_OptimizeImageProcessing_ShouldOptimizeEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _performanceService.OptimizeImageProcessingAsync();

        // Assert
        stopwatch.Stop();
        result.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000); // Should complete within 3 seconds
    }

    [Fact]
    public async Task PerformanceMonitoring_OptimizeDatabaseQueries_ShouldOptimizeEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _performanceService.OptimizeDatabaseQueriesAsync();

        // Assert
        stopwatch.Stop();
        result.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
    }
}