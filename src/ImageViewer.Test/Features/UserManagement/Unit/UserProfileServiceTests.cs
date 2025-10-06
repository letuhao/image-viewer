using FluentAssertions;
using Moq;
using Xunit;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.UserProfile;
using MongoDB.Bson;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Test.Features.UserManagement.Unit;

/// <summary>
/// Unit tests for UserProfileService - User Profile Management features
/// </summary>
public class UserProfileServiceTests
{
    private readonly Mock<ILogger<UserProfileService>> _mockLogger;
    private readonly UserProfileService _userProfileService;

    public UserProfileServiceTests()
    {
        _mockLogger = new Mock<ILogger<UserProfileService>>();
        _userProfileService = new UserProfileService(_mockLogger.Object);
    }

    [Fact]
    public void UserProfileService_ShouldExist()
    {
        // Arrange & Act
        var service = _userProfileService;

        // Assert
        service.Should().NotBeNull();
        service.Should().BeOfType<UserProfileService>();
    }

    [Fact]
    public async Task ProfileCreation_WithValidRequest_ShouldCreateProfile()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var request = new CreateUserProfileRequest
        {
            UserId = userId,
            FirstName = "John",
            LastName = "Doe",
            DisplayName = "John Doe",
            Bio = "Software developer and photography enthusiast",
            Location = "New York, NY",
            Website = "https://johndoe.com",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = "Male",
            Language = "en",
            Timezone = "America/New_York",
            IsPublic = true,
            Tags = new List<string> { "photography", "travel", "nature" },
            CustomFields = new Dictionary<string, object>
            {
                { "favorite_camera", "Canon EOS R5" },
                { "experience_years", 5 }
            }
        };

