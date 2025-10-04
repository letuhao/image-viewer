using FluentAssertions;
using ImageViewer.Api.Controllers;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.Statistics;
using ImageViewer.Tests.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MongoDB.Bson;

namespace ImageViewer.Tests.Api.Controllers;

public class StatisticsControllerTests : TestBase
{
    private readonly Mock<IStatisticsService> _statisticsServiceMock;
    private readonly Mock<ILogger<StatisticsController>> _loggerMock;
    private readonly StatisticsController _controller;

    public StatisticsControllerTests()
    {
        _statisticsServiceMock = CreateMock<IStatisticsService>();
        _loggerMock = CreateMock<ILogger<StatisticsController>>();
        _controller = new StatisticsController(_statisticsServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetOverallStatistics_ShouldReturnOkResult()
    {
        // Arrange
        var expectedStatistics = new SystemStatisticsDto
        {
            TotalCollections = 10,
            TotalImages = 1000,
            TotalSize = 5000000,
            TotalCacheSize = 2000000,
            TotalViewSessions = 50,
            TotalViewTime = 3600.0,
            AverageImagesPerCollection = 100.0,
            AverageViewTimePerSession = 72.0
        };
        _statisticsServiceMock
            .Setup(x => x.GetSystemStatisticsAsync())
            .ReturnsAsync(expectedStatistics);

        // Act
        var result = await _controller.GetOverallStatistics();

        // Assert
        result.Should().BeOfType<ActionResult<SystemStatisticsDto>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var statistics = okResult!.Value as SystemStatisticsDto;
        statistics.Should().NotBeNull();
        statistics!.TotalCollections.Should().Be(10);
        statistics.TotalImages.Should().Be(1000);
        statistics.TotalSize.Should().Be(5000000);
    }

    [Fact]
    public async Task GetOverallStatistics_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        _statisticsServiceMock
            .Setup(x => x.GetSystemStatisticsAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetOverallStatistics();

        // Assert
        result.Should().BeOfType<ActionResult<SystemStatisticsDto>>();
        var statusResult = result.Result as ObjectResult;
        statusResult.Should().NotBeNull();
        statusResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetCollectionStatistics_WithValidCollectionId_ShouldReturnOkResult()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var expectedStatistics = new CollectionStatisticsDto
        {
            CollectionId = collectionId,
            ViewCount = 25,
            TotalViewTime = 1800.0,
            SearchCount = 5,
            LastViewed = DateTime.UtcNow,
            LastSearched = DateTime.UtcNow.AddDays(-1),
            AverageViewTime = 72.0,
            TotalImages = 100,
            TotalSize = 1000000,
            AverageFileSize = 10000,
            CachedImages = 85,
            CachePercentage = 85.0
        };
        _statisticsServiceMock
            .Setup(x => x.GetCollectionStatisticsAsync(collectionId))
            .ReturnsAsync(expectedStatistics);

        // Act
        var result = await _controller.GetCollectionStatistics(collectionId);

        // Assert
        result.Should().BeOfType<ActionResult<CollectionStatisticsDto>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var statistics = okResult!.Value as CollectionStatisticsDto;
        statistics.Should().NotBeNull();
        statistics!.CollectionId.Should().Be(collectionId);
        statistics.TotalImages.Should().Be(100);
        statistics.TotalSize.Should().Be(1000000);
    }

    [Fact]
    public async Task GetCollectionStatistics_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        _statisticsServiceMock
            .Setup(x => x.GetCollectionStatisticsAsync(collectionId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetCollectionStatistics(collectionId);

        // Assert
        result.Should().BeOfType<ActionResult<CollectionStatisticsDto>>();
        var statusResult = result.Result as ObjectResult;
        statusResult.Should().NotBeNull();
        statusResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetImageStatistics_WithValidImageId_ShouldReturnOkResult()
    {
        // Arrange
        var imageId = ObjectId.GenerateNewId();
        var expectedStatistics = new ImageStatisticsDto
        {
            TotalImages = 1,
            TotalSize = 50000,
            AverageFileSize = 50000,
            CachedImages = 1,
            CachePercentage = 100.0,
            FormatStatistics = new List<FormatStatisticsDto>
            {
                new() { Format = "jpg", Count = 1, TotalSize = 50000, AverageSize = 50000.0 }
            }
        };
        _statisticsServiceMock
            .Setup(x => x.GetImageStatisticsAsync(imageId))
            .ReturnsAsync(expectedStatistics);

        // Act
        var result = await _controller.GetImageStatistics(imageId);

        // Assert
        result.Should().BeOfType<ActionResult<ImageStatisticsDto>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var statistics = okResult!.Value as ImageStatisticsDto;
        statistics.Should().NotBeNull();
        statistics!.TotalImages.Should().Be(1);
        statistics.TotalSize.Should().Be(50000);
        statistics.CachePercentage.Should().Be(100.0);
    }

    [Fact]
    public async Task GetImageStatistics_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var imageId = ObjectId.GenerateNewId();
        _statisticsServiceMock
            .Setup(x => x.GetImageStatisticsAsync(imageId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetImageStatistics(imageId);

        // Assert
        result.Should().BeOfType<ActionResult<ImageStatisticsDto>>();
        var statusResult = result.Result as ObjectResult;
        statusResult.Should().NotBeNull();
        statusResult!.StatusCode.Should().Be(500);
    }
}
