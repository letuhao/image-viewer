using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Application.Options;
using MongoDB.Bson;

namespace ImageViewer.Test.Features.Cache.Unit;

/// <summary>
/// Unit tests for optimized cache statistics aggregation
/// 缓存统计聚合优化单元测试 - Kiểm thử đơn vị tối ưu thống kê cache
/// </summary>
public class CacheStatisticsAggregationTests
{
    private readonly Mock<ICacheFolderRepository> _mockCacheFolderRepository;
    private readonly Mock<ICollectionRepository> _mockCollectionRepository;
    private readonly Mock<ILogger<CacheService>> _mockLogger;
    private readonly CacheService _service;

    public CacheStatisticsAggregationTests()
    {
        _mockCacheFolderRepository = new Mock<ICacheFolderRepository>();
        _mockCollectionRepository = new Mock<ICollectionRepository>();
        _mockLogger = new Mock<ILogger<CacheService>>();

        var options = Options.Create(new ImageSizeOptions
        {
            ThumbnailWidth = 300,
            ThumbnailHeight = 300,
            CacheWidth = 1920,
            CacheHeight = 1080
        });

        _service = new CacheService(
            _mockCacheFolderRepository.Object,
            _mockCollectionRepository.Object,
            _mockLogger.Object,
            options
        );
    }

    [Fact]
    public async Task GetCacheStatistics_UsesAggregationPipeline()
    {
        // Arrange
        _mockCacheFolderRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<CacheFolder>());

        // Setup aggregation to return statistics
        _mockCollectionRepository
            .Setup(x => x.GetCacheStatisticsAsync())
            .ReturnsAsync((
                totalImages: 1000,
                cachedImages: 750,
                totalCacheSize: 5000000000L, // 5GB
                collectionsWithCache: 50
            ));

        _mockCollectionRepository
            .Setup(x => x.GetActiveCollectionCountAsync())
            .ReturnsAsync(100);

        // Act
        var result = await _service.GetCacheStatisticsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Summary);
        Assert.Equal(100, result.Summary.TotalCollections);
        Assert.Equal(50, result.Summary.CollectionsWithCache);
        Assert.Equal(1000, result.Summary.TotalImages);
        Assert.Equal(750, result.Summary.CachedImages);
        Assert.Equal(5000000000L, result.Summary.TotalCacheSize);
        Assert.Equal(75.0, result.Summary.CachePercentage); // 750/1000 = 75%

        // Verify aggregation was called (not GetAllAsync for iteration)
        _mockCollectionRepository.Verify(x => x.GetCacheStatisticsAsync(), Times.Once);
        _mockCollectionRepository.Verify(x => x.GetAllAsync(), Times.Never, 
            "Should use aggregation, not fetch all collections");
    }

    [Fact]
    public async Task GetCacheStatistics_WhenNoImages_ReturnsZeroPercentage()
    {
        // Arrange
        _mockCacheFolderRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<CacheFolder>());

        _mockCollectionRepository
            .Setup(x => x.GetCacheStatisticsAsync())
            .ReturnsAsync((0, 0, 0L, 0));

        _mockCollectionRepository
            .Setup(x => x.GetActiveCollectionCountAsync())
            .ReturnsAsync(0);

        // Act
        var result = await _service.GetCacheStatisticsAsync();

        // Assert
        Assert.Equal(0, result.Summary.TotalImages);
        Assert.Equal(0, result.Summary.CachedImages);
        Assert.Equal(0.0, result.Summary.CachePercentage); // Avoid division by zero
    }

    [Fact]
    public async Task GetCacheStatistics_CalculatesCorrectPercentage()
    {
        // Arrange
        _mockCacheFolderRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<CacheFolder>());

        // Test various scenarios
        var testCases = new[]
        {
            (total: 100, cached: 50, expected: 50.0),
            (total: 1000, cached: 333, expected: 33.3),
            (total: 500, cached: 500, expected: 100.0),
            (total: 750, cached: 0, expected: 0.0)
        };

        foreach (var testCase in testCases)
        {
            _mockCollectionRepository
                .Setup(x => x.GetCacheStatisticsAsync())
                .ReturnsAsync((testCase.total, testCase.cached, 0L, 0));

            _mockCollectionRepository
                .Setup(x => x.GetActiveCollectionCountAsync())
                .ReturnsAsync(10);

            // Act
            var result = await _service.GetCacheStatisticsAsync();

            // Assert
            Assert.Equal(testCase.expected, result.Summary.CachePercentage, precision: 1);
        }
    }

}

