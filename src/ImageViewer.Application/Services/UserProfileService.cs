using Microsoft.Extensions.Logging;
using ImageViewer.Application.DTOs.UserProfile;
using MongoDB.Bson;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service for user profile management operations
/// </summary>
public class UserProfileService : IUserProfileService
{
    private readonly ILogger<UserProfileService> _logger;
    private readonly Dictionary<ObjectId, UserProfileDto> _profiles = new();
    private readonly Dictionary<ObjectId, UserProfilePrivacySettings> _privacySettings = new();
    private readonly Dictionary<ObjectId, UserProfileCustomizationSettings> _customizationSettings = new();

    public UserProfileService(ILogger<UserProfileService> logger)
    {
        _logger = logger;
    }

    public async Task<UserProfileDto> CreateProfileAsync(CreateUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _logger.LogInformation("Creating profile for user {UserId}", request.UserId);

        try
        {
            var profile = new UserProfileDto
            {
                Id = ObjectId.GenerateNewId(),
                UserId = request.UserId,
                Username = $"user_{request.UserId}", // Would be retrieved from user service
                Email = $"user_{request.UserId}@example.com", // Would be retrieved from user service
                FirstName = request.FirstName,
                LastName = request.LastName,
                DisplayName = request.DisplayName,
                Bio = request.Bio,
                Location = request.Location,
                Website = request.Website,
                BirthDate = request.BirthDate,
                Gender = request.Gender,
                Language = request.Language,
                Timezone = request.Timezone,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsPublic = request.IsPublic,
                IsVerified = false,
                Tags = request.Tags,
                CustomFields = request.CustomFields
            };

            _profiles[request.UserId] = profile;

            // Initialize default privacy settings
            _privacySettings[request.UserId] = new UserProfilePrivacySettings
            {
                UserId = request.UserId,
                IsPublic = request.IsPublic,
                ShowEmail = false,
                ShowLocation = true,
                ShowBirthDate = false,
                ShowWebsite = true,
                ShowBio = true,
                ShowTags = true,
                ShowCustomFields = false,
                AllowSearch = true,
                AllowContact = true
            };

            // Initialize default customization settings
            _customizationSettings[request.UserId] = new UserProfileCustomizationSettings
            {
                UserId = request.UserId,
                Theme = "default",
                ColorScheme = "light",
                Layout = "grid",
                ItemsPerPage = 20,
                ShowThumbnails = true,
                ShowMetadata = true,
                ShowTags = true,
                ShowStatistics = true,
                DefaultSort = "name",
                DefaultSortOrder = "asc"
            };

            _logger.LogInformation("Profile created successfully for user {UserId}", request.UserId);
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile for user {UserId}", request.UserId);
            throw;
        }
    }

    public async Task<UserProfileDto> GetProfileAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting profile for user {UserId}", userId);

