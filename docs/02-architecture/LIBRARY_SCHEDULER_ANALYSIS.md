# 📋 Library Scheduler & Auto-Monitoring Analysis

**Date:** 2025-01-11  
**Purpose:** Analyze capability to support automated library folder monitoring and scheduled scans  
**Status:** 🔍 Analysis Complete - Implementation Plan Ready

---

## 🎯 Requirements Analysis

### User Requirements:
1. **Automated Library Monitoring:**
   - Monitor library folders for file changes (add/remove/modify)
   - Detect new collections automatically
   - Detect collection changes (files added/removed/modified)
   - Auto-trigger cache/thumbnail rebuild for changed files
   - Full rebuild option for new collections
   
2. **Scheduler Manager:**
   - Configurable scheduled jobs
   - Not just RabbitMQ consumers (need cron-like scheduling)
   - Monitoring and management UI
   - Start/stop/pause schedulers
   
3. **Enhanced Library Feature:**
   - Integration with file system watching
   - Incremental vs full rebuild logic
   - Change detection and smart caching

---

## 📊 Current System Capability Assessment

### Question 1: Can Current DB Support Monitoring Logic?

**Answer: ✅ YES - 80% Ready**

#### ✅ **What We Have:**

**1. Library Entity with WatchInfo:**
```csharp
public class Library : BaseEntity
{
    public WatchInfo WatchInfo { get; private set; }
    
    public void EnableWatching() { ... }
    public void DisableWatching() { ... }
}

public class WatchInfo
{
    public bool IsWatching { get; private set; }
    public string WatchPath { get; private set; }
    public List<string> WatchFilters { get; private set; }
    public DateTime? LastWatchDate { get; private set; }
    public long WatchCount { get; private set; }
    public DateTime? LastChangeDetected { get; private set; }
    public long ChangeCount { get; private set; }
    
    public void RecordChange() { ... }
    public void AddWatchFilter(string filter) { ... }
}
```

**Status:** ✅ **Basic structure exists**

**2. BackgroundJob Entity:**
```csharp
public class BackgroundJob : BaseEntity
{
    public string JobType { get; private set; }
    public string Status { get; private set; }
    public Dictionary<string, JobStageInfo> Stages { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
}
```

**Status:** ✅ **Can track scan jobs**

**3. Collection Entity:**
```csharp
public class Collection : BaseEntity
{
    public List<ImageEmbedded> Images { get; private set; }
    public List<CacheImageEmbedded> CacheImages { get; set; }
    public List<ThumbnailEmbedded> Thumbnails { get; private set; }
    public CollectionWatchInfo WatchInfo { get; private set; }
    public DateTime UpdatedAt { get; private set; }
}
```

**Status:** ✅ **Has change tracking**

#### ⚠️ **What's Missing for Full Monitoring:**

**1. File Change History Tracking:**
```csharp
// NEEDED: Track individual file changes for incremental rebuild
public class FileChangeLog : BaseEntity
{
    public ObjectId LibraryId { get; set; }
    public ObjectId? CollectionId { get; set; }
    public string FilePath { get; set; }
    public FileChangeType ChangeType { get; set; } // Added, Modified, Deleted
    public DateTime DetectedAt { get; set; }
    public bool Processed { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public long FileSize { get; set; }
    public string FileHash { get; set; } // MD5/SHA256 for modification detection
}

public enum FileChangeType
{
    Added,
    Modified,
    Deleted,
    Renamed
}
```

**2. Collection Sync State:**
```csharp
// NEEDED: Track sync state for incremental updates
public class CollectionSyncState
{
    public DateTime LastFullScan { get; set; }
    public DateTime? LastIncrementalScan { get; set; }
    public int FilesAdded { get; set; }
    public int FilesRemoved { get; set; }
    public int FilesModified { get; set; }
    public List<string> PendingRebuildImageIds { get; set; } // Images needing cache rebuild
    public SyncStatus Status { get; set; }
}

public enum SyncStatus
{
    InSync,
    ScanRequired,
    RebuildRequired,
    Scanning,
    Rebuilding
}
```

**3. Incremental Cache Rebuild Tracking:**
```csharp
// NEEDED: Track which images need cache/thumbnail rebuild
public class RebuildQueue : BaseEntity
{
    public ObjectId LibraryId { get; set; }
    public ObjectId CollectionId { get; set; }
    public List<string> ImageIdsToRebuildCache { get; set; }
    public List<string> ImageIdsToRebuildThumbnail { get; set; }
    public int Priority { get; set; } // 1-10, higher = rebuild sooner
    public DateTime QueuedAt { get; set; }
}
```

#### **DB Capability Verdict:**

✅ **MongoDB Can Handle:**
- Embedded documents for fast queries ✅
- Atomic array operations for thread-safety ✅
- Efficient indexing for change logs ✅
- Flexible schema for future enhancements ✅

⚠️ **Needs Schema Additions:**
- `FileChangeLog` collection (20% work)
- `CollectionSyncState` embedded in Collection
- `RebuildQueue` collection or embedded queue

**Overall: 80% ready, needs 3 new entities/value objects**

---

### Question 2: Do We Have Scheduler Manager?

**Answer: ❌ NO - Need to Build (0% Ready)**

#### ✅ **What We Have:**

**1. IHostedService Pattern:**
```csharp
// FileProcessingJobRecoveryHostedService.cs
public class FileProcessingJobRecoveryHostedService : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Runs once on startup
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        await recoveryService.RecoverIncompleteJobsAsync();
    }
}
```

