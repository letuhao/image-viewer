using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service interface for User operations
/// </summary>
public interface IUserService
{
    #region User Management
    
    Task<User> CreateUserAsync(string username, string email, string passwordHash);
    Task<User> GetUserByIdAsync(ObjectId userId);
    Task<User> GetUserByUsernameAsync(string username);
    Task<User> GetUserByEmailAsync(string email);
    Task<IEnumerable<User>> GetUsersAsync(int page = 1, int pageSize = 20);
    Task<User> UpdateUserAsync(ObjectId userId, UpdateUserRequest request);
    Task DeleteUserAsync(ObjectId userId);
    
    #endregion
    
    #region User Profile Management
    
    Task<User> UpdateProfileAsync(ObjectId userId, UpdateProfileRequest request);
    Task<User> UpdateSettingsAsync(ObjectId userId, UpdateSettingsRequest request);
    Task<User> UpdateSecurityAsync(ObjectId userId, UpdateSecurityRequest request);
    
    #endregion
    
    #region User Status Management
    
    Task<User> ActivateUserAsync(ObjectId userId);
    Task<User> DeactivateUserAsync(ObjectId userId);
    Task<User> VerifyEmailAsync(ObjectId userId);
    
    #endregion
    
    #region User Search and Filtering
    
    Task<IEnumerable<User>> SearchUsersAsync(string query, int page = 1, int pageSize = 20);
    Task<IEnumerable<User>> GetUsersByFilterAsync(UserFilterRequest filter, int page = 1, int pageSize = 20);
    
    #endregion
    
    #region User Statistics
    
    Task<UserStatistics> GetUserStatisticsAsync();
    Task<IEnumerable<User>> GetTopUsersByActivityAsync(int limit = 10);
    Task<IEnumerable<User>> GetRecentUsersAsync(int limit = 10);
    
    #endregion
}

/// <summary>
/// Request model for updating user information
/// </summary>
public class UpdateUserRequest
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
}

/// <summary>
/// Request model for updating user profile
/// </summary>
public class UpdateProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? Avatar { get; set; }
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? Language { get; set; }
    public string? Timezone { get; set; }
}

/// <summary>
/// Request model for updating user settings
/// </summary>
public class UpdateSettingsRequest
{
    public string? DisplayMode { get; set; }
    public int? ItemsPerPage { get; set; }
    public string? Theme { get; set; }
    public string? Language { get; set; }
    public string? Timezone { get; set; }
    public NotificationSettings? Notifications { get; set; }
    public PrivacySettings? Privacy { get; set; }
    public PerformanceSettings? Performance { get; set; }
}

/// <summary>
/// Request model for updating user security
/// </summary>
public class UpdateSecurityRequest
{
    public bool? TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
    public List<string>? BackupCodes { get; set; }
    public List<string>? IpWhitelist { get; set; }
    public List<string>? AllowedLocations { get; set; }
}

/// <summary>
/// Request model for filtering users
/// </summary>
public class UserFilterRequest
{
    public bool? IsActive { get; set; }
    public bool? IsEmailVerified { get; set; }
    public string? Role { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public DateTime? LastLoginAfter { get; set; }
    public DateTime? LastLoginBefore { get; set; }
    public string? Location { get; set; }
    public string? Language { get; set; }
    public string? Timezone { get; set; }
}
