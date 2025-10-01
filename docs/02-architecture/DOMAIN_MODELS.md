# Domain Models - Image Viewer System

## Tổng quan

Document này mô tả chi tiết các domain models và business logic của hệ thống Image Viewer, được thiết kế theo Domain-Driven Design (DDD) principles.

## 🏗️ Domain Architecture

### Core Domain
- **Collection Management**: Quản lý collections và images
- **Image Processing**: Xử lý và tối ưu hóa images
- **Caching System**: Hệ thống cache thông minh
- **User Experience**: Trải nghiệm người dùng

### Supporting Domains
- **Authentication**: Xác thực và phân quyền
- **Analytics**: Thống kê và báo cáo
- **Notifications**: Thông báo và alerts
- **File Management**: Quản lý files và storage

## 📋 Core Domain Models

### 1. Collection Aggregate

#### Collection Entity
```csharp
public class Collection : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Path { get; private set; }
    public CollectionType Type { get; private set; }
    public CollectionSettings Settings { get; private set; }
    public CollectionStatistics Statistics { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    
    // Navigation properties
    public ICollection<Image> Images { get; private set; } = new List<Image>();
    public ICollection<CollectionTag> Tags { get; private set; } = new List<CollectionTag>();
    public ICollection<ViewSession> ViewSessions { get; private set; } = new List<ViewSession>();
    
    // Domain events
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    // Constructors
    private Collection() { } // EF Core
    
    public Collection(string name, string path, CollectionType type, CollectionSettings settings)
    {
        Id = Guid.NewGuid();
        Name = name;
        Path = path;
        Type = type;
        Settings = settings;
        Statistics = new CollectionStatistics();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;
        
        AddDomainEvent(new CollectionCreatedEvent(Id, Name, Path, Type));
    }
    
    // Business methods
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Collection name cannot be empty", nameof(name));
            
        Name = name;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new CollectionNameUpdatedEvent(Id, Name));
    }
    
    public void UpdateSettings(CollectionSettings settings)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new CollectionSettingsUpdatedEvent(Id, Settings));
    }
    
    public void AddImage(Image image)
    {
        if (image == null)
            throw new ArgumentNullException(nameof(image));
            
        Images.Add(image);
        Statistics.IncrementImageCount();
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new ImageAddedToCollectionEvent(Id, image.Id));
    }
    
    public void RemoveImage(Guid imageId)
    {
        var image = Images.FirstOrDefault(i => i.Id == imageId);
        if (image == null)
            throw new InvalidOperationException($"Image {imageId} not found in collection {Id}");
            
        Images.Remove(image);
        Statistics.DecrementImageCount();
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new ImageRemovedFromCollectionEvent(Id, imageId));
    }
    
    public void AddTag(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name cannot be empty", nameof(tagName));
            
        if (Tags.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
            return; // Tag already exists
            
        var tag = new CollectionTag(tagName);
        Tags.Add(tag);
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new TagAddedToCollectionEvent(Id, tagName));
    }
    
    public void RemoveTag(string tagName)
    {
        var tag = Tags.FirstOrDefault(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
        if (tag == null)
            return;
            
        Tags.Remove(tag);
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new TagRemovedFromCollectionEvent(Id, tagName));
    }
    
    public void MarkAsDeleted()
    {
        if (IsDeleted)
            return;
            
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new CollectionDeletedEvent(Id, Name));
    }
    
    public void Restore()
    {
        if (!IsDeleted)
            return;
            
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new CollectionRestoredEvent(Id, Name));
    }
    
    public void UpdateStatistics(CollectionStatistics newStatistics)
    {
        Statistics = newStatistics ?? throw new ArgumentNullException(nameof(newStatistics));
        UpdatedAt = DateTime.UtcNow;
    }
    
    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

#### CollectionType Enum
```csharp
public enum CollectionType : byte
{
    Folder = 1,
    Zip = 2,
    SevenZip = 3,
    Rar = 4,
    Tar = 5,
    Gzip = 6,
    Bzip2 = 7
}
```

#### CollectionSettings Value Object
```csharp
public class CollectionSettings : ValueObject
{
    public bool AutoScan { get; private set; }
    public bool GenerateThumbnails { get; private set; }
    public bool GenerateCache { get; private set; }
    public ThumbnailSettings ThumbnailSettings { get; private set; }
    public CacheSettings CacheSettings { get; private set; }
    public ScanSettings ScanSettings { get; private set; }
    
