# Image Viewer System - MongoDB Database Design

## Tổng quan Database Schema

### Database Technology
- **Primary Database**: MongoDB 7.0+
- **Message Queue**: RabbitMQ 3.12+
- **Cache**: Redis 7.0+ (optional)
- **File Storage**: Local File System hoặc Azure Blob Storage
- **Search Engine**: MongoDB Atlas Search (optional)

### Database Architecture
```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
├─────────────────────────────────────────────────────────────┤
│  MongoDB Driver 2.28+                                      │
│  - Connection Pooling                                      │
│  - Change Streams                                          │
│  - Aggregation Pipelines                                   │
│  - GridFS (for large files)                               │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Database Layer                           │
├─────────────────────────────────────────────────────────────┤
│  MongoDB 7.0+                                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │   Core      │  │   System    │  │   Analytics │        │
│  │ Collections │  │ Collections │  │ Collections │        │
│  │             │  │             │  │             │        │
│  │ - Libraries │  │ - Settings  │  │ - AuditLogs │        │
│  │ - Collections│  │ - Jobs     │  │ - Metrics   │        │
│  │ - MediaItems│  │ - Watchers  │  │ - Sessions  │        │
│  │ - FavoriteLists│ - Users    │  │ - Errors    │        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
└─────────────────────────────────────────────────────────────┘
```

## Core Collections

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

### 14. User Behavior Events Collection
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

### 15. User Analytics Collection
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

### 16. Content Popularity Collection
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

### 17. Search Analytics Collection
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

### 18. Performance Metrics Collection
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

## Social & Sharing Features Collections

### 19. User Collections Collection
```javascript
{
  _id: ObjectId,
  userId: String, // Owner of the collection
  title: String,
  description: String,
  category: String, // "photography", "art", "nature", "portrait", "landscape", "abstract", "other"
  tags: [String],
  visibility: String, // "public", "private", "friends", "followers"
  status: String, // "draft", "published", "archived", "banned"
  mediaItems: [{
    mediaId: ObjectId,
    order: Number,
    addedAt: Date,
    notes: String
  }],
  metadata: {
    totalItems: Number,
    totalSize: Number,
    averageRating: Number,
    totalRatings: Number,
    totalViews: Number,
    totalDownloads: Number,
    totalShares: Number,
    totalComments: Number,
    totalLikes: Number,
    totalFavorites: Number
  },
  settings: {
    allowComments: Boolean,
    allowDownloads: Boolean,
    allowSharing: Boolean,
    allowRating: Boolean,
    downloadQuality: String, // "original", "high", "medium", "low"
    watermarkEnabled: Boolean,
    copyrightNotice: String
  },
  uploadInfo: {
    originalSource: String, // "user_upload", "imported", "shared"
    uploadDate: Date,
    uploadMethod: String, // "web", "api", "bulk", "sync"
    fileCount: Number,
    totalSize: Number,
    compressionRatio: Number
  },
  moderation: {
    isModerated: Boolean,
    moderatedBy: String,
    moderatedAt: Date,
    moderationNotes: String,
    flags: [{
      reason: String,
      reportedBy: String,
      reportedAt: Date,
      status: String // "pending", "resolved", "dismissed"
    }]
  },
  createdAt: Date,
  updatedAt: Date,
  publishedAt: Date
}
```

### 20. Collection Ratings Collection
```javascript
{
  _id: ObjectId,
  collectionId: ObjectId,
  userId: String,
  rating: Number, // 1-5 stars
  review: String,
  aspects: {
    quality: Number, // 1-5
    originality: Number, // 1-5
    composition: Number, // 1-5
    technical: Number, // 1-5
    creativity: Number // 1-5
  },
  helpful: {
    helpfulCount: Number,
    notHelpfulCount: Number,
    helpfulUsers: [String] // users who marked as helpful
  },
  status: String, // "active", "hidden", "deleted"
  createdAt: Date,
  updatedAt: Date
}
```

### 21. User Follows Collection
```javascript
{
  _id: ObjectId,
  followerId: String, // User who follows
  followingId: String, // User being followed
  followType: String, // "user", "collection", "tag"
  targetId: ObjectId, // ID of the target (user, collection, or tag)
  notifications: {
    newCollections: Boolean,
    newRatings: Boolean,
    newComments: Boolean,
    newFollowers: Boolean
  },
  status: String, // "active", "muted", "blocked"
  createdAt: Date,
  updatedAt: Date
}
```

### 22. Collection Comments Collection
```javascript
{
  _id: ObjectId,
  collectionId: ObjectId,
  userId: String,
  parentCommentId: ObjectId, // For replies
  content: String,
  mentions: [String], // @username mentions
  attachments: [{
    type: String, // "image", "link", "file"
    url: String,
    filename: String,
    size: Number
  }],
  reactions: {
    likes: [String], // user IDs who liked
    dislikes: [String], // user IDs who disliked
    love: [String], // user IDs who loved
    laugh: [String], // user IDs who laughed
    angry: [String] // user IDs who got angry
  },
  moderation: {
    isModerated: Boolean,
    moderatedBy: String,
    moderatedAt: Date,
    moderationReason: String,
    isHidden: Boolean
  },
  status: String, // "active", "hidden", "deleted", "spam"
  createdAt: Date,
  updatedAt: Date,
  editedAt: Date
}
```

### 23. User Messages Collection
```javascript
{
  _id: ObjectId,
  senderId: String,
  recipientId: String,
  conversationId: ObjectId,
  messageType: String, // "text", "image", "file", "collection_share", "system"
  content: String,
  attachments: [{
    type: String, // "image", "file", "collection"
    url: String,
    filename: String,
    size: Number,
    metadata: Object
  }],
  sharedCollection: {
    collectionId: ObjectId,
    title: String,
    thumbnail: String,
    description: String
  },
  status: {
    sent: Boolean,
    delivered: Boolean,
    read: Boolean,
    readAt: Date
  },
  reactions: [{
    userId: String,
    emoji: String,
    createdAt: Date
  }],
  replyTo: ObjectId, // Reply to another message
  isEdited: Boolean,
  editedAt: Date,
  createdAt: Date
}
```

### 24. Conversations Collection
```javascript
{
  _id: ObjectId,
  type: String, // "direct", "group"
  participants: [{
    userId: String,
    role: String, // "admin", "member", "moderator"
    joinedAt: Date,
    lastReadAt: Date,
    notifications: Boolean
  }],
  name: String, // For group chats
  description: String, // For group chats
  avatar: String, // For group chats
  settings: {
    allowInvites: Boolean,
    allowFileSharing: Boolean,
    allowCollectionSharing: Boolean,
    maxParticipants: Number
  },
  lastMessage: {
    messageId: ObjectId,
    senderId: String,
    content: String,
    timestamp: Date
  },
  status: String, // "active", "archived", "deleted"
  createdAt: Date,
  updatedAt: Date
}
```

### 25. Torrents Collection
```javascript
{
  _id: ObjectId,
  collectionId: ObjectId,
  userId: String, // Creator of the torrent
  infoHash: String, // Unique torrent identifier
  name: String,
  description: String,
  category: String, // "images", "videos", "mixed", "art", "photography"
  tags: [String],
  files: [{
    name: String,
    size: Number,
    path: String,
    hash: String
  }],
  totalSize: Number,
  pieceSize: Number,
  pieceCount: Number,
  trackers: [String], // Tracker URLs
  magnetLink: String,
  torrentFile: {
    url: String,
    size: Number,
    uploadedAt: Date
  },
  statistics: {
    seeders: Number,
    leechers: Number,
    completed: Number,
    totalDownloads: Number,
    lastUpdate: Date
  },
  quality: {
    resolution: String, // "4K", "1080p", "720p", "480p", "original"
    compression: String, // "lossless", "high", "medium", "low"
    format: String // "zip", "rar", "7z", "tar"
  },
  status: String, // "active", "paused", "completed", "failed", "banned"
  moderation: {
    isModerated: Boolean,
    moderatedBy: String,
    moderatedAt: Date,
    moderationReason: String
  },
  createdAt: Date,
  updatedAt: Date
}
```

### 26. Download Links Collection
```javascript
{
  _id: ObjectId,
  collectionId: ObjectId,
  userId: String, // Creator of the link
  title: String,
  description: String,
  links: [{
    provider: String, // "mega", "google_drive", "dropbox", "onedrive", "mediafire", "zippyshare"
    url: String,
    quality: String, // "original", "high", "medium", "low"
    format: String, // "zip", "rar", "7z", "tar"
    size: Number,
    password: String, // Encrypted password if needed
    expiresAt: Date,
    maxDownloads: Number,
    currentDownloads: Number
  }],
  mirrors: [{
    provider: String,
    url: String,
    status: String, // "active", "dead", "checking"
    lastChecked: Date,
    checkCount: Number
  }],
  statistics: {
    totalDownloads: Number,
    uniqueDownloaders: Number,
    lastDownload: Date,
    averageDownloadTime: Number
  },
  health: {
    isHealthy: Boolean,
    lastHealthCheck: Date,
    deadLinks: Number,
    totalLinks: Number,
    healthScore: Number // 0-100
  },
  status: String, // "active", "paused", "expired", "banned"
  createdAt: Date,
  updatedAt: Date
}
```

### 27. Torrent Statistics Collection
```javascript
{
  _id: ObjectId,
  torrentId: ObjectId,
  peerId: String, // Unique peer identifier
  userId: String, // User who is seeding/leeching
  ipAddress: String,
  port: Number,
  status: String, // "seeding", "leeching", "completed", "stopped"
  statistics: {
    uploaded: Number, // bytes uploaded
    downloaded: Number, // bytes downloaded
    left: Number, // bytes left to download
    speed: {
      upload: Number, // bytes per second
      download: Number // bytes per second
    },
    ratio: Number, // upload/download ratio
    connectedTime: Number, // seconds connected
    lastAnnounce: Date
  },
  client: {
    name: String, // "qBittorrent", "Transmission", "Deluge", etc.
    version: String
  },
  location: {
    country: String,
    region: String,
    city: String
  },
  createdAt: Date,
  updatedAt: Date,
  lastSeen: Date
}
```

### 28. Link Health Checker Collection
```javascript
{
  _id: ObjectId,
  linkId: ObjectId,
  url: String,
  provider: String,
  checkResults: [{
    timestamp: Date,
    status: String, // "active", "dead", "slow", "error"
    responseTime: Number, // milliseconds
    httpStatus: Number,
    errorMessage: String,
    fileSize: Number,
    contentType: String,
    lastModified: Date
  }],
  health: {
    isHealthy: Boolean,
    healthScore: Number, // 0-100
    averageResponseTime: Number,
    uptime: Number, // percentage
    lastHealthyCheck: Date,
    consecutiveFailures: Number
  },
  settings: {
    checkInterval: Number, // minutes
    timeout: Number, // seconds
    retryCount: Number,
    alertThreshold: Number // consecutive failures before alert
  },
  alerts: [{
    type: String, // "dead_link", "slow_response", "high_failure_rate"
    message: String,
    timestamp: Date,
    resolved: Boolean,
    resolvedAt: Date
  }],
  createdAt: Date,
  updatedAt: Date
}
```

### 29. Download Quality Options Collection
```javascript
{
  _id: ObjectId,
  collectionId: ObjectId,
  quality: String, // "original", "4K", "1080p", "720p", "480p", "360p"
  format: String, // "zip", "rar", "7z", "tar", "individual"
  compression: String, // "lossless", "high", "medium", "low"
  settings: {
    maxWidth: Number,
    maxHeight: Number,
    quality: Number, // 1-100 for JPEG
    compressionLevel: Number, // 1-9 for ZIP
    watermark: Boolean,
    watermarkText: String,
    watermarkPosition: String // "top-left", "top-right", "bottom-left", "bottom-right", "center"
  },
  fileInfo: {
    totalSize: Number,
    fileCount: Number,
    estimatedDownloadTime: Number, // seconds
    bandwidth: String // "low", "medium", "high", "unlimited"
  },
  availability: {
    isAvailable: Boolean,
    generatedAt: Date,
    expiresAt: Date,
    downloadCount: Number,
    maxDownloads: Number
  },
  sources: [{
    type: String, // "torrent", "direct_link", "cdn", "mirror"
    url: String,
    priority: Number, // 1-10, higher is better
    status: String, // "active", "slow", "dead"
    lastChecked: Date
  }],
  createdAt: Date,
  updatedAt: Date
}
```

### 30. Distribution Nodes Collection
```javascript
{
  _id: ObjectId,
  nodeId: String, // Unique node identifier
  userId: String, // User who registered as node
  name: String,
  description: String,
  location: {
    country: String,
    region: String,
    city: String,
    coordinates: {
      latitude: Number,
      longitude: Number
    }
  },
  capabilities: {
    maxStorage: Number, // bytes
    maxBandwidth: Number, // bytes per second
    supportedFormats: [String], // ["images", "videos", "archives"]
    maxConcurrentDownloads: Number,
    availableHours: [Number], // 0-23 hours when node is available
    timezone: String
  },
  performance: {
    uptime: Number, // percentage
    averageResponseTime: Number, // milliseconds
    totalDataServed: Number, // bytes
    totalDownloads: Number,
    averageDownloadSpeed: Number, // bytes per second
    errorRate: Number, // percentage
    lastPerformanceCheck: Date
  },
  quality: {
    score: Number, // 0-100 overall quality score
    reliability: Number, // 0-100
    speed: Number, // 0-100
    availability: Number, // 0-100
    userSatisfaction: Number, // 0-100 average user rating
    lastQualityCheck: Date
  },
  status: String, // "active", "inactive", "maintenance", "banned", "pending_approval"
  collections: [{
    collectionId: ObjectId,
    assignedAt: Date,
    priority: Number, // 1-10
    status: String, // "active", "syncing", "synced", "failed"
    lastSync: Date,
    syncProgress: Number // 0-100
  }],
  monitoring: {
    lastHeartbeat: Date,
    heartbeatInterval: Number, // seconds
    consecutiveFailures: Number,
    lastError: String,
    lastErrorAt: Date
  },
  rewards: {
    totalEarned: Number, // points or credits earned
    currentLevel: String, // "bronze", "silver", "gold", "platinum"
    badges: [String], // achievement badges
    lastReward: Date
  },
  createdAt: Date,
  updatedAt: Date,
  lastActive: Date
}
```

### 31. Node Performance Metrics Collection
```javascript
{
  _id: ObjectId,
  nodeId: ObjectId,
  timestamp: Date,
  metrics: {
    cpu: {
      usage: Number, // percentage
      cores: Number,
      load: Number
    },
    memory: {
      total: Number, // bytes
      used: Number, // bytes
      available: Number, // bytes
      usage: Number // percentage
    },
    disk: {
      total: Number, // bytes
      used: Number, // bytes
      available: Number, // bytes
      usage: Number, // percentage
      readSpeed: Number, // bytes per second
      writeSpeed: Number // bytes per second
    },
    network: {
      uploadSpeed: Number, // bytes per second
      downloadSpeed: Number, // bytes per second
      latency: Number, // milliseconds
      packetLoss: Number // percentage
    }
  },
  performance: {
    activeConnections: Number,
    totalRequests: Number,
    successfulRequests: Number,
    failedRequests: Number,
    averageResponseTime: Number, // milliseconds
    throughput: Number, // requests per second
    errorRate: Number // percentage
  },
  collections: {
    totalAssigned: Number,
    activeSyncs: Number,
    completedSyncs: Number,
    failedSyncs: Number,
    totalDataServed: Number, // bytes
    averageDownloadSpeed: Number // bytes per second
  },
  environment: {
    os: String,
    osVersion: String,
    nodeVersion: String,
    uptime: Number, // seconds
    timezone: String
  },
  createdAt: Date
}
```

