# Domain Models - Image Viewer System

## Tổng quan

Document này mô tả chi tiết các domain models và business logic của hệ thống Image Viewer, được thiết kế theo Domain-Driven Design (DDD) principles.

## 🏗️ Domain Architecture

### Core Domain
- **Library Management**: Quản lý libraries và folders
- **Collection Management**: Quản lý collections và media items
- **Media Processing**: Xử lý và tối ưu hóa images/videos
- **Caching System**: Hệ thống cache thông minh
- **File System Monitoring**: Theo dõi thay đổi filesystem
- **Favorite Lists**: Quản lý danh sách yêu thích
- **User Experience**: Trải nghiệm người dùng
- **Analytics & Tracking**: User behavior tracking và content analytics

### Supporting Domains
- **Background Jobs**: Xử lý tác vụ nền
- **System Settings**: Cấu hình hệ thống
- **User Settings**: Cài đặt người dùng
- **Analytics & Reporting**: Thống kê, báo cáo và insights
- **Search Analytics**: Search performance và query analysis
- **Content Popularity**: Popularity scoring và trending analysis
- **Notifications**: Thông báo và alerts
- **File Management**: Quản lý files và storage

## 📋 Core Domain Models

### 1. Library Aggregate

#### Library Entity
```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Library : AggregateRoot<ObjectId>
{
    [BsonId]
    public ObjectId Id { get; private set; }
    
    public string Name { get; private set; }
    public string Path { get; private set; }
    public string Type { get; private set; } // "local", "network", "cloud"
    public LibrarySettings Settings { get; private set; }
    public LibraryMetadata Metadata { get; private set; }
    public LibraryStatistics Statistics { get; private set; }
    public WatchInfo WatchInfo { get; private set; }
    public SearchIndex SearchIndex { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    // Domain events
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    // Constructors
    private Library() { } // MongoDB
    
    public Library(string name, string path, string type, LibrarySettings settings)
    {
        Id = ObjectId.GenerateNewId();
        Name = name;
        Path = path;
        Type = type;
        Settings = settings;
        Metadata = new LibraryMetadata();
        Statistics = new LibraryStatistics();
        WatchInfo = new WatchInfo { IsWatching = false };
        SearchIndex = new SearchIndex();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new LibraryCreatedEvent(Id, Name, Path, Type));
    }
    
    // Business methods
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Library name cannot be empty", nameof(name));
            
        Name = name;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new LibraryNameUpdatedEvent(Id, Name));
    }
    
    public void UpdateSettings(LibrarySettings settings)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new LibrarySettingsUpdatedEvent(Id, Settings));
    }
    
    public void EnableWatching()
    {
        WatchInfo.IsWatching = true;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new LibraryWatchingEnabledEvent(Id));
    }
    
    public void DisableWatching()
    {
        WatchInfo.IsWatching = false;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new LibraryWatchingDisabledEvent(Id));
    }
    
    public void UpdateStatistics(LibraryStatistics statistics)
    {
        Statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new LibraryStatisticsUpdatedEvent(Id, Statistics));
    }
    
    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty", nameof(tag));
            
        if (!Metadata.Tags.Contains(tag))
        {
            Metadata.Tags.Add(tag);
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new LibraryTagAddedEvent(Id, tag));
        }
    }
    
    public void RemoveTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;
            
        if (Metadata.Tags.Remove(tag))
        {
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new LibraryTagRemovedEvent(Id, tag));
        }
    }
    
    public void UpdateSearchIndex()
    {
        SearchIndex.SearchableText = $"{Name} {string.Join(" ", Metadata.Tags)} {Path}";
        SearchIndex.Tags = Metadata.Tags;
        SearchIndex.Path = Path;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Delete()
    {
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new LibraryDeletedEvent(Id, Name));
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
    
    // Domain event handling
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

#### Library Settings
```csharp
public class LibrarySettings
{
    public bool AutoScan { get; set; } = true;
    public int ScanInterval { get; set; } = 60; // minutes
    public string WatchMode { get; set; } = "scheduled"; // "realtime", "scheduled", "manual"
    public bool IncludeSubfolders { get; set; } = true;
    public FileFilters FileFilters { get; set; } = new();
}

public class FileFilters
{
    public List<string> Images { get; set; } = new() { ".jpg", ".png", ".gif", ".webp", ".bmp", ".tiff" };
    public List<string> Videos { get; set; } = new() { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv" };
    public List<string> ExcludePatterns { get; set; } = new() { "*.tmp", "*.log", "*.DS_Store" };
}

public class LibraryMetadata
{
    public string Description { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedDate { get; set; }
    public DateTime LastModified { get; set; }
    public int TotalCollections { get; set; }
    public long TotalSize { get; set; }
    public DateTime LastScanDate { get; set; }
}

public class LibraryStatistics
{
    public int CollectionCount { get; set; }
    public int TotalItems { get; set; }
    public long TotalSize { get; set; }
    public double LastScanDuration { get; set; }
    public int ScanCount { get; set; }
    public DateTime LastScanDate { get; set; }
}

public class WatchInfo
{
    public bool IsWatching { get; set; }
    public DateTime LastWatchCheck { get; set; }
    public List<WatchError> WatchErrors { get; set; } = new();
    public FileSystemWatcherInfo FileSystemWatcher { get; set; } = new();
}

public class WatchError
{
    public string Path { get; set; }
    public string Error { get; set; }
    public DateTime Timestamp { get; set; }
}

public class FileSystemWatcherInfo
{
    public bool Enabled { get; set; }
    public DateTime LastEvent { get; set; }
    public int EventCount { get; set; }
}

public class SearchIndex
{
    public string SearchableText { get; set; }
    public List<string> Tags { get; set; }
    public string Path { get; set; }
}
```

### 2. Collection Aggregate

#### Collection Entity
```csharp
public class Collection : AggregateRoot<ObjectId>
{
    [BsonId]
    public ObjectId Id { get; private set; }
    