**Capabilities:**
- ✅ Runs on worker startup
- ✅ Can have background loops
- ❌ No scheduling (cron/interval)
- ❌ No start/stop/pause control
- ❌ No configuration UI

**2. BackgroundJob Entity:**
```csharp
// Can track job execution
public class BackgroundJob : BaseEntity
{
    public string JobType { get; private set; }
    public string Status { get; private set; }
    public Dictionary<string, JobStageInfo> Stages { get; private set; }
}
```

**Capabilities:**
- ✅ Job tracking
- ✅ Progress monitoring
- ❌ No scheduling metadata
- ❌ No recurrence logic

**3. RabbitMQ Message Queue:**
```csharp
// Good for: async work distribution
// Bad for: scheduled/recurring tasks
```

#### ❌ **What's Missing:**

**Need Full Scheduler System:**

```csharp
// 1. Scheduled Job Entity
public class ScheduledJob : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string JobType { get; set; } // "LibraryScan", "CacheCleanup", etc.
    public string CronExpression { get; set; } // "0 */30 * * * *" = every 30 min
    public bool IsEnabled { get; set; }
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    public int RunCount { get; set; }
    public int FailureCount { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public ScheduleType ScheduleType { get; set; } // Cron, Interval, Once
    public TimeSpan? Interval { get; set; } // For interval-based
}

public enum ScheduleType
{
    Cron,        // Cron expression "0 0 * * * *"
    Interval,    // Every X minutes/hours
    Once,        // One-time execution
    Manual       // Only via API trigger
}

// 2. Scheduled Job Run History
public class ScheduledJobRun : BaseEntity
{
    public ObjectId ScheduledJobId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string Status { get; set; } // Running, Completed, Failed
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Result { get; set; }
}

// 3. Scheduler Service Interface
public interface ISchedulerService
{
    Task<ScheduledJob> CreateScheduledJobAsync(ScheduledJob job);
    Task<bool> EnableScheduledJobAsync(ObjectId jobId);
    Task<bool> DisableScheduledJobAsync(ObjectId jobId);
    Task<bool> PauseScheduledJobAsync(ObjectId jobId);
    Task<bool> ResumeScheduledJobAsync(ObjectId jobId);
    Task<DateTime?> GetNextRunTimeAsync(ObjectId jobId);
    Task<IEnumerable<ScheduledJob>> GetActiveScheduledJobsAsync();
    Task ExecuteScheduledJobAsync(ObjectId jobId);
}

// 4. Scheduler Hosted Service (Background Loop)
public class SchedulerHostedService : IHostedService
{
    private Timer _timer;
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(async (state) =>
        {
            await CheckAndExecuteScheduledJobs();
        }, null, TimeSpan.Zero, TimeSpan.FromMinutes(1)); // Check every minute
    }
    
    private async Task CheckAndExecuteScheduledJobs()
    {
        var dueJobs = await _schedulerService.GetDueJobsAsync(DateTime.UtcNow);
        
        foreach (var job in dueJobs)
        {
            // Execute in background
            _ = Task.Run(async () => await ExecuteJob(job));
        }
    }
}
```

**Recommended Solution:** Use existing scheduler library:
- **Hangfire** (most popular, has UI dashboard)
- **Quartz.NET** (more flexible, no UI)
- **NCrontab** + custom implementation

**Implementation Estimate:** 2-3 weeks for full scheduler system

---

### Question 3: Current Library Feature vs Requirements

**Answer: ⚠️ PARTIAL - 40% Ready**

#### ✅ **What Works Now:**

**1. Manual Library Creation:**
```csharp
// ✅ Can create libraries
var library = new Library(name, path, ownerId);
await _libraryRepository.CreateAsync(library);
```

**2. Bulk Collection Scan:**
```csharp
// ✅ Can scan folder and auto-create collections
await _bulkService.BulkAddCollectionsAsync(new BulkAddCollectionsRequest
{
    ParentPath = libraryPath,
    IncludeSubfolders = true,
    AutoScan = true
});
```

**3. WatchInfo Structure:**
```csharp
// ✅ Has basic watching metadata
public class WatchInfo
{
    public bool IsWatching { get; set; }
    public DateTime? LastChangeDetected { get; set; }
    public long ChangeCount { get; set; }
}
```

#### ❌ **What's Missing:**

**1. No File System Watching:**
```csharp
// NEEDED: FileSystemWatcher integration
public class LibraryFileWatcher : IDisposable
{
    private FileSystemWatcher _watcher;
    
    public void StartWatching(string libraryPath)
    {
        _watcher = new FileSystemWatcher(libraryPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | 
                          NotifyFilters.DirectoryName | 
                          NotifyFilters.LastWrite
        };
        
        _watcher.Created += OnFileCreated;
        _watcher.Deleted += OnFileDeleted;
        _watcher.Changed += OnFileChanged;
        _watcher.Renamed += OnFileRenamed;
        
        _watcher.EnableRaisingEvents = true;
    }
    
    private async void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        // Queue file change event
        await _messageQueue.PublishAsync(new FileChangeEvent
        {
            LibraryId = _libraryId,
            FilePath = e.FullPath,
            ChangeType = FileChangeType.Added,
            DetectedAt = DateTime.UtcNow
        });
    }
}
```