## Reward System Collections

### 32. User Rewards Collection
```javascript
{
  _id: ObjectId,
  userId: String,
  currentPoints: Number, // Current available points
  totalEarned: Number, // Total points earned (lifetime)
  totalSpent: Number, // Total points spent (lifetime)
  level: String, // "bronze", "silver", "gold", "platinum", "diamond"
  badges: [{
    badgeId: String,
    badgeName: String,
    earnedAt: Date,
    description: String,
    icon: String
  }],
  achievements: [{
    achievementId: String,
    achievementName: String,
    earnedAt: Date,
    points: Number,
    description: String
  }],
  statistics: {
    uploads: {
      totalCollections: Number,
      totalPoints: Number,
      lastUpload: Date
    },
    seeding: {
      totalSeeded: Number, // bytes
      totalPoints: Number,
      averageRatio: Number,
      lastSeeded: Date
    },
    nodeOperation: {
      totalUptime: Number, // hours
      totalPoints: Number,
      averageQuality: Number,
      lastActive: Date
    },
    tagCreation: {
      totalTags: Number,
      totalPoints: Number,
      lastCreated: Date
    },
    torrentCreation: {
      totalTorrents: Number,
      totalPoints: Number,
      lastCreated: Date
    },
    social: {
      totalComments: Number,
      totalLikes: Number,
      totalShares: Number,
      totalPoints: Number
    }
  },
  preferences: {
    autoSpend: Boolean, // Auto-spend points for premium downloads
    notifications: {
      pointsEarned: Boolean,
      pointsSpent: Boolean,
      levelUp: Boolean,
      achievementUnlocked: Boolean
    }
  },
  createdAt: Date,
  updatedAt: Date,
  lastActivity: Date
}
```

### 33. Reward Transactions Collection
```javascript
{
  _id: ObjectId,
  userId: String,
  transactionType: String, // "earn", "spend", "bonus", "penalty", "refund"
  category: String, // "upload", "seeding", "node", "tag", "torrent", "social", "download", "premium"
  action: String, // "collection_upload", "torrent_seed", "node_operation", "tag_create", "premium_download"
  points: Number, // Positive for earning, negative for spending
  description: String,
  metadata: {
    // Upload related
    collectionId: ObjectId,
    collectionTitle: String,
    fileCount: Number,
    totalSize: Number,
    
    // Seeding related
    torrentId: ObjectId,
    torrentName: String,
    seededBytes: Number,
    ratio: Number,
    duration: Number, // hours
    
    // Node related
    nodeId: ObjectId,
    nodeName: String,
    uptime: Number, // hours
    qualityScore: Number,
    dataServed: Number, // bytes
    
    // Tag related
    tagName: String,
    tagUsage: Number, // times used by others
    
    // Torrent related
    torrentSize: Number,
    downloadCount: Number,
    
    // Social related
    targetId: ObjectId, // collection, comment, etc.
    targetType: String,
    
    // Download related
    downloadQuality: String,
    downloadSize: Number,
    downloadSpeed: Number,
    
    // Premium features
    featureType: String,
    duration: Number, // hours/days
    priority: Number
  },
  multiplier: Number, // Bonus multiplier (1.0 = normal, 1.5 = 50% bonus)
  bonusReason: String, // Reason for bonus multiplier
  status: String, // "pending", "completed", "cancelled", "refunded"
  expiresAt: Date, // For time-limited rewards
  processedAt: Date,
  createdAt: Date
}
```

### 34. Reward Settings Collection
```javascript
{
  _id: ObjectId,
  settingType: String, // "earning", "spending", "level", "bonus", "penalty"
  category: String, // "upload", "seeding", "node", "tag", "torrent", "social", "download"
  action: String, // Specific action name
  settings: {
    // Earning settings
    basePoints: Number, // Base points for action
    multipliers: {
      quality: {
        high: Number, // 1.5 for high quality content
        medium: Number, // 1.0 for medium quality
        low: Number // 0.5 for low quality
      },
      popularity: {
        viral: Number, // 2.0 for viral content
        popular: Number, // 1.5 for popular content
        normal: Number // 1.0 for normal content
      },
      time: {
        peak: Number, // 1.2 for peak hours
        normal: Number // 1.0 for normal hours
      },
      user: {
        newUser: Number, // 1.5 for new users (first 30 days)
        regular: Number, // 1.0 for regular users
        premium: Number // 1.2 for premium users
      }
    },
    limits: {
      daily: Number, // Max points per day
      weekly: Number, // Max points per week
      monthly: Number, // Max points per month
      perAction: Number // Max points per single action
    },
    requirements: {
      minQuality: Number, // Minimum quality score
      minSize: Number, // Minimum file size
      minDuration: Number, // Minimum duration for seeding
      minUptime: Number // Minimum uptime for nodes
    }
  },
  // Spending settings
  spending: {
    baseCost: Number, // Base cost in points
    qualityMultipliers: {
      original: Number, // 1.0 for original quality
      high: Number, // 0.8 for high quality
      medium: Number, // 0.6 for medium quality
      low: Number // 0.4 for low quality
    },
    speedMultipliers: {
      fast: Number, // 1.5 for fast download
      normal: Number, // 1.0 for normal speed
      slow: Number // 0.5 for slow speed
    },
    priorityMultipliers: {
      high: Number, // 2.0 for high priority
      normal: Number, // 1.0 for normal priority
      low: Number // 0.5 for low priority
    }
  },
  // Level settings
  levels: {
    bronze: {
      minPoints: Number,
      maxPoints: Number,
      benefits: [String],
      multiplier: Number
    },
    silver: {
      minPoints: Number,
      maxPoints: Number,
      benefits: [String],
      multiplier: Number
    },
    gold: {
      minPoints: Number,
      maxPoints: Number,
      benefits: [String],
      multiplier: Number
    },
    platinum: {
      minPoints: Number,
      maxPoints: Number,
      benefits: [String],
      multiplier: Number
    },
    diamond: {
      minPoints: Number,
      maxPoints: Number,
      benefits: [String],
      multiplier: Number
    }
  },
  // Bonus settings
  bonuses: {
    firstUpload: Number, // Bonus for first collection upload
    milestoneUploads: [{
      count: Number,
      bonus: Number
    }],
    consecutiveDays: [{
      days: Number,
      bonus: Number
    }],
    referral: Number, // Bonus for referring new users
    anniversary: Number // Bonus for account anniversary
  },
  // Penalty settings
  penalties: {
    lowQuality: Number, // Penalty for low quality content
    spam: Number, // Penalty for spam
    abuse: Number, // Penalty for abuse
    inactive: Number // Penalty for inactive accounts
  },
  isActive: Boolean,
  effectiveFrom: Date,
  effectiveTo: Date,
  createdBy: String,
  createdAt: Date,
  updatedAt: Date
}
```

### 35. Reward Achievements Collection
```javascript
{
  _id: ObjectId,
  achievementId: String, // Unique identifier
  name: String,
  description: String,
  category: String, // "upload", "seeding", "node", "social", "milestone"
  type: String, // "single", "cumulative", "streak", "milestone"
  icon: String, // Icon URL or name
  rarity: String, // "common", "uncommon", "rare", "epic", "legendary"
  points: Number, // Points awarded
  requirements: {
    // Upload achievements
    uploads: {
      totalCollections: Number,
      totalSize: Number, // bytes
      categories: [String],
      quality: Number // minimum average rating
    },
    // Seeding achievements
    seeding: {
      totalSeeded: Number, // bytes
      totalRatio: Number,
      consecutiveDays: Number,
      maxRatio: Number
    },
    // Node achievements
    node: {
      totalUptime: Number, // hours
      averageQuality: Number,
      totalDataServed: Number, // bytes
      consecutiveDays: Number
    },
    // Social achievements
    social: {
      totalComments: Number,
      totalLikes: Number,
      totalShares: Number,
      totalFollowers: Number,
      helpfulRatings: Number
    },
    // Milestone achievements
    milestone: {
      totalPoints: Number,
      level: String,
      consecutiveDays: Number,
      totalDownloads: Number
    }
  },
  rewards: {
    points: Number,
    badges: [String],
    benefits: [String], // Special benefits unlocked
    multiplier: Number // Permanent multiplier bonus
  },
  isActive: Boolean,
  isHidden: Boolean, // Hidden until unlocked
  createdAt: Date,
  updatedAt: Date
}
```

### 36. Reward Badges Collection
```javascript
{
  _id: ObjectId,
  badgeId: String, // Unique identifier
  name: String,
  description: String,
  category: String, // "upload", "seeding", "node", "social", "special"
  type: String, // "earned", "purchased", "special", "seasonal"
  icon: String, // Icon URL or name
  rarity: String, // "common", "uncommon", "rare", "epic", "legendary"
  requirements: {
    // How to earn this badge
    action: String, // "upload_collection", "seed_torrent", "operate_node", etc.
    criteria: {
      count: Number, // Number of actions required
      quality: Number, // Minimum quality required
      duration: Number, // Duration required (for streaks)
      size: Number // Size requirement
    }
  },
  benefits: {
    pointsMultiplier: Number, // Permanent multiplier
    specialAccess: [String], // Special features unlocked
    displayPriority: Number // Higher number = more prominent display
  },
  isActive: Boolean,
  isLimited: Boolean, // Limited time badge
  availableFrom: Date,
  availableTo: Date,
  createdAt: Date,
  updatedAt: Date
}
```

### 37. Premium Features Collection
```javascript
{
  _id: ObjectId,
  featureId: String, // Unique identifier
  name: String,
  description: String,
  category: String, // "download", "upload", "social", "analytics", "customization"
  type: String, // "one_time", "duration", "permanent"
  pricing: {
    points: Number, // Cost in points
    duration: Number, // Duration in hours/days (for duration-based features)
    maxUses: Number, // Maximum uses (for one-time features)
    cooldown: Number // Cooldown period in hours
  },
  features: {
    // Download features
    download: {
      quality: [String], // Available qualities
      speed: String, // "fast", "normal", "slow"
      priority: String, // "high", "normal", "low"
      bandwidth: Number, // Bandwidth limit in bytes/second
      concurrent: Number // Concurrent download limit
    },
    // Upload features
    upload: {
      maxSize: Number, // Maximum upload size
      maxFiles: Number, // Maximum files per upload
      priority: String, // Processing priority
      watermark: Boolean, // Custom watermark
      analytics: Boolean // Advanced analytics
    },
    // Social features
    social: {
      customProfile: Boolean, // Custom profile page
      advancedStats: Boolean, // Advanced statistics
      prioritySupport: Boolean, // Priority customer support
      exclusiveContent: Boolean // Access to exclusive content
    },
    // Analytics features
    analytics: {
      detailedStats: Boolean, // Detailed statistics
      exportData: Boolean, // Export data capability
      customReports: Boolean, // Custom report generation
      realTimeData: Boolean // Real-time data access
    }
  },
  requirements: {
    minLevel: String, // Minimum user level required
    minPoints: Number, // Minimum points required
    achievements: [String] // Required achievements
  },
  isActive: Boolean,
  isPopular: Boolean, // Featured feature
  createdAt: Date,
  updatedAt: Date
}
```

### 38. User Premium Features Collection
```javascript
{
  _id: ObjectId,
  userId: String,
  featureId: ObjectId,
  featureName: String,
  type: String, // "one_time", "duration", "permanent"
  status: String, // "active", "expired", "cancelled", "pending"
  purchasedAt: Date,
  activatedAt: Date,
  expiresAt: Date,
  pointsSpent: Number,
  usage: {
    totalUses: Number,
    remainingUses: Number,
    lastUsed: Date,
    usageHistory: [{
      usedAt: Date,
      action: String,
      metadata: Object
    }]
  },
  settings: {
    // User-specific settings for this feature
    preferences: Object,
    limits: Object,
    customizations: Object
  },
  createdAt: Date,
  updatedAt: Date
}
```

## Enhanced Settings & Storage Management Collections

### 39. System Settings Collection
```javascript
{
  _id: ObjectId,
  settingKey: String, // Unique key for the setting
  category: String, // "storage", "performance", "security", "features", "monitoring", "backup"
  subcategory: String, // "local", "cdn", "cache", "compression", "encryption"
  name: String, // Human-readable name
  description: String, // Detailed description
  value: Object, // The actual setting value
  dataType: String, // "string", "number", "boolean", "object", "array"
  defaultValue: Object, // Default value
  validation: {
    min: Number,
    max: Number,
    pattern: String, // Regex pattern
    required: Boolean,
    options: [Object] // Valid options for enum types
  },
  storage: {
    // Storage configuration
    defaultProvider: String, // "local", "aws_s3", "azure_blob", "google_cloud", "cdn"
    providers: [{
      provider: String,
      enabled: Boolean,
      priority: Number, // 1-10, higher is better
      configuration: {
        endpoint: String,
        accessKey: String,
        secretKey: String,
        bucket: String,
        region: String,
        path: String
      },
      limits: {
        maxFileSize: Number,
        maxFiles: Number,
        maxStorage: Number,
        allowedTypes: [String]
      },
      performance: {
        uploadSpeed: Number, // bytes per second
        downloadSpeed: Number, // bytes per second
        latency: Number, // milliseconds
        reliability: Number // percentage
      }
    }],
    replication: {
      enabled: Boolean,
      minReplicas: Number,
      maxReplicas: Number,
      strategy: String, // "immediate", "delayed", "scheduled"
      providers: [String] // Which providers to replicate to
    },
    compression: {
      enabled: Boolean,
      algorithm: String, // "gzip", "brotli", "lz4", "zstd"
      level: Number, // 1-9
      minSize: Number, // Minimum size to compress
      types: [String] // File types to compress
    },
    encryption: {
      enabled: Boolean,
      algorithm: String, // "AES-256", "ChaCha20"
      keyRotation: Boolean,
      keyRotationInterval: Number // days
    }
  },
  performance: {
    // Performance settings
    cache: {
      enabled: Boolean,
      ttl: Number, // seconds
      maxSize: Number, // bytes
      strategy: String // "LRU", "LFU", "FIFO"
    },
    compression: {
      enabled: Boolean,
      level: Number,
      threshold: Number // Minimum size to compress
    },
    rateLimiting: {
      enabled: Boolean,
      requestsPerMinute: Number,
      requestsPerHour: Number,
      requestsPerDay: Number
    }
  },
  security: {
    // Security settings
    authentication: {
      enabled: Boolean,
      methods: [String], // "password", "oauth", "saml", "ldap"
      sessionTimeout: Number, // minutes
      maxLoginAttempts: Number,
      lockoutDuration: Number // minutes
    },
    authorization: {
      enabled: Boolean,
      defaultRole: String,
      roles: [{
        name: String,
        permissions: [String],
        description: String
      }]
    },
    encryption: {
      enabled: Boolean,
      algorithm: String,
      keySize: Number,
      rotationInterval: Number // days
    }
  },
  monitoring: {
    // Monitoring settings
    logging: {
      enabled: Boolean,
      level: String, // "debug", "info", "warn", "error"
      retention: Number, // days
      maxSize: Number, // bytes
      compression: Boolean
    },
    metrics: {
      enabled: Boolean,
      interval: Number, // seconds
      retention: Number, // days
      alerts: [{
        metric: String,
        threshold: Number,
        operator: String, // "gt", "lt", "eq", "gte", "lte"
        action: String // "email", "sms", "webhook"
      }]
    },
    healthChecks: {
      enabled: Boolean,
      interval: Number, // seconds
      timeout: Number, // seconds
      endpoints: [String]
    }
  },
  backup: {
    // Backup settings
    enabled: Boolean,
    schedule: String, // Cron expression
    retention: Number, // days
    compression: Boolean,
    encryption: Boolean,
    destinations: [{
      type: String, // "local", "s3", "azure", "google"
      path: String,
      configuration: Object
    }]
  },
  isActive: Boolean,
  isReadOnly: Boolean, // System-managed settings
  requiresRestart: Boolean, // Whether setting requires restart
  effectiveFrom: Date,
  effectiveTo: Date,
  createdBy: String,
  updatedBy: String,
  createdAt: Date,
  updatedAt: Date
}
```

