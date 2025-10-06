using ImageViewer.Application.DTOs.UserProfile;
using MongoDB.Bson;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service interface for user profile management operations
/// </summary>
public interface IUserProfileService
{
    /// <summary>
    /// Creates a new user profile
    /// </summary>
    /// <param name="request">Profile creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user profile</returns>
    Task<UserProfileDto> CreateProfileAsync(CreateUserProfileRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user profile by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User profile</returns>
    Task<UserProfileDto> GetProfileAsync(ObjectId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user profile
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Profile update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user profile</returns>
    Task<UserProfileDto> UpdateProfileAsync(ObjectId userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user profile
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if profile was deleted successfully</returns>
    Task<bool> DeleteProfileAsync(ObjectId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a user profile
    /// </summary>
    /// <param name="request">Profile validation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<UserProfileValidationResult> ValidateProfileAsync(UpdateUserProfileRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user profile privacy settings
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Privacy settings</returns>
    Task<UserProfilePrivacySettings> GetPrivacySettingsAsync(ObjectId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user profile privacy settings
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="settings">Privacy settings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated privacy settings</returns>
    Task<UserProfilePrivacySettings> UpdatePrivacySettingsAsync(ObjectId userId, UserProfilePrivacySettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user profile customization settings
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Customization settings</returns>
    Task<UserProfileCustomizationSettings> GetCustomizationSettingsAsync(ObjectId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user profile customization settings
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="settings">Customization settings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated customization settings</returns>
    Task<UserProfileCustomizationSettings> UpdateCustomizationSettingsAsync(ObjectId userId, UserProfileCustomizationSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user profile statistics
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Profile statistics</returns>
    Task<UserProfileStatistics> GetProfileStatisticsAsync(ObjectId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches user profiles
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching profiles</returns>
    Task<List<UserProfileDto>> SearchProfilesAsync(string query, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets public user profiles
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of public profiles</returns>
    Task<List<UserProfileDto>> GetPublicProfilesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}
