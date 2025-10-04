# MongoDB Database Redesign for ImageViewer

## 🎯 Overview

This document outlines the redesign of the ImageViewer database structure from PostgreSQL (relational) to MongoDB (NoSQL) to better align with the application's requirements and UI/UX patterns.

## 🏗️ Current Issues with PostgreSQL Design

1. **Over-normalization**: Too many separate tables for what should be embedded documents
2. **Complex joins**: Multiple tables for metadata, cache info, and relationships
3. **Performance issues**: Heavy joins for collection browsing and searching
4. **Scalability concerns**: Difficult to scale with large image collections
5. **UI/UX mismatch**: Database structure doesn't match user interaction patterns

## 🎨 UI/UX Requirements Analysis

### 1. Collection List Screen
- **Purpose**: Quick search and browse collections
- **Features**: Search by name, tags, metadata, creation date, size
- **Performance**: Fast filtering and sorting
- **Data needed**: Collection summary, tags, metadata, statistics

### 2. Collection Detail Screen
- **Purpose**: View collection items (images, videos)
- **Features**: Grid/list view, filtering, sorting, pagination
- **Performance**: Fast item loading with thumbnails
- **Data needed**: Items with metadata, thumbnails, cache info

### 3. Media Detail Screen
- **Purpose**: View individual image/video
- **Features**: Full resolution, metadata, navigation
- **Performance**: Fast loading of media and metadata
- **Data needed**: Full media info, metadata, cache paths

### 4. Cache Management Screens
- **Purpose**: Manage cache folders and rebuild operations
- **Features**: Cache status, rebuild progress, settings
- **Performance**: Real-time status updates
- **Data needed**: Cache info, job status, settings

## 🗄️ New MongoDB Database Structure

### 1. Libraries Collection
```javascript
{
  _id: ObjectId,
  name: String,
  path: String,
  type: String, // "local", "network", "cloud"
  settings: {
    autoScan: Boolean,
    scanInterval: Number, // minutes
    watchMode: String, // "realtime", "scheduled", "manual"
    includeSubfolders: Boolean,
    fileFilters: {
      images: [String], // [".jpg", ".png", ".gif"]
      videos: [String], // [".mp4", ".avi", ".mov"]
      excludePatterns: [String] // ["*.tmp", "*.log"]
    }
  },
  metadata: {
    description: String,
    tags: [String],
    createdDate: Date,
    lastModified: Date,
    totalCollections: Number,
    totalSize: Number,
    lastScanDate: Date
  },
  statistics: {
    collectionCount: Number,
    totalItems: Number,
    totalSize: Number,
    lastScanDuration: Number,
    scanCount: Number
  },
  watchInfo: {
    isWatching: Boolean,
    lastWatchCheck: Date,
    watchErrors: [{
      path: String,
      error: String,
      timestamp: Date
    }],
    fileSystemWatcher: {
      enabled: Boolean,
      lastEvent: Date,
      eventCount: Number
    }
  },
  searchIndex: {
    searchableText: String,
    tags: [String],
    path: String
  },
  createdAt: Date,
  updatedAt: Date
}
```

### 2. Collections Collection
```javascript
{
  _id: ObjectId,
  libraryId: ObjectId, // Reference to parent library
  name: String,
  path: String,
  type: String, // "image", "video", "mixed"
  settings: {
    thumbnailSize: Number,
    cacheEnabled: Boolean,
    autoScan: Boolean,
    scanInterval: Number,
    priority: Number, // For scan priority
    watchMode: String // "realtime", "scheduled", "manual"
  },
  metadata: {
    description: String,
    tags: [String],
    createdDate: Date,
    lastModified: Date,
    totalItems: Number,
    totalSize: Number,
    averageFileSize: Number
  },
  statistics: {
    imageCount: Number,
    videoCount: Number,
    totalSize: Number,
    lastScanDate: Date,
    scanDuration: Number,
    lastFileSystemCheck: Date
  },
  cacheInfo: {
    enabled: Boolean,
    folderPath: String,
    lastRebuild: Date,
    rebuildStatus: String, // "idle", "running", "completed", "failed"
    progress: Number
  },
  watchInfo: {
    isWatching: Boolean,
    lastWatchCheck: Date,
    fileSystemHash: String, // Hash of directory structure for change detection
    lastDirectoryScan: Date,
    watchErrors: [{
      path: String,
      error: String,
      timestamp: Date
    }]
  },
  searchIndex: {
    // For full-text search
    searchableText: String,
    tags: [String],
    metadata: Object
  },
  createdAt: Date,
  updatedAt: Date
}
```

### 3. Media Items Collection
```javascript
{
  _id: ObjectId,
  collectionId: ObjectId,
  libraryId: ObjectId, // Reference to parent library
  filename: String,
  filePath: String,
  fileType: String, // "image", "video"
  mimeType: String,
  fileSize: Number,
  dimensions: {
    width: Number,
    height: Number
  },
  fileInfo: {
    // File system info for change detection
    lastModified: Date,
    fileHash: String, // MD5/SHA256 hash for change detection
    fileSystemHash: String, // Hash of file system metadata
    exists: Boolean,
    lastChecked: Date
  },
  metadata: {
    // Image metadata
    camera: String,
    lens: String,
    exposure: String,
    iso: Number,
    aperture: String,
    focalLength: String,
    dateTaken: Date,
    gps: {
      latitude: Number,
      longitude: Number,
      altitude: Number
    },
    // Video metadata
    duration: Number,
    frameRate: Number,
    bitrate: Number,
    codec: String
  },
  tags: [String],
  cacheInfo: {
    thumbnailPath: String,
    previewPath: String,
    fullSizePath: String,
    lastGenerated: Date,
    status: String, // "generated", "generating", "failed"
    needsRegeneration: Boolean, // Flag for cache invalidation
    cacheHash: String // Hash of cached files
  },
  statistics: {
    viewCount: Number,
    lastViewed: Date,
    rating: Number,
    favorite: Boolean
  },
  searchIndex: {
    searchableText: String,
    tags: [String],
    metadata: Object
  },
  createdAt: Date,
  updatedAt: Date
}
```

### 4. File System Watchers Collection
```javascript
{
  _id: ObjectId,
  libraryId: ObjectId,
  collectionId: ObjectId, // Optional, for collection-specific watchers
  path: String,
  type: String, // "library", "collection", "file"
  settings: {
    watchSubdirectories: Boolean,
    includeFilters: [String],
    excludeFilters: [String],
    bufferSize: Number,
    notifyFilters: [String] // ["FileName", "DirectoryName", "Size", "LastWrite"]
  },
  status: {
    isActive: Boolean,
    lastEvent: Date,
    eventCount: Number,
    errorCount: Number,
    lastError: String,
    lastErrorDate: Date
  },
  performance: {
    averageEventProcessingTime: Number,
    eventsPerMinute: Number,
    lastPerformanceCheck: Date
  },
  createdAt: Date,
  updatedAt: Date
}
```

### 5. Cache Folders Collection
```javascript
{
  _id: ObjectId,
  name: String,
  path: String,
  type: String, // "thumbnail", "preview", "full"
  settings: {
    maxSize: Number,
    compressionQuality: Number,
    format: String, // "jpg", "png", "webp"
    autoCleanup: Boolean,
    cleanupDays: Number
  },
  statistics: {
    totalFiles: Number,
    totalSize: Number,
    lastCleanup: Date,
    lastRebuild: Date
  },
  status: {
    isActive: Boolean,
    lastError: String,
    lastErrorDate: Date
  },
  createdAt: Date,
  updatedAt: Date
}
```

