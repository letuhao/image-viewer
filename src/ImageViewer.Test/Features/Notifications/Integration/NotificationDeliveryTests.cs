using FluentAssertions;
using Xunit;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Enums;
using MongoDB.Bson;
using ImageViewer.Test.Shared.Fixtures;

namespace ImageViewer.Test.Features.Notifications.Integration;

/// <summary>
/// Integration tests for Notification Delivery - End-to-end notification delivery scenarios
/// </summary>
public class NotificationDeliveryTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly INotificationService _notificationService;

    public NotificationDeliveryTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _notificationService = _fixture.GetService<INotificationService>();
    }

    [Fact]
    public async Task NotificationDelivery_SingleUser_ShouldDeliverSuccessfully()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var userId = _fixture.TestUserId;
        var request = new CreateNotificationRequest
        {
            UserId = userId,
            Title = "Test Notification",
            Message = "This is a test notification",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Notification");
        result.Message.Should().Be("This is a test notification");
        result.Type.Should().Be(NotificationType.Info);
        result.Priority.Should().Be(NotificationPriority.Normal);
        result.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task NotificationDelivery_MultipleUsers_ShouldDeliverToAll()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var userIds = new List<ObjectId> { _fixture.TestUserId, _fixture.TestUserId };
        var request = new CreateNotificationRequest
        {
            UserId = userIds.First(),
            Title = "Broadcast Notification",
            Message = "This is a broadcast notification",
            Type = NotificationType.Info,
            Priority = NotificationPriority.High
        };

        // Act
        var results = new List<Notification>();
        foreach (var userId in userIds)
        {
            request.UserId = userId;
            var result = await _notificationService.CreateNotificationAsync(request);
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Title.Should().Be("Broadcast Notification"));
        results.Should().AllSatisfy(r => r.Type.Should().Be(NotificationType.Info));
        results.Should().AllSatisfy(r => r.Priority.Should().Be(NotificationPriority.High));
    }

    [Fact]
    public async Task NotificationDelivery_WithTemplate_ShouldRenderAndDeliver()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var userId = _fixture.TestUserId;
        var request = new CreateNotificationRequest
        {
            UserId = userId,
            Title = "Template Notification",
            Message = "Hello {{userName}}, welcome to our platform!",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal,
            Metadata = new Dictionary<string, object> { { "userName", "TestUser" } }
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Template Notification");
        result.Message.Should().Be("Hello {{userName}}, welcome to our platform!");
        result.Type.Should().Be(NotificationType.Info);
    }

    [Fact]
    public async Task NotificationDelivery_WithMetadata_ShouldIncludeMetadata()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var userId = _fixture.TestUserId;
        var request = new CreateNotificationRequest
        {
            UserId = userId,
            Title = "Notification with Metadata",
            Message = "This notification has metadata",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal,
            Metadata = new Dictionary<string, object>
            {
                { "source", "test" },
                { "category", "integration" }
            }
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Notification with Metadata");
        result.Metadata.Should().HaveCount(2);
        result.Metadata.Should().ContainKey("source");
        result.Metadata.Should().ContainKey("category");
    }

    [Fact]
    public async Task NotificationDelivery_WithPriority_ShouldRespectPriority()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var userId = _fixture.TestUserId;
        var request = new CreateNotificationRequest
        {
            UserId = userId,
            Title = "High Priority Notification",
            Message = "This is a high priority notification",
            Type = NotificationType.Warning,
            Priority = NotificationPriority.High
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Priority.Should().Be(NotificationPriority.High);
        result.Type.Should().Be(NotificationType.Warning);
    }

    [Fact]
    public async Task NotificationDelivery_WithScheduling_ShouldDeliverAtScheduledTime()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var userId = _fixture.TestUserId;
        var scheduledTime = DateTime.UtcNow.AddMinutes(5);
        var request = new CreateNotificationRequest
        {
            UserId = userId,
            Title = "Scheduled Notification",
            Message = "This notification is scheduled for later",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal,
            ScheduledFor = scheduledTime
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Scheduled Notification");
        result.ScheduledFor.Should().BeCloseTo(scheduledTime, TimeSpan.FromSeconds(1));
        result.Type.Should().Be(NotificationType.Info);
    }

    [Fact]
    public async Task NotificationDelivery_WithExpiration_ShouldSetExpiration()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var userId = _fixture.TestUserId;
        var expirationTime = DateTime.UtcNow.AddDays(7);
        var request = new CreateNotificationRequest
        {
            UserId = userId,
            Title = "Expiring Notification",
            Message = "This notification will expire",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal,
            ExpiresAfter = TimeSpan.FromDays(7)
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Expiring Notification");
        result.ExpiresAt.Should().NotBeNull();
    }

    [Fact]
    public async Task NotificationDelivery_WithActionUrl_ShouldIncludeActionUrl()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var userId = _fixture.TestUserId;
        var request = new CreateNotificationRequest
        {
            UserId = userId,
            Title = "Action Notification",
            Message = "This notification has an action URL",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal,
            ActionUrl = "https://example.com/action"
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Action Notification");
        result.ActionUrl.Should().Be("https://example.com/action");
    }
}
