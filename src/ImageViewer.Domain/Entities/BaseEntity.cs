using ImageViewer.Domain.Events;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Base entity class with domain events support
/// </summary>
public abstract class BaseEntity
{
    // Optional legacy column; ignored in EF mapping when using PostgreSQL xmin
    public byte[]? RowVersion { get; set; }

    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public BaseEntity()
    {
    }

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    protected void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