    public ObjectId LibraryId { get; private set; }
    public string Name { get; private set; }
    public string Path { get; private set; }
    public string Type { get; private set; } // "image", "video", "mixed"
    public CollectionSettings Settings { get; private set; }
    public CollectionMetadata Metadata { get; private set; }
    public CollectionStatistics Statistics { get; private set; }
    public CacheInfo CacheInfo { get; private set; }
    public WatchInfo WatchInfo { get; private set; }
    public SearchIndex SearchIndex { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    // Domain events
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    // Constructors
    private Collection() { } // MongoDB
    
    public Collection(ObjectId libraryId, string name, string path, string type, CollectionSettings settings)
    {
        Id = ObjectId.GenerateNewId();
        LibraryId = libraryId;
        Name = name;
        Path = path;
        Type = type;
        Settings = settings;
        Metadata = new CollectionMetadata();
        Statistics = new CollectionStatistics();
        CacheInfo = new CacheInfo();
        WatchInfo = new WatchInfo { IsWatching = false };
        SearchIndex = new SearchIndex();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new CollectionCreatedEvent(Id, LibraryId, Name, Path, Type));
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
    
    public void EnableWatching()
    {
        WatchInfo.IsWatching = true;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new CollectionWatchingEnabledEvent(Id));
    }
    
    public void DisableWatching()
    {
        WatchInfo.IsWatching = false;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new CollectionWatchingDisabledEvent(Id));
    }
    
    public void UpdateStatistics(CollectionStatistics statistics)
    {
        Statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new CollectionStatisticsUpdatedEvent(Id, Statistics));
    }
    
    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty", nameof(tag));
            
        if (!Metadata.Tags.Contains(tag))
        {
            Metadata.Tags.Add(tag);
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new CollectionTagAddedEvent(Id, tag));
        }
    }
    
    public void RemoveTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;
            
        if (Metadata.Tags.Remove(tag))
        {
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new CollectionTagRemovedEvent(Id, tag));
        }
    }
    
    public void UpdateSearchIndex()
    {
        SearchIndex.SearchableText = $"{Name} {string.Join(" ", Metadata.Tags)} {Path}";
        SearchIndex.Tags = Metadata.Tags;
        SearchIndex.Metadata = Metadata;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Delete()
    {
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new CollectionDeletedEvent(Id, Name));
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
    
    // Domain event handling
    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}

#### Collection Settings Value Object
```csharp
public class CollectionSettings : ValueObject
{
    public int ThumbnailSize { get; private set; }
    public bool CacheEnabled { get; private set; }
    public bool AutoScan { get; private set; }
    public int ScanInterval { get; private set; }
    public int Priority { get; private set; }
    public string WatchMode { get; private set; }
    
    private CollectionSettings() { } // MongoDB
    
    public CollectionSettings(
        int thumbnailSize = 300,
        bool cacheEnabled = true,
        bool autoScan = true,
        int scanInterval = 60,
        int priority = 0,
        string watchMode = "scheduled")
    {
        ThumbnailSize = thumbnailSize;
        CacheEnabled = cacheEnabled;
        AutoScan = autoScan;
        ScanInterval = scanInterval;
        Priority = priority;
        WatchMode = watchMode;
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ThumbnailSize;
        yield return CacheEnabled;
        yield return AutoScan;
        yield return ScanInterval;
        yield return Priority;
        yield return WatchMode;
    }
}
```

#### CollectionStatistics Value Object
```csharp
public class CollectionStatistics : ValueObject
{
    public int ImageCount { get; private set; }
    public int VideoCount { get; private set; }
    public long TotalSize { get; private set; }
    public DateTime LastScanDate { get; private set; }
    public double ScanDuration { get; private set; }
    public DateTime LastFileSystemCheck { get; private set; }
    
    private CollectionStatistics() { } // MongoDB
    
    public CollectionStatistics()
    {
        ImageCount = 0;
        VideoCount = 0;
        TotalSize = 0;
        LastScanDate = DateTime.UtcNow;
        ScanDuration = 0;
        LastFileSystemCheck = DateTime.UtcNow;
    }
    
    public void UpdateScanResults(int imageCount, int videoCount, long size, double duration)
    {
        ImageCount = imageCount;
        VideoCount = videoCount;
        TotalSize = size;
        LastScanDate = DateTime.UtcNow;
        ScanDuration = duration;
    }
    
    public void UpdateFileSystemCheck()
    {
        LastFileSystemCheck = DateTime.UtcNow;
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ImageCount;
        yield return VideoCount;
        yield return TotalSize;
        yield return LastScanDate;
        yield return ScanDuration;
        yield return LastFileSystemCheck;
    }
}
```

### 3. Media Item Aggregate

#### Media Item Entity
```csharp
public class MediaItem : AggregateRoot<ObjectId>
{
    [BsonId]
    public ObjectId Id { get; private set; }
    
    public ObjectId CollectionId { get; private set; }
    public ObjectId LibraryId { get; private set; }
    public string Filename { get; private set; }
    public string FilePath { get; private set; }
    public string FileType { get; private set; } // "image", "video"
    public string MimeType { get; private set; }
    public long FileSize { get; private set; }
    public Dimensions Dimensions { get; private set; }
    public FileInfo FileInfo { get; private set; }
    public MediaMetadata Metadata { get; private set; }
    public List<string> Tags { get; private set; }
    public CacheInfo CacheInfo { get; private set; }
    public MediaStatistics Statistics { get; private set; }
    public SearchIndex SearchIndex { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    // Domain events
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    // Constructors
    private MediaItem() { } // MongoDB
    
    public MediaItem(
        ObjectId collectionId,
        ObjectId libraryId,
        string filename,
        string filePath,
        string fileType,
        string mimeType,
        long fileSize,
        Dimensions dimensions,
        MediaMetadata metadata)
    {
        Id = ObjectId.GenerateNewId();
        CollectionId = collectionId;
        LibraryId = libraryId;
        Filename = filename;
        FilePath = filePath;
        FileType = fileType;
        MimeType = mimeType;
        FileSize = fileSize;
        Dimensions = dimensions;
        Metadata = metadata;
        Tags = new List<string>();
        CacheInfo = new CacheInfo();
        Statistics = new MediaStatistics();
        SearchIndex = new SearchIndex();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new MediaItemCreatedEvent(Id, CollectionId, LibraryId, Filename, FileType));
    }
    
    // Business methods
    public void UpdateMetadata(MediaMetadata metadata)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new MediaItemMetadataUpdatedEvent(Id, Metadata));
    }
    
    public void UpdateCacheInfo(CacheInfo cacheInfo)
    {
        CacheInfo = cacheInfo ?? throw new ArgumentNullException(nameof(cacheInfo));
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new MediaItemCacheInfoUpdatedEvent(Id, CacheInfo));
    }
    
    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty", nameof(tag));
            
        if (!Tags.Contains(tag))
        {
            Tags.Add(tag);
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new TagAddedToMediaItemEvent(Id, tag));
        }
    }
    
    public void RemoveTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;
            
        if (Tags.Remove(tag))
        {
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new TagRemovedFromMediaItemEvent(Id, tag));
        }
    }
    
    public void Delete()
    {
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new MediaItemDeletedEvent(Id, Filename));
    }
    
    public void UpdateSearchIndex()
    {
        SearchIndex.SearchableText = $"{Filename} {string.Join(" ", Tags)} {FilePath}";
        SearchIndex.Tags = Tags;
        SearchIndex.Metadata = Metadata;
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

#### Media Item Value Objects
```csharp
public class Dimensions : ValueObject
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    
    private Dimensions() { } // MongoDB
    
    public Dimensions(int width, int height)
    {
        if (width <= 0) throw new ArgumentException("Width must be positive", nameof(width));
        if (height <= 0) throw new ArgumentException("Height must be positive", nameof(height));
        
        Width = width;
        Height = height;
    }
    
    public double AspectRatio => (double)Width / Height;
    public bool IsLandscape => Width > Height;
    public bool IsPortrait => Height > Width;
    public bool IsSquare => Width == Height;
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Width;
        yield return Height;
    }
}