### 40. User Settings Collection
```javascript
{
  _id: ObjectId,
  userId: String,
  category: String, // "display", "upload", "download", "social", "privacy", "notifications", "storage"
  settings: {
    // Display settings
    display: {
      theme: String, // "light", "dark", "auto"
      language: String, // "en", "vi", "zh", "ja"
      timezone: String, // "UTC", "Asia/Ho_Chi_Minh"
      dateFormat: String, // "DD/MM/YYYY", "MM/DD/YYYY", "YYYY-MM-DD"
      timeFormat: String, // "12h", "24h"
      itemsPerPage: Number, // 10, 20, 50, 100
      gridSize: String, // "small", "medium", "large"
      showThumbnails: Boolean,
      showMetadata: Boolean,
      showFileSize: Boolean,
      showUploadDate: Boolean,
      sortBy: String, // "name", "date", "size", "rating"
      sortOrder: String // "asc", "desc"
    },
    
    // Upload settings
    upload: {
      defaultStorage: String, // "local", "cdn", "auto"
      preferredProviders: [String], // Ordered list of preferred providers
      autoCompress: Boolean,
      compressionLevel: Number, // 1-9
      maxFileSize: Number, // bytes
      maxFiles: Number,
      allowedTypes: [String], // File extensions
      watermark: {
        enabled: Boolean,
        text: String,
        position: String, // "top-left", "top-right", "bottom-left", "bottom-right", "center"
        opacity: Number, // 0-1
        fontSize: Number,
        color: String
      },
      metadata: {
        extractExif: Boolean,
        extractIptc: Boolean,
        extractXmp: Boolean,
        generateThumbnails: Boolean,
        thumbnailSize: Number, // pixels
        thumbnailQuality: Number // 1-100
      },
      processing: {
        priority: String, // "low", "normal", "high"
        background: Boolean,
        notifyOnComplete: Boolean
      }
    },
    
    // Download settings
    download: {
      defaultQuality: String, // "original", "high", "medium", "low"
      defaultFormat: String, // "zip", "rar", "7z", "individual"
      defaultSpeed: String, // "fast", "normal", "slow"
      autoDownload: Boolean,
      downloadLocation: String, // Local path
      maxConcurrent: Number,
      resumeDownloads: Boolean,
      verifyChecksums: Boolean,
      extractArchives: Boolean
    },
    
    // Social settings
    social: {
      profileVisibility: String, // "public", "friends", "private"
      showOnlineStatus: Boolean,
      allowMessages: Boolean,
      allowComments: Boolean,
      allowRatings: Boolean,
      allowShares: Boolean,
      showActivity: Boolean,
      friendRequests: String, // "auto", "manual", "disabled"
      blockList: [String] // Blocked user IDs
    },
    
    // Privacy settings
    privacy: {
      dataCollection: Boolean,
      analytics: Boolean,
      personalizedAds: Boolean,
      locationTracking: Boolean,
      searchHistory: Boolean,
      downloadHistory: Boolean,
      shareData: Boolean,
      anonymizeData: Boolean
    },
    
    // Notification settings
    notifications: {
      email: {
        enabled: Boolean,
        frequency: String, // "immediate", "daily", "weekly", "disabled"
        types: [String] // "upload_complete", "new_follower", "new_comment", "system_update"
      },
      push: {
        enabled: Boolean,
        types: [String]
      },
      inApp: {
        enabled: Boolean,
        types: [String]
      },
      sms: {
        enabled: Boolean,
        types: [String]
      }
    },
    
    // Storage settings
    storage: {
      preferredProviders: [String], // Ordered list
      autoBackup: Boolean,
      backupFrequency: String, // "daily", "weekly", "monthly"
      backupRetention: Number, // days
      syncSettings: {
        enabled: Boolean,
        devices: [String], // Device IDs
        realTime: Boolean
      },
      quota: {
        maxStorage: Number, // bytes
        warningThreshold: Number, // percentage
        autoCleanup: Boolean,
        cleanupStrategy: String // "oldest", "largest", "least_used"
      }
    },
    
    // Advanced settings
    advanced: {
      apiAccess: Boolean,
      apiKey: String,
      webhooks: [{
        url: String,
        events: [String],
        secret: String,
        enabled: Boolean
      }],
      customCSS: String,
      customJS: String,
      experimentalFeatures: Boolean,
      debugMode: Boolean
    }
  },
  preferences: {
    // User preferences
    autoSave: Boolean,
    autoSync: Boolean,
    rememberLastLocation: Boolean,
    showTutorials: Boolean,
    showTips: Boolean,
    keyboardShortcuts: Boolean,
    mouseGestures: Boolean
  },
  lastUpdated: Date,
  version: Number, // Settings version for migration
  createdAt: Date,
  updatedAt: Date
}
```

### 41. Storage Locations Collection
```javascript
{
  _id: ObjectId,
  locationId: String, // Unique identifier
  name: String,
  description: String,
  type: String, // "local", "cdn", "cloud", "hybrid"
  provider: String, // "local", "aws_s3", "azure_blob", "google_cloud", "cloudflare", "custom"
  status: String, // "active", "inactive", "maintenance", "error"
  configuration: {
    endpoint: String,
    accessKey: String,
    secretKey: String,
    bucket: String,
    region: String,
    path: String,
    customHeaders: Object,
    ssl: Boolean,
    timeout: Number // seconds
  },
  capabilities: {
    maxFileSize: Number, // bytes
    maxFiles: Number,
    maxStorage: Number, // bytes
    allowedTypes: [String], // File extensions
    supportedOperations: [String], // "read", "write", "delete", "list", "copy"
    compression: Boolean,
    encryption: Boolean,
    versioning: Boolean,
    lifecycle: Boolean
  },
  performance: {
    uploadSpeed: Number, // bytes per second
    downloadSpeed: Number, // bytes per second
    latency: Number, // milliseconds
    throughput: Number, // requests per second
    reliability: Number, // percentage
    availability: Number, // percentage
    lastTested: Date
  },
  costs: {
    storage: Number, // Cost per GB per month
    bandwidth: Number, // Cost per GB
    requests: Number, // Cost per 1000 requests
    currency: String // "USD", "EUR", "VND"
  },
  location: {
    country: String,
    region: String,
    city: String,
    coordinates: {
      latitude: Number,
      longitude: Number
    },
    timezone: String
  },
  health: {
    isHealthy: Boolean,
    lastHealthCheck: Date,
    consecutiveFailures: Number,
    averageResponseTime: Number,
    errorRate: Number,
    uptime: Number // percentage
  },
  usage: {
    totalFiles: Number,
    totalSize: Number, // bytes
    totalRequests: Number,
    lastAccess: Date,
    peakUsage: Number, // bytes
    averageUsage: Number // bytes
  },
  monitoring: {
    alerts: [{
      type: String, // "error", "warning", "info"
      message: String,
      timestamp: Date,
      resolved: Boolean,
      resolvedAt: Date
    }],
    metrics: [{
      timestamp: Date,
      metric: String,
      value: Number,
      unit: String
    }]
  },
  isDefault: Boolean,
  priority: Number, // 1-10, higher is better
  createdAt: Date,
  updatedAt: Date
}
```

### 42. File Storage Mapping Collection
```javascript
{
  _id: ObjectId,
  fileId: ObjectId, // Reference to media item or collection
  fileType: String, // "media", "collection", "thumbnail", "cache", "backup"
  originalPath: String, // Original file path
  fileName: String,
  fileSize: Number, // bytes
  fileHash: String, // SHA-256 hash
  mimeType: String,
  storageLocations: [{
    locationId: ObjectId,
    provider: String,
    path: String, // Full path on the storage provider
    url: String, // Access URL
    status: String, // "active", "syncing", "synced", "failed", "deleted"
    priority: Number, // 1-10, higher is better
    isPrimary: Boolean, // Primary storage location
    isReplica: Boolean, // Replica storage location
    uploadedAt: Date,
    lastAccessed: Date,
    accessCount: Number,
    checksum: String, // File checksum for verification
    metadata: {
      compression: Boolean,
      encryption: Boolean,
      version: Number,
      tags: [String]
    }
  }],
  replication: {
    enabled: Boolean,
    strategy: String, // "immediate", "delayed", "scheduled"
    minReplicas: Number,
    maxReplicas: Number,
    currentReplicas: Number,
    lastReplication: Date,
    nextReplication: Date
  },
  access: {
    public: Boolean,
    authenticated: Boolean,
    permissions: [{
      userId: String,
      permission: String, // "read", "write", "delete", "admin"
      grantedAt: Date,
      grantedBy: String,
      expiresAt: Date
    }],
    downloadCount: Number,
    lastDownload: Date,
    bandwidthUsed: Number // bytes
  },
  lifecycle: {
    retention: Number, // days
    archiveAfter: Number, // days
    deleteAfter: Number, // days
    lastAccessed: Date,
    accessCount: Number,
    isArchived: Boolean,
    archivedAt: Date,
    isDeleted: Boolean,
    deletedAt: Date
  },
  quality: {
    original: Boolean,
    compressed: Boolean,
    thumbnail: Boolean,
    preview: Boolean,
    formats: [{
      format: String, // "original", "compressed", "thumbnail", "preview"
      size: Number, // bytes
      dimensions: {
        width: Number,
        height: Number
      },
      quality: Number, // 1-100
      path: String,
      locationId: ObjectId
    }]
  },
  createdAt: Date,
  updatedAt: Date
}
```

## Missing Features Collections

### 43. Content Moderation Collection
```javascript
{
  _id: ObjectId,
  contentId: ObjectId, // Reference to collection, media, comment, etc.
  contentType: String, // "collection", "media", "comment", "message", "user_profile"
  moderationStatus: String, // "pending", "approved", "rejected", "flagged", "under_review"
  moderationReason: String,
  flaggedBy: [{
    userId: String,
    reason: String, // "inappropriate", "spam", "copyright", "violence", "hate_speech"
    timestamp: Date,
    details: String
  }],
  moderatedBy: String, // Moderator ID
  moderatedAt: Date,
  moderationNotes: String,
  aiAnalysis: {
    enabled: Boolean,
    confidence: Number, // 0-100
    categories: [{
      category: String, // "inappropriate", "spam", "copyright", "violence", "hate_speech", "nudity"
      confidence: Number, // 0-100
      details: Object
    }],
    model: String, // AI model used
    analyzedAt: Date
  },
  humanReview: {
    required: Boolean,
    reviewedBy: String,
    reviewedAt: Date,
    reviewNotes: String,
    reviewResult: String // "approved", "rejected", "flagged_for_human"
  },
  appeals: [{
    userId: String,
    reason: String,
    submittedAt: Date,
    status: String, // "pending", "approved", "rejected"
    reviewedBy: String,
    reviewedAt: Date,
    reviewNotes: String
  }],
  actions: [{
    action: String, // "hide", "delete", "restrict", "warn_user"
    takenBy: String,
    takenAt: Date,
    reason: String,
    duration: Number, // days (for temporary actions)
    expiresAt: Date
  }],
  statistics: {
    flagCount: Number,
    appealCount: Number,
    actionCount: Number,
    lastFlagged: Date,
    lastAppealed: Date
  },
  createdAt: Date,
  updatedAt: Date
}
```

### 44. Copyright Management Collection
```javascript
{
  _id: ObjectId,
  contentId: ObjectId,
  contentType: String, // "collection", "media"
  copyrightStatus: String, // "original", "licensed", "fair_use", "infringing", "public_domain"
  license: {
    type: String, // "cc", "commercial", "public_domain", "all_rights_reserved", "custom"
    version: String, // "CC BY 4.0", "MIT", "GPL", etc.
    url: String, // License URL
    restrictions: [String], // "no_commercial", "no_derivatives", "attribution_required"
    expiresAt: Date
  },
  attribution: {
    author: String,
    authorEmail: String,
    source: String,
    sourceUrl: String,
    license: String,
    copyrightNotice: String,
    isVerified: Boolean,
    verifiedAt: Date
  },
  ownership: {
    claimedBy: String, // User ID
    claimedAt: Date,
    verified: Boolean,
    verificationMethod: String, // "manual", "document", "email", "third_party"
    verificationDocument: String, // Document URL
    originalCreator: String,
    creationDate: Date,
    publicationDate: Date
  },
  dmca: {
    isReported: Boolean,
    reportId: String,
    reportedBy: String,
    reportedAt: Date,
    status: String, // "pending", "processing", "resolved", "dismissed"
    takedownDate: Date,
    restoredDate: Date,
    counterNotice: {
      submitted: Boolean,
      submittedBy: String,
      submittedAt: Date,
      status: String
    }
  },
  fairUse: {
    isFairUse: Boolean,
    purpose: String, // "education", "research", "criticism", "news", "parody"
    nature: String, // "factual", "creative", "published", "unpublished"
    amount: Number, // percentage used
    effect: String, // "no_impact", "minimal_impact", "significant_impact"
    analysis: String
  },
  permissions: [{
    grantedTo: String, // User ID or email
    permission: String, // "use", "modify", "distribute", "commercial"
    grantedBy: String,
    grantedAt: Date,
    expiresAt: Date,
    conditions: [String],
    isActive: Boolean
  }],
  violations: [{
    type: String, // "unauthorized_use", "missing_attribution", "license_violation"
    reportedBy: String,
    reportedAt: Date,
    status: String, // "pending", "resolved", "dismissed"
    resolution: String,
    resolvedAt: Date
  }],
  createdAt: Date,
  updatedAt: Date
}
```

### 45. Search History Collection
```javascript
{
  _id: ObjectId,
  userId: String,
  sessionId: String,
  query: String,
  queryType: String, // "text", "image", "semantic", "tag", "metadata"
  filters: {
    dateRange: {
      from: Date,
      to: Date
    },
    fileTypes: [String],
    categories: [String],
    tags: [String],
    ratings: {
      min: Number,
      max: Number
    },
    size: {
      min: Number,
      max: Number
    }
  },
  results: {
    totalFound: Number,
    clickedResults: [{
      contentId: ObjectId,
      contentType: String,
      position: Number,
      clickedAt: Date,
      timeSpent: Number // seconds
    }],
    timeSpent: Number, // seconds on search page
    refinements: Number, // number of filter refinements
    searches: Number // number of searches in this session
  },
  context: {
    referrer: String,
    userAgent: String,
    ip: String,
    location: {
      country: String,
      region: String,
      city: String
    },
    device: {
      type: String, // "desktop", "mobile", "tablet"
      os: String,
      browser: String
    }
  },
  timestamp: Date,
  createdAt: Date
}
```

### 46. Content Similarity Collection
```javascript
{
  _id: ObjectId,
  contentId: ObjectId,
  contentType: String, // "collection", "media"
  similarContent: [{
    contentId: ObjectId,
    contentType: String,
    similarityScore: Number, // 0-100
    similarityType: String, // "visual", "semantic", "metadata", "text"
    algorithm: String, // "perceptual_hash", "feature_matching", "semantic_analysis"
    confidence: Number, // 0-100
    matchedFeatures: [String], // ["color", "composition", "objects", "style"]
    distance: Number, // Algorithm-specific distance metric
    lastCalculated: Date
  }],
  visualFeatures: {
    dominantColors: [String],
    colorHistogram: [Number],
    texture: Object,
    composition: Object,
    objects: [String],
    faces: Number,
    perceptualHash: String
  },
  semanticFeatures: {
    tags: [String],
    categories: [String],
    concepts: [String],
    emotions: [String],
    style: [String],
    subject: [String]
  },
  metadata: {
    dimensions: {
      width: Number,
      height: Number
    },
    fileSize: Number,
    format: String,
    createdDate: Date,
    modifiedDate: Date
  },
  lastUpdated: Date,
  createdAt: Date
}
```

