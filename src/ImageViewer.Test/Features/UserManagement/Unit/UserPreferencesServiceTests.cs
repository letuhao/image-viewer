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
/// Unit tests for UserPreferencesService - User Preferences Management features
/// </summary>
public class UserPreferencesServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IUserSettingRepository> _mockUserSettingRepository;
    private readonly Mock<ILogger<UserPreferencesService>> _mockLogger;
    private readonly UserPreferencesService _userPreferencesService;

    public UserPreferencesServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUserSettingRepository = new Mock<IUserSettingRepository>();
        _mockLogger = new Mock<ILogger<UserPreferencesService>>();
        _userPreferencesService = new UserPreferencesService(
            _mockUserRepository.Object, 
            _mockUserSettingRepository.Object, 
            _mockLogger.Object);
    }

    #region GetUserPreferencesAsync Tests

    [Fact]
    public async Task GetUserPreferencesAsync_WithExistingUser_ShouldReturnPreferences()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = new User("testuser", "test@example.com", "hashedpassword");
        var userSetting = UserSetting.Create(userId, "user_preferences", "{}", "JSON", "General", "User preferences", false, false, null, "User");

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockUserSettingRepository.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userSetting);

        // Act
        var result = await _userPreferencesService.GetUserPreferencesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Display.Should().NotBeNull();
        result.Privacy.Should().NotBeNull();
        result.Performance.Should().NotBeNull();
        result.Notifications.Should().NotBeNull();
        _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        _mockUserSettingRepository.Verify(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserPreferencesAsync_WithNonExistentUser_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User)null!);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _userPreferencesService.GetUserPreferencesAsync(userId));
    }

    [Fact]
    public async Task GetUserPreferencesAsync_WithNoExistingSettings_ShouldReturnDefaultPreferences()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = new User("testuser", "test@example.com", "hashedpassword");

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockUserSettingRepository.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSetting)null!);

        // Act
        var result = await _userPreferencesService.GetUserPreferencesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Display.Should().NotBeNull();
        result.Privacy.Should().NotBeNull();
        result.Performance.Should().NotBeNull();
        result.Notifications.Should().NotBeNull();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region UpdateUserPreferencesAsync Tests

    [Fact]
    public async Task UpdateUserPreferencesAsync_WithValidRequest_ShouldUpdatePreferences()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = new User("testuser", "test@example.com", "hashedpassword");
        var request = new UpdateUserPreferencesRequest
        {
            Display = new DisplayPreferences
            {
                DisplayMode = DisplayMode.List,
                ItemsPerPage = 50,
                Theme = "dark"
            }
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockUserSettingRepository.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSetting)null!);
        _mockUserSettingRepository.Setup(x => x.CreateAsync(It.IsAny<UserSetting>()))
            .ReturnsAsync((UserSetting us) => us);

        // Act
        var result = await _userPreferencesService.UpdateUserPreferencesAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Display.DisplayMode.Should().Be(DisplayMode.List);
        result.Display.ItemsPerPage.Should().Be(50);
        result.Display.Theme.Should().Be("dark");
        _mockUserSettingRepository.Verify(x => x.CreateAsync(It.IsAny<UserSetting>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserPreferencesAsync_WithNullRequest_ShouldThrowValidationException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userPreferencesService.UpdateUserPreferencesAsync(userId, null!));
    }

    [Fact]
    public async Task UpdateUserPreferencesAsync_WithNonExistentUser_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var request = new UpdateUserPreferencesRequest();

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User)null!);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _userPreferencesService.UpdateUserPreferencesAsync(userId, request));
    }

    [Fact]
    public async Task UpdateUserPreferencesAsync_WithInvalidDisplayPreferences_ShouldThrowValidationException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = new User("testuser", "test@example.com", "hashedpassword");
        var request = new UpdateUserPreferencesRequest
        {
            Display = new DisplayPreferences
            {
                ItemsPerPage = 0, // Invalid: should be between 1 and 100
                ThumbnailSize = 30, // Invalid: should be between 50 and 500
                Theme = "", // Invalid: should not be empty
                Language = "" // Invalid: should not be empty
            }
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userPreferencesService.UpdateUserPreferencesAsync(userId, request));
    }

    [Fact]
    public async Task UpdateUserPreferencesAsync_WithInvalidPerformancePreferences_ShouldThrowValidationException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = new User("testuser", "test@example.com", "hashedpassword");
        var request = new UpdateUserPreferencesRequest
        {
            Performance = new PerformancePreferences
            {
                CacheSize = 5, // Invalid: should be between 10 and 1000
                MaxConcurrentDownloads = 0, // Invalid: should be between 1 and 10
                AutoSaveInterval = 2 // Invalid: should be between 5 and 300
            }
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userPreferencesService.UpdateUserPreferencesAsync(userId, request));
    }

    #endregion

    #region ResetUserPreferencesAsync Tests

    [Fact]
    public async Task ResetUserPreferencesAsync_WithValidUser_ShouldResetToDefaults()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = new User("testuser", "test@example.com", "hashedpassword");

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockUserSettingRepository.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSetting)null!);
        _mockUserSettingRepository.Setup(x => x.CreateAsync(It.IsAny<UserSetting>()))
            .ReturnsAsync((UserSetting us) => us);

        // Act
        var result = await _userPreferencesService.ResetUserPreferencesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Display.Should().NotBeNull();
        result.Privacy.Should().NotBeNull();
        result.Performance.Should().NotBeNull();
        result.Notifications.Should().NotBeNull();
        _mockUserSettingRepository.Verify(x => x.CreateAsync(It.IsAny<UserSetting>()), Times.Once);
    }

    [Fact]
    public async Task ResetUserPreferencesAsync_WithNonExistentUser_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User)null!);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _userPreferencesService.ResetUserPreferencesAsync(userId));
    }

    #endregion

    #region ValidatePreferencesAsync Tests

    [Fact]
    public async Task ValidatePreferencesAsync_WithValidRequest_ShouldReturnTrue()
    {
        // Arrange
        var request = new UpdateUserPreferencesRequest
        {
            Display = new DisplayPreferences
            {
                ItemsPerPage = 20,
                ThumbnailSize = 200,
                Theme = "light",
                Language = "en"
            },
            Performance = new PerformancePreferences
            {
                CacheSize = 100,
                MaxConcurrentDownloads = 3,
                AutoSaveInterval = 30
            }
        };

        // Act
        var result = await _userPreferencesService.ValidatePreferencesAsync(request);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePreferencesAsync_WithNullRequest_ShouldReturnFalse()
    {
        // Act
        var result = await _userPreferencesService.ValidatePreferencesAsync(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePreferencesAsync_WithInvalidDisplayPreferences_ShouldReturnFalse()
    {
        // Arrange
        var request = new UpdateUserPreferencesRequest
        {
            Display = new DisplayPreferences
            {
                ItemsPerPage = 0, // Invalid
                ThumbnailSize = 30, // Invalid
                Theme = "", // Invalid
                Language = "" // Invalid
            }
        };

        // Act
        var result = await _userPreferencesService.ValidatePreferencesAsync(request);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePreferencesAsync_WithInvalidPerformancePreferences_ShouldReturnFalse()
    {
        // Arrange
        var request = new UpdateUserPreferencesRequest
        {
            Performance = new PerformancePreferences
            {
                CacheSize = 5, // Invalid
                MaxConcurrentDownloads = 0, // Invalid
                AutoSaveInterval = 2 // Invalid
            }
        };

        // Act
        var result = await _userPreferencesService.ValidatePreferencesAsync(request);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetDisplayPreferencesAsync Tests

    [Fact]
    public async Task GetDisplayPreferencesAsync_WithValidUser_ShouldReturnDisplayPreferences()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = new User("testuser", "test@example.com", "hashedpassword");

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockUserSettingRepository.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSetting)null!);

        // Act
        var result = await _userPreferencesService.GetDisplayPreferencesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.DisplayMode.Should().Be(DisplayMode.Grid);
        result.ItemsPerPage.Should().Be(20);
        result.ThumbnailSize.Should().Be(200);
        result.Theme.Should().Be("light");
        result.Language.Should().Be("en");
    }

    #endregion

    #region UpdateDisplayPreferencesAsync Tests

    [Fact]
    public async Task UpdateDisplayPreferencesAsync_WithValidRequest_ShouldUpdateDisplayPreferences()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = new User("testuser", "test@example.com", "hashedpassword");
        var request = new UpdateDisplayPreferencesRequest
        {
            DisplayMode = DisplayMode.List,
            ItemsPerPage = 50,
            ThumbnailSize = 300,
            Theme = "dark",
            Language = "es"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockUserSettingRepository.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSetting)null!);
        _mockUserSettingRepository.Setup(x => x.CreateAsync(It.IsAny<UserSetting>()))
            .ReturnsAsync((UserSetting us) => us);

        // Act
        var result = await _userPreferencesService.UpdateDisplayPreferencesAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.DisplayMode.Should().Be(DisplayMode.List);
        result.ItemsPerPage.Should().Be(50);
        result.ThumbnailSize.Should().Be(300);
        result.Theme.Should().Be("dark");
        result.Language.Should().Be("es");
    }

    [Fact]
    public async Task UpdateDisplayPreferencesAsync_WithNullRequest_ShouldThrowValidationException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userPreferencesService.UpdateDisplayPreferencesAsync(userId, null!));
    }

    #endregion

    #region GetPrivacyPreferencesAsync Tests

    [Fact]
    public async Task GetPrivacyPreferencesAsync_WithValidUser_ShouldReturnPrivacyPreferences()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = new User("testuser", "test@example.com", "hashedpassword");

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockUserSettingRepository.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSetting)null!);

        // Act
        var result = await _userPreferencesService.GetPrivacyPreferencesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.ProfilePublic.Should().BeFalse();
        result.ShowOnlineStatus.Should().BeTrue();
        result.AllowDirectMessages.Should().BeTrue();
        result.ShowActivity.Should().BeTrue();
        result.AllowSearchIndexing.Should().BeTrue();
        result.ShareUsageData.Should().BeFalse();
        result.AllowAnalytics.Should().BeTrue();
        result.AllowCookies.Should().BeTrue();
    }

    #endregion

    #region UpdatePrivacyPreferencesAsync Tests

    [Fact]
    public async Task UpdatePrivacyPreferencesAsync_WithValidRequest_ShouldUpdatePrivacyPreferences()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = new User("testuser", "test@example.com", "hashedpassword");
        var request = new UpdatePrivacyPreferencesRequest
        {
            ProfilePublic = true,
            ShowOnlineStatus = false,
            AllowDirectMessages = false,
            ShowActivity = false,
            AllowSearchIndexing = false,
            ShareUsageData = true,
            AllowAnalytics = false,
            AllowCookies = false
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockUserSettingRepository.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSetting)null!);
        _mockUserSettingRepository.Setup(x => x.CreateAsync(It.IsAny<UserSetting>()))
            .ReturnsAsync((UserSetting us) => us);

        // Act
        var result = await _userPreferencesService.UpdatePrivacyPreferencesAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.ProfilePublic.Should().BeTrue();
        result.ShowOnlineStatus.Should().BeFalse();
        result.AllowDirectMessages.Should().BeFalse();
        result.ShowActivity.Should().BeFalse();
        result.AllowSearchIndexing.Should().BeFalse();
        result.ShareUsageData.Should().BeTrue();
        result.AllowAnalytics.Should().BeFalse();
        result.AllowCookies.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePrivacyPreferencesAsync_WithNullRequest_ShouldThrowValidationException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userPreferencesService.UpdatePrivacyPreferencesAsync(userId, null!));
    }

    #endregion

    #region GetPerformancePreferencesAsync Tests

    [Fact]
    public async Task GetPerformancePreferencesAsync_WithValidUser_ShouldReturnPerformancePreferences()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = new User("testuser", "test@example.com", "hashedpassword");

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockUserSettingRepository.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSetting)null!);

        // Act
        var result = await _userPreferencesService.GetPerformancePreferencesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.CacheSize.Should().Be(100);
        result.EnableLazyLoading.Should().BeTrue();
        result.EnableImageOptimization.Should().BeTrue();
        result.MaxConcurrentDownloads.Should().Be(3);
        result.EnableBackgroundSync.Should().BeTrue();
        result.AutoSaveInterval.Should().Be(30);
        result.EnableCompression.Should().BeTrue();
        result.EnableCaching.Should().BeTrue();
    }

    #endregion

    #region UpdatePerformancePreferencesAsync Tests

    [Fact]
    public async Task UpdatePerformancePreferencesAsync_WithValidRequest_ShouldUpdatePerformancePreferences()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var user = new User("testuser", "test@example.com", "hashedpassword");
        var request = new UpdatePerformancePreferencesRequest
        {
            CacheSize = 500,
            EnableLazyLoading = false,
            EnableImageOptimization = false,
            MaxConcurrentDownloads = 5,
            EnableBackgroundSync = false,
            AutoSaveInterval = 60,
            EnableCompression = false,
            EnableCaching = false
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);
        _mockUserSettingRepository.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSetting)null!);
        _mockUserSettingRepository.Setup(x => x.CreateAsync(It.IsAny<UserSetting>()))
            .ReturnsAsync((UserSetting us) => us);

        // Act
        var result = await _userPreferencesService.UpdatePerformancePreferencesAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.CacheSize.Should().Be(500);
        result.EnableLazyLoading.Should().BeFalse();
        result.EnableImageOptimization.Should().BeFalse();
        result.MaxConcurrentDownloads.Should().Be(5);
        result.EnableBackgroundSync.Should().BeFalse();
        result.AutoSaveInterval.Should().Be(60);
        result.EnableCompression.Should().BeFalse();
        result.EnableCaching.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePerformancePreferencesAsync_WithNullRequest_ShouldThrowValidationException()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userPreferencesService.UpdatePerformancePreferencesAsync(userId, null!));
    }

    #endregion
}
