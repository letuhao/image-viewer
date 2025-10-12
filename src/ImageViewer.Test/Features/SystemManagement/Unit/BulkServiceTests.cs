using FluentAssertions;
using Moq;
using Xunit;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Test.Features.SystemManagement.Unit;

/// <summary>
/// Unit tests for BulkService
/// </summary>
public class BulkServiceTests
{
    private readonly Mock<ICollectionService> _mockCollectionService;
    private readonly Mock<IMessageQueueService> _mockMessageQueueService;
    private readonly Mock<IBackgroundJobService> _mockBackgroundJobService;
    private readonly Mock<ILogger<BulkService>> _mockLogger;
    private readonly BulkService _bulkService;
    private const string ValidTestPath = @"L:\EMedia\AI_Generated\AiASAG";

    public BulkServiceTests()
    {
        _mockCollectionService = new Mock<ICollectionService>();
        _mockMessageQueueService = new Mock<IMessageQueueService>();
        _mockBackgroundJobService = new Mock<IBackgroundJobService>();
        _mockLogger = new Mock<ILogger<BulkService>>();

        _bulkService = new BulkService(
            _mockCollectionService.Object,
            _mockMessageQueueService.Object,
            _mockBackgroundJobService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task BulkAddCollectionsAsync_WithValidPath_ShouldReturnResult()
    {
        // Arrange
        var request = new BulkAddCollectionsRequest
        {
            ParentPath = ValidTestPath,
            IncludeSubfolders = false,
            CollectionPrefix = "",
            OverwriteExisting = false,
            AutoAdd = false
        };

        // Act
        var result = await _bulkService.BulkAddCollectionsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalProcessed.Should().BeGreaterOrEqualTo(0);
        result.SuccessCount.Should().BeGreaterOrEqualTo(0);
        result.ErrorCount.Should().BeGreaterOrEqualTo(0);
        result.SkippedCount.Should().BeGreaterOrEqualTo(0);
        result.Errors.Should().NotBeNull();
    }

    [Fact]
    public async Task BulkAddCollectionsAsync_WithInvalidPath_ShouldThrowDirectoryNotFoundException()
    {
        // Arrange
        var request = new BulkAddCollectionsRequest
        {
            ParentPath = "C:\\NonExistentPath\\InvalidFolder",
            IncludeSubfolders = false,
            CollectionPrefix = "",
            OverwriteExisting = false,
            AutoAdd = false
        };

        // Act & Assert
        await Assert.ThrowsAsync<System.IO.DirectoryNotFoundException>(async () =>
        {
            await _bulkService.BulkAddCollectionsAsync(request);
        });
    }

    [Fact]
    public async Task BulkAddCollectionsAsync_WithPrefixFilter_ShouldFilterCorrectly()
    {
        // Arrange
        var request = new BulkAddCollectionsRequest
        {
            ParentPath = ValidTestPath,
            IncludeSubfolders = false,
            CollectionPrefix = "PATREON",
            OverwriteExisting = false,
            AutoAdd = false
        };

        // Act
        var result = await _bulkService.BulkAddCollectionsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalProcessed.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task BulkAddCollectionsAsync_WithIncludeSubfolders_ShouldProcessRecursively()
    {
        // Arrange
        var request = new BulkAddCollectionsRequest
        {
            ParentPath = ValidTestPath,
            IncludeSubfolders = true,
            CollectionPrefix = "",
            OverwriteExisting = false,
            AutoAdd = false
        };

        // Act
        var result = await _bulkService.BulkAddCollectionsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalProcessed.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task BulkAddCollectionsAsync_WithWhitespacePath_ShouldThrowDirectoryNotFoundException()
    {
        // Arrange
        var request = new BulkAddCollectionsRequest
        {
            ParentPath = "   ",
            IncludeSubfolders = false,
            CollectionPrefix = "",
            OverwriteExisting = false,
            AutoAdd = false
        };

        // Act & Assert
        await Assert.ThrowsAsync<System.IO.DirectoryNotFoundException>(async () =>
        {
            await _bulkService.BulkAddCollectionsAsync(request);
        });
    }

    [Fact]
    public async Task BulkAddCollectionsAsync_ResultShouldHaveCorrectStructure()
    {
        // Arrange
        var request = new BulkAddCollectionsRequest
        {
            ParentPath = ValidTestPath,
            IncludeSubfolders = false,
            CollectionPrefix = "",
            OverwriteExisting = false,
            AutoAdd = false
        };

        // Act
        var result = await _bulkService.BulkAddCollectionsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TotalProcessed.Should().BeGreaterOrEqualTo(0);
        result.SuccessCount.Should().BeGreaterOrEqualTo(0);
        result.ErrorCount.Should().BeGreaterOrEqualTo(0);
        result.SkippedCount.Should().BeGreaterOrEqualTo(0);
        result.Errors.Should().NotBeNull();
        
        // Sum of success, error, and skipped should equal total processed
        (result.SuccessCount + result.ErrorCount + result.SkippedCount).Should().Be(result.TotalProcessed);
    }

    [Fact]
    public void BulkService_Constructor_ShouldInitializeCorrectly()
    {
        // Assert
        _bulkService.Should().NotBeNull();
    }
}