        // Act
        var result = await _userProfileService.CreateProfileAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.DisplayName.Should().Be("John Doe");
        result.Bio.Should().Be("Software developer and photography enthusiast");
        result.Location.Should().Be("New York, NY");
        result.Website.Should().Be("https://johndoe.com");
        result.BirthDate.Should().Be(new DateTime(1990, 1, 1));
        result.Gender.Should().Be("Male");
        result.Language.Should().Be("en");
        result.Timezone.Should().Be("America/New_York");
        result.IsPublic.Should().BeTrue();
        result.Tags.Should().Contain("photography");
        result.Tags.Should().Contain("travel");
        result.Tags.Should().Contain("nature");
        result.CustomFields.Should().ContainKey("favorite_camera");
        result.CustomFields.Should().ContainKey("experience_years");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ProfileCreation_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        CreateUserProfileRequest request = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _userProfileService.CreateProfileAsync(request));
    }

    [Fact]
    public async Task ProfileRetrieval_WithValidUserId_ShouldReturnProfile()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var request = new CreateUserProfileRequest
        {
            UserId = userId,
            FirstName = "Jane",
            LastName = "Smith",
            DisplayName = "Jane Smith",
            Bio = "Artist and designer",
            IsPublic = true
        };

        await _userProfileService.CreateProfileAsync(request);

        // Act
        var result = await _userProfileService.GetProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.FirstName.Should().Be("Jane");
        result.LastName.Should().Be("Smith");
        result.DisplayName.Should().Be("Jane Smith");
        result.Bio.Should().Be("Artist and designer");
        result.IsPublic.Should().BeTrue();
    }

    [Fact]
    public async Task ProfileRetrieval_WithNonExistentUserId_ShouldThrowArgumentException()
    {
        // Arrange
        var nonExistentUserId = ObjectId.GenerateNewId();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _userProfileService.GetProfileAsync(nonExistentUserId));
    }

    [Fact]
    public async Task ProfileUpdate_WithValidRequest_ShouldUpdateProfile()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var createRequest = new CreateUserProfileRequest
        {
            UserId = userId,
            FirstName = "John",
            LastName = "Doe",
            DisplayName = "John Doe",
            Bio = "Original bio",
            IsPublic = true
        };

        await _userProfileService.CreateProfileAsync(createRequest);

        var updateRequest = new UpdateUserProfileRequest
        {
            FirstName = "Johnny",
            LastName = "Doe",
            DisplayName = "Johnny Doe",
            Bio = "Updated bio",
            Location = "Los Angeles, CA",
            Website = "https://johnnydoe.com",
            IsPublic = false,
            Tags = new List<string> { "updated", "tags" }
        };

        // Act
        var result = await _userProfileService.UpdateProfileAsync(userId, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.FirstName.Should().Be("Johnny");
        result.LastName.Should().Be("Doe");
        result.DisplayName.Should().Be("Johnny Doe");
        result.Bio.Should().Be("Updated bio");
        result.Location.Should().Be("Los Angeles, CA");
        result.Website.Should().Be("https://johnnydoe.com");
        result.IsPublic.Should().BeFalse();
        result.Tags.Should().Contain("updated");
        result.Tags.Should().Contain("tags");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ProfileUpdate_WithNonExistentUserId_ShouldThrowArgumentException()
    {
        // Arrange
        var nonExistentUserId = ObjectId.GenerateNewId();
        var updateRequest = new UpdateUserProfileRequest
        {
            FirstName = "Updated"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _userProfileService.UpdateProfileAsync(nonExistentUserId, updateRequest));
    }

    [Fact]
    public async Task ProfileValidation_WithValidData_ShouldReturnValidResult()
    {
        // Arrange
        var request = new UpdateUserProfileRequest
        {
            DisplayName = "Valid Display Name",
            Bio = "Valid bio content",
            Website = "https://example.com",
            BirthDate = new DateTime(1990, 1, 1),
            Language = "en",
            Timezone = "America/New_York",
            Tags = new List<string> { "tag1", "tag2" }
        };

        // Act
        var result = await _userProfileService.ValidateProfileAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
        result.FieldErrors.Should().BeEmpty();
    }

    [Fact]
    public async Task ProfileValidation_WithInvalidData_ShouldReturnInvalidResult()
    {
        // Arrange
        var request = new UpdateUserProfileRequest
        {
            DisplayName = new string('A', 101), // Too long
            Bio = new string('B', 501), // Too long
            Website = "invalid-url",
            BirthDate = DateTime.UtcNow.AddDays(1), // Future date
            Language = "invalid",
            Timezone = "invalid/timezone",
            Tags = new List<string> { new string('T', 51) } // Too long
        };

        // Act
        var result = await _userProfileService.ValidateProfileAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.FieldErrors.Should().NotBeEmpty();
        result.FieldErrors.Should().ContainKey("DisplayName");
        result.FieldErrors.Should().ContainKey("Bio");
        result.FieldErrors.Should().ContainKey("Website");
        result.FieldErrors.Should().ContainKey("BirthDate");
        result.FieldErrors.Should().ContainKey("Language");
        result.FieldErrors.Should().ContainKey("Timezone");
        result.FieldErrors.Should().ContainKey("Tags");
    }

    [Fact]
    public async Task ProfilePrivacy_WithValidSettings_ShouldUpdatePrivacySettings()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var createRequest = new CreateUserProfileRequest
        {
            UserId = userId,
            FirstName = "John",
            LastName = "Doe",
            DisplayName = "John Doe",
            IsPublic = true
        };

        await _userProfileService.CreateProfileAsync(createRequest);

        var privacySettings = new UserProfilePrivacySettings
        {
            UserId = userId,
            IsPublic = false,
            ShowEmail = false,
            ShowLocation = true,
            ShowBirthDate = false,
            ShowWebsite = true,
            ShowBio = true,
            ShowTags = true,
            ShowCustomFields = false,
            AllowSearch = true,
            AllowContact = true,
            BlockedUsers = new List<string> { "user1", "user2" },
            AllowedUsers = new List<string> { "user3", "user4" }
        };

        // Act
        var result = await _userProfileService.UpdatePrivacySettingsAsync(userId, privacySettings);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.IsPublic.Should().BeFalse();
        result.ShowEmail.Should().BeFalse();
        result.ShowLocation.Should().BeTrue();
        result.ShowBirthDate.Should().BeFalse();
        result.ShowWebsite.Should().BeTrue();
        result.ShowBio.Should().BeTrue();
        result.ShowTags.Should().BeTrue();
        result.ShowCustomFields.Should().BeFalse();
        result.AllowSearch.Should().BeTrue();
        result.AllowContact.Should().BeTrue();
        result.BlockedUsers.Should().Contain("user1");
        result.BlockedUsers.Should().Contain("user2");
        result.AllowedUsers.Should().Contain("user3");
        result.AllowedUsers.Should().Contain("user4");
    }

    [Fact]
    public async Task ProfileCustomization_WithValidSettings_ShouldUpdateCustomizationSettings()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var createRequest = new CreateUserProfileRequest
        {
            UserId = userId,
            FirstName = "John",
            LastName = "Doe",
            DisplayName = "John Doe",
            IsPublic = true
        };

        await _userProfileService.CreateProfileAsync(createRequest);

        var customizationSettings = new UserProfileCustomizationSettings
        {
            UserId = userId,
            Theme = "dark",
            ColorScheme = "dark",
            Layout = "list",
            ItemsPerPage = 50,
            ShowThumbnails = false,
            ShowMetadata = true,
            ShowTags = false,
            ShowStatistics = true,
            DefaultSort = "date",
            DefaultSortOrder = "desc",
            CustomFields = new List<string> { "custom1", "custom2" },
            CustomSettings = new Dictionary<string, object>
            {
                { "setting1", "value1" },
                { "setting2", "value2" }
            }
        };

        // Act
        var result = await _userProfileService.UpdateCustomizationSettingsAsync(userId, customizationSettings);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Theme.Should().Be("dark");
        result.ColorScheme.Should().Be("dark");
        result.Layout.Should().Be("list");
        result.ItemsPerPage.Should().Be(50);
        result.ShowThumbnails.Should().BeFalse();
        result.ShowMetadata.Should().BeTrue();
        result.ShowTags.Should().BeFalse();
        result.ShowStatistics.Should().BeTrue();
        result.DefaultSort.Should().Be("date");
        result.DefaultSortOrder.Should().Be("desc");
        result.CustomFields.Should().Contain("custom1");
        result.CustomFields.Should().Contain("custom2");
        result.CustomSettings.Should().ContainKey("setting1");
        result.CustomSettings.Should().ContainKey("setting2");
    }

    [Fact]
    public async Task ProfileStatistics_WithValidUserId_ShouldReturnStatistics()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var createRequest = new CreateUserProfileRequest
        {
            UserId = userId,
            FirstName = "John",
            LastName = "Doe",
            DisplayName = "John Doe",
            IsPublic = true
        };

        await _userProfileService.CreateProfileAsync(createRequest);

        // Act
        var result = await _userProfileService.GetProfileStatisticsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.TotalCollections.Should().BeGreaterThanOrEqualTo(0);
        result.TotalMediaItems.Should().BeGreaterThanOrEqualTo(0);
        result.TotalStorageUsed.Should().BeGreaterThanOrEqualTo(0);
        result.TotalViews.Should().BeGreaterThanOrEqualTo(0);
        result.TotalDownloads.Should().BeGreaterThanOrEqualTo(0);
        result.TotalShares.Should().BeGreaterThanOrEqualTo(0);
        result.TotalLikes.Should().BeGreaterThanOrEqualTo(0);
        result.TotalComments.Should().BeGreaterThanOrEqualTo(0);
        result.LastActivity.Should().BeBefore(DateTime.UtcNow);
        result.ProfileCreated.Should().BeBefore(DateTime.UtcNow);
        result.LastUpdated.Should().BeBefore(DateTime.UtcNow);
        result.ActivityByMonth.Should().NotBeEmpty();
        result.PopularTags.Should().NotBeEmpty();
        result.StorageByType.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ProfileDeletion_WithValidUserId_ShouldDeleteProfile()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var createRequest = new CreateUserProfileRequest
        {
            UserId = userId,
            FirstName = "John",
            LastName = "Doe",
            DisplayName = "John Doe",
            IsPublic = true
        };

        await _userProfileService.CreateProfileAsync(createRequest);

        // Act
        var result = await _userProfileService.DeleteProfileAsync(userId);

        // Assert
        result.Should().BeTrue();

        // Verify profile is deleted
        await Assert.ThrowsAsync<ArgumentException>(() => _userProfileService.GetProfileAsync(userId));
    }

    [Fact]
    public async Task ProfileDeletion_WithNonExistentUserId_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentUserId = ObjectId.GenerateNewId();

        // Act
        var result = await _userProfileService.DeleteProfileAsync(nonExistentUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SearchProfiles_WithValidQuery_ShouldReturnMatchingProfiles()
    {
        // Arrange
        var userId1 = ObjectId.GenerateNewId();
        var userId2 = ObjectId.GenerateNewId();
        var userId3 = ObjectId.GenerateNewId();

        var createRequest1 = new CreateUserProfileRequest
        {
            UserId = userId1,
            FirstName = "John",
            LastName = "Doe",
            DisplayName = "John Doe",
            Bio = "Photography enthusiast",
            Location = "New York",
            Tags = new List<string> { "photography", "travel" },
            IsPublic = true
        };

        var createRequest2 = new CreateUserProfileRequest
        {
            UserId = userId2,
            FirstName = "Jane",
            LastName = "Smith",
            DisplayName = "Jane Smith",
            Bio = "Artist and designer",
            Location = "Los Angeles",
            Tags = new List<string> { "art", "design" },
            IsPublic = true
        };

        var createRequest3 = new CreateUserProfileRequest
        {
            UserId = userId3,
            FirstName = "Bob",
            LastName = "Johnson",
            DisplayName = "Bob Johnson",
            Bio = "Software developer",
            Location = "Seattle",
            Tags = new List<string> { "programming", "tech" },
            IsPublic = false // Private profile
        };

        await _userProfileService.CreateProfileAsync(createRequest1);
        await _userProfileService.CreateProfileAsync(createRequest2);
        await _userProfileService.CreateProfileAsync(createRequest3);

        // Act
        var result = await _userProfileService.SearchProfilesAsync("photography");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().UserId.Should().Be(userId1);
        result.First().DisplayName.Should().Be("John Doe");
    }

    [Fact]
    public async Task GetPublicProfiles_ShouldReturnOnlyPublicProfiles()
    {
        // Arrange
        var userId1 = ObjectId.GenerateNewId();
        var userId2 = ObjectId.GenerateNewId();

        var createRequest1 = new CreateUserProfileRequest
        {
            UserId = userId1,
            FirstName = "John",
            LastName = "Doe",
            DisplayName = "John Doe",
            IsPublic = true
        };

        var createRequest2 = new CreateUserProfileRequest
        {
            UserId = userId2,
            FirstName = "Jane",
            LastName = "Smith",
            DisplayName = "Jane Smith",
            IsPublic = false
        };

        await _userProfileService.CreateProfileAsync(createRequest1);
        await _userProfileService.CreateProfileAsync(createRequest2);

        // Act
        var result = await _userProfileService.GetPublicProfilesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().UserId.Should().Be(userId1);
        result.First().IsPublic.Should().BeTrue();
    }
}
