# Image Viewer System - .NET 8 Architecture Design

## Tổng quan kiến trúc mới

### Kiến trúc tổng thể
```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
├─────────────────────────────────────────────────────────────┤
│  Blazor Server/WebAssembly  │  Progressive Web App        │
│  - Image Grid Component     │  - Offline Support          │
│  - Image Viewer Component   │  - Push Notifications       │
│  - Collection Management    │  - Mobile Optimization      │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    API Gateway Layer                       │
├─────────────────────────────────────────────────────────────┤
│  ASP.NET Core Web API                                      │
│  - Authentication/Authorization                            │
│  - Rate Limiting                                           │
│  - Request/Response Logging                                │
│  - API Versioning                                          │
│  - Swagger/OpenAPI Documentation                           │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                       │
├─────────────────────────────────────────────────────────────┤
│  CQRS + MediatR                                            │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │   Commands  │  │   Queries   │  │   Events    │        │
│  │             │  │             │  │             │        │
│  │ - Create    │  │ - Get       │  │ - Created   │        │
│  │ - Update    │  │ - Search    │  │ - Updated   │        │
│  │ - Delete    │  │ - List      │  │ - Deleted   │        │
│  │ - Process   │  │ - Count     │  │ - Processed │        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Domain Layer                            │
├─────────────────────────────────────────────────────────────┤
│  Domain Models & Business Logic                            │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │ Libraries   │  │ Collections │  │ Media Items │        │
│  │             │  │             │  │             │        │
│  │ - Entity    │  │ - Entity    │  │ - Entity    │        │
│  │ - Services  │  │ - Services  │  │ - Services  │        │
│  │ - Rules     │  │ - Rules     │  │ - Rules     │        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │ Analytics   │  │ Settings    │  │ Background  │        │
│  │             │  │             │  │ Jobs        │        │
│  │ - Tracking  │  │ - System    │  │ - Processing│        │
│  │ - Metrics   │  │ - User      │  │ - Queuing   │        │
│  │ - Reports   │  │ - Validation│  │ - Monitoring│        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                     │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │   Database  │  │    Cache    │  │   Storage   │        │
│  │             │  │             │  │             │        │
│  │ - MongoDB   │  │ - Redis     │  │ - File      │        │
│  │ - Driver    │  │ - Memory    │  │ - Blob      │        │
│  │ - GridFS    │  │ - Distributed│  │ - CDN      │        │
│  │ - Analytics │  │ - Analytics │  │ - Monitoring│        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │   Background│  │   External │  │   Logging   │        │
│  │   Services  │  │   Services  │  │             │        │
│  │             │  │             │  │             │        │
│  │ - RabbitMQ  │  │ - Image     │  │ - Serilog   │        │
│  │ - Worker    │  │   Processing│  │ - ELK Stack │        │
│  │ - Analytics │  │ - AI/ML     │  │ - Audit Logs│        │
│  │ - Monitoring│  │ - Analytics │  │ - Error Logs│        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
└─────────────────────────────────────────────────────────────┘
```

## Domain Models

### Library Entity
```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Library : BaseEntity
{
    [BsonId]
    public ObjectId Id { get; set; }
    
    public string Name { get; set; }
    public string Path { get; set; }
    public string Type { get; set; } // "local", "network", "cloud"
    public LibrarySettings Settings { get; set; }
    public LibraryMetadata Metadata { get; set; }
    public LibraryStatistics Statistics { get; set; }
    public WatchInfo WatchInfo { get; set; }
    public SearchIndex SearchIndex { get; set; }
    
    // Domain methods
    public bool NeedsScan() => Statistics.LastScanDate < DateTime.UtcNow.AddHours(-24);
    public void UpdateStatistics(LibraryStatistics stats) => Statistics = stats;
    public void AddTag(string tag) => Metadata.Tags.Add(tag);
    public bool IsWatching() => WatchInfo.IsWatching;
}
```

### Collection Entity
```csharp
public class Collection : BaseEntity
{
    [BsonId]
    public ObjectId Id { get; set; }
    
    public ObjectId LibraryId { get; set; }
    public string Name { get; set; }
    public string Path { get; set; }
    public string Type { get; set; } // "image", "video", "mixed"
    public CollectionSettings Settings { get; set; }
    public CollectionMetadata Metadata { get; set; }
    public CollectionStatistics Statistics { get; set; }
    public CacheInfo CacheInfo { get; set; }
    public WatchInfo WatchInfo { get; set; }
    public SearchIndex SearchIndex { get; set; }
    
    // Domain methods
    public bool NeedsScan() => Statistics.LastFileSystemCheck < DateTime.UtcNow.AddHours(-1);
    public void UpdateStatistics(CollectionStatistics stats) => Statistics = stats;
    public void AddTag(string tag) => Metadata.Tags.Add(tag);
    public bool IsWatching() => WatchInfo.IsWatching;
    public bool HasCache() => CacheInfo.Enabled;
}

public enum CollectionType
{
    Folder,
    Zip,
    SevenZip,
    Rar,
    Tar
}
```

### Media Item Entity
```csharp
public class MediaItem : BaseEntity
{
    [BsonId]
    public ObjectId Id { get; set; }
    
    public ObjectId CollectionId { get; set; }
    public ObjectId LibraryId { get; set; }
    public string Filename { get; set; }
    public string FilePath { get; set; }
    public string FileType { get; set; } // "image", "video"
    public string MimeType { get; set; }
    public long FileSize { get; set; }
    public Dimensions Dimensions { get; set; }
    public FileInfo FileInfo { get; set; }
    public MediaMetadata Metadata { get; set; }
    public List<string> Tags { get; set; }
    public CacheInfo CacheInfo { get; set; }
    public MediaStatistics Statistics { get; set; }
    public SearchIndex SearchIndex { get; set; }
    
    // Domain methods
    public bool IsCached() => CacheInfo != null && CacheInfo.Status == "generated";
    public string GetThumbnailPath() => CacheInfo?.ThumbnailPath;
    public void UpdateCache(CacheInfo cacheInfo) => CacheInfo = cacheInfo;
    public bool NeedsCacheRegeneration() => CacheInfo?.NeedsRegeneration == true;
    public bool FileExists() => FileInfo?.Exists == true;
}

public class Dimensions
{
    public int Width { get; set; }
    public int Height { get; set; }
}

public class FileInfo
{
    public DateTime LastModified { get; set; }
    public string FileHash { get; set; }
    public string FileSystemHash { get; set; }
    public bool Exists { get; set; }
    public DateTime LastChecked { get; set; }
}

public class MediaMetadata
{
    // Image metadata
    public string Camera { get; set; }
    public string Lens { get; set; }
    public string Exposure { get; set; }
    public int Iso { get; set; }
    public string Aperture { get; set; }
    public string FocalLength { get; set; }
    public DateTime? DateTaken { get; set; }
    public GpsData Gps { get; set; }
    
    // Video metadata
    public double Duration { get; set; }
    public double FrameRate { get; set; }
    public long Bitrate { get; set; }
    public string Codec { get; set; }
}

public class GpsData
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
}
```

