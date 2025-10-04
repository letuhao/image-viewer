using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ImageViewer.Domain.Events;
using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Library aggregate root - represents a library containing collections
/// </summary>
public class Library : BaseEntity
{
    [BsonElement("name")]
    public string Name { get; private set; }
    
    [BsonElement("description")]
    public string Description { get; private set; }
    
    [BsonElement("path")]
    public string Path { get; private set; }
    
    [BsonElement("ownerId")]
    public ObjectId OwnerId { get; private set; }
    
    [BsonElement("isPublic")]
    public bool IsPublic { get; private set; }
    
    [BsonElement("isActive")]
    public bool IsActive { get; private set; }
    
    [BsonElement("settings")]
    public LibrarySettings Settings { get; private set; }
    
    [BsonElement("metadata")]
    public LibraryMetadata Metadata { get; private set; }
    
    [BsonElement("statistics")]
    public LibraryStatistics Statistics { get; private set; }
    
    [BsonElement("watchInfo")]
    public WatchInfo WatchInfo { get; private set; }

    // Private constructor for MongoDB
    private Library() { }

    public Library(string name, string path, ObjectId ownerId, string description = "")
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        OwnerId = ownerId;
        Description = description ?? string.Empty;
        
        IsPublic = false;
        IsActive = true;
        
        Settings = new LibrarySettings();
        Metadata = new LibraryMetadata();
        Statistics = new LibraryStatistics();
        WatchInfo = new WatchInfo();
        
        AddDomainEvent(new LibraryCreatedEvent(Id, Name, OwnerId));
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Library name cannot be null or empty", nameof(newName));
        
        Name = newName;
        UpdateTimestamp();
        
        AddDomainEvent(new LibraryNameChangedEvent(Id, newName));
    }

    public void UpdateDescription(string newDescription)
    {
        Description = newDescription ?? string.Empty;
        UpdateTimestamp();
        
        AddDomainEvent(new LibraryDescriptionChangedEvent(Id, newDescription));
    }

    public void UpdatePath(string newPath)
    {
        if (string.IsNullOrWhiteSpace(newPath))
            throw new ArgumentException("Library path cannot be null or empty", nameof(newPath));
        
        Path = newPath;
        WatchInfo.UpdateWatchPath(newPath);
        UpdateTimestamp();
        
        AddDomainEvent(new LibraryPathChangedEvent(Id, newPath));
    }

    public void SetPublic(bool isPublic)
    {
        if (IsPublic != isPublic)
        {
            IsPublic = isPublic;
            UpdateTimestamp();
            
            AddDomainEvent(new LibraryVisibilityChangedEvent(Id, isPublic));
        }
    }

    public void Activate()
    {
        if (!IsActive)
        {
            IsActive = true;
            UpdateTimestamp();
            
            AddDomainEvent(new LibraryActivatedEvent(Id));
        }
    }

    public void Deactivate()
    {
        if (IsActive)
        {
            IsActive = false;
            UpdateTimestamp();
            
            AddDomainEvent(new LibraryDeactivatedEvent(Id));
        }
    }

    public void UpdateSettings(LibrarySettings newSettings)
    {
        Settings = newSettings ?? throw new ArgumentNullException(nameof(newSettings));
        UpdateTimestamp();
        
        AddDomainEvent(new LibrarySettingsUpdatedEvent(Id));
    }

    public void UpdateMetadata(LibraryMetadata newMetadata)
    {
        Metadata = newMetadata ?? throw new ArgumentNullException(nameof(newMetadata));
        UpdateTimestamp();
        
        AddDomainEvent(new LibraryMetadataUpdatedEvent(Id));
    }

    public void UpdateStatistics(LibraryStatistics newStatistics)
    {
        Statistics = newStatistics ?? throw new ArgumentNullException(nameof(newStatistics));
        UpdateTimestamp();
    }

    public void EnableWatching()
    {
        if (!WatchInfo.IsWatching)
        {
            WatchInfo.EnableWatching();
            UpdateTimestamp();
            
            AddDomainEvent(new LibraryWatchingEnabledEvent(Id));
        }
    }

    public void DisableWatching()
    {
        if (WatchInfo.IsWatching)
        {
            WatchInfo.DisableWatching();
            UpdateTimestamp();
            
            AddDomainEvent(new LibraryWatchingDisabledEvent(Id));
        }
    }
}
