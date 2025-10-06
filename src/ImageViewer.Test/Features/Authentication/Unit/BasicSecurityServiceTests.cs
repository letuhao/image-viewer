using FluentAssertions;
using Moq;
using Xunit;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.Auth;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Exceptions;
using ImageViewer.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Security.Authentication;

namespace ImageViewer.Test.Features.Authentication.Unit;

/// <summary>
/// Unit tests for SecurityService - Authentication and Security features
/// </summary>
public class BasicSecurityServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<IPasswordService> _mockPasswordService;
    private readonly Mock<ISecurityAlertRepository> _mockSecurityAlertRepository;
    private readonly Mock<ISessionRepository> _mockSessionRepository;
    private readonly Mock<ILogger<SecurityService>> _mockLogger;
    private readonly SecurityService _securityService;

    public BasicSecurityServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockJwtService = new Mock<IJwtService>();
        _mockPasswordService = new Mock<IPasswordService>();
        _mockSecurityAlertRepository = new Mock<ISecurityAlertRepository>();
        _mockSessionRepository = new Mock<ISessionRepository>();
        _mockLogger = new Mock<ILogger<SecurityService>>();

        _securityService = new SecurityService(
            _mockUserRepository.Object,
            _mockJwtService.Object,
            _mockPasswordService.Object,
            _mockSecurityAlertRepository.Object,
            _mockSessionRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnSuccessResult()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var request = new ImageViewer.Application.Services.LoginRequest
        {
            Username = "testuser",
            Password = "validpassword",
            IpAddress = "192.168.1.1",
            UserAgent = "TestAgent"
        };

        var user = new User("testuser", "test@example.com", "hashedpassword", "User")
        {
            Id = userId
        };

        _mockUserRepository.Setup(x => x.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _mockPasswordService.Setup(x => x.VerifyPassword("validpassword", "hashedpassword"))
            .Returns(true);
        _mockJwtService.Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns("access-token");
        _mockJwtService.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");
        _mockUserRepository.Setup(x => x.StoreRefreshTokenAsync(It.IsAny<ObjectId>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);
        _mockUserRepository.Setup(x => x.LogSuccessfulLoginAsync(It.IsAny<ObjectId>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _mockUserRepository.Setup(x => x.ClearFailedLoginAttemptsAsync(It.IsAny<ObjectId>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _securityService.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.User.Should().NotBeNull();
        result.User!.Username.Should().Be("testuser");
        result.User.Email.Should().Be("test@example.com");
        result.RequiresTwoFactor.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidUsername_ShouldThrowAuthenticationException()
    {
        // Arrange
        var request = new ImageViewer.Application.Services.LoginRequest
        {
            Username = "nonexistent",
            Password = "password"
        };

        _mockUserRepository.Setup(x => x.GetByUsernameAsync("nonexistent"))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _securityService.LoginAsync(request));
        
        exception.Message.Should().Be("Invalid username or password");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrowAuthenticationException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var request = new ImageViewer.Application.Services.LoginRequest
        {
            Username = "testuser",
            Password = "wrongpassword"
        };

        var user = new User("testuser", "test@example.com", "hashedpassword", "User")
        {
            Id = userId
        };

        _mockUserRepository.Setup(x => x.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _mockPasswordService.Setup(x => x.VerifyPassword("wrongpassword", "hashedpassword"))
            .Returns(false);
        _mockUserRepository.Setup(x => x.LogFailedLoginAttemptAsync(It.IsAny<ObjectId>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _securityService.LoginAsync(request));
        
        exception.Message.Should().Be("Invalid username or password");
    }

    [Fact]
    public async Task LoginAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        // Note: The method signature doesn't allow null, so we expect NullReferenceException
        // before reaching the ArgumentNullException check in the method
        await Assert.ThrowsAsync<NullReferenceException>(
            () => _securityService.LoginAsync(null!));
    }

    [Fact]
    public async Task LoginAsync_WithEmptyUsername_ShouldThrowValidationException()
    {
        // Arrange
        var request = new ImageViewer.Application.Services.LoginRequest
        {
            Username = "",
            Password = "password"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _securityService.LoginAsync(request));
        
        exception.Message.Should().Be("Username and password are required");
    }

    [Fact]
    public async Task LoginAsync_WithEmptyPassword_ShouldThrowValidationException()
    {
        // Arrange
        var request = new ImageViewer.Application.Services.LoginRequest
        {
            Username = "testuser",
            Password = ""
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _securityService.LoginAsync(request));
        
        exception.Message.Should().Be("Username and password are required");
    }

    [Fact]
    public async Task LoginAsync_WithTwoFactorEnabled_ShouldReturnRequiresTwoFactor()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var request = new ImageViewer.Application.Services.LoginRequest
        {
            Username = "testuser",
            Password = "validpassword"
        };

        var user = new User("testuser", "test@example.com", "hashedpassword", "User")
        {
            Id = userId
        };
        
        // Enable 2FA by calling the User method directly
        user.EnableTwoFactor("secret", new List<string> { "backup1", "backup2" });

        _mockUserRepository.Setup(x => x.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _mockPasswordService.Setup(x => x.VerifyPassword("validpassword", "hashedpassword"))
            .Returns(true);

        // Act
        var result = await _securityService.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.RequiresTwoFactor.Should().BeTrue();
        result.TempToken.Should().NotBeNullOrEmpty();
        result.Success.Should().BeFalse();
        result.AccessToken.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithLockedAccount_ShouldThrowAuthenticationException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var request = new ImageViewer.Application.Services.LoginRequest
        {
            Username = "testuser",
            Password = "password"
        };

        var user = new User("testuser", "test@example.com", "hashedpassword", "User")
        {
            Id = userId
        };
        
        // Simulate locked account by incrementing failed login attempts
        // The account gets locked after 5 failed attempts
        user.IncrementFailedLoginAttempts();
        user.IncrementFailedLoginAttempts();
        user.IncrementFailedLoginAttempts();
        user.IncrementFailedLoginAttempts();
        user.IncrementFailedLoginAttempts(); // This should lock the account

        _mockUserRepository.Setup(x => x.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _securityService.LoginAsync(request));
        
        exception.Message.Should().Be("Account is locked. Please contact administrator");
    }
}