### Cache Info Entity
```csharp
public class CacheInfo
{
    public bool Enabled { get; set; }
    public string FolderPath { get; set; }
    public DateTime LastRebuild { get; set; }
    public string RebuildStatus { get; set; } // "idle", "running", "completed", "failed"
    public int Progress { get; set; }
    public string ThumbnailPath { get; set; }
    public string PreviewPath { get; set; }
    public string FullSizePath { get; set; }
    public DateTime LastGenerated { get; set; }
    public string Status { get; set; } // "generated", "generating", "failed"
    public bool NeedsRegeneration { get; set; }
    public string CacheHash { get; set; }
}

### Cache Folder Entity
```csharp
public class CacheFolder
{
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public string Path { get; set; }
    public string Type { get; set; } // "thumbnail", "preview", "full"
    public CacheFolderSettings Settings { get; set; }
    public CacheFolderStatistics Statistics { get; set; }
    public CacheFolderStatus Status { get; set; }
    
    // Domain methods
    public bool HasSpace(long requiredSize) => Statistics.TotalSize + requiredSize <= Settings.MaxSize;
    public void UpdateUsage(long sizeDelta, int fileCountDelta)
    {
        Statistics.TotalSize += sizeDelta;
        Statistics.TotalFiles += fileCountDelta;
    }
    public bool IsActive() => Status.IsActive;
}

public class CacheFolderSettings
{
    public long MaxSize { get; set; }
    public int CompressionQuality { get; set; }
    public string Format { get; set; } // "jpg", "png", "webp"
    public bool AutoCleanup { get; set; }
    public int CleanupDays { get; set; }
}

public class CacheFolderStatistics
{
    public int TotalFiles { get; set; }
    public long TotalSize { get; set; }
    public DateTime LastCleanup { get; set; }
    public DateTime LastRebuild { get; set; }
}

public class CacheFolderStatus
{
    public bool IsActive { get; set; }
    public string LastError { get; set; }
    public DateTime LastErrorDate { get; set; }
}
```

### System Settings Entity
```csharp
public class SystemSetting
{
    public ObjectId Id { get; set; }
    public string Key { get; set; }
    public object Value { get; set; }
    public string Type { get; set; } // "boolean", "string", "number", "object", "array"
    public string Category { get; set; } // "sync", "cache", "performance", "security", "backup"
    public string Description { get; set; }
    public object DefaultValue { get; set; }
    public SettingValidation Validation { get; set; }
    public SettingMetadata Metadata { get; set; }
    
    // Domain methods
    public T GetValue<T>() => (T)Value;
    public void SetValue<T>(T value) => Value = value;
    public bool IsReadOnly() => Metadata.IsReadOnly;
    public bool IsAdvanced() => Metadata.IsAdvanced;
}

public class SettingValidation
{
    public double? Min { get; set; }
    public double? Max { get; set; }
    public List<object> AllowedValues { get; set; }
    public bool Required { get; set; }
}

public class SettingMetadata
{
    public string Version { get; set; }
    public string LastModifiedBy { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsAdvanced { get; set; }
}
```

### User Settings Entity
```csharp
public class UserSettings
{
    public ObjectId Id { get; set; }
    public string UserId { get; set; }
    public UserPreferences Preferences { get; set; }
    public DisplaySettings DisplaySettings { get; set; }
    public NavigationSettings NavigationSettings { get; set; }
    public SearchSettings SearchSettings { get; set; }
    public FavoriteListSettings FavoriteListSettings { get; set; }
    public NotificationSettings NotificationSettings { get; set; }
    public PrivacySettings PrivacySettings { get; set; }
    
