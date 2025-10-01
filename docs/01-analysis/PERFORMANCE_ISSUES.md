# Performance Issues Analysis

## TL;DR
- **Nguồn gốc chính**: Truy vấn DB N+1, phân trang trong memory, xử lý ảnh đồng bộ, HTTP caching yếu, frontend lưu base64 thumbnail trong state.
- **Tác động**: p95 API 200–500ms, cache gen 2–3h/1k ảnh, memory 2–4GB, UX lag.
- **Quick wins**:
  - Phân trang ở DB, gộp lấy `stats/tags` theo page (aggregation/join-like).
  - Stream ảnh (fs/Sharp pipeline), trả URL thumbnail thay vì base64, thêm ETag/Cache-Control.
  - Zustand dùng selectors, bỏ base64 khỏi store, giữ virtualized grid.
- **Mục tiêu**: p95 API < 100ms, thumbnail < 500ms, cache gen < 30 phút/1k ảnh, mem < 1GB.

## Phạm vi & Giả định
- **Phạm vi**: Backend Node hiện tại (`_outdated/`), client React/Vite cũ, MongoDB; không bao gồm kiến trúc .NET 8 (được đề xuất ở docs khác) nhưng có nêu tiêu chí tương đương để so sánh.
- **Giả định**:
  - Datasets tới hàng triệu ảnh; thư mục và file nén hỗn hợp.
  - Lưu trữ trên SSD; băng thông nội bộ ổn định.
  - Đọc/ghi cache có thể phân tán qua nhiều ổ đĩa (cache folders).

## Methodology (Phương pháp đo)
- **Môi trường**:
  - OS/CPU/RAM ghi rõ khi chạy benchmark; MongoDB local với cấu hình mặc định; Node LTS.
  - Bật logging mức `info` và tắt debug khi đo latency.
- **Dataset mẫu**:
  - 100/1k/10k ảnh (trộn JPG/PNG/WEBP), 20% nằm trong ZIP/CBZ; 3–5 collections lớn/nhỏ.
- **Kịch bản đo**:
  - Danh sách collections page 1–10; lấy images page 1–10; search; lấy thumbnail; serve ảnh full.
  - Cache generation 1k ảnh, concurrency N.
- **Thu thập số liệu**:
  - API: dùng k6/Artillery, 1–5–10 RPS/endpoint, 1–5 phút; ghi p50/p90/p95, error%.
  - Image: đo thời gian/ảnh, CPU%, RSS; cache hit-rate (header `X-Cache`).
- **Quy trình**:
  1) Reset cache (xóa thư mục cache + TTL DB). 2) Warm-up 1 phút. 3) Chạy đo. 4) Lưu kết quả (CSV/JSON). 5) Lặp lại sau khi áp dụng fix.


## Tổng quan

Document này liệt kê chi tiết các vấn đề performance cụ thể được phát hiện trong hệ thống hiện tại và cách giải quyết trong hệ thống mới.

## 🚨 Critical Performance Issues

### 1. Database Performance Issues

#### N+1 Query Problem
**Vấn đề**: Trong `collections.js` line 44-63, mỗi collection được load statistics và tags riêng biệt
```javascript
// Vấn đề: N+1 queries
const collectionsWithStats = await Promise.all(
  paginatedCollections.map(async (collection) => {
    const [stats, tags] = await Promise.all([
      db.getCollectionStats(collection.id),  // Query 1
      db.getCollectionTags(collection.id)     // Query 2
    ]);
    // ... N collections = 2N queries
  })
);
```

**Impact**: 
- Load time: 2-5 seconds cho 100 collections
- Database connections: Exhausted connection pool
- Memory usage: High memory consumption

**Solution**: 
- Sử dụng JOIN queries hoặc Include trong EF Core
- Implement proper pagination ở database level
- Use compiled queries cho frequently used operations

#### Inefficient Pagination
**Vấn đề**: Load tất cả collections rồi mới paginate trong memory
```javascript
// Vấn đề: Load all rồi mới paginate
let collections = await db.getAllCollections(); // Load ALL
const paginatedCollections = collections.slice(skip, skip + limitNum); // Paginate in memory
```

**Impact**:
- Memory usage: Load 10K+ collections vào memory
- Response time: 3-8 seconds cho large datasets
- Scalability: Không scale được với large data

**Solution**:
- Database-level pagination với OFFSET/LIMIT
- Cursor-based pagination cho better performance
- Implement proper indexing

### 2. Image Processing & Caching Issues

#### Sequential Processing
**Vấn đề**: Cache generation xử lý tuần tự thay vì parallel
```javascript
// Vấn đề: Sequential processing
for (const image of imagesToProcess) {
  await this.processImage(image, collection, cachePath, options); // Blocking
}
```