**2. No Change Detection Logic:**
```csharp
// NEEDED: Detect what changed and decide action
public class LibraryChangeDetectionService
{
    public async Task<LibraryChangeSet> DetectChangesAsync(ObjectId libraryId)
    {
        var library = await _libraryRepository.GetByIdAsync(libraryId);
        var changeSet = new LibraryChangeSet();
        
        // Scan file system
        var currentFiles = ScanDirectory(library.Path);
        
        // Compare with database
        var collections = await _collectionRepository.GetByLibraryIdAsync(libraryId);
        
        foreach (var file in currentFiles)
        {
            var collection = FindCollectionForFile(file, collections);
            
            if (collection == null)
            {
                // New file → new collection or add to existing
                changeSet.NewFiles.Add(file);
            }
            else
            {
                var image = collection.Images.FirstOrDefault(i => i.RelativePath == file.RelativePath);
                
                if (image == null)
                {
                    // File added to existing collection
                    changeSet.AddedFiles.Add(file);
                }
                else if (file.LastModified > image.UpdatedAt)
                {
                    // File modified
                    changeSet.ModifiedFiles.Add(file);
                }
            }
        }
        
        // Detect deletions
        foreach (var collection in collections)
        {
            foreach (var image in collection.Images)
            {
                var fullPath = Path.Combine(collection.Path, image.RelativePath);
                if (!File.Exists(fullPath))
                {
                    changeSet.DeletedFiles.Add(image);
                }
            }
        }
        
        return changeSet;
    }
}

public class LibraryChangeSet
{
    public List<FileInfo> NewFiles { get; set; } = new();
    public List<FileInfo> AddedFiles { get; set; } = new();
    public List<FileInfo> ModifiedFiles { get; set; } = new();
    public List<ImageEmbedded> DeletedFiles { get; set; } = new();
    
    public bool HasChanges => NewFiles.Any() || AddedFiles.Any() || 
                             ModifiedFiles.Any() || DeletedFiles.Any();
}
```

**3. No Incremental Rebuild Logic:**
```csharp
// NEEDED: Smart rebuild based on changes
public class IncrementalRebuildService
{
    public async Task RebuildChangedFilesAsync(LibraryChangeSet changes, ObjectId libraryId)
    {
        // Added/Modified files → rebuild cache + thumbnail
        var filesToRebuild = changes.AddedFiles.Concat(changes.ModifiedFiles);
        
        foreach (var file in filesToRebuild)
        {
            // Queue cache generation
            await _messageQueue.PublishAsync(new CacheGenerationMessage
            {
                ImageId = file.ImageId,
                CollectionId = file.CollectionId.ToString(),
                ImagePath = file.FullPath,
                ForceRegenerate = true // Overwrite existing cache
            });
            
            // Queue thumbnail generation
            await _messageQueue.PublishAsync(new ThumbnailGenerationMessage
            {
                ImageId = file.ImageId,
                CollectionId = file.CollectionId.ToString(),
                ImagePath = file.FullPath,
                ForceRegenerate = true
            });
        }
        
        // Deleted files → mark as deleted in DB, remove cache
        foreach (var deletedImage in changes.DeletedFiles)
        {
            await _collectionRepository.AtomicMarkImageAsDeletedAsync(
                deletedImage.CollectionId, 
                deletedImage.Id
            );
            
            // Queue cache cleanup
            await _cacheCleanupService.RemoveCacheForImageAsync(deletedImage.Id);
        }
        
        // New files → create collections if needed
        if (changes.NewFiles.Any())
        {
            await _bulkService.BulkAddCollectionsAsync(new BulkAddCollectionsRequest
            {
                ParentPath = library.Path,
                AutoScan = true
            });
        }
    }
}
```

#### **DB Schema Enhancements Needed:**

```csharp
// 1. Add to Library entity
public class Library : BaseEntity
{
    // EXISTING
    public WatchInfo WatchInfo { get; private set; }
    
    // ADD
    public ScheduleSettings ScheduleSettings { get; private set; } // NEW
    public LibraryScanState ScanState { get; private set; } // NEW
}

// 2. New Value Objects
public class ScheduleSettings
{
    public bool AutoScanEnabled { get; set; }
    public string CronExpression { get; set; } // "0 */30 * * * *"
    public TimeSpan? ScanInterval { get; set; } // Alternative to cron
    public bool IncrementalScanEnabled { get; set; } // vs full scan
    public bool AutoRebuildCache { get; set; }
    public bool AutoRebuildThumbnails { get; set; }
}

public class LibraryScanState
{
    public DateTime? LastFullScan { get; set; }
    public DateTime? LastIncrementalScan { get; set; }
    public int FilesAddedSinceLastScan { get; set; }
    public int FilesRemovedSinceLastScan { get; set; }
    public int FilesModifiedSinceLastScan { get; set; }
    public ScanStatus Status { get; set; } // Idle, Scanning, RebuildingCache
    public List<string> PendingActions { get; set; } // "rebuild_cache", "scan_new_folders"
}

// 3. New Entity for File Changes
public class FileChangeEvent : BaseEntity
{
    public ObjectId LibraryId { get; set; }
    public ObjectId? CollectionId { get; set; }
    public string FilePath { get; set; }
    public FileChangeType ChangeType { get; set; }
    public DateTime DetectedAt { get; set; }
    public bool Processed { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FileHash { get; set; } // For duplicate detection
    public long FileSize { get; set; }
}
```

**Verdict: ✅ MongoDB is perfect for this - just need to add entities**

---

## 🏗️ Architecture Design: Library Scheduler System