### 47. Media Processing Jobs Collection
```javascript
{
  _id: ObjectId,
  mediaId: ObjectId,
  jobType: String, // "enhance", "convert", "compress", "extract_audio", "generate_thumbnail", "ai_analysis"
  status: String, // "pending", "queued", "processing", "completed", "failed", "cancelled"
  priority: String, // "low", "normal", "high", "urgent"
  parameters: {
    // Enhancement parameters
    enhancement: {
      brightness: Number, // -100 to 100
      contrast: Number, // -100 to 100
      saturation: Number, // -100 to 100
      sharpness: Number, // -100 to 100
      noiseReduction: Boolean,
      autoEnhance: Boolean
    },
    // Conversion parameters
    conversion: {
      format: String, // "jpg", "png", "webp", "avif"
      quality: Number, // 1-100
      resize: {
        enabled: Boolean,
        width: Number,
        height: Number,
        maintainAspectRatio: Boolean
      }
    },
    // Compression parameters
    compression: {
      algorithm: String, // "jpeg", "png", "webp", "avif"
      level: Number, // 1-9
      lossless: Boolean
    },
    // AI analysis parameters
    aiAnalysis: {
      detectObjects: Boolean,
      detectFaces: Boolean,
      extractText: Boolean,
      analyzeSentiment: Boolean,
      generateTags: Boolean
    }
  },
  progress: Number, // 0-100
  result: {
    outputPath: String,
    outputSize: Number, // bytes
    processingTime: Number, // milliseconds
    quality: Number, // 1-100
    beforeMetrics: {
      size: Number,
      dimensions: Object,
      format: String
    },
    afterMetrics: {
      size: Number,
      dimensions: Object,
      format: String,
      compressionRatio: Number
    },
    artifacts: [{
      type: String,
      path: String,
      description: String
    }]
  },
  error: {
    code: String,
    message: String,
    details: Object,
    occurredAt: Date
  },
  worker: {
    workerId: String,
    nodeId: String,
    assignedAt: Date,
    startedAt: Date,
    completedAt: Date
  },
  retry: {
    attempts: Number,
    maxAttempts: Number,
    lastAttempt: Date,
    nextRetry: Date
  },
  createdAt: Date,
  updatedAt: Date
}
```

### 48. Custom Reports Collection
```javascript
{
  _id: ObjectId,
  reportId: String, // Unique identifier
  userId: String, // Creator
  name: String,
  description: String,
  reportType: String, // "user", "content", "system", "business", "analytics"
  category: String, // "performance", "usage", "revenue", "security", "compliance"
  parameters: {
    dateRange: {
      from: Date,
      to: Date,
      granularity: String // "hour", "day", "week", "month", "year"
    },
    filters: {
      users: [String],
      content: [ObjectId],
      categories: [String],
      tags: [String],
      locations: [String]
    },
    metrics: [String], // ["views", "downloads", "revenue", "users", "storage"]
    dimensions: [String], // ["time", "user", "content", "location", "device"]
    aggregations: [String] // ["sum", "avg", "count", "min", "max"]
  },
  query: {
    aggregationPipeline: [Object], // MongoDB aggregation pipeline
    sqlQuery: String, // For SQL-based reports
    apiEndpoints: [String] // External API calls
  },
  schedule: {
    enabled: Boolean,
    frequency: String, // "daily", "weekly", "monthly", "quarterly", "yearly"
    time: String, // "HH:MM"
    dayOfWeek: Number, // 0-6 (for weekly)
    dayOfMonth: Number, // 1-31 (for monthly)
    recipients: [{
      userId: String,
      email: String,
      format: String // "pdf", "excel", "csv", "json"
    }],
    lastGenerated: Date,
    nextGeneration: Date
  },
  output: {
    format: String, // "pdf", "excel", "csv", "json", "html"
    template: String, // Template ID
    styling: Object,
    branding: Object
  },
  access: {
    isPublic: Boolean,
    sharedWith: [{
      userId: String,
      permission: String, // "read", "write", "admin"
      grantedAt: Date
    }],
    password: String, // Encrypted
    expiresAt: Date
  },
  version: {
    current: Number,
    history: [{
      version: Number,
      changes: String,
      createdBy: String,
      createdAt: Date
    }]
  },
  statistics: {
    generationCount: Number,
    lastAccessed: Date,
    accessCount: Number,
    downloadCount: Number
  },
  createdAt: Date,
  updatedAt: Date
}
```

### 49. User Security Collection
```javascript
{
  _id: ObjectId,
  userId: String,
  twoFactor: {
    enabled: Boolean,
    secret: String, // Encrypted
    backupCodes: [String], // Encrypted
    recoveryCodes: [String], // Encrypted
    lastUsed: Date,
    setupDate: Date
  },
  devices: [{
    deviceId: String,
    deviceName: String,
    deviceType: String, // "desktop", "mobile", "tablet"
    os: String,
    browser: String,
    fingerprint: String,
    lastUsed: Date,
    isTrusted: Boolean,
    trustDate: Date,
    location: {
      country: String,
      region: String,
      city: String,
      coordinates: {
        latitude: Number,
        longitude: Number
      }
    },
    ipAddress: String,
    userAgent: String
  }],
  securitySettings: {
    ipWhitelist: [String],
    ipBlacklist: [String],
    geolocationRestrictions: [String], // Country codes
    sessionTimeout: Number, // minutes
    maxConcurrentSessions: Number,
    requireReauth: {
      enabled: Boolean,
      interval: Number // hours
    },
    loginNotifications: Boolean,
    suspiciousActivityAlerts: Boolean
  },
  securityEvents: [{
    type: String, // "login", "logout", "failed_login", "password_change", "device_added", "suspicious_activity"
    timestamp: Date,
    ip: String,
    location: Object,
    device: Object,
    details: Object,
    severity: String, // "low", "medium", "high", "critical"
    resolved: Boolean,
    resolvedAt: Date
  }],
  loginHistory: [{
    timestamp: Date,
    ip: String,
    location: Object,
    device: Object,
    success: Boolean,
    failureReason: String,
    twoFactorUsed: Boolean
  }],
  passwordHistory: [{
    hash: String,
    createdAt: Date,
    isActive: Boolean
  }],
  apiKeys: [{
    keyId: String,
    name: String,
    key: String, // Hashed
    permissions: [String],
    lastUsed: Date,
    expiresAt: Date,
    isActive: Boolean,
    createdBy: String,
    createdAt: Date
  }],
  riskScore: {
    current: Number, // 0-100
    factors: [{
      factor: String,
      score: Number,
      weight: Number,
      description: String
    }],
    lastCalculated: Date
  },
  createdAt: Date,
  updatedAt: Date
}
```

### 50. Notification Templates Collection
```javascript
{
  _id: ObjectId,
  templateId: String, // Unique identifier
  name: String,
  description: String,
  type: String, // "email", "push", "sms", "in_app", "webhook"
  category: String, // "system", "user", "content", "security", "marketing"
  language: String, // "en", "vi", "zh", "ja"
  subject: String, // For email
  content: String, // Template content
  htmlContent: String, // HTML version for email
  variables: [{
    name: String,
    type: String, // "string", "number", "date", "object", "array"
    required: Boolean,
    description: String,
    defaultValue: String
  }],
  styling: {
    theme: String,
    colors: Object,
    fonts: Object,
    layout: Object
  },
  conditions: [{
    field: String,
    operator: String, // "eq", "ne", "gt", "lt", "contains", "in"
    value: String,
    logic: String // "and", "or"
  }],
  scheduling: {
    allowed: Boolean,
    timezone: String,
    businessHours: {
      enabled: Boolean,
      start: String, // "HH:MM"
      end: String, // "HH:MM"
      days: [Number] // 0-6
    }
  },
  delivery: {
    channels: [String], // ["email", "push", "sms"]
    priority: String, // "low", "normal", "high", "urgent"
    retryPolicy: {
      maxAttempts: Number,
      backoffStrategy: String, // "linear", "exponential"
      maxDelay: Number // seconds
    }
  },
  compliance: {
    gdpr: Boolean,
    canSpam: Boolean,
    optOut: Boolean,
    unsubscribe: Boolean
  },
  analytics: {
    sent: Number,
    delivered: Number,
    opened: Number,
    clicked: Number,
    unsubscribed: Number,
    bounced: Number,
    lastUsed: Date
  },
  isActive: Boolean,
  isDefault: Boolean,
  version: Number,
  createdBy: String,
  updatedBy: String,
  createdAt: Date,
  updatedAt: Date
}
```

### 51. Notification Queue Collection
```javascript
{
  _id: ObjectId,
  notificationId: String, // Unique identifier
  userId: String,
  templateId: ObjectId,
  type: String, // "email", "push", "sms", "in_app", "webhook"
  priority: String, // "low", "normal", "high", "urgent"
  status: String, // "pending", "queued", "processing", "sent", "delivered", "failed", "cancelled"
  subject: String,
  content: String,
  htmlContent: String,
  variables: Object, // Template variables
  recipient: {
    email: String,
    phone: String,
    pushToken: String,
    webhookUrl: String,
    language: String,
    timezone: String
  },
  delivery: {
    scheduledFor: Date,
    sentAt: Date,
    deliveredAt: Date,
    openedAt: Date,
    clickedAt: Date,
    failedAt: Date,
    retryCount: Number,
    maxRetries: Number,
    nextRetry: Date,
    failureReason: String
  },
  tracking: {
    messageId: String,
    externalId: String, // Provider's message ID
    trackingPixel: String,
    clickTracking: Boolean,
    openTracking: Boolean
  },
  metadata: {
    campaign: String,
    source: String,
    tags: [String],
    customData: Object
  },
  compliance: {
    gdpr: Boolean,
    optOut: Boolean,
    unsubscribeToken: String,
    suppressionList: Boolean
  },
  createdAt: Date,
  updatedAt: Date
}
```

### 52. File Versions Collection
```javascript
{
  _id: ObjectId,
  fileId: ObjectId, // Reference to original file
  version: Number, // Version number
  versionName: String, // User-defined version name
  changes: String, // Description of changes
  createdBy: String, // User ID
  createdAt: Date,
  fileSize: Number, // bytes
  fileHash: String, // SHA-256 hash
  storageLocation: ObjectId,
  path: String, // Full path to version
  url: String, // Access URL
  metadata: {
    originalName: String,
    mimeType: String,
    dimensions: {
      width: Number,
      height: Number
    },
    duration: Number, // For videos/audio
    format: String,
    compression: Object
  },
  diff: {
    sizeChange: Number, // bytes difference
    compressionRatio: Number,
    qualityChange: Number,
    formatChange: Boolean,
    dimensionsChange: Boolean
  },
  isActive: Boolean, // Is this the current version
  isDeleted: Boolean,
  deletedAt: Date,
  retention: {
    policy: String, // "keep_all", "keep_latest", "keep_scheduled"
    keepUntil: Date,
    autoDelete: Boolean
  },
  access: {
    public: Boolean,
    permissions: [{
      userId: String,
      permission: String, // "read", "write", "delete"
      grantedAt: Date,
      expiresAt: Date
    }]
  },
  statistics: {
    downloadCount: Number,
    lastDownloaded: Date,
    viewCount: Number,
    lastViewed: Date
  },
  createdAt: Date,
  updatedAt: Date
}
```

### 53. File Permissions Collection
```javascript
{
  _id: ObjectId,
  fileId: ObjectId,
  permissionType: String, // "user", "group", "role", "public"
  targetId: String, // User ID, Group ID, Role ID, or "public"
  targetType: String, // "user", "group", "role", "public"
  permissions: [String], // "read", "write", "delete", "share", "admin"
  grantedBy: String, // User ID who granted permission
  grantedAt: Date,
  expiresAt: Date,
  isActive: Boolean,
  conditions: [{
    type: String, // "time", "location", "device", "ip"
    operator: String, // "eq", "ne", "in", "not_in", "gt", "lt"
    value: String,
    description: String
  }],
  restrictions: {
    downloadLimit: Number,
    viewLimit: Number,
    timeLimit: Number, // seconds
    watermark: Boolean,
    printAllowed: Boolean,
    editAllowed: Boolean,
    shareAllowed: Boolean
  },
  audit: {
    lastUsed: Date,
    usageCount: Number,
    violations: [{
      type: String,
      timestamp: Date,
      details: String,
      action: String
    }]
  },
  source: String, // "direct", "inherited", "group", "role"
  parentPermission: ObjectId, // Reference to parent permission if inherited
  metadata: {
    reason: String, // Why permission was granted
    notes: String,
    tags: [String]
  },
  createdAt: Date,
  updatedAt: Date
}
```

### 54. User Groups Collection
```javascript
{
  _id: ObjectId,
  groupId: String, // Unique identifier
  name: String,
  description: String,
  type: String, // "public", "private", "invite_only", "moderated"
  category: String, // "interest", "location", "skill", "organization", "custom"
  members: [{
    userId: String,
    role: String, // "member", "moderator", "admin", "owner"
    joinedAt: Date,
    invitedBy: String,
    status: String, // "active", "pending", "banned", "left"
    permissions: [String] // Group-specific permissions
  }],
  permissions: [String], // Global group permissions
  settings: {
    maxMembers: Number,
    allowMemberInvites: Boolean,
    requireApproval: Boolean,
    allowFileSharing: Boolean,
    allowMessaging: Boolean,
    allowEvents: Boolean,
    visibility: String, // "public", "private", "hidden"
    joinPolicy: String // "open", "approval", "invite_only"
  },
  content: {
    sharedCollections: [ObjectId],
    sharedFiles: [ObjectId],
    discussions: [ObjectId],
    events: [ObjectId]
  },
  statistics: {
    memberCount: Number,
    activeMembers: Number,
    totalPosts: Number,
    totalFiles: Number,
    lastActivity: Date
  },
  moderation: {
    moderators: [String],
    bannedUsers: [String],
    rules: [String],
    autoModeration: Boolean,
    reportCount: Number
  },
  notifications: {
    newMembers: Boolean,
    newPosts: Boolean,
    newFiles: Boolean,
    events: Boolean,
    weeklyDigest: Boolean
  },
  createdBy: String,
  createdAt: Date,
  updatedAt: Date,
  lastActivity: Date
}
```

### 55. User Activity Logs Collection
```javascript
{
  _id: ObjectId,
  userId: String,
  sessionId: String,
  action: String, // "login", "logout", "view", "download", "upload", "share", "comment", "rate"
  resource: String, // "collection", "media", "user", "group", "system"
  resourceId: ObjectId,
  details: {
    // Action-specific details
    viewDuration: Number, // seconds
    downloadSize: Number, // bytes
    uploadSize: Number, // bytes
    shareMethod: String, // "direct", "social", "embed"
    rating: Number, // 1-5
    commentLength: Number,
    searchQuery: String,
    filters: Object
  },
  context: {
    ip: String,
    userAgent: String,
    location: {
      country: String,
      region: String,
      city: String,
      coordinates: {
        latitude: Number,
        longitude: Number
      }
    },
    device: {
      type: String, // "desktop", "mobile", "tablet"
      os: String,
      browser: String,
      screenResolution: String
    },
    referrer: String,
    pageUrl: String
  },
  performance: {
    responseTime: Number, // milliseconds
    loadTime: Number, // milliseconds
    errorOccurred: Boolean,
    errorMessage: String
  },
  privacy: {
    isAnonymized: Boolean,
    dataRetention: Number, // days
    gdprCompliant: Boolean
  },
  timestamp: Date,
  createdAt: Date
}
```