public class MediaMetadata : ValueObject
{
    // Image metadata
    public string Camera { get; private set; }
    public string Lens { get; private set; }
    public string Exposure { get; private set; }
    public int Iso { get; private set; }
    public string Aperture { get; private set; }
    public string FocalLength { get; private set; }
    public DateTime? DateTaken { get; private set; }
    public GpsData Gps { get; private set; }
    
    // Video metadata
    public double Duration { get; private set; }
    public double FrameRate { get; private set; }
    public long Bitrate { get; private set; }
    public string Codec { get; private set; }
    
    private MediaMetadata() { } // MongoDB
    
    public MediaMetadata(
        string camera = null,
        string lens = null,
        string exposure = null,
        int iso = 0,
        string aperture = null,
        string focalLength = null,
        DateTime? dateTaken = null,
        GpsData gps = null,
        double duration = 0,
        double frameRate = 0,
        long bitrate = 0,
        string codec = null)
    {
        Camera = camera;
        Lens = lens;
        Exposure = exposure;
        Iso = iso;
        Aperture = aperture;
        FocalLength = focalLength;
        DateTaken = dateTaken;
        Gps = gps;
        Duration = duration;
        FrameRate = frameRate;
        Bitrate = bitrate;
        Codec = codec;
    }
    
    public bool HasExifData() => !string.IsNullOrEmpty(Camera) || !string.IsNullOrEmpty(Lens);
    public bool HasGpsData() => Gps != null;
    public bool IsVideo() => Duration > 0;
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Camera;
        yield return Lens;
        yield return Exposure;
        yield return Iso;
        yield return Aperture;
        yield return FocalLength;
        yield return DateTaken;
        yield return Gps;
        yield return Duration;
        yield return FrameRate;
        yield return Bitrate;
        yield return Codec;
    }
}

public class GpsData : ValueObject
{
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public double Altitude { get; private set; }
    
    private GpsData() { } // MongoDB
    
    public GpsData(double latitude, double longitude, double altitude = 0)
    {
        if (latitude < -90 || latitude > 90) throw new ArgumentException("Latitude must be between -90 and 90", nameof(latitude));
        if (longitude < -180 || longitude > 180) throw new ArgumentException("Longitude must be between -180 and 180", nameof(longitude));
        
        Latitude = latitude;
        Longitude = longitude;
        Altitude = altitude;
    }
    
    public bool IsValid() => Latitude != 0 || Longitude != 0;
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
        yield return Altitude;
    }
}

public class MediaStatistics : ValueObject
{
    public int ViewCount { get; private set; }
    public DateTime LastViewed { get; private set; }
    public double Rating { get; private set; }
    public bool Favorite { get; private set; }
    
    private MediaStatistics() { } // MongoDB
    
    public MediaStatistics()
    {
        ViewCount = 0;
        LastViewed = DateTime.UtcNow;
        Rating = 0;
        Favorite = false;
    }
    
    public void IncrementViewCount()
    {
        ViewCount++;
        LastViewed = DateTime.UtcNow;
    }
    
    public void SetRating(double rating)
    {
        if (rating < 0 || rating > 5) throw new ArgumentException("Rating must be between 0 and 5", nameof(rating));
        Rating = rating;
    }
    
    public void SetFavorite(bool favorite)
    {
        Favorite = favorite;
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ViewCount;
        yield return LastViewed;
        yield return Rating;
        yield return Favorite;
    }
}
```

## Summary

Tôi đã cập nhật toàn bộ thiết kế database và architecture để phù hợp với MongoDB:

### 1. **Database Design (DATABASE_DESIGN.md)**
- Chuyển từ PostgreSQL sang MongoDB
- Thiết kế lại schema với document-oriented approach
- Thêm các collections mới: Libraries, System Settings, User Settings, Favorite Lists
- Cập nhật indexing strategy cho MongoDB
- Thêm aggregation pipelines cho performance

### 2. **Architecture Design (ARCHITECTURE_DESIGN.md)**
- Cập nhật infrastructure layer từ EF Core sang MongoDB Driver
- Thay đổi từ Hangfire sang RabbitMQ cho background services
- Cập nhật domain models với MongoDB attributes
- Thêm các entities mới: Library, MediaItem, SystemSetting, UserSettings, FavoriteList

### 3. **Domain Models (DOMAIN_MODELS.md)**
- Cập nhật từ Collection-centric sang Library-centric architecture
- Thêm Library aggregate với file system monitoring
- Cập nhật MediaItem entity thay cho Image entity
- Thêm các value objects mới: Dimensions, FileInfo, MediaMetadata, GpsData
- Cập nhật domain events cho MongoDB

### 4. **Key Changes**
- **Libraries**: Top-level containers cho folders/galleries
- **Collections**: Nested trong libraries với file system monitoring
- **Media Items**: Thay cho Images, hỗ trợ cả image và video
- **System Settings**: Cấu hình hệ thống tách biệt
- **User Settings**: Cài đặt người dùng cá nhân
- **Favorite Lists**: Danh sách yêu thích với smart filtering
- **Background Jobs**: RabbitMQ-based job processing
- **File System Monitoring**: Real-time change detection

### 5. **Benefits**
- **Performance**: MongoDB aggregation pipelines và indexing
- **Scalability**: Document-oriented design cho horizontal scaling
- **Flexibility**: Embedded documents giảm joins
- **Real-time**: Change streams cho live updates
- **Monitoring**: Comprehensive logging và metrics
- **Maintenance**: TTL indexes và automated cleanup

Thiết kế mới này sẽ hỗ trợ hệ thống image viewer với hàng triệu media items và hàng nghìn libraries một cách hiệu quả, với khả năng scale và maintain dễ dàng hơn so với relational database.

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

## 📊 Analytics Domain Models

### 7. User Behavior Tracking Aggregate

#### User Behavior Event Entity
```csharp
public class UserBehaviorEvent : AggregateRoot<ObjectId>
{
    [BsonId]
    public ObjectId Id { get; private set; }

