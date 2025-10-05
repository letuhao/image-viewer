using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Application.DTOs.Auth;
using ImageViewer.Application.DTOs.Security;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service interface for security and authentication operations
/// </summary>
public interface ISecurityService
{
    #region Authentication
    
    /// <summary>
    /// Authenticate user with username and password
    /// </summary>
    /// <param name="request">Login request</param>
    /// <returns>Login result</returns>
    Task<LoginResult> LoginAsync(LoginRequest request);
    
    /// <summary>
    /// Register new user
    /// </summary>
    /// <param name="request">Registration request</param>
    /// <returns>Registration result</returns>
    Task<RegisterResult> RegisterAsync(RegisterRequest request);
    
    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="refreshToken">Refresh token</param>
    /// <returns>New login result</returns>
    Task<LoginResult> RefreshTokenAsync(string refreshToken);
    
    /// <summary>
    /// Logout user and invalidate tokens
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="refreshToken">Refresh token to invalidate</param>
    Task LogoutAsync(ObjectId userId, string? refreshToken = null);
    
    /// <summary>
    /// Validate JWT token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>True if valid, false otherwise</returns>
    bool ValidateToken(string token);
    
    /// <summary>
    /// Change user password
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="currentPassword">Current password</param>
    /// <param name="newPassword">New password</param>
    Task ChangePasswordAsync(ObjectId userId, string currentPassword, string newPassword);
    
    #endregion
    
    #region Two-Factor Authentication
    
    Task<TwoFactorSetupResult> SetupTwoFactorAsync(ObjectId userId);
    Task<bool> VerifyTwoFactorAsync(ObjectId userId, string code);
    Task<bool> DisableTwoFactorAsync(ObjectId userId, string code);
    Task<TwoFactorStatus> GetTwoFactorStatusAsync(ObjectId userId);
    
    #endregion
    
    #region Device Management
    
    Task<DeviceInfo> RegisterDeviceAsync(ObjectId userId, RegisterDeviceRequest request);
    Task<IEnumerable<DeviceInfo>> GetUserDevicesAsync(ObjectId userId);
    Task<DeviceInfo> UpdateDeviceAsync(ObjectId deviceId, UpdateDeviceRequest request);
    Task<bool> RevokeDeviceAsync(ObjectId deviceId);
    Task<bool> RevokeAllDevicesAsync(ObjectId userId);
    
    #endregion
    
    #region Session Management
    
    Task<SessionInfo> CreateSessionAsync(ObjectId userId, CreateSessionRequest request);
    Task<IEnumerable<SessionInfo>> GetUserSessionsAsync(ObjectId userId);
    Task<SessionInfo> UpdateSessionAsync(ObjectId sessionId, UpdateSessionRequest request);
    Task<bool> TerminateSessionAsync(ObjectId sessionId);
    Task<bool> TerminateAllSessionsAsync(ObjectId userId);
    
    #endregion
    
    #region IP Whitelisting
    
    Task<IPWhitelistEntry> AddIPToWhitelistAsync(ObjectId userId, string ipAddress);
    Task<IEnumerable<IPWhitelistEntry>> GetUserIPWhitelistAsync(ObjectId userId);
    Task<bool> RemoveIPFromWhitelistAsync(ObjectId userId, string ipAddress);
    Task<bool> IsIPWhitelistedAsync(ObjectId userId, string ipAddress);
    
    #endregion
    
    #region Geolocation Security
    
    Task<GeolocationInfo> GetGeolocationInfoAsync(string ipAddress);
    Task<GeolocationSecurityResult> CheckGeolocationSecurityAsync(ObjectId userId, string ipAddress);
    Task<GeolocationAlert> CreateGeolocationAlertAsync(ObjectId userId, string ipAddress, string location);
    
    #endregion
    
    #region Security Alerts
    
    Task<DTOs.Security.SecurityAlert> CreateSecurityAlertAsync(ObjectId userId, SecurityAlertType type, string description);
    Task<IEnumerable<DTOs.Security.SecurityAlert>> GetUserSecurityAlertsAsync(ObjectId userId, int page = 1, int pageSize = 20);
    Task<DTOs.Security.SecurityAlert> MarkAlertAsReadAsync(ObjectId alertId);
    Task<bool> DeleteSecurityAlertAsync(ObjectId alertId);
    
    #endregion
    
    #region Risk Assessment
    
    Task<RiskAssessment> AssessUserRiskAsync(ObjectId userId);
    Task<RiskAssessment> AssessLoginRiskAsync(ObjectId userId, string ipAddress, string userAgent);
    Task<RiskAssessment> AssessActionRiskAsync(ObjectId userId, string action, string? ipAddress = null);
    
    #endregion
    
    #region Security Monitoring
    
    Task<SecurityMetrics> GetSecurityMetricsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<SecurityReport> GenerateSecurityReportAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<SecurityEvent>> GetSecurityEventsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    
    #endregion
}

/// <summary>
/// Request model for login
/// </summary>
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? TwoFactorCode { get; set; }
    public string? DeviceId { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public bool RememberMe { get; set; } = false;
}

/// <summary>
/// Request model for refresh token
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
}

/// <summary>
/// Request model for device registration
/// </summary>
public class RegisterDeviceRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsTrusted { get; set; } = false;
}

/// <summary>
/// Request model for device update
/// </summary>
public class UpdateDeviceRequest
{
    public string? DeviceName { get; set; }
    public bool? IsTrusted { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Request model for session creation
/// </summary>
public class CreateSessionRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsPersistent { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Request model for session update
/// </summary>
public class UpdateSessionRequest
{
    public bool? IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Location { get; set; }
}

/// <summary>
/// Authentication result
/// </summary>
public class AuthenticationResult
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public ObjectId? UserId { get; set; }
    public string? ErrorMessage { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public SecurityRiskLevel RiskLevel { get; set; }
}

/// <summary>
/// Two-factor authentication setup result
/// </summary>
public class TwoFactorSetupResult
{
    public bool Success { get; set; }
    public string? SecretKey { get; set; }
    public string? QrCodeUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Two-factor authentication status
/// </summary>
public class TwoFactorStatus
{
    public bool IsEnabled { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? LastUsed { get; set; }
    public List<string> BackupCodes { get; set; } = new();
}

/// <summary>
/// Device information
/// </summary>
public class DeviceInfo
{
    public ObjectId Id { get; set; }
    public ObjectId UserId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsTrusted { get; set; }
    public bool IsActive { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Session information
/// </summary>
public class SessionInfo
{
    public ObjectId Id { get; set; }
    public ObjectId UserId { get; set; }
    public ObjectId DeviceId { get; set; }
    public string SessionToken { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsActive { get; set; }
    public bool IsPersistent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivity { get; set; }
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// IP whitelist entry
/// </summary>
public class IPWhitelistEntry
{
    public ObjectId Id { get; set; }
    public ObjectId UserId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}


/// <summary>
/// Security alert
/// </summary>
public class SecurityAlert
{
    public ObjectId Id { get; set; }
    public ObjectId UserId { get; set; }
    public SecurityAlertType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SecurityRiskLevel RiskLevel { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Location { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

/// <summary>
/// Risk assessment
/// </summary>
public class RiskAssessment
{
    public ObjectId Id { get; set; }
    public ObjectId UserId { get; set; }
    public SecurityRiskLevel RiskLevel { get; set; }
    public double RiskScore { get; set; }
    public string AssessmentType { get; set; } = string.Empty;
    public string? Context { get; set; }
    public List<RiskFactor> RiskFactors { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime AssessedAt { get; set; }
}

/// <summary>
/// Risk factor
/// </summary>
public class RiskFactor
{
    public string Factor { get; set; } = string.Empty;
    public double Weight { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
}


/// <summary>
/// Enums
/// </summary>
public enum SecurityAlertType
{
    LoginAttempt,
    TwoFactorAttempt,
    SuspiciousActivity,
    UnauthorizedAccess,
    DataBreach,
    Malware,
    Phishing,
    BruteForce,
    AccountTakeover,
    PrivilegeEscalation
}

public enum SecurityRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}