### Component Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    SCHEDULER SYSTEM                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  SchedulerHostedService (Background Service)                │ │
│  │  - Runs every minute                                         │ │
│  │  - Checks for due jobs                                       │ │
│  │  - Executes via SchedulerExecutionService                    │ │
│  └────────────────────────────────────────────────────────────┘ │
│                          │                                        │
│                          ▼                                        │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  ScheduledJob Repository (MongoDB)                          │ │
│  │  - GetDueJobs(DateTime now)                                  │ │
│  │  - UpdateNextRunTime(jobId, nextRun)                         │ │
│  └────────────────────────────────────────────────────────────┘ │
│                          │                                        │
│                          ▼                                        │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  Job Type Executors (Strategy Pattern)                      │ │
│  │  ┌──────────────────────────────────────────────────────┐  │ │
│  │  │ LibraryScanExecutor                                   │  │ │
│  │  │  - Scans library folder                                │  │ │
│  │  │  - Detects changes                                     │  │ │
│  │  │  - Queues rebuild jobs                                 │  │ │
│  │  └──────────────────────────────────────────────────────┘  │ │
│  │  ┌──────────────────────────────────────────────────────┐  │ │
│  │  │ CacheCleanupExecutor                                  │  │ │
│  │  │  - Cleans orphaned cache                               │  │ │
│  │  └──────────────────────────────────────────────────────┘  │ │
│  │  ┌──────────────────────────────────────────────────────┐  │ │
│  │  │ StaleJobRecoveryExecutor                              │  │ │
│  │  │  - Recovers stuck jobs                                 │  │ │
│  │  └──────────────────────────────────────────────────────┘  │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

### Data Flow for Library Monitoring

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Scheduler Triggers Library Scan (Every 30 min)          │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. LibraryScanExecutor                                      │
│    - Scan file system                                        │
│    - Compare with DB (collections + images)                  │
│    - Detect changes (add/remove/modify)                      │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. Save FileChangeEvents to MongoDB                         │
│    - New files discovered                                    │
│    - Modified files (compare LastWriteTime vs UpdatedAt)    │
│    - Deleted files (in DB but not on disk)                   │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. IncrementalRebuildService                                │
│    ├─ New files → Create collection/Add to collection       │
│    ├─ Modified files → Queue cache + thumbnail rebuild      │
│    └─ Deleted files → Mark as deleted, remove cache         │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. Publish to RabbitMQ                                      │
│    - CacheGenerationMessage (for modified/new)              │
│    - ThumbnailGenerationMessage (for modified/new)          │
│    - CollectionCreationMessage (for new folders)            │
└─────────────────────────────────────────────────────────────┘
```

---

## 📋 Implementation Plan

### Phase 1: DB Schema Enhancements (Week 1)

**1.1 Create ScheduledJob Entity**
```csharp
// File: src/ImageViewer.Domain/Entities/ScheduledJob.cs
public class ScheduledJob : BaseEntity
{
    public string Name { get; private set; }
    public string JobType { get; private set; }
    public string CronExpression { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTime? LastRunAt { get; private set; }
    public DateTime? NextRunAt { get; private set; }
    public Dictionary<string, object> Parameters { get; private set; }
    
    // Methods
    public void Enable() { IsEnabled = true; }
    public void Disable() { IsEnabled = false; }
    public void UpdateNextRunTime(DateTime nextRun) { NextRunAt = nextRun; }
    public void RecordRun(DateTime runTime) { LastRunAt = runTime; }
}
```

**1.2 Create FileChangeEvent Entity**
```csharp
// File: src/ImageViewer.Domain/Entities/FileChangeEvent.cs
public class FileChangeEvent : BaseEntity
{
    public ObjectId LibraryId { get; private set; }
    public string FilePath { get; private set; }
    public FileChangeType ChangeType { get; private set; }
    public DateTime DetectedAt { get; private set; }
    public bool Processed { get; private set; }
    
    public void MarkAsProcessed() { Processed = true; }
}
```

**1.3 Enhance Library Entity**
```csharp
// Add to Library.cs
public ScheduleSettings ScheduleSettings { get; private set; }
public LibraryScanState ScanState { get; private set; }

public void ConfigureSchedule(ScheduleSettings settings)
{
    ScheduleSettings = settings;
    UpdateTimestamp();
}

public void UpdateScanState(LibraryScanState state)
{
    ScanState = state;
    UpdateTimestamp();
}
```

**Files to Create:**
- ✅ `src/ImageViewer.Domain/Entities/ScheduledJob.cs`
- ✅ `src/ImageViewer.Domain/Entities/FileChangeEvent.cs`
- ✅ `src/ImageViewer.Domain/ValueObjects/ScheduleSettings.cs`
- ✅ `src/ImageViewer.Domain/ValueObjects/LibraryScanState.cs`
- ✅ `src/ImageViewer.Domain/Enums/FileChangeType.cs`
- ✅ `src/ImageViewer.Domain/Enums/ScanStatus.cs`

**Repositories:**
- ✅ `IScheduledJobRepository` + `MongoScheduledJobRepository`
- ✅ `IFileChangeEventRepository` + `MongoFileChangeEventRepository`

---

### Phase 2: Scheduler Core (Week 2)

**2.1 Install Scheduler Library**
```bash
dotnet add src/ImageViewer.Worker package Hangfire.Core
dotnet add src/ImageViewer.Worker package Hangfire.Mongo
dotnet add src/ImageViewer.Api package Hangfire.AspNetCore
```

**OR** custom implementation:
```bash
dotnet add src/ImageViewer.Worker package NCrontab
```

**2.2 Create Scheduler Service**
```csharp
// File: src/ImageViewer.Application/Services/SchedulerService.cs
public interface ISchedulerService
{
    Task<ScheduledJob> CreateScheduledJobAsync(ScheduledJob job);
    Task<bool> EnableJobAsync(ObjectId jobId);
    Task<bool> DisableJobAsync(ObjectId jobId);
    Task<DateTime?> CalculateNextRunTime(string cronExpression);
    Task<IEnumerable<ScheduledJob>> GetDueJobsAsync(DateTime now);
    Task ExecuteJobAsync(ObjectId jobId);
}

public class SchedulerService : ISchedulerService
{
    private readonly IScheduledJobRepository _jobRepository;
    private readonly IScheduledJobExecutorFactory _executorFactory;
    