### 6. Background Jobs Collection
```javascript
{
  _id: ObjectId,
  type: String, // "scan", "thumbnail", "cache", "rebuild", "watch", "sync"
  status: String, // "pending", "running", "completed", "failed", "cancelled"
  priority: Number,
  progress: {
    current: Number,
    total: Number,
    percentage: Number,
    message: String,
    currentItem: String,
    estimatedTimeRemaining: Number
  },
  target: {
    libraryId: ObjectId,
    collectionId: ObjectId,
    mediaId: ObjectId,
    cacheFolderId: ObjectId,
    path: String
  },
  parameters: {
    scanMode: String, // "full", "incremental", "quick"
    forceRegenerate: Boolean,
    skipExisting: Boolean,
    batchSize: Number
  },
  result: {
    success: Boolean,
    message: String,
    data: Object,
    filesProcessed: Number,
    filesSkipped: Number,
    filesFailed: Number,
    errors: [{
      file: String,
      error: String,
      timestamp: Date
    }]
  },
  timing: {
    startedAt: Date,
    completedAt: Date,
    duration: Number,
    estimatedDuration: Number
  },
  retry: {
    count: Number,
    maxRetries: Number,
    nextRetry: Date,
    retryReason: String
  },
  performance: {
    itemsPerSecond: Number,
    averageProcessingTime: Number,
    memoryUsage: Number,
    cpuUsage: Number
  },
  createdAt: Date,
  updatedAt: Date
}
```

### 7. System Settings Collection
```javascript
{
  _id: ObjectId,
  key: String, // Unique key for setting
  value: Object, // Setting value (can be any type)
  type: String, // "boolean", "string", "number", "object", "array"
  category: String, // "sync", "cache", "performance", "security", "backup"
  description: String,
  defaultValue: Object,
  validation: {
    min: Number,
    max: Number,
    allowedValues: [Object],
    required: Boolean
  },
  metadata: {
    version: String,
    lastModifiedBy: String, // "system", "admin", "user"
    isReadOnly: Boolean,
    isAdvanced: Boolean
  },
  createdAt: Date,
  updatedAt: Date
}
```

### 8. User Settings Collection
```javascript
{
  _id: ObjectId,
  userId: String, // For future multi-user support
  preferences: {
    theme: String, // "light", "dark", "auto"
    language: String,
    defaultView: String, // "grid", "list", "masonry"
    itemsPerPage: Number,
    thumbnailSize: Number,
    showMetadata: Boolean,
    showFileInfo: Boolean,
    autoPlayVideos: Boolean,
    videoVolume: Number
  },
  displaySettings: {
    gridColumns: Number,
    listItemHeight: Number,
    showThumbnails: Boolean,
    thumbnailQuality: String, // "low", "medium", "high"
    showFileNames: Boolean,
    showFileSizes: Boolean,
    showDates: Boolean,
    dateFormat: String,
    timeFormat: String
  },
  navigationSettings: {
    enableKeyboardShortcuts: Boolean,
    enableMouseGestures: Boolean,
    defaultSortField: String,
    defaultSortDirection: String,
    rememberLastPosition: Boolean,
    autoAdvance: Boolean,
    autoAdvanceDelay: Number
  },
  searchSettings: {
    defaultSearchFields: [String],
    searchHistory: [String],
    savedSearches: [{
      name: String,
      query: Object,
      createdAt: Date
    }],
    searchSuggestions: Boolean,
    highlightResults: Boolean
  },
  favoriteListSettings: {
    defaultListType: String, // "manual", "smart"
    autoCreateLists: Boolean,
    maxListsPerUser: Number,
    defaultListSettings: {
      isPublic: Boolean,
      allowDuplicates: Boolean,
      maxItems: Number
    }
  },
  notificationSettings: {
    enableNotifications: Boolean,
    notifyOnScanComplete: Boolean,
    notifyOnCacheComplete: Boolean,
    notifyOnSmartListUpdate: Boolean,
    soundEnabled: Boolean
  },
  privacySettings: {
    shareUsageData: Boolean,
    shareErrorReports: Boolean,
    rememberSearchHistory: Boolean,
    rememberViewHistory: Boolean
  },
  createdAt: Date,
  updatedAt: Date
}
```

### 9. Favorite Lists Collection
```javascript
{
  _id: ObjectId,
  userId: String, // For future multi-user support
  name: String,
  description: String,
  type: String, // "manual", "smart", "auto"
  settings: {
    isPublic: Boolean,
    allowDuplicates: Boolean,
    maxItems: Number,
    autoSort: Boolean,
    sortField: String, // "createdAt", "viewCount", "rating", "filename"
    sortDirection: String // "asc", "desc"
  },
  items: [{
    mediaId: ObjectId,
    collectionId: ObjectId,
    libraryId: ObjectId,
    addedAt: Date,
    addedBy: String, // "user", "auto", "smart"
    notes: String,
    tags: [String],
    customOrder: Number // For manual ordering
  }],
  smartFilters: {
    // For smart favorite lists
    enabled: Boolean,
    rules: [{
      field: String, // "tags", "fileType", "rating", "viewCount", "dateRange"
      operator: String, // "equals", "contains", "greaterThan", "lessThan", "between"
      value: Object,
      logic: String // "AND", "OR"
    }],
    autoUpdate: Boolean,
    lastUpdate: Date,
    updateInterval: Number // minutes
  },
  statistics: {
    totalItems: Number,
    totalSize: Number,
    lastAccessed: Date,
    accessCount: Number,
    averageRating: Number,
    mostViewedItem: ObjectId
  },
  metadata: {
    tags: [String],
    category: String,
    color: String, // For UI theming
    icon: String // For UI display
  },
  searchIndex: {
    searchableText: String,
    tags: [String],
    items: [String] // Filenames for search
  },
  createdAt: Date,
  updatedAt: Date
}
```

### 10. View Sessions Collection
```javascript
{
  _id: ObjectId,
  sessionId: String,
  userId: String,
  collectionId: ObjectId,
  currentItemId: ObjectId,
  viewHistory: [{
    itemId: ObjectId,
    viewedAt: Date,
    duration: Number
  }],
  filters: {
    tags: [String],
    fileTypes: [String],
    dateRange: {
      start: Date,
      end: Date
    }
  },
  sortOrder: {
    field: String,
    direction: String // "asc", "desc"
  },
  createdAt: Date,
  updatedAt: Date
}
```

### 11. Audit Logs Collection
```javascript
{
  _id: ObjectId,
  userId: String,
  action: String, // "create", "update", "delete", "view", "scan", "cache"
  resourceType: String, // "library", "collection", "media", "favoriteList", "settings"
  resourceId: ObjectId,
  details: {
    oldValue: Object,
    newValue: Object,
    changes: [String],
    metadata: Object
  },
  ipAddress: String,
  userAgent: String,
  timestamp: Date,
  sessionId: String,
  severity: String, // "info", "warning", "error", "critical"
  category: String // "user_action", "system_event", "error", "security"
}
```

