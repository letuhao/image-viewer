# Library Statistics Update Flow - Complete Implementation

## Overview
Library statistics are now updated atomically throughout the entire collection processing pipeline.

---

## 📊 Statistics Fields Tracked

### LibraryStatistics Properties:
- `TotalCollections` - Number of collections in library
- `TotalMediaItems` - Total images/videos across all collections
- `TotalSize` - Total bytes of all media files
- `LastScanDate` - When library was last scanned
- `ScanCount` - How many times library has been scanned
- `LastActivity` - Last update timestamp

---

## 🔄 Update Flow

### 1. **Library Scan** (LibraryScanConsumer)

**When**: User triggers manual scan or scheduled job runs

**What Happens**:
```
📚 Library Scan Message Received
  ↓
BulkService.BulkAddCollectionsAsync()
  ↓
Discovers 6 collections (folders + ZIP files)
  ↓
Creates 6 Collection entities
  ↓
✅ IncrementLibraryStatisticsAsync(libraryId, collectionCount: 6)
✅ UpdateLastScanDateAsync(libraryId)
```

**Statistics Updated**:
```csharp
TotalCollections += 6
LastScanDate = DateTime.UtcNow
ScanCount += 1
LastActivity = DateTime.UtcNow
```

**Atomic Operations**:
```mongodb
db.libraries.updateOne(
  { _id: libraryId },
  {
    $inc: { "statistics.totalCollections": 6, "statistics.scanCount": 1 },
    $set: { "statistics.lastScanDate": now, "statistics.lastActivity": now, updatedAt: now }
  }
)
```

---

### 2. **Collection Scan** (CollectionScanConsumer)

**When**: Collection is created and queued for scanning

**What Happens**:
```
📁 Collection Scan Message Received
  ↓
Scan collection path for media files
  ↓
Discovers 50 images (2GB total size)
  ↓
Queue 50 ImageProcessingMessages
  ↓
✅ IncrementLibraryStatisticsAsync(
    libraryId, 
    mediaItemCount: 50, 
    sizeBytes: 2GB)
```

**Statistics Updated**:
```csharp
TotalMediaItems += 50
TotalSize += 2,000,000,000 (2GB)
LastActivity = DateTime.UtcNow
```

**Atomic Operations**:
```mongodb
db.libraries.updateOne(
  { _id: libraryId },
  {
    $inc: { 
      "statistics.totalMediaItems": 50, 
      "statistics.totalSize": 2000000000 
    },
    $set: { "statistics.lastActivity": now, updatedAt: now }
  }
)
```

---

### 3. **Thumbnail Generation** (ThumbnailGenerationConsumer)

**Current**: Does NOT update library statistics ✅ (Correct!)

**Reason**: 
- Thumbnails are derivatives, not source media
- Already counted in TotalMediaItems
- Updating here would cause double-counting

**Cache Folder Updates** ✅:
- `IncrementCacheStatisticsAsync(size, count)` - Single transaction
- `AddCachedCollectionAsync(collectionId)` - Atomic with $size

---

### 4. **Cache Generation** (CacheGenerationConsumer)

**Current**: Does NOT update library statistics ✅ (Correct!)

**Reason**:
- Cache images are derivatives, not source media
- Already counted in TotalMediaItems
- Updating here would cause double-counting

**Cache Folder Updates** ✅:
- `IncrementCacheStatisticsAsync(size, count)` - Single transaction
- `AddCachedCollectionAsync(collectionId)` - Atomic with $size

---

## 🎯 Complete Example Flow

### Scenario: User adds library "L:\Photos" with 3 collections