    public string UserId { get; private set; }
    public string SessionId { get; private set; }
    public string EventType { get; private set; } // "view", "search", "filter", "navigate", "download", "share", "like", "favorite"
    public string TargetType { get; private set; } // "media", "collection", "library", "favorite_list", "tag"
    public ObjectId TargetId { get; private set; }
    public EventMetadata Metadata { get; private set; }
    public EventContext Context { get; private set; }
    public DateTime Timestamp { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Domain events
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Constructors
    private UserBehaviorEvent() { } // MongoDB

    public UserBehaviorEvent(
        string userId,
        string sessionId,
        string eventType,
        string targetType,
        ObjectId targetId,
        EventMetadata metadata,
        EventContext context)
    {
        Id = ObjectId.GenerateNewId();
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        TargetId = targetId;
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Timestamp = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserBehaviorEventCreatedEvent(Id, UserId, EventType, TargetType, TargetId));
    }

    // Business methods
    public bool IsViewEvent() => EventType == "view";
    public bool IsSearchEvent() => EventType == "search";
    public bool IsInteractionEvent() => new[] { "like", "share", "download", "favorite" }.Contains(EventType);
    public bool IsNavigationEvent() => EventType == "navigate";
    
    public double GetDuration() => Metadata.Duration;
    public string GetDeviceType() => Context.Device;
    public string GetQuery() => Metadata.Query;
    public int GetResultCount() => Metadata.ResultCount;

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    // Domain event handling
    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
```

#### Event Metadata Value Object
```csharp
public class EventMetadata : ValueObject
{
    // View events
    public double Duration { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public Viewport Viewport { get; private set; }
    public double ZoomLevel { get; private set; }

    // Search events
    public string Query { get; private set; }
    public SearchFilters Filters { get; private set; }
    public int ResultCount { get; private set; }
    public long SearchTime { get; private set; }
    public List<ObjectId> ClickedResults { get; private set; }

    // Navigation events
    public string FromPage { get; private set; }
    public string ToPage { get; private set; }
    public List<string> NavigationPath { get; private set; }
    public double TimeOnPage { get; private set; }

    // Interaction events
    public string Action { get; private set; }
    public Coordinates Coordinates { get; private set; }
    public string Element { get; private set; }
    public string ElementType { get; private set; }

    // Content events
    public string ContentType { get; private set; }
    public long ContentSize { get; private set; }
    public string ContentFormat { get; private set; }
    public List<string> Tags { get; private set; }
    public double Rating { get; private set; }

    private EventMetadata() { } // MongoDB

    public EventMetadata(
        double duration = 0,
        DateTime? startTime = null,
        DateTime? endTime = null,
        Viewport viewport = null,
        double zoomLevel = 1.0,
        string query = null,
        SearchFilters filters = null,
        int resultCount = 0,
        long searchTime = 0,
        List<ObjectId> clickedResults = null,
        string fromPage = null,
        string toPage = null,
        List<string> navigationPath = null,
        double timeOnPage = 0,
        string action = null,
        Coordinates coordinates = null,
        string element = null,
        string elementType = null,
        string contentType = null,
        long contentSize = 0,
        string contentFormat = null,
        List<string> tags = null,
        double rating = 0)
    {
        Duration = duration;
        StartTime = startTime ?? DateTime.UtcNow;
        EndTime = endTime ?? DateTime.UtcNow;
        Viewport = viewport;
        ZoomLevel = zoomLevel;
        Query = query;
        Filters = filters;
        ResultCount = resultCount;
        SearchTime = searchTime;
        ClickedResults = clickedResults ?? new List<ObjectId>();
        FromPage = fromPage;
        ToPage = toPage;
        NavigationPath = navigationPath ?? new List<string>();
        TimeOnPage = timeOnPage;
        Action = action;
        Coordinates = coordinates;
        Element = element;
        ElementType = elementType;
        ContentType = contentType;
        ContentSize = contentSize;
        ContentFormat = contentFormat;
        Tags = tags ?? new List<string>();
        Rating = rating;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Duration;
        yield return StartTime;
        yield return EndTime;
        yield return Viewport;
        yield return ZoomLevel;
        yield return Query;
        yield return Filters;
        yield return ResultCount;
        yield return SearchTime;
        yield return FromPage;
        yield return ToPage;
        yield return Action;
        yield return ContentType;
        yield return ContentSize;
        yield return ContentFormat;
        yield return Rating;
    }
}
```

#### Event Context Value Object
```csharp
public class EventContext : ValueObject
{
    public string UserAgent { get; private set; }
    public string IpAddress { get; private set; }
    public string Referrer { get; private set; }
    public string Language { get; private set; }
    public string Timezone { get; private set; }
    public string Device { get; private set; } // "desktop", "mobile", "tablet"
    public string Browser { get; private set; }
    public string Os { get; private set; }

    private EventContext() { } // MongoDB