    // Domain methods
    public void UpdatePreference<T>(string key, T value) => Preferences[key] = value;
    public T GetPreference<T>(string key) => (T)Preferences[key];
}

public class UserPreferences
{
    public string Theme { get; set; } = "light";
    public string Language { get; set; } = "en";
    public string DefaultView { get; set; } = "grid";
    public int ItemsPerPage { get; set; } = 20;
    public int ThumbnailSize { get; set; } = 150;
    public bool ShowMetadata { get; set; } = true;
    public bool ShowFileInfo { get; set; } = true;
    public bool AutoPlayVideos { get; set; } = false;
    public double VideoVolume { get; set; } = 0.5;
}

public class DisplaySettings
{
    public int GridColumns { get; set; } = 4;
    public int ListItemHeight { get; set; } = 60;
    public bool ShowThumbnails { get; set; } = true;
    public string ThumbnailQuality { get; set; } = "medium";
    public bool ShowFileNames { get; set; } = true;
    public bool ShowFileSizes { get; set; } = true;
    public bool ShowDates { get; set; } = true;
    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public string TimeFormat { get; set; } = "HH:mm";
}

public class NavigationSettings
{
    public bool EnableKeyboardShortcuts { get; set; } = true;
    public bool EnableMouseGestures { get; set; } = true;
    public string DefaultSortField { get; set; } = "filename";
    public string DefaultSortDirection { get; set; } = "asc";
    public bool RememberLastPosition { get; set; } = true;
    public bool AutoAdvance { get; set; } = false;
    public int AutoAdvanceDelay { get; set; } = 5;
}

public class SearchSettings
{
    public List<string> DefaultSearchFields { get; set; } = new() { "filename", "tags", "metadata" };
    public List<string> SearchHistory { get; set; } = new();
    public List<SavedSearch> SavedSearches { get; set; } = new();
    public bool SearchSuggestions { get; set; } = true;
    public bool HighlightResults { get; set; } = true;
}

public class SavedSearch
{
    public string Name { get; set; }
    public object Query { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FavoriteListSettings
{
    public string DefaultListType { get; set; } = "manual";
    public bool AutoCreateLists { get; set; } = false;
    public int MaxListsPerUser { get; set; } = 50;
    public DefaultListSettings DefaultListSettings { get; set; } = new();
}

public class DefaultListSettings
{
    public bool IsPublic { get; set; } = false;
    public bool AllowDuplicates { get; set; } = false;
    public int MaxItems { get; set; } = 1000;
}

public class NotificationSettings
{
    public bool EnableNotifications { get; set; } = true;
    public bool NotifyOnScanComplete { get; set; } = true;
    public bool NotifyOnCacheComplete { get; set; } = true;
    public bool NotifyOnSmartListUpdate { get; set; } = true;
    public bool SoundEnabled { get; set; } = true;
}

public class PrivacySettings
{
    public bool ShareUsageData { get; set; } = false;
    public bool ShareErrorReports { get; set; } = true;
    public bool RememberSearchHistory { get; set; } = true;
    public bool RememberViewHistory { get; set; } = true;
}
```

### Favorite List Entity
```csharp
public class FavoriteList
{
    public ObjectId Id { get; set; }
    public string UserId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Type { get; set; } // "manual", "smart", "auto"
    public FavoriteListSettings Settings { get; set; }
    public List<FavoriteListItem> Items { get; set; }
    public SmartFilters SmartFilters { get; set; }
    public FavoriteListStatistics Statistics { get; set; }
    public FavoriteListMetadata Metadata { get; set; }
    public SearchIndex SearchIndex { get; set; }
    
    // Domain methods
    public void AddItem(ObjectId mediaId, ObjectId collectionId, ObjectId libraryId)
    {
        Items.Add(new FavoriteListItem
        {
            MediaId = mediaId,
            CollectionId = collectionId,
            LibraryId = libraryId,
            AddedAt = DateTime.UtcNow,
            AddedBy = "user"
        });
        Statistics.TotalItems = Items.Count;
    }
    
    public void RemoveItem(ObjectId mediaId)
    {
        Items.RemoveAll(item => item.MediaId == mediaId);
        Statistics.TotalItems = Items.Count;
    }
    
    public bool IsSmart() => Type == "smart";
    public bool IsPublic() => Settings.IsPublic;
    public bool HasSpace() => Items.Count < Settings.MaxItems;
}

public class FavoriteListItem
{
    public ObjectId MediaId { get; set; }
    public ObjectId CollectionId { get; set; }
    public ObjectId LibraryId { get; set; }
    public DateTime AddedAt { get; set; }
    public string AddedBy { get; set; } // "user", "auto", "smart"
    public string Notes { get; set; }
    public List<string> Tags { get; set; }
    public int CustomOrder { get; set; }
}

public class SmartFilters
{
    public bool Enabled { get; set; }
    public List<FilterRule> Rules { get; set; }
    public bool AutoUpdate { get; set; }
    public DateTime LastUpdate { get; set; }
    public int UpdateInterval { get; set; } // minutes
}

public class FilterRule
{
    public string Field { get; set; } // "tags", "fileType", "rating", "viewCount", "dateRange"
    public string Operator { get; set; } // "equals", "contains", "greaterThan", "lessThan", "between"
    public object Value { get; set; }
    public string Logic { get; set; } // "AND", "OR"
}

public class FavoriteListStatistics
{
    public int TotalItems { get; set; }
    public long TotalSize { get; set; }
    public DateTime LastAccessed { get; set; }
    public int AccessCount { get; set; }
    public double AverageRating { get; set; }
    public ObjectId MostViewedItem { get; set; }
}

public class FavoriteListMetadata
{
    public List<string> Tags { get; set; }
    public string Category { get; set; }
    public string Color { get; set; }
    public string Icon { get; set; }
}
```

### Background Job Entity
```csharp
public class BackgroundJob
{
    public ObjectId Id { get; set; }
    public string Type { get; set; } // "scan", "thumbnail", "cache", "rebuild", "watch", "sync"
    public string Status { get; set; } // "pending", "running", "completed", "failed", "cancelled"
    public int Priority { get; set; }
    public JobProgress Progress { get; set; }
    public JobTarget Target { get; set; }
    public JobParameters Parameters { get; set; }
    public JobResult Result { get; set; }
    public JobTiming Timing { get; set; }
    public JobRetry Retry { get; set; }
    public JobPerformance Performance { get; set; }

    // Domain methods
    public void Start()
    {
        Status = "running";
        Timing.StartedAt = DateTime.UtcNow;
    }

    public void Complete(JobResult result)
    {
        Status = "completed";
        Result = result;
        Timing.CompletedAt = DateTime.UtcNow;
        Timing.Duration = (Timing.CompletedAt - Timing.StartedAt).Value.TotalMilliseconds;
    }

    public void Fail(string error)
    {
        Status = "failed";
        Result = new JobResult { Success = false, Message = error };
        Timing.CompletedAt = DateTime.UtcNow;
    }