### 12. Error Logs Collection
```javascript
{
  _id: ObjectId,
  errorId: String, // Unique error identifier
  type: String, // "validation", "system", "network", "file_system", "database"
  severity: String, // "low", "medium", "high", "critical"
  message: String,
  stackTrace: String,
  context: {
    userId: String,
    sessionId: String,
    requestId: String,
    operation: String,
    resourceType: String,
    resourceId: ObjectId
  },
  environment: {
    version: String,
    platform: String,
    userAgent: String,
    ipAddress: String
  },
  resolution: {
    status: String, // "unresolved", "investigating", "resolved", "ignored"
    assignedTo: String,
    resolution: String,
    resolvedAt: Date,
    resolvedBy: String
  },
  occurrences: [{
    timestamp: Date,
    count: Number,
    lastOccurrence: Date
  }],
  createdAt: Date,
  updatedAt: Date
}
```

### 13. Backup History Collection
```javascript
{
  _id: ObjectId,
  backupId: String,
  type: String, // "full", "incremental", "differential"
  status: String, // "pending", "running", "completed", "failed", "cancelled"
  source: {
    collections: [String],
    libraries: [ObjectId],
    dateRange: {
      start: Date,
      end: Date
    }
  },
  destination: {
    path: String,
    type: String, // "local", "network", "cloud"
    credentials: Object // Encrypted credentials
  },
  statistics: {
    totalFiles: Number,
    totalSize: Number,
    filesProcessed: Number,
    filesSkipped: Number,
    filesFailed: Number,
    duration: Number
  },
  schedule: {
    isScheduled: Boolean,
    cronExpression: String,
    nextRun: Date,
    lastRun: Date
  },
  retention: {
    keepDays: Number,
    maxBackups: Number,
    autoCleanup: Boolean
  },
  encryption: {
    enabled: Boolean,
    algorithm: String,
    keyId: String
  },
  verification: {
    checksum: String,
    verified: Boolean,
    verifiedAt: Date
  },
  createdAt: Date,
  updatedAt: Date
}
```

### 13. User Behavior Events Collection
```javascript
{
  _id: ObjectId,
  userId: String,
  sessionId: String,
  eventType: String, // "view", "search", "filter", "navigate", "download", "share", "like", "favorite"
  targetType: String, // "media", "collection", "library", "favorite_list", "tag"
  targetId: ObjectId,
  metadata: {
    // View events
    duration: Number, // seconds
    startTime: Date,
    endTime: Date,
    viewport: { width: Number, height: Number },
    zoomLevel: Number,
    
    // Search events
    query: String,
    filters: Object,
    resultCount: Number,
    clickedResults: [ObjectId],
    searchTime: Number, // time to complete search
    
    // Navigation events
    fromPage: String,
    toPage: String,
    navigationPath: [String],
    timeOnPage: Number,
    
    // Interaction events
    action: String, // "click", "scroll", "zoom", "like", "dislike", "share", "download"
    coordinates: { x: Number, y: Number },
    element: String,
    elementType: String, // "button", "image", "link", "video"
    
    // Content events
    contentType: String, // "image", "video", "collection"
    contentSize: Number,
    contentFormat: String,
    tags: [String],
    rating: Number
  },
  context: {
    userAgent: String,
    ipAddress: String,
    referrer: String,
    language: String,
    timezone: String,
    device: String, // "desktop", "mobile", "tablet"
    browser: String,
    os: String
  },
  timestamp: Date,
  createdAt: Date
}
```

### 14. User Analytics Collection
```javascript
{
  _id: ObjectId,
  userId: String,
  period: String, // "daily", "weekly", "monthly", "yearly"
  date: Date,
  metrics: {
    // View metrics
    totalViews: Number,
    uniqueMediaViewed: Number,
    uniqueCollectionsViewed: Number,
    averageViewDuration: Number,
    totalViewTime: Number,
    viewsByDevice: { desktop: Number, mobile: Number, tablet: Number },
    
    // Search metrics
    totalSearches: Number,
    uniqueQueries: Number,
    averageResultClickRate: Number,
    mostUsedFilters: [String],
    searchSuccessRate: Number,
    
    // Navigation metrics
    totalPageViews: Number,
    averageSessionDuration: Number,
    bounceRate: Number,
    mostVisitedPages: [String],
    navigationDepth: Number,
    
    // Interaction metrics
    totalLikes: Number,
    totalDislikes: Number,
    totalShares: Number,
    totalDownloads: Number,
    totalFavorites: Number,
    totalComments: Number,
    
    // Engagement metrics
    returnVisits: Number,
    timeBetweenVisits: Number,
    favoriteListCreations: Number,
    customTagCreations: Number
  },
  topContent: {
    mostViewedMedia: [{ mediaId: ObjectId, views: Number, totalTime: Number }],
    mostViewedCollections: [{ collectionId: ObjectId, views: Number, totalTime: Number }],
    mostSearchedTerms: [{ term: String, count: Number }],
    mostUsedTags: [{ tag: String, count: Number }],
    mostLikedContent: [{ targetId: ObjectId, targetType: String, likes: Number }]
  },
  preferences: {
    favoriteFileTypes: [String],
    favoriteTags: [String],
    preferredViewModes: [String],
    mostActiveHours: [Number],
    mostActiveDays: [String],
    preferredCollections: [ObjectId]
  },
  demographics: {
    ageGroup: String,
    gender: String,
    country: String,
    language: String
  },
  createdAt: Date,
  updatedAt: Date
}
```

### 15. Content Popularity Collection
```javascript
{
  _id: ObjectId,
  targetType: String, // "media", "collection", "library", "tag"
  targetId: ObjectId,
  period: String, // "daily", "weekly", "monthly", "yearly", "all_time"
  date: Date,
  metrics: {
    // View metrics
    totalViews: Number,
    uniqueViewers: Number,
    averageViewDuration: Number,
    totalViewTime: Number,
    viewsByDevice: { desktop: Number, mobile: Number, tablet: Number },
    viewsByCountry: Object,
    viewsByAge: Object,
    
    // Engagement metrics
    likes: Number,
    dislikes: Number,
    shares: Number,
    downloads: Number,
    favorites: Number,
    comments: Number,
    ratings: { total: Number, average: Number, distribution: Object },
    
    // Search metrics
    searchImpressions: Number,
    searchClicks: Number,
    clickThroughRate: Number,
    searchRankings: [Number], // average position in search results
    
    // Ranking metrics
    popularityScore: Number,
    trendingScore: Number,
    engagementScore: Number,
    viralityScore: Number,
    retentionScore: Number
  },
  trends: {
    dailyGrowth: Number,
    weeklyGrowth: Number,
    monthlyGrowth: Number,
    peakHours: [Number],
    peakDays: [String],
    seasonalPatterns: Object,
    viralMoments: [Date]
  },
  relatedContent: {
    frequentlyViewedTogether: [ObjectId],
    similarTags: [String],
    relatedCollections: [ObjectId],
    userSegments: [String] // "photographers", "artists", "casual_viewers"
  },
  createdAt: Date,
  updatedAt: Date
}
```