    public async Task<IEnumerable<ScheduledJob>> GetDueJobsAsync(DateTime now)
    {
        // Get jobs where NextRunAt <= now AND IsEnabled = true
        return await _jobRepository.GetDueJobsAsync(now);
    }
    
    public async Task ExecuteJobAsync(ObjectId jobId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null) return;
        
        // Get executor for this job type
        var executor = _executorFactory.GetExecutor(job.JobType);
        
        // Execute
        await executor.ExecuteAsync(job);
        
        // Update next run time
        var nextRun = await CalculateNextRunTime(job.CronExpression);
        job.UpdateNextRunTime(nextRun.Value);
        await _jobRepository.UpdateAsync(job);
    }
}
```

**2.3 Create Scheduler Hosted Service**
```csharp
// File: src/ImageViewer.Worker/Services/SchedulerHostedService.cs
public class SchedulerHostedService : IHostedService
{
    private Timer? _timer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SchedulerHostedService> _logger;
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🕐 Scheduler starting...");
        
        _timer = new Timer(async (state) =>
        {
            await CheckAndExecuteScheduledJobs();
        }, null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1));
        
        // Check immediately on startup
        await CheckAndExecuteScheduledJobs();
    }
    
    private async Task CheckAndExecuteScheduledJobs()
    {
        using var scope = _serviceProvider.CreateScope();
        var schedulerService = scope.ServiceProvider.GetRequiredService<ISchedulerService>();
        
        var dueJobs = await schedulerService.GetDueJobsAsync(DateTime.UtcNow);
        
        foreach (var job in dueJobs)
        {
            _logger.LogInformation("⏰ Executing scheduled job: {JobName} ({JobType})", 
                job.Name, job.JobType);
            
            // Execute in background (don't block timer)
            _ = Task.Run(async () =>
            {
                try
                {
                    await schedulerService.ExecuteJobAsync(job.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute scheduled job {JobId}", job.Id);
                }
            });
        }
    }
}
```

---

### Phase 3: Library File Monitoring (Week 3)

**3.1 Create Library File Watcher Service**
```csharp
// File: src/ImageViewer.Worker/Services/LibraryFileWatcherService.cs
public class LibraryFileWatcherService : IHostedService, IDisposable
{
    private readonly Dictionary<ObjectId, FileSystemWatcher> _watchers = new();
    private readonly IServiceProvider _serviceProvider;
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Get all libraries with watching enabled
        using var scope = _serviceProvider.CreateScope();
        var libraryRepository = scope.ServiceProvider.GetRequiredService<ILibraryRepository>();
        
        var libraries = await libraryRepository.GetAllAsync();
        var watchingLibraries = libraries.Where(l => l.WatchInfo.IsWatching);
        
        foreach (var library in watchingLibraries)
        {
            StartWatchingLibrary(library);
        }
    }
    
    private void StartWatchingLibrary(Library library)
    {
        if (_watchers.ContainsKey(library.Id))
            return;
        
        var watcher = new FileSystemWatcher(library.Path)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | 
                          NotifyFilters.DirectoryName | 
                          NotifyFilters.LastWrite | 
                          NotifyFilters.Size
        };
        
        watcher.Created += (s, e) => OnFileChanged(library.Id, e.FullPath, FileChangeType.Added);
        watcher.Deleted += (s, e) => OnFileChanged(library.Id, e.FullPath, FileChangeType.Deleted);
        watcher.Changed += (s, e) => OnFileChanged(library.Id, e.FullPath, FileChangeType.Modified);
        watcher.Renamed += (s, e) => OnFileRenamed(library.Id, e.OldFullPath, e.FullPath);
        
        watcher.EnableRaisingEvents = true;
        _watchers[library.Id] = watcher;
        
        _logger.LogInformation("👁️ Started watching library: {Name} at {Path}", 
            library.Name, library.Path);
    }
    
    private async void OnFileChanged(ObjectId libraryId, string filePath, FileChangeType changeType)
    {
        // Debounce: ignore if not an image file
        if (!IsImageFile(filePath))
            return;
        
        _logger.LogInformation("📝 File change detected: {ChangeType} - {Path}", 
            changeType, filePath);
        
        // Queue file change event
        using var scope = _serviceProvider.CreateScope();
        var messageQueue = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
        
        await messageQueue.PublishAsync(new FileChangeMessage
        {
            LibraryId = libraryId.ToString(),
            FilePath = filePath,
            ChangeType = changeType.ToString(),
            DetectedAt = DateTime.UtcNow
        });
    }
}
```

**3.2 Create File Change Processor**
```csharp
// File: src/ImageViewer.Worker/Services/FileChangeConsumer.cs
public class FileChangeConsumer : BaseMessageConsumer
{
    protected override async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        var changeMessage = JsonSerializer.Deserialize<FileChangeMessage>(message);
        
