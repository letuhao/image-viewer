using ImageViewer.Application.Services;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace ImageViewer.Test.Features.Notifications.Unit;

/// <summary>
/// Unit tests for RealTimeNotificationService - Real-time Notification features
/// </summary>
public class RealTimeNotificationServiceTests
{
    private readonly Mock<ILogger<RealTimeNotificationService>> _mockLogger;
    private readonly RealTimeNotificationService _realTimeNotificationService;

    public RealTimeNotificationServiceTests()
    {
        _mockLogger = new Mock<ILogger<RealTimeNotificationService>>();
        _realTimeNotificationService = new RealTimeNotificationService(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidLogger_ShouldCreateInstance()
    {
        // Arrange & Act
        var service = new RealTimeNotificationService(_mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => new RealTimeNotificationService(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #region Connection Management Tests

    [Fact]
    public async Task ConnectAsync_WithValidParameters_ShouldConnectUser()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var connectionId = "connection-123";

        // Act
        await _realTimeNotificationService.ConnectAsync(userId, connectionId);

        // Assert
        var connections = await _realTimeNotificationService.GetUserConnectionsAsync(userId);
        connections.Should().Contain(connectionId);
        
        var retrievedUserId = await _realTimeNotificationService.GetUserIdByConnectionAsync(connectionId);
        retrievedUserId.Should().Be(userId);
    }

    [Fact]
    public async Task ConnectAsync_WithEmptyConnectionId_ShouldThrowArgumentException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var connectionId = "";

        // Act & Assert
        var action = async () => await _realTimeNotificationService.ConnectAsync(userId, connectionId);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("connectionId");
    }

    [Fact]
    public async Task DisconnectAsync_WithValidConnectionId_ShouldDisconnectUser()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var connectionId = "connection-123";
        await _realTimeNotificationService.ConnectAsync(userId, connectionId);

        // Act
        await _realTimeNotificationService.DisconnectAsync(connectionId);

        // Assert
        var connections = await _realTimeNotificationService.GetUserConnectionsAsync(userId);
        connections.Should().NotContain(connectionId);
        
        var retrievedUserId = await _realTimeNotificationService.GetUserIdByConnectionAsync(connectionId);
        retrievedUserId.Should().BeNull();
    }

    [Fact]
    public async Task DisconnectAsync_WithEmptyConnectionId_ShouldThrowArgumentException()
    {
        // Arrange
        var connectionId = "";

        // Act & Assert
        var action = async () => await _realTimeNotificationService.DisconnectAsync(connectionId);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("connectionId");
    }

    [Fact]
    public async Task GetUserConnectionsAsync_WithNonExistentUser_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();

        // Act
        var connections = await _realTimeNotificationService.GetUserConnectionsAsync(userId);

        // Assert
        connections.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserIdByConnectionAsync_WithNonExistentConnection_ShouldReturnNull()
    {
        // Arrange
        var connectionId = "non-existent-connection";

        // Act
        var userId = await _realTimeNotificationService.GetUserIdByConnectionAsync(connectionId);

        // Assert
        userId.Should().BeNull();
    }

    #endregion

    #region Real-time Delivery Tests

    [Fact]
    public async Task SendToUserAsync_WithValidParameters_ShouldSendNotification()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var connectionId = "connection-123";
        await _realTimeNotificationService.ConnectAsync(userId, connectionId);
        
        var message = new NotificationMessage
        {
            Title = "Test Notification",
            Message = "This is a test message",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        await _realTimeNotificationService.SendToUserAsync(userId, message);

        // Assert
        var history = await _realTimeNotificationService.GetNotificationHistoryAsync(userId);
        history.Should().HaveCount(1);
        history.First().Message.Title.Should().Be("Test Notification");
    }

    [Fact]
    public async Task SendToUserAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();

        // Act & Assert
        var action = async () => await _realTimeNotificationService.SendToUserAsync(userId, null!);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("message");
    }

    [Fact]
    public async Task SendToUserAsync_WithNoConnections_ShouldNotSendNotification()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var message = new NotificationMessage
        {
            Title = "Test Notification",
            Message = "This is a test message",
            Type = NotificationType.Info
        };

        // Act
        await _realTimeNotificationService.SendToUserAsync(userId, message);

        // Assert
        var history = await _realTimeNotificationService.GetNotificationHistoryAsync(userId);
        history.Should().BeEmpty();
    }

    [Fact]
    public async Task SendToConnectionAsync_WithValidParameters_ShouldSendNotification()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var connectionId = "connection-123";
        await _realTimeNotificationService.ConnectAsync(userId, connectionId);
        