### 16. Search Analytics Collection
```javascript
{
  _id: ObjectId,
  userId: String,
  sessionId: String,
  query: String,
  queryHash: String, // for anonymization
  filters: Object,
  resultCount: Number,
  searchTime: Number, // milliseconds
  clickedResults: [{
    position: Number,
    mediaId: ObjectId,
    collectionId: ObjectId,
    clickedAt: Date,
    timeToClick: Number, // seconds from search to click
    dwellTime: Number // time spent on clicked result
  }],
  searchPath: [String], // navigation path after search
  searchSuccess: Boolean, // whether user found what they were looking for
  searchSatisfaction: Number, // 1-5 rating if provided
  timestamp: Date,
  context: {
    page: String,
    referrer: String,
    device: String,
    userSegment: String
  },
  createdAt: Date
}
```

### 17. Performance Metrics Collection
```javascript
{
  _id: ObjectId,
  timestamp: Date,
  metricType: String, // "query", "scan", "cache", "thumbnail", "api", "user_behavior"
  operation: String,
  duration: Number,
  resourceUsage: {
    cpu: Number,
    memory: Number,
    disk: Number,
    network: Number
  },
  context: {
    userId: String,
    collectionId: ObjectId,
    libraryId: ObjectId,
    itemCount: Number,
    dataSize: Number
  },
  performance: {
    throughput: Number, // items per second
    latency: Number,
    errorRate: Number,
    cacheHitRate: Number
  },
  environment: {
    version: String,
    platform: String,
    nodeId: String
  },
  createdAt: Date
}
```

## 🔍 File System Monitoring Strategy

### 1. Change Detection Methods

#### **File System Watchers (Real-time)**
```javascript
// High-priority collections with real-time monitoring
db.fileSystemWatchers.createIndex({ 
  "collectionId": 1, 
  "status.isActive": 1 
})

// Library-level watchers
db.fileSystemWatchers.createIndex({ 
  "libraryId": 1, 
  "type": 1, 
  "status.isActive": 1 
})
```

#### **Hash-based Change Detection (Efficient)**
```javascript
// Media items with file hash for change detection
db.mediaItems.createIndex({ 
  "fileInfo.fileHash": 1, 
  "fileInfo.lastModified": 1 
})

// Collections with directory structure hash
db.collections.createIndex({ 
  "watchInfo.fileSystemHash": 1, 
  "watchInfo.lastDirectoryScan": 1 
})
```

#### **Scheduled Scanning (Batch)**
```javascript
// Collections needing scheduled scans
db.collections.createIndex({ 
  "settings.watchMode": 1, 
  "statistics.lastFileSystemCheck": 1 
})
```

### 2. Performance Optimization Strategies

#### **Incremental Scanning**
- **Quick Scan**: Check file modification times only
- **Hash Scan**: Compare file hashes for changed files
- **Full Scan**: Complete directory traversal (rarely needed)

#### **Batch Processing**
- **Small Batches**: Process 10-50 files per batch
- **Priority Queuing**: High-priority collections first
- **Resource Management**: Limit concurrent operations

#### **Caching Strategy**
- **Directory Structure Cache**: Cache folder listings
- **File Metadata Cache**: Cache file system metadata
- **Hash Cache**: Cache file hashes for comparison

### 3. Background Job Types

#### **Library Management Jobs**
- `library_scan`: Full library scan for new collections
- `library_watch`: Set up file system watchers
- `library_sync`: Sync library with file system changes

#### **Collection Management Jobs**
- `collection_scan`: Scan collection for new/updated files
- `collection_watch`: Monitor collection directory
- `collection_metadata`: Update collection metadata

#### **Media Processing Jobs**
- `media_scan`: Scan individual media files
- `thumbnail_generate`: Generate thumbnails
- `cache_build`: Build cache files
- `metadata_extract`: Extract media metadata

## 🔍 Indexing Strategy

### Libraries Collection Indexes
```javascript
// Text search index
db.libraries.createIndex({
  "searchIndex.searchableText": "text",
  "searchIndex.tags": "text"
})

// Performance indexes
db.libraries.createIndex({ "name": 1 })
db.libraries.createIndex({ "type": 1 })
db.libraries.createIndex({ "settings.autoScan": 1 })
db.libraries.createIndex({ "watchInfo.isWatching": 1 })
db.libraries.createIndex({ "statistics.lastScanDate": -1 })
```

### Collections Collection Indexes
```javascript
// Text search index
db.collections.createIndex({
  "searchIndex.searchableText": "text",
  "searchIndex.tags": "text"
})

// Performance indexes
db.collections.createIndex({ "libraryId": 1, "name": 1 })
db.collections.createIndex({ "libraryId": 1, "type": 1 })
db.collections.createIndex({ "metadata.tags": 1 })
db.collections.createIndex({ "createdAt": -1 })
db.collections.createIndex({ "statistics.totalSize": -1 })
db.collections.createIndex({ "watchInfo.isWatching": 1 })
db.collections.createIndex({ "settings.priority": -1 })
```

### Media Items Collection Indexes
```javascript
// Text search index
db.mediaItems.createIndex({
  "searchIndex.searchableText": "text",
  "searchIndex.tags": "text"
})

// Performance indexes
db.mediaItems.createIndex({ "libraryId": 1, "collectionId": 1, "filename": 1 })
db.mediaItems.createIndex({ "collectionId": 1, "fileType": 1 })
db.mediaItems.createIndex({ "collectionId": 1, "createdAt": -1 })
db.mediaItems.createIndex({ "metadata.tags": 1 })
db.mediaItems.createIndex({ "statistics.viewCount": -1 })
db.mediaItems.createIndex({ "statistics.rating": -1 })
db.mediaItems.createIndex({ "fileInfo.lastModified": -1 })
db.mediaItems.createIndex({ "fileInfo.exists": 1 })
db.mediaItems.createIndex({ "cacheInfo.needsRegeneration": 1 })
```

### Background Jobs Collection Indexes
```javascript
db.backgroundJobs.createIndex({ "status": 1, "priority": -1 })
db.backgroundJobs.createIndex({ "type": 1, "status": 1 })
db.backgroundJobs.createIndex({ "target.libraryId": 1 })
db.backgroundJobs.createIndex({ "target.collectionId": 1 })
db.backgroundJobs.createIndex({ "createdAt": -1 })
db.backgroundJobs.createIndex({ "parameters.scanMode": 1 })
db.backgroundJobs.createIndex({ "timing.startedAt": -1 })
```

### File System Watchers Collection Indexes
```javascript
db.fileSystemWatchers.createIndex({ "libraryId": 1, "status.isActive": 1 })
db.fileSystemWatchers.createIndex({ "collectionId": 1, "status.isActive": 1 })
db.fileSystemWatchers.createIndex({ "type": 1, "status.isActive": 1 })
db.fileSystemWatchers.createIndex({ "path": 1 })
db.fileSystemWatchers.createIndex({ "status.lastEvent": -1 })
```

### System Settings Collection Indexes
```javascript
// Unique key index
db.systemSettings.createIndex({ "key": 1 }, { unique: true })

// Performance indexes
db.systemSettings.createIndex({ "category": 1 })
db.systemSettings.createIndex({ "type": 1 })
db.systemSettings.createIndex({ "metadata.isReadOnly": 1 })
db.systemSettings.createIndex({ "metadata.isAdvanced": 1 })
db.systemSettings.createIndex({ "updatedAt": -1 })
```

### User Settings Collection Indexes
```javascript
// Unique user index
db.userSettings.createIndex({ "userId": 1 }, { unique: true })

// Performance indexes
db.userSettings.createIndex({ "preferences.theme": 1 })
db.userSettings.createIndex({ "preferences.language": 1 })
db.userSettings.createIndex({ "preferences.defaultView": 1 })
db.userSettings.createIndex({ "notificationSettings.enableNotifications": 1 })
db.userSettings.createIndex({ "privacySettings.shareUsageData": 1 })
```