    private CollectionSettings() { } // EF Core
    
    public CollectionSettings(
        bool autoScan = true,
        bool generateThumbnails = true,
        bool generateCache = true,
        ThumbnailSettings thumbnailSettings = null,
        CacheSettings cacheSettings = null,
        ScanSettings scanSettings = null)
    {
        AutoScan = autoScan;
        GenerateThumbnails = generateThumbnails;
        GenerateCache = generateCache;
        ThumbnailSettings = thumbnailSettings ?? ThumbnailSettings.Default;
        CacheSettings = cacheSettings ?? CacheSettings.Default;
        ScanSettings = scanSettings ?? ScanSettings.Default;
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return AutoScan;
        yield return GenerateThumbnails;
        yield return GenerateCache;
        yield return ThumbnailSettings;
        yield return CacheSettings;
        yield return ScanSettings;
    }
}
```

#### CollectionStatistics Value Object
```csharp
public class CollectionStatistics : ValueObject
{
    public int ImageCount { get; private set; }
    public long TotalSize { get; private set; }
    public int ThumbnailCount { get; private set; }
    public int CacheCount { get; private set; }
    public DateTime LastScanned { get; private set; }
    public DateTime LastUpdated { get; private set; }
    
    private CollectionStatistics() { } // EF Core
    
    public CollectionStatistics()
    {
        ImageCount = 0;
        TotalSize = 0;
        ThumbnailCount = 0;
        CacheCount = 0;
        LastScanned = DateTime.UtcNow;
        LastUpdated = DateTime.UtcNow;
    }
    
    public void IncrementImageCount()
    {
        ImageCount++;
        LastUpdated = DateTime.UtcNow;
    }
    
    public void DecrementImageCount()
    {
        if (ImageCount > 0)
            ImageCount--;
        LastUpdated = DateTime.UtcNow;
    }
    
    public void UpdateTotalSize(long size)
    {
        TotalSize = size;
        LastUpdated = DateTime.UtcNow;
    }
    
    public void UpdateThumbnailCount(int count)
    {
        ThumbnailCount = count;
        LastUpdated = DateTime.UtcNow;
    }
    
    public void UpdateCacheCount(int count)
    {
        CacheCount = count;
        LastUpdated = DateTime.UtcNow;
    }
    
    public void UpdateLastScanned()
    {
        LastScanned = DateTime.UtcNow;
        LastUpdated = DateTime.UtcNow;
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ImageCount;
        yield return TotalSize;
        yield return ThumbnailCount;
        yield return CacheCount;
        yield return LastScanned;
        yield return LastUpdated;
    }
}
```

### 2. Image Aggregate

#### Image Entity
```csharp
public class Image : AggregateRoot<Guid>
{
    public Guid CollectionId { get; private set; }
    public string FileName { get; private set; }
    public string FilePath { get; private set; }
    public string RelativePath { get; private set; }
    public long FileSize { get; private set; }
    public ImageMetadata Metadata { get; private set; }
    public ImageCacheInfo CacheInfo { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    
    // Navigation properties
    public Collection Collection { get; private set; }
    public ICollection<ImageTag> Tags { get; private set; } = new List<ImageTag>();
    public ICollection<ViewSession> ViewSessions { get; private set; } = new List<ViewSession>();
    
    // Domain events
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    // Constructors
    private Image() { } // EF Core
    
    public Image(
        Guid collectionId,
        string fileName,
        string filePath,
        string relativePath,
        long fileSize,
        ImageMetadata metadata)
    {
        Id = Guid.NewGuid();
        CollectionId = collectionId;
        FileName = fileName;
        FilePath = filePath;
        RelativePath = relativePath;
        FileSize = fileSize;
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        CacheInfo = new ImageCacheInfo();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;
        
        AddDomainEvent(new ImageCreatedEvent(Id, CollectionId, FileName, FilePath));
    }
    
    // Business methods
    public void UpdateMetadata(ImageMetadata metadata)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new ImageMetadataUpdatedEvent(Id, Metadata));
    }
    
    public void UpdateCacheInfo(ImageCacheInfo cacheInfo)
    {
        CacheInfo = cacheInfo ?? throw new ArgumentNullException(nameof(cacheInfo));
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void AddTag(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name cannot be empty", nameof(tagName));
            
        if (Tags.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
            return; // Tag already exists
            
        var tag = new ImageTag(tagName);
        Tags.Add(tag);
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new TagAddedToImageEvent(Id, tagName));
    }
    
    public void RemoveTag(string tagName)
    {
        var tag = Tags.FirstOrDefault(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
        if (tag == null)
            return;
            
        Tags.Remove(tag);
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new TagRemovedFromImageEvent(Id, tagName));
    }
    
    public void MarkAsDeleted()
    {
        if (IsDeleted)
            return;
            
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new ImageDeletedEvent(Id, FileName));
    }
    
    public void Restore()
    {
        if (!IsDeleted)
            return;
            
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new ImageRestoredEvent(Id, FileName));
    }
    
    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

