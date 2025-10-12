# Redis Collection Index Migration Plan

## 📊 Current Collection Query Analysis

### All Collection Query Methods in CollectionService.cs

| Method | Current Implementation | Sort Logic | Use Redis? | Priority |
|--------|----------------------|------------|-----------|----------|
| **GetCollectionsAsync** | MongoDB Find + Sort by CreatedAt DESC | Hardcoded | ✅ YES | 🔴 HIGH |
| **GetCollectionNavigationAsync** | MongoDB COUNT + Filters | By sortBy param | ✅ YES | 🔴 HIGH |
| **GetCollectionSiblingsAsync** | Load ALL collections | By sortBy param | ✅ YES | 🔴 HIGH |
| **GetSortedCollectionsAsync** | MongoDB Find + Sort | By sortBy param | ✅ YES | 🟡 MEDIUM |
| **GetCollectionsByLibraryAsync** | MongoDB Find + Sort by CreatedAt DESC | Hardcoded | ✅ YES | 🟡 MEDIUM |
| **GetCollectionsByTypeAsync** | MongoDB Find + Sort by CreatedAt DESC | Hardcoded | ✅ YES | 🟡 MEDIUM |
| **GetRecentCollectionsAsync** | Repository method (likely CreatedAt DESC) | Hardcoded | ✅ YES | 🟢 LOW |
| **GetTopCollectionsByActivityAsync** | Repository method (by ViewCount) | Special | 🤔 MAYBE | 🟢 LOW |
| **GetCollectionsByFilterAsync** | Complex filters | Multiple | ❌ NO | - |
| **GetCollectionByIdAsync** | By ID lookup | None | ❌ NO | - |
| **GetCollectionByPathAsync** | By path lookup | None | ❌ NO | - |
| **GetCollectionsByLibraryIdAsync** | By LibraryId filter | None | 🤔 MAYBE | 🟢 LOW |
| **GetTotalCollectionsCountAsync** | MongoDB COUNT | None | ✅ YES | 🔴 HIGH |
| **GetCollectionStatisticsAsync** | Aggregation | None | ❌ NO | - |

---

## 🎯 Migration Strategy

### Phase 1: Core Index Infrastructure (TODAY)

#### 1.1 Enhance RedisCollectionIndexService
Add missing methods to support ALL use cases:

```csharp
public interface ICollectionIndexService
{
    // Existing methods
    Task RebuildIndexAsync(CancellationToken cancellationToken = default);
    Task AddOrUpdateCollectionAsync(Collection collection, CancellationToken cancellationToken = default);
    Task RemoveCollectionAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
    Task<CollectionNavigationResult> GetNavigationAsync(...);
    Task<CollectionSiblingsResult> GetSiblingsAsync(...);
    Task<bool> IsIndexValidAsync(CancellationToken cancellationToken = default);
    Task<CollectionIndexStats> GetIndexStatsAsync(CancellationToken cancellationToken = default);
    
    // NEW: Add these methods
    Task<CollectionPageResult> GetCollectionPageAsync(
        int page, 
        int pageSize, 
        string sortBy = "updatedAt", 
        string sortDirection = "desc",
        CancellationToken cancellationToken = default);
    
    Task<List<CollectionSummary>> GetCollectionsByLibraryAsync(
        ObjectId libraryId,
        int page,
        int pageSize,
        string sortBy = "updatedAt",
        string sortDirection = "desc",
        CancellationToken cancellationToken = default);
    
    Task<List<CollectionSummary>> GetCollectionsByTypeAsync(
        CollectionType type,
        int page,
        int pageSize,
        string sortBy = "updatedAt",
        string sortDirection = "desc",
        CancellationToken cancellationToken = default);
    
    Task<int> GetTotalCollectionsCountAsync(CancellationToken cancellationToken = default);
    
    Task<int> GetCollectionsCountByLibraryAsync(ObjectId libraryId, CancellationToken cancellationToken = default);
    
    Task<int> GetCollectionsCountByTypeAsync(CollectionType type, CancellationToken cancellationToken = default);
}
```

#### 1.2 Enhance CollectionSummary
Add fields needed for filtering and display:

```csharp
public class CollectionSummary
{
    // Existing fields
    public string Id { get; set; }
    public string Name { get; set; }
    public string? FirstImageId { get; set; }
    public string? FirstImageThumbnailUrl { get; set; }
    public int ImageCount { get; set; }
    public int ThumbnailCount { get; set; }
    public int CacheCount { get; set; }
    public long TotalSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // NEW: Add these fields
    public string LibraryId { get; set; }           // For filtering by library
    public string? Description { get; set; }        // For display
    public CollectionType Type { get; set; }        // For filtering by type (Folder/CompressedFile)
    public List<string> Tags { get; set; }          // For search/filter (future)
    public string Path { get; set; }                // For uniqueness checks
}
```

#### 1.3 Add Secondary Indexes in Redis
For filtering by library and type:

```
collection_index:by_library:{libraryId}:sorted:{field}:{direction}  → Sorted Set
collection_index:by_type:{type}:sorted:{field}:{direction}          → Sorted Set
```

---

### Phase 2: Migrate CollectionService Methods

#### 2.1 HIGH Priority (Immediate Performance Impact)

**A. GetCollectionsAsync (Collection List Page)**
```csharp
// BEFORE: MongoDB query
public async Task<IEnumerable<Collection>> GetCollectionsAsync(int page = 1, int pageSize = 20)
{
    var skip = (page - 1) * pageSize;
    return await _collectionRepository.FindAsync(
        Builders<Collection>.Filter.Empty,
        Builders<Collection>.Sort.Descending(c => c.CreatedAt), // Hardcoded!
        pageSize,
        skip
    );
}

// AFTER: Redis index with flexible sorting
public async Task<CollectionPageResult> GetCollectionsAsync(
    int page = 1, 
    int pageSize = 20,
    string sortBy = "updatedAt",
    string sortDirection = "desc")
{
    // Use Redis index for pagination
    return await _collectionIndexService.GetCollectionPageAsync(
        page, pageSize, sortBy, sortDirection);
}
```

**B. GetCollectionNavigationAsync**
```csharp
// BEFORE: MongoDB COUNT + filters (slow)
public async Task<CollectionNavigationDto> GetCollectionNavigationAsync(...)
{
    // Complex MongoDB queries...
}

// AFTER: Redis ZRANK (fast)
public async Task<CollectionNavigationDto> GetCollectionNavigationAsync(
    ObjectId collectionId, 
    string sortBy = "updatedAt", 
    string sortDirection = "desc")
{
    var result = await _collectionIndexService.GetNavigationAsync(
        collectionId, sortBy, sortDirection);
    
    return new CollectionNavigationDto
    {
        PreviousCollectionId = result.PreviousCollectionId,
        NextCollectionId = result.NextCollectionId,
        CurrentPosition = result.CurrentPosition,
        TotalCollections = result.TotalCollections,
        HasPrevious = result.HasPrevious,
        HasNext = result.HasNext
    };
}
```

**C. GetCollectionSiblingsAsync**
```csharp
// BEFORE: Load ALL 24k collections into memory!
public async Task<CollectionSiblingsDto> GetCollectionSiblingsAsync(...)
{
    var allCollections = (await GetSortedCollectionsAsync(sortBy, sortDirection)).ToList();
    // ... pagination logic ...
}

// AFTER: Redis ZRANGE (fast, no memory overhead)
public async Task<CollectionSiblingsDto> GetCollectionSiblingsAsync(
    ObjectId collectionId, 
    int page = 1, 
    int pageSize = 20, 
    string sortBy = "updatedAt", 
    string sortDirection = "desc")
{
    var result = await _collectionIndexService.GetSiblingsAsync(
        collectionId, page, pageSize, sortBy, sortDirection);
    
    // Convert to DTOs...
    return dto;
}
```

**D. GetTotalCollectionsCountAsync**
```csharp
// BEFORE: MongoDB COUNT
public async Task<long> GetTotalCollectionsCountAsync()
{
    return await _collectionRepository.CountAsync(Builders<Collection>.Filter.Empty);
}

// AFTER: Redis ZCARD (O(1))
public async Task<long> GetTotalCollectionsCountAsync()
{
    return await _collectionIndexService.GetTotalCollectionsCountAsync();
}
```

#### 2.2 MEDIUM Priority (Good for Consistency)