        try
        {
            if (_profiles.TryGetValue(userId, out var profile))
            {
                return profile;
            }

            throw new ArgumentException($"Profile not found for user {userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserProfileDto> UpdateProfileAsync(ObjectId userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating profile for user {UserId}", userId);

        try
        {
            if (!_profiles.TryGetValue(userId, out var profile))
            {
                throw new ArgumentException($"Profile not found for user {userId}");
            }

            // Update profile fields
            if (request.FirstName != null) profile.FirstName = request.FirstName;
            if (request.LastName != null) profile.LastName = request.LastName;
            if (request.DisplayName != null) profile.DisplayName = request.DisplayName;
            if (request.Avatar != null) profile.Avatar = request.Avatar;
            if (request.Bio != null) profile.Bio = request.Bio;
            if (request.Location != null) profile.Location = request.Location;
            if (request.Website != null) profile.Website = request.Website;
            if (request.BirthDate.HasValue) profile.BirthDate = request.BirthDate;
            if (request.Gender != null) profile.Gender = request.Gender;
            if (request.Language != null) profile.Language = request.Language;
            if (request.Timezone != null) profile.Timezone = request.Timezone;
            if (request.IsPublic.HasValue) profile.IsPublic = request.IsPublic.Value;
            if (request.Tags != null) profile.Tags = request.Tags;
            if (request.CustomFields != null) profile.CustomFields = request.CustomFields;

            profile.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Profile updated successfully for user {UserId}", userId);
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DeleteProfileAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting profile for user {UserId}", userId);

        try
        {
            var deleted = _profiles.Remove(userId);
            _privacySettings.Remove(userId);
            _customizationSettings.Remove(userId);

            _logger.LogInformation("Profile deleted successfully for user {UserId}", userId);
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting profile for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserProfileValidationResult> ValidateProfileAsync(UpdateUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating profile");

        try
        {
            var result = new UserProfileValidationResult { IsValid = true };

            // Validate display name
            if (!string.IsNullOrEmpty(request.DisplayName) && request.DisplayName.Length > 100)
            {
                result.IsValid = false;
                result.Errors.Add("Display name cannot exceed 100 characters");
                result.FieldErrors["DisplayName"] = "Display name cannot exceed 100 characters";
            }

            // Validate bio
            if (!string.IsNullOrEmpty(request.Bio) && request.Bio.Length > 500)
            {
                result.IsValid = false;
                result.Errors.Add("Bio cannot exceed 500 characters");
                result.FieldErrors["Bio"] = "Bio cannot exceed 500 characters";
            }

            // Validate website URL
            if (!string.IsNullOrEmpty(request.Website) && !IsValidUrl(request.Website))
            {
                result.IsValid = false;
                result.Errors.Add("Website must be a valid URL");
                result.FieldErrors["Website"] = "Website must be a valid URL";
            }

            // Validate birth date
            if (request.BirthDate.HasValue && request.BirthDate.Value > DateTime.UtcNow)
            {
                result.IsValid = false;
                result.Errors.Add("Birth date cannot be in the future");
                result.FieldErrors["BirthDate"] = "Birth date cannot be in the future";
            }

            // Validate language
            if (!string.IsNullOrEmpty(request.Language) && !IsValidLanguage(request.Language))
            {
                result.IsValid = false;
                result.Errors.Add("Language must be a valid language code");
                result.FieldErrors["Language"] = "Language must be a valid language code";
            }

            // Validate timezone
            if (!string.IsNullOrEmpty(request.Timezone) && !IsValidTimezone(request.Timezone))
            {
                result.IsValid = false;
                result.Errors.Add("Timezone must be a valid timezone identifier");
                result.FieldErrors["Timezone"] = "Timezone must be a valid timezone identifier";
            }

            // Validate tags
            if (request.Tags != null && request.Tags.Count > 10)
            {
                result.IsValid = false;
                result.Errors.Add("Cannot have more than 10 tags");
                result.FieldErrors["Tags"] = "Cannot have more than 10 tags";
            }

            if (request.Tags != null && request.Tags.Any(tag => tag.Length > 50))
            {
                result.IsValid = false;
                result.Errors.Add("Tags cannot exceed 50 characters");
                result.FieldErrors["Tags"] = "Tags cannot exceed 50 characters";
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating profile");
            throw;
        }
    }

    public async Task<UserProfilePrivacySettings> GetPrivacySettingsAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting privacy settings for user {UserId}", userId);

        try
        {
            if (_privacySettings.TryGetValue(userId, out var settings))
            {
                return settings;
            }

            throw new ArgumentException($"Privacy settings not found for user {userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting privacy settings for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserProfilePrivacySettings> UpdatePrivacySettingsAsync(ObjectId userId, UserProfilePrivacySettings settings, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating privacy settings for user {UserId}", userId);

        try
        {
            settings.UserId = userId;
            _privacySettings[userId] = settings;

            _logger.LogInformation("Privacy settings updated successfully for user {UserId}", userId);
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating privacy settings for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserProfileCustomizationSettings> GetCustomizationSettingsAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting customization settings for user {UserId}", userId);

        try
        {
            if (_customizationSettings.TryGetValue(userId, out var settings))
            {
                return settings;
            }

            throw new ArgumentException($"Customization settings not found for user {userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customization settings for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserProfileCustomizationSettings> UpdateCustomizationSettingsAsync(ObjectId userId, UserProfileCustomizationSettings settings, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating customization settings for user {UserId}", userId);

        try
        {
            settings.UserId = userId;
            _customizationSettings[userId] = settings;

            _logger.LogInformation("Customization settings updated successfully for user {UserId}", userId);
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customization settings for user {UserId}", userId);
            throw;
        }
    }

    public async Task<UserProfileStatistics> GetProfileStatisticsAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting profile statistics for user {UserId}", userId);

        try
        {
            var statistics = new UserProfileStatistics
            {
                UserId = userId,
                TotalCollections = Random.Shared.Next(0, 100),
                TotalMediaItems = Random.Shared.Next(0, 1000),
                TotalStorageUsed = Random.Shared.NextInt64(0, 10L * 1024 * 1024 * 1024), // 0-10GB
                TotalViews = Random.Shared.Next(0, 10000),
                TotalDownloads = Random.Shared.Next(0, 1000),
                TotalShares = Random.Shared.Next(0, 500),
                TotalLikes = Random.Shared.Next(0, 2000),
                TotalComments = Random.Shared.Next(0, 100),
                LastActivity = DateTime.UtcNow.AddDays(-Random.Shared.Next(0, 30)),
                ProfileCreated = DateTime.UtcNow.AddDays(-Random.Shared.Next(30, 365)),
                LastUpdated = DateTime.UtcNow.AddDays(-Random.Shared.Next(0, 7)),
                ActivityByMonth = new Dictionary<string, int>
                {
                    { "2024-01", Random.Shared.Next(0, 100) },
                    { "2024-02", Random.Shared.Next(0, 100) },
                    { "2024-03", Random.Shared.Next(0, 100) }
                },
                PopularTags = new Dictionary<string, int>
                {
                    { "nature", Random.Shared.Next(0, 50) },
                    { "landscape", Random.Shared.Next(0, 50) },
                    { "portrait", Random.Shared.Next(0, 50) }
                },
                StorageByType = new Dictionary<string, long>
                {
                    { "images", Random.Shared.NextInt64(0, 5L * 1024 * 1024 * 1024) },
                    { "videos", Random.Shared.NextInt64(0, 3L * 1024 * 1024 * 1024) },
                    { "documents", Random.Shared.NextInt64(0, 2L * 1024 * 1024 * 1024) }
                }
            };

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile statistics for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<UserProfileDto>> SearchProfilesAsync(string query, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching profiles with query: {Query}", query);

        try
        {
            var profiles = _profiles.Values
                .Where(p => p.IsPublic && (
                    p.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    p.Bio.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    p.Location.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    p.Tags.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase))
                ))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return profiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching profiles with query: {Query}", query);
            throw;
        }
    }

    public async Task<List<UserProfileDto>> GetPublicProfilesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting public profiles");

        try
        {
            var profiles = _profiles.Values
                .Where(p => p.IsPublic)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return profiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public profiles");
            throw;
        }
    }

    private static bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && 
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool IsValidLanguage(string language)
    {
        var validLanguages = new[] { "en", "es", "fr", "de", "it", "pt", "ru", "zh", "ja", "ko" };
        return validLanguages.Contains(language.ToLower());
    }

    private static bool IsValidTimezone(string timezone)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timezone);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