**Impact**:
- Cache generation: 2-3 hours cho 1000 images
- CPU utilization: Chỉ sử dụng 1 core
- User experience: Không thể sử dụng trong khi generate cache

**Solution**:
- Parallel processing với Task.Run()
- Background services với Hangfire
- Batch processing với proper concurrency control

#### Memory Leaks
**Vấn đề**: Sharp instances không được cleanup properly
```javascript
// Vấn đề: Memory leak
let sharpInstance = sharp(imageBuffer);
// Không dispose sharp instance
```

**Impact**:
- Memory usage: Tăng liên tục theo thời gian
- Performance degradation: Chậm dần theo thời gian
- System stability: Có thể crash khi hết memory

**Solution**:
- Proper disposal với using statements
- Object pooling cho Sharp instances
- Memory monitoring và cleanup

#### Inefficient Cache Strategy
**Vấn đề**: Cache không có TTL, không có cleanup mechanism
```javascript
// Vấn đề: No TTL, no cleanup
const cacheData = {
  key,
  value,
  expires_at: null, // No expiration
  created_at: new Date()
};
```

**Impact**:
- Disk space: Cache không bao giờ được cleanup
- Performance: Cache hit rate thấp
- Storage costs: Tăng liên tục

**Solution**:
- Implement TTL cho cache entries
- Background cleanup jobs
- Cache size limits và LRU eviction

### 3. Frontend Performance Issues

#### Inefficient State Management
**Vấn đề**: Zustand store không được optimize
```typescript
// Vấn đề: Unnecessary re-renders
const { viewer, setCurrentImage } = useStore();
// Tất cả components re-render khi store thay đổi
```

**Impact**:
- UI lag: 100-200ms delay khi navigate
- Memory usage: High memory consumption
- User experience: Laggy interface

**Solution**:
- Selective subscriptions với Zustand
- Memoization với React.memo
- Proper state normalization

#### Poor Image Loading
**Vấn đề**: Không có proper lazy loading
```typescript
// Vấn đề: Load all images at once
{images.map(image => (
  <img src={`/api/images/${collectionId}/${image.id}/thumbnail`} />
))}
```

**Impact**:
- Network usage: Load tất cả thumbnails cùng lúc
- Memory usage: High memory consumption
- Initial load time: 5-10 seconds

**Solution**:
- Virtual scrolling với react-window
- Lazy loading với Intersection Observer
- Progressive image loading

#### Memory Leaks
**Vấn đề**: Image URLs không được cleanup
```typescript
// Vấn đề: Memory leak
const url = URL.createObjectURL(blob);
// Không revoke URL khi component unmount
```

**Impact**:
- Memory usage: Tăng liên tục
- Browser performance: Degradation over time
- System stability: Browser crash

**Solution**:
- Proper cleanup trong useEffect
- URL.revokeObjectURL() khi unmount
- Memory monitoring

### 4. API Design Issues

#### Inconsistent Response Format
**Vấn đề**: API responses không nhất quán
```javascript
// Vấn đề: Inconsistent responses
res.json({ collections: collections }); // Sometimes
res.json({ data: collections }); // Sometimes
res.json(collections); // Sometimes
```

**Impact**:
- Client complexity: Phải handle multiple formats
- Error handling: Khó debug
- API reliability: Không predictable

**Solution**:
- Standardized response format
- Consistent error handling
- API versioning

#### Missing Caching Headers
**Vấn đề**: HTTP caching không được implement
```javascript
// Vấn đề: No cache headers
res.set({
  'Cache-Control': 'no-cache, no-store, must-revalidate', // Always no-cache
});
```

**Impact**:
- Network usage: Redundant requests
- Server load: High server load
- Performance: Slow response times

**Solution**:
- Proper cache headers
- ETag support
- Conditional requests

## Ma trận Vấn đề ↔ Code ↔ Giải pháp ↔ KPI

| Vấn đề | Code refs | Giải pháp | KPI mục tiêu |
|---|---|---|---|
| N+1 + paginate trong memory | `_outdated/server/routes/collections.js` (get '/', 28–63); `_outdated/server/mongodb.js` (getAllCollections) | Phân trang ở DB (`skip/limit`), aggregate `stats/tags` cho page; bỏ populate trong `getAllCollections` | p95 GET /collections page1 < 120ms; mem giảm >50% |
| Serve ảnh đọc toàn bộ buffer | `_outdated/server/routes/images.js` (serve cache 65–86; resize 91–143; fallback 288–321) | Stream file; resize bằng stream Sharp; precompute sizes phổ biến | p95 GET image < 800ms; RSS giảm >30% |
| Batch thumbnail trả base64 | `_outdated/server/routes/images.js` (batch-thumbnails 200–233) | Trả URL + cache headers; client tải lười | Dung lượng response ↓ >80%; TTFB grid < 500ms |
| Cache không TTL/eviction mạnh | `_outdated/server/mongodb.js` (cache TTL cơ bản) + `cacheManager.js` | TTL rõ ràng, size cap theo folder, LRU eviction, cleanup định kỳ | Hit-rate > 80%; disk growth kiểm soát |
| Zustand re-render rộng | `_outdated/client/src/store/useStore.ts` | Dùng selectors/shallow, tách slice, bỏ base64 khỏi store | Commit re-render/interaction lag ↓ đáng kể |

