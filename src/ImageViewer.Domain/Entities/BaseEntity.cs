using ImageViewer.Domain.Events;
using System.ComponentModel.DataAnnotations;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Base entity class with domain events support
/// </summary>
public abstract class BaseEntity
{
    // Concurrency token
    [Timestamp]  // EF Core sẽ dùng cho RowVersion
    public byte[] RowVersion { get; set; }

    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public BaseEntity()
    {
        RowVersion = new byte[8];  // Initialize with 8 bytes for timestamp
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