#### ImageMetadata Value Object
```csharp
public class ImageMetadata : ValueObject
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public string Format { get; private set; }
    public string ColorSpace { get; private set; }
    public int BitDepth { get; private set; }
    public bool HasTransparency { get; private set; }
    public ExifData ExifData { get; private set; }
    public DateTime? TakenAt { get; private set; }
    public string Camera { get; private set; }
    public string Lens { get; private set; }
    public string Location { get; private set; }
    
    private ImageMetadata() { } // EF Core
    
    public ImageMetadata(
        int width,
        int height,
        string format,
        string colorSpace = null,
        int bitDepth = 8,
        bool hasTransparency = false,
        ExifData exifData = null,
        DateTime? takenAt = null,
        string camera = null,
        string lens = null,
        string location = null)
    {
        Width = width;
        Height = height;
        Format = format ?? throw new ArgumentNullException(nameof(format));
        ColorSpace = colorSpace;
        BitDepth = bitDepth;
        HasTransparency = hasTransparency;
        ExifData = exifData;
        TakenAt = takenAt;
        Camera = camera;
        Lens = lens;
        Location = location;
    }
    
    public double AspectRatio => Width > 0 && Height > 0 ? (double)Width / Height : 1.0;
    
    public string Orientation => Width > Height ? "landscape" : Width < Height ? "portrait" : "square";
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Width;
        yield return Height;
        yield return Format;
        yield return ColorSpace;
        yield return BitDepth;
        yield return HasTransparency;
        yield return ExifData;
        yield return TakenAt;
        yield return Camera;
        yield return Lens;
        yield return Location;
    }
}
```

#### ImageCacheInfo Value Object
```csharp
public class ImageCacheInfo : ValueObject
{
    public bool HasThumbnail { get; private set; }
    public bool HasCache { get; private set; }
    public string ThumbnailPath { get; private set; }
    public string CachePath { get; private set; }
    public DateTime? ThumbnailGeneratedAt { get; private set; }
    public DateTime? CacheGeneratedAt { get; private set; }
    public string ThumbnailHash { get; private set; }
    public string CacheHash { get; private set; }
    
    private ImageCacheInfo() { } // EF Core
    
    public ImageCacheInfo()
    {
        HasThumbnail = false;
        HasCache = false;
        ThumbnailPath = null;
        CachePath = null;
        ThumbnailGeneratedAt = null;
        CacheGeneratedAt = null;
        ThumbnailHash = null;
        CacheHash = null;
    }
    
    public void UpdateThumbnailInfo(string path, string hash)
    {
        HasThumbnail = true;
        ThumbnailPath = path;
        ThumbnailHash = hash;
        ThumbnailGeneratedAt = DateTime.UtcNow;
    }
    
    public void UpdateCacheInfo(string path, string hash)
    {
        HasCache = true;
        CachePath = path;
        CacheHash = hash;
        CacheGeneratedAt = DateTime.UtcNow;
    }
    
    public void ClearThumbnailInfo()
    {
        HasThumbnail = false;
        ThumbnailPath = null;
        ThumbnailHash = null;
        ThumbnailGeneratedAt = null;
    }
    
    public void ClearCacheInfo()
    {
        HasCache = false;
        CachePath = null;
        CacheHash = null;
        CacheGeneratedAt = null;
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return HasThumbnail;
        yield return HasCache;
        yield return ThumbnailPath;
        yield return CachePath;
        yield return ThumbnailGeneratedAt;
        yield return CacheGeneratedAt;
        yield return ThumbnailHash;
        yield return CacheHash;
    }
}
```

