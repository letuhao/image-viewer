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
        result.HitRate.Should().Be(1.0); // Both entries are recent (within 24 hours)
        result.MissRate.Should().Be(0.0); // 1.0 - 1.0 = 0.0
        result.TotalHits.Should().Be(2); // Both entries are recent
        result.TotalMisses.Should().Be(0); // 2 - 2 = 0
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
        result.IsOptimized.Should().BeFalse(); // Has 1 pending job, so not optimized
        result.MaxConcurrentProcesses.Should().Be(4);
        result.QueueSize.Should().Be(1); // 1 pending job
        result.Status.Should().Be("Processing"); // Has active jobs
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
        result.SuccessRate.Should().BeApproximately(66.67, 0.1); // 2/3 = 66.666...
        result.AverageProcessingTime.Should().Be(TimeSpan.FromMilliseconds(2500));
        result.TotalProcessingTime.Should().Be(5000);
        result.LastProcessed.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.ProcessedByFormat.Should().NotBeEmpty();
        result.ProcessedBySize.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetDatabasePerformanceInfoAsync_ShouldReturnPerformanceInfo()
    {
        // Arrange
            var users = new List<User>
            {
                new User("test1@example.com", "Test User 1", "hash1"),
                new User("test2@example.com", "Test User 2", "hash2"),
                new User("test3@example.com", "Test User 3", "hash3"),
                new User("test4@example.com", "Test User 4", "hash4"),
                new User("test5@example.com", "Test User 5", "hash5"),
                new User("test6@example.com", "Test User 6", "hash6"),
                new User("test7@example.com", "Test User 7", "hash7"),
                new User("test8@example.com", "Test User 8", "hash8"),
                new User("test9@example.com", "Test User 9", "hash9"),
                new User("test10@example.com", "Test User 10", "hash10"),
                new User("test11@example.com", "Test User 11", "hash11"),
                new User("test12@example.com", "Test User 12", "hash12")
            };
        _mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(users);

        // Act
        var result = await _performanceService.GetDatabasePerformanceInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(ObjectId.Empty);
        result.IsOptimized.Should().BeTrue();
        result.ActiveConnections.Should().Be(12); // Min(12, 12) = 12
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
        // Arrange
        var users = new List<User>
        {
            new User("test1@example.com", "Test User 1", "hash1"),
            new User("test2@example.com", "Test User 2", "hash2"),
            new User("test3@example.com", "Test User 3", "hash3"),
            new User("test4@example.com", "Test User 4", "hash4"),
            new User("test5@example.com", "Test User 5", "hash5")
        };
        _mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(users);

        // Act
        var result = await _performanceService.GetDatabaseStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalQueries.Should().Be(5000); // 5 users * 1000
        result.SlowQueries.Should().Be(50); // 1% of 5000
        result.AverageQueryTime.Should().Be(50.0);
        result.TotalQueryTime.Should().Be(TimeSpan.FromMilliseconds(250000)); // 5000 * 50
        result.LastOptimized.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.QueriesByType.Should().NotBeEmpty();
        result.QueryTimesByType.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCDNInfoAsync_ShouldReturnCDNInfo()
    {
        // Arrange
        var users = new List<User>
        {
            new User("test1@example.com", "Test User 1", "hash1"),
            new User("test2@example.com", "Test User 2", "hash2"),
            new User("test3@example.com", "Test User 3", "hash3"),
            new User("test4@example.com", "Test User 4", "hash4"),
            new User("test5@example.com", "Test User 5", "hash5"),
            new User("test6@example.com", "Test User 6", "hash6"),
            new User("test7@example.com", "Test User 7", "hash7"),
            new User("test8@example.com", "Test User 8", "hash8"),
            new User("test9@example.com", "Test User 9", "hash9"),
            new User("test10@example.com", "Test User 10", "hash10"),
            new User("test11@example.com", "Test User 11", "hash11")
        };
        _mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(users);

        // Act
        var result = await _performanceService.GetCDNInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(ObjectId.Empty);
        result.Provider.Should().Be("AWS CloudFront"); // >10 users enables CDN
        result.Endpoint.Should().Be("cdn.example.com");
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
        // Arrange
        var users = new List<User>
        {
            new User("test1@example.com", "Test User 1", "hash1"),
            new User("test2@example.com", "Test User 2", "hash2")
        };
        _mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(users);

        // Act
        var result = await _performanceService.GetCDNStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalRequests.Should().Be(100000); // 2 users * 50000
        result.TotalBytesServed.Should().Be(102400000); // 100000 * 1024
        result.AverageResponseTime.Should().Be(150.0);
        result.CacheHitRate.Should().Be(0.85); // 85% cache hit rate
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
        // Arrange
        var users = new List<User>
        {
            new User("test1@example.com", "Test User 1", "hash1"),
            new User("test2@example.com", "Test User 2", "hash2")
        };
        _mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(users);

        // Act
        var result = await _performanceService.GetLazyLoadingStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalRequests.Should().Be(2000); // 2 users * 1000
        result.TotalPreloaded.Should().Be(1600); // 80% of 2000
        result.PreloadSuccessRate.Should().Be(0.8);
        result.AveragePreloadTime.Should().Be(TimeSpan.FromMilliseconds(250));
        result.LastPreload.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.PreloadedByType.Should().NotBeEmpty();
        result.PreloadTimesByType.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPerformanceMetricsAsync_ShouldReturnMetrics()
    {
        // Arrange
        var users = new List<User>
        {
            new User("test1@example.com", "Test User 1", "hash1"),
            new User("test2@example.com", "Test User 2", "hash2")
        };
        _mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(users);

        var performanceMetrics = new List<PerformanceMetric>
        {
            PerformanceMetric.Create("cpu", "Gauge", 25.5, "percent", "System")
        };
        _mockPerformanceMetricRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(performanceMetrics);

        // Act
        var result = await _performanceService.GetPerformanceMetricsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(ObjectId.Empty);
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.CpuUsage.Should().Be(25.5);
        result.MemoryUsage.Should().Be(200); // 2 users * 100
        result.DiskUsage.Should().Be(2000); // 2 users * 1000
        result.NetworkUsage.Should().Be(1000); // 2 users * 500
        result.ResponseTime.Should().Be(150.0);
        result.RequestCount.Should().Be(200); // 2 users * 100
        result.ErrorRate.Should().Be(0.02); // 2% error rate
        result.CustomMetrics.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPerformanceMetricsByTimeRangeAsync_ShouldReturnMetrics()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;

        var users = new List<User>
        {
            new User("test1@example.com", "Test User 1", "hash1"),
            new User("test2@example.com", "Test User 2", "hash2")
        };
        _mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(users);

        var metric1 = PerformanceMetric.Create("cpu", "Gauge", 25.5, "percent", "System");
        var metric2 = PerformanceMetric.Create("cpu", "Gauge", 30.0, "percent", "System");
        
        // Set the SampledAt to be within the time range
        var midTime = startDate.AddDays((endDate - startDate).TotalDays / 2);
        metric1.GetType().GetProperty("SampledAt")?.SetValue(metric1, midTime);
        metric2.GetType().GetProperty("SampledAt")?.SetValue(metric2, midTime.AddHours(1));
        
        var performanceMetrics = new List<PerformanceMetric> { metric1, metric2 };
        _mockPerformanceMetricRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(performanceMetrics);

        // Act
        var result = await _performanceService.GetPerformanceMetricsByTimeRangeAsync(startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(ObjectId.Empty);
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.CpuUsage.Should().Be(27.75); // Average of 25.5 and 30.0
        result.MemoryUsage.Should().Be(200); // 2 users * 100
        result.DiskUsage.Should().Be(2000); // 2 users * 1000
        result.NetworkUsage.Should().Be(1000); // 2 users * 500
        result.ResponseTime.Should().Be(175.0); // Average of 150.0 and 200.0
        result.RequestCount.Should().Be(200); // 2 users * 100
        result.ErrorRate.Should().Be(0.02); // 2% error rate
        result.CustomMetrics.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GeneratePerformanceReportAsync_ShouldReturnReport()
    {
        // Arrange
        var users = new List<User>
        {
            new User("test1@example.com", "Test User 1", "hash1"),
            new User("test2@example.com", "Test User 2", "hash2")
        };
        _mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(users);

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
        result.Summary.AverageMemoryUsage.Should().Be(200); // 2 users * 100
        result.Summary.AverageDiskUsage.Should().Be(2000); // 2 users * 1000
        result.Summary.AverageNetworkUsage.Should().Be(1000); // 2 users * 500
        result.Summary.AverageResponseTime.Should().Be(150.0);
        result.Summary.TotalRequests.Should().Be(2000); // 2 users * 1000
        result.Summary.AverageErrorRate.Should().Be(0.02); // 2% error rate
        result.Summary.OverallStatus.Should().Be("Good");
        result.Metrics.Should().NotBeEmpty();
        result.Recommendations.Should().NotBeEmpty();
    }
}