        using var scope = _serviceScopeFactory.CreateScope();
        var changeProcessor = scope.ServiceProvider.GetRequiredService<IFileChangeProcessorService>();
        
        await changeProcessor.ProcessFileChangeAsync(changeMessage);
    }
}

public interface IFileChangeProcessorService
{
    Task ProcessFileChangeAsync(FileChangeMessage change);
}

public class FileChangeProcessorService : IFileChangeProcessorService
{
    public async Task ProcessFileChangeAsync(FileChangeMessage change)
    {
        var libraryId = ObjectId.Parse(change.LibraryId);
        var library = await _libraryRepository.GetByIdAsync(libraryId);
        
        switch (change.ChangeType)
        {
            case "Added":
                await HandleFileAddedAsync(library, change.FilePath);
                break;
                
            case "Modified":
                await HandleFileModifiedAsync(library, change.FilePath);
                break;
                
            case "Deleted":
                await HandleFileDeletedAsync(library, change.FilePath);
                break;
        }
    }
    
    private async Task HandleFileModifiedAsync(Library library, string filePath)
    {
        // Find which collection contains this file
        var collections = await _collectionRepository.GetByLibraryIdAsync(library.Id);
        var collection = collections.FirstOrDefault(c => filePath.StartsWith(c.Path));
        
        if (collection != null)
        {
            var image = collection.Images.FirstOrDefault(i => 
                Path.Combine(collection.Path, i.RelativePath) == filePath);
            
            if (image != null)
            {
                _logger.LogInformation("🔄 File modified, queuing rebuild: {Path}", filePath);
                
                // Queue cache regeneration
                await _messageQueue.PublishAsync(new CacheGenerationMessage
                {
                    ImageId = image.Id,
                    CollectionId = collection.Id.ToString(),
                    ImagePath = filePath,
                    ForceRegenerate = true // Overwrite old cache
                });
                
                // Queue thumbnail regeneration
                await _messageQueue.PublishAsync(new ThumbnailGenerationMessage
                {
                    ImageId = image.Id,
                    CollectionId = collection.Id.ToString(),
                    ImagePath = filePath,
                    ForceRegenerate = true
                });
            }
        }
    }
}
```

---

### Phase 4: Scheduler Management UI (Week 4)

**4.1 Backend API**
```csharp
// File: src/ImageViewer.Api/Controllers/SchedulerController.cs
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class SchedulerController : ControllerBase
{
    [HttpGet("jobs")]
    public async Task<ActionResult<IEnumerable<ScheduledJobDto>>> GetScheduledJobs()
    {
        var jobs = await _schedulerService.GetAllScheduledJobsAsync();
        return Ok(jobs);
    }
    
    [HttpPost("jobs")]
    public async Task<ActionResult<ScheduledJobDto>> CreateScheduledJob(CreateScheduledJobRequest request)
    {
        // Create library scan job, cache cleanup job, etc.
        var job = await _schedulerService.CreateScheduledJobAsync(request);
        return CreatedAtAction(nameof(GetScheduledJob), new { id = job.Id }, job);
    }
    
    [HttpPost("jobs/{id}/enable")]
    public async Task<ActionResult> EnableJob(string id)
    {
        await _schedulerService.EnableJobAsync(ObjectId.Parse(id));
        return Ok();
    }
    
    [HttpPost("jobs/{id}/disable")]
    public async Task<ActionResult> DisableJob(string id)
    {
        await _schedulerService.DisableJobAsync(ObjectId.Parse(id));
        return Ok();
    }
    
    [HttpPost("jobs/{id}/run-now")]
    public async Task<ActionResult> RunJobNow(string id)
    {
        await _schedulerService.ExecuteJobAsync(ObjectId.Parse(id));
        return Accepted();
    }
    
    [HttpGet("jobs/{id}/history")]
    public async Task<ActionResult<IEnumerable<ScheduledJobRunDto>>> GetJobHistory(
        string id, 
        [FromQuery] int page = 1, 
        [FromQuery] int limit = 20)
    {
        var history = await _schedulerService.GetJobHistoryAsync(ObjectId.Parse(id), page, limit);
        return Ok(history);
    }
}
```

**4.2 Frontend UI**
```typescript
// File: client/src/pages/SchedulerManagement.tsx
const SchedulerManagement: React.FC = () => {
  const [jobs, setJobs] = useState<ScheduledJob[]>([]);
  
  const { data: scheduledJobs } = useQuery({
    queryKey: ['scheduledJobs'],
    queryFn: async () => {
      const response = await api.get('/scheduler/jobs');
      return response.data;
    },
    refetchInterval: 10000 // 10 seconds
  });
  
  return (
    <div className="p-6">
      <h1>Scheduled Jobs</h1>
      
      {/* Job List */}
      {scheduledJobs?.map(job => (
        <div key={job.id} className="job-card">
          <h3>{job.name}</h3>
          <p>Type: {job.jobType}</p>
          <p>Schedule: {job.cronExpression}</p>
          <p>Next Run: {formatDistanceToNow(job.nextRunAt)}</p>
          <p>Last Run: {job.lastRunAt ? formatDistanceToNow(job.lastRunAt) : 'Never'}</p>
          
          {/* Controls */}
          <button onClick={() => toggleJob(job.id, job.isEnabled)}>
            {job.isEnabled ? 'Disable' : 'Enable'}
          </button>
          <button onClick={() => runJobNow(job.id)}>
            Run Now
          </button>
        </div>
      ))}
      
      {/* Create New Job */}
      <CreateScheduledJobForm />
    </div>
  );
};
```

---

## 🎯 Detailed Implementation Roadmap

### **Week 1: DB Schema & Repositories**

**Day 1-2: Domain Entities**
- [ ] Create `ScheduledJob` entity with BSON attributes
- [ ] Create `FileChangeEvent` entity
- [ ] Create `ScheduleSettings` value object
- [ ] Create `LibraryScanState` value object
- [ ] Add enums (`FileChangeType`, `ScanStatus`)
- [ ] Update `Library` entity with new properties
- [ ] Write unit tests for entities

**Day 3-4: Repositories**
- [ ] Create `IScheduledJobRepository` interface
- [ ] Implement `MongoScheduledJobRepository`
- [ ] Create `IFileChangeEventRepository` interface
- [ ] Implement `MongoFileChangeEventRepository`
- [ ] Add repository registrations to DI
- [ ] Write repository unit tests

**Day 5: DTOs & Mappings**
- [ ] Create `ScheduledJobDto`
- [ ] Create `ScheduledJobRunDto`
- [ ] Create mapping extensions
- [ ] Create request/response models

---

### **Week 2: Scheduler Core Logic**

**Day 1-2: Scheduler Service**
- [ ] Decide: Hangfire vs Quartz vs Custom
- [ ] Install chosen library
- [ ] Implement `ISchedulerService`
- [ ] Implement cron expression parser
- [ ] Add next run time calculation
- [ ] Write scheduler service tests

**Day 3: Job Executors**
- [ ] Create `IScheduledJobExecutor` interface
- [ ] Implement `LibraryScanExecutor`
- [ ] Implement `CacheCleanupExecutor`
- [ ] Implement `StaleJobRecoveryExecutor`
- [ ] Create executor factory pattern
- [ ] Write executor tests

**Day 4-5: Hosted Service**
- [ ] Implement `SchedulerHostedService`
- [ ] Add timer-based job checking
- [ ] Handle concurrent execution prevention
- [ ] Add graceful shutdown
- [ ] Register in `Program.cs`
- [ ] Integration testing

---

### **Week 3: Library Monitoring**

**Day 1-2: File Watching**
- [ ] Implement `LibraryFileWatcherService`
- [ ] Add `FileSystemWatcher` per library
- [ ] Handle file change events
- [ ] Add debouncing (ignore rapid changes)
- [ ] Queue file change messages
- [ ] Write file watcher tests

**Day 3: Change Detection**
- [ ] Implement `LibraryChangeDetectionService`
- [ ] Compare file system vs database
- [ ] Detect new/modified/deleted files
- [ ] Calculate change set
- [ ] Write change detection tests

**Day 4-5: Incremental Rebuild**
- [ ] Implement `IncrementalRebuildService`
- [ ] Handle added files (add to collection)
- [ ] Handle modified files (queue rebuild)
- [ ] Handle deleted files (mark as deleted)
- [ ] Smart rebuild logic (only changed)
- [ ] Write rebuild service tests

---

### **Week 4: UI & Integration**

**Day 1-2: Backend API**
- [ ] Create `SchedulerController`
- [ ] Implement CRUD for scheduled jobs
- [ ] Add enable/disable endpoints
- [ ] Add run-now endpoint
- [ ] Add job history endpoint
- [ ] Write API integration tests

**Day 3-4: Frontend**
- [ ] Create `SchedulerManagement.tsx` page
- [ ] Add job list with status
- [ ] Add create/edit job form
- [ ] Add enable/disable controls
- [ ] Add run-now button
- [ ] Add job history view
- [ ] Add to navigation menu

**Day 5: Testing & Documentation**
- [ ] End-to-end testing
- [ ] Load testing (100+ scheduled jobs)
- [ ] Write deployment guide
- [ ] Create operator runbook
- [ ] Update architecture docs

---

## 💾 Database Collections Summary

### **Existing Collections:**
| Collection | Purpose | Schema Ready |
|------------|---------|--------------|
| `libraries` | Library metadata | ✅ 80% (needs ScheduleSettings) |
| `collections` | Collections with images | ✅ 100% |
| `file_processing_job_states` | Job tracking | ✅ 100% |
| `background_jobs` | Background tasks | ✅ 100% |
| `cache_folders` | Cache storage | ✅ 100% |

### **New Collections Needed:**
| Collection | Purpose | Priority | Estimated Size |
|------------|---------|----------|----------------|
| `scheduled_jobs` | Scheduler config | 🔴 High | ~100 docs |
| `scheduled_job_runs` | Execution history | 🟡 Medium | ~10k docs/month |
| `file_change_events` | File change log | 🟡 Medium | ~100k docs/month |

### **Indexes Required:**
```javascript
// scheduled_jobs
db.scheduled_jobs.createIndex({ "isEnabled": 1, "nextRunAt": 1 });
db.scheduled_jobs.createIndex({ "jobType": 1 });