### 3. Cache Management Domain

#### CacheFolder Entity
```csharp
public class CacheFolder : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Path { get; private set; }
    public long MaxSize { get; private set; }
    public long CurrentSize { get; private set; }
    public int MaxCollections { get; private set; }
    public int CurrentCollections { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    // Navigation properties
    public ICollection<Collection> Collections { get; private set; } = new List<Collection>();
    
    // Domain events
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    // Constructors
    private CacheFolder() { } // EF Core
    
    public CacheFolder(string name, string path, long maxSize, int maxCollections)
    {
        Id = Guid.NewGuid();
        Name = name;
        Path = path;
        MaxSize = maxSize;
        CurrentSize = 0;
        MaxCollections = maxCollections;
        CurrentCollections = 0;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new CacheFolderCreatedEvent(Id, Name, Path));
    }
    
    // Business methods
    public void UpdateSize(long size)
    {
        if (size < 0)
            throw new ArgumentException("Size cannot be negative", nameof(size));
            
        CurrentSize = size;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateCollectionCount(int count)
    {
        if (count < 0)
            throw new ArgumentException("Collection count cannot be negative", nameof(count));
            
        CurrentCollections = count;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Activate()
    {
        if (IsActive)
            return;
            
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new CacheFolderActivatedEvent(Id, Name));
    }
    
    public void Deactivate()
    {
        if (!IsActive)
            return;
            
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new CacheFolderDeactivatedEvent(Id, Name));
    }
    
    public bool CanAcceptCollection(long collectionSize)
    {
        return IsActive && 
               CurrentSize + collectionSize <= MaxSize && 
               CurrentCollections < MaxCollections;
    }
    
    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

### 4. Tag Management Domain

#### Tag Entity
```csharp
public class Tag : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public TagType Type { get; private set; }
    public TagColor Color { get; private set; }
    public int UsageCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    
    // Navigation properties
    public ICollection<CollectionTag> CollectionTags { get; private set; } = new List<CollectionTag>();
    public ICollection<ImageTag> ImageTags { get; private set; } = new List<ImageTag>();
    
    // Domain events
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    // Constructors
    private Tag() { } // EF Core
    
    public Tag(string name, string description = null, TagType type = TagType.General, TagColor color = TagColor.Default)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
        Type = type;
        Color = color;
        UsageCount = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;
        
        AddDomainEvent(new TagCreatedEvent(Id, Name, Type));
    }
    
    // Business methods
    public void UpdateDescription(string description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new TagDescriptionUpdatedEvent(Id, Name, Description));
    }
    
    public void UpdateType(TagType type)
    {
        Type = type;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new TagTypeUpdatedEvent(Id, Name, Type));
    }
    
    public void UpdateColor(TagColor color)
    {
        Color = color;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new TagColorUpdatedEvent(Id, Name, Color));
    }
    
    public void IncrementUsageCount()
    {
        UsageCount++;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void DecrementUsageCount()
    {
        if (UsageCount > 0)
            UsageCount--;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsDeleted()
    {
        if (IsDeleted)
            return;
            
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new TagDeletedEvent(Id, Name));
    }
    
    public void Restore()
    {
        if (!IsDeleted)
            return;
            
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new TagRestoredEvent(Id, Name));
    }
    
    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

#### TagType Enum
```csharp
public enum TagType : byte
{
    General = 1,
    Category = 2,
    Color = 3,
    Location = 4,
    Person = 5,
    Event = 6,
    Custom = 7
}
```