### Favorite Lists Collection Indexes
```javascript
// Text search index
db.favoriteLists.createIndex({
  "searchIndex.searchableText": "text",
  "searchIndex.tags": "text",
  "searchIndex.items": "text"
})

// Performance indexes
db.favoriteLists.createIndex({ "userId": 1, "name": 1 })
db.favoriteLists.createIndex({ "userId": 1, "type": 1 })
db.favoriteLists.createIndex({ "userId": 1, "createdAt": -1 })
db.favoriteLists.createIndex({ "settings.isPublic": 1 })
db.favoriteLists.createIndex({ "statistics.lastAccessed": -1 })
db.favoriteLists.createIndex({ "items.mediaId": 1 })
db.favoriteLists.createIndex({ "items.collectionId": 1 })
db.favoriteLists.createIndex({ "smartFilters.enabled": 1 })
db.favoriteLists.createIndex({ "smartFilters.lastUpdate": -1 })
```

### Audit Logs Collection Indexes
```javascript
// Performance indexes
db.auditLogs.createIndex({ "userId": 1, "timestamp": -1 })
db.auditLogs.createIndex({ "action": 1, "timestamp": -1 })
db.auditLogs.createIndex({ "resourceType": 1, "resourceId": 1 })
db.auditLogs.createIndex({ "severity": 1, "timestamp": -1 })
db.auditLogs.createIndex({ "category": 1, "timestamp": -1 })
db.auditLogs.createIndex({ "sessionId": 1, "timestamp": -1 })
db.auditLogs.createIndex({ "timestamp": -1 }) // TTL index for cleanup
```

### Error Logs Collection Indexes
```javascript
// Unique error identifier
db.errorLogs.createIndex({ "errorId": 1 }, { unique: true })

// Performance indexes
db.errorLogs.createIndex({ "type": 1, "severity": 1 })
db.errorLogs.createIndex({ "severity": 1, "timestamp": -1 })
db.errorLogs.createIndex({ "resolution.status": 1 })
db.errorLogs.createIndex({ "context.userId": 1, "timestamp": -1 })
db.errorLogs.createIndex({ "context.resourceType": 1, "context.resourceId": 1 })
db.errorLogs.createIndex({ "timestamp": -1 }) // TTL index for cleanup
```

### Backup History Collection Indexes
```javascript
// Unique backup identifier
db.backupHistory.createIndex({ "backupId": 1 }, { unique: true })

// Performance indexes
db.backupHistory.createIndex({ "type": 1, "status": 1 })
db.backupHistory.createIndex({ "status": 1, "createdAt": -1 })
db.backupHistory.createIndex({ "schedule.isScheduled": 1, "schedule.nextRun": 1 })
db.backupHistory.createIndex({ "destination.type": 1 })
db.backupHistory.createIndex({ "createdAt": -1 })
```

### Performance Metrics Collection Indexes
```javascript
// Time-series indexes
db.performanceMetrics.createIndex({ "timestamp": -1, "metricType": 1 })
db.performanceMetrics.createIndex({ "metricType": 1, "timestamp": -1 })
db.performanceMetrics.createIndex({ "context.userId": 1, "timestamp": -1 })
db.performanceMetrics.createIndex({ "context.collectionId": 1, "timestamp": -1 })
db.performanceMetrics.createIndex({ "operation": 1, "timestamp": -1 })
db.performanceMetrics.createIndex({ "timestamp": -1 }) // TTL index for cleanup
```

## 🎨 UI/UX Screens Design

### 1. Library Management Screen
- **Purpose**: Manage libraries and their settings
- **Features**: Add/remove libraries, configure watch settings, view statistics
- **Data needed**: Library list with statistics, watch status, scan progress

### 2. Collection List Screen
- **Purpose**: Browse and search collections within libraries
- **Features**: Filter by library, search by name/tags, sort by size/date
- **Data needed**: Collection summaries with metadata, statistics

### 3. Collection Detail Screen
- **Purpose**: View collection items with filtering and sorting
- **Features**: Grid/list view, pagination, real-time updates
- **Data needed**: Media items with thumbnails, metadata, cache status

### 4. Media Detail Screen
- **Purpose**: View individual media with full metadata
- **Features**: Full resolution view, metadata display, navigation
- **Data needed**: Full media info, metadata, cache paths

### 5. Background Jobs Screen
- **Purpose**: Monitor and manage background operations
- **Features**: Real-time progress, job history, error handling
- **Data needed**: Job status, progress, performance metrics

### 6. Cache Management Screen
- **Purpose**: Manage cache folders and rebuild operations
- **Features**: Cache status, rebuild progress, settings
- **Data needed**: Cache info, job status, performance metrics

### 7. Favorite Lists Management Screen
- **Purpose**: Create and manage favorite lists
- **Features**: Create/edit lists, add/remove items, smart filters, sharing
- **Data needed**: Favorite lists with items, statistics, settings

### 8. Favorite List Detail Screen
- **Purpose**: View and manage items in a favorite list
- **Features**: Grid/list view, sorting, filtering, item management
- **Data needed**: List items with media info, metadata, statistics

### 9. System Settings Screen
- **Purpose**: Configure system-wide settings
- **Features**: Enable/disable sync, cache settings, performance tuning, security
- **Data needed**: System settings with categories, validation rules

### 10. User Settings Screen
- **Purpose**: Configure user-specific preferences
- **Features**: Display modes, navigation, notifications, privacy settings
- **Data needed**: User settings with preferences, display options

### 11. Audit Logs Screen
- **Purpose**: View and analyze system audit logs
- **Features**: Filter by user, action, resource, time range, export logs
- **Data needed**: Audit logs with user actions, system events

### 12. Error Management Screen
- **Purpose**: Monitor and manage system errors
- **Features**: Error tracking, resolution status, assignment, analytics
- **Data needed**: Error logs with resolution status, occurrence patterns

### 13. Backup Management Screen
- **Purpose**: Manage backup operations and history
- **Features**: Schedule backups, view history, restore operations, monitoring
- **Data needed**: Backup history with status, schedules, statistics

### 14. Performance Monitoring Screen
- **Purpose**: Monitor system performance and metrics
- **Features**: Real-time metrics, performance charts, alerts, optimization suggestions
- **Data needed**: Performance metrics with trends, resource usage

## 🚀 Performance Optimizations

### 1. Aggregation Pipelines for Library List
```javascript
// Fast library search with statistics
db.libraries.aggregate([
  {
    $match: {
      $or: [
        { "name": { $regex: searchTerm, $options: "i" } },
        { "searchIndex.tags": { $in: searchTags } }
      ]
    }
  },
  {
    $lookup: {
      from: "collections",
      localField: "_id",
      foreignField: "libraryId",
      as: "collections"
    }
  },
  {
    $addFields: {
      "statistics.collectionCount": { $size: "$collections" },
      "statistics.totalItems": { $sum: "$collections.statistics.totalItems" },
      "statistics.totalSize": { $sum: "$collections.statistics.totalSize" }
    }
  },
  {
    $project: {
      collections: 0 // Remove collections array to reduce response size
    }
  }
])
```

