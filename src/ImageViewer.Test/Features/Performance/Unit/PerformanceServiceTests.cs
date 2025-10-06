using FluentAssertions;
using Moq;
using Xunit;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Test.Features.Performance.Unit;

public class PerformanceServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPerformanceMetricRepository> _mockPerformanceMetricRepository;
    private readonly Mock<ICacheInfoRepository> _mockCacheInfoRepository;
    private readonly Mock<IMediaProcessingJobRepository> _mockMediaProcessingJobRepository;
    private readonly Mock<ILogger<PerformanceService>> _mockLogger;
    private readonly PerformanceService _performanceService;

    public PerformanceServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPerformanceMetricRepository = new Mock<IPerformanceMetricRepository>();
        _mockCacheInfoRepository = new Mock<ICacheInfoRepository>();
        _mockMediaProcessingJobRepository = new Mock<IMediaProcessingJobRepository>();
        _mockLogger = new Mock<ILogger<PerformanceService>>();
        _performanceService = new PerformanceService(
            _mockUserRepository.Object,
            _mockPerformanceMetricRepository.Object,
            _mockCacheInfoRepository.Object,
            _mockMediaProcessingJobRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetCacheInfoAsync_ShouldReturnCacheInfo()
    {
        // Arrange
        var cacheEntries = new List<ImageCacheInfo>
        {
            new ImageCacheInfo(ObjectId.GenerateNewId(), "/cache/path1", "1280x720", 1024L, DateTime.UtcNow.AddDays(1)),
            new ImageCacheInfo(ObjectId.GenerateNewId(), "/cache/path2", "1280x720", 2048L, DateTime.UtcNow.AddDays(1))
        };

        _mockCacheInfoRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(cacheEntries);

        // Act
        var result = await _performanceService.GetCacheInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(CacheType.System);
        result.Size.Should().Be(3072); // 1024 + 2048
        result.ItemCount.Should().Be(2);
        result.IsOptimized.Should().BeTrue();
        result.Status.Should().Be("Active");
    }

    [Fact]
    public async Task ClearCacheAsync_ShouldReturnClearedCacheInfo()
    {
        // Arrange
        var cacheEntries = new List<ImageCacheInfo>
        {
            new ImageCacheInfo(ObjectId.GenerateNewId(), "/cache/path1", "1280x720", 1024L, DateTime.UtcNow.AddDays(1))
        };

        _mockCacheInfoRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(cacheEntries);

        // Act
        var result = await _performanceService.ClearCacheAsync();

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Cleared");
        result.Size.Should().Be(0);
        result.ItemCount.Should().Be(0);
    }

    [Fact]
    public async Task OptimizeCacheAsync_ShouldReturnOptimizedCacheInfo()
    {
        // Arrange
        var cacheEntries = new List<ImageCacheInfo>
        {
            new ImageCacheInfo(ObjectId.GenerateNewId(), "/cache/path1", "1280x720", 1024L, DateTime.UtcNow.AddDays(1))
        };

        _mockCacheInfoRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(cacheEntries);

        // Act
        var result = await _performanceService.OptimizeCacheAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsOptimized.Should().BeTrue();
        result.Status.Should().Be("Optimized");
    }

    [Fact]
    public async Task GetCacheStatisticsAsync_ShouldReturnStatistics()
    {
        // Arrange
        var cacheEntries = new List<ImageCacheInfo>
        {
            new ImageCacheInfo(ObjectId.GenerateNewId(), "/cache/path1", "1280x720", 1024L, DateTime.UtcNow.AddDays(1)),
            new ImageCacheInfo(ObjectId.GenerateNewId(), "/cache/path2", "1280x720", 2048L, DateTime.UtcNow.AddDays(1))
        };

        _mockCacheInfoRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(cacheEntries);

        // Act
        var result = await _performanceService.GetCacheStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalSize.Should().Be(3072);
        result.TotalItems.Should().Be(2);
        result.HitRate.Should().Be(85.5);
        result.MissRate.Should().Be(14.5);
        result.TotalHits.Should().Be(1000);
        result.TotalMisses.Should().Be(150);
    }

    [Fact]
    public async Task GetImageProcessingInfoAsync_ShouldReturnProcessingInfo()
    {
        // Arrange
        var processingJobs = new List<MediaProcessingJob>
        {
            MediaProcessingJob.Create("Thumbnail Job", "thumbnail"),
            MediaProcessingJob.Create("Resize Job", "resize")
        };
        processingJobs[0].Schedule(DateTime.UtcNow.AddMinutes(5));
        processingJobs[1].Start();
        processingJobs[1].Complete();

        _mockMediaProcessingJobRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(processingJobs);

        // Act
        var result = await _performanceService.GetImageProcessingInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(ObjectId.Empty);
        result.IsOptimized.Should().BeTrue();
        result.MaxConcurrentProcesses.Should().Be(4);
        result.QueueSize.Should().Be(1); // 1 pending job
        result.Status.Should().Be("Active");
        result.SupportedFormats.Should().NotBeEmpty();
        result.OptimizationSettings.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OptimizeImageProcessingAsync_ShouldReturnOptimizedInfo()
    {
        // Arrange
        var processingJobs = new List<MediaProcessingJob>
        {
            MediaProcessingJob.Create("Thumbnail Job", "thumbnail")
        };
        processingJobs[0].Schedule(DateTime.UtcNow.AddMinutes(5));

        _mockMediaProcessingJobRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(processingJobs);

        // Act
        var result = await _performanceService.OptimizeImageProcessingAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsOptimized.Should().BeTrue();
        result.Status.Should().Be("Optimized");
        result.LastOptimized.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetImageProcessingStatisticsAsync_ShouldReturnStatistics()
    {
        // Arrange
        var processingJobs = new List<MediaProcessingJob>
        {
            MediaProcessingJob.Create("Thumbnail Job 1", "thumbnail"),
            MediaProcessingJob.Create("Resize Job", "resize"),
            MediaProcessingJob.Create("Thumbnail Job 2", "thumbnail")
        };
        processingJobs[0].Start();
        processingJobs[0].Complete();
        processingJobs[1].Start();
        processingJobs[1].Complete();
        processingJobs[2].Start();
        processingJobs[2].Fail("Processing failed");

        _mockMediaProcessingJobRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(processingJobs);

        // Act
        var result = await _performanceService.GetImageProcessingStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalProcessed.Should().Be(2);
        result.TotalFailed.Should().Be(1);
        result.SuccessRate.Should().Be(66.7);
        result.AverageProcessingTime.Should().Be(TimeSpan.FromMilliseconds(2500));
        result.TotalProcessingTime.Should().Be(5000);
        result.LastProcessed.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.ProcessedByFormat.Should().NotBeEmpty();
        result.ProcessedBySize.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetDatabasePerformanceInfoAsync_ShouldReturnPerformanceInfo()
    {
        // Act
        var result = await _performanceService.GetDatabasePerformanceInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(ObjectId.Empty);
        result.IsOptimized.Should().BeTrue();
        result.ActiveConnections.Should().Be(12);
        result.MaxConnections.Should().Be(100);
        result.Status.Should().Be("Active");
        result.LastOptimized.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.OptimizedQueries.Should().NotBeEmpty();
        result.Indexes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OptimizeDatabaseQueriesAsync_ShouldReturnOptimizedInfo()
    {
        // Act
        var result = await _performanceService.OptimizeDatabaseQueriesAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsOptimized.Should().BeTrue();
        result.Status.Should().Be("Optimized");
        result.LastOptimized.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetDatabaseStatisticsAsync_ShouldReturnStatistics()
    {
        // Act
        var result = await _performanceService.GetDatabaseStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalQueries.Should().Be(10000);
        result.SlowQueries.Should().Be(25);
        result.AverageQueryTime.Should().Be(15.5);
        result.TotalQueryTime.Should().Be(TimeSpan.FromMilliseconds(155000));
        result.LastOptimized.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.QueriesByType.Should().NotBeEmpty();
        result.QueryTimesByType.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCDNInfoAsync_ShouldReturnCDNInfo()
    {
        // Act
        var result = await _performanceService.GetCDNInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(ObjectId.Empty);
        result.Provider.Should().Be("AWS CloudFront");
        result.Endpoint.Should().Be("https://cdn.example.com");
        result.Region.Should().Be("us-east-1");
        result.Bucket.Should().Be("imageviewer-cdn");
        result.IsEnabled.Should().BeTrue();
        result.EnableCompression.Should().BeTrue();
        result.EnableCaching.Should().BeTrue();
        result.CacheExpiration.Should().Be(3600);
        result.AllowedFileTypes.Should().NotBeEmpty();
        result.Status.Should().Be("Active");
        result.LastConfigured.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ConfigureCDNAsync_ShouldReturnConfiguredCDNInfo()
    {
        // Arrange
        var cdnConfig = new CDNConfigurationRequest
        {
            Provider = "AWS CloudFront",
            Endpoint = "https://cdn.example.com",
            AccessKey = "access-key",
            SecretKey = "secret-key",
            Region = "us-east-1",
            Bucket = "imageviewer-cdn",
            EnableCompression = true,
            EnableCaching = true,
            CacheExpiration = 3600,
            AllowedFileTypes = new List<string> { "jpg", "png", "gif" }
        };

        // Act
        var result = await _performanceService.ConfigureCDNAsync(cdnConfig);

        // Assert
        result.Should().NotBeNull();
        result.Provider.Should().Be("AWS CloudFront");
        result.Endpoint.Should().Be("https://cdn.example.com");
        result.Region.Should().Be("us-east-1");
        result.Bucket.Should().Be("imageviewer-cdn");
        result.IsEnabled.Should().BeTrue();
        result.EnableCompression.Should().BeTrue();
        result.EnableCaching.Should().BeTrue();
        result.CacheExpiration.Should().Be(3600);
        result.AllowedFileTypes.Should().HaveCount(3);
        result.Status.Should().Be("Configured");
        result.LastConfigured.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetCDNStatisticsAsync_ShouldReturnStatistics()
    {
        // Act
        var result = await _performanceService.GetCDNStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalRequests.Should().Be(50000);
        result.TotalBytesServed.Should().Be(1024000000);
        result.AverageResponseTime.Should().Be(150.5);
        result.CacheHitRate.Should().Be(88.5);
        result.LastRequest.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.RequestsByFileType.Should().NotBeEmpty();
        result.RequestsByRegion.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetLazyLoadingInfoAsync_ShouldReturnLazyLoadingInfo()
    {
        // Act
        var result = await _performanceService.GetLazyLoadingInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(ObjectId.Empty);
        result.IsEnabled.Should().BeTrue();
        result.BatchSize.Should().Be(20);
        result.PreloadCount.Should().Be(5);
        result.MaxConcurrentRequests.Should().Be(3);
        result.EnableImagePreloading.Should().BeTrue();
        result.EnableMetadataPreloading.Should().BeTrue();
        result.PreloadTimeout.Should().Be(5000);
        result.Status.Should().Be("Active");
        result.LastConfigured.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ConfigureLazyLoadingAsync_ShouldReturnConfiguredInfo()
    {
        // Arrange
        var lazyLoadingConfig = new LazyLoadingConfigurationRequest
        {
            EnableLazyLoading = true,
            BatchSize = 20,
            PreloadCount = 5,
            MaxConcurrentRequests = 3,
            EnableImagePreloading = true,
            EnableMetadataPreloading = true,
            PreloadTimeout = 5000
        };

        // Act
        var result = await _performanceService.ConfigureLazyLoadingAsync(lazyLoadingConfig);

        // Assert
        result.Should().NotBeNull();
        result.IsEnabled.Should().BeTrue();
        result.BatchSize.Should().Be(20);
        result.PreloadCount.Should().Be(5);
        result.MaxConcurrentRequests.Should().Be(3);
        result.EnableImagePreloading.Should().BeTrue();
        result.EnableMetadataPreloading.Should().BeTrue();
        result.PreloadTimeout.Should().Be(5000);
        result.Status.Should().Be("Configured");
        result.LastConfigured.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetLazyLoadingStatisticsAsync_ShouldReturnStatistics()
    {
        // Act
        var result = await _performanceService.GetLazyLoadingStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalRequests.Should().Be(1000);
        result.TotalPreloaded.Should().Be(950);
        result.PreloadSuccessRate.Should().Be(95.0);
        result.AveragePreloadTime.Should().Be(TimeSpan.FromMilliseconds(100));
        result.LastPreload.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.PreloadedByType.Should().NotBeEmpty();
        result.PreloadTimesByType.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPerformanceMetricsAsync_ShouldReturnMetrics()
    {
        // Act
        var result = await _performanceService.GetPerformanceMetricsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(ObjectId.Empty);
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.CpuUsage.Should().Be(25.5);
        result.MemoryUsage.Should().Be(512000000);
        result.DiskUsage.Should().Be(1024000000);
        result.NetworkUsage.Should().Be(256000000);
        result.ResponseTime.Should().Be(150.5);
        result.RequestCount.Should().Be(1000);
        result.ErrorRate.Should().Be(0.5);
        result.CustomMetrics.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPerformanceMetricsByTimeRangeAsync_ShouldReturnMetrics()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;

        // Act
        var result = await _performanceService.GetPerformanceMetricsByTimeRangeAsync(startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(ObjectId.Empty);
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.CpuUsage.Should().Be(25.5);
        result.MemoryUsage.Should().Be(512000000);
        result.DiskUsage.Should().Be(1024000000);
        result.NetworkUsage.Should().Be(256000000);
        result.ResponseTime.Should().Be(150.5);
        result.RequestCount.Should().Be(1000);
        result.ErrorRate.Should().Be(0.5);
        result.CustomMetrics.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePerformanceReportAsync_ShouldReturnReport()
    {
        // Act
        var result = await _performanceService.GeneratePerformanceReportAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(ObjectId.Empty);
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.FromDate.Should().BeCloseTo(DateTime.UtcNow.AddDays(-7), TimeSpan.FromSeconds(1));
        result.ToDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.Summary.Should().NotBeNull();
        result.Summary.AverageCpuUsage.Should().Be(25.5);
        result.Summary.AverageMemoryUsage.Should().Be(512000000);
        result.Summary.AverageDiskUsage.Should().Be(1024000000);
        result.Summary.AverageNetworkUsage.Should().Be(256000000);
        result.Summary.AverageResponseTime.Should().Be(150.5);
        result.Summary.TotalRequests.Should().Be(10000);
        result.Summary.AverageErrorRate.Should().Be(0.5);
        result.Summary.OverallStatus.Should().Be("Good");
        result.Metrics.Should().NotBeEmpty();
        result.Recommendations.Should().NotBeEmpty();
    }
}