using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service implementation for user preferences operations
/// </summary>
public class UserPreferencesService : IUserPreferencesService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserPreferencesService> _logger;

    public UserPreferencesService(IUserRepository userRepository, ILogger<UserPreferencesService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UserPreferences> GetUserPreferencesAsync(ObjectId userId)
    {
        try
        {
            // Check if user exists
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            // TODO: Implement when preferences repository is available
            // For now, return default preferences
            return new UserPreferences
            {
                Id = ObjectId.GenerateNewId(),
                UserId = userId,
                Display = new DisplayPreferences(),
                Privacy = new PrivacyPreferences(),
                Performance = new PerformancePreferences(),
                Notifications = new NotificationPreferences(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to get user preferences for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to get user preferences for user '{userId}'", ex);
        }
    }

    public async Task<UserPreferences> UpdateUserPreferencesAsync(ObjectId userId, UpdateUserPreferencesRequest request)
    {
        try
        {
            // Validate input
            if (request == null)
                throw new ValidationException("Update request cannot be null");

            // Check if user exists
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            // Validate preferences
            if (!await ValidatePreferencesAsync(request))
                throw new ValidationException("Invalid preferences data");

            // Get current preferences
            var currentPreferences = await GetUserPreferencesAsync(userId);

            // Update preferences
            if (request.Display != null)
            {
                currentPreferences.Display = new DisplayPreferences
                {
                    DisplayMode = request.Display.DisplayMode,
                    ItemsPerPage = request.Display.ItemsPerPage,
                    ThumbnailSize = request.Display.ThumbnailSize,
                    ShowMetadata = request.Display.ShowMetadata,
                    ShowFileSize = request.Display.ShowFileSize,
                    ShowCreationDate = request.Display.ShowCreationDate,
                    Theme = request.Display.Theme,
                    Language = request.Display.Language,
                    TimeZone = request.Display.TimeZone,
                    EnableAnimations = request.Display.EnableAnimations,
                    EnableTooltips = request.Display.EnableTooltips
                };
            }

            if (request.Privacy != null)
            {
                currentPreferences.Privacy = new PrivacyPreferences
                {
                    ProfilePublic = request.Privacy.ProfilePublic,
                    ShowOnlineStatus = request.Privacy.ShowOnlineStatus,
                    AllowDirectMessages = request.Privacy.AllowDirectMessages,
                    ShowActivity = request.Privacy.ShowActivity,
                    AllowSearchIndexing = request.Privacy.AllowSearchIndexing,
                    ShareUsageData = request.Privacy.ShareUsageData,
                    AllowAnalytics = request.Privacy.AllowAnalytics,
                    AllowCookies = request.Privacy.AllowCookies
                };
            }

            if (request.Performance != null)
            {
                currentPreferences.Performance = new PerformancePreferences
                {
                    CacheSize = request.Performance.CacheSize,
                    EnableLazyLoading = request.Performance.EnableLazyLoading,
                    EnableImageOptimization = request.Performance.EnableImageOptimization,
                    MaxConcurrentDownloads = request.Performance.MaxConcurrentDownloads,
                    EnableBackgroundSync = request.Performance.EnableBackgroundSync,
                    AutoSaveInterval = request.Performance.AutoSaveInterval,
                    EnableCompression = request.Performance.EnableCompression,
                    EnableCaching = request.Performance.EnableCaching
                };
            }

            if (request.Notifications != null)
            {
                currentPreferences.Notifications = new NotificationPreferences
                {
                    Id = ObjectId.GenerateNewId(),
                    UserId = userId,
                    EmailEnabled = request.Notifications.EmailEnabled,
                    PushEnabled = request.Notifications.PushEnabled,
                    InAppEnabled = request.Notifications.InAppEnabled,
                    SmsEnabled = request.Notifications.SmsEnabled,
                    TypePreferences = request.Notifications.TypePreferences,
                    QuietHoursStart = request.Notifications.QuietHoursStart,
                    QuietHoursEnd = request.Notifications.QuietHoursEnd,
                    QuietHoursEnabled = request.Notifications.QuietHoursEnabled,
                    QuietDays = request.Notifications.QuietDays,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            currentPreferences.UpdatedAt = DateTime.UtcNow;

            // TODO: Save to database when preferences repository is implemented
            _logger.LogInformation("Updated user preferences for user {UserId}", userId);

            return currentPreferences;
        }
        catch (Exception ex) when (!(ex is ValidationException || ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to update user preferences for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to update user preferences for user '{userId}'", ex);
        }
    }

    public async Task<UserPreferences> ResetUserPreferencesAsync(ObjectId userId)
    {
        try
        {
            // Check if user exists
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");

            // Reset to default preferences
            var defaultPreferences = new UserPreferences
            {
                Id = ObjectId.GenerateNewId(),
                UserId = userId,
                Display = new DisplayPreferences(),
                Privacy = new PrivacyPreferences(),
                Performance = new PerformancePreferences(),
                Notifications = new NotificationPreferences(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // TODO: Save to database when preferences repository is implemented
            _logger.LogInformation("Reset user preferences for user {UserId}", userId);

            return defaultPreferences;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to reset user preferences for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to reset user preferences for user '{userId}'", ex);
        }
    }

    public async Task<bool> ValidatePreferencesAsync(UpdateUserPreferencesRequest request)
    {
        try
        {
            if (request == null)
                return false;

            // Validate display preferences
            if (request.Display != null)
            {
                if (request.Display.ItemsPerPage < 1 || request.Display.ItemsPerPage > 100)
                    return false;
                
                if (request.Display.ThumbnailSize < 50 || request.Display.ThumbnailSize > 500)
                    return false;
                
                if (string.IsNullOrWhiteSpace(request.Display.Theme))
                    return false;
                
                if (string.IsNullOrWhiteSpace(request.Display.Language))
                    return false;
            }

            // Validate performance preferences
            if (request.Performance != null)
            {
                if (request.Performance.CacheSize < 10 || request.Performance.CacheSize > 1000)
                    return false;
                
                if (request.Performance.MaxConcurrentDownloads < 1 || request.Performance.MaxConcurrentDownloads > 10)
                    return false;
                
                if (request.Performance.AutoSaveInterval < 5 || request.Performance.AutoSaveInterval > 300)
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate preferences");
            return false;
        }
    }

    public async Task<DisplayPreferences> GetDisplayPreferencesAsync(ObjectId userId)
    {
        try
        {
            var preferences = await GetUserPreferencesAsync(userId);
            return preferences.Display;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get display preferences for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to get display preferences for user '{userId}'", ex);
        }
    }

    public async Task<DisplayPreferences> UpdateDisplayPreferencesAsync(ObjectId userId, UpdateDisplayPreferencesRequest request)
    {
        try
        {
            if (request == null)
                throw new ValidationException("Update request cannot be null");

            var updateRequest = new UpdateUserPreferencesRequest
            {
                Display = new DisplayPreferences
                {
                    DisplayMode = request.DisplayMode,
                    ItemsPerPage = request.ItemsPerPage,
                    ThumbnailSize = request.ThumbnailSize,
                    ShowMetadata = request.ShowMetadata,
                    ShowFileSize = request.ShowFileSize,
                    ShowCreationDate = request.ShowCreationDate,
                    Theme = request.Theme,
                    Language = request.Language,
                    TimeZone = request.TimeZone,
                    EnableAnimations = request.EnableAnimations,
                    EnableTooltips = request.EnableTooltips
                }
            };

            var preferences = await UpdateUserPreferencesAsync(userId, updateRequest);
            return preferences.Display;
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to update display preferences for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to update display preferences for user '{userId}'", ex);
        }
    }

    public async Task<PrivacyPreferences> GetPrivacyPreferencesAsync(ObjectId userId)
    {
        try
        {
            var preferences = await GetUserPreferencesAsync(userId);
            return preferences.Privacy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get privacy preferences for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to get privacy preferences for user '{userId}'", ex);
        }
    }

    public async Task<PrivacyPreferences> UpdatePrivacyPreferencesAsync(ObjectId userId, UpdatePrivacyPreferencesRequest request)
    {
        try
        {
            if (request == null)
                throw new ValidationException("Update request cannot be null");

            var updateRequest = new UpdateUserPreferencesRequest
            {
                Privacy = new PrivacyPreferences
                {
                    ProfilePublic = request.ProfilePublic,
                    ShowOnlineStatus = request.ShowOnlineStatus,
                    AllowDirectMessages = request.AllowDirectMessages,
                    ShowActivity = request.ShowActivity,
                    AllowSearchIndexing = request.AllowSearchIndexing,
                    ShareUsageData = request.ShareUsageData,
                    AllowAnalytics = request.AllowAnalytics,
                    AllowCookies = request.AllowCookies
                }
            };

            var preferences = await UpdateUserPreferencesAsync(userId, updateRequest);
            return preferences.Privacy;
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to update privacy preferences for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to update privacy preferences for user '{userId}'", ex);
        }
    }

    public async Task<PerformancePreferences> GetPerformancePreferencesAsync(ObjectId userId)
    {
        try
        {
            var preferences = await GetUserPreferencesAsync(userId);
            return preferences.Performance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance preferences for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to get performance preferences for user '{userId}'", ex);
        }
    }

    public async Task<PerformancePreferences> UpdatePerformancePreferencesAsync(ObjectId userId, UpdatePerformancePreferencesRequest request)
    {
        try
        {
            if (request == null)
                throw new ValidationException("Update request cannot be null");

            var updateRequest = new UpdateUserPreferencesRequest
            {
                Performance = new PerformancePreferences
                {
                    CacheSize = request.CacheSize,
                    EnableLazyLoading = request.EnableLazyLoading,
                    EnableImageOptimization = request.EnableImageOptimization,
                    MaxConcurrentDownloads = request.MaxConcurrentDownloads,
                    EnableBackgroundSync = request.EnableBackgroundSync,
                    AutoSaveInterval = request.AutoSaveInterval,
                    EnableCompression = request.EnableCompression,
                    EnableCaching = request.EnableCaching
                }
            };

            var preferences = await UpdateUserPreferencesAsync(userId, updateRequest);
            return preferences.Performance;
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to update performance preferences for user {UserId}", userId);
            throw new BusinessRuleException($"Failed to update performance preferences for user '{userId}'", ex);
        }
    }
}