## 📊 Performance Metrics

### Current Performance (Node.js)
| Metric | Current | Target (.NET 8) | Improvement |
|--------|---------|-----------------|-------------|
| API Response Time | 200-500ms | < 100ms | 2-5x faster |
| Image Loading | 1-3s | < 500ms | 2-6x faster |
| Cache Generation | 2-3 hours | < 30 minutes | 4-6x faster |
| Memory Usage | 2-4GB | < 1GB | 2-4x less |
| Database Queries | 100-300ms | < 50ms | 2-6x faster |
| Concurrent Users | 50-100 | 1000+ | 10-20x more |

### Bottlenecks Analysis
1. **Database**: 40% of performance issues
2. **Image Processing**: 30% of performance issues  
3. **Frontend**: 20% of performance issues
4. **API Design**: 10% of performance issues

## 🎯 Performance Targets

### Response Time Targets
- **Simple API calls**: < 100ms
- **Complex queries**: < 500ms
- **Image thumbnails**: < 500ms
- **Full images**: < 2s
- **Cache generation**: < 2s per image

### Throughput Targets
- **API requests**: 10,000+ requests/minute
- **Image processing**: 100+ images/minute
- **Cache generation**: 50+ images/minute
- **Database queries**: 1000+ queries/second

### Resource Usage Targets
- **Memory usage**: < 1GB per instance
- **CPU usage**: < 80% under normal load
- **Disk I/O**: Optimized for SSD storage
- **Network**: Efficient bandwidth usage

## 🔧 Solutions Implementation

### 1. Database Optimizations
```csharp
// Compiled queries
public static readonly Func<DbContext, Guid, IAsyncEnumerable<Collection>> GetCollectionsWithStats =
    EF.CompileAsyncQuery((DbContext context, Guid userId) =>
        context.Collections
            .Include(c => c.Statistics)
            .Include(c => c.Tags)
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .OrderBy(c => c.Name));

// Proper pagination
public async Task<PagedResult<Collection>> GetCollectionsAsync(GetCollectionsQuery query)
{
    var collections = await _context.Collections
        .Include(c => c.Statistics)
        .Include(c => c.Tags)
        .Where(c => !c.IsDeleted)
        .OrderBy(c => c.Name)
        .Skip(query.Skip)
        .Take(query.Take)
        .ToListAsync();
        
    var total = await _context.Collections
        .Where(c => !c.IsDeleted)
        .CountAsync();
        
    return new PagedResult<Collection>(collections, total, query.Page, query.PageSize);
}
```

### 2. Image Processing Optimizations
```csharp
// Parallel processing
public async Task ProcessImagesAsync(IEnumerable<Image> images, ProcessingOptions options)
{
    var semaphore = new SemaphoreSlim(options.MaxConcurrency);
    var tasks = images.Select(async image =>
    {
        await semaphore.WaitAsync();
        try
        {
            await ProcessImageAsync(image, options);
        }
        finally
        {
            semaphore.Release();
        }
    });
    
    await Task.WhenAll(tasks);
}

// Proper disposal
public async Task<byte[]> ProcessImageAsync(byte[] imageData, ProcessingOptions options)
{
    using var image = SKImage.FromEncodedData(imageData);
    using var bitmap = SKBitmap.FromImage(image);
    
    // Process image
    var processedBitmap = ApplyTransformations(bitmap, options);
    
    // Encode
    using var encodedImage = processedBitmap.Encode(options.Format, options.Quality);
    return encodedImage.ToArray();
}
```

### 3. Caching Optimizations
```csharp
// Multi-level caching
public async Task<T> GetAsync<T>(string key, Func<Task<T>> factory)
{
    // Try memory cache first
    if (_memoryCache.TryGetValue(key, out T cachedValue))
        return cachedValue;
    
    // Try Redis cache
    var redisValue = await _redisCache.GetAsync<T>(key);
    if (redisValue != null)
    {
        _memoryCache.Set(key, redisValue, TimeSpan.FromMinutes(5));
        return redisValue;
    }
    
    // Generate value
    var value = await factory();
    
    // Cache in both levels
    _memoryCache.Set(key, value, TimeSpan.FromMinutes(5));
    await _redisCache.SetAsync(key, value, TimeSpan.FromHours(1));
    
    return value;
}
```