    public bool CanRetry() => Retry.Count < Retry.MaxRetries;
    public void IncrementRetry() => Retry.Count++;
}

### Analytics Entities

#### User Behavior Event Entity
```csharp
public class UserBehaviorEvent
{
    public ObjectId Id { get; set; }
    public string UserId { get; set; }
    public string SessionId { get; set; }
    public string EventType { get; set; } // "view", "search", "filter", "navigate", "download", "share", "like", "favorite"
    public string TargetType { get; set; } // "media", "collection", "library", "favorite_list", "tag"
    public ObjectId TargetId { get; set; }
    public EventMetadata Metadata { get; set; }
    public EventContext Context { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime CreatedAt { get; set; }

    // Domain methods
    public bool IsViewEvent() => EventType == "view";
    public bool IsSearchEvent() => EventType == "search";
    public bool IsInteractionEvent() => new[] { "like", "share", "download", "favorite" }.Contains(EventType);
    public double GetDuration() => Metadata.Duration;
    public string GetDeviceType() => Context.Device;
}
```

#### User Analytics Entity
```csharp
public class UserAnalytics
{
    public ObjectId Id { get; set; }
    public string UserId { get; set; }
    public string Period { get; set; } // "daily", "weekly", "monthly", "yearly"
    public DateTime Date { get; set; }
    public UserMetrics Metrics { get; set; }
    public TopContent TopContent { get; set; }
    public UserPreferences Preferences { get; set; }
    public UserDemographics Demographics { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Domain methods
    public bool IsActiveUser() => Metrics.TotalViews > 0 || Metrics.TotalSearches > 0;
    public double GetEngagementScore() => (Metrics.TotalLikes + Metrics.TotalShares + Metrics.TotalFavorites) / Math.Max(Metrics.TotalViews, 1);
    public List<string> GetTopTags() => TopContent.MostUsedTags.Take(10).Select(t => t.Tag).ToList();
    public string GetUserSegment() => GetUserSegmentByActivity();
    
    private string GetUserSegmentByActivity()
    {
        if (Metrics.TotalViews > 1000) return "power_user";
        if (Metrics.TotalViews > 100) return "active_user";
        if (Metrics.TotalViews > 10) return "regular_user";
        return "casual_user";
    }
}
```

#### Content Popularity Entity
```csharp
public class ContentPopularity
{
    public ObjectId Id { get; set; }
    public string TargetType { get; set; } // "media", "collection", "library", "tag"
    public ObjectId TargetId { get; set; }
    public string Period { get; set; } // "daily", "weekly", "monthly", "yearly", "all_time"
    public DateTime Date { get; set; }
    public PopularityMetrics Metrics { get; set; }
    public ContentTrends Trends { get; set; }
    public RelatedContent RelatedContent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Domain methods
    public bool IsTrending() => Metrics.TrendingScore > 0.7;
    public bool IsViral() => Metrics.ViralityScore > 0.8;
    public double GetEngagementRate() => Metrics.EngagementScore;
    public bool IsPopular() => Metrics.PopularityScore > 0.5;
    public List<string> GetTrendingTags() => RelatedContent.SimilarTags.Take(5).ToList();
}
```

#### Search Analytics Entity
```csharp
public class SearchAnalytics
{
    public ObjectId Id { get; set; }
    public string UserId { get; set; }
    public string SessionId { get; set; }
    public string Query { get; set; }
    public string QueryHash { get; set; } // for anonymization
    public SearchFilters Filters { get; set; }
    public int ResultCount { get; set; }
    public long SearchTime { get; set; } // milliseconds
    public List<SearchClick> ClickedResults { get; set; }
    public List<string> SearchPath { get; set; } // navigation path after search
    public bool SearchSuccess { get; set; }
    public int? SearchSatisfaction { get; set; } // 1-5 rating
    public DateTime Timestamp { get; set; }
    public SearchContext Context { get; set; }
    public DateTime CreatedAt { get; set; }

    // Domain methods
    public double GetClickThroughRate() => ClickedResults.Count / Math.Max(ResultCount, 1);
    public double GetAverageTimeToClick() => ClickedResults.Any() ? ClickedResults.Average(c => c.TimeToClick) : 0;
    public bool IsSuccessfulSearch() => SearchSuccess && (SearchSatisfaction ?? 0) >= 3;
    public string GetDeviceType() => Context.Device;
    public bool HasResults() => ResultCount > 0;
}
```

public class JobProgress
{
    public int Current { get; set; }
    public int Total { get; set; }
    public int Percentage { get; set; }
    public string Message { get; set; }
    public string CurrentItem { get; set; }
    public int EstimatedTimeRemaining { get; set; }
}

public class JobTarget
{
    public ObjectId LibraryId { get; set; }
    public ObjectId CollectionId { get; set; }
    public ObjectId MediaId { get; set; }
    public ObjectId CacheFolderId { get; set; }
    public string Path { get; set; }
}

public class JobParameters
{
    public string ScanMode { get; set; } // "full", "incremental", "quick"
    public bool ForceRegenerate { get; set; }
    public bool SkipExisting { get; set; }
    public int BatchSize { get; set; }
}

public class JobResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public object Data { get; set; }
    public int FilesProcessed { get; set; }
    public int FilesSkipped { get; set; }
    public int FilesFailed { get; set; }
    public List<JobError> Errors { get; set; }
}

public class JobError
{
    public string File { get; set; }
    public string Error { get; set; }
    public DateTime Timestamp { get; set; }
}

public class JobTiming
{
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double Duration { get; set; }
    public double EstimatedDuration { get; set; }
}

public class JobRetry
{
    public int Count { get; set; }
    public int MaxRetries { get; set; }
    public DateTime NextRetry { get; set; }
    public string RetryReason { get; set; }
}

public class JobPerformance
{
    public double ItemsPerSecond { get; set; }
    public double AverageProcessingTime { get; set; }
    public long MemoryUsage { get; set; }
    public double CpuUsage { get; set; }
}
```

## Application Services

### Library Service
```csharp
public interface ILibraryService
{
    Task<Library> CreateLibraryAsync(CreateLibraryCommand command);
    Task<Library> UpdateLibraryAsync(UpdateLibraryCommand command);
    Task DeleteLibraryAsync(ObjectId libraryId);
    Task<Library> GetLibraryByIdAsync(ObjectId libraryId);
    Task<List<Library>> GetLibrariesAsync(GetLibrariesQuery query);
    Task<List<Library>> SearchLibrariesAsync(SearchLibrariesQuery query);
    Task<LibraryStatistics> GetLibraryStatisticsAsync(ObjectId libraryId);
    Task StartLibraryScanAsync(ObjectId libraryId);
    Task StopLibraryScanAsync(ObjectId libraryId);
    Task EnableLibraryWatchingAsync(ObjectId libraryId);
    Task DisableLibraryWatchingAsync(ObjectId libraryId);
}

public class LibraryService : ILibraryService
{
    private readonly ILibraryRepository _libraryRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly IMessageQueueService _messageQueueService;
    private readonly ILogger<LibraryService> _logger;