### 2. Aggregation Pipelines for Collection List
```javascript
// Fast collection search with statistics
db.collections.aggregate([
  {
    $match: {
      "libraryId": ObjectId(libraryId),
      $or: [
        { "name": { $regex: searchTerm, $options: "i" } },
        { "searchIndex.tags": { $in: searchTags } }
      ]
    }
  },
  {
    $lookup: {
      from: "mediaItems",
      localField: "_id",
      foreignField: "collectionId",
      as: "items"
    }
  },
  {
    $addFields: {
      "statistics.totalItems": { $size: "$items" },
      "statistics.totalSize": { $sum: "$items.fileSize" }
    }
  },
  {
    $project: {
      items: 0 // Remove items array to reduce response size
    }
  }
])
```

### 3. Efficient Media Item Loading
```javascript
// Load media items with pagination
db.mediaItems.find({
  "libraryId": ObjectId(libraryId),
  "collectionId": ObjectId(collectionId)
})
.sort({ "createdAt": -1 })
.skip(page * pageSize)
.limit(pageSize)
.projection({
  "filename": 1,
  "fileType": 1,
  "fileSize": 1,
  "dimensions": 1,
  "cacheInfo.thumbnailPath": 1,
  "tags": 1,
  "statistics.viewCount": 1,
  "fileInfo.exists": 1
})
```

### 4. Real-time Job Status Updates
```javascript
// Get job progress for UI updates
db.backgroundJobs.find({
  "status": { $in: ["pending", "running"] }
})
.sort({ "priority": -1, "createdAt": 1 })
.projection({
  "type": 1,
  "status": 1,
  "progress": 1,
  "target": 1,
  "performance": 1
})
```

### 5. File System Change Detection
```javascript
// Find collections that need scanning
db.collections.find({
  $or: [
    { "watchInfo.lastDirectoryScan": { $lt: new Date(Date.now() - scanInterval) } },
    { "watchInfo.fileSystemHash": { $exists: false } }
  ],
  "settings.autoScan": true
})
.sort({ "settings.priority": -1, "watchInfo.lastDirectoryScan": 1 })
.projection({
  "name": 1,
  "path": 1,
  "watchInfo": 1,
  "settings": 1
})
```

### 6. Cache Invalidation Detection
```javascript
// Find media items that need cache regeneration
db.mediaItems.find({
  "cacheInfo.needsRegeneration": true,
  "fileInfo.exists": true
})
.sort({ "statistics.viewCount": -1, "createdAt": -1 })
.projection({
  "filename": 1,
  "filePath": 1,
  "fileType": 1,
  "cacheInfo": 1
})
```

### 7. Favorite Lists Management
```javascript
// Get user's favorite lists with statistics
db.favoriteLists.aggregate([
  {
    $match: { "userId": userId }
  },
  {
    $lookup: {
      from: "mediaItems",
      localField: "items.mediaId",
      foreignField: "_id",
      as: "mediaItems"
    }
  },
  {
    $addFields: {
      "statistics.totalItems": { $size: "$items" },
      "statistics.totalSize": { $sum: "$mediaItems.fileSize" },
      "statistics.averageRating": { $avg: "$mediaItems.statistics.rating" }
    }
  },
  {
    $project: {
      mediaItems: 0 // Remove mediaItems array to reduce response size
    }
  }
])
```

### 8. Favorite List Items with Media Info
```javascript
// Get favorite list items with full media information
db.favoriteLists.aggregate([
  {
    $match: { "_id": ObjectId(favoriteListId) }
  },
  {
    $unwind: "$items"
  },
  {
    $lookup: {
      from: "mediaItems",
      localField: "items.mediaId",
      foreignField: "_id",
      as: "mediaInfo"
    }
  },
  {
    $lookup: {
      from: "collections",
      localField: "items.collectionId",
      foreignField: "_id",
      as: "collectionInfo"
    }
  },
  {
    $unwind: "$mediaInfo"
  },
  {
    $unwind: "$collectionInfo"
  },
  {
    $project: {
      "items": 1,
      "mediaInfo.filename": 1,
      "mediaInfo.fileType": 1,
      "mediaInfo.fileSize": 1,
      "mediaInfo.dimensions": 1,
      "mediaInfo.cacheInfo.thumbnailPath": 1,
      "mediaInfo.statistics.viewCount": 1,
      "mediaInfo.statistics.rating": 1,
      "collectionInfo.name": 1,
      "collectionInfo.path": 1
    }
  }
])
```

### 9. Smart Favorite List Updates
```javascript
// Find smart favorite lists that need updating
db.favoriteLists.find({
  "smartFilters.enabled": true,
  "smartFilters.autoUpdate": true,
  $or: [
    { "smartFilters.lastUpdate": { $lt: new Date(Date.now() - updateInterval) } },
    { "smartFilters.lastUpdate": { $exists: false } }
  ]
})
.projection({
  "name": 1,
  "smartFilters": 1,
  "settings": 1
})
```

### 10. System Settings Management
```javascript
// Get system settings by category
db.systemSettings.find({
  "category": categoryName
})
.sort({ "key": 1 })
.projection({
  "key": 1,
  "value": 1,
  "type": 1,
  "description": 1,
  "defaultValue": 1,
  "metadata": 1
})

// Get all system settings for configuration
db.systemSettings.aggregate([
  {
    $group: {
      _id: "$category",
      settings: {
        $push: {
          key: "$key",
          value: "$value",
          type: "$type",
          description: "$description",
          isReadOnly: "$metadata.isReadOnly",
          isAdvanced: "$metadata.isAdvanced"
        }
      }
    }
  },
  {
    $sort: { "_id": 1 }
  }
])
```

### 11. User Settings Management
```javascript
// Get user settings with defaults
db.userSettings.findOne({
  "userId": userId
})

// Get user preferences for UI rendering
db.userSettings.find({
  "userId": userId
})
.projection({
  "preferences": 1,
  "displaySettings": 1,
  "navigationSettings": 1,
  "notificationSettings": 1
})

// Get user settings by category
db.userSettings.aggregate([
  {
    $match: { "userId": userId }
  },
  {
    $project: {
      preferences: 1,
      displaySettings: 1,
      navigationSettings: 1,
      searchSettings: 1,
      favoriteListSettings: 1,
      notificationSettings: 1,
      privacySettings: 1
    }
  }
])
```

### 12. Audit Logs Analysis
```javascript
// Get user activity summary
db.auditLogs.aggregate([
  {
    $match: {
      "userId": userId,
      "timestamp": { $gte: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000) }
    }
  },
  {
    $group: {
      _id: "$action",
      count: { $sum: 1 },
      lastActivity: { $max: "$timestamp" }
    }
  },
  {
    $sort: { "count": -1 }
  }
])

// Get resource access patterns
db.auditLogs.aggregate([
  {
    $match: {
      "resourceType": resourceType,
      "timestamp": { $gte: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000) }
    }
  },
  {
    $group: {
      _id: "$resourceId",
      accessCount: { $sum: 1 },
      lastAccess: { $max: "$timestamp" },
      users: { $addToSet: "$userId" }
    }
  },
  {
    $sort: { "accessCount": -1 }
  }
])
```