### 56. System Health Collection
```javascript
{
  _id: ObjectId,
  timestamp: Date,
  component: String, // "database", "storage", "api", "worker", "cache", "queue"
  status: String, // "healthy", "warning", "critical", "down", "maintenance"
  metrics: {
    cpu: {
      usage: Number, // percentage
      cores: Number,
      load: [Number] // 1min, 5min, 15min averages
    },
    memory: {
      total: Number, // bytes
      used: Number, // bytes
      available: Number, // bytes
      usage: Number // percentage
    },
    disk: {
      total: Number, // bytes
      used: Number, // bytes
      available: Number, // bytes
      usage: Number, // percentage
      readSpeed: Number, // bytes per second
      writeSpeed: Number // bytes per second
    },
    network: {
      uploadSpeed: Number, // bytes per second
      downloadSpeed: Number, // bytes per second
      latency: Number, // milliseconds
      packetLoss: Number // percentage
    },
    database: {
      connections: Number,
      activeQueries: Number,
      slowQueries: Number,
      cacheHitRate: Number // percentage
    },
    storage: {
      totalFiles: Number,
      totalSize: Number, // bytes
      availableSpace: Number, // bytes
      replicationHealth: Number // percentage
    }
  },
  performance: {
    responseTime: Number, // milliseconds
    throughput: Number, // requests per second
    errorRate: Number, // percentage
    uptime: Number // percentage
  },
  alerts: [{
    type: String, // "error", "warning", "info"
    message: String,
    severity: String, // "low", "medium", "high", "critical"
    timestamp: Date,
    resolved: Boolean,
    resolvedAt: Date
  }],
  actions: [{
    action: String, // "restart", "scale", "alert", "maintenance"
    takenBy: String, // System or user
    takenAt: Date,
    result: String,
    duration: Number // seconds
  }],
  environment: {
    version: String,
    build: String,
    deployment: String,
    region: String,
    datacenter: String
  },
  dependencies: [{
    name: String, // "mongodb", "redis", "rabbitmq", "storage"
    status: String, // "healthy", "warning", "critical", "down"
    responseTime: Number,
    lastCheck: Date
  }],
  createdAt: Date
}
```

### 57. System Maintenance Collection
```javascript
{
  _id: ObjectId,
  maintenanceId: String, // Unique identifier
  type: String, // "scheduled", "emergency", "upgrade", "backup", "security"
  category: String, // "database", "storage", "api", "worker", "infrastructure"
  title: String,
  description: String,
  priority: String, // "low", "normal", "high", "critical"
  status: String, // "scheduled", "in_progress", "completed", "failed", "cancelled"
  schedule: {
    plannedStart: Date,
    plannedEnd: Date,
    actualStart: Date,
    actualEnd: Date,
    estimatedDuration: Number, // minutes
    actualDuration: Number // minutes
  },
  scope: {
    affectedServices: [String],
    affectedUsers: [String],
    affectedRegions: [String],
    impact: String // "none", "minimal", "moderate", "high", "critical"
  },
  notifications: {
    preMaintenance: {
      sent: Boolean,
      sentAt: Date,
      recipients: [String]
    },
    duringMaintenance: {
      sent: Boolean,
      sentAt: Date,
      recipients: [String]
    },
    postMaintenance: {
      sent: Boolean,
      sentAt: Date,
      recipients: [String]
    }
  },
  procedures: [{
    step: Number,
    description: String,
    status: String, // "pending", "in_progress", "completed", "failed"
    startedAt: Date,
    completedAt: Date,
    notes: String,
    performedBy: String
  }],
  rollback: {
    plan: String,
    executed: Boolean,
    executedAt: Date,
    reason: String
  },
  verification: {
    checks: [{
      name: String,
      status: String, // "passed", "failed", "warning"
      result: String,
      checkedAt: Date
    }],
    overallStatus: String,
    verifiedBy: String,
    verifiedAt: Date
  },
  communication: {
    publicNotice: String,
    internalNotes: String,
    stakeholderUpdates: [{
      timestamp: Date,
      message: String,
      sentBy: String
    }]
  },
  resources: {
    assigned: [String], // Team members
    tools: [String],
    documentation: [String],
    tickets: [String]
  },
  metrics: {
    downtime: Number, // minutes
    userImpact: Number, // number of affected users
    revenueImpact: Number, // estimated revenue loss
    satisfaction: Number, // user satisfaction score
    lessonsLearned: String
  },
  createdBy: String,
  approvedBy: String,
  approvedAt: Date,
  createdAt: Date,
  updatedAt: Date
}
```

## MongoDB Configuration

### Connection Configuration
```csharp
public class MongoDbOptions
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "image_viewer";
    public int ConnectionPoolSize { get; set; } = 100;
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan SocketTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan ServerSelectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IMongoClient client, string databaseName)
    {
        _database = client.GetDatabase(databaseName);
    }

    // Core Collections
    public IMongoCollection<Library> Libraries => _database.GetCollection<Library>("libraries");
    public IMongoCollection<Collection> Collections => _database.GetCollection<Collection>("collections");
    public IMongoCollection<MediaItem> MediaItems => _database.GetCollection<MediaItem>("mediaItems");
    public IMongoCollection<FileSystemWatcher> FileSystemWatchers => _database.GetCollection<FileSystemWatcher>("fileSystemWatchers");
    public IMongoCollection<CacheFolder> CacheFolders => _database.GetCollection<CacheFolder>("cacheFolders");
    public IMongoCollection<BackgroundJob> BackgroundJobs => _database.GetCollection<BackgroundJob>("backgroundJobs");
    public IMongoCollection<SystemSetting> SystemSettings => _database.GetCollection<SystemSetting>("systemSettings");
    public IMongoCollection<UserSettings> UserSettings => _database.GetCollection<UserSettings>("userSettings");
    public IMongoCollection<FavoriteList> FavoriteLists => _database.GetCollection<FavoriteList>("favoriteLists");
    public IMongoCollection<ViewSession> ViewSessions => _database.GetCollection<ViewSession>("viewSessions");
    public IMongoCollection<AuditLog> AuditLogs => _database.GetCollection<AuditLog>("auditLogs");
    public IMongoCollection<ErrorLog> ErrorLogs => _database.GetCollection<ErrorLog>("errorLogs");
    public IMongoCollection<BackupHistory> BackupHistory => _database.GetCollection<BackupHistory>("backupHistory");
    public IMongoCollection<PerformanceMetric> PerformanceMetrics => _database.GetCollection<PerformanceMetric>("performanceMetrics");

    // Analytics Collections
    public IMongoCollection<UserBehaviorEvent> UserBehaviorEvents => _database.GetCollection<UserBehaviorEvent>("userBehaviorEvents");
    public IMongoCollection<UserAnalytics> UserAnalytics => _database.GetCollection<UserAnalytics>("userAnalytics");
    public IMongoCollection<ContentPopularity> ContentPopularity => _database.GetCollection<ContentPopularity>("contentPopularity");
    public IMongoCollection<SearchAnalytics> SearchAnalytics => _database.GetCollection<SearchAnalytics>("searchAnalytics");

    // Social Collections
    public IMongoCollection<UserCollection> UserCollections => _database.GetCollection<UserCollection>("userCollections");
    public IMongoCollection<CollectionRating> CollectionRatings => _database.GetCollection<CollectionRating>("collectionRatings");
    public IMongoCollection<UserFollow> UserFollows => _database.GetCollection<UserFollow>("userFollows");
    public IMongoCollection<CollectionComment> CollectionComments => _database.GetCollection<CollectionComment>("collectionComments");
    public IMongoCollection<UserMessage> UserMessages => _database.GetCollection<UserMessage>("userMessages");
    public IMongoCollection<Conversation> Conversations => _database.GetCollection<Conversation>("conversations");

    // Distribution Collections
    public IMongoCollection<Torrent> Torrents => _database.GetCollection<Torrent>("torrents");
    public IMongoCollection<DownloadLink> DownloadLinks => _database.GetCollection<DownloadLink>("downloadLinks");
    public IMongoCollection<TorrentStatistic> TorrentStatistics => _database.GetCollection<TorrentStatistic>("torrentStatistics");
    public IMongoCollection<LinkHealthChecker> LinkHealthChecker => _database.GetCollection<LinkHealthChecker>("linkHealthChecker");
    public IMongoCollection<DownloadQualityOption> DownloadQualityOptions => _database.GetCollection<DownloadQualityOption>("downloadQualityOptions");
    public IMongoCollection<DistributionNode> DistributionNodes => _database.GetCollection<DistributionNode>("distributionNodes");
    public IMongoCollection<NodePerformanceMetric> NodePerformanceMetrics => _database.GetCollection<NodePerformanceMetric>("nodePerformanceMetrics");

    // Reward System Collections
    public IMongoCollection<UserReward> UserRewards => _database.GetCollection<UserReward>("userRewards");
    public IMongoCollection<RewardTransaction> RewardTransactions => _database.GetCollection<RewardTransaction>("rewardTransactions");
    public IMongoCollection<RewardSetting> RewardSettings => _database.GetCollection<RewardSetting>("rewardSettings");
    public IMongoCollection<RewardAchievement> RewardAchievements => _database.GetCollection<RewardAchievement>("rewardAchievements");
    public IMongoCollection<RewardBadge> RewardBadges => _database.GetCollection<RewardBadge>("rewardBadges");
    public IMongoCollection<PremiumFeature> PremiumFeatures => _database.GetCollection<PremiumFeature>("premiumFeatures");
    public IMongoCollection<UserPremiumFeature> UserPremiumFeatures => _database.GetCollection<UserPremiumFeature>("userPremiumFeatures");

    // Enhanced Settings Collections
    public IMongoCollection<StorageLocation> StorageLocations => _database.GetCollection<StorageLocation>("storageLocations");
    public IMongoCollection<FileStorageMapping> FileStorageMapping => _database.GetCollection<FileStorageMapping>("fileStorageMapping");

    // Missing Features Collections
    public IMongoCollection<ContentModeration> ContentModeration => _database.GetCollection<ContentModeration>("contentModeration");
    public IMongoCollection<CopyrightManagement> CopyrightManagement => _database.GetCollection<CopyrightManagement>("copyrightManagement");
    public IMongoCollection<SearchHistory> SearchHistory => _database.GetCollection<SearchHistory>("searchHistory");
    public IMongoCollection<ContentSimilarity> ContentSimilarity => _database.GetCollection<ContentSimilarity>("contentSimilarity");
    public IMongoCollection<MediaProcessingJob> MediaProcessingJobs => _database.GetCollection<MediaProcessingJob>("mediaProcessingJobs");
    public IMongoCollection<CustomReport> CustomReports => _database.GetCollection<CustomReport>("customReports");
    public IMongoCollection<UserSecurity> UserSecurity => _database.GetCollection<UserSecurity>("userSecurity");
    public IMongoCollection<NotificationTemplate> NotificationTemplates => _database.GetCollection<NotificationTemplate>("notificationTemplates");
    public IMongoCollection<NotificationQueue> NotificationQueue => _database.GetCollection<NotificationQueue>("notificationQueue");
    public IMongoCollection<FileVersion> FileVersions => _database.GetCollection<FileVersion>("fileVersions");
    public IMongoCollection<FilePermission> FilePermissions => _database.GetCollection<FilePermission>("filePermissions");
    public IMongoCollection<UserGroup> UserGroups => _database.GetCollection<UserGroup>("userGroups");
    public IMongoCollection<UserActivityLog> UserActivityLogs => _database.GetCollection<UserActivityLog>("userActivityLogs");
    public IMongoCollection<SystemHealth> SystemHealth => _database.GetCollection<SystemHealth>("systemHealth");
    public IMongoCollection<SystemMaintenance> SystemMaintenance => _database.GetCollection<SystemMaintenance>("systemMaintenance");
}
```