    public async Task<Library> CreateLibraryAsync(CreateLibraryCommand command)
    {
        var library = new Library
        {
            Name = command.Name,
            Path = command.Path,
            Type = command.Type,
            Settings = command.Settings,
            Metadata = new LibraryMetadata
            {
                Description = command.Description,
                Tags = command.Tags,
                CreatedDate = DateTime.UtcNow
            },
            Statistics = new LibraryStatistics(),
            WatchInfo = new WatchInfo { IsWatching = false },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _libraryRepository.AddAsync(library);
        
        // Queue initial scan
        await _messageQueueService.PublishAsync("library.scan", new
        {
            LibraryId = library.Id,
            ScanMode = "full"
        });

        return library;
    }
}
```

### Collection Service
```csharp
public interface ICollectionService
{
    Task<Collection> GetByIdAsync(ObjectId id);
    Task<List<Collection>> GetByLibraryIdAsync(ObjectId libraryId);
    Task<PagedResult<Collection>> GetPagedAsync(GetCollectionsQuery query);
    Task<Collection> CreateAsync(CreateCollectionCommand command);
    Task<Collection> UpdateAsync(UpdateCollectionCommand command);
    Task DeleteAsync(DeleteCollectionCommand command);
    Task<Collection> ScanAsync(ScanCollectionCommand command);
    Task<CollectionStatistics> GetStatisticsAsync(string id);
}

public class CollectionService : ICollectionService
{
    private readonly ICollectionRepository _repository;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<CollectionService> _logger;
    
    public async Task<Collection> CreateAsync(CreateCollectionCommand command)
    {
        var collection = new Collection(command.Name, command.Path, command.Type);
        
        // Validate collection path
        if (!await _imageProcessingService.ValidatePathAsync(command.Path, command.Type))
            throw new InvalidOperationException("Invalid collection path");
        
        // Create collection
        await _repository.AddAsync(collection);
        
        // Start background scan
        await _backgroundJobService.EnqueueAsync(new ScanCollectionJob(collection.Id));
        
        return collection;
    }
}
```

### Image Processing Service
```csharp
public interface IImageProcessingService
{
    Task<bool> ValidatePathAsync(string path, CollectionType type);
    Task<List<ImageInfo>> ScanCollectionAsync(string collectionId, string path, CollectionType type);
    Task<byte[]> ProcessImageAsync(ImageProcessingRequest request);
    Task<string> GenerateThumbnailAsync(ThumbnailRequest request);
    Task<ImageMetadata> GetMetadataAsync(string imagePath);
}

public class ImageProcessingService : IImageProcessingService
{
    private readonly IFileSystemService _fileSystemService;
    private readonly IImageCacheService _cacheService;
    private readonly ILogger<ImageProcessingService> _logger;
    
    public async Task<byte[]> ProcessImageAsync(ImageProcessingRequest request)
    {
        using var image = SKImage.FromEncodedData(request.ImageData);
        using var bitmap = SKBitmap.FromImage(image);
        
        // Apply transformations
        var processedBitmap = ApplyTransformations(bitmap, request.Transformations);
        
        // Encode to requested format
        var encodedData = EncodeImage(processedBitmap, request.OutputFormat, request.Quality);
        
        return encodedData;
    }
    
    private SKBitmap ApplyTransformations(SKBitmap bitmap, List<ImageTransformation> transformations)
    {
        // Apply resize, crop, filters, etc.
        return bitmap;
    }
}
```

### Cache Service
```csharp
public interface IImageCacheService
{
    Task<CacheInfo> GetCacheAsync(string imageId, CacheOptions options);
    Task<CacheInfo> SetCacheAsync(string imageId, byte[] imageData, CacheOptions options);
    Task<bool> DeleteCacheAsync(string imageId);
    Task<CacheStatistics> GetStatisticsAsync();
    Task CleanupExpiredAsync();
}

public class ImageCacheService : IImageCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IRedisCache _redisCache;
    private readonly IFileSystemService _fileSystemService;
    private readonly ICacheFolderService _cacheFolderService;
    
    public async Task<CacheInfo> GetCacheAsync(string imageId, CacheOptions options)
    {
        // Try memory cache first
        var memoryKey = $"image:{imageId}:{options.GetHashCode()}";
        if (_memoryCache.TryGetValue(memoryKey, out byte[] cachedData))
            return new CacheInfo { Data = cachedData, Source = CacheSource.Memory };
        
        // Try Redis cache
        var redisKey = $"image:{imageId}:{options.GetHashCode()}";
        var redisData = await _redisCache.GetAsync(redisKey);
        if (redisData != null)
        {
            _memoryCache.Set(memoryKey, redisData, TimeSpan.FromMinutes(5));
            return new CacheInfo { Data = redisData, Source = CacheSource.Redis };
        }
        
        // Try file cache
        var cacheFolder = await _cacheFolderService.GetCacheFolderAsync(imageId);
        var filePath = Path.Combine(cacheFolder.Path, $"{imageId}_{options.GetHashCode()}.{options.Format}");
        
        if (await _fileSystemService.ExistsAsync(filePath))
        {
            var fileData = await _fileSystemService.ReadAllBytesAsync(filePath);
            await _redisCache.SetAsync(redisKey, fileData, TimeSpan.FromHours(24));
            _memoryCache.Set(memoryKey, fileData, TimeSpan.FromMinutes(5));
            return new CacheInfo { Data = fileData, Source = CacheSource.File };
        }
        
        return null;
    }
}
```

## Infrastructure Services

### Database Context
```csharp
public class ImageViewerDbContext : DbContext
{
    public DbSet<Collection> Collections { get; set; }
    public DbSet<Image> Images { get; set; }
    public DbSet<CacheInfo> CacheInfos { get; set; }
    public DbSet<CacheFolder> CacheFolders { get; set; }
    public DbSet<CollectionTag> CollectionTags { get; set; }
    public DbSet<CollectionStatistics> CollectionStatistics { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Collection configuration
        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(1000);
            entity.HasIndex(e => e.Path).IsUnique();
            entity.HasOne(e => e.CacheFolder).WithMany(cf => cf.Collections);
        });
        