#### TagColor Value Object
```csharp
public class TagColor : ValueObject
{
    public string Hex { get; private set; }
    public string Name { get; private set; }
    
    private TagColor() { } // EF Core
    
    public TagColor(string hex, string name = null)
    {
        Hex = hex ?? throw new ArgumentNullException(nameof(hex));
        Name = name;
    }
    
    public static TagColor Default => new("#6B7280", "Gray");
    public static TagColor Red => new("#EF4444", "Red");
    public static TagColor Blue => new("#3B82F6", "Blue");
    public static TagColor Green => new("#10B981", "Green");
    public static TagColor Yellow => new("#F59E0B", "Yellow");
    public static TagColor Purple => new("#8B5CF6", "Purple");
    public static TagColor Pink => new("#EC4899", "Pink");
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Hex;
        yield return Name;
    }
}
```

### 5. View Session Domain

#### ViewSession Entity
```csharp
public class ViewSession : AggregateRoot<Guid>
{
    public Guid CollectionId { get; private set; }
    public Guid? ImageId { get; private set; }
    public string UserId { get; private set; }
    public ViewSessionSettings Settings { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public TimeSpan Duration => EndedAt?.Subtract(StartedAt) ?? TimeSpan.Zero;
    
    // Navigation properties
    public Collection Collection { get; private set; }
    public Image CurrentImage { get; private set; }
    public ICollection<ViewEvent> Events { get; private set; } = new List<ViewEvent>();
    
    // Domain events
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    // Constructors
    private ViewSession() { } // EF Core
    
    public ViewSession(
        Guid collectionId,
        Guid? imageId,
        string userId,
        ViewSessionSettings settings)
    {
        Id = Guid.NewGuid();
        CollectionId = collectionId;
        ImageId = imageId;
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        StartedAt = DateTime.UtcNow;
        EndedAt = null;
        
        AddDomainEvent(new ViewSessionStartedEvent(Id, CollectionId, ImageId, UserId));
    }
    
    // Business methods
    public void NavigateToImage(Guid imageId)
    {
        ImageId = imageId;
        
        var viewEvent = new ViewEvent(ViewEventType.Navigation, imageId, DateTime.UtcNow);
        Events.Add(viewEvent);
        
        AddDomainEvent(new ViewSessionNavigatedEvent(Id, imageId));
    }
    
    public void UpdateSettings(ViewSessionSettings settings)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        
        AddDomainEvent(new ViewSessionSettingsUpdatedEvent(Id, Settings));
    }
    
    public void End()
    {
        if (EndedAt.HasValue)
            return;
            
        EndedAt = DateTime.UtcNow;
        
        AddDomainEvent(new ViewSessionEndedEvent(Id, Duration));
    }
    
    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

## 🔄 Domain Events

### Base Domain Event
```csharp
public abstract class DomainEvent : IDomainEvent
{
    public Guid Id { get; private set; }
    public DateTime OccurredOn { get; private set; }
    public string EventType { get; private set; }
    
    protected DomainEvent()
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
        EventType = GetType().Name;
    }
}
```

### Collection Events
```csharp
public class CollectionCreatedEvent : DomainEvent
{
    public Guid CollectionId { get; private set; }
    public string Name { get; private set; }
    public string Path { get; private set; }
    public CollectionType Type { get; private set; }
    
    public CollectionCreatedEvent(Guid collectionId, string name, string path, CollectionType type)
    {
        CollectionId = collectionId;
        Name = name;
        Path = path;
        Type = type;
    }
}

public class CollectionDeletedEvent : DomainEvent
{
    public Guid CollectionId { get; private set; }
    public string Name { get; private set; }
    
    public CollectionDeletedEvent(Guid collectionId, string name)
    {
        CollectionId = collectionId;
        Name = name;
    }
}

public class ImageAddedToCollectionEvent : DomainEvent
{
    public Guid CollectionId { get; private set; }
    public Guid ImageId { get; private set; }
    
    public ImageAddedToCollectionEvent(Guid collectionId, Guid imageId)
    {
        CollectionId = collectionId;
        ImageId = imageId;
    }
}
```

### Image Events
```csharp
public class ImageCreatedEvent : DomainEvent
{
    public Guid ImageId { get; private set; }
    public Guid CollectionId { get; private set; }
    public string FileName { get; private set; }
    public string FilePath { get; private set; }
    
