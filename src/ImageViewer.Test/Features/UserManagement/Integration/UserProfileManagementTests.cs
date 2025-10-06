using FluentAssertions;
using ImageViewer.Application.DTOs.UserProfile;
using ImageViewer.Application.Services;
using ImageViewer.Test.Shared.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;

namespace ImageViewer.Test.Features.UserManagement.Integration;

/// <summary>
/// Integration tests for User Profile Management - End-to-end user profile scenarios
/// </summary>
[Collection("Integration")]
public class UserProfileManagementTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly IUserProfileService _userProfileService;

    public UserProfileManagementTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _userProfileService = _fixture.GetService<IUserProfileService>();
    }

    [Fact]
    public async Task UserProfile_CreateProfile_ShouldCreateProfile()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateUserProfileRequest
        {
            UserId = ObjectId.GenerateNewId(),
            FirstName = "John",
            LastName = "Doe",
            DisplayName = "John Doe",
            Bio = "Software developer and photographer",
            Location = "New York, NY",
            Website = "https://johndoe.com",
            Language = "en",
            Timezone = "America/New_York",
            IsPublic = true,
            Tags = new List<string> { "photography", "software", "travel" }
        };

        // Act
        var result = await _userProfileService.CreateProfileAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(request.UserId);
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.DisplayName.Should().Be("John Doe");
        result.Bio.Should().Be("Software developer and photographer");
        result.Location.Should().Be("New York, NY");
        result.Website.Should().Be("https://johndoe.com");
        result.Language.Should().Be("en");
        result.Timezone.Should().Be("America/New_York");
        result.IsPublic.Should().BeTrue();
        result.Tags.Should().HaveCount(3);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UserProfile_GetProfile_ShouldRetrieveProfile()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var userId = ObjectId.GenerateNewId();

        // Act
        var result = await _userProfileService.GetProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Username.Should().NotBeNullOrEmpty();
        result.Email.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UserProfile_UpdateProfile_ShouldUpdateProfile()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var userId = ObjectId.GenerateNewId();
        var request = new UpdateUserProfileRequest
        {
            FirstName = "Jane",
            LastName = "Smith",
            DisplayName = "Jane Smith",
            Bio = "Updated bio",
            Location = "San Francisco, CA",
            Website = "https://janesmith.com",
            Language = "es",
            Timezone = "America/Los_Angeles",
            IsPublic = false,
            Tags = new List<string> { "design", "art", "music" }
        };

        // Act
        var result = await _userProfileService.UpdateProfileAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.FirstName.Should().Be("Jane");
        result.LastName.Should().Be("Smith");
        result.DisplayName.Should().Be("Jane Smith");
        result.Bio.Should().Be("Updated bio");
        result.Location.Should().Be("San Francisco, CA");
        result.Website.Should().Be("https://janesmith.com");
        result.Language.Should().Be("es");
        result.Timezone.Should().Be("America/Los_Angeles");
        result.IsPublic.Should().BeFalse();
        result.Tags.Should().HaveCount(3);
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UserProfile_DeleteProfile_ShouldDeleteProfile()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var userId = ObjectId.GenerateNewId();

        // Act
        var result = await _userProfileService.DeleteProfileAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UserProfile_ValidateProfile_ShouldValidateProfile()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new UpdateUserProfileRequest
        {
            FirstName = "Valid",
            LastName = "Name",
            DisplayName = "Valid Display Name",
            Bio = "Valid bio",
            Website = "https://valid.com"
        };

        // Act
        var result = await _userProfileService.ValidateProfileAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task UserProfile_PrivacySettings_ShouldManagePrivacySettings()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var userId = ObjectId.GenerateNewId();
        var settings = new UserProfilePrivacySettings
        {
            UserId = userId,
            IsPublic = false,
            ShowEmail = false,
            ShowLocation = true,
            ShowBirthDate = false,
            ShowWebsite = true,
            ShowBio = true,
            AllowSearch = false,
            AllowContact = true
        };

        // Act
        var result = await _userProfileService.UpdatePrivacySettingsAsync(userId, settings);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.IsPublic.Should().BeFalse();
        result.ShowEmail.Should().BeFalse();
        result.ShowLocation.Should().BeTrue();
        result.ShowBirthDate.Should().BeFalse();
        result.ShowWebsite.Should().BeTrue();
        result.ShowBio.Should().BeTrue();
        result.AllowSearch.Should().BeFalse();
        result.AllowContact.Should().BeTrue();
    }

    [Fact]
    public async Task UserProfile_CustomizationSettings_ShouldManageCustomizationSettings()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var userId = ObjectId.GenerateNewId();
        var settings = new UserProfileCustomizationSettings
        {
            UserId = userId,
            Theme = "dark",
            ColorScheme = "dark",
            Layout = "list",
            ItemsPerPage = 50,
            ShowThumbnails = false,
            ShowMetadata = true,
            ShowTags = true,
            ShowStatistics = false,
            DefaultSort = "date",
            DefaultSortOrder = "desc"
        };

        // Act
        var result = await _userProfileService.UpdateCustomizationSettingsAsync(userId, settings);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Theme.Should().Be("dark");
        result.ColorScheme.Should().Be("dark");
        result.Layout.Should().Be("list");
        result.ItemsPerPage.Should().Be(50);
        result.ShowThumbnails.Should().BeFalse();
        result.ShowMetadata.Should().BeTrue();
        result.ShowTags.Should().BeTrue();
        result.ShowStatistics.Should().BeFalse();
        result.DefaultSort.Should().Be("date");
        result.DefaultSortOrder.Should().Be("desc");
    }

    [Fact]
    public async Task UserProfile_ProfileStatistics_ShouldGetProfileStatistics()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var userId = ObjectId.GenerateNewId();

        // Act
        var result = await _userProfileService.GetProfileStatisticsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.TotalCollections.Should().BeGreaterOrEqualTo(0);
        result.TotalMediaItems.Should().BeGreaterOrEqualTo(0);
        result.TotalStorageUsed.Should().BeGreaterOrEqualTo(0);
        result.TotalViews.Should().BeGreaterOrEqualTo(0);
        result.TotalDownloads.Should().BeGreaterOrEqualTo(0);
        result.TotalShares.Should().BeGreaterOrEqualTo(0);
        result.TotalLikes.Should().BeGreaterOrEqualTo(0);
        result.TotalComments.Should().BeGreaterOrEqualTo(0);
        result.ActivityByMonth.Should().NotBeNull();
        result.PopularTags.Should().NotBeNull();
        result.StorageByType.Should().NotBeNull();
    }

    [Fact]
    public async Task UserProfile_SearchProfiles_ShouldSearchProfiles()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var query = "test";
        var page = 1;
        var pageSize = 10;

        // Act
        var result = await _userProfileService.SearchProfilesAsync(query, page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<UserProfileDto>>();
    }

    [Fact]
    public async Task UserProfile_PublicProfiles_ShouldGetPublicProfiles()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var page = 1;
        var pageSize = 10;

        // Act
        var result = await _userProfileService.GetPublicProfilesAsync(page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<UserProfileDto>>();
    }
}