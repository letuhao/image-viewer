using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ImageViewer.Domain.Events;
using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// User aggregate root - represents a system user
/// </summary>
public class User : BaseEntity
{
    [BsonElement("username")]
    public string Username { get; private set; }
    
    [BsonElement("email")]
    public string Email { get; private set; }
    
    [BsonElement("passwordHash")]
    public string PasswordHash { get; private set; }
    
    [BsonElement("isActive")]
    public bool IsActive { get; private set; }
    
    [BsonElement("isEmailVerified")]
    public bool IsEmailVerified { get; private set; }
    
    [BsonElement("profile")]
    public UserProfile Profile { get; private set; }
    
    [BsonElement("settings")]
    public UserSettings Settings { get; private set; }
    
    [BsonElement("security")]
    public UserSecuritySettings Security { get; private set; }
    
    [BsonElement("statistics")]
    public UserStatistics Statistics { get; private set; }
    
    [BsonElement("role")]
    public string? Role { get; private set; }
    
    [BsonElement("twoFactorEnabled")]
    public bool TwoFactorEnabled { get; private set; }
    
    [BsonElement("twoFactorSecret")]
    public string? TwoFactorSecret { get; private set; }
    
    [BsonElement("backupCodes")]
    public List<string> BackupCodes { get; private set; } = new();
    
    [BsonElement("failedLoginAttempts")]
    public int FailedLoginAttempts { get; private set; }
    
    [BsonElement("isLocked")]
    public bool IsLocked { get; private set; }
    
    [BsonElement("lockedUntil")]
    public DateTime? LockedUntil { get; private set; }
    
    [BsonElement("lastLoginAt")]
    public DateTime? LastLoginAt { get; private set; }
    
    [BsonElement("lastLoginIp")]
    public string? LastLoginIp { get; private set; }

    // Private constructor for MongoDB
    private User() { }

    public User(string username, string email, string passwordHash, string? role = null)
    {
        Username = username ?? throw new ArgumentNullException(nameof(username));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        Role = role ?? "User";
        
        IsActive = true;
        IsEmailVerified = false;
        TwoFactorEnabled = false;
        FailedLoginAttempts = 0;
        IsLocked = false;
        
        Profile = new UserProfile();
        Settings = new UserSettings();
        Security = UserSecuritySettings.Create(Id);
        Statistics = new UserStatistics();
        
        AddDomainEvent(new UserCreatedEvent(Id, Username, Email));
    }

    public void UpdateUsername(string newUsername)
    {
        if (string.IsNullOrWhiteSpace(newUsername))
            throw new ArgumentException("Username cannot be null or empty", nameof(newUsername));
        
        Username = newUsername;
        UpdateTimestamp();
        
        AddDomainEvent(new UserUsernameChangedEvent(Id, newUsername));
    }

    public void UpdateEmail(string newEmail)
    {
        if (string.IsNullOrWhiteSpace(newEmail))
            throw new ArgumentException("Email cannot be null or empty", nameof(newEmail));
        
        Email = newEmail;
        IsEmailVerified = false;
        UpdateTimestamp();
        
        AddDomainEvent(new UserEmailChangedEvent(Id, newEmail));
    }

    public void VerifyEmail()
    {
        if (!IsEmailVerified)
        {
            IsEmailVerified = true;
            UpdateTimestamp();
            
            AddDomainEvent(new UserEmailVerifiedEvent(Id));
        }
    }

    public void Activate()
    {
        if (!IsActive)
        {
            IsActive = true;
            UpdateTimestamp();
            
            AddDomainEvent(new UserActivatedEvent(Id));
        }
    }

    public void Deactivate()
    {
        if (IsActive)
        {
            IsActive = false;
            UpdateTimestamp();
            
            AddDomainEvent(new UserDeactivatedEvent(Id));
        }
    }

    public void UpdateProfile(UserProfile newProfile)
    {
        Profile = newProfile ?? throw new ArgumentNullException(nameof(newProfile));
        UpdateTimestamp();
        
        AddDomainEvent(new UserProfileUpdatedEvent(Id));
    }

