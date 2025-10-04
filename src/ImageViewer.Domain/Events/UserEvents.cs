using MongoDB.Bson;
using ImageViewer.Domain.Interfaces;

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