        // Image configuration
        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Filename).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RelativePath).IsRequired().HasMaxLength(1000);
            entity.HasIndex(e => new { e.CollectionId, e.Filename }).IsUnique();
            entity.HasOne(e => e.Collection).WithMany(c => c.Images);
        });
        
        // Cache configuration
        modelBuilder.Entity<CacheInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CachePath).IsRequired().HasMaxLength(1000);
            entity.HasIndex(e => e.ImageId);
            entity.HasIndex(e => e.ExpiresAt);
    });
}

## Analytics Services

### User Behavior Tracking Service
```csharp
public interface IUserBehaviorTrackingService
{
    Task TrackViewEventAsync(TrackViewEventCommand command);
    Task TrackSearchEventAsync(TrackSearchEventCommand command);
    Task TrackInteractionEventAsync(TrackInteractionEventCommand command);
    Task TrackNavigationEventAsync(TrackNavigationEventCommand command);
    Task<List<UserBehaviorEvent>> GetUserEventsAsync(GetUserEventsQuery query);
    Task<UserAnalytics> GetUserAnalyticsAsync(GetUserAnalyticsQuery query);
    Task<List<ContentPopularity>> GetPopularContentAsync(GetPopularContentQuery query);
}

public class UserBehaviorTrackingService : IUserBehaviorTrackingService
{
    private readonly IUserBehaviorEventRepository _eventRepository;
    private readonly IUserAnalyticsRepository _analyticsRepository;
    private readonly IContentPopularityRepository _popularityRepository;
    private readonly ISearchAnalyticsRepository _searchAnalyticsRepository;
    private readonly ILogger<UserBehaviorTrackingService> _logger;

    public async Task TrackViewEventAsync(TrackViewEventCommand command)
    {
        var viewEvent = new UserBehaviorEvent
        {
            UserId = command.UserId,
            SessionId = command.SessionId,
            EventType = "view",
            TargetType = command.TargetType,
            TargetId = command.TargetId,
            Metadata = new EventMetadata
            {
                Duration = command.Duration,
                StartTime = command.StartTime,
                EndTime = command.EndTime,
                Viewport = command.Viewport,
                ZoomLevel = command.ZoomLevel
            },
            Context = command.Context,
            Timestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _eventRepository.AddAsync(viewEvent);

        // Update real-time analytics
        await UpdateContentPopularityAsync(command.TargetId, command.TargetType, "view");
        await UpdateUserAnalyticsAsync(command.UserId, "view");
    }

    public async Task TrackSearchEventAsync(TrackSearchEventCommand command)
    {
        var searchEvent = new UserBehaviorEvent
        {
            UserId = command.UserId,
            SessionId = command.SessionId,
            EventType = "search",
            TargetType = "search",
            TargetId = ObjectId.GenerateNewId(),
            Metadata = new EventMetadata
            {
                Query = command.Query,
                Filters = command.Filters,
                ResultCount = command.ResultCount,
                SearchTime = command.SearchTime,
                ClickedResults = command.ClickedResults
            },
            Context = command.Context,
            Timestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _eventRepository.AddAsync(searchEvent);

        // Also store in search analytics
        var searchAnalytics = new SearchAnalytics
        {
            UserId = command.UserId,
            SessionId = command.SessionId,
            Query = command.Query,
            QueryHash = ComputeQueryHash(command.Query),
            Filters = command.Filters,
            ResultCount = command.ResultCount,
            SearchTime = command.SearchTime,
            ClickedResults = command.ClickedResults,
            SearchPath = command.SearchPath,
            SearchSuccess = command.SearchSuccess,
            SearchSatisfaction = command.SearchSatisfaction,
            Timestamp = DateTime.UtcNow,
            Context = command.Context,
            CreatedAt = DateTime.UtcNow
        };

        await _searchAnalyticsRepository.AddAsync(searchAnalytics);
    }

    private async Task UpdateContentPopularityAsync(ObjectId targetId, string targetType, string eventType)
    {
        var popularity = await _popularityRepository.GetByTargetAsync(targetId, targetType, "daily");
        if (popularity == null)
        {
            popularity = new ContentPopularity
            {
                TargetType = targetType,
                TargetId = targetId,
                Period = "daily",
                Date = DateTime.UtcNow.Date,
                Metrics = new PopularityMetrics(),
                Trends = new ContentTrends(),
                RelatedContent = new RelatedContent(),
                CreatedAt = DateTime.UtcNow
            };
        }

        switch (eventType)
        {
            case "view":
                popularity.Metrics.TotalViews++;
                break;
            case "like":
                popularity.Metrics.Likes++;
                break;
            case "share":
                popularity.Metrics.Shares++;
                break;
            case "favorite":
                popularity.Metrics.Favorites++;
                break;
        }

        // Recalculate scores
        popularity.Metrics.PopularityScore = CalculatePopularityScore(popularity.Metrics);
        popularity.Metrics.EngagementScore = CalculateEngagementScore(popularity.Metrics);
        popularity.Metrics.TrendingScore = CalculateTrendingScore(popularity.Metrics);

        popularity.UpdatedAt = DateTime.UtcNow;
        await _popularityRepository.UpsertAsync(popularity);
    }

    private async Task UpdateUserAnalyticsAsync(string userId, string eventType)
    {
        var today = DateTime.UtcNow.Date;
        var analytics = await _analyticsRepository.GetByUserAndDateAsync(userId, today, "daily");
        
        if (analytics == null)
        {
            analytics = new UserAnalytics
            {
                UserId = userId,
                Period = "daily",
                Date = today,
                Metrics = new UserMetrics(),
                TopContent = new TopContent(),
                Preferences = new UserPreferences(),
                Demographics = new UserDemographics(),
                CreatedAt = DateTime.UtcNow
            };
        }

        switch (eventType)
        {
            case "view":
                analytics.Metrics.TotalViews++;
                break;
            case "search":
                analytics.Metrics.TotalSearches++;
                break;
            case "like":
                analytics.Metrics.TotalLikes++;
                break;
            case "share":
                analytics.Metrics.TotalShares++;
                break;
            case "favorite":
                analytics.Metrics.TotalFavorites++;
                break;
        }

        analytics.UpdatedAt = DateTime.UtcNow;
        await _analyticsRepository.UpsertAsync(analytics);
    }

    private string ComputeQueryHash(string query) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(query.ToLowerInvariant())));
    private double CalculatePopularityScore(PopularityMetrics metrics) => (metrics.TotalViews * 0.4 + metrics.Likes * 0.3 + metrics.Shares * 0.3) / 100.0;
    private double CalculateEngagementScore(PopularityMetrics metrics) => (metrics.Likes + metrics.Shares + metrics.Favorites) / Math.Max(metrics.TotalViews, 1);
    private double CalculateTrendingScore(PopularityMetrics metrics) => metrics.DailyGrowth > 0.1 ? 0.8 : 0.2;
}
```

