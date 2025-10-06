using FluentAssertions;
using Moq;
using Xunit;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Test.Features.UserManagement.Unit;

/// <summary>
/// Unit tests for UserService - User Management and Registration features
/// </summary>
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<UserService>>();
        _userService = new UserService(_mockUserRepository.Object, _mockLogger.Object);
    }

    #region CreateUserAsync Tests

    [Fact]
    public async Task CreateUserAsync_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var username = "testuser";
        var email = "test@example.com";
        var passwordHash = "hashedpassword";
        var user = new User(username, email, passwordHash);

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(username))
            .ReturnsAsync((User)null!);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync((User)null!);
        _mockUserRepository.Setup(x => x.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.CreateUserAsync(username, email, passwordHash);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be(username);
        result.Email.Should().Be(email);
        _mockUserRepository.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_WithNullUsername_ShouldThrowValidationException()
    {
        // Arrange
        var email = "test@example.com";
        var passwordHash = "hashedpassword";

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.CreateUserAsync(null!, email, passwordHash));
    }

    [Fact]
    public async Task CreateUserAsync_WithEmptyUsername_ShouldThrowValidationException()
    {
        // Arrange
        var email = "test@example.com";
        var passwordHash = "hashedpassword";

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.CreateUserAsync("", email, passwordHash));
    }

    [Fact]
    public async Task CreateUserAsync_WithNullEmail_ShouldThrowValidationException()
    {
        // Arrange
        var username = "testuser";
        var passwordHash = "hashedpassword";

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.CreateUserAsync(username, null!, passwordHash));
    }

    [Fact]
    public async Task CreateUserAsync_WithNullPasswordHash_ShouldThrowValidationException()
    {
        // Arrange
        var username = "testuser";
        var email = "test@example.com";

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.CreateUserAsync(username, email, null!));
    }

    [Fact]
    public async Task CreateUserAsync_WithExistingUsername_ShouldThrowDuplicateEntityException()
    {
        // Arrange
        var username = "existinguser";
        var email = "test@example.com";
        var passwordHash = "hashedpassword";
        var existingUser = new User(username, "other@example.com", "otherhash");

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(username))
            .ReturnsAsync(existingUser);

        // Act & Assert
        await Assert.ThrowsAsync<DuplicateEntityException>(() =>
            _userService.CreateUserAsync(username, email, passwordHash));
    }

    [Fact]
    public async Task CreateUserAsync_WithExistingEmail_ShouldThrowDuplicateEntityException()
    {
        // Arrange
        var username = "testuser";
        var email = "existing@example.com";
        var passwordHash = "hashedpassword";
        var existingUser = new User("otheruser", email, "otherhash");

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(username))
            .ReturnsAsync((User)null!);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(existingUser);

        // Act & Assert
        await Assert.ThrowsAsync<DuplicateEntityException>(() =>
            _userService.CreateUserAsync(username, email, passwordHash));
    }

    #endregion

    #region GetUserByIdAsync Tests

    [Fact]
    public async Task GetUserByIdAsync_WithValidId_ShouldReturnUser()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = new User("testuser", "test@example.com", "hashedpassword");

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetUserByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(user);
        _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNonExistentId_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User)null!);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _userService.GetUserByIdAsync(userId));
    }

    #endregion

    #region GetUserByUsernameAsync Tests

    [Fact]
    public async Task GetUserByUsernameAsync_WithValidUsername_ShouldReturnUser()
    {
        // Arrange
        var username = "testuser";
        var user = new User(username, "test@example.com", "hashedpassword");

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(username))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetUserByUsernameAsync(username);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(user);
        _mockUserRepository.Verify(x => x.GetByUsernameAsync(username), Times.Once);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_WithNullUsername_ShouldThrowValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.GetUserByUsernameAsync(null!));
    }

    [Fact]
    public async Task GetUserByUsernameAsync_WithEmptyUsername_ShouldThrowValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.GetUserByUsernameAsync(""));
    }

    [Fact]
    public async Task GetUserByUsernameAsync_WithNonExistentUsername_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var username = "nonexistentuser";

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(username))
            .ReturnsAsync((User)null!);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _userService.GetUserByUsernameAsync(username));
    }

    #endregion

    #region GetUserByEmailAsync Tests

    [Fact]
    public async Task GetUserByEmailAsync_WithValidEmail_ShouldReturnUser()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User("testuser", email, "hashedpassword");

        _mockUserRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetUserByEmailAsync(email);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(user);
        _mockUserRepository.Verify(x => x.GetByEmailAsync(email), Times.Once);
    }

    [Fact]
    public async Task GetUserByEmailAsync_WithNullEmail_ShouldThrowValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.GetUserByEmailAsync(null!));
    }

    [Fact]
    public async Task GetUserByEmailAsync_WithEmptyEmail_ShouldThrowValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.GetUserByEmailAsync(""));
    }

    [Fact]
    public async Task GetUserByEmailAsync_WithNonExistentEmail_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var email = "nonexistent@example.com";

        _mockUserRepository.Setup(x => x.GetByEmailAsync(email))
            .ReturnsAsync((User)null!);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _userService.GetUserByEmailAsync(email));
    }

    #endregion

    #region GetUsersAsync Tests

    [Fact]
    public async Task GetUsersAsync_WithValidPagination_ShouldReturnUsers()
    {
        // Arrange
        var page = 1;
        var pageSize = 20;
        var users = new List<User>
        {
            new User("user1", "user1@example.com", "hash1"),
            new User("user2", "user2@example.com", "hash2")
        };

        _mockUserRepository.Setup(x => x.FindAsync(
            It.IsAny<MongoDB.Driver.FilterDefinition<User>>(),
            It.IsAny<MongoDB.Driver.SortDefinition<User>>(),
            pageSize,
            (page - 1) * pageSize))
            .ReturnsAsync(users);

        // Act
        var result = await _userService.GetUsersAsync(page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _mockUserRepository.Verify(x => x.FindAsync(
            It.IsAny<MongoDB.Driver.FilterDefinition<User>>(),
            It.IsAny<MongoDB.Driver.SortDefinition<User>>(),
            pageSize,
            (page - 1) * pageSize), Times.Once);
    }

    [Fact]
    public async Task GetUsersAsync_WithInvalidPage_ShouldThrowValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.GetUsersAsync(0, 20));
    }

    [Fact]
    public async Task GetUsersAsync_WithInvalidPageSize_ShouldThrowValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.GetUsersAsync(1, 0));
    }

    [Fact]
    public async Task GetUsersAsync_WithPageSizeTooLarge_ShouldThrowValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.GetUsersAsync(1, 101));
    }

    #endregion

    #region UpdateUserAsync Tests

    [Fact]
    public async Task UpdateUserAsync_WithValidData_ShouldUpdateUser()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = new User("testuser", "test@example.com", "hashedpassword");
        var request = new UpdateUserRequest
        {
            Username = "newusername",
            Email = "newemail@example.com"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.GetByUsernameAsync(request.Username))
            .ReturnsAsync((User)null!);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(request.Email))
            .ReturnsAsync((User)null!);
        _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.UpdateUserAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_WithNonExistentUser_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var request = new UpdateUserRequest { Username = "newusername" };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User)null!);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _userService.UpdateUserAsync(userId, request));
    }

    [Fact]
    public async Task UpdateUserAsync_WithDuplicateUsername_ShouldThrowDuplicateEntityException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = new User("testuser", "test@example.com", "hashedpassword");
        var existingUser = new User("existinguser", "other@example.com", "otherhash");
        var request = new UpdateUserRequest { Username = "existinguser" };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.GetByUsernameAsync(request.Username))
            .ReturnsAsync(existingUser);

        // Act & Assert
        await Assert.ThrowsAsync<DuplicateEntityException>(() =>
            _userService.UpdateUserAsync(userId, request));
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task DeleteUserAsync_WithValidId_ShouldDeleteUser()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = new User("testuser", "test@example.com", "hashedpassword");

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.DeleteAsync(userId))
            .Returns(Task.CompletedTask);

        // Act
        await _userService.DeleteUserAsync(userId);

        // Assert
        _mockUserRepository.Verify(x => x.DeleteAsync(userId), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_WithNonExistentId_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User)null!);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _userService.DeleteUserAsync(userId));
    }

    #endregion

    #region ActivateUserAsync Tests

    [Fact]
    public async Task ActivateUserAsync_WithValidId_ShouldActivateUser()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = new User("testuser", "test@example.com", "hashedpassword");

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.ActivateUserAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.IsActive.Should().BeTrue();
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    #endregion

    #region DeactivateUserAsync Tests

    [Fact]
    public async Task DeactivateUserAsync_WithValidId_ShouldDeactivateUser()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = new User("testuser", "test@example.com", "hashedpassword");

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.DeactivateUserAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.IsActive.Should().BeFalse();
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    #endregion

    #region VerifyEmailAsync Tests

    [Fact]
    public async Task VerifyEmailAsync_WithValidId_ShouldVerifyEmail()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = new User("testuser", "test@example.com", "hashedpassword");

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.VerifyEmailAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.IsEmailVerified.Should().BeTrue();
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    #endregion

    #region SearchUsersAsync Tests

    [Fact]
    public async Task SearchUsersAsync_WithValidQuery_ShouldReturnUsers()
    {
        // Arrange
        var query = "test";
        var page = 1;
        var pageSize = 20;
        var users = new List<User>
        {
            new User("testuser1", "test1@example.com", "hash1"),
            new User("testuser2", "test2@example.com", "hash2")
        };

        _mockUserRepository.Setup(x => x.FindAsync(
            It.IsAny<MongoDB.Driver.FilterDefinition<User>>(),
            It.IsAny<MongoDB.Driver.SortDefinition<User>>(),
            pageSize,
            (page - 1) * pageSize))
            .ReturnsAsync(users);

        // Act
        var result = await _userService.SearchUsersAsync(query, page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _mockUserRepository.Verify(x => x.FindAsync(
            It.IsAny<MongoDB.Driver.FilterDefinition<User>>(),
            It.IsAny<MongoDB.Driver.SortDefinition<User>>(),
            pageSize,
            (page - 1) * pageSize), Times.Once);
    }

    [Fact]
    public async Task SearchUsersAsync_WithNullQuery_ShouldThrowValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.SearchUsersAsync(null!, 1, 20));
    }

    [Fact]
    public async Task SearchUsersAsync_WithEmptyQuery_ShouldThrowValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.SearchUsersAsync("", 1, 20));
    }

    #endregion

    #region GetUserStatisticsAsync Tests

    [Fact]
    public async Task GetUserStatisticsAsync_ShouldReturnStatistics()
    {
        // Arrange
        var statistics = new ImageViewer.Domain.ValueObjects.UserStatistics();

        _mockUserRepository.Setup(x => x.GetUserStatisticsAsync())
            .ReturnsAsync(statistics);

        // Act
        var result = await _userService.GetUserStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(statistics);
        _mockUserRepository.Verify(x => x.GetUserStatisticsAsync(), Times.Once);
    }

    #endregion

    #region GetTopUsersByActivityAsync Tests

    [Fact]
    public async Task GetTopUsersByActivityAsync_WithValidLimit_ShouldReturnUsers()
    {
        // Arrange
        var limit = 10;
        var users = new List<User>
        {
            new User("user1", "user1@example.com", "hash1"),
            new User("user2", "user2@example.com", "hash2")
        };

        _mockUserRepository.Setup(x => x.GetTopUsersByActivityAsync(limit))
            .ReturnsAsync(users);

        // Act
        var result = await _userService.GetTopUsersByActivityAsync(limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _mockUserRepository.Verify(x => x.GetTopUsersByActivityAsync(limit), Times.Once);
    }

    [Fact]
    public async Task GetTopUsersByActivityAsync_WithInvalidLimit_ShouldThrowValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.GetTopUsersByActivityAsync(0));
    }

    [Fact]
    public async Task GetTopUsersByActivityAsync_WithLimitTooLarge_ShouldThrowValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.GetTopUsersByActivityAsync(101));
    }

    #endregion

    #region GetRecentUsersAsync Tests

    [Fact]
    public async Task GetRecentUsersAsync_WithValidLimit_ShouldReturnUsers()
    {
        // Arrange
        var limit = 10;
        var users = new List<User>
        {
            new User("user1", "user1@example.com", "hash1"),
            new User("user2", "user2@example.com", "hash2")
        };

        _mockUserRepository.Setup(x => x.GetRecentUsersAsync(limit))
            .ReturnsAsync(users);

        // Act
        var result = await _userService.GetRecentUsersAsync(limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _mockUserRepository.Verify(x => x.GetRecentUsersAsync(limit), Times.Once);
    }

    [Fact]
    public async Task GetRecentUsersAsync_WithInvalidLimit_ShouldThrowValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.GetRecentUsersAsync(0));
    }

    [Fact]
    public async Task GetRecentUsersAsync_WithLimitTooLarge_ShouldThrowValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.GetRecentUsersAsync(101));
    }

    #endregion
}
