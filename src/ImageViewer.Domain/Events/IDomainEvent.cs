namespace ImageViewer.Domain.Events;

/// <summary>
/// Domain event interface
/// </summary>
public interface IDomainEvent
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
}