**E. GetCollectionsByLibraryAsync**
```csharp
// BEFORE: MongoDB query with hardcoded sort
public async Task<IEnumerable<Collection>> GetCollectionsByLibraryAsync(
    ObjectId libraryId, int page = 1, int pageSize = 20)
{
    var skip = (page - 1) * pageSize;
    return await _collectionRepository.FindAsync(
        Builders<Collection>.Filter.Eq(c => c.LibraryId, libraryId),
        Builders<Collection>.Sort.Descending(c => c.CreatedAt), // Hardcoded!
        pageSize,
        skip
    );
}

// AFTER: Redis secondary index with flexible sorting
public async Task<List<CollectionSummary>> GetCollectionsByLibraryAsync(
    ObjectId libraryId, 
    int page = 1, 
    int pageSize = 20,
    string sortBy = "updatedAt",
    string sortDirection = "desc")
{
    return await _collectionIndexService.GetCollectionsByLibraryAsync(
        libraryId, page, pageSize, sortBy, sortDirection);
}
```

**F. GetCollectionsByTypeAsync**
```csharp
// Similar to GetCollectionsByLibraryAsync
// Use Redis secondary index: collection_index:by_type:{type}:sorted:...
```

**G. GetSortedCollectionsAsync**
```csharp
// BEFORE: Used internally, loads many/all collections
public async Task<IEnumerable<Collection>> GetSortedCollectionsAsync(
    string sortBy = "updatedAt", 
    string sortDirection = "desc", 
    int? limit = null)
{
    return await _collectionRepository.FindAsync(...);
}

// AFTER: Use Redis index
public async Task<List<CollectionSummary>> GetSortedCollectionsAsync(
    string sortBy = "updatedAt", 
    string sortDirection = "desc", 
    int? limit = null)
{
    return await _collectionIndexService.GetCollectionPageAsync(
        1, limit ?? int.MaxValue, sortBy, sortDirection);
}
```

#### 2.3 LOW Priority (Special Cases)

**H. GetRecentCollectionsAsync**
```csharp
// Can use Redis index sorted by createdAt DESC, limit 10
public async Task<List<CollectionSummary>> GetRecentCollectionsAsync(int limit = 10)
{
    return await _collectionIndexService.GetCollectionPageAsync(
        1, limit, "createdAt", "desc");
}
```

**I. GetTopCollectionsByActivityAsync**
```csharp
// This needs ViewCount sorting - NOT in current index!
// Options:
// 1. Add viewCount to sorted sets (requires index rebuild on every view)
// 2. Keep as MongoDB query (acceptable for "top 10" queries)
// Recommendation: Keep as MongoDB query for now
```

---

### Phase 3: Update All Controllers

#### API Controllers to Update:

**A. CollectionsController**
```csharp
// Add sortBy and sortDirection parameters to:
[HttpGet]
public async Task<IActionResult> GetCollections(
    [FromQuery] int page = 1, 
    [FromQuery] int pageSize = 20,
    [FromQuery] string sortBy = "updatedAt",      // NEW
    [FromQuery] string sortDirection = "desc")    // NEW

[HttpGet("library/{libraryId}")]
public async Task<IActionResult> GetCollectionsByLibrary(
    string libraryId,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string sortBy = "updatedAt",      // NEW
    [FromQuery] string sortDirection = "desc")    // NEW
```

**B. Update API responses to return CollectionSummary DTOs**
- Lighter payloads
- Faster serialization
- Consistent with Redis data

---

### Phase 4: Update Frontend

#### A. Collection List Page (`Collections.tsx`)
```typescript
// Add sort controls
const [sortBy, setSortBy] = useState('updatedAt');
const [sortDirection, setSortDirection] = useState('desc');

// Update API call
const { data, isLoading } = useQuery({
  queryKey: ['collections', page, pageSize, sortBy, sortDirection, search],
  queryFn: () => collectionsApi.getAll(page, pageSize, sortBy, sortDirection, search)
});

// Add UI controls
<SortControls 
  sortBy={sortBy}
  sortDirection={sortDirection}
  onSortChange={(field, dir) => {
    setSortBy(field);
    setSortDirection(dir);
  }}
/>
```

#### B. Collection Detail Sidebar (`CollectionDetail.tsx`)
```typescript
// Update navigation API calls to use accurate positions
const { data: navigation } = useQuery({
  queryKey: ['collection-navigation', id, sortBy, sortDirection],
  queryFn: () => collectionsApi.getNavigation(id, sortBy, sortDirection)
});

// Display accurate position
<div>Collection {navigation.currentPosition} of {navigation.totalCollections}</div>
```

---

## 🔄 Index Sync Strategy

### When to Update Index:

| Event | Action | Method |
|-------|--------|--------|
| **Collection Created** | Add to index | `AddOrUpdateCollectionAsync` |
| **Collection Updated** | Update in index | `AddOrUpdateCollectionAsync` |
| **Collection Deleted** | Remove from index | `RemoveCollectionAsync` |
| **Bulk Import (>100 collections)** | Full rebuild | `RebuildIndexAsync` |
| **Collection Scan Complete** | Update statistics | `AddOrUpdateCollectionAsync` |
| **Application Startup** | Validate & rebuild if needed | `IsIndexValidAsync` + `RebuildIndexAsync` |

### Implementation in CollectionService:

```csharp
public async Task<Collection> CreateCollectionAsync(...)
{
    var collection = await _collectionRepository.CreateAsync(newCollection);
    
    // Update Redis index
    await _collectionIndexService.AddOrUpdateCollectionAsync(collection);
    
    return collection;
}

public async Task<Collection> UpdateCollectionAsync(...)
{
    await _collectionRepository.UpdateAsync(collection);
    
    // Update Redis index
    await _collectionIndexService.AddOrUpdateCollectionAsync(collection);
    
    return collection;
}

public async Task DeleteCollectionAsync(ObjectId collectionId)
{
    await _collectionRepository.SoftDeleteAsync(collectionId);
    
    // Remove from Redis index
    await _collectionIndexService.RemoveCollectionAsync(collectionId);
}
```

---

## 📈 Performance Expectations

### Current (MongoDB):
- Collection List: 200-500ms
- Navigation: 500-2000ms
- Siblings: 2000-5000ms
- Total Count: 100-200ms

### After Redis Index:
- Collection List: 10-30ms (10-50x faster)
- Navigation: 10-20ms (50-100x faster)
- Siblings: 20-30ms (100-250x faster)
- Total Count: <1ms (100-200x faster)

### Memory Usage:
- Index: ~30 MB for 24k collections
- Per collection: ~500 bytes (including extra fields)

---

## ✅ Implementation Checklist

### Phase 1: Infrastructure
- [ ] Enhance `CollectionSummary` with extra fields
- [ ] Add `GetCollectionPageAsync` to `ICollectionIndexService`
- [ ] Add secondary indexes (by_library, by_type) to `RedisCollectionIndexService`
- [ ] Add filtering methods to `RedisCollectionIndexService`
- [ ] Register service in DI (API, Worker, Scheduler)
- [ ] Add startup index validation & rebuild

### Phase 2: Backend Migration
- [ ] Migrate `GetCollectionsAsync` to use Redis
- [ ] Migrate `GetCollectionNavigationAsync` to use Redis
- [ ] Migrate `GetCollectionSiblingsAsync` to use Redis
- [ ] Migrate `GetTotalCollectionsCountAsync` to use Redis
- [ ] Migrate `GetCollectionsByLibraryAsync` to use Redis
- [ ] Migrate `GetCollectionsByTypeAsync` to use Redis
- [ ] Migrate `GetRecentCollectionsAsync` to use Redis
- [ ] Add index updates to Create/Update/Delete methods
- [ ] Add index rebuild to BulkService

### Phase 3: API Updates
- [ ] Add `sortBy` and `sortDirection` params to `GET /collections`
- [ ] Add `sortBy` and `sortDirection` params to `GET /collections/library/{id}`
- [ ] Update DTOs to match `CollectionSummary`
- [ ] Add `POST /collections/rebuild-index` admin endpoint
- [ ] Add `GET /collections/index-stats` admin endpoint

### Phase 4: Frontend Updates
- [ ] Add sort controls to Collections page
- [ ] Update API calls to include sortBy/sortDirection
- [ ] Display accurate position in navigation
- [ ] Add sort persistence to sessionStorage
- [ ] Update loading states for faster response

### Phase 5: Testing & Monitoring
- [ ] Test with 24k collections
- [ ] Verify position accuracy
- [ ] Verify sort consistency across all pages
- [ ] Monitor Redis memory usage
- [ ] Add metrics/logging for Redis operations
- [ ] Add fallback logic if Redis is down

---

## 🎯 Success Criteria

✅ All collection queries use Redis index for sorting
✅ Position numbers are accurate across all pages
✅ Navigation prev/next work correctly at page boundaries
✅ Sort order is consistent everywhere
✅ Response times under 50ms for all operations
✅ Index stays in sync with MongoDB
✅ Graceful degradation if Redis is unavailable