    public void UpdateSettings(UserSettings newSettings)
    {
        Settings = newSettings ?? throw new ArgumentNullException(nameof(newSettings));
        UpdateTimestamp();
        
        AddDomainEvent(new UserSettingsUpdatedEvent(Id));
    }

    public void UpdateSecurity(UserSecuritySettings newSecurity)
    {
        Security = newSecurity ?? throw new ArgumentNullException(nameof(newSecurity));
        UpdateTimestamp();
        
        AddDomainEvent(new UserSecurityUpdatedEvent(Id));
    }

    public void UpdateStatistics(UserStatistics newStatistics)
    {
        Statistics = newStatistics ?? throw new ArgumentNullException(nameof(newStatistics));
        UpdateTimestamp();
    }

    /// <summary>
    /// Update user password hash
    /// </summary>
    /// <param name="newPasswordHash">New password hash</param>
    public void UpdatePasswordHash(string newPasswordHash)
    {
        PasswordHash = newPasswordHash ?? throw new ArgumentNullException(nameof(newPasswordHash));
        UpdateTimestamp();
        
        AddDomainEvent(new UserPasswordChangedEvent(Id));
    }

    /// <summary>
    /// Enable two-factor authentication
    /// </summary>
    /// <param name="secret">TOTP secret key</param>
    /// <param name="backupCodes">Backup codes</param>
    public void EnableTwoFactor(string secret, List<string> backupCodes)
    {
        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("Secret cannot be null or empty", nameof(secret));
        
        if (backupCodes == null || !backupCodes.Any())
            throw new ArgumentException("Backup codes cannot be null or empty", nameof(backupCodes));

        TwoFactorEnabled = true;
        TwoFactorSecret = secret;
        BackupCodes = backupCodes;
        UpdateTimestamp();
        
        AddDomainEvent(new UserTwoFactorEnabledEvent(Id));
    }

    /// <summary>
    /// Disable two-factor authentication
    /// </summary>
    public void DisableTwoFactor()
    {
        TwoFactorEnabled = false;
        TwoFactorSecret = null;
        BackupCodes.Clear();
        UpdateTimestamp();
        
        AddDomainEvent(new UserTwoFactorDisabledEvent(Id));
    }

    /// <summary>
    /// Increment failed login attempts and lock account if threshold reached
    /// </summary>
    public void IncrementFailedLoginAttempts()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 5)
        {
            IsLocked = true;
            LockedUntil = DateTime.UtcNow.AddMinutes(30);
        }
        UpdateTimestamp();
        
        AddDomainEvent(new UserLoginFailedEvent(Id, FailedLoginAttempts));
    }

    /// <summary>
    /// Clear failed login attempts and unlock account
    /// </summary>
    public void ClearFailedLoginAttempts()
    {
        FailedLoginAttempts = 0;
        IsLocked = false;
        LockedUntil = null;
        UpdateTimestamp();
    }

    /// <summary>
    /// Record successful login
    /// </summary>
    /// <param name="ipAddress">IP address of the login</param>
    public void RecordSuccessfulLogin(string ipAddress)
    {
        LastLoginAt = DateTime.UtcNow;
        LastLoginIp = ipAddress;
        ClearFailedLoginAttempts();
        UpdateTimestamp();
        
        AddDomainEvent(new UserLoginSuccessfulEvent(Id, ipAddress));
    }

    /// <summary>
    /// Update user role
    /// </summary>
    /// <param name="newRole">New role</param>
    public void UpdateRole(string newRole)
    {
        Role = newRole ?? throw new ArgumentNullException(nameof(newRole));
        UpdateTimestamp();
        
        AddDomainEvent(new UserRoleUpdatedEvent(Id, newRole));
    }

    /// <summary>
    /// Check if account is locked due to failed login attempts
    /// </summary>
    /// <returns>True if account is locked</returns>
    public bool IsAccountLocked()
    {
        return IsLocked && LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
    }

    /// <summary>
    /// Check if backup code is valid and remove it after use
    /// </summary>
    /// <param name="code">Backup code to check</param>
    /// <returns>True if code is valid</returns>
    public bool ValidateAndRemoveBackupCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || !BackupCodes.Contains(code))
            return false;

        BackupCodes.Remove(code);
        UpdateTimestamp();
        return true;
    }
}