    public EventContext(
        string userAgent = null,
        string ipAddress = null,
        string referrer = null,
        string language = null,
        string timezone = null,
        string device = null,
        string browser = null,
        string os = null)
    {
        UserAgent = userAgent;
        IpAddress = ipAddress;
        Referrer = referrer;
        Language = language;
        Timezone = timezone;
        Device = device;
        Browser = browser;
        Os = os;
    }

    public bool IsMobile() => Device == "mobile";
    public bool IsDesktop() => Device == "desktop";
    public bool IsTablet() => Device == "tablet";

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return UserAgent;
        yield return IpAddress;
        yield return Referrer;
        yield return Language;
        yield return Timezone;
        yield return Device;
        yield return Browser;
        yield return Os;
    }
}
```

### 8. User Analytics Aggregate

#### User Analytics Entity
```csharp
public class UserAnalytics : AggregateRoot<ObjectId>
{
    [BsonId]
    public ObjectId Id { get; private set; }

    public string UserId { get; private set; }
    public string Period { get; private set; } // "daily", "weekly", "monthly", "yearly"
    public DateTime Date { get; private set; }
    public UserMetrics Metrics { get; private set; }
    public TopContent TopContent { get; private set; }
    public UserPreferences Preferences { get; private set; }
    public UserDemographics Demographics { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Domain events
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Constructors
    private UserAnalytics() { } // MongoDB

    public UserAnalytics(
        string userId,
        string period,
        DateTime date,
        UserMetrics metrics,
        TopContent topContent,
        UserPreferences preferences,
        UserDemographics demographics)
    {
        Id = ObjectId.GenerateNewId();
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Period = period ?? throw new ArgumentNullException(nameof(period));
        Date = date;
        Metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        TopContent = topContent ?? throw new ArgumentNullException(nameof(topContent));
        Preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        Demographics = demographics ?? throw new ArgumentNullException(nameof(demographics));
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserAnalyticsCreatedEvent(Id, UserId, Period, Date));
    }

    // Business methods
    public bool IsActiveUser() => Metrics.TotalViews > 0 || Metrics.TotalSearches > 0;
    public double GetEngagementScore() => (Metrics.TotalLikes + Metrics.TotalShares + Metrics.TotalFavorites) / Math.Max(Metrics.TotalViews, 1);
    public List<string> GetTopTags() => TopContent.MostUsedTags.Take(10).Select(t => t.Tag).ToList();
    
    public string GetUserSegment()
    {
        if (Metrics.TotalViews > 1000) return "power_user";
        if (Metrics.TotalViews > 100) return "active_user";
        if (Metrics.TotalViews > 10) return "regular_user";
        return "casual_user";
    }

    public void UpdateMetrics(UserMetrics newMetrics)
    {
        Metrics = newMetrics ?? throw new ArgumentNullException(nameof(newMetrics));
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserAnalyticsUpdatedEvent(Id, UserId, Metrics));
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    // Domain event handling
    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
```

### 9. Content Popularity Aggregate

#### Content Popularity Entity
```csharp
public class ContentPopularity : AggregateRoot<ObjectId>
{
    [BsonId]
    public ObjectId Id { get; private set; }

    public string TargetType { get; private set; } // "media", "collection", "library", "tag"
    public ObjectId TargetId { get; private set; }
    public string Period { get; private set; } // "daily", "weekly", "monthly", "yearly", "all_time"
    public DateTime Date { get; private set; }
    public PopularityMetrics Metrics { get; private set; }
    public ContentTrends Trends { get; private set; }
    public RelatedContent RelatedContent { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Domain events
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Constructors
    private ContentPopularity() { } // MongoDB

    public ContentPopularity(
        string targetType,
        ObjectId targetId,
        string period,
        DateTime date,
        PopularityMetrics metrics,
        ContentTrends trends,
        RelatedContent relatedContent)
    {
        Id = ObjectId.GenerateNewId();
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        TargetId = targetId;
        Period = period ?? throw new ArgumentNullException(nameof(period));
        Date = date;
        Metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        Trends = trends ?? throw new ArgumentNullException(nameof(trends));
        RelatedContent = relatedContent ?? throw new ArgumentNullException(nameof(relatedContent));
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ContentPopularityCreatedEvent(Id, TargetType, TargetId, Period, Date));
    }

    // Business methods
    public bool IsTrending() => Metrics.TrendingScore > 0.7;
    public bool IsViral() => Metrics.ViralityScore > 0.8;
    public double GetEngagementRate() => Metrics.EngagementScore;
    public bool IsPopular() => Metrics.PopularityScore > 0.5;
    public List<string> GetTrendingTags() => RelatedContent.SimilarTags.Take(5).ToList();

    public void UpdateMetrics(PopularityMetrics newMetrics)
    {
        Metrics = newMetrics ?? throw new ArgumentNullException(nameof(newMetrics));
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ContentPopularityUpdatedEvent(Id, TargetType, TargetId, Metrics));
    }

    public void UpdateTrends(ContentTrends newTrends)
    {
        Trends = newTrends ?? throw new ArgumentNullException(nameof(newTrends));
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ContentTrendsUpdatedEvent(Id, TargetType, TargetId, Trends));
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    // Domain event handling
    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
```

## 🚀 Missing Features Domain Models

### 1. Content Moderation Aggregate

#### ContentModeration Entity
```csharp
public class ContentModeration : AggregateRoot<ObjectId>
{
    [BsonId]
    public ObjectId Id { get; private set; }
    
    public ObjectId ContentId { get; private set; }
    public string ContentType { get; private set; } // "collection", "media", "comment", "message"
    public ModerationStatus Status { get; private set; }
    public string ModerationReason { get; private set; }
    public List<FlaggedBy> FlaggedBy { get; private set; }
    public string ModeratedBy { get; private set; }
    public DateTime? ModeratedAt { get; private set; }
    public string ModerationNotes { get; private set; }
    public AIAnalysis AIAnalysis { get; private set; }
    public HumanReview HumanReview { get; private set; }
    public List<Appeal> Appeals { get; private set; }
    public List<ModerationAction> Actions { get; private set; }
    public ModerationStatistics Statistics { get; private set; }
    