    public ImageCreatedEvent(Guid imageId, Guid collectionId, string fileName, string filePath)
    {
        ImageId = imageId;
        CollectionId = collectionId;
        FileName = fileName;
        FilePath = filePath;
    }
}

public class ImageMetadataUpdatedEvent : DomainEvent
{
    public Guid ImageId { get; private set; }
    public ImageMetadata Metadata { get; private set; }
    
    public ImageMetadataUpdatedEvent(Guid imageId, ImageMetadata metadata)
    {
        ImageId = imageId;
        Metadata = metadata;
    }
}
```

## 🏭 Domain Services

### Collection Domain Service
```csharp
public interface ICollectionDomainService
{
    Task<Collection> CreateCollectionAsync(string name, string path, CollectionType type, CollectionSettings settings);
    Task<bool> CanAddImageAsync(Guid collectionId, long imageSize);
    Task<Collection> GetCollectionWithImagesAsync(Guid collectionId);
    Task<IEnumerable<Collection>> GetCollectionsByTagAsync(string tagName);
    Task<CollectionStatistics> CalculateStatisticsAsync(Guid collectionId);
}

public class CollectionDomainService : ICollectionDomainService
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly IImageRepository _imageRepository;
    
    public CollectionDomainService(
        ICollectionRepository collectionRepository,
        IImageRepository imageRepository)
    {
        _collectionRepository = collectionRepository;
        _imageRepository = imageRepository;
    }
    
    public async Task<Collection> CreateCollectionAsync(string name, string path, CollectionType type, CollectionSettings settings)
    {
        // Validate collection name uniqueness
        var existingCollection = await _collectionRepository.GetByNameAsync(name);
        if (existingCollection != null)
            throw new InvalidOperationException($"Collection with name '{name}' already exists");
            
        // Validate path
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Path '{path}' does not exist");
            
        // Create collection
        var collection = new Collection(name, path, type, settings);
        
        return collection;
    }
    
    public async Task<bool> CanAddImageAsync(Guid collectionId, long imageSize)
    {
        var collection = await _collectionRepository.GetByIdAsync(collectionId);
        if (collection == null)
            return false;
            
        // Check if collection has space for new image
        var currentSize = await _imageRepository.GetTotalSizeAsync(collectionId);
        var maxSize = collection.Settings.CacheSettings.MaxSize;
        
        return currentSize + imageSize <= maxSize;
    }
    
    public async Task<Collection> GetCollectionWithImagesAsync(Guid collectionId)
    {
        return await _collectionRepository.GetWithImagesAsync(collectionId);
    }
    
    public async Task<IEnumerable<Collection>> GetCollectionsByTagAsync(string tagName)
    {
        return await _collectionRepository.GetByTagAsync(tagName);
    }
    
    public async Task<CollectionStatistics> CalculateStatisticsAsync(Guid collectionId)
    {
        var images = await _imageRepository.GetByCollectionIdAsync(collectionId);
        
        var statistics = new CollectionStatistics
        {
            ImageCount = images.Count(),
            TotalSize = images.Sum(i => i.FileSize),
            ThumbnailCount = images.Count(i => i.CacheInfo.HasThumbnail),
            CacheCount = images.Count(i => i.CacheInfo.HasCache),
            LastScanned = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        
        return statistics;
    }
}
```

### Image Domain Service
```csharp
public interface IImageDomainService
{
    Task<Image> CreateImageAsync(Guid collectionId, string filePath, ImageMetadata metadata);
    Task<Image> GetImageWithMetadataAsync(Guid imageId);
    Task<IEnumerable<Image>> GetImagesByTagAsync(string tagName);
    Task<ImageMetadata> ExtractMetadataAsync(string filePath);
    Task<bool> IsImageFileAsync(string filePath);
}

public class ImageDomainService : IImageDomainService
{
    private readonly IImageRepository _imageRepository;
    private readonly IMetadataExtractor _metadataExtractor;
    
