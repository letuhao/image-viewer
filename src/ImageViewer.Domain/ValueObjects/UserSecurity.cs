using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// User security value object
/// </summary>
public class UserSecurity
{
    [BsonElement("twoFactorEnabled")]
    public bool TwoFactorEnabled { get; private set; }
    
    [BsonElement("twoFactorSecret")]
    public string TwoFactorSecret { get; private set; }
    
    [BsonElement("backupCodes")]
    public List<string> BackupCodes { get; private set; }
    
    [BsonElement("lastLogin")]
    public DateTime? LastLogin { get; private set; }
    
    [BsonElement("lastPasswordChange")]
    public DateTime? LastPasswordChange { get; private set; }
    
    [BsonElement("failedLoginAttempts")]
    public int FailedLoginAttempts { get; private set; }
    
    [BsonElement("lockedUntil")]
    public DateTime? LockedUntil { get; private set; }
    
    [BsonElement("ipWhitelist")]
    public List<string> IpWhitelist { get; private set; }
    
    [BsonElement("allowedLocations")]
    public List<string> AllowedLocations { get; private set; }

    public UserSecurity()
    {
        TwoFactorEnabled = false;
        TwoFactorSecret = string.Empty;
        BackupCodes = new List<string>();
        FailedLoginAttempts = 0;
        IpWhitelist = new List<string>();
        AllowedLocations = new List<string>();
    }

    public void EnableTwoFactor(string secret, List<string> backupCodes)
    {
        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("Two-factor secret cannot be null or empty", nameof(secret));
        
        if (backupCodes == null || !backupCodes.Any())
            throw new ArgumentException("Backup codes cannot be null or empty", nameof(backupCodes));
        
        TwoFactorEnabled = true;
        TwoFactorSecret = secret;
        BackupCodes = backupCodes;
    }

    public void DisableTwoFactor()
    {
        TwoFactorEnabled = false;
        TwoFactorSecret = string.Empty;
        BackupCodes.Clear();
    }

    public void UpdateLastLogin(DateTime loginTime)
    {
        LastLogin = loginTime;
    }

    public void UpdateLastPasswordChange(DateTime changeTime)
    {
        LastPasswordChange = changeTime;
    }

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        
        // Lock account after 5 failed attempts for 30 minutes
        if (FailedLoginAttempts >= 5)
        {
            LockedUntil = DateTime.UtcNow.AddMinutes(30);
        }
    }

    public void ResetFailedLogins()
    {
        FailedLoginAttempts = 0;
        LockedUntil = null;
    }

    public bool IsAccountLocked()
    {
        return LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
    }

    public void AddIpToWhitelist(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address cannot be null or empty", nameof(ipAddress));
        
        if (!IpWhitelist.Contains(ipAddress))
        {
            IpWhitelist.Add(ipAddress);
        }
    }

    public void RemoveIpFromWhitelist(string ipAddress)
    {
        IpWhitelist.Remove(ipAddress);
    }

    public bool IsIpWhitelisted(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;
        
        return IpWhitelist.Contains(ipAddress);
    }

    public void AddAllowedLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location cannot be null or empty", nameof(location));
        
        if (!AllowedLocations.Contains(location))
        {
            AllowedLocations.Add(location);
        }
    }

    public void RemoveAllowedLocation(string location)
    {
        AllowedLocations.Remove(location);
    }

    public bool IsLocationAllowed(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return false;
        
        return AllowedLocations.Contains(location);
    }
}