    // Domain methods
    public bool IsPending() => Status == ModerationStatus.Pending;
    public bool IsApproved() => Status == ModerationStatus.Approved;
    public bool IsRejected() => Status == ModerationStatus.Rejected;
    
    public void FlagContent(string userId, string reason, string details)
    {
        var flaggedBy = new FlaggedBy(userId, reason, DateTime.UtcNow, details);
        FlaggedBy.Add(flaggedBy);
        Status = ModerationStatus.Flagged;
        Statistics.IncrementFlagCount();
        
        AddDomainEvent(new ContentFlaggedEvent(Id, ContentId, userId, reason));
    }
    
    public void ModerateContent(string moderatorId, ModerationStatus status, string notes)
    {
        ModeratedBy = moderatorId;
        ModeratedAt = DateTime.UtcNow;
        ModerationNotes = notes;
        Status = status;
        
        AddDomainEvent(new ContentModeratedEvent(Id, ContentId, moderatorId, status));
    }
    
    public void AppealDecision(string userId, string reason)
    {
        var appeal = new Appeal(userId, reason, DateTime.UtcNow);
        Appeals.Add(appeal);
        Statistics.IncrementAppealCount();
        
        AddDomainEvent(new ContentAppealedEvent(Id, ContentId, userId, reason));
    }
}
```

#### ModerationStatus Value Object
```csharp
public class ModerationStatus : ValueObject
{
    public static readonly ModerationStatus Pending = new("pending");
    public static readonly ModerationStatus Approved = new("approved");
    public static readonly ModerationStatus Rejected = new("rejected");
    public static readonly ModerationStatus Flagged = new("flagged");
    public static readonly ModerationStatus UnderReview = new("under_review");
    
    public string Value { get; private set; }
    