### Indexing Strategy
```javascript
// Libraries Collection Indexes
db.libraries.createIndex({ "name": 1 });
db.libraries.createIndex({ "type": 1 });
db.libraries.createIndex({ "settings.autoScan": 1 });
db.libraries.createIndex({ "watchInfo.isWatching": 1 });
db.libraries.createIndex({ "statistics.lastScanDate": -1 });
db.libraries.createIndex({ "searchIndex.searchableText": "text", "searchIndex.tags": "text" });

// Collections Collection Indexes
db.collections.createIndex({ "libraryId": 1, "name": 1 });
db.collections.createIndex({ "libraryId": 1, "type": 1 });
db.collections.createIndex({ "metadata.tags": 1 });
db.collections.createIndex({ "createdAt": -1 });
db.collections.createIndex({ "statistics.totalSize": -1 });
db.collections.createIndex({ "watchInfo.isWatching": 1 });
db.collections.createIndex({ "settings.priority": -1 });
db.collections.createIndex({ "searchIndex.searchableText": "text", "searchIndex.tags": "text" });

// Media Items Collection Indexes
db.mediaItems.createIndex({ "libraryId": 1, "collectionId": 1, "filename": 1 });
db.mediaItems.createIndex({ "collectionId": 1, "fileType": 1 });
db.mediaItems.createIndex({ "collectionId": 1, "createdAt": -1 });
db.mediaItems.createIndex({ "metadata.tags": 1 });
db.mediaItems.createIndex({ "statistics.viewCount": -1 });
db.mediaItems.createIndex({ "statistics.rating": -1 });
db.mediaItems.createIndex({ "fileInfo.lastModified": -1 });
db.mediaItems.createIndex({ "fileInfo.exists": 1 });
db.mediaItems.createIndex({ "cacheInfo.needsRegeneration": 1 });
db.mediaItems.createIndex({ "searchIndex.searchableText": "text", "searchIndex.tags": "text" });

// Background Jobs Collection Indexes
db.backgroundJobs.createIndex({ "status": 1, "priority": -1 });
db.backgroundJobs.createIndex({ "type": 1, "status": 1 });
db.backgroundJobs.createIndex({ "target.libraryId": 1 });
db.backgroundJobs.createIndex({ "target.collectionId": 1 });
db.backgroundJobs.createIndex({ "createdAt": -1 });
db.backgroundJobs.createIndex({ "parameters.scanMode": 1 });
db.backgroundJobs.createIndex({ "timing.startedAt": -1 });

// System Settings Collection Indexes
db.systemSettings.createIndex({ "key": 1 }, { unique: true });
db.systemSettings.createIndex({ "category": 1 });
db.systemSettings.createIndex({ "type": 1 });
db.systemSettings.createIndex({ "metadata.isReadOnly": 1 });
db.systemSettings.createIndex({ "metadata.isAdvanced": 1 });
db.systemSettings.createIndex({ "updatedAt": -1 });

// User Settings Collection Indexes
db.userSettings.createIndex({ "userId": 1 }, { unique: true });
db.userSettings.createIndex({ "preferences.theme": 1 });
db.userSettings.createIndex({ "preferences.language": 1 });
db.userSettings.createIndex({ "preferences.defaultView": 1 });
db.userSettings.createIndex({ "notificationSettings.enableNotifications": 1 });
db.userSettings.createIndex({ "privacySettings.shareUsageData": 1 });

// Favorite Lists Collection Indexes
db.favoriteLists.createIndex({ "userId": 1, "name": 1 });
db.favoriteLists.createIndex({ "userId": 1, "type": 1 });

// Missing Features Collections Indexes

// Content Moderation Collection Indexes
db.contentModeration.createIndex({ "contentId": 1, "contentType": 1 });
db.contentModeration.createIndex({ "moderationStatus": 1, "createdAt": -1 });
db.contentModeration.createIndex({ "flaggedBy.userId": 1 });
db.contentModeration.createIndex({ "moderatedBy": 1, "moderatedAt": -1 });
db.contentModeration.createIndex({ "aiAnalysis.confidence": -1 });
db.contentModeration.createIndex({ "statistics.flagCount": -1 });

// Copyright Management Collection Indexes
db.copyrightManagement.createIndex({ "contentId": 1, "contentType": 1 });
db.copyrightManagement.createIndex({ "copyrightStatus": 1 });
db.copyrightManagement.createIndex({ "license.type": 1 });
db.copyrightManagement.createIndex({ "ownership.claimedBy": 1 });
db.copyrightManagement.createIndex({ "dmca.status": 1, "dmca.reportedAt": -1 });
db.copyrightManagement.createIndex({ "permissions.grantedTo": 1 });

// Search History Collection Indexes
db.searchHistory.createIndex({ "userId": 1, "timestamp": -1 });
db.searchHistory.createIndex({ "sessionId": 1 });
db.searchHistory.createIndex({ "queryType": 1, "timestamp": -1 });
db.searchHistory.createIndex({ "timestamp": 1 }, { expireAfterSeconds: 86400 * 90 });

// Content Similarity Collection Indexes
db.contentSimilarity.createIndex({ "contentId": 1, "contentType": 1 });
db.contentSimilarity.createIndex({ "similarContent.contentId": 1 });
db.contentSimilarity.createIndex({ "similarContent.similarityScore": -1 });
db.contentSimilarity.createIndex({ "lastUpdated": -1 });

// Media Processing Jobs Collection Indexes
db.mediaProcessingJobs.createIndex({ "mediaId": 1, "status": 1 });
db.mediaProcessingJobs.createIndex({ "jobType": 1, "status": 1 });
db.mediaProcessingJobs.createIndex({ "status": 1, "priority": -1 });
db.mediaProcessingJobs.createIndex({ "createdAt": 1 }, { expireAfterSeconds: 86400 * 30 });

// Custom Reports Collection Indexes
db.customReports.createIndex({ "userId": 1, "createdAt": -1 });
db.customReports.createIndex({ "reportType": 1, "category": 1 });
db.customReports.createIndex({ "access.isPublic": 1 });
db.customReports.createIndex({ "schedule.enabled": 1, "schedule.nextGeneration": 1 });

// User Security Collection Indexes
db.userSecurity.createIndex({ "userId": 1 }, { unique: true });
db.userSecurity.createIndex({ "twoFactor.enabled": 1 });
db.userSecurity.createIndex({ "devices.deviceId": 1 });
db.userSecurity.createIndex({ "securityEvents.type": 1, "securityEvents.timestamp": -1 });
db.userSecurity.createIndex({ "riskScore.current": -1 });

// Notification Templates Collection Indexes
db.notificationTemplates.createIndex({ "templateId": 1 }, { unique: true });
db.notificationTemplates.createIndex({ "type": 1, "category": 1 });
db.notificationTemplates.createIndex({ "isActive": 1, "isDefault": 1 });
db.notificationTemplates.createIndex({ "language": 1 });

// Notification Queue Collection Indexes
db.notificationQueue.createIndex({ "userId": 1, "status": 1 });
db.notificationQueue.createIndex({ "type": 1, "status": 1 });
db.notificationQueue.createIndex({ "status": 1, "priority": -1 });
db.notificationQueue.createIndex({ "delivery.scheduledFor": 1 });
db.notificationQueue.createIndex({ "createdAt": 1 }, { expireAfterSeconds: 86400 * 7 });

// File Versions Collection Indexes
db.fileVersions.createIndex({ "fileId": 1, "version": 1 });
db.fileVersions.createIndex({ "fileId": 1, "isActive": 1 });
db.fileVersions.createIndex({ "createdBy": 1, "createdAt": -1 });
db.fileVersions.createIndex({ "retention.keepUntil": 1 });

// File Permissions Collection Indexes
db.filePermissions.createIndex({ "fileId": 1, "targetId": 1 });
db.filePermissions.createIndex({ "targetType": 1, "isActive": 1 });
db.filePermissions.createIndex({ "grantedBy": 1, "grantedAt": -1 });
db.filePermissions.createIndex({ "expiresAt": 1 });

// User Groups Collection Indexes
db.userGroups.createIndex({ "groupId": 1 }, { unique: true });
db.userGroups.createIndex({ "type": 1, "category": 1 });
db.userGroups.createIndex({ "members.userId": 1 });
db.userGroups.createIndex({ "createdBy": 1, "createdAt": -1 });
db.userGroups.createIndex({ "lastActivity": -1 });

// User Activity Logs Collection Indexes
db.userActivityLogs.createIndex({ "userId": 1, "timestamp": -1 });
db.userActivityLogs.createIndex({ "action": 1, "timestamp": -1 });
db.userActivityLogs.createIndex({ "resource": 1, "resourceId": 1 });
db.userActivityLogs.createIndex({ "sessionId": 1 });
db.userActivityLogs.createIndex({ "timestamp": 1 }, { expireAfterSeconds: 86400 * 365 });

// System Health Collection Indexes
db.systemHealth.createIndex({ "component": 1, "timestamp": -1 });
db.systemHealth.createIndex({ "status": 1, "timestamp": -1 });
db.systemHealth.createIndex({ "timestamp": 1 }, { expireAfterSeconds: 86400 * 7 });

// System Maintenance Collection Indexes
db.systemMaintenance.createIndex({ "maintenanceId": 1 }, { unique: true });
db.systemMaintenance.createIndex({ "type": 1, "status": 1 });
db.systemMaintenance.createIndex({ "priority": 1, "schedule.plannedStart": 1 });
db.systemMaintenance.createIndex({ "createdBy": 1, "createdAt": -1 });
db.favoriteLists.createIndex({ "userId": 1, "createdAt": -1 });
db.favoriteLists.createIndex({ "settings.isPublic": 1 });
db.favoriteLists.createIndex({ "statistics.lastAccessed": -1 });
db.favoriteLists.createIndex({ "items.mediaId": 1 });
db.favoriteLists.createIndex({ "items.collectionId": 1 });
db.favoriteLists.createIndex({ "smartFilters.enabled": 1 });
db.favoriteLists.createIndex({ "smartFilters.lastUpdate": -1 });
db.favoriteLists.createIndex({ "searchIndex.searchableText": "text", "searchIndex.tags": "text", "searchIndex.items": "text" });

// Audit Logs Collection Indexes
db.auditLogs.createIndex({ "userId": 1, "timestamp": -1 });
db.auditLogs.createIndex({ "action": 1, "timestamp": -1 });
db.auditLogs.createIndex({ "resourceType": 1, "resourceId": 1 });
db.auditLogs.createIndex({ "severity": 1, "timestamp": -1 });
db.auditLogs.createIndex({ "category": 1, "timestamp": -1 });
db.auditLogs.createIndex({ "sessionId": 1, "timestamp": -1 });
db.auditLogs.createIndex({ "timestamp": -1 }, { expireAfterSeconds: 2592000 }); // TTL: 30 days

// Error Logs Collection Indexes
db.errorLogs.createIndex({ "errorId": 1 }, { unique: true });
db.errorLogs.createIndex({ "type": 1, "severity": 1 });
db.errorLogs.createIndex({ "severity": 1, "timestamp": -1 });
db.errorLogs.createIndex({ "resolution.status": 1 });
db.errorLogs.createIndex({ "context.userId": 1, "timestamp": -1 });
db.errorLogs.createIndex({ "context.resourceType": 1, "context.resourceId": 1 });
db.errorLogs.createIndex({ "timestamp": -1 }, { expireAfterSeconds: 7776000 }); // TTL: 90 days

// User Behavior Events Collection Indexes
db.userBehaviorEvents.createIndex({ "userId": 1, "timestamp": -1 });
db.userBehaviorEvents.createIndex({ "sessionId": 1, "timestamp": -1 });
db.userBehaviorEvents.createIndex({ "eventType": 1, "timestamp": -1 });
db.userBehaviorEvents.createIndex({ "targetType": 1, "targetId": 1 });
db.userBehaviorEvents.createIndex({ "userId": 1, "eventType": 1, "timestamp": -1 });
db.userBehaviorEvents.createIndex({ "context.device": 1, "timestamp": -1 });
db.userBehaviorEvents.createIndex({ "metadata.query": "text" });
db.userBehaviorEvents.createIndex({ "timestamp": -1 }, { expireAfterSeconds: 7776000 }); // TTL: 90 days

// User Analytics Collection Indexes
db.userAnalytics.createIndex({ "userId": 1, "period": 1, "date": -1 });
db.userAnalytics.createIndex({ "period": 1, "date": -1 });
db.userAnalytics.createIndex({ "userId": 1, "date": -1 });
db.userAnalytics.createIndex({ "metrics.totalViews": -1 });
db.userAnalytics.createIndex({ "metrics.totalSearches": -1 });
db.userAnalytics.createIndex({ "demographics.country": 1, "date": -1 });

// Content Popularity Collection Indexes
db.contentPopularity.createIndex({ "targetType": 1, "targetId": 1, "period": 1, "date": -1 });
db.contentPopularity.createIndex({ "targetType": 1, "period": 1, "date": -1 });
db.contentPopularity.createIndex({ "metrics.popularityScore": -1 });
db.contentPopularity.createIndex({ "metrics.trendingScore": -1 });
db.contentPopularity.createIndex({ "metrics.engagementScore": -1 });
db.contentPopularity.createIndex({ "period": 1, "date": -1 });

// Search Analytics Collection Indexes
db.searchAnalytics.createIndex({ "userId": 1, "timestamp": -1 });
db.searchAnalytics.createIndex({ "query": "text" });
db.searchAnalytics.createIndex({ "queryHash": 1 });
db.searchAnalytics.createIndex({ "resultCount": 1 });
db.searchAnalytics.createIndex({ "searchSuccess": 1, "timestamp": -1 });
db.searchAnalytics.createIndex({ "context.device": 1, "timestamp": -1 });
db.searchAnalytics.createIndex({ "timestamp": -1 }, { expireAfterSeconds: 2592000 }); // TTL: 30 days

// Performance Metrics Collection Indexes
db.performanceMetrics.createIndex({ "timestamp": -1, "metricType": 1 });
db.performanceMetrics.createIndex({ "metricType": 1, "timestamp": -1 });
db.performanceMetrics.createIndex({ "context.userId": 1, "timestamp": -1 });
db.performanceMetrics.createIndex({ "context.collectionId": 1, "timestamp": -1 });
db.performanceMetrics.createIndex({ "operation": 1, "timestamp": -1 });
db.performanceMetrics.createIndex({ "timestamp": -1 }, { expireAfterSeconds: 2592000 }); // TTL: 30 days

// Social & Sharing Features Indexes

// User Collections Collection Indexes
db.userCollections.createIndex({ "userId": 1, "status": 1, "createdAt": -1 });
db.userCollections.createIndex({ "visibility": 1, "status": 1, "createdAt": -1 });
db.userCollections.createIndex({ "category": 1, "status": 1, "createdAt": -1 });
db.userCollections.createIndex({ "tags": 1, "status": 1 });
db.userCollections.createIndex({ "metadata.averageRating": -1, "metadata.totalRatings": -1 });
db.userCollections.createIndex({ "metadata.totalViews": -1 });
db.userCollections.createIndex({ "metadata.totalDownloads": -1 });
db.userCollections.createIndex({ "title": "text", "description": "text", "tags": "text" });
db.userCollections.createIndex({ "publishedAt": -1 });

// Collection Ratings Collection Indexes
db.collectionRatings.createIndex({ "collectionId": 1, "createdAt": -1 });
db.collectionRatings.createIndex({ "userId": 1, "createdAt": -1 });
db.collectionRatings.createIndex({ "rating": 1, "createdAt": -1 });
db.collectionRatings.createIndex({ "collectionId": 1, "rating": 1 });
db.collectionRatings.createIndex({ "status": 1, "createdAt": -1 });

// User Follows Collection Indexes
db.userFollows.createIndex({ "followerId": 1, "status": 1 });
db.userFollows.createIndex({ "followingId": 1, "status": 1 });
db.userFollows.createIndex({ "followType": 1, "targetId": 1 });
db.userFollows.createIndex({ "followerId": 1, "followType": 1 });
db.userFollows.createIndex({ "createdAt": -1 });

// Collection Comments Collection Indexes
db.collectionComments.createIndex({ "collectionId": 1, "createdAt": -1 });
db.collectionComments.createIndex({ "userId": 1, "createdAt": -1 });
db.collectionComments.createIndex({ "parentCommentId": 1, "createdAt": -1 });
db.collectionComments.createIndex({ "status": 1, "createdAt": -1 });
db.collectionComments.createIndex({ "mentions": 1 });

// User Messages Collection Indexes
db.userMessages.createIndex({ "conversationId": 1, "createdAt": -1 });
db.userMessages.createIndex({ "senderId": 1, "createdAt": -1 });
db.userMessages.createIndex({ "recipientId": 1, "createdAt": -1 });
db.userMessages.createIndex({ "status.read": 1, "createdAt": -1 });
db.userMessages.createIndex({ "messageType": 1, "createdAt": -1 });

// Conversations Collection Indexes
db.conversations.createIndex({ "participants.userId": 1, "status": 1 });
db.conversations.createIndex({ "type": 1, "status": 1 });
db.conversations.createIndex({ "lastMessage.timestamp": -1 });
db.conversations.createIndex({ "createdAt": -1 });

// Torrents Collection Indexes
db.torrents.createIndex({ "collectionId": 1, "status": 1 });
db.torrents.createIndex({ "userId": 1, "status": 1 });
db.torrents.createIndex({ "infoHash": 1 }, { unique: true });
db.torrents.createIndex({ "category": 1, "status": 1 });
db.torrents.createIndex({ "tags": 1, "status": 1 });
db.torrents.createIndex({ "statistics.seeders": -1, "statistics.leechers": -1 });
db.torrents.createIndex({ "quality.resolution": 1, "status": 1 });
db.torrents.createIndex({ "name": "text", "description": "text" });
db.torrents.createIndex({ "createdAt": -1 });

// Download Links Collection Indexes
db.downloadLinks.createIndex({ "collectionId": 1, "status": 1 });
db.downloadLinks.createIndex({ "userId": 1, "status": 1 });
db.downloadLinks.createIndex({ "health.isHealthy": 1, "health.lastHealthCheck": -1 });
db.downloadLinks.createIndex({ "links.provider": 1, "status": 1 });
db.downloadLinks.createIndex({ "statistics.totalDownloads": -1 });
db.downloadLinks.createIndex({ "createdAt": -1 });

// Torrent Statistics Collection Indexes
db.torrentStatistics.createIndex({ "torrentId": 1, "status": 1 });
db.torrentStatistics.createIndex({ "userId": 1, "status": 1 });
db.torrentStatistics.createIndex({ "peerId": 1 }, { unique: true });
db.torrentStatistics.createIndex({ "ipAddress": 1, "port": 1 });
db.torrentStatistics.createIndex({ "lastSeen": -1 });
db.torrentStatistics.createIndex({ "statistics.ratio": -1 });
db.torrentStatistics.createIndex({ "createdAt": -1 }, { expireAfterSeconds: 604800 }); // TTL: 7 days

// Link Health Checker Collection Indexes
db.linkHealthChecker.createIndex({ "linkId": 1, "url": 1 });
db.linkHealthChecker.createIndex({ "provider": 1, "health.isHealthy": 1 });
db.linkHealthChecker.createIndex({ "health.lastHealthyCheck": -1 });
db.linkHealthChecker.createIndex({ "checkResults.timestamp": -1 });
db.linkHealthChecker.createIndex({ "alerts.resolved": 1, "alerts.timestamp": -1 });

// Download Quality Options Collection Indexes
db.downloadQualityOptions.createIndex({ "collectionId": 1, "quality": 1 });
db.downloadQualityOptions.createIndex({ "quality": 1, "format": 1 });
db.downloadQualityOptions.createIndex({ "availability.isAvailable": 1, "availability.expiresAt": 1 });
db.downloadQualityOptions.createIndex({ "sources.status": 1, "sources.priority": -1 });

// Distribution Nodes Collection Indexes
db.distributionNodes.createIndex({ "nodeId": 1 }, { unique: true });
db.distributionNodes.createIndex({ "userId": 1, "status": 1 });
db.distributionNodes.createIndex({ "status": 1, "quality.score": -1 });
db.distributionNodes.createIndex({ "location.country": 1, "status": 1 });
db.distributionNodes.createIndex({ "capabilities.maxStorage": -1, "status": 1 });
db.distributionNodes.createIndex({ "performance.uptime": -1, "status": 1 });
db.distributionNodes.createIndex({ "collections.collectionId": 1 });
db.distributionNodes.createIndex({ "monitoring.lastHeartbeat": -1 });
db.distributionNodes.createIndex({ "createdAt": -1 });

// Node Performance Metrics Collection Indexes
db.nodePerformanceMetrics.createIndex({ "nodeId": 1, "timestamp": -1 });
db.nodePerformanceMetrics.createIndex({ "timestamp": -1, "metrics.cpu.usage": -1 });
db.nodePerformanceMetrics.createIndex({ "performance.errorRate": -1, "timestamp": -1 });
db.nodePerformanceMetrics.createIndex({ "performance.throughput": -1, "timestamp": -1 });
db.nodePerformanceMetrics.createIndex({ "timestamp": -1 }, { expireAfterSeconds: 2592000 }); // TTL: 30 days

// Reward System Indexes

// User Rewards Collection Indexes
db.userRewards.createIndex({ "userId": 1 }, { unique: true });
db.userRewards.createIndex({ "currentPoints": -1 });
db.userRewards.createIndex({ "totalEarned": -1 });
db.userRewards.createIndex({ "level": 1, "currentPoints": -1 });
db.userRewards.createIndex({ "badges.badgeId": 1 });
db.userRewards.createIndex({ "achievements.achievementId": 1 });
db.userRewards.createIndex({ "lastActivity": -1 });

// Reward Transactions Collection Indexes
db.rewardTransactions.createIndex({ "userId": 1, "createdAt": -1 });
db.rewardTransactions.createIndex({ "transactionType": 1, "createdAt": -1 });
db.rewardTransactions.createIndex({ "category": 1, "action": 1 });
db.rewardTransactions.createIndex({ "points": -1, "createdAt": -1 });
db.rewardTransactions.createIndex({ "status": 1, "createdAt": -1 });
db.rewardTransactions.createIndex({ "metadata.collectionId": 1 });
db.rewardTransactions.createIndex({ "metadata.torrentId": 1 });
db.rewardTransactions.createIndex({ "metadata.nodeId": 1 });
db.rewardTransactions.createIndex({ "expiresAt": 1 }, { expireAfterSeconds: 0 }); // TTL based on expiresAt

// Reward Settings Collection Indexes
db.rewardSettings.createIndex({ "settingType": 1, "category": 1 });
db.rewardSettings.createIndex({ "action": 1, "isActive": 1 });
db.rewardSettings.createIndex({ "effectiveFrom": 1, "effectiveTo": 1 });
db.rewardSettings.createIndex({ "isActive": 1, "effectiveFrom": 1 });

// Reward Achievements Collection Indexes
db.rewardAchievements.createIndex({ "achievementId": 1 }, { unique: true });
db.rewardAchievements.createIndex({ "category": 1, "type": 1 });
db.rewardAchievements.createIndex({ "rarity": 1, "points": -1 });
db.rewardAchievements.createIndex({ "isActive": 1, "isHidden": 1 });

// Reward Badges Collection Indexes
db.rewardBadges.createIndex({ "badgeId": 1 }, { unique: true });
db.rewardBadges.createIndex({ "category": 1, "type": 1 });
db.rewardBadges.createIndex({ "rarity": 1, "displayPriority": -1 });
db.rewardBadges.createIndex({ "isActive": 1, "isLimited": 1 });
db.rewardBadges.createIndex({ "availableFrom": 1, "availableTo": 1 });

// Premium Features Collection Indexes
db.premiumFeatures.createIndex({ "featureId": 1 }, { unique: true });
db.premiumFeatures.createIndex({ "category": 1, "type": 1 });
db.premiumFeatures.createIndex({ "pricing.points": 1 });
db.premiumFeatures.createIndex({ "isActive": 1, "isPopular": 1 });
db.premiumFeatures.createIndex({ "requirements.minLevel": 1, "requirements.minPoints": 1 });

// User Premium Features Collection Indexes
db.userPremiumFeatures.createIndex({ "userId": 1, "status": 1 });
db.userPremiumFeatures.createIndex({ "featureId": 1, "status": 1 });
db.userPremiumFeatures.createIndex({ "status": 1, "expiresAt": 1 });
db.userPremiumFeatures.createIndex({ "purchasedAt": -1 });
db.userPremiumFeatures.createIndex({ "usage.lastUsed": -1 });
```