### Analytics Reporting Service
```csharp
public interface IAnalyticsReportingService
{
    Task<UserAnalyticsReport> GetUserAnalyticsReportAsync(GetUserAnalyticsReportQuery query);
    Task<ContentPopularityReport> GetContentPopularityReportAsync(GetContentPopularityReportQuery query);
    Task<SearchAnalyticsReport> GetSearchAnalyticsReportAsync(GetSearchAnalyticsReportQuery query);
    Task<TrendingContentReport> GetTrendingContentReportAsync(GetTrendingContentReportQuery query);
    Task<UserSegmentReport> GetUserSegmentReportAsync(GetUserSegmentReportQuery query);
}

public class AnalyticsReportingService : IAnalyticsReportingService
{
    private readonly IUserAnalyticsRepository _userAnalyticsRepository;
    private readonly IContentPopularityRepository _popularityRepository;
    private readonly ISearchAnalyticsRepository _searchAnalyticsRepository;
    private readonly IUserBehaviorEventRepository _eventRepository;
    private readonly ILogger<AnalyticsReportingService> _logger;

    public async Task<UserAnalyticsReport> GetUserAnalyticsReportAsync(GetUserAnalyticsReportQuery query)
    {
        var userAnalytics = await _userAnalyticsRepository.GetByUserAndPeriodAsync(
            query.UserId, 
            query.StartDate, 
            query.EndDate, 
            query.Period
        );

        var report = new UserAnalyticsReport
        {
            UserId = query.UserId,
            Period = query.Period,
            StartDate = query.StartDate,
            EndDate = query.EndDate,
            TotalViews = userAnalytics.Sum(u => u.Metrics.TotalViews),
            TotalSearches = userAnalytics.Sum(u => u.Metrics.TotalSearches),
            TotalLikes = userAnalytics.Sum(u => u.Metrics.TotalLikes),
            TotalShares = userAnalytics.Sum(u => u.Metrics.TotalShares),
            AverageSessionDuration = userAnalytics.Average(u => u.Metrics.AverageSessionDuration),
            UserSegment = userAnalytics.FirstOrDefault()?.GetUserSegment() ?? "unknown"
        };

        return report;
    }

    public async Task<TrendingContentReport> GetTrendingContentReportAsync(GetTrendingContentReportQuery query)
    {
        var trendingContent = await _popularityRepository.GetTrendingContentAsync(
            query.TargetType,
            query.Period,
            query.StartDate,
            query.EndDate,
            query.Limit
        );

        var report = new TrendingContentReport
        {
            Period = query.Period,
            StartDate = query.StartDate,
            EndDate = query.EndDate,
            TrendingItems = trendingContent.Select(c => new TrendingItem
            {
                TargetId = c.TargetId,
                TargetType = c.TargetType,
                TrendingScore = c.Metrics.TrendingScore,
                PopularityScore = c.Metrics.PopularityScore,
                EngagementScore = c.Metrics.EngagementScore,
                TotalViews = c.Metrics.TotalViews,
                TotalLikes = c.Metrics.Likes,
                TotalShares = c.Metrics.Shares,
                GrowthRate = c.Trends.DailyGrowth
            }).ToList()
        };

        return report;
    }
}
}
```

### Background Job Service
```csharp
public interface IBackgroundJobService
{
    Task<string> EnqueueAsync<T>(T job) where T : IBackgroundJob;
    Task<string> ScheduleAsync<T>(T job, TimeSpan delay) where T : IBackgroundJob;
    Task<string> ScheduleAsync<T>(T job, DateTimeOffset scheduleAt) where T : IBackgroundJob;
    Task<bool> DeleteAsync(string jobId);
    Task<JobStatus> GetStatusAsync(string jobId);
}

public class HangfireBackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;
    
    public async Task<string> EnqueueAsync<T>(T job) where T : IBackgroundJob
    {
        return _backgroundJobClient.Enqueue<IBackgroundJobProcessor<T>>(processor => processor.ProcessAsync(job));
    }
}

// Background job processors
public class ScanCollectionJobProcessor : IBackgroundJobProcessor<ScanCollectionJob>
{
    private readonly ICollectionService _collectionService;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly ILogger<ScanCollectionJobProcessor> _logger;
    
    public async Task ProcessAsync(ScanCollectionJob job)
    {
        _logger.LogInformation("Starting collection scan for {CollectionId}", job.CollectionId);
        
        var collection = await _collectionService.GetByIdAsync(job.CollectionId);
        var images = await _imageProcessingService.ScanCollectionAsync(
            job.CollectionId, 
            collection.Path, 
            collection.Type
        );
        
        // Process images in batches
        var batchSize = 10;
        for (int i = 0; i < images.Count; i += batchSize)
        {
            var batch = images.Skip(i).Take(batchSize);
            await ProcessBatchAsync(batch);
        }
        
        _logger.LogInformation("Completed collection scan for {CollectionId}", job.CollectionId);
    }
}
```

## API Controllers

