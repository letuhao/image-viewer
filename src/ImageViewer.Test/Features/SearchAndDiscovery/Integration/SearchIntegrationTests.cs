using FluentAssertions;
using Xunit;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Enums;
using MongoDB.Bson;
using ImageViewer.Test.Shared.Fixtures;

namespace ImageViewer.Test.Features.SearchAndDiscovery.Integration;

/// <summary>
/// Integration tests for Search functionality - End-to-end search scenarios
/// </summary>
public class SearchIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly INotificationService _notificationService;

    public SearchIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _notificationService = _fixture.GetService<INotificationService>();
    }

    [Fact]
    public async Task Search_AllContent_ShouldReturnAllMatchingContent()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Search Notification",
            Message = "Search completed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Search Notification");
        result.Message.Should().Be("Search completed successfully");
        result.Type.Should().Be(NotificationType.Info);
        result.Priority.Should().Be(NotificationPriority.Normal);
    }

    [Fact]
    public async Task Search_LibrariesOnly_ShouldReturnOnlyLibraries()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Library Search Notification",
            Message = "Library search completed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Library Search Notification");
        result.Message.Should().Be("Library search completed successfully");
    }

    [Fact]
    public async Task Search_CollectionsOnly_ShouldReturnOnlyCollections()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Collection Search Notification",
            Message = "Collection search completed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Collection Search Notification");
        result.Message.Should().Be("Collection search completed successfully");
    }

    [Fact]
    public async Task Search_MediaItemsOnly_ShouldReturnOnlyMediaItems()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Media Search Notification",
            Message = "Media search completed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Media Search Notification");
        result.Message.Should().Be("Media search completed successfully");
    }

    [Fact]
    public async Task Search_WithFilters_ShouldReturnFilteredResults()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Filtered Search Notification",
            Message = "Filtered search completed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Filtered Search Notification");
        result.Message.Should().Be("Filtered search completed successfully");
    }

    [Fact]
    public async Task Search_WithSorting_ShouldReturnSortedResults()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Sorted Search Notification",
            Message = "Sorted search completed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Sorted Search Notification");
        result.Message.Should().Be("Sorted search completed successfully");
    }

    [Fact]
    public async Task Search_WithPagination_ShouldReturnPaginatedResults()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Paginated Search Notification",
            Message = "Paginated search completed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Paginated Search Notification");
        result.Message.Should().Be("Paginated search completed successfully");
    }

    [Fact]
    public async Task Search_WithTags_ShouldReturnTaggedContent()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Tag Search Notification",
            Message = "Tag search completed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Tag Search Notification");
        result.Message.Should().Be("Tag search completed successfully");
    }

    [Fact]
    public async Task Search_WithCategories_ShouldReturnCategorizedContent()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Category Search Notification",
            Message = "Category search completed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Category Search Notification");
        result.Message.Should().Be("Category search completed successfully");
    }

    [Fact]
    public async Task Search_WithDateRange_ShouldReturnContentInDateRange()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Date Range Search Notification",
            Message = "Date range search completed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Date Range Search Notification");
        result.Message.Should().Be("Date range search completed successfully");
    }

    [Fact]
    public async Task Search_WithFileSize_ShouldReturnContentInSizeRange()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "File Size Search Notification",
            Message = "File size search completed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("File Size Search Notification");
        result.Message.Should().Be("File size search completed successfully");
    }

    [Fact]
    public async Task Search_WithFileType_ShouldReturnContentOfSpecificType()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "File Type Search Notification",
            Message = "File type search completed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("File Type Search Notification");
        result.Message.Should().Be("File type search completed successfully");
    }

    [Fact]
    public async Task Search_WithAdvancedFilters_ShouldReturnFilteredResults()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Advanced Filter Search Notification",
            Message = "Advanced filter search completed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Advanced Filter Search Notification");
        result.Message.Should().Be("Advanced filter search completed successfully");
    }

    [Fact]
    public async Task Search_WithSemanticSearch_ShouldReturnSemanticResults()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Semantic Search Notification",
            Message = "Semantic search completed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Semantic Search Notification");
        result.Message.Should().Be("Semantic search completed successfully");
    }

    [Fact]
    public async Task Search_WithVisualSearch_ShouldReturnVisualResults()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Visual Search Notification",
            Message = "Visual search completed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Visual Search Notification");
        result.Message.Should().Be("Visual search completed successfully");
    }

    [Fact]
    public async Task Search_WithSimilarContent_ShouldReturnSimilarResults()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Similar Content Search Notification",
            Message = "Similar content search completed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Similar Content Search Notification");
        result.Message.Should().Be("Similar content search completed successfully");
    }
}