## Aggregation Pipelines

### Library Management
```javascript
// Get libraries with statistics
db.libraries.aggregate([
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
      collections: 0
    }
  }
]);
```

### Collection Management
```javascript
// Get collections with media statistics
db.collections.aggregate([
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
      items: 0
    }
  }
]);
```

### Media Items with Cache Info
```javascript
// Get media items with cache status
db.mediaItems.find({
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
});
```

### Background Jobs Status
```javascript
// Get active jobs with progress
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
});
```

### User Behavior Analytics
```javascript
// Get user engagement metrics
db.userBehaviorEvents.aggregate([
  {
    $match: {
      userId: ObjectId(userId),
      timestamp: { $gte: startDate, $lte: endDate }
    }
  },
  {
    $group: {
      _id: "$eventType",
      count: { $sum: 1 },
      totalDuration: { $sum: "$metadata.duration" },
      avgDuration: { $avg: "$metadata.duration" }
    }
  }
]);

// Get most popular content
db.userBehaviorEvents.aggregate([
  {
    $match: {
      eventType: "view",
      timestamp: { $gte: startDate, $lte: endDate }
    }
  },
  {
    $group: {
      _id: { targetType: "$targetType", targetId: "$targetId" },
      viewCount: { $sum: 1 },
      uniqueUsers: { $addToSet: "$userId" },
      totalDuration: { $sum: "$metadata.duration" }
    }
  },
  {
    $addFields: {
      uniqueUserCount: { $size: "$uniqueUsers" }
    }
  },
  {
    $sort: { viewCount: -1 }
  },
  {
    $limit: 100
  }
]);

// Get search analytics
db.searchAnalytics.aggregate([
  {
    $match: {
      timestamp: { $gte: startDate, $lte: endDate }
    }
  },
  {
    $group: {
      _id: "$query",
      searchCount: { $sum: 1 },
      avgResultCount: { $avg: "$resultCount" },
      avgSearchTime: { $avg: "$searchTime" },
      successRate: { $avg: { $cond: ["$searchSuccess", 1, 0] } },
      avgSatisfaction: { $avg: "$searchSatisfaction" }
    }
  },
  {
    $sort: { searchCount: -1 }
  },
  {
    $limit: 50
  }
]);
```

### Content Popularity Analysis
```javascript
// Get trending content
db.contentPopularity.aggregate([
  {
    $match: {
      period: "daily",
      date: { $gte: startDate, $lte: endDate }
    }
  },
  {
    $group: {
      _id: { targetType: "$targetType", targetId: "$targetId" },
      avgPopularityScore: { $avg: "$metrics.popularityScore" },
      avgTrendingScore: { $avg: "$metrics.trendingScore" },
      totalViews: { $sum: "$metrics.totalViews" },
      totalEngagement: { $sum: { $add: ["$metrics.likes", "$metrics.shares", "$metrics.favorites"] } }
    }
  },
  {
    $sort: { avgTrendingScore: -1 }
  },
  {
    $limit: 100
  }
]);

// Get content performance by demographics
db.contentPopularity.aggregate([
  {
    $match: {
      targetType: "media",
      targetId: ObjectId(mediaId)
    }
  },
  {
    $unwind: "$metrics.viewsByCountry"
  },
  {
    $group: {
      _id: "$metrics.viewsByCountry",
      totalViews: { $sum: "$metrics.totalViews" },
      avgEngagement: { $avg: "$metrics.engagementScore" }
    }
  },
  {
    $sort: { totalViews: -1 }
  }
]);
```

### User Segmentation
```javascript
// Segment users by behavior
db.userAnalytics.aggregate([
  {
    $match: {
      period: "monthly",
      date: { $gte: startDate, $lte: endDate }
    }
  },
  {
    $bucket: {
      groupBy: "$metrics.totalViews",
      boundaries: [0, 10, 50, 100, 500, 1000],
      default: "power_users",
      output: {
        count: { $sum: 1 },
        avgSearches: { $avg: "$metrics.totalSearches" },
        avgSessionDuration: { $avg: "$metrics.averageSessionDuration" },
        avgFavorites: { $avg: "$metrics.totalFavorites" }
      }
    }
  }
]);

// Get user preferences analysis
db.userAnalytics.aggregate([
  {
    $match: {
      period: "monthly",
      date: { $gte: startDate, $lte: endDate }
    }
  },
  {
    $unwind: "$preferences.favoriteTags"
  },
  {
    $group: {
      _id: "$preferences.favoriteTags",
      userCount: { $sum: 1 },
      avgViews: { $avg: "$metrics.totalViews" }
    }
  },
  {
    $sort: { userCount: -1 }
  },
  {
    $limit: 20
  }
]);
```

### Social Features Analytics

#### User Collections Analytics
```javascript
// Get popular collections by category
db.userCollections.aggregate([
  {
    $match: {
      status: "published",
      visibility: "public"
    }
  },
  {
    $group: {
      _id: "$category",
      totalCollections: { $sum: 1 },
      avgRating: { $avg: "$metadata.averageRating" },
      totalViews: { $sum: "$metadata.totalViews" },
      totalDownloads: { $sum: "$metadata.totalDownloads" },
      totalLikes: { $sum: "$metadata.totalLikes" }
    }
  },
  {
    $sort: { totalViews: -1 }
  }
]);

// Get trending collections
db.userCollections.aggregate([
  {
    $match: {
      status: "published",
      visibility: "public",
      publishedAt: { $gte: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000) } // Last 7 days
    }
  },
  {
    $addFields: {
      trendingScore: {
        $add: [
          { $multiply: ["$metadata.totalViews", 0.3] },
          { $multiply: ["$metadata.totalLikes", 0.4] },
          { $multiply: ["$metadata.totalDownloads", 0.3] }
        ]
      }
    }
  },
  {
    $sort: { trendingScore: -1 }
  },
  {
    $limit: 50
  }
]);

// Get user's collection statistics
db.userCollections.aggregate([
  {
    $match: {
      userId: userId,
      status: "published"
    }
  },
  {
    $group: {
      _id: null,
      totalCollections: { $sum: 1 },
      totalViews: { $sum: "$metadata.totalViews" },
      totalDownloads: { $sum: "$metadata.totalDownloads" },
      totalLikes: { $sum: "$metadata.totalLikes" },
      avgRating: { $avg: "$metadata.averageRating" },
      totalComments: { $sum: "$metadata.totalComments" }
    }
  }
]);
```

#### Rating System Analytics
```javascript
// Get collection rating distribution
db.collectionRatings.aggregate([
  {
    $match: {
      collectionId: ObjectId(collectionId),
      status: "active"
    }
  },
  {
    $group: {
      _id: "$rating",
      count: { $sum: 1 },
      avgQuality: { $avg: "$aspects.quality" },
      avgOriginality: { $avg: "$aspects.originality" },
      avgComposition: { $avg: "$aspects.composition" },
      avgTechnical: { $avg: "$aspects.technical" },
      avgCreativity: { $avg: "$aspects.creativity" }
    }
  },
  {
    $sort: { _id: 1 }
  }
]);

// Get top rated collections
db.collectionRatings.aggregate([
  {
    $match: {
      status: "active"
    }
  },
  {
    $group: {
      _id: "$collectionId",
      avgRating: { $avg: "$rating" },
      totalRatings: { $sum: 1 },
      avgQuality: { $avg: "$aspects.quality" },
      avgOriginality: { $avg: "$aspects.originality" },
      avgComposition: { $avg: "$aspects.composition" },
      avgTechnical: { $avg: "$aspects.technical" },
      avgCreativity: { $avg: "$aspects.creativity" }
    }
  },
  {
    $match: {
      totalRatings: { $gte: 5 } // Minimum 5 ratings
    }
  },
  {
    $sort: { avgRating: -1, totalRatings: -1 }
  },
  {
    $limit: 100
  }
]);
```

#### Follow System Analytics
```javascript
// Get user's followers and following
db.userFollows.aggregate([
  {
    $match: {
      followerId: userId,
      status: "active"
    }
  },
  {
    $group: {
      _id: "$followType",
      count: { $sum: 1 },
      targets: { $push: "$targetId" }
    }
  }
]);

// Get most followed users
db.userFollows.aggregate([
  {
    $match: {
      followType: "user",
      status: "active"
    }
  },
  {
    $group: {
      _id: "$followingId",
      followerCount: { $sum: 1 }
    }
  },
  {
    $sort: { followerCount: -1 }
  },
  {
    $limit: 50
  }
]);

// Get follow recommendations
db.userFollows.aggregate([
  {
    $match: {
      followerId: userId,
      status: "active"
    }
  },
  {
    $lookup: {
      from: "userFollows",
      localField: "followingId",
      foreignField: "followerId",
      as: "mutualFollows"
    }
  },
  {
    $unwind: "$mutualFollows"
  },
  {
    $group: {
      _id: "$mutualFollows.followingId",
      mutualCount: { $sum: 1 }
    }
  },
  {
    $match: {
      _id: { $ne: userId } // Exclude self
    }
  },
  {
    $sort: { mutualCount: -1 }
  },
  {
    $limit: 20
  }
]);
```

#### Torrent System Analytics
```javascript
// Get torrent statistics
db.torrents.aggregate([
  {
    $match: {
      status: "active"
    }
  },
  {
    $group: {
      _id: null,
      totalTorrents: { $sum: 1 },
      totalSeeders: { $sum: "$statistics.seeders" },
      totalLeechers: { $sum: "$statistics.leechers" },
      totalCompleted: { $sum: "$statistics.completed" },
      totalSize: { $sum: "$totalSize" },
      avgSeeders: { $avg: "$statistics.seeders" },
      avgLeechers: { $avg: "$statistics.leechers" }
    }
  }
]);

// Get most active seeders
db.torrentStatistics.aggregate([
  {
    $match: {
      status: "seeding"
    }
  },
  {
    $group: {
      _id: "$userId",
      totalUploaded: { $sum: "$statistics.uploaded" },
      totalDownloaded: { $sum: "$statistics.downloaded" },
      avgRatio: { $avg: "$statistics.ratio" },
      totalConnectedTime: { $sum: "$statistics.connectedTime" },
      activeTorrents: { $sum: 1 }
    }
  },
  {
    $addFields: {
      overallRatio: { $divide: ["$totalUploaded", { $max: ["$totalDownloaded", 1] }] }
    }
  },
  {
    $sort: { totalUploaded: -1 }
  },
  {
    $limit: 50
  }
]);

// Get torrent health by category
db.torrents.aggregate([
  {
    $match: {
      status: "active"
    }
  },
  {
    $group: {
      _id: "$category",
      totalTorrents: { $sum: 1 },
      avgSeeders: { $avg: "$statistics.seeders" },
      avgLeechers: { $avg: "$statistics.leechers" },
      totalSize: { $sum: "$totalSize" },
      healthyTorrents: {
        $sum: {
          $cond: [
            { $gt: ["$statistics.seeders", 0] },
            1,
            0
          ]
        }
      }
    }
  },
  {
    $addFields: {
      healthPercentage: {
        $multiply: [
          { $divide: ["$healthyTorrents", "$totalTorrents"] },
          100
        ]
      }
    }
  },
  {
    $sort: { healthPercentage: -1 }
  }
]);
```