    private ModerationStatus(string value)
    {
        Value = value;
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
```

### 2. Copyright Management Aggregate

#### CopyrightManagement Entity
```csharp
public class CopyrightManagement : AggregateRoot<ObjectId>
{
    [BsonId]
    public ObjectId Id { get; private set; }
    
    public ObjectId ContentId { get; private set; }
    public string ContentType { get; private set; }
    public CopyrightStatus Status { get; private set; }
    public LicenseInfo License { get; private set; }
    public AttributionInfo Attribution { get; private set; }
    public OwnershipInfo Ownership { get; private set; }
    public DMCAInfo DMCA { get; private set; }
    public FairUseInfo FairUse { get; private set; }
    public List<Permission> Permissions { get; private set; }
    public List<Violation> Violations { get; private set; }
    
    // Domain methods
    public bool IsOriginal() => Status == CopyrightStatus.Original;
    public bool IsLicensed() => Status == CopyrightStatus.Licensed;
    public bool IsFairUse() => Status == CopyrightStatus.FairUse;
    
    public void ClaimOwnership(string userId, string verificationMethod)
    {
        Ownership = new OwnershipInfo(userId, DateTime.UtcNow, true, verificationMethod);
        Status = CopyrightStatus.Original;
        
        AddDomainEvent(new OwnershipClaimedEvent(Id, ContentId, userId));
    }
    
    public void ReportDMCA(string reporterId, string reportId)
    {
        DMCA = new DMCAInfo(true, reportId, reporterId, DateTime.UtcNow, DMCAStatus.Pending);
        
        AddDomainEvent(new DMCAReportedEvent(Id, ContentId, reporterId, reportId));
    }
    
    public void GrantPermission(string userId, string permission, DateTime? expiresAt)
    {
        var newPermission = new Permission(userId, permission, DateTime.UtcNow, expiresAt, true);
        Permissions.Add(newPermission);
        
        AddDomainEvent(new PermissionGrantedEvent(Id, ContentId, userId, permission));
    }
}
```

### 3. User Security Aggregate

#### UserSecurity Entity
```csharp
public class UserSecurity : AggregateRoot<ObjectId>
{
    [BsonId]
    public ObjectId Id { get; private set; }
    
    public string UserId { get; private set; }
    public TwoFactorInfo TwoFactor { get; private set; }
    public List<Device> Devices { get; private set; }
    public SecuritySettings SecuritySettings { get; private set; }
    public List<SecurityEvent> SecurityEvents { get; private set; }
    public List<LoginHistory> LoginHistory { get; private set; }
    public List<PasswordHistory> PasswordHistory { get; private set; }
    public List<APIKey> APIKeys { get; private set; }
    public RiskScore RiskScore { get; private set; }
    
    // Domain methods
    public bool IsTwoFactorEnabled() => TwoFactor.Enabled;
    
    public bool IsDeviceTrusted(string deviceId)
    {
        var device = Devices.FirstOrDefault(d => d.DeviceId == deviceId);
        return device?.IsTrusted ?? false;
    }
    
    public void AddDevice(Device device)
    {
        Devices.Add(device);
        RecordSecurityEvent(new SecurityEvent("device_added", DateTime.UtcNow, device.IPAddress, device.Location, device, "low"));
        
        AddDomainEvent(new DeviceAddedEvent(UserId, device.DeviceId));
    }
    
    public void RemoveDevice(string deviceId)
    {
        var device = Devices.FirstOrDefault(d => d.DeviceId == deviceId);
        if (device != null)
        {
            Devices.Remove(device);
            RecordSecurityEvent(new SecurityEvent("device_removed", DateTime.UtcNow, device.IPAddress, device.Location, device, "low"));
            
            AddDomainEvent(new DeviceRemovedEvent(UserId, deviceId));
        }
    }
    
    public void RecordSecurityEvent(SecurityEvent securityEvent)
    {
        SecurityEvents.Add(securityEvent);
        
        // Update risk score based on event
        UpdateRiskScore(securityEvent);
    }
    
    public void UpdateRiskScore(SecurityEvent securityEvent)
    {
        var riskFactors = new List<RiskFactor>();
        
        // Analyze security event and calculate risk
        if (securityEvent.Type == "failed_login")
        {
            riskFactors.Add(new RiskFactor("failed_login", 20, 0.3, "Multiple failed login attempts"));
        }
        else if (securityEvent.Type == "suspicious_activity")
        {
            riskFactors.Add(new RiskFactor("suspicious_activity", 40, 0.5, "Suspicious activity detected"));
        }
        
        RiskScore = new RiskScore(CalculateRiskScore(riskFactors), riskFactors, DateTime.UtcNow);
    }
    
    public bool IsIPWhitelisted(string ip)
    {
        return SecuritySettings.IPWhitelist.Contains(ip);
    }
    
    public bool IsLocationAllowed(string country)
    {
        return !SecuritySettings.GeolocationRestrictions.Contains(country);
    }
    
    private int CalculateRiskScore(List<RiskFactor> factors)
    {
        return (int)factors.Sum(f => f.Score * f.Weight);
    }
}
```

### 4. System Health Aggregate

#### SystemHealth Entity
```csharp
public class SystemHealth : AggregateRoot<ObjectId>
{
    [BsonId]
    public ObjectId Id { get; private set; }
    
    public DateTime Timestamp { get; private set; }
    public string Component { get; private set; } // "database", "storage", "api", "worker"
    public HealthStatus Status { get; private set; }
    public HealthMetrics Metrics { get; private set; }
    public PerformanceMetrics Performance { get; private set; }
    public List<HealthAlert> Alerts { get; private set; }
    public List<HealthAction> Actions { get; private set; }
    public EnvironmentInfo Environment { get; private set; }
    public List<DependencyHealth> Dependencies { get; private set; }
    
    // Domain methods
    public bool IsHealthy() => Status == HealthStatus.Healthy;
    public bool IsWarning() => Status == HealthStatus.Warning;
    public bool IsCritical() => Status == HealthStatus.Critical;
    public bool IsDown() => Status == HealthStatus.Down;
    
    public void AddAlert(HealthAlert alert)
    {
        Alerts.Add(alert);
        
        // Update status based on alert severity
        if (alert.Severity == "critical" && Status != HealthStatus.Critical)
        {
            Status = HealthStatus.Critical;
        }
        else if (alert.Severity == "warning" && Status == HealthStatus.Healthy)
        {
            Status = HealthStatus.Warning;
        }
        
        AddDomainEvent(new HealthAlertAddedEvent(Component, alert));
    }
    
    public void ResolveAlert(string alertId)
    {
        var alert = Alerts.FirstOrDefault(a => a.Id == alertId);
        if (alert != null)
        {
            alert.Resolve();
            
            // Check if we can improve status
            if (Status == HealthStatus.Critical && !Alerts.Any(a => a.Severity == "critical" && !a.Resolved))
            {
                Status = HealthStatus.Warning;
            }
            else if (Status == HealthStatus.Warning && !Alerts.Any(a => !a.Resolved))
            {
                Status = HealthStatus.Healthy;
            }
            
            AddDomainEvent(new HealthAlertResolvedEvent(Component, alertId));
        }
    }
    
    public void RecordAction(HealthAction action)
    {
        Actions.Add(action);
        
        AddDomainEvent(new HealthActionRecordedEvent(Component, action));
    }
    
    public void UpdateMetrics(HealthMetrics metrics)
    {
        Metrics = metrics;
        
        // Analyze metrics and determine health status
        var newStatus = AnalyzeHealthStatus(metrics);
        if (newStatus != Status)
        {
            Status = newStatus;
            AddDomainEvent(new HealthStatusChangedEvent(Component, Status, metrics));
        }
    }
    
    private HealthStatus AnalyzeHealthStatus(HealthMetrics metrics)
    {
        // CPU usage analysis
        if (metrics.CPU.Usage > 90)
            return HealthStatus.Critical;
        else if (metrics.CPU.Usage > 80)
            return HealthStatus.Warning;
        
        // Memory usage analysis
        if (metrics.Memory.Usage > 95)
            return HealthStatus.Critical;
        else if (metrics.Memory.Usage > 85)
            return HealthStatus.Warning;
        
        // Disk usage analysis
        if (metrics.Disk.Usage > 95)
            return HealthStatus.Critical;
        else if (metrics.Disk.Usage > 85)
            return HealthStatus.Warning;
        
        return HealthStatus.Healthy;
    }
}
```

### 5. Notification Template Aggregate

#### NotificationTemplate Entity
```csharp
public class NotificationTemplate : AggregateRoot<ObjectId>
{
    [BsonId]
    public ObjectId Id { get; private set; }
    
    public string TemplateId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public NotificationType Type { get; private set; }
    public string Category { get; private set; }
    public string Language { get; private set; }
    public string Subject { get; private set; }
    public string Content { get; private set; }
    public string HtmlContent { get; private set; }
    public List<TemplateVariable> Variables { get; private set; }
    public TemplateStyling Styling { get; private set; }
    public List<TemplateCondition> Conditions { get; private set; }
    public TemplateScheduling Scheduling { get; private set; }
    public TemplateDelivery Delivery { get; private set; }
    public TemplateCompliance Compliance { get; private set; }
    public TemplateAnalytics Analytics { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDefault { get; private set; }
    public int Version { get; private set; }
    
    // Domain methods
    public bool IsActiveTemplate() => IsActive;
    public bool IsDefaultTemplate() => IsDefault;
    
    public void Activate()
    {
        IsActive = true;
        AddDomainEvent(new TemplateActivatedEvent(TemplateId));
    }
    
    public void Deactivate()
    {
        IsActive = false;
        AddDomainEvent(new TemplateDeactivatedEvent(TemplateId));
    }
    
    public void SetAsDefault()
    {
        IsDefault = true;
        AddDomainEvent(new TemplateSetAsDefaultEvent(TemplateId));
    }
    
    public void UpdateVersion(int newVersion)
    {
        Version = newVersion;
        AddDomainEvent(new TemplateVersionUpdatedEvent(TemplateId, newVersion));
    }
    
    public bool MatchesConditions(Dictionary<string, object> context)
    {
        return Conditions.All(condition => condition.Evaluate(context));
    }
    
    public string RenderContent(Dictionary<string, object> variables)
    {
        var renderedContent = Content;
        
        foreach (var variable in Variables)
        {
            if (variables.ContainsKey(variable.Name))
            {
                var value = variables[variable.Name]?.ToString() ?? variable.DefaultValue;
                renderedContent = renderedContent.Replace($"{{{variable.Name}}}", value);
            }
        }
        
        return renderedContent;
    }
}
```

### 6. File Version Aggregate

#### FileVersion Entity
```csharp
public class FileVersion : AggregateRoot<ObjectId>
{
    [BsonId]
    public ObjectId Id { get; private set; }
    
    public ObjectId FileId { get; private set; }
    public int Version { get; private set; }
    public string VersionName { get; private set; }
    public string Changes { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public long FileSize { get; private set; }
    public string FileHash { get; private set; }
    public ObjectId StorageLocation { get; private set; }
    public string Path { get; private set; }
    public string Url { get; private set; }
    public FileMetadata Metadata { get; private set; }
    public VersionDiff Diff { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public VersionRetention Retention { get; private set; }
    public VersionAccess Access { get; private set; }
    public VersionStatistics Statistics { get; private set; }
    
    // Domain methods
    public bool IsCurrentVersion() => IsActive;
    public bool IsDeletedVersion() => IsDeleted;
    
    public void Activate()
    {
        IsActive = true;
        AddDomainEvent(new FileVersionActivatedEvent(FileId, Version));
    }
    
    public void Deactivate()
    {
        IsActive = false;
        AddDomainEvent(new FileVersionDeactivatedEvent(FileId, Version));
    }
    
    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        AddDomainEvent(new FileVersionDeletedEvent(FileId, Version));
    }
    
    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        AddDomainEvent(new FileVersionRestoredEvent(FileId, Version));
    }
    
    public bool ShouldRetain()
    {
        if (Retention.Policy == "keep_all")
            return true;
        else if (Retention.Policy == "keep_latest")
            return IsActive;
        else if (Retention.Policy == "keep_scheduled")
            return DateTime.UtcNow < Retention.KeepUntil;
        
        return false;
    }
    
    public bool CanAccess(string userId)
    {
        if (Access.Public)
            return true;
        
        var permission = Access.Permissions.FirstOrDefault(p => p.UserId == userId);
        return permission != null && permission.Permission == "read" && 
               (permission.ExpiresAt == null || permission.ExpiresAt > DateTime.UtcNow);
    }
    
    public void RecordDownload(string userId)
    {
        Statistics.IncrementDownloadCount();
        Statistics.UpdateLastDownloaded(DateTime.UtcNow);
        
        AddDomainEvent(new FileVersionDownloadedEvent(FileId, Version, userId));
    }
    
    public void RecordView(string userId)
    {
        Statistics.IncrementViewCount();
        Statistics.UpdateLastViewed(DateTime.UtcNow);
        
        AddDomainEvent(new FileVersionViewedEvent(FileId, Version, userId));
    }
}
```

### 7. User Group Aggregate

#### UserGroup Entity
```csharp
public class UserGroup : AggregateRoot<ObjectId>
{
    [BsonId]
    public ObjectId Id { get; private set; }
    
    public string GroupId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public GroupType Type { get; private set; }
    public string Category { get; private set; }
    public List<GroupMember> Members { get; private set; }
    public List<string> Permissions { get; private set; }
    public GroupSettings Settings { get; private set; }
    public GroupContent Content { get; private set; }
    public GroupStatistics Statistics { get; private set; }
    public GroupModeration Moderation { get; private set; }
    public GroupNotifications Notifications { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime LastActivity { get; private set; }
    
    // Domain methods
    public bool IsPublic() => Type == GroupType.Public;
    public bool IsPrivate() => Type == GroupType.Private;
    public bool IsInviteOnly() => Type == GroupType.InviteOnly;
    
    public void AddMember(string userId, string role)
    {
        if (Members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("User is already a member");
        
        var member = new GroupMember(userId, role, DateTime.UtcNow, CreatedBy, "active");
        Members.Add(member);
        Statistics.IncrementMemberCount();
        UpdateLastActivity();
        
        AddDomainEvent(new UserAddedToGroupEvent(GroupId, userId, role));
    }
    
    public void RemoveMember(string userId)
    {
        var member = Members.FirstOrDefault(m => m.UserId == userId);
        if (member != null)
        {
            Members.Remove(member);
            Statistics.DecrementMemberCount();
            UpdateLastActivity();
            
            AddDomainEvent(new UserRemovedFromGroupEvent(GroupId, userId));
        }
    }
    
    public void UpdateMemberRole(string userId, string newRole)
    {
        var member = Members.FirstOrDefault(m => m.UserId == userId);
        if (member != null)
        {
            var oldRole = member.Role;
            member.UpdateRole(newRole);
            UpdateLastActivity();
            
            AddDomainEvent(new UserRoleUpdatedEvent(GroupId, userId, oldRole, newRole));
        }
    }
    
    public bool IsMember(string userId)
    {
        return Members.Any(m => m.UserId == userId && m.Status == "active");
    }
    
    public bool IsModerator(string userId)
    {
        var member = Members.FirstOrDefault(m => m.UserId == userId);
        return member?.Role == "moderator" || member?.Role == "admin";
    }
    
    public bool IsAdmin(string userId)
    {
        var member = Members.FirstOrDefault(m => m.UserId == userId);
        return member?.Role == "admin";
    }
    
    public void BanUser(string userId, string reason)
    {
        var member = Members.FirstOrDefault(m => m.UserId == userId);
        if (member != null)
        {
            member.Ban(reason);
            Moderation.AddBannedUser(userId);
            UpdateLastActivity();
            
            AddDomainEvent(new UserBannedFromGroupEvent(GroupId, userId, reason));
        }
    }
    
    public void UnbanUser(string userId)
    {
        var member = Members.FirstOrDefault(m => m.UserId == userId);
        if (member != null)
        {
            member.Unban();
            Moderation.RemoveBannedUser(userId);
            UpdateLastActivity();
            
            AddDomainEvent(new UserUnbannedFromGroupEvent(GroupId, userId));
        }
    }
    
    public void UpdateSettings(GroupSettings newSettings)
    {
        Settings = newSettings;
        UpdatedAt = DateTime.UtcNow;
        UpdateLastActivity();
        
        AddDomainEvent(new GroupSettingsUpdatedEvent(GroupId, newSettings));
    }
    
    private void UpdateLastActivity()
    {
        LastActivity = DateTime.UtcNow;
    }
}
```

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
- **Enterprise Features**: Support cho các tính năng enterprise-level
- **Security**: Built-in security và compliance features
- **Analytics**: Comprehensive tracking và monitoring
