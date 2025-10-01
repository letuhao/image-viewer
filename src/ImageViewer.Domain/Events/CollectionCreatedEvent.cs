using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Events;

/// <summary>
/// Domain event raised when a collection is created
/// </summary>
public class CollectionCreatedEvent : IDomainEvent
{
    public Guid Id { get; }
    public DateTime OccurredOn { get; }
    public Collection Collection { get; }

    public CollectionCreatedEvent(Collection collection)
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
        Collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }
}
