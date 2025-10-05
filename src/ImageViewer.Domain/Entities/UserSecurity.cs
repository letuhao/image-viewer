using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// User security settings entity - represents user security settings and history
/// </summary>
public class UserSecuritySettings : BaseEntity
{
    [BsonElement("userId")]
    public ObjectId UserId { get; private set; }

    [BsonElement("twoFactorEnabled")]
    public bool TwoFactorEnabled { get; private set; }

    [BsonElement("twoFactorSecret")]
    public string? TwoFactorSecret { get; private set; }

    [BsonElement("backupCodes")]
    public List<string> BackupCodes { get; private set; } = new();

    [BsonElement("loginAttempts")]
    public List<LoginAttempt> LoginAttempts { get; private set; } = new();

    [BsonElement("securityQuestions")]
    public List<SecurityQuestion> SecurityQuestions { get; private set; } = new();

    [BsonElement("trustedDevices")]
    public List<TrustedDevice> TrustedDevices { get; private set; } = new();

    [BsonElement("ipWhitelist")]
    public List<string> IpWhitelist { get; private set; } = new();

    [BsonElement("lastPasswordChange")]
    public DateTime? LastPasswordChange { get; private set; }

    [BsonElement("passwordHistory")]
    public List<string> PasswordHistory { get; private set; } = new();


    [BsonElement("riskScore")]
    public double RiskScore { get; private set; } // 0.0 to 1.0

    [BsonElement("lastRiskAssessment")]
    public DateTime? LastRiskAssessment { get; private set; }

    [BsonElement("securitySettings")]
    public SecuritySettings SecuritySettings { get; private set; } = new();

    // Navigation properties
    [BsonIgnore]
    public User User { get; private set; } = null!;

    // Private constructor for EF Core
    private UserSecuritySettings() { }

    // Public constructor for creating new instances
    public static UserSecuritySettings Create(ObjectId userId)
    {
        return new UserSecuritySettings(userId);
    }

    public UserSecuritySettings(ObjectId userId)
    {
        UserId = userId;
        TwoFactorEnabled = false;
        RiskScore = 0.0;
        SecuritySettings = new SecuritySettings();
        LoginAttempts = new List<LoginAttempt>();
        SecurityQuestions = new List<SecurityQuestion>();
        TrustedDevices = new List<TrustedDevice>();
        IpWhitelist = new List<string>();
        PasswordHistory = new List<string>();
        BackupCodes = new List<string>();
    }

    public void EnableTwoFactor(string secretKey, List<string> backupCodes)
    {
        TwoFactorEnabled = true;
        TwoFactorSecret = secretKey;
        BackupCodes = backupCodes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DisableTwoFactor()
    {
        TwoFactorEnabled = false;
        TwoFactorSecret = null;
        BackupCodes.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddLoginAttempt(string ipAddress, string userAgent, bool successful, string? reason = null)
    {
        var attempt = new LoginAttempt
        {
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Successful = successful,
            AttemptedAt = DateTime.UtcNow,
            Reason = reason
        };

        LoginAttempts.Add(attempt);

        // Keep only last 50 attempts
        if (LoginAttempts.Count > 50)
        {
            LoginAttempts = LoginAttempts.TakeLast(50).ToList();
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void AddTrustedDevice(string deviceId, string deviceName, string ipAddress)
    {
        var device = new TrustedDevice
        {
            DeviceId = deviceId,
            DeviceName = deviceName,
            IpAddress = ipAddress,
            TrustedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow
        };

        TrustedDevices.Add(device);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveTrustedDevice(string deviceId)
    {
        TrustedDevices.RemoveAll(d => d.DeviceId == deviceId);
        UpdatedAt = DateTime.UtcNow;
    }


    public void UpdatePassword(string newPasswordHash)
    {
        LastPasswordChange = DateTime.UtcNow;
        PasswordHistory.Add(newPasswordHash);

        // Keep only last 5 passwords
        if (PasswordHistory.Count > 5)
        {
            PasswordHistory = PasswordHistory.TakeLast(5).ToList();
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRiskScore(double newScore)
    {
        RiskScore = Math.Max(0.0, Math.Min(1.0, newScore));
        LastRiskAssessment = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsDeviceTrusted(string deviceId)
    {
        return TrustedDevices.Any(d => d.DeviceId == deviceId);
    }

    public bool IsIpWhitelisted(string ipAddress)
    {
        return IpWhitelist.Contains(ipAddress);
    }

    public int GetFailedLoginAttempts(TimeSpan timeWindow)
    {
        var cutoff = DateTime.UtcNow - timeWindow;
        return LoginAttempts.Count(a => !a.Successful && a.AttemptedAt > cutoff);
    }

    public void AddIpToWhitelist(string ipAddress)
    {
        if (!IpWhitelist.Contains(ipAddress))
        {
            IpWhitelist.Add(ipAddress);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void AddAllowedLocation(string location)
    {
        // This method is a placeholder - you might want to implement location-based restrictions
        // For now, we'll add it to a generic allowed locations list
        // You could create a separate AllowedLocations property if needed
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Login attempt entity
/// </summary>
public class LoginAttempt
{
    [BsonElement("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;

    [BsonElement("userAgent")]
    public string UserAgent { get; set; } = string.Empty;

    [BsonElement("successful")]
    public bool Successful { get; set; }

    [BsonElement("attemptedAt")]
    public DateTime AttemptedAt { get; set; }

    [BsonElement("reason")]
    public string? Reason { get; set; }
}

/// <summary>
/// Security question entity
/// </summary>
public class SecurityQuestion
{
    [BsonElement("question")]
    public string Question { get; set; } = string.Empty;

    [BsonElement("answerHash")]
    public string AnswerHash { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Trusted device entity
/// </summary>
public class TrustedDevice
{
    [BsonElement("deviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [BsonElement("deviceName")]
    public string DeviceName { get; set; } = string.Empty;

    [BsonElement("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;

    [BsonElement("trustedAt")]
    public DateTime TrustedAt { get; set; }

    [BsonElement("lastUsedAt")]
    public DateTime LastUsedAt { get; set; }
}


/// <summary>
/// Security settings entity
/// </summary>
public class SecuritySettings
{
    [BsonElement("requireTwoFactor")]
    public bool RequireTwoFactor { get; set; } = false;

    [BsonElement("sessionTimeoutMinutes")]
    public int SessionTimeoutMinutes { get; set; } = 60;

    [BsonElement("maxLoginAttempts")]
    public int MaxLoginAttempts { get; set; } = 5;

    [BsonElement("lockoutDurationMinutes")]
    public int LockoutDurationMinutes { get; set; } = 15;

    [BsonElement("requireIpWhitelist")]
    public bool RequireIpWhitelist { get; set; } = false;

    [BsonElement("allowRememberMe")]
    public bool AllowRememberMe { get; set; } = true;

    [BsonElement("requirePasswordChange")]
    public bool RequirePasswordChange { get; set; } = false;

    [BsonElement("passwordChangeIntervalDays")]
    public int PasswordChangeIntervalDays { get; set; } = 90;
}
