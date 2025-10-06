using FluentAssertions;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Exceptions;
using ImageViewer.Application.DTOs.Auth;
using ImageViewer.Application.DTOs.Security;
using MongoDB.Bson;
using Moq;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ImageViewer.Test.Features.Authentication.Unit;

/// <summary>
/// Unit tests for SecurityService - Authentication and Security features
/// </summary>
public class SecurityServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IPasswordService> _passwordServiceMock;
    private readonly Mock<ISecurityAlertRepository> _securityAlertRepositoryMock;
    private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly Mock<ILogger<SecurityService>> _loggerMock;
    private readonly SecurityService _securityService;

    public SecurityServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _jwtServiceMock = new Mock<IJwtService>();
        _passwordServiceMock = new Mock<IPasswordService>();
        _securityAlertRepositoryMock = new Mock<ISecurityAlertRepository>();
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _loggerMock = new Mock<ILogger<SecurityService>>();

        _securityService = new SecurityService(
            _userRepositoryMock.Object,
            _jwtServiceMock.Object,
            _passwordServiceMock.Object,
            _securityAlertRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            _loggerMock.Object);
    }

    #region Login Tests

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccessResult()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = CreateTestUser(userId, "testuser", "test@example.com");
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "ValidPassword123!",
            IpAddress = "192.168.1.1",
            UserAgent = "TestAgent"
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _passwordServiceMock.Setup(x => x.VerifyPassword("ValidPassword123!", user.PasswordHash))
            .Returns(true);
        _jwtServiceMock.Setup(x => x.GenerateAccessToken(user))
            .Returns("access_token");
        _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");
        _userRepositoryMock.Setup(x => x.StoreRefreshTokenAsync(userId, "refresh_token", It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);
        _userRepositoryMock.Setup(x => x.LogSuccessfulLoginAsync(userId, "192.168.1.1", "TestAgent"))
            .Returns(Task.CompletedTask);
        _userRepositoryMock.Setup(x => x.ClearFailedLoginAttemptsAsync(userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _securityService.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
        result.User.Should().NotBeNull();
        result.User.Username.Should().Be("testuser");
        result.User.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task LoginAsync_InvalidUsername_ThrowsAuthenticationException()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "nonexistent",
            Password = "ValidPassword123!"
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("nonexistent"))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _securityService.LoginAsync(request));
        
        exception.Message.Should().Be("Invalid username or password");
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsAuthenticationException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = CreateTestUser(userId, "testuser", "test@example.com");
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "WrongPassword"
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _passwordServiceMock.Setup(x => x.VerifyPassword("WrongPassword", user.PasswordHash))
            .Returns(false);
        _userRepositoryMock.Setup(x => x.LogFailedLoginAttemptAsync(userId))
            .Returns(Task.CompletedTask);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _securityService.LoginAsync(request));
        
        exception.Message.Should().Be("Invalid username or password");
    }

    [Fact]
    public async Task LoginAsync_LockedAccount_ThrowsAuthenticationException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = CreateTestUser(userId, "testuser", "test@example.com");
        user.GetType().GetProperty("IsLocked")?.SetValue(user, true);
        user.GetType().GetProperty("LockedUntil")?.SetValue(user, DateTime.UtcNow.AddMinutes(30));
        
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "ValidPassword123!"
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _securityService.LoginAsync(request));
        
        exception.Message.Should().Be("Account is locked. Please contact administrator");
    }

    [Fact]
    public async Task LoginAsync_RequiresTwoFactor_ReturnsRequiresTwoFactorResult()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = CreateTestUser(userId, "testuser", "test@example.com");
        user.GetType().GetProperty("TwoFactorEnabled")?.SetValue(user, true);
        
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "ValidPassword123!"
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _passwordServiceMock.Setup(x => x.VerifyPassword("ValidPassword123!", user.PasswordHash))
            .Returns(true);

        // Act
        var result = await _securityService.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.RequiresTwoFactor.Should().BeTrue();
        result.TempToken.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Registration Tests

    [Fact]
    public async Task RegisterAsync_ValidData_ReturnsSuccessResult()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "ValidPassword123!",
            FirstName = "John",
            LastName = "Doe"
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("newuser"))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync("newuser@example.com"))
            .ReturnsAsync((User?)null);
        _passwordServiceMock.Setup(x => x.IsStrongPassword("ValidPassword123!"))
            .Returns(true);
        _passwordServiceMock.Setup(x => x.HashPassword("ValidPassword123!"))
            .Returns("hashed_password");
        _userRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _securityService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.UserId.Should().NotBeNullOrEmpty();
        result.RequiresEmailVerification.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_ExistingUsername_ReturnsFailureResult()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var existingUser = CreateTestUser(userId, "existinguser", "existing@example.com");
        var request = new RegisterRequest
        {
            Username = "existinguser",
            Email = "newuser@example.com",
            Password = "ValidPassword123!"
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("existinguser"))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _securityService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Username already exists");
    }

    [Fact]
    public async Task RegisterAsync_ExistingEmail_ReturnsFailureResult()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var existingUser = CreateTestUser(userId, "newuser", "existing@example.com");
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "existing@example.com",
            Password = "ValidPassword123!"
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("newuser"))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync("existing@example.com"))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _securityService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Email already exists");
    }

    [Fact]
    public async Task RegisterAsync_WeakPassword_ReturnsFailureResult()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "weak"
        };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("newuser"))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync("newuser@example.com"))
            .ReturnsAsync((User?)null);
        _passwordServiceMock.Setup(x => x.IsStrongPassword("weak"))
            .Returns(false);

        // Act
        var result = await _securityService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Password does not meet strength requirements");
    }

    #endregion

    #region Token Refresh Tests

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ReturnsNewTokens()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = CreateTestUser(userId, "testuser", "test@example.com");
        var refreshToken = "valid_refresh_token";

        _userRepositoryMock.Setup(x => x.GetByRefreshTokenAsync(refreshToken))
            .ReturnsAsync(user);
        _jwtServiceMock.Setup(x => x.GenerateAccessToken(user))
            .Returns("new_access_token");
        _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("new_refresh_token");
        _userRepositoryMock.Setup(x => x.StoreRefreshTokenAsync(userId, "new_refresh_token", It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _securityService.RefreshTokenAsync(refreshToken);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("new_access_token");
        result.RefreshToken.Should().Be("new_refresh_token");
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidToken_ThrowsAuthenticationException()
    {
        // Arrange
        var refreshToken = "invalid_refresh_token";

        _userRepositoryMock.Setup(x => x.GetByRefreshTokenAsync(refreshToken))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _securityService.RefreshTokenAsync(refreshToken));
        
        exception.Message.Should().Be("Invalid refresh token");
    }

    #endregion

    #region Password Change Tests

    [Fact]
    public async Task ChangePasswordAsync_ValidCurrentPassword_UpdatesPassword()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = CreateTestUser(userId, "testuser", "test@example.com");
        var currentPassword = "CurrentPassword123!";
        var newPassword = "NewPassword123!";

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _passwordServiceMock.Setup(x => x.VerifyPassword(currentPassword, user.PasswordHash))
            .Returns(true);
        _passwordServiceMock.Setup(x => x.IsStrongPassword(newPassword))
            .Returns(true);
        _passwordServiceMock.Setup(x => x.HashPassword(newPassword))
            .Returns("new_hashed_password");
        _userRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        await _securityService.ChangePasswordAsync(userId, currentPassword, newPassword);

        // Assert
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_InvalidCurrentPassword_ThrowsAuthenticationException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = CreateTestUser(userId, "testuser", "test@example.com");
        var currentPassword = "WrongPassword";
        var newPassword = "NewPassword123!";

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _passwordServiceMock.Setup(x => x.VerifyPassword(currentPassword, user.PasswordHash))
            .Returns(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _securityService.ChangePasswordAsync(userId, currentPassword, newPassword));
        
        exception.Message.Should().Be("Current password is incorrect");
    }

    [Fact]
    public async Task ChangePasswordAsync_WeakNewPassword_ThrowsValidationException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = CreateTestUser(userId, "testuser", "test@example.com");
        var currentPassword = "CurrentPassword123!";
        var newPassword = "weak";

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _passwordServiceMock.Setup(x => x.VerifyPassword(currentPassword, user.PasswordHash))
            .Returns(true);
        _passwordServiceMock.Setup(x => x.IsStrongPassword(newPassword))
            .Returns(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _securityService.ChangePasswordAsync(userId, currentPassword, newPassword));
        
        exception.Message.Should().Be("New password does not meet strength requirements");
    }

    #endregion

    #region Token Validation Tests

    [Fact]
    public void ValidateToken_ValidToken_ReturnsTrue()
    {
        // Arrange
        var token = "valid_token";

        _jwtServiceMock.Setup(x => x.ValidateToken(token))
            .Returns(new System.Security.Claims.ClaimsPrincipal());
        _jwtServiceMock.Setup(x => x.IsTokenExpired(token))
            .Returns(false);

        // Act
        var result = _securityService.ValidateToken(token);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_ExpiredToken_ReturnsFalse()
    {
        // Arrange
        var token = "expired_token";

        _jwtServiceMock.Setup(x => x.ValidateToken(token))
            .Returns(new System.Security.Claims.ClaimsPrincipal());
        _jwtServiceMock.Setup(x => x.IsTokenExpired(token))
            .Returns(true);

        // Act
        var result = _securityService.ValidateToken(token);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsFalse()
    {
        // Arrange
        var token = "invalid_token";

        _jwtServiceMock.Setup(x => x.ValidateToken(token))
            .Returns((System.Security.Claims.ClaimsPrincipal?)null);

        // Act
        var result = _securityService.ValidateToken(token);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private User CreateTestUser(ObjectId id, string username, string email)
    {
        var user = new User(username, email, "hashed_password", "User");
        // Use reflection to set the Id since it's private
        user.GetType().GetProperty("Id")?.SetValue(user, id);
        return user;
    }

    #endregion
}
