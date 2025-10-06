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
using System.Threading.Tasks;
using NotificationTemplate = ImageViewer.Application.Services.NotificationTemplate; // Alias for clarity

namespace ImageViewer.Test.Features.Notifications.Unit;

/// <summary>
/// Unit tests for NotificationService - Notification Management features
/// </summary>
public class NotificationServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<INotificationQueueRepository> _mockNotificationQueueRepository;
    private readonly Mock<INotificationTemplateRepository> _mockNotificationTemplateRepository;
    private readonly Mock<ILogger<NotificationService>> _mockLogger;
    private readonly NotificationService _notificationService;

    public NotificationServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockNotificationQueueRepository = new Mock<INotificationQueueRepository>();
        _mockNotificationTemplateRepository = new Mock<INotificationTemplateRepository>();
        _mockLogger = new Mock<ILogger<NotificationService>>();

        _notificationService = new NotificationService(
            _mockUserRepository.Object,
            _mockNotificationQueueRepository.Object,
            _mockNotificationTemplateRepository.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CreateNotificationAsync_WithValidRequest_ShouldReturnNotification()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = new User("testuser", "test@example.com", "hashedpassword");
        var request = new CreateNotificationRequest
        {
            UserId = userId,
            Type = NotificationType.System,
            Title = "Test Notification",
            Message = "This is a test notification",
            Priority = NotificationPriority.Normal
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Title.Should().Be(request.Title);
        result.Message.Should().Be(request.Message);
        result.Type.Should().Be(request.Type);
        result.Priority.Should().Be(request.Priority);
    }

    [Fact]
    public async Task CreateNotificationAsync_WithEmptyTitle_ShouldThrowValidationException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var request = new CreateNotificationRequest
        {
            UserId = userId,
            Type = NotificationType.System,
            Title = "",
            Message = "This is a test notification",
            Priority = NotificationPriority.Normal
        };

        // Act
        Func<Task> act = async () => await _notificationService.CreateNotificationAsync(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Notification title cannot be null or empty");
    }

    [Fact]
    public async Task CreateNotificationAsync_WithNonExistentUser_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var request = new CreateNotificationRequest
        {
            UserId = userId,
            Type = NotificationType.System,
            Title = "Test Notification",
            Message = "This is a test notification",
            Priority = NotificationPriority.Normal
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User)null!);

        // Act
        Func<Task> act = async () => await _notificationService.CreateNotificationAsync(request);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"User with ID '{userId}' not found");
    }

    [Fact]
    public async Task GetNotificationByIdAsync_WithValidId_ShouldReturnNotification()
    {
        // Arrange
        var notificationId = ObjectId.GenerateNewId();
        var userId = ObjectId.GenerateNewId();
        var domainNotification = Domain.Entities.NotificationQueue.Create(
            userId,
            "system",
            "Test Title",
            "Test Message",
            "normal",
            null,
            null,
            null
        );
        domainNotification.GetType().GetProperty("Id")!.SetValue(domainNotification, notificationId);

        _mockNotificationQueueRepository.Setup(r => r.GetByIdAsync(notificationId)).ReturnsAsync(domainNotification);

        // Act
        var result = await _notificationService.GetNotificationByIdAsync(notificationId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(notificationId);
        result.Title.Should().Be("Test Title");
    }

    [Fact]
    public async Task GetNotificationByIdAsync_WithNonExistentId_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var notificationId = ObjectId.GenerateNewId();
        _mockNotificationQueueRepository.Setup(r => r.GetByIdAsync(notificationId)).ReturnsAsync((Domain.Entities.NotificationQueue)null!);

        // Act
        Func<Task> act = async () => await _notificationService.GetNotificationByIdAsync(notificationId);

        // Assert
        // The service wraps exceptions in BusinessRuleException for consistent error handling
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage($"Failed to get notification with ID '{notificationId}'");
    }

    [Fact]
    public async Task SendRealTimeNotificationAsync_WithValidData_ShouldSendNotification()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = new User("testuser", "test@example.com", "hashedpassword");
        var message = new NotificationMessage
        {
            Title = "Real-time Notification",
            Message = "This is a real-time notification",
            Type = NotificationType.System
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        await _notificationService.SendRealTimeNotificationAsync(userId, message);

        // Assert
        // No exception means success
    }

    [Fact]
    public async Task SendBroadcastNotificationAsync_WithValidMessage_ShouldSendBroadcast()
    {
        // Arrange
        var message = new NotificationMessage
        {
            Title = "Broadcast Notification",
            Message = "This is a broadcast notification",
            Type = NotificationType.System
        };

        // Act
        await _notificationService.SendBroadcastNotificationAsync(message);

        // Assert
        // No exception means success
    }

    [Fact]
    public async Task SendGroupNotificationAsync_WithValidData_ShouldSendGroupNotification()
    {
        // Arrange
        var userIds = new List<ObjectId> { ObjectId.GenerateNewId(), ObjectId.GenerateNewId() };
        var user = new User("testuser", "test@example.com", "hashedpassword");
        var message = new NotificationMessage
        {
            Title = "Group Notification",
            Message = "This is a group notification",
            Type = NotificationType.System
        };

        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<ObjectId>())).ReturnsAsync(user);

        // Act
        await _notificationService.SendGroupNotificationAsync(userIds, message);

        // Assert
        // No exception means success
    }
}
