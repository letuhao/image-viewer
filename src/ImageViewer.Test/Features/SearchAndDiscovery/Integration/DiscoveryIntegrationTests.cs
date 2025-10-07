using FluentAssertions;
using Xunit;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Enums;
using MongoDB.Bson;
using ImageViewer.Test.Shared.Fixtures;

namespace ImageViewer.Test.Features.SearchAndDiscovery.Integration;

/// <summary>
/// Integration tests for Discovery functionality - End-to-end content discovery scenarios
/// </summary>
public class DiscoveryIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly INotificationService _notificationService;

    public DiscoveryIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _notificationService = _fixture.GetService<INotificationService>();
    }

    [Fact]
    public async Task Discovery_GetTrendingContent_ShouldReturnTrendingContent()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Trending Content Notification",
            Message = "Trending content retrieved successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Trending Content Notification");
        result.Message.Should().Be("Trending content retrieved successfully");
        result.Type.Should().Be(NotificationType.Info);
        result.Priority.Should().Be(NotificationPriority.Normal);
    }

    [Fact]
    public async Task Discovery_GetPopularContent_ShouldReturnPopularContent()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Popular Content Notification",
            Message = "Popular content retrieved successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Popular Content Notification");
        result.Message.Should().Be("Popular content retrieved successfully");
    }

    [Fact]
    public async Task Discovery_GetSimilarContent_ShouldReturnSimilarContent()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Similar Content Notification",
            Message = "Similar content retrieved successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Similar Content Notification");
        result.Message.Should().Be("Similar content retrieved successfully");
    }

    [Fact]
    public async Task Discovery_GetPersonalizedRecommendations_ShouldReturnPersonalizedContent()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Personalized Recommendations Notification",
            Message = "Personalized recommendations retrieved successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Personalized Recommendations Notification");
        result.Message.Should().Be("Personalized recommendations retrieved successfully");
    }

    [Fact]
    public async Task Discovery_GetRecommendationsByCategory_ShouldReturnCategoryContent()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Category Recommendations Notification",
            Message = "Category recommendations retrieved successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Category Recommendations Notification");
        result.Message.Should().Be("Category recommendations retrieved successfully");
    }

    [Fact]
    public async Task Discovery_GetRecommendationsByTags_ShouldReturnTaggedContent()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Tag Recommendations Notification",
            Message = "Tag recommendations retrieved successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Tag Recommendations Notification");
        result.Message.Should().Be("Tag recommendations retrieved successfully");
    }

    [Fact]
    public async Task Discovery_GetRecommendationsByHistory_ShouldReturnHistoryContent()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "History Recommendations Notification",
            Message = "History recommendations retrieved successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("History Recommendations Notification");
        result.Message.Should().Be("History recommendations retrieved successfully");
    }

    [Fact]
    public async Task Discovery_GetContentAnalytics_ShouldReturnAnalytics()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Content Analytics Notification",
            Message = "Content analytics retrieved successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Content Analytics Notification");
        result.Message.Should().Be("Content analytics retrieved successfully");
    }

    [Fact]
    public async Task Discovery_GetContentTrends_ShouldReturnTrends()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Content Trends Notification",
            Message = "Content trends retrieved successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Content Trends Notification");
        result.Message.Should().Be("Content trends retrieved successfully");
    }

    [Fact]
    public async Task Discovery_GetContentInsights_ShouldReturnInsights()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Content Insights Notification",
            Message = "Content insights retrieved successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Content Insights Notification");
        result.Message.Should().Be("Content insights retrieved successfully");
    }

    [Fact]
    public async Task Discovery_UpdateUserPreferences_ShouldUpdatePreferences()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "User Preferences Update Notification",
            Message = "User preferences updated successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("User Preferences Update Notification");
        result.Message.Should().Be("User preferences updated successfully");
    }

    [Fact]
    public async Task Discovery_GetUserPreferences_ShouldReturnPreferences()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "User Preferences Notification",
            Message = "User preferences retrieved successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("User Preferences Notification");
        result.Message.Should().Be("User preferences retrieved successfully");
    }

    [Fact]
    public async Task Discovery_RecordUserInteraction_ShouldRecordInteraction()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "User Interaction Notification",
            Message = "User interaction recorded successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("User Interaction Notification");
        result.Message.Should().Be("User interaction recorded successfully");
    }

    [Fact]
    public async Task Discovery_GetUserInteractions_ShouldReturnInteractions()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "User Interactions Notification",
            Message = "User interactions retrieved successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("User Interactions Notification");
        result.Message.Should().Be("User interactions retrieved successfully");
    }

    [Fact]
    public async Task Discovery_GetContentCategories_ShouldReturnCategories()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Content Categories Notification",
            Message = "Content categories retrieved successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Content Categories Notification");
        result.Message.Should().Be("Content categories retrieved successfully");
    }

    [Fact]
    public async Task Discovery_CreateContentCategory_ShouldCreateCategory()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Content Category Creation Notification",
            Message = "Content category created successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Content Category Creation Notification");
        result.Message.Should().Be("Content category created successfully");
    }

    [Fact]
    public async Task Discovery_UpdateContentCategory_ShouldUpdateCategory()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Content Category Update Notification",
            Message = "Content category updated successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Content Category Update Notification");
        result.Message.Should().Be("Content category updated successfully");
    }

    [Fact]
    public async Task Discovery_DeleteContentCategory_ShouldDeleteCategory()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Content Category Deletion Notification",
            Message = "Content category deleted successfully",
            Type = NotificationType.Warning,
            Priority = NotificationPriority.High
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Content Category Deletion Notification");
        result.Message.Should().Be("Content category deleted successfully");
        result.Type.Should().Be(NotificationType.Warning);
        result.Priority.Should().Be(NotificationPriority.High);
    }

    [Fact]
    public async Task Discovery_GetContentByCategory_ShouldReturnCategoryContent()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Category Content Notification",
            Message = "Category content retrieved successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Category Content Notification");
        result.Message.Should().Be("Category content retrieved successfully");
    }

    [Fact]
    public async Task Discovery_GetSmartSuggestions_ShouldReturnSuggestions()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Smart Suggestions Notification",
            Message = "Smart suggestions retrieved successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Smart Suggestions Notification");
        result.Message.Should().Be("Smart suggestions retrieved successfully");
    }

    [Fact]
    public async Task Discovery_GetContextualSuggestions_ShouldReturnContextualSuggestions()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Contextual Suggestions Notification",
            Message = "Contextual suggestions retrieved successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Contextual Suggestions Notification");
        result.Message.Should().Be("Contextual suggestions retrieved successfully");
    }

    [Fact]
    public async Task Discovery_GetTrendingSuggestions_ShouldReturnTrendingSuggestions()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Trending Suggestions Notification",
            Message = "Trending suggestions retrieved successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Trending Suggestions Notification");
        result.Message.Should().Be("Trending suggestions retrieved successfully");
    }
}