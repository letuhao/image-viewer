using MongoDB.Bson;

namespace ImageViewer.Domain.Events;

/// <summary>
/// MediaItem created domain event
/// </summary>
public class MediaItemCreatedEvent : DomainEvent
{
    public ObjectId MediaItemId { get; }
    public string MediaItemName { get; }
    public ObjectId CollectionId { get; }

    public MediaItemCreatedEvent(ObjectId mediaItemId, string mediaItemName, ObjectId collectionId)
        : base("MediaItemCreated")
    {
        MediaItemId = mediaItemId;
        MediaItemName = mediaItemName;
        CollectionId = collectionId;
    }
}

/// <summary>
/// MediaItem name changed domain event
/// </summary>
public class MediaItemNameChangedEvent : DomainEvent
{
    public ObjectId MediaItemId { get; }
    public string NewName { get; }

    public MediaItemNameChangedEvent(ObjectId mediaItemId, string newName)
        : base("MediaItemNameChanged")
    {
        MediaItemId = mediaItemId;
        NewName = newName;
    }
}

/// <summary>
/// MediaItem filename changed domain event
/// </summary>
public class MediaItemFilenameChangedEvent : DomainEvent
{
    public ObjectId MediaItemId { get; }
    public string NewFilename { get; }

    public MediaItemFilenameChangedEvent(ObjectId mediaItemId, string newFilename)
        : base("MediaItemFilenameChanged")
    {
        MediaItemId = mediaItemId;
        NewFilename = newFilename;
    }
}

/// <summary>
/// MediaItem path changed domain event
/// </summary>
public class MediaItemPathChangedEvent : DomainEvent
{
    public ObjectId MediaItemId { get; }
    public string NewPath { get; }

    public MediaItemPathChangedEvent(ObjectId mediaItemId, string newPath)
        : base("MediaItemPathChanged")
    {
        MediaItemId = mediaItemId;
        NewPath = newPath;
    }
}

/// <summary>
/// MediaItem dimensions changed domain event
/// </summary>
public class MediaItemDimensionsChangedEvent : DomainEvent
{
    public ObjectId MediaItemId { get; }
    public int Width { get; }
    public int Height { get; }

    public MediaItemDimensionsChangedEvent(ObjectId mediaItemId, int width, int height)
        : base("MediaItemDimensionsChanged")
    {
        MediaItemId = mediaItemId;
        Width = width;
        Height = height;
    }
}

/// <summary>
/// MediaItem duration changed domain event
/// </summary>
public class MediaItemDurationChangedEvent : DomainEvent
{
    public ObjectId MediaItemId { get; }
    public TimeSpan? Duration { get; }

    public MediaItemDurationChangedEvent(ObjectId mediaItemId, TimeSpan? duration)
        : base("MediaItemDurationChanged")
    {
        MediaItemId = mediaItemId;
        Duration = duration;
    }
}

/// <summary>
/// MediaItem activated domain event
/// </summary>
public class MediaItemActivatedEvent : DomainEvent
{
    public ObjectId MediaItemId { get; }

    public MediaItemActivatedEvent(ObjectId mediaItemId)
        : base("MediaItemActivated")
    {
        MediaItemId = mediaItemId;
    }
}

/// <summary>
/// MediaItem deactivated domain event
/// </summary>
public class MediaItemDeactivatedEvent : DomainEvent
{
    public ObjectId MediaItemId { get; }

    public MediaItemDeactivatedEvent(ObjectId mediaItemId)
        : base("MediaItemDeactivated")
    {
        MediaItemId = mediaItemId;
    }
}

/// <summary>
/// MediaItem metadata updated domain event
/// </summary>
public class MediaItemMetadataUpdatedEvent : DomainEvent
{
    public ObjectId MediaItemId { get; }

    public MediaItemMetadataUpdatedEvent(ObjectId mediaItemId)
        : base("MediaItemMetadataUpdated")
    {
        MediaItemId = mediaItemId;
    }
}

/// <summary>
/// MediaItem cache info updated domain event
/// </summary>
public class MediaItemCacheInfoUpdatedEvent : DomainEvent
{
    public ObjectId MediaItemId { get; }

    public MediaItemCacheInfoUpdatedEvent(ObjectId mediaItemId)
        : base("MediaItemCacheInfoUpdated")
    {
        MediaItemId = mediaItemId;
    }
}
