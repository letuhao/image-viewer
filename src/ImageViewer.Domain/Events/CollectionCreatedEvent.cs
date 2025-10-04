using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Events;

/// <summary>
/// Domain event raised when a collection is created
/// </summary>
public class CollectionCreatedEvent : DomainEvent
{
    public ObjectId CollectionId { get; }
    public string CollectionName { get; }
    public ObjectId LibraryId { get; }

    public CollectionCreatedEvent(ObjectId collectionId, string collectionName, ObjectId libraryId)
        : base("CollectionCreated")
    {
        CollectionId = collectionId;
        CollectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
        LibraryId = libraryId;
    }
}