    public ImageDomainService(
        IImageRepository imageRepository,
        IMetadataExtractor metadataExtractor)
    {
        _imageRepository = imageRepository;
        _metadataExtractor = metadataExtractor;
    }
    
    public async Task<Image> CreateImageAsync(Guid collectionId, string filePath, ImageMetadata metadata)
    {
        var fileName = Path.GetFileName(filePath);
        var relativePath = Path.GetRelativePath(Path.GetDirectoryName(filePath), filePath);
        
        var image = new Image(collectionId, fileName, filePath, relativePath, new FileInfo(filePath).Length, metadata);
        
        return image;
    }
    
    public async Task<Image> GetImageWithMetadataAsync(Guid imageId)
    {
        return await _imageRepository.GetWithMetadataAsync(imageId);
    }
    
    public async Task<IEnumerable<Image>> GetImagesByTagAsync(string tagName)
    {
        return await _imageRepository.GetByTagAsync(tagName);
    }
    
    public async Task<ImageMetadata> ExtractMetadataAsync(string filePath)
    {
        return await _metadataExtractor.ExtractAsync(filePath);
    }
    
    public async Task<bool> IsImageFileAsync(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };
        
        return supportedExtensions.Contains(extension);
    }
}
```

## 🎯 Business Rules

### Collection Business Rules
1. **Unique Name**: Collection names must be unique within the system
2. **Valid Path**: Collection paths must exist and be accessible
3. **Size Limits**: Collections cannot exceed maximum size limits
4. **Image Limits**: Collections cannot exceed maximum image count
5. **Soft Delete**: Collections are soft deleted, not permanently removed

### Image Business Rules
1. **Valid Format**: Only supported image formats are allowed
2. **Metadata Required**: All images must have valid metadata
3. **Unique Path**: Image paths must be unique within a collection
4. **Size Validation**: Image sizes must be within acceptable limits
5. **Soft Delete**: Images are soft deleted, not permanently removed

### Cache Business Rules
1. **Size Limits**: Cache folders cannot exceed maximum size
2. **Collection Limits**: Cache folders cannot exceed maximum collection count
3. **Active Status**: Only active cache folders can accept new collections
4. **Cleanup**: Inactive cache folders are cleaned up automatically

### Tag Business Rules
1. **Unique Names**: Tag names must be unique (case-insensitive)
2. **Usage Tracking**: Tag usage counts are maintained automatically
3. **Soft Delete**: Tags are soft deleted, not permanently removed
4. **Color Validation**: Tag colors must be valid hex values

## 📊 Domain Model Summary

### Aggregates
- **Collection**: Manages collections and their images
- **Image**: Manages individual images and their metadata
- **CacheFolder**: Manages cache storage and distribution
- **Tag**: Manages tags and their usage
- **ViewSession**: Manages user viewing sessions

### Value Objects
- **CollectionSettings**: Collection configuration
- **CollectionStatistics**: Collection statistics
- **ImageMetadata**: Image technical information
- **ImageCacheInfo**: Image cache status
- **TagColor**: Tag color information
- **ViewSessionSettings**: View session configuration

### Domain Events
- **Collection Events**: Created, Updated, Deleted, etc.
- **Image Events**: Created, Updated, Deleted, etc.
- **Tag Events**: Created, Updated, Deleted, etc.
- **View Session Events**: Started, Ended, Navigated, etc.

### Domain Services
- **CollectionDomainService**: Collection business logic
- **ImageDomainService**: Image business logic
- **CacheDomainService**: Cache management logic
- **TagDomainService**: Tag management logic

## 🎯 Conclusion

Domain models được thiết kế theo DDD principles với:

1. **Clear Boundaries**: Mỗi aggregate có boundary rõ ràng
2. **Business Logic**: Logic được encapsulate trong domain models
3. **Domain Events**: Events để communicate giữa aggregates
4. **Value Objects**: Immutable objects cho data consistency
5. **Domain Services**: Services cho complex business logic

Thiết kế này đảm bảo:
- **Maintainability**: Code dễ maintain và extend
- **Testability**: Domain logic dễ test
- **Consistency**: Business rules được enforce consistently
- **Scalability**: Architecture có thể scale được