### 13. Error Analysis and Resolution
```javascript
// Get error trends by type
db.errorLogs.aggregate([
  {
    $match: {
      "timestamp": { $gte: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000) }
    }
  },
  {
    $group: {
      _id: {
        type: "$type",
        severity: "$severity"
      },
      count: { $sum: 1 },
      lastOccurrence: { $max: "$timestamp" },
      unresolved: {
        $sum: { $cond: [{ $eq: ["$resolution.status", "unresolved"] }, 1, 0] }
      }
    }
  },
  {
    $sort: { "count": -1 }
  }
])

// Get errors by user
db.errorLogs.aggregate([
  {
    $match: {
      "context.userId": userId,
      "timestamp": { $gte: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000) }
    }
  },
  {
    $group: {
      _id: "$type",
      count: { $sum: 1 },
      severity: { $push: "$severity" },
      lastError: { $max: "$timestamp" }
    }
  }
])
```

### 14. Backup Status and History
```javascript
// Get backup status summary
db.backupHistory.aggregate([
  {
    $group: {
      _id: "$type",
      totalBackups: { $sum: 1 },
      successful: {
        $sum: { $cond: [{ $eq: ["$status", "completed"] }, 1, 0] }
      },
      failed: {
        $sum: { $cond: [{ $eq: ["$status", "failed"] }, 1, 0] }
      },
      totalSize: { $sum: "$statistics.totalSize" },
      lastBackup: { $max: "$createdAt" }
    }
  }
])

// Get scheduled backups
db.backupHistory.find({
  "schedule.isScheduled": true,
  "status": { $in: ["pending", "running"] }
})
.sort({ "schedule.nextRun": 1 })
.projection({
  "backupId": 1,
  "type": 1,
  "schedule.nextRun": 1,
  "source": 1,
  "destination": 1
})
```

### 15. Performance Metrics Analysis
```javascript
// Get performance trends
db.performanceMetrics.aggregate([
  {
    $match: {
      "timestamp": { $gte: new Date(Date.now() - 24 * 60 * 60 * 1000) },
      "metricType": metricType
    }
  },
  {
    $group: {
      _id: {
        hour: { $hour: "$timestamp" },
        operation: "$operation"
      },
      avgDuration: { $avg: "$duration" },
      avgThroughput: { $avg: "$performance.throughput" },
      avgLatency: { $avg: "$performance.latency" },
      errorRate: { $avg: "$performance.errorRate" },
      sampleCount: { $sum: 1 }
    }
  },
  {
    $sort: { "_id.hour": 1 }
  }
])

// Get resource usage patterns
db.performanceMetrics.aggregate([
  {
    $match: {
      "timestamp": { $gte: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000) }
    }
  },
  {
    $group: {
      _id: {
        day: { $dayOfWeek: "$timestamp" },
        hour: { $hour: "$timestamp" }
      },
      avgCpu: { $avg: "$resourceUsage.cpu" },
      avgMemory: { $avg: "$resourceUsage.memory" },
      avgDisk: { $avg: "$resourceUsage.disk" },
      avgNetwork: { $avg: "$resourceUsage.network" }
    }
  },
  {
    $sort: { "_id.day": 1, "_id.hour": 1 }
  }
])
```

## 🎯 Benefits of New Design

### 1. Performance Improvements
- **Faster searches**: Embedded search indexes and text search
- **Reduced queries**: Single document contains all related data
- **Better caching**: Document structure matches UI patterns
- **Efficient pagination**: Optimized indexes for large datasets
- **Minimal disk I/O**: Hash-based change detection reduces file system access
- **Batch processing**: Efficient background job processing

### 2. UI/UX Alignment
- **Library management**: Easy library and collection management
- **Collection list**: Single query with all needed data
- **Media browsing**: Efficient pagination and filtering
- **Real-time updates**: Job status and progress tracking
- **Search experience**: Full-text search across all content
- **Background monitoring**: Real-time job and watch status
- **Favorite lists**: Personal collections with smart filtering
- **User experience**: Customizable lists with sharing capabilities
- **Settings management**: Separated system and user settings
- **Configuration**: Flexible system configuration with validation

### 3. File System Monitoring
- **Real-time watching**: File system watchers for immediate updates
- **Efficient scanning**: Hash-based change detection
- **Batch processing**: Scheduled scans for large collections
- **Performance tracking**: Monitor scan performance and optimize
- **Error handling**: Track and handle file system errors

### 4. Scalability
- **Horizontal scaling**: MongoDB sharding support
- **Flexible schema**: Easy to add new fields
- **Efficient storage**: Embedded documents reduce storage overhead
- **Better indexing**: Optimized for common query patterns
- **Resource management**: Limit concurrent operations

### 5. Development Benefits
- **Simpler queries**: Fewer joins and complex operations
- **Better caching**: Document structure matches UI needs
- **Easier maintenance**: Clear separation of concerns
- **Future-proof**: Easy to extend with new features
- **Monitoring**: Built-in performance and error tracking

## 🔄 Migration Strategy

### Phase 1: Data Migration
1. Export existing PostgreSQL data
2. Transform data to new MongoDB structure
3. Create indexes and search indexes
4. Validate data integrity

### Phase 2: Application Updates
1. Update repository interfaces
2. Implement new MongoDB queries
3. Update UI components for new data structure
4. Add real-time job status updates

### Phase 3: Performance Optimization
1. Monitor query performance
2. Optimize indexes based on usage patterns
3. Implement caching strategies
4. Fine-tune aggregation pipelines

## 📊 Monitoring and Analytics

### Key Metrics to Track
- **Library Management**: Library scan performance, watch status
- **Collection Performance**: Collection search, media loading times
- **Background Jobs**: Job completion rates, processing times
- **File System**: Scan performance, change detection accuracy
- **Cache Performance**: Cache hit ratios, generation times
- **User Interaction**: Search patterns, view statistics
- **Favorite Lists**: List usage, item additions, smart filter performance
- **User Engagement**: List creation, sharing, access patterns
- **System Settings**: Configuration changes, validation errors
- **User Settings**: Preference changes, UI customization usage
- **Audit & Security**: User actions, system events, security incidents
- **Error Management**: Error rates, resolution times, system stability
- **Backup & Recovery**: Backup success rates, restore times, data integrity
- **Performance**: System performance, resource usage, optimization opportunities

### Performance Targets
- **Library list load**: < 200ms
- **Collection list load**: < 200ms
- **Media item pagination**: < 100ms
- **Search results**: < 300ms
- **Job status updates**: < 50ms
- **File system scan**: < 1000ms per 1000 files
- **Cache generation**: < 500ms per thumbnail
- **Favorite list load**: < 150ms
- **Smart list update**: < 500ms per 100 items
- **Settings load**: < 100ms
- **Settings save**: < 200ms
- **Audit log query**: < 500ms
- **Error analysis**: < 300ms
- **Backup status**: < 100ms
- **Performance metrics**: < 200ms

### Monitoring Queries
```javascript
// Performance monitoring
db.backgroundJobs.aggregate([
  {
    $match: {
      "status": "completed",
      "timing.completedAt": { $gte: new Date(Date.now() - 24 * 60 * 60 * 1000) }
    }
  },
  {
    $group: {
      _id: "$type",
      averageDuration: { $avg: "$timing.duration" },
      totalJobs: { $sum: 1 },
      successRate: {
        $avg: { $cond: ["$result.success", 1, 0] }
      }
    }
  }
])

// File system monitoring
db.collections.aggregate([
  {
    $group: {
      _id: null,
      totalCollections: { $sum: 1 },
      watchedCollections: {
        $sum: { $cond: ["$watchInfo.isWatching", 1, 0] }
      },
      averageScanDuration: { $avg: "$statistics.scanDuration" }
    }
  }
])

// Favorite lists monitoring
db.favoriteLists.aggregate([
  {
    $group: {
      _id: "$type",
      totalLists: { $sum: 1 },
      totalItems: { $sum: { $size: "$items" } },
      averageItemsPerList: { $avg: { $size: "$items" } },
      mostAccessedList: { $max: "$statistics.accessCount" }
    }
  }
])
```