        var message = new NotificationMessage
        {
            Title = "Test Notification",
            Message = "This is a test message",
            Type = NotificationType.Info
        };

        // Act
        await _realTimeNotificationService.SendToConnectionAsync(connectionId, message);

        // Assert
        var history = await _realTimeNotificationService.GetNotificationHistoryAsync(userId);
        history.Should().HaveCount(1);
        history.First().ConnectionId.Should().Be(connectionId);
    }

    [Fact]
    public async Task SendToConnectionAsync_WithEmptyConnectionId_ShouldThrowArgumentException()
    {
        // Arrange
        var message = new NotificationMessage
        {
            Title = "Test Notification",
            Message = "This is a test message",
            Type = NotificationType.Info
        };

        // Act & Assert
        var action = async () => await _realTimeNotificationService.SendToConnectionAsync("", message);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("connectionId");
    }

    [Fact]
    public async Task SendToConnectionAsync_WithNonExistentConnection_ShouldNotSendNotification()
    {
        // Arrange
        var connectionId = "non-existent-connection";
        var message = new NotificationMessage
        {
            Title = "Test Notification",
            Message = "This is a test message",
            Type = NotificationType.Info
        };

        // Act
        await _realTimeNotificationService.SendToConnectionAsync(connectionId, message);

        // Assert
        var statistics = await _realTimeNotificationService.GetStatisticsAsync();
        statistics.NotificationsSent.Should().Be(0);
    }

    [Fact]
    public async Task BroadcastAsync_WithValidMessage_ShouldSendToAllOnlineUsers()
    {
        // Arrange
        var userId1 = ObjectId.GenerateNewId();
        var userId2 = ObjectId.GenerateNewId();
        var connectionId1 = "connection-1";
        var connectionId2 = "connection-2";
        
        await _realTimeNotificationService.ConnectAsync(userId1, connectionId1);
        await _realTimeNotificationService.ConnectAsync(userId2, connectionId2);
        
        var message = new NotificationMessage
        {
            Title = "Broadcast Notification",
            Message = "This is a broadcast message",
            Type = NotificationType.System
        };

        // Act
        await _realTimeNotificationService.BroadcastAsync(message);

        // Assert
        var history1 = await _realTimeNotificationService.GetNotificationHistoryAsync(userId1);
        var history2 = await _realTimeNotificationService.GetNotificationHistoryAsync(userId2);
        
        history1.Should().HaveCount(1);
        history2.Should().HaveCount(1);
    }

    [Fact]
    public async Task BroadcastAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = async () => await _realTimeNotificationService.BroadcastAsync(null!);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("message");
    }

    [Fact]
    public async Task SendToGroupAsync_WithValidParameters_ShouldSendToAllUsers()
    {
        // Arrange
        var userId1 = ObjectId.GenerateNewId();
        var userId2 = ObjectId.GenerateNewId();
        var connectionId1 = "connection-1";
        var connectionId2 = "connection-2";
        
        await _realTimeNotificationService.ConnectAsync(userId1, connectionId1);
        await _realTimeNotificationService.ConnectAsync(userId2, connectionId2);
        
        var userIds = new[] { userId1, userId2 };
        var message = new NotificationMessage
        {
            Title = "Group Notification",
            Message = "This is a group message",
            Type = NotificationType.User
        };

        // Act
        await _realTimeNotificationService.SendToGroupAsync(userIds, message);

        // Assert
        var history1 = await _realTimeNotificationService.GetNotificationHistoryAsync(userId1);
        var history2 = await _realTimeNotificationService.GetNotificationHistoryAsync(userId2);
        
        history1.Should().HaveCount(1);
        history2.Should().HaveCount(1);
    }

    [Fact]
    public async Task SendToGroupAsync_WithNullUserIds_ShouldThrowArgumentNullException()
    {
        // Arrange
        var message = new NotificationMessage
        {
            Title = "Group Notification",
            Message = "This is a group message",
            Type = NotificationType.User
        };

        // Act & Assert
        var action = async () => await _realTimeNotificationService.SendToGroupAsync(null!, message);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("userIds");
    }

    #endregion

    #region User Presence Tests

    [Fact]
    public async Task UpdateUserPresenceAsync_WithValidParameters_ShouldUpdatePresence()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var status = UserPresenceStatus.Away;

        // Act
        await _realTimeNotificationService.UpdateUserPresenceAsync(userId, status);

        // Assert
        var retrievedStatus = await _realTimeNotificationService.GetUserPresenceAsync(userId);
        retrievedStatus.Should().Be(status);
    }

    [Fact]
    public async Task GetUserPresenceAsync_WithNonExistentUser_ShouldReturnOffline()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();

        // Act
        var status = await _realTimeNotificationService.GetUserPresenceAsync(userId);

        // Assert
        status.Should().Be(UserPresenceStatus.Offline);
    }

    [Fact]
    public async Task GetOnlineUsersAsync_WithNoOnlineUsers_ShouldReturnEmptyList()
    {
        // Act
        var onlineUsers = await _realTimeNotificationService.GetOnlineUsersAsync();

        // Assert
        onlineUsers.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOnlineUsersAsync_WithOnlineUsers_ShouldReturnOnlineUsers()
    {
        // Arrange
        var userId1 = ObjectId.GenerateNewId();
        var userId2 = ObjectId.GenerateNewId();
        var connectionId1 = "connection-1";
        var connectionId2 = "connection-2";
        
        await _realTimeNotificationService.ConnectAsync(userId1, connectionId1);
        await _realTimeNotificationService.ConnectAsync(userId2, connectionId2);

        // Act
        var onlineUsers = await _realTimeNotificationService.GetOnlineUsersAsync();

        // Assert
        onlineUsers.Should().HaveCount(2);
        onlineUsers.Should().Contain(userId1);
        onlineUsers.Should().Contain(userId2);
    }

    [Fact]
    public async Task ConnectAsync_ShouldSetUserAsOnline()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var connectionId = "connection-123";

        // Act
        await _realTimeNotificationService.ConnectAsync(userId, connectionId);

        // Assert
        var status = await _realTimeNotificationService.GetUserPresenceAsync(userId);
        status.Should().Be(UserPresenceStatus.Online);
    }

    [Fact]
    public async Task DisconnectAsync_WithLastConnection_ShouldSetUserAsOffline()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var connectionId = "connection-123";
        await _realTimeNotificationService.ConnectAsync(userId, connectionId);

        // Act
        await _realTimeNotificationService.DisconnectAsync(connectionId);

        // Assert
        var status = await _realTimeNotificationService.GetUserPresenceAsync(userId);
        status.Should().Be(UserPresenceStatus.Offline);
    }

    #endregion

    #region Notification History Tests

    [Fact]
    public async Task GetNotificationHistoryAsync_WithNoNotifications_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();

        // Act
        var history = await _realTimeNotificationService.GetNotificationHistoryAsync(userId);

        // Assert
        history.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNotificationHistoryAsync_WithNotifications_ShouldReturnOrderedHistory()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var connectionId = "connection-123";
        await _realTimeNotificationService.ConnectAsync(userId, connectionId);
        
        var message1 = new NotificationMessage { Title = "First", Type = NotificationType.Info };
        var message2 = new NotificationMessage { Title = "Second", Type = NotificationType.Info };
        
        await _realTimeNotificationService.SendToUserAsync(userId, message1);
        await Task.Delay(10); // Ensure different timestamps
        await _realTimeNotificationService.SendToUserAsync(userId, message2);

        // Act
        var history = await _realTimeNotificationService.GetNotificationHistoryAsync(userId);

        // Assert
        history.Should().HaveCount(2);
        history.First().Message.Title.Should().Be("Second"); // Most recent first
        history.Last().Message.Title.Should().Be("First");
    }

    [Fact]
    public async Task GetNotificationHistoryAsync_WithLimit_ShouldRespectLimit()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var connectionId = "connection-123";
        await _realTimeNotificationService.ConnectAsync(userId, connectionId);
        
        for (int i = 0; i < 5; i++)
        {
            var message = new NotificationMessage { Title = $"Message {i}", Type = NotificationType.Info };
            await _realTimeNotificationService.SendToUserAsync(userId, message);
        }

        // Act
        var history = await _realTimeNotificationService.GetNotificationHistoryAsync(userId, limit: 3);

        // Assert
        history.Should().HaveCount(3);
    }

    [Fact]
    public async Task MarkAsReadAsync_WithValidNotification_ShouldMarkAsRead()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var connectionId = "connection-123";
        await _realTimeNotificationService.ConnectAsync(userId, connectionId);
        
        var message = new NotificationMessage { Title = "Test", Type = NotificationType.Info };
        await _realTimeNotificationService.SendToUserAsync(userId, message);
        
        var history = await _realTimeNotificationService.GetNotificationHistoryAsync(userId);
        var notificationId = history.First().Id;

        // Act
        await _realTimeNotificationService.MarkAsReadAsync(notificationId, userId);

        // Assert
        var updatedHistory = await _realTimeNotificationService.GetNotificationHistoryAsync(userId);
        updatedHistory.First().IsRead.Should().BeTrue();
        updatedHistory.First().ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ClearHistoryAsync_WithValidUser_ShouldClearHistory()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var connectionId = "connection-123";
        await _realTimeNotificationService.ConnectAsync(userId, connectionId);
        
        var message = new NotificationMessage { Title = "Test", Type = NotificationType.Info };
        await _realTimeNotificationService.SendToUserAsync(userId, message);

        // Act
        await _realTimeNotificationService.ClearHistoryAsync(userId);

        // Assert
        var history = await _realTimeNotificationService.GetNotificationHistoryAsync(userId);
        history.Should().BeEmpty();
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public async Task GetStatisticsAsync_WithNoActivity_ShouldReturnZeroStatistics()
    {
        // Act
        var statistics = await _realTimeNotificationService.GetStatisticsAsync();

        // Assert
        statistics.TotalConnections.Should().Be(0);
        statistics.OnlineUsers.Should().Be(0);
        statistics.NotificationsSent.Should().Be(0);
        statistics.NotificationsDelivered.Should().Be(0);
        statistics.NotificationsRead.Should().Be(0);
        statistics.DeliveryRate.Should().Be(0);
        statistics.ReadRate.Should().Be(0);
    }

    [Fact]
    public async Task GetStatisticsAsync_WithActivity_ShouldReturnCorrectStatistics()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var connectionId = "connection-123";
        await _realTimeNotificationService.ConnectAsync(userId, connectionId);
        
        var message = new NotificationMessage { Title = "Test", Type = NotificationType.Info };
        await _realTimeNotificationService.SendToUserAsync(userId, message);
        
        var history = await _realTimeNotificationService.GetNotificationHistoryAsync(userId);
        await _realTimeNotificationService.MarkAsReadAsync(history.First().Id, userId);

        // Act
        var statistics = await _realTimeNotificationService.GetStatisticsAsync();

        // Assert
        statistics.TotalConnections.Should().Be(1);
        statistics.OnlineUsers.Should().Be(1);
        statistics.NotificationsSent.Should().Be(1);
        statistics.NotificationsDelivered.Should().Be(1);
        statistics.NotificationsRead.Should().Be(1);
        statistics.DeliveryRate.Should().Be(100);
        statistics.ReadRate.Should().Be(100);
        statistics.PresenceDistribution.Should().ContainKey(UserPresenceStatus.Online);
        statistics.NotificationsByType.Should().ContainKey(NotificationType.Info);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task MultipleConnections_WithSameUser_ShouldHandleCorrectly()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var connectionId1 = "connection-1";
        var connectionId2 = "connection-2";
        
        await _realTimeNotificationService.ConnectAsync(userId, connectionId1);
        await _realTimeNotificationService.ConnectAsync(userId, connectionId2);

        // Act
        var connections = await _realTimeNotificationService.GetUserConnectionsAsync(userId);
        var status = await _realTimeNotificationService.GetUserPresenceAsync(userId);

        // Assert
        connections.Should().HaveCount(2);
        connections.Should().Contain(connectionId1);
        connections.Should().Contain(connectionId2);
        status.Should().Be(UserPresenceStatus.Online);
    }

    [Fact]
    public async Task DisconnectOneConnection_WithMultipleConnections_ShouldKeepUserOnline()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var connectionId1 = "connection-1";
        var connectionId2 = "connection-2";
        
        await _realTimeNotificationService.ConnectAsync(userId, connectionId1);
        await _realTimeNotificationService.ConnectAsync(userId, connectionId2);

        // Act
        await _realTimeNotificationService.DisconnectAsync(connectionId1);
        
        var connections = await _realTimeNotificationService.GetUserConnectionsAsync(userId);
        var status = await _realTimeNotificationService.GetUserPresenceAsync(userId);

        // Assert
        connections.Should().HaveCount(1);
        connections.Should().Contain(connectionId2);
        status.Should().Be(UserPresenceStatus.Online);
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldHandleCorrectly()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var connectionId = "connection-123";
        var message = new NotificationMessage { Title = "Test", Type = NotificationType.Info };

        // Act - Simulate concurrent operations
        var tasks = new[]
        {
            _realTimeNotificationService.ConnectAsync(userId, connectionId),
            _realTimeNotificationService.UpdateUserPresenceAsync(userId, UserPresenceStatus.Online),
            _realTimeNotificationService.SendToUserAsync(userId, message)
        };

        await Task.WhenAll(tasks);

        // Assert
        var connections = await _realTimeNotificationService.GetUserConnectionsAsync(userId);
        var status = await _realTimeNotificationService.GetUserPresenceAsync(userId);
        var history = await _realTimeNotificationService.GetNotificationHistoryAsync(userId);

        connections.Should().Contain(connectionId);
        status.Should().Be(UserPresenceStatus.Online);
        history.Should().HaveCount(1);
    }

    #endregion
}
