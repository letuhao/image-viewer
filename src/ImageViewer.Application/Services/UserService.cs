using MongoDB.Bson;
using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service implementation for User operations
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<User> CreateUserAsync(string username, string email, string passwordHash)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(username))
                throw new ValidationException("Username cannot be null or empty");
            
            if (string.IsNullOrWhiteSpace(email))
                throw new ValidationException("Email cannot be null or empty");
            
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ValidationException("Password hash cannot be null or empty");

            // Check if user already exists
            var existingUser = await _userRepository.GetByUsernameAsync(username);
            if (existingUser != null)
                throw new DuplicateEntityException($"User with username '{username}' already exists");

            existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser != null)
                throw new DuplicateEntityException($"User with email '{email}' already exists");

            // Create new user
            var user = new User(username, email, passwordHash);
            return await _userRepository.CreateAsync(user);
        }
        catch (Exception ex) when (!(ex is ValidationException || ex is DuplicateEntityException))
        {
            _logger.LogError(ex, "Failed to create user with username {Username}", username);
            throw new BusinessRuleException($"Failed to create user with username '{username}'", ex);
        }
    }

    public async Task<User> GetUserByIdAsync(ObjectId userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new EntityNotFoundException($"User with ID '{userId}' not found");
            
            return user;
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to get user with ID {UserId}", userId);
            throw new BusinessRuleException($"Failed to get user with ID '{userId}'", ex);
        }
    }

    public async Task<User> GetUserByUsernameAsync(string username)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ValidationException("Username cannot be null or empty");

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
                throw new EntityNotFoundException($"User with username '{username}' not found");
            
            return user;
        }
        catch (Exception ex) when (!(ex is ValidationException || ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to get user with username {Username}", username);
            throw new BusinessRuleException($"Failed to get user with username '{username}'", ex);
        }
    }

    public async Task<User> GetUserByEmailAsync(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ValidationException("Email cannot be null or empty");

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                throw new EntityNotFoundException($"User with email '{email}' not found");
            
            return user;
        }
        catch (Exception ex) when (!(ex is ValidationException || ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to get user with email {Email}", email);
            throw new BusinessRuleException($"Failed to get user with email '{email}'", ex);
        }
    }

    public async Task<IEnumerable<User>> GetUsersAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1)
                throw new ValidationException("Page must be greater than 0");
            
            if (pageSize < 1 || pageSize > 100)
                throw new ValidationException("Page size must be between 1 and 100");

            var skip = (page - 1) * pageSize;
            return await _userRepository.FindAsync(
                Builders<User>.Filter.Empty,
                Builders<User>.Sort.Descending(u => u.CreatedAt),
                pageSize,
                skip
            );
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get users for page {Page} with page size {PageSize}", page, pageSize);
            throw new BusinessRuleException($"Failed to get users for page {page}", ex);
        }
    }

    public async Task<User> UpdateUserAsync(ObjectId userId, UpdateUserRequest request)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            
            if (request.Username != null)
            {
                // Check if username is already taken
                var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
                if (existingUser != null && existingUser.Id != userId)
                    throw new DuplicateEntityException($"Username '{request.Username}' is already taken");
                
                user.UpdateUsername(request.Username);
            }
            
            if (request.Email != null)
            {
                // Check if email is already taken
                var existingUser = await _userRepository.GetByEmailAsync(request.Email);
                if (existingUser != null && existingUser.Id != userId)
                    throw new DuplicateEntityException($"Email '{request.Email}' is already taken");
                
                user.UpdateEmail(request.Email);
            }
            
            return await _userRepository.UpdateAsync(user);
        }
        catch (Exception ex) when (!(ex is ValidationException || ex is EntityNotFoundException || ex is DuplicateEntityException))
        {
            _logger.LogError(ex, "Failed to update user with ID {UserId}", userId);
            throw new BusinessRuleException($"Failed to update user with ID '{userId}'", ex);
        }
    }

    public async Task DeleteUserAsync(ObjectId userId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            await _userRepository.DeleteAsync(userId);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to delete user with ID {UserId}", userId);
            throw new BusinessRuleException($"Failed to delete user with ID '{userId}'", ex);
        }
    }

    public async Task<User> UpdateProfileAsync(ObjectId userId, UpdateProfileRequest request)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            
            var newProfile = new UserProfile(
                request.FirstName ?? user.Profile.FirstName,
                request.LastName ?? user.Profile.LastName,
                request.DisplayName ?? user.Profile.DisplayName,
                request.Avatar ?? user.Profile.Avatar,
                request.Bio ?? user.Profile.Bio,
                request.Location ?? user.Profile.Location,
                request.Website ?? user.Profile.Website,
                request.BirthDate ?? user.Profile.BirthDate,
                request.Gender ?? user.Profile.Gender,
                request.Language ?? user.Profile.Language,
                request.Timezone ?? user.Profile.Timezone
            );
            
            user.UpdateProfile(newProfile);
            return await _userRepository.UpdateAsync(user);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to update profile for user with ID {UserId}", userId);
            throw new BusinessRuleException($"Failed to update profile for user with ID '{userId}'", ex);
        }
    }

    public async Task<User> UpdateSettingsAsync(ObjectId userId, UpdateSettingsRequest request)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            
            var newSettings = new UserSettings();
            
            if (request.DisplayMode != null)
                newSettings.UpdateDisplayMode(request.DisplayMode);
            
            if (request.ItemsPerPage.HasValue)
                newSettings.UpdateItemsPerPage(request.ItemsPerPage.Value);
            
            if (request.Theme != null)
                newSettings.UpdateTheme(request.Theme);
            
            if (request.Language != null)
                newSettings.UpdateLanguage(request.Language);
            
            if (request.Timezone != null)
                newSettings.UpdateTimezone(request.Timezone);
            
            if (request.Notifications != null)
                newSettings.UpdateNotifications(request.Notifications);
            
            if (request.Privacy != null)
                newSettings.UpdatePrivacy(request.Privacy);
            
            if (request.Performance != null)
                newSettings.UpdatePerformance(request.Performance);
            
            user.UpdateSettings(newSettings);
            return await _userRepository.UpdateAsync(user);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to update settings for user with ID {UserId}", userId);
            throw new BusinessRuleException($"Failed to update settings for user with ID '{userId}'", ex);
        }
    }

    public async Task<User> UpdateSecurityAsync(ObjectId userId, UpdateSecurityRequest request)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            
            var newSecurity = UserSecuritySettings.Create(userId);
            
            if (request.TwoFactorEnabled.HasValue && request.TwoFactorEnabled.Value)
            {
                if (string.IsNullOrWhiteSpace(request.TwoFactorSecret))
                    throw new ValidationException("Two-factor secret is required when enabling 2FA");
                
                if (request.BackupCodes == null || !request.BackupCodes.Any())
                    throw new ValidationException("Backup codes are required when enabling 2FA");
                
                newSecurity.EnableTwoFactor(request.TwoFactorSecret, request.BackupCodes);
            }
            
            if (request.IpWhitelist != null)
            {
                foreach (var ip in request.IpWhitelist)
                {
                    newSecurity.AddIpToWhitelist(ip);
                }
            }
            
            if (request.AllowedLocations != null)
            {
                foreach (var location in request.AllowedLocations)
                {
                    newSecurity.AddAllowedLocation(location);
                }
            }
            
            user.UpdateSecurity(newSecurity);
            return await _userRepository.UpdateAsync(user);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException || ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to update security for user with ID {UserId}", userId);
            throw new BusinessRuleException($"Failed to update security for user with ID '{userId}'", ex);
        }
    }

    public async Task<User> ActivateUserAsync(ObjectId userId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            user.Activate();
            return await _userRepository.UpdateAsync(user);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to activate user with ID {UserId}", userId);
            throw new BusinessRuleException($"Failed to activate user with ID '{userId}'", ex);
        }
    }

    public async Task<User> DeactivateUserAsync(ObjectId userId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            user.Deactivate();
            return await _userRepository.UpdateAsync(user);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to deactivate user with ID {UserId}", userId);
            throw new BusinessRuleException($"Failed to deactivate user with ID '{userId}'", ex);
        }
    }

    public async Task<User> VerifyEmailAsync(ObjectId userId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            user.VerifyEmail();
            return await _userRepository.UpdateAsync(user);
        }
        catch (Exception ex) when (!(ex is EntityNotFoundException))
        {
            _logger.LogError(ex, "Failed to verify email for user with ID {UserId}", userId);
            throw new BusinessRuleException($"Failed to verify email for user with ID '{userId}'", ex);
        }
    }

    public async Task<IEnumerable<User>> SearchUsersAsync(string query, int page = 1, int pageSize = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ValidationException("Search query cannot be null or empty");
            
            if (page < 1)
                throw new ValidationException("Page must be greater than 0");
            
            if (pageSize < 1 || pageSize > 100)
                throw new ValidationException("Page size must be between 1 and 100");

            var skip = (page - 1) * pageSize;
            return await _userRepository.FindAsync(
                Builders<User>.Filter.Or(
                    Builders<User>.Filter.Regex(u => u.Username, new BsonRegularExpression(query, "i")),
                    Builders<User>.Filter.Regex(u => u.Email, new BsonRegularExpression(query, "i")),
                    Builders<User>.Filter.Regex(u => u.Profile.FirstName, new BsonRegularExpression(query, "i")),
                    Builders<User>.Filter.Regex(u => u.Profile.LastName, new BsonRegularExpression(query, "i")),
                    Builders<User>.Filter.Regex(u => u.Profile.DisplayName, new BsonRegularExpression(query, "i"))
                ),
                Builders<User>.Sort.Descending(u => u.CreatedAt),
                pageSize,
                skip
            );
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to search users with query {Query}", query);
            throw new BusinessRuleException($"Failed to search users with query '{query}'", ex);
        }
    }

    public async Task<IEnumerable<User>> GetUsersByFilterAsync(UserFilterRequest filter, int page = 1, int pageSize = 20)
    {
        try
        {
            if (page < 1)
                throw new ValidationException("Page must be greater than 0");
            
            if (pageSize < 1 || pageSize > 100)
                throw new ValidationException("Page size must be between 1 and 100");

            var userFilter = new UserFilter
            {
                IsActive = filter.IsActive,
                IsEmailVerified = filter.IsEmailVerified,
                Role = filter.Role,
                CreatedAfter = filter.CreatedAfter,
                CreatedBefore = filter.CreatedBefore,
                Location = filter.Location,
                Language = filter.Language,
                Timezone = filter.Timezone
            };

            var skip = (page - 1) * pageSize;
            return await _userRepository.FindAsync(
                Builders<User>.Filter.Empty,
                Builders<User>.Sort.Descending(u => u.CreatedAt),
                pageSize,
                skip
            );
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get users by filter");
            throw new BusinessRuleException("Failed to get users by filter", ex);
        }
    }

    public async Task<Domain.ValueObjects.UserStatistics> GetUserStatisticsAsync()
    {
        try
        {
            return await _userRepository.GetUserStatisticsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user statistics");
            throw new BusinessRuleException("Failed to get user statistics", ex);
        }
    }

    public async Task<IEnumerable<User>> GetTopUsersByActivityAsync(int limit = 10)
    {
        try
        {
            if (limit < 1 || limit > 100)
                throw new ValidationException("Limit must be between 1 and 100");

            return await _userRepository.GetTopUsersByActivityAsync(limit);
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get top users by activity");
            throw new BusinessRuleException("Failed to get top users by activity", ex);
        }
    }

    public async Task<IEnumerable<User>> GetRecentUsersAsync(int limit = 10)
    {
        try
        {
            if (limit < 1 || limit > 100)
                throw new ValidationException("Limit must be between 1 and 100");

            return await _userRepository.GetRecentUsersAsync(limit);
        }
        catch (Exception ex) when (!(ex is ValidationException))
        {
            _logger.LogError(ex, "Failed to get recent users");
            throw new BusinessRuleException("Failed to get recent users", ex);
        }
    }
}
