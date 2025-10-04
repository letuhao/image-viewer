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
    public UserSecurity Security { get; private set; }
    
    [BsonElement("statistics")]
    public UserStatistics Statistics { get; private set; }

    // Private constructor for MongoDB
    private User() { }

    public User(string username, string email, string passwordHash)
    {
        Username = username ?? throw new ArgumentNullException(nameof(username));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        
        IsActive = true;
        IsEmailVerified = false;
        
        Profile = new UserProfile();
        Settings = new UserSettings();
        Security = new UserSecurity();
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

    public void UpdateSecurity(UserSecurity newSecurity)
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
}