#### Distribution Nodes Analytics
```javascript
// Get node performance by location
db.distributionNodes.aggregate([
  {
    $match: {
      status: "active"
    }
  },
  {
    $group: {
      _id: "$location.country",
      totalNodes: { $sum: 1 },
      avgUptime: { $avg: "$performance.uptime" },
      avgResponseTime: { $avg: "$performance.averageResponseTime" },
      avgDownloadSpeed: { $avg: "$performance.averageDownloadSpeed" },
      totalDataServed: { $sum: "$performance.totalDataServed" },
      avgQualityScore: { $avg: "$quality.score" }
    }
  },
  {
    $sort: { avgQualityScore: -1 }
  }
]);

// Get top performing nodes
db.distributionNodes.aggregate([
  {
    $match: {
      status: "active",
      "quality.score": { $gte: 80 }
    }
  },
  {
    $sort: {
      "quality.score": -1,
      "performance.uptime": -1,
      "performance.averageDownloadSpeed": -1
    }
  },
  {
    $limit: 20
  },
  {
    $project: {
      nodeId: 1,
      name: 1,
      location: 1,
      quality: 1,
      performance: 1,
      capabilities: 1
    }
  }
]);

// Get node assignment recommendations
db.distributionNodes.aggregate([
  {
    $match: {
      status: "active",
      "quality.score": { $gte: 70 },
      "performance.uptime": { $gte: 95 }
    }
  },
  {
    $addFields: {
      assignmentScore: {
        $add: [
          { $multiply: ["$quality.score", 0.4] },
          { $multiply: ["$performance.uptime", 0.3] },
          { $multiply: ["$performance.averageDownloadSpeed", 0.3] }
        ]
      }
    }
  },
  {
    $sort: { assignmentScore: -1 }
  },
  {
    $limit: 10
  }
]);
```

### Reward System Analytics

#### User Rewards Analytics
```javascript
// Get top users by points
db.userRewards.aggregate([
  {
    $sort: { currentPoints: -1 }
  },
  {
    $limit: 100
  },
  {
    $project: {
      userId: 1,
      currentPoints: 1,
      totalEarned: 1,
      level: 1,
      badges: { $size: "$badges" },
      achievements: { $size: "$achievements" },
      lastActivity: 1
    }
  }
]);

// Get level distribution
db.userRewards.aggregate([
  {
    $group: {
      _id: "$level",
      userCount: { $sum: 1 },
      avgPoints: { $avg: "$currentPoints" },
      totalPoints: { $sum: "$currentPoints" }
    }
  },
  {
    $sort: { avgPoints: -1 }
  }
]);

// Get user earning statistics by category
db.userRewards.aggregate([
  {
    $project: {
      userId: 1,
      uploadPoints: "$statistics.uploads.totalPoints",
      seedingPoints: "$statistics.seeding.totalPoints",
      nodePoints: "$statistics.nodeOperation.totalPoints",
      tagPoints: "$statistics.tagCreation.totalPoints",
      torrentPoints: "$statistics.torrentCreation.totalPoints",
      socialPoints: "$statistics.social.totalPoints"
    }
  },
  {
    $group: {
      _id: null,
      avgUploadPoints: { $avg: "$uploadPoints" },
      avgSeedingPoints: { $avg: "$seedingPoints" },
      avgNodePoints: { $avg: "$nodePoints" },
      avgTagPoints: { $avg: "$tagPoints" },
      avgTorrentPoints: { $avg: "$torrentPoints" },
      avgSocialPoints: { $avg: "$socialPoints" }
    }
  }
]);
```

#### Reward Transactions Analytics
```javascript
// Get transaction statistics by category
db.rewardTransactions.aggregate([
  {
    $match: {
      status: "completed",
      createdAt: { $gte: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000) } // Last 30 days
    }
  },
  {
    $group: {
      _id: "$category",
      totalTransactions: { $sum: 1 },
      totalPoints: { $sum: "$points" },
      avgPoints: { $avg: "$points" },
      earningTransactions: {
        $sum: {
          $cond: [{ $gt: ["$points", 0] }, 1, 0]
        }
      },
      spendingTransactions: {
        $sum: {
          $cond: [{ $lt: ["$points", 0] }, 1, 0]
        }
      }
    }
  },
  {
    $sort: { totalPoints: -1 }
  }
]);

// Get daily points flow
db.rewardTransactions.aggregate([
  {
    $match: {
      status: "completed",
      createdAt: { $gte: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000) }
    }
  },
  {
    $group: {
      _id: {
        year: { $year: "$createdAt" },
        month: { $month: "$createdAt" },
        day: { $dayOfMonth: "$createdAt" }
      },
      totalEarned: {
        $sum: {
          $cond: [{ $gt: ["$points", 0] }, "$points", 0]
        }
      },
      totalSpent: {
        $sum: {
          $cond: [{ $lt: ["$points", 0] }, { $abs: "$points" }, 0]
        }
      },
      transactionCount: { $sum: 1 }
    }
  },
  {
    $sort: { "_id.year": -1, "_id.month": -1, "_id.day": -1 }
  }
]);

// Get top earners by category
db.rewardTransactions.aggregate([
  {
    $match: {
      transactionType: "earn",
      status: "completed",
      createdAt: { $gte: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000) }
    }
  },
  {
    $group: {
      _id: {
        userId: "$userId",
        category: "$category"
      },
      totalPoints: { $sum: "$points" },
      transactionCount: { $sum: 1 },
      avgPoints: { $avg: "$points" }
    }
  },
  {
    $sort: { totalPoints: -1 }
  },
  {
    $limit: 50
  }
]);
```

#### Achievement Analytics
```javascript
// Get achievement completion rates
db.rewardAchievements.aggregate([
  {
    $lookup: {
      from: "userRewards",
      localField: "achievementId",
      foreignField: "achievements.achievementId",
      as: "completedBy"
    }
  },
  {
    $addFields: {
      completionCount: { $size: "$completedBy" }
    }
  },
  {
    $lookup: {
      from: "userRewards",
      localField: "achievementId",
      foreignField: "achievements.achievementId",
      as: "allUsers"
    }
  },
  {
    $addFields: {
      totalUsers: { $size: "$allUsers" },
      completionRate: {
        $multiply: [
          { $divide: ["$completionCount", { $max: ["$totalUsers", 1] }] },
          100
        ]
      }
    }
  },
  {
    $project: {
      achievementId: 1,
      name: 1,
      category: 1,
      rarity: 1,
      points: 1,
      completionCount: 1,
      completionRate: 1
    }
  },
  {
    $sort: { completionRate: -1 }
  }
]);

// Get rare achievements
db.rewardAchievements.aggregate([
  {
    $match: {
      rarity: { $in: ["rare", "epic", "legendary"] }
    }
  },
  {
    $lookup: {
      from: "userRewards",
      localField: "achievementId",
      foreignField: "achievements.achievementId",
      as: "completedBy"
    }
  },
  {
    $addFields: {
      completionCount: { $size: "$completedBy" }
    }
  },
  {
    $sort: { completionCount: 1 }
  },
  {
    $limit: 20
  }
]);
```

#### Premium Features Analytics
```javascript
// Get popular premium features
db.userPremiumFeatures.aggregate([
  {
    $match: {
      status: "active"
    }
  },
  {
    $group: {
      _id: "$featureId",
      purchaseCount: { $sum: 1 },
      totalPointsSpent: { $sum: "$pointsSpent" },
      avgPointsSpent: { $avg: "$pointsSpent" }
    }
  },
  {
    $lookup: {
      from: "premiumFeatures",
      localField: "_id",
      foreignField: "_id",
      as: "feature"
    }
  },
  {
    $unwind: "$feature"
  },
  {
    $project: {
      featureName: "$feature.name",
      category: "$feature.category",
      purchaseCount: 1,
      totalPointsSpent: 1,
      avgPointsSpent: 1
    }
  },
  {
    $sort: { purchaseCount: -1 }
  }
]);

// Get feature usage statistics
db.userPremiumFeatures.aggregate([
  {
    $match: {
      status: "active"
    }
  },
  {
    $group: {
      _id: "$featureId",
      totalUses: { $sum: "$usage.totalUses" },
      avgUses: { $avg: "$usage.totalUses" },
      activeUsers: { $sum: 1 }
    }
  },
  {
    $lookup: {
      from: "premiumFeatures",
      localField: "_id",
      foreignField: "_id",
      as: "feature"
    }
  },
  {
    $unwind: "$feature"
  },
  {
    $project: {
      featureName: "$feature.name",
      category: "$feature.category",
      totalUses: 1,
      avgUses: 1,
      activeUsers: 1,
      usagePerUser: { $divide: ["$totalUses", "$activeUsers"] }
    }
  },
  {
    $sort: { totalUses: -1 }
  }
]);
```

## Performance Optimizations

### 1. Connection Pooling
```csharp
services.AddSingleton<IMongoClient>(provider =>
{
    var options = provider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
    var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);
    
    settings.MaxConnectionPoolSize = options.ConnectionPoolSize;
    settings.ConnectTimeout = options.ConnectionTimeout;
    settings.SocketTimeout = options.SocketTimeout;
    settings.ServerSelectionTimeout = options.ServerSelectionTimeout;
    
    return new MongoClient(settings);
});
```

### 2. Change Streams for Real-time Updates
```csharp
public class ChangeStreamService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<ChangeStreamService> _logger;

    public async Task StartWatchingAsync(CancellationToken cancellationToken)
    {
        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
            .Match(change => change.OperationType == ChangeStreamOperationType.Insert ||
                           change.OperationType == ChangeStreamOperationType.Update ||
                           change.OperationType == ChangeStreamOperationType.Replace);

        var options = new ChangeStreamOptions
        {
            FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
        };

        var collection = _database.GetCollection<BsonDocument>("backgroundJobs");
        var changeStream = collection.Watch(pipeline, options, cancellationToken);

        await foreach (var change in changeStream.ToAsyncEnumerable())
        {
            _logger.LogInformation("Change detected: {OperationType}", change.OperationType);
            // Handle change notification
        }
    }
}
```

### 3. Bulk Operations
```csharp
public class BulkOperations
{
    public async Task BulkInsertMediaItemsAsync(IEnumerable<MediaItem> items)
    {
        var collection = _database.GetCollection<MediaItem>("mediaItems");
        var operations = items.Select(item => new InsertOneModel<MediaItem>(item));
        
        await collection.BulkWriteAsync(operations, new BulkWriteOptions
        {
            IsOrdered = false,
            BypassDocumentValidation = false
        });
    }

    public async Task BulkUpdateCacheInfoAsync(IEnumerable<MediaItem> items)
    {
        var collection = _database.GetCollection<MediaItem>("mediaItems");
        var operations = items.Select(item => 
            new UpdateOneModel<MediaItem>(
                Builders<MediaItem>.Filter.Eq(x => x.Id, item.Id),
                Builders<MediaItem>.Update.Set(x => x.CacheInfo, item.CacheInfo)
            )
        );
        
        await collection.BulkWriteAsync(operations);
    }
}
```

### 4. Aggregation Pipeline Optimization
```csharp
public class OptimizedQueries
{
    public async Task<List<Collection>> GetCollectionsWithStatsAsync(ObjectId libraryId, int page, int pageSize)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument("libraryId", libraryId)),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "mediaItems" },
                { "localField", "_id" },
                { "foreignField", "collectionId" },
                { "as", "items" }
            }),
            new BsonDocument("$addFields", new BsonDocument
            {
                { "statistics.totalItems", new BsonDocument("$size", "$items") },
                { "statistics.totalSize", new BsonDocument("$sum", "$items.fileSize") }
            }),
            new BsonDocument("$project", new BsonDocument("items", 0)),
            new BsonDocument("$skip", page * pageSize),
            new BsonDocument("$limit", pageSize)
        };

        var collection = _database.GetCollection<Collection>("collections");
        return await collection.Aggregate<Collection>(pipeline).ToListAsync();
    }
}
```

## Data Migration from PostgreSQL

### Migration Script
```csharp
public class MigrationService
{
    public async Task MigrateFromPostgresqlAsync()
    {
        // 1. Export data from PostgreSQL
        var collections = await _postgresContext.Collections.ToListAsync();
        var images = await _postgresContext.Images.ToListAsync();
        var cacheInfos = await _postgresContext.CacheInfos.ToListAsync();

        // 2. Transform data to MongoDB format
        var mongoCollections = collections.Select(TransformToMongoCollection);
        var mongoMediaItems = images.Select(TransformToMongoMediaItem);
        var mongoCacheFolders = cacheInfos.Select(TransformToMongoCacheFolder);

        // 3. Insert into MongoDB
        await _mongoContext.Collections.InsertManyAsync(mongoCollections);
        await _mongoContext.MediaItems.InsertManyAsync(mongoMediaItems);
        await _mongoContext.CacheFolders.InsertManyAsync(mongoCacheFolders);

        // 4. Create indexes
        await CreateIndexesAsync();
    }

    private Collection TransformToMongoCollection(PostgresCollection pgCollection)
    {
        return new Collection
        {
            Id = pgCollection.Id,
            Name = pgCollection.Name,
            Path = pgCollection.Path,
            Type = pgCollection.Type.ToString().ToLower(),
            Settings = new CollectionSettings
            {
                ThumbnailSize = 300,
                CacheEnabled = true,
                AutoScan = true
            },
            Metadata = new CollectionMetadata
            {
                Description = "",
                Tags = pgCollection.Tags?.Select(t => t.Tag).ToList() ?? new List<string>(),
                CreatedDate = pgCollection.CreatedAt,
                LastModified = pgCollection.UpdatedAt
            },
            Statistics = new CollectionStatistics
            {
                ImageCount = 0, // Will be calculated
                VideoCount = 0,
                TotalSize = 0
            },
            CreatedAt = pgCollection.CreatedAt,
            UpdatedAt = pgCollection.UpdatedAt
        };
    }
}
```

## Monitoring and Maintenance

### Health Check
```csharp
public class MongoHealthCheck : IHealthCheck
{
    private readonly IMongoDatabase _database;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("MongoDB is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB is unhealthy", ex);
        }
    }
}
```

### Cleanup Tasks
```javascript
// Cleanup expired cache entries
db.mediaItems.updateMany(
  { "cacheInfo.lastGenerated": { $lt: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000) } },
  { $set: { "cacheInfo.needsRegeneration": true } }
);

// Cleanup old background jobs
db.backgroundJobs.deleteMany({
  "status": { $in: ["completed", "failed", "cancelled"] },
  "createdAt": { $lt: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000) }
});
```

## Conclusion

MongoDB database design này được tối ưu cho:

1. **Performance**: Aggregation pipelines và indexes được thiết kế cho các query patterns phổ biến
2. **Scalability**: Schema hỗ trợ horizontal scaling và sharding
3. **Flexibility**: Document structure linh hoạt với embedded documents
4. **Real-time**: Change streams cho real-time updates
5. **Monitoring**: Comprehensive logging và performance metrics
6. **Security**: Audit logs và error tracking
7. **Maintenance**: TTL indexes và automated cleanup

Database này sẽ hỗ trợ hệ thống image viewer với hàng triệu images và hàng nghìn collections một cách hiệu quả, với khả năng scale và maintain dễ dàng hơn so với relational database.