### Collections Controller
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class CollectionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CollectionsController> _logger;
    
    [HttpGet]
    public async Task<ActionResult<PagedResult<CollectionDto>>> GetCollections(
        [FromQuery] GetCollectionsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<CollectionDto>> GetCollection(string id)
    {
        var query = new GetCollectionQuery { Id = id };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
    
    [HttpPost]
    public async Task<ActionResult<CollectionDto>> CreateCollection(
        [FromBody] CreateCollectionCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetCollection), new { id = result.Id }, result);
    }
    
    [HttpPost("{id}/scan")]
    public async Task<ActionResult> ScanCollection(string id)
    {
        var command = new ScanCollectionCommand { Id = id };
        await _mediator.Send(command);
        return Accepted();
    }
}
```

### Images Controller
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IImageProcessingService _imageProcessingService;
    
    [HttpGet("{collectionId}")]
    public async Task<ActionResult<PagedResult<ImageDto>>> GetImages(
        string collectionId, [FromQuery] GetImagesQuery query)
    {
        query.CollectionId = collectionId;
        var result = await _mediator.Send(query);
        return Ok(result);
    }
    
    [HttpGet("{collectionId}/{imageId}/file")]
    public async Task<ActionResult> GetImageFile(
        string collectionId, string imageId, [FromQuery] GetImageFileQuery query)
    {
        query.CollectionId = collectionId;
        query.ImageId = imageId;
        var result = await _mediator.Send(query);
        
        return File(result.Data, result.ContentType, result.Filename);
    }
    
    [HttpGet("{collectionId}/{imageId}/thumbnail")]
    public async Task<ActionResult> GetThumbnail(
        string collectionId, string imageId, [FromQuery] GetThumbnailQuery query)
    {
        query.CollectionId = collectionId;
        query.ImageId = imageId;
        var result = await _mediator.Send(query);
        
        return File(result.Data, "image/jpeg", $"{imageId}_thumb.jpg");
    }
}
```

## Configuration & Startup

### Program.cs
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<ImageViewerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

builder.Services.AddMemoryCache();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddHangfire(config => config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

// Add custom services
builder.Services.AddScoped<ICollectionService, CollectionService>();
builder.Services.AddScoped<IImageProcessingService, ImageProcessingService>();
builder.Services.AddScoped<IImageCacheService, ImageCacheService>();
builder.Services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## Performance Optimizations

### 1. Database Optimizations
- **Connection Pooling**: Configure EF Core connection pooling
- **Query Optimization**: Use compiled queries for frequently used queries
- **Indexing Strategy**: Create appropriate indexes for common query patterns
- **Bulk Operations**: Use bulk insert/update for large datasets

### 2. Caching Strategy
- **Multi-level Caching**: Memory → Redis → File system
- **Cache Invalidation**: Smart cache invalidation based on data changes
- **Cache Warming**: Pre-populate cache for frequently accessed data
- **Cache Compression**: Compress cached data to reduce memory usage

### 3. Image Processing Optimizations
- **Parallel Processing**: Process multiple images concurrently
- **Memory Management**: Use object pooling for image processing
- **Format Optimization**: Choose optimal image formats for different use cases
- **Progressive Loading**: Implement progressive image loading

### 4. API Optimizations
- **Response Compression**: Compress API responses
- **Pagination**: Implement efficient pagination
- **Field Selection**: Allow clients to select specific fields
- **Rate Limiting**: Implement rate limiting to prevent abuse

## Security Considerations

### 1. Authentication & Authorization
- **JWT Tokens**: Use JWT for stateless authentication
- **Role-based Access**: Implement role-based access control
- **API Keys**: Support API key authentication for external services

### 2. Data Protection
- **Input Validation**: Validate all inputs
- **SQL Injection Prevention**: Use parameterized queries
- **File Upload Security**: Validate file types and sizes
- **Path Traversal Prevention**: Prevent directory traversal attacks

### 3. Infrastructure Security
- **HTTPS**: Enforce HTTPS for all communications
- **CORS**: Configure CORS properly
- **Security Headers**: Add security headers
- **Logging**: Implement comprehensive security logging

## Monitoring & Observability

### 1. Logging
- **Structured Logging**: Use structured logging with Serilog
- **Log Levels**: Appropriate log levels for different scenarios
- **Correlation IDs**: Use correlation IDs for request tracing
- **Sensitive Data**: Avoid logging sensitive data

### 2. Metrics
- **Application Metrics**: Track application performance metrics
- **Business Metrics**: Track business-specific metrics
- **Infrastructure Metrics**: Monitor infrastructure health
- **Custom Metrics**: Define custom metrics for specific use cases

### 3. Health Checks
- **Database Health**: Check database connectivity
- **Cache Health**: Check cache connectivity
- **External Services**: Check external service health
- **Custom Health Checks**: Implement custom health checks

## Deployment Strategy

### 1. Containerization
- **Docker**: Containerize the application
- **Multi-stage Builds**: Optimize Docker images
- **Health Checks**: Implement Docker health checks
- **Resource Limits**: Set appropriate resource limits

### 2. Orchestration
- **Kubernetes**: Use Kubernetes for orchestration
- **Helm Charts**: Use Helm for deployment management
- **Service Mesh**: Consider service mesh for microservices
- **Auto-scaling**: Implement horizontal pod autoscaling

### 3. CI/CD
- **GitHub Actions**: Use GitHub Actions for CI/CD
- **Automated Testing**: Implement comprehensive automated testing
- **Deployment Pipelines**: Create deployment pipelines
- **Rollback Strategy**: Implement rollback strategies

## Conclusion

Kiến trúc mới này sẽ giải quyết các vấn đề performance và logic không nhất quán trong hệ thống hiện tại. Việc sử dụng .NET 8 với Clean Architecture, CQRS, và các best practices sẽ giúp:

1. **Cải thiện Performance**: Async programming, efficient caching, optimized database queries
2. **Tăng Reliability**: Better error handling, comprehensive logging, health checks
3. **Dễ Maintain**: Clean architecture, separation of concerns, testable code
4. **Scalability**: Horizontal scaling, microservices architecture, cloud-native design
5. **Developer Experience**: Better tooling, debugging, and development experience

Kiến trúc này được thiết kế để có thể scale từ single instance đến distributed microservices architecture tùy theo nhu cầu.
