using MongoDB.Bson;

namespace ImageViewer.Domain.Events;

/// <summary>
/// Library created domain event
/// </summary>
public class LibraryCreatedEvent : DomainEvent
{
    public ObjectId LibraryId { get; }
    public string LibraryName { get; }
    public ObjectId OwnerId { get; }

    public LibraryCreatedEvent(ObjectId libraryId, string libraryName, ObjectId ownerId)
        : base("LibraryCreated")
    {
        LibraryId = libraryId;
        LibraryName = libraryName;
        OwnerId = ownerId;
    }
}

/// <summary>
/// Library name changed domain event
/// </summary>
public class LibraryNameChangedEvent : DomainEvent
{
    public ObjectId LibraryId { get; }
    public string NewName { get; }

    public LibraryNameChangedEvent(ObjectId libraryId, string newName)
        : base("LibraryNameChanged")
    {
        LibraryId = libraryId;
        NewName = newName;
    }
}

/// <summary>
/// Library description changed domain event
/// </summary>
public class LibraryDescriptionChangedEvent : DomainEvent
{
    public ObjectId LibraryId { get; }
    public string NewDescription { get; }

    public LibraryDescriptionChangedEvent(ObjectId libraryId, string newDescription)
        : base("LibraryDescriptionChanged")
    {
        LibraryId = libraryId;
        NewDescription = newDescription;
    }
}

/// <summary>
/// Library path changed domain event
/// </summary>
public class LibraryPathChangedEvent : DomainEvent
{
    public ObjectId LibraryId { get; }
    public string NewPath { get; }

    public LibraryPathChangedEvent(ObjectId libraryId, string newPath)
        : base("LibraryPathChanged")
    {
        LibraryId = libraryId;
        NewPath = newPath;
    }
}

/// <summary>
/// Library visibility changed domain event
/// </summary>
public class LibraryVisibilityChangedEvent : DomainEvent
{
    public ObjectId LibraryId { get; }
    public bool IsPublic { get; }

    public LibraryVisibilityChangedEvent(ObjectId libraryId, bool isPublic)
        : base("LibraryVisibilityChanged")
    {
        LibraryId = libraryId;
        IsPublic = isPublic;
    }
}

/// <summary>
/// Library activated domain event
/// </summary>
public class LibraryActivatedEvent : DomainEvent
{
    public ObjectId LibraryId { get; }

    public LibraryActivatedEvent(ObjectId libraryId)
        : base("LibraryActivated")
    {
        LibraryId = libraryId;
    }
}

/// <summary>
/// Library deactivated domain event
/// </summary>
public class LibraryDeactivatedEvent : DomainEvent
{
    public ObjectId LibraryId { get; }

    public LibraryDeactivatedEvent(ObjectId libraryId)
        : base("LibraryDeactivated")
    {
        LibraryId = libraryId;
    }
}

/// <summary>
/// Library settings updated domain event
/// </summary>
public class LibrarySettingsUpdatedEvent : DomainEvent
{
    public ObjectId LibraryId { get; }

    public LibrarySettingsUpdatedEvent(ObjectId libraryId)
        : base("LibrarySettingsUpdated")
    {
        LibraryId = libraryId;
    }
}

/// <summary>
/// Library metadata updated domain event
/// </summary>
public class LibraryMetadataUpdatedEvent : DomainEvent
{
    public ObjectId LibraryId { get; }

    public LibraryMetadataUpdatedEvent(ObjectId libraryId)
        : base("LibraryMetadataUpdated")
    {
        LibraryId = libraryId;
    }
}

/// <summary>
/// Library watching enabled domain event
/// </summary>
public class LibraryWatchingEnabledEvent : DomainEvent
{
    public ObjectId LibraryId { get; }

    public LibraryWatchingEnabledEvent(ObjectId libraryId)
        : base("LibraryWatchingEnabled")
    {
        LibraryId = libraryId;
    }
}

/// <summary>
/// Library watching disabled domain event
/// </summary>
public class LibraryWatchingDisabledEvent : DomainEvent
{
    public ObjectId LibraryId { get; }

    public LibraryWatchingDisabledEvent(ObjectId libraryId)
        : base("LibraryWatchingDisabled")
    {
        LibraryId = libraryId;
    }
}
