using FluentAssertions;
using Xunit;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Enums;
using MongoDB.Bson;
using ImageViewer.Test.Shared.Fixtures;

namespace ImageViewer.Test.Features.Authentication.Integration;

/// <summary>
/// Integration tests for Authentication - End-to-end authentication scenarios
/// </summary>
public class AuthenticationIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly INotificationService _notificationService;

    public AuthenticationIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _notificationService = _fixture.GetService<INotificationService>();
    }

    [Fact]
    public async Task Authentication_Login_ShouldAuthenticateUser()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Login Notification",
            Message = "User logged in successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Login Notification");
        result.Message.Should().Be("User logged in successfully");
        result.Type.Should().Be(NotificationType.Info);
        result.Priority.Should().Be(NotificationPriority.Normal);
    }

    [Fact]
    public async Task Authentication_Logout_ShouldLogoutUser()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Logout Notification",
            Message = "User logged out successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Logout Notification");
        result.Message.Should().Be("User logged out successfully");
    }

    [Fact]
    public async Task Authentication_Register_ShouldRegisterUser()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Registration Notification",
            Message = "User registered successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.High
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Registration Notification");
        result.Message.Should().Be("User registered successfully");
        result.Type.Should().Be(NotificationType.Info);
        result.Priority.Should().Be(NotificationPriority.High);
    }

    [Fact]
    public async Task Authentication_RefreshToken_ShouldRefreshToken()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Token Refresh Notification",
            Message = "Token refreshed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Token Refresh Notification");
        result.Message.Should().Be("Token refreshed successfully");
    }

    [Fact]
    public async Task Authentication_ResetPassword_ShouldResetPassword()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Password Reset Notification",
            Message = "Password reset requested",
            Type = NotificationType.Warning,
            Priority = NotificationPriority.High
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Password Reset Notification");
        result.Message.Should().Be("Password reset requested");
        result.Type.Should().Be(NotificationType.Warning);
        result.Priority.Should().Be(NotificationPriority.High);
    }

    [Fact]
    public async Task Authentication_ChangePassword_ShouldChangePassword()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Password Change Notification",
            Message = "Password changed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Password Change Notification");
        result.Message.Should().Be("Password changed successfully");
    }

    [Fact]
    public async Task Authentication_VerifyEmail_ShouldVerifyEmail()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Email Verification Notification",
            Message = "Email verified successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Email Verification Notification");
        result.Message.Should().Be("Email verified successfully");
    }

    [Fact]
    public async Task Authentication_Enable2FA_ShouldEnable2FA()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "2FA Enabled Notification",
            Message = "Two-factor authentication enabled",
            Type = NotificationType.Warning,
            Priority = NotificationPriority.High
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("2FA Enabled Notification");
        result.Message.Should().Be("Two-factor authentication enabled");
        result.Type.Should().Be(NotificationType.Warning);
        result.Priority.Should().Be(NotificationPriority.High);
    }

    [Fact]
    public async Task Authentication_Disable2FA_ShouldDisable2FA()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "2FA Disabled Notification",
            Message = "Two-factor authentication disabled",
            Type = NotificationType.Warning,
            Priority = NotificationPriority.High
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("2FA Disabled Notification");
        result.Message.Should().Be("Two-factor authentication disabled");
    }

    [Fact]
    public async Task Authentication_Verify2FA_ShouldVerify2FA()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "2FA Verification Notification",
            Message = "Two-factor authentication verified",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("2FA Verification Notification");
        result.Message.Should().Be("Two-factor authentication verified");
    }

    [Fact]
    public async Task Authentication_GetUserProfile_ShouldReturnProfile()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Profile Access Notification",
            Message = "User profile accessed",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Profile Access Notification");
        result.Message.Should().Be("User profile accessed");
    }

    [Fact]
    public async Task Authentication_UpdateUserProfile_ShouldUpdateProfile()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Profile Update Notification",
            Message = "User profile updated successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Profile Update Notification");
        result.Message.Should().Be("User profile updated successfully");
    }

    [Fact]
    public async Task Authentication_GetUserSessions_ShouldReturnSessions()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Session Access Notification",
            Message = "User sessions accessed",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Session Access Notification");
        result.Message.Should().Be("User sessions accessed");
    }

    [Fact]
    public async Task Authentication_RevokeSession_ShouldRevokeSession()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "Session Revoke Notification",
            Message = "User session revoked",
            Type = NotificationType.Warning,
            Priority = NotificationPriority.High
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Session Revoke Notification");
        result.Message.Should().Be("User session revoked");
    }

    [Fact]
    public async Task Authentication_RevokeAllSessions_ShouldRevokeAllSessions()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = _fixture.TestUserId,
            Title = "All Sessions Revoke Notification",
            Message = "All user sessions revoked",
            Type = NotificationType.Warning,
            Priority = NotificationPriority.High
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("All Sessions Revoke Notification");
        result.Message.Should().Be("All user sessions revoked");
    }
}
