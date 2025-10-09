using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ImageViewer.Domain.Events;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Base entity class with domain events support for MongoDB
/// </summary>
public abstract class BaseEntity
{
    [BsonId]
    public ObjectId Id { get; set; }
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }
    
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
    
    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; } = false;
    
    // Creator/Modifier tracking
    [BsonElement("createdBy")]
    public string? CreatedBy { get; set; }
    
    [BsonElement("updatedBy")]
    public string? UpdatedBy { get; set; }
    
    [BsonElement("createdBySystem")]
    public string? CreatedBySystem { get; set; } // Track which system created the record
    
    [BsonElement("updatedBySystem")]
    public string? UpdatedBySystem { get; set; } // Track which system updated the record

    [BsonIgnore]
    private readonly List<IDomainEvent> _domainEvents = new();

    [BsonIgnore]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected BaseEntity()
    {
        Id = ObjectId.GenerateNewId();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
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

    protected void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Set creator information when creating a new entity
    /// </summary>
    protected void SetCreator(string? createdBy = null, string? createdBySystem = null)
    {
        CreatedBy = createdBy;
        CreatedBySystem = createdBySystem;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Set modifier information when updating an entity
    /// </summary>
    protected void SetModifier(string? updatedBy = null, string? updatedBySystem = null)
    {
        UpdatedBy = updatedBy;
        UpdatedBySystem = updatedBySystem;
        UpdatedAt = DateTime.UtcNow;
    }
}