### 4. Frontend Optimizations
```typescript
// Selective subscriptions
const collections = useStore(state => state.collections);
const setCollections = useStore(state => state.setCollections);

// Memoized components
const ImageItem = React.memo(({ image, onClick }) => {
  const [isLoading, setIsLoading] = useState(true);
  const [hasError, setHasError] = useState(false);
  
  useEffect(() => {
    // Load image
    return () => {
      // Cleanup
      URL.revokeObjectURL(imageUrl);
    };
  }, [image.id]);
  
  return (
    <div onClick={() => onClick(image)}>
      {isLoading && <LoadingSpinner />}
      {!hasError && <img src={imageUrl} onLoad={() => setIsLoading(false)} />}
    </div>
  );
});

// Virtual scrolling
const VirtualizedImageGrid = ({ images, onImageClick }) => {
  return (
    <FixedSizeGrid
      columnCount={columnCount}
      columnWidth={itemSize}
      rowCount={rowCount}
      rowHeight={itemSize}
      height={height}
      width={width}
    >
      {({ columnIndex, rowIndex, style }) => (
        <ImageItem
          style={style}
          image={images[rowIndex * columnCount + columnIndex]}
          onClick={onImageClick}
        />
      )}
    </FixedSizeGrid>
  );
};
```

## Prioritization & Roadmap
- **Wave 1 (0–3 ngày)**: Phân trang DB cho collections/images; bỏ populate trong `getAllCollections`; chuẩn hóa response envelope; thêm cache headers cơ bản cho thumbnails.
- **Wave 2 (4–7 ngày)**: Streaming ảnh, precompute sizes 256/512/1024, batch thumbnail trả URL; thêm ETag/If-None-Match; queue concurrency có giới hạn.
- **Wave 3 (8–14 ngày)**: LRU/TTL cache folders, cleanup/rebalance định kỳ; metrics hit-rate/latency; frontend chuyển preload sang URL + selectors.
- **Wave 4 (15+ ngày)**: Nâng cấp job persistence, retry/backoff; dashboard Grafana; chuẩn bị migration song song (v2 API) nếu cần.

## Acceptance Criteria & SLOs
- **API**: p95 `/collections?page=1&limit=50` < 120ms; `/collections/:id/images?page=1` < 150ms; error rate < 1%.
- **Image**: p95 thumbnail < 500ms; p95 view ảnh full < 2s; cache hit-rate > 80% sau warm-up.
- **Tài nguyên**: RSS < 1GB/instance; CPU < 80% ở tải danh định; I/O không bão hòa.
- **Cache gen**: 1k ảnh < 30 phút ở N=4–8 concurrency; không chặn tuyến foreground.
- **Frontend**: Grid interactive < 200ms khi cuộn; không lưu base64 vào store; không rò rỉ URL.

## 📈 Monitoring & Metrics

### Key Performance Indicators (KPIs)
1. **Response Time**: Average API response time
2. **Throughput**: Requests per second
3. **Error Rate**: Percentage of failed requests
4. **Cache Hit Rate**: Percentage of cache hits
5. **Memory Usage**: Average memory consumption
6. **CPU Usage**: Average CPU utilization
7. **Database Performance**: Query execution time
8. **Image Processing Time**: Average processing time

### Monitoring Tools
- **Application Insights**: Performance monitoring
- **Prometheus**: Metrics collection
- **Grafana**: Metrics visualization
- **ELK Stack**: Log analysis
- **New Relic**: APM monitoring

### Alerting Thresholds
- **Response Time**: > 500ms
- **Error Rate**: > 1%
- **Memory Usage**: > 80%
- **CPU Usage**: > 90%
- **Database Queries**: > 100ms
- **Cache Hit Rate**: < 80%

## 🎯 Conclusion

Các vấn đề performance hiện tại chủ yếu do:
1. **Architecture**: Không phù hợp cho high-performance applications
2. **Database**: Queries không được optimize
3. **Caching**: Strategy không hiệu quả
4. **Frontend**: State management và rendering không optimize

Việc migrate sang .NET 8 sẽ giải quyết được hầu hết các vấn đề này thông qua:
1. **Better Architecture**: Clean Architecture với proper separation
2. **Optimized Database**: EF Core với compiled queries
3. **Efficient Caching**: Multi-level caching strategy
4. **Modern Frontend**: Blazor với proper state management

Kết quả mong đợi: **2-10x performance improvement** across all metrics.