// scheduled_job_runs
db.scheduled_job_runs.createIndex({ "scheduledJobId": 1, "startedAt": -1 });
db.scheduled_job_runs.createIndex({ "status": 1, "completedAt": -1 });

// file_change_events
db.file_change_events.createIndex({ "libraryId": 1, "processed": 1, "detectedAt": -1 });
db.file_change_events.createIndex({ "processed": 1, "detectedAt": 1 });
```

---

## 🔧 Technology Stack Recommendations

### **Option A: Hangfire (Recommended)**

**Pros:**
- ✅ Built-in dashboard UI
- ✅ Cron expression support
- ✅ Distributed execution
- ✅ Automatic retry
- ✅ MongoDB storage provider exists
- ✅ Production-proven

**Cons:**
- ⚠️ Additional dependency
- ⚠️ Separate UI (not integrated)

**Integration:**
```csharp
// Startup.cs
services.AddHangfire(config =>
{
    config.UseMongoStorage(mongoConnectionString, "imageviewer_hangfire");
});
services.AddHangfireServer();

// Schedule jobs
RecurringJob.AddOrUpdate<LibraryScanExecutor>(
    "library-scan",
    x => x.ExecuteAsync(),
    "*/30 * * * *" // Every 30 minutes
);
```

### **Option B: Quartz.NET**

**Pros:**
- ✅ Very flexible
- ✅ Advanced scheduling
- ✅ Fine-grained control

**Cons:**
- ⚠️ No built-in UI
- ⚠️ More setup required

### **Option C: Custom Implementation**

**Pros:**
- ✅ Full control
- ✅ Integrated with our system
- ✅ Lighter weight

**Cons:**
- ⚠️ More development time
- ⚠️ Need to build UI
- ⚠️ Need to handle edge cases

**Recommendation:** **Hangfire** for faster delivery with dashboard

---

## 📊 Effort Estimation

| Phase | Tasks | Effort | Dependencies |
|-------|-------|--------|--------------|
| **Phase 1: DB Schema** | 6 entities, 2 repos | 3-5 days | None |
| **Phase 2: Scheduler Core** | Hangfire integration, executors | 5-7 days | Phase 1 |
| **Phase 3: File Monitoring** | FileWatcher, change detection | 5-7 days | Phase 1, 2 |
| **Phase 4: UI & Testing** | React UI, integration tests | 5-7 days | Phase 1, 2, 3 |
| **Total** | 4 phases | **18-26 days** | Sequential |

**With 2 developers:** **2-3 weeks**  
**With 1 developer:** **4-5 weeks**

---

## 🚀 Quick Start Option (MVP - 1 Week)

**Minimal Viable Product:**

1. **Simple Scheduled Scan** (3 days)
   - Use `PeriodicTimer` in hosted service
   - Run full library scan every N hours
   - Queue full cache/thumbnail rebuild
   - No incremental logic
   
2. **Basic UI** (2 days)
   - Enable/disable auto-scan per library
   - Configure scan interval
   - View last scan time
   
3. **Testing** (2 days)
   - Unit tests for scan logic
   - Manual testing

**Code:**
```csharp
public class SimpleLibrarySchedulerService : IHostedService
{
    private PeriodicTimer? _timer;
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new PeriodicTimer(TimeSpan.FromHours(1)); // Every hour
        
