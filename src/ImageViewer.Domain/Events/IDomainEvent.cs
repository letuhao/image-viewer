namespace ImageViewer.Domain.Events;

/// <summary>
/// Domain event interface
/// </summary>
public interface IDomainEvent
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
}

/// <summary>
/// Base domain event class
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    public Guid Id { get; }
    public DateTime OccurredOn { get; }
    public string EventType { get; }

    protected DomainEvent(string eventType)
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
        EventType = eventType;
    }
}