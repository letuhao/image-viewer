using Xunit;
using Moq;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Enums;
using ImageViewer.Application.DTOs.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace ImageViewer.Test.Features.Collections.Unit;

/// <summary>
/// Unit tests for collection navigation features
/// </summary>
public class CollectionNavigationTests
{
    private readonly Mock<ICollectionRepository> _mockCollectionRepository;
    private readonly Mock<IMessageQueueService> _mockMessageQueueService;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<CollectionService>> _mockLogger;
    private readonly CollectionService _collectionService;

    public CollectionNavigationTests()
    {
        _mockCollectionRepository = new Mock<ICollectionRepository>();
        _mockMessageQueueService = new Mock<IMessageQueueService>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<CollectionService>>();

        _collectionService = new CollectionService(
            _mockCollectionRepository.Object,
            _mockMessageQueueService.Object,
            _mockServiceProvider.Object,
            _mockLogger.Object
        );
    }

    #region GetCollectionNavigationAsync Tests

    [Fact]
    public async Task GetCollectionNavigationAsync_WithMiddleCollection_ReturnsCorrectPreviousAndNext()
    {
        // Arrange
        var collections = CreateTestCollections(5);
        var targetCollectionId = collections[2].Id; // Middle collection

        _mockCollectionRepository.Setup(r => r.FindAsync(
            It.IsAny<FilterDefinition<Collection>>(),
            It.IsAny<SortDefinition<Collection>>(),
            It.IsAny<int>(),
            It.IsAny<int>()
        )).ReturnsAsync(collections);

        // Act
        var result = await _collectionService.GetCollectionNavigationAsync(targetCollectionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(collections[1].Id.ToString(), result.PreviousCollectionId);
        Assert.Equal(collections[3].Id.ToString(), result.NextCollectionId);
        Assert.Equal(3, result.CurrentPosition); // 1-based
        Assert.Equal(5, result.TotalCollections);
        Assert.True(result.HasPrevious);
        Assert.True(result.HasNext);
    }

    [Fact]
    public async Task GetCollectionNavigationAsync_WithFirstCollection_ReturnsNoPrevious()
    {
        // Arrange
        var collections = CreateTestCollections(5);
        var targetCollectionId = collections[0].Id; // First collection

        _mockCollectionRepository.Setup(r => r.FindAsync(
            It.IsAny<FilterDefinition<Collection>>(),
            It.IsAny<SortDefinition<Collection>>(),
            It.IsAny<int>(),
            It.IsAny<int>()
        )).ReturnsAsync(collections);

        // Act
        var result = await _collectionService.GetCollectionNavigationAsync(targetCollectionId);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.PreviousCollectionId);
        Assert.Equal(collections[1].Id.ToString(), result.NextCollectionId);
        Assert.Equal(1, result.CurrentPosition);
        Assert.False(result.HasPrevious);
        Assert.True(result.HasNext);
    }

    [Fact]
    public async Task GetCollectionNavigationAsync_WithLastCollection_ReturnsNoNext()
    {
        // Arrange
        var collections = CreateTestCollections(5);
        var targetCollectionId = collections[4].Id; // Last collection

        _mockCollectionRepository.Setup(r => r.FindAsync(
            It.IsAny<FilterDefinition<Collection>>(),
            It.IsAny<SortDefinition<Collection>>(),
            It.IsAny<int>(),
            It.IsAny<int>()
        )).ReturnsAsync(collections);

        // Act
        var result = await _collectionService.GetCollectionNavigationAsync(targetCollectionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(collections[3].Id.ToString(), result.PreviousCollectionId);
        Assert.Null(result.NextCollectionId);
        Assert.Equal(5, result.CurrentPosition);
        Assert.True(result.HasPrevious);
        Assert.False(result.HasNext);
    }

    #endregion

    #region GetCollectionSiblingsAsync Tests

    [Fact]
    public async Task GetCollectionSiblingsAsync_WithValidCollection_ReturnsPaginatedSiblings()
    {
        // Arrange
        var collections = CreateTestCollections(10);
        var targetCollectionId = collections[5].Id;

        _mockCollectionRepository.Setup(r => r.FindAsync(
            It.IsAny<FilterDefinition<Collection>>(),
            It.IsAny<SortDefinition<Collection>>(),
            It.IsAny<int>(),
            It.IsAny<int>()
        )).ReturnsAsync(collections);

        // Act
        var result = await _collectionService.GetCollectionSiblingsAsync(targetCollectionId, page: 1, pageSize: 5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Siblings.Count);
        Assert.Equal(6, result.CurrentPosition); // 1-based
        Assert.Equal(10, result.TotalCount);
    }

    #endregion

    #region GetSortedCollectionsAsync Tests

    [Fact]
    public async Task GetSortedCollectionsAsync_SortByCreatedAt_Ascending_ReturnsSortedCollections()
    {
        // Arrange
        var collections = CreateTestCollections(5);

        _mockCollectionRepository.Setup(r => r.FindAsync(
            It.IsAny<FilterDefinition<Collection>>(),
            It.Is<SortDefinition<Collection>>(s => s != null),
            It.IsAny<int>(),
            It.IsAny<int>()
        )).ReturnsAsync(collections);

        // Act
        var result = await _collectionService.GetSortedCollectionsAsync("createdAt", "asc");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(collections.Count, result.Count());
    }

    [Fact]
    public async Task GetSortedCollectionsAsync_SortByName_Descending_ReturnsSortedCollections()
    {
        // Arrange
        var collections = CreateTestCollections(3);

        _mockCollectionRepository.Setup(r => r.FindAsync(
            It.IsAny<FilterDefinition<Collection>>(),
            It.IsAny<SortDefinition<Collection>>(),
            It.IsAny<int>(),
            It.IsAny<int>()
        )).ReturnsAsync(collections);

        // Act
        var result = await _collectionService.GetSortedCollectionsAsync("name", "desc");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
    }

    #endregion

    #region Helper Methods

    private List<Collection> CreateTestCollections(int count)
    {
        var collections = new List<Collection>();
        var baseDate = DateTime.UtcNow.AddDays(-count);
        var libraryId = ObjectId.GenerateNewId();

        for (int i = 0; i < count; i++)
        {
            var collection = new Collection(
                libraryId: libraryId,
                name: $"Test Collection {i + 1}",
                path: $"C:\\Test\\Collection{i + 1}",
                type: CollectionType.Folder,
                description: $"Test collection {i + 1}",
                createdBy: null,
                createdBySystem: "UnitTest"
            );

            // Set creation dates to be sequential
            var createdAtField = typeof(Collection).GetProperty("CreatedAt");
            createdAtField?.SetValue(collection, baseDate.AddDays(i));

            collections.Add(collection);
        }

        return collections;
    }

    #endregion
}

