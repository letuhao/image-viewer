using MongoDB.Bson;

namespace ImageViewer.Domain.Events;

/// <summary>
/// User created domain event
/// </summary>
public class UserCreatedEvent : DomainEvent
{
    public ObjectId UserId { get; }
    public string Username { get; }
    public string Email { get; }

    public UserCreatedEvent(ObjectId userId, string username, string email)
        : base("UserCreated")
    {
        UserId = userId;
        Username = username;
        Email = email;
    }
}

/// <summary>
/// User username changed domain event
/// </summary>
public class UserUsernameChangedEvent : DomainEvent
{
    public ObjectId UserId { get; }
    public string NewUsername { get; }

    public UserUsernameChangedEvent(ObjectId userId, string newUsername)
        : base("UserUsernameChanged")
    {
        UserId = userId;
        NewUsername = newUsername;
    }
}

/// <summary>
/// User email changed domain event
/// </summary>
public class UserEmailChangedEvent : DomainEvent
{
    public ObjectId UserId { get; }
    public string NewEmail { get; }

    public UserEmailChangedEvent(ObjectId userId, string newEmail)
        : base("UserEmailChanged")
    {
        UserId = userId;
        NewEmail = newEmail;
    }
}

/// <summary>
/// User email verified domain event
/// </summary>
public class UserEmailVerifiedEvent : DomainEvent
{
    public ObjectId UserId { get; }

    public UserEmailVerifiedEvent(ObjectId userId)
        : base("UserEmailVerified")
    {
        UserId = userId;
    }
}

/// <summary>
/// User activated domain event
/// </summary>
public class UserActivatedEvent : DomainEvent
{
    public ObjectId UserId { get; }

    public UserActivatedEvent(ObjectId userId)
        : base("UserActivated")
    {
        UserId = userId;
    }
}

/// <summary>
/// User deactivated domain event
/// </summary>
public class UserDeactivatedEvent : DomainEvent
{
    public ObjectId UserId { get; }

    public UserDeactivatedEvent(ObjectId userId)
        : base("UserDeactivated")
    {
        UserId = userId;
    }
}

/// <summary>
/// User profile updated domain event
/// </summary>
public class UserProfileUpdatedEvent : DomainEvent
{
    public ObjectId UserId { get; }

    public UserProfileUpdatedEvent(ObjectId userId)
        : base("UserProfileUpdated")
    {
        UserId = userId;
    }
}

/// <summary>
/// User settings updated domain event
/// </summary>
public class UserSettingsUpdatedEvent : DomainEvent
{
    public ObjectId UserId { get; }

    public UserSettingsUpdatedEvent(ObjectId userId)
        : base("UserSettingsUpdated")
    {
        UserId = userId;
    }
}

/// <summary>
/// User security updated domain event
/// </summary>
public class UserSecurityUpdatedEvent : DomainEvent
{
    public ObjectId UserId { get; }

    public UserSecurityUpdatedEvent(ObjectId userId)
        : base("UserSecurityUpdated")
    {
        UserId = userId;
    }
}

/// <summary>
/// User password changed domain event
/// </summary>
public class UserPasswordChangedEvent : DomainEvent
{
    public ObjectId UserId { get; }

    public UserPasswordChangedEvent(ObjectId userId)
        : base("UserPasswordChanged")
    {
        UserId = userId;
    }
}

/// <summary>
/// User two-factor authentication enabled domain event
/// </summary>
public class UserTwoFactorEnabledEvent : DomainEvent
{
    public ObjectId UserId { get; }

    public UserTwoFactorEnabledEvent(ObjectId userId)
        : base("UserTwoFactorEnabled")
    {
        UserId = userId;
    }
}

/// <summary>
/// User two-factor authentication disabled domain event
/// </summary>
public class UserTwoFactorDisabledEvent : DomainEvent
{
    public ObjectId UserId { get; }

    public UserTwoFactorDisabledEvent(ObjectId userId)
        : base("UserTwoFactorDisabled")
    {
        UserId = userId;
    }
}

/// <summary>
/// User login failed domain event
/// </summary>
public class UserLoginFailedEvent : DomainEvent
{
    public ObjectId UserId { get; }
    public int FailedAttempts { get; }

    public UserLoginFailedEvent(ObjectId userId, int failedAttempts)
        : base("UserLoginFailed")
    {
        UserId = userId;
        FailedAttempts = failedAttempts;
    }
}

/// <summary>
/// User login successful domain event
/// </summary>
public class UserLoginSuccessfulEvent : DomainEvent
{
    public ObjectId UserId { get; }
    public string IpAddress { get; }

    public UserLoginSuccessfulEvent(ObjectId userId, string ipAddress)
        : base("UserLoginSuccessful")
    {
        UserId = userId;
        IpAddress = ipAddress;
    }
}

/// <summary>
/// User role updated domain event
/// </summary>
public class UserRoleUpdatedEvent : DomainEvent
{
    public ObjectId UserId { get; }
    public string NewRole { get; }

    public UserRoleUpdatedEvent(ObjectId userId, string newRole)
        : base("UserRoleUpdated")
    {
        UserId = userId;
        NewRole = newRole;
    }
}