## 🚀 Implementation Roadmap

### Phase 1: Core Infrastructure (Week 1-2)
1. **Database Setup**: Create MongoDB collections and indexes
2. **Basic Entities**: Implement Libraries, Collections, MediaItems
3. **Repository Layer**: Create MongoDB repositories
4. **Basic API**: Implement CRUD operations

### Phase 2: File System Monitoring (Week 3-4)
1. **File System Watchers**: Implement real-time monitoring
2. **Hash-based Detection**: Implement change detection
3. **Background Jobs**: Implement job processing system
4. **Performance Monitoring**: Add metrics and monitoring

### Phase 3: UI/UX Implementation (Week 5-6)
1. **Library Management**: Implement library management screens
2. **Collection Browsing**: Implement collection list and detail
3. **Media Viewing**: Implement media detail and navigation
4. **Background Monitoring**: Implement job status screens
5. **Favorite Lists**: Implement favorite list management and viewing
6. **Settings Management**: Implement system and user settings screens
7. **Admin Screens**: Implement audit logs, error management, backup management
8. **Performance Monitoring**: Implement performance monitoring dashboard

### Phase 4: Optimization (Week 7-8)
1. **Performance Tuning**: Optimize queries and indexes
2. **Caching Strategy**: Implement advanced caching
3. **Error Handling**: Improve error handling and recovery
4. **Testing**: Comprehensive testing and validation

## 🎯 Favorite Lists Features

### 1. List Types
- **Manual Lists**: User manually adds/removes items
- **Smart Lists**: Automatically populated based on rules
- **Auto Lists**: Automatically created based on user behavior

### 2. Smart Filtering
- **Tag-based**: Items with specific tags
- **Rating-based**: Items with minimum rating
- **View-based**: Most viewed items
- **Date-based**: Items from specific date ranges
- **File type**: Specific file types (images, videos)
- **Collection-based**: Items from specific collections

### 3. List Management
- **Create/Edit**: Create new lists with custom settings
- **Share**: Public lists for sharing with others
- **Import/Export**: Backup and restore lists
- **Categories**: Organize lists by categories
- **Custom Ordering**: Manual item ordering
- **Auto-sorting**: Automatic sorting by various criteria

### 4. User Experience
- **Quick Add**: Add items to lists from media viewer
- **Bulk Operations**: Add/remove multiple items
- **Search**: Search within favorite lists
- **Statistics**: View list usage and item statistics
- **Notifications**: Notify when smart lists are updated

### 5. Performance Features
- **Lazy Loading**: Load items on demand
- **Caching**: Cache frequently accessed lists
- **Background Updates**: Update smart lists in background
- **Optimized Queries**: Efficient database queries for large lists

## ⚙️ System Settings Features

### 1. System Configuration Categories
- **Sync Settings**: Enable/disable sync from disk, scan intervals, watch modes
- **Cache Settings**: Cache folder paths, compression settings, cleanup policies
- **Performance Settings**: Batch sizes, concurrent operations, resource limits
- **Security Settings**: Access controls, encryption, backup policies
- **Backup Settings**: Backup schedules, retention policies, storage locations

### 2. Setting Management
- **Key-Value Storage**: Flexible key-value pairs with type validation
- **Category Organization**: Grouped settings by functionality
- **Validation Rules**: Min/max values, allowed values, required fields
- **Version Control**: Track setting changes and versions
- **Read-Only Settings**: System-protected settings that can't be modified

### 3. Advanced Features
- **Default Values**: Fallback values for missing settings
- **Advanced Mode**: Hide/show advanced settings
- **Import/Export**: Backup and restore system configuration
- **Validation**: Real-time validation of setting values
- **Audit Trail**: Track who changed what settings when

## 👤 User Settings Features

### 1. User Preference Categories
- **Display Preferences**: Theme, language, view modes, thumbnail sizes
- **Navigation Settings**: Keyboard shortcuts, mouse gestures, sorting
- **Search Settings**: Default search fields, history, saved searches
- **Favorite List Settings**: Default list types, auto-creation rules
- **Notification Settings**: Enable/disable notifications, sound settings
- **Privacy Settings**: Data sharing, history retention, error reporting

### 2. Personalization
- **Theme Customization**: Light/dark themes, custom colors
- **Layout Preferences**: Grid columns, item heights, information display
- **Behavior Settings**: Auto-advance, remember positions, shortcuts
- **Accessibility**: High contrast, large text, keyboard navigation

### 3. User Experience
- **Per-User Settings**: Individual settings for each user
- **Settings Sync**: Sync settings across devices (future)
- **Quick Reset**: Reset to default settings
- **Settings Backup**: Export/import user preferences
- **Settings Search**: Find specific settings quickly

## 🔒 Security & Compliance Features

### 1. Audit Logging
- **User Actions**: Track all user activities and changes
- **System Events**: Monitor system operations and changes
- **Security Events**: Track security-related activities
- **Compliance**: Meet regulatory requirements for data tracking

### 2. Error Management
- **Error Tracking**: Comprehensive error logging and analysis
- **Resolution Workflow**: Assign, track, and resolve errors
- **Trend Analysis**: Identify patterns and recurring issues
- **Alert System**: Notify administrators of critical errors

### 3. Backup & Recovery
- **Automated Backups**: Scheduled backup operations
- **Multiple Destinations**: Local, network, and cloud storage
- **Encryption**: Secure backup data with encryption
- **Verification**: Verify backup integrity and completeness

### 4. Performance Monitoring
- **Real-time Metrics**: Monitor system performance continuously
- **Resource Usage**: Track CPU, memory, disk, and network usage
- **Performance Trends**: Analyze performance over time
- **Optimization**: Identify bottlenecks and optimization opportunities

## 🎯 Summary of Additional Features

### **Collections Added:**
1. **Audit Logs**: Complete audit trail for security and compliance
2. **Error Logs**: Comprehensive error tracking and management
3. **Backup History**: Backup operations and recovery management
4. **Performance Metrics**: System performance monitoring and analysis

### **Key Benefits:**
- **Security**: Complete audit trail and error tracking
- **Reliability**: Backup and recovery capabilities
- **Performance**: Continuous monitoring and optimization
- **Compliance**: Meet regulatory and security requirements
- **Maintenance**: Proactive error management and resolution

### **Production Readiness:**
- **Monitoring**: Comprehensive system monitoring
- **Logging**: Detailed audit and error logging
- **Backup**: Automated backup and recovery
- **Security**: Security event tracking and compliance
- **Performance**: Performance monitoring and optimization

This redesign provides a solid foundation for a scalable, performant, secure, and compliant image viewer application that aligns with modern UI/UX patterns and MongoDB best practices, with efficient file system monitoring, background job processing, comprehensive favorite lists management, flexible system and user settings management, and enterprise-grade security, monitoring, and backup capabilities.