        while (await _timer.WaitForNextTickAsync(cancellationToken))
        {
            await ScanAllLibrariesAsync();
        }
    }
    
    private async Task ScanAllLibrariesAsync()
    {
        var libraries = await _libraryRepository.GetAllAsync();
        var watchingLibraries = libraries.Where(l => l.WatchInfo.IsWatching);
        
        foreach (var library in watchingLibraries)
        {
            // Publish bulk scan message
            await _messageQueue.PublishAsync(new BulkOperationMessage
            {
                OperationType = "ScanLibrary",
                Parameters = new Dictionary<string, object>
                {
                    { "LibraryId", library.Id.ToString() },
                    { "FullScan", true }
                }
            });
        }
    }
}
```

---

## ✅ Final Recommendations

### **Immediate Actions (This Sprint):**

1. **Decide on Scheduler Approach:**
   - ✅ **Recommended: Hangfire** (fastest, has UI)
   - Alternative: Custom (more work, more control)

2. **Create POC (Proof of Concept):**
   - Install Hangfire
   - Create one scheduled job (library scan)
   - Test basic execution
   - Evaluate fit

3. **Design DB Schema:**
   - Finalize `ScheduledJob` schema
   - Design `FileChangeEvent` schema
   - Plan indexes

### **Next Sprint:**
- Implement full scheduler system
- Add file system watching
- Build management UI

### **Future Enhancements:**
- Smart caching (only rebuild changed)
- Distributed file watching (multiple workers)
- Change batching (don't rebuild on every save)
- ML-based prediction (anticipate cache needs)

---

## 📚 Summary Table

| Question | Answer | Current State | Work Needed |
|----------|--------|---------------|-------------|
| **1. Can DB handle monitoring?** | ✅ **YES** | 80% ready | Add 3 entities |
| **2. Do we have scheduler?** | ❌ **NO** | 0% | Full implementation |
| **3. Is library feature ready?** | ⚠️ **PARTIAL** | 40% | Add monitoring logic |

### **Overall Project Status:**
- **Current Capability:** 40%
- **Effort Required:** 4-5 weeks (1 developer)
- **Technology:** Hangfire + FileSystemWatcher
- **Complexity:** Medium
- **Risk:** Low (well-established patterns)

---

## 🎯 Recommended Next Steps

**Option A: Full Implementation** (Recommended)
- 4-5 weeks development
- Production-grade solution
- Hangfire dashboard
- Comprehensive monitoring

**Option B: MVP First** (Faster)
- 1 week MVP
- Basic scheduled scans
- Simple interval-based
- Iterate later

**Option C: Phased Approach** (Balanced)
- Week 1: DB schema + repos
- Week 2: Basic scheduler (no UI)
- Week 3: File watching
- Week 4: Management UI

**What would you like to proceed with?** 🚀