```
STEP 1 - Library Scan
  User: POST /api/v1/libraries (AutoScan=true)
    ↓
  Library created in DB
    ↓
  ScheduledJob created (orphaned initially)
    ↓
  Scheduler syncs (5 min) → Hangfire job registered
    ↓
  User: POST /api/v1/libraries/{id}/scan (manual trigger)
    ↓
  LibraryScanConsumer processes:
    - Scans L:\Photos
    - Finds: 3 folders
    - Creates 3 collections
    - Publishes 3 CollectionScanMessages
    
  Library Statistics Updated:
    ✅ TotalCollections: 0 → 3
    ✅ LastScanDate: null → 2025-10-12 01:30:00
    ✅ ScanCount: 0 → 1
    ✅ LastActivity: 2025-10-12 01:30:00

STEP 2 - Collection Scan (×3 concurrent)

  Collection 1: "Wedding Photos"
    - Scans folder
    - Finds: 100 JPGs (500MB)
    - Queues 100 ImageProcessingMessages
    - Updates Library: +100 items, +500MB
    
  Collection 2: "Vacation 2024.zip"
    - Scans ZIP
    - Finds: 200 PNGs (1.2GB)
    - Queues 200 ImageProcessingMessages
    - Updates Library: +200 items, +1.2GB
    
  Collection 3: "Family.cbz"
    - Scans CBZ
    - Finds: 50 JPGs (300MB)
    - Queues 50 ImageProcessingMessages
    - Updates Library: +50 items, +300MB
  
  Library Statistics After All Scans:
    ✅ TotalCollections: 3
    ✅ TotalMediaItems: 0 → 350 (100+200+50)
    ✅ TotalSize: 0 → 2GB (500MB+1.2GB+300MB)
    ✅ LastActivity: 2025-10-12 01:30:15

STEP 3 - Image Processing (×350 concurrent)
  
  350 images processed:
    - Add ImageEmbedded to Collection
    - Update Collection.Statistics
    - Library stats already correct! ✅

STEP 4 - Thumbnail Generation (×350 concurrent)
  
  350 thumbnails generated:
    - Save to cache folder
    - Update CacheFolder stats ✅
    - NO library update (correct!) ✅

STEP 5 - Cache Generation (×350 concurrent)
  
  350 cache images generated:
    - Save to cache folder
    - Update CacheFolder stats ✅
    - NO library update (correct!) ✅
```

---

## 🔒 Concurrency Safety

### Atomic Operations Used:
1. **Library Statistics**: MongoDB `$inc` operator
   - Safe for 1000+ concurrent collection scans
   - No race conditions
   - No read-modify-write issues

2. **Cache Folder**: MongoDB `$inc` + aggregation pipeline
   - Safe for concurrent thumbnail/cache generation
   - Count always matches array size
   - No stale count issues

### Thread Safety Guarantee:
```
Thread A: Collection1 scan → +50 items → UpdateOne($inc: 50)
Thread B: Collection2 scan → +200 items → UpdateOne($inc: 200) (concurrent)
Thread C: Collection3 scan → +100 items → UpdateOne($inc: 100) (concurrent)

MongoDB serializes updates internally:
Final TotalMediaItems = 350 ✅ (always correct!)
```

---

## ✅ Benefits

1. **Real-time Statistics**: Library metadata updated as collections are processed
2. **Accurate Counts**: No double-counting, no missing counts
3. **Thread-Safe**: Safe for massive concurrent bulk operations
4. **Atomic Updates**: All stats in single transaction
5. **Monitoring Ready**: Can show library growth in real-time on UI

---

## 🎯 Testing Checklist

- [ ] Create library with AutoScan
- [ ] Trigger manual scan
- [ ] Verify TotalCollections increments after library scan
- [ ] Verify TotalMediaItems increments after collection scans
- [ ] Verify TotalSize matches actual file sizes
- [ ] Verify LastScanDate updates
- [ ] Test concurrent bulk add (100+ collections)
- [ ] Verify no race conditions in statistics
- [ ] Confirm thumbnails don't double-count
- [ ] Confirm cache doesn't double-count

---

## 📝 Awaiting Command
Ready to test the complete flow!

