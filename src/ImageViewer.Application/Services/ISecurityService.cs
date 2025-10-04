using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service interface for security and authentication operations
/// </summary>
public interface ISecurityService
{
    #region Authentication
    
    Task<AuthenticationResult> AuthenticateAsync(LoginRequest request);
    Task<AuthenticationResult> RefreshTokenAsync(RefreshTokenRequest request);
    Task LogoutAsync(ObjectId userId, string? token = null);
    Task<bool> ValidateTokenAsync(string token);
    
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
    
    Task<SecurityAlert> CreateSecurityAlertAsync(ObjectId userId, SecurityAlertType type, string description);
    Task<IEnumerable<SecurityAlert>> GetUserSecurityAlertsAsync(ObjectId userId, int page = 1, int pageSize = 20);
    Task<SecurityAlert> MarkAlertAsReadAsync(ObjectId alertId);
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
/// Geolocation information
/// </summary>
public class GeolocationInfo
{
    public string IpAddress { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Timezone { get; set; } = string.Empty;
    public string Isp { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
}

/// <summary>
/// Geolocation security result
/// </summary>
public class GeolocationSecurityResult
{
    public bool IsAllowed { get; set; }
    public SecurityRiskLevel RiskLevel { get; set; }
    public string? Reason { get; set; }
    public GeolocationInfo? Location { get; set; }
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Geolocation alert
/// </summary>
public class GeolocationAlert
{
    public ObjectId Id { get; set; }
    public ObjectId UserId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
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
/// Security metrics
/// </summary>
public class SecurityMetrics
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public long TotalLogins { get; set; }
    public long FailedLogins { get; set; }
    public long TwoFactorAttempts { get; set; }
    public long SecurityAlerts { get; set; }
    public long BlockedIPs { get; set; }
    public long SuspiciousActivities { get; set; }
    public double LoginSuccessRate { get; set; }
    public double TwoFactorSuccessRate { get; set; }
    public Dictionary<SecurityAlertType, long> AlertsByType { get; set; } = new();
    public Dictionary<SecurityRiskLevel, long> RisksByLevel { get; set; } = new();
}

/// <summary>
/// Security report
/// </summary>
public class SecurityReport
{
    public ObjectId Id { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public SecuritySummary Summary { get; set; } = new();
    public List<SecurityEvent> Events { get; set; } = new();
    public List<SecurityRecommendation> Recommendations { get; set; } = new();
}

/// <summary>
/// Security summary
/// </summary>
public class SecuritySummary
{
    public long TotalEvents { get; set; }
    public long HighRiskEvents { get; set; }
    public long MediumRiskEvents { get; set; }
    public long LowRiskEvents { get; set; }
    public double OverallRiskScore { get; set; }
    public string OverallStatus { get; set; } = string.Empty;
    public List<string> TopThreats { get; set; } = new();
    public List<string> TopRecommendations { get; set; } = new();
}

/// <summary>
/// Security event
/// </summary>
public class SecurityEvent
{
    public ObjectId Id { get; set; }
    public ObjectId UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SecurityRiskLevel RiskLevel { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Location { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime OccurredAt { get; set; }
}

/// <summary>
/// Security recommendation
/// </summary>
public class SecurityRecommendation
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public List<string> Actions { get; set; } = new();
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
