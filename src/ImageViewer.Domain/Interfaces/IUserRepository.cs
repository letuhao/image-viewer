using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for User operations
/// </summary>
public interface IUserRepository : IRepository<User>
{
    #region Query Methods
    
    Task<User> GetByUsernameAsync(string username);
    Task<User> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetActiveUsersAsync();
    Task<IEnumerable<User>> GetUsersByRoleAsync(string role);
    Task<long> GetUserCountAsync();
    Task<long> GetActiveUserCountAsync();
    
    #endregion
    
    #region Search Methods
    
    Task<IEnumerable<User>> SearchUsersAsync(string query);
    Task<IEnumerable<User>> GetUsersByFilterAsync(UserFilter filter);
    
    #endregion
    
    #region Statistics Methods
    
    Task<UserStatistics> GetUserStatisticsAsync();
    Task<IEnumerable<User>> GetTopUsersByActivityAsync(int limit = 10);
    Task<IEnumerable<User>> GetRecentUsersAsync(int limit = 10);
    
    #endregion
    
    #region Security Methods
    
    Task LogFailedLoginAttemptAsync(ObjectId userId);
    Task LogSuccessfulLoginAsync(ObjectId userId, string ipAddress, string userAgent);
    Task ClearFailedLoginAttemptsAsync(ObjectId userId);
    Task StoreRefreshTokenAsync(ObjectId userId, string refreshToken, DateTime expiryDate);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task InvalidateRefreshTokenAsync(ObjectId userId, string refreshToken);
    
    #endregion
}

/// <summary>
/// User filter for advanced queries
/// </summary>
public class UserFilter
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

/// <summary>
/// User statistics for reporting
/// </summary>
public class UserStatistics
{
    public long TotalUsers { get; set; }
    public long ActiveUsers { get; set; }
    public long VerifiedUsers { get; set; }
    public long NewUsersThisMonth { get; set; }
    public long NewUsersThisWeek { get; set; }
    public long NewUsersToday { get; set; }
    public Dictionary<string, long> UsersByRole { get; set; } = new();
    public Dictionary<string, long> UsersByLocation { get; set; } = new();
    public Dictionary<string, long> UsersByLanguage { get; set; } = new();
}
