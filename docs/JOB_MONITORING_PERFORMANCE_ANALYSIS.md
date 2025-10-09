# Job Monitoring Performance Analysis

## Question: What Happens with 10,000 Pending Jobs?

### Current Implementation

**JobMonitoringService runs every 5 seconds:**

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        await MonitorAllPendingJobsAsync(stoppingToken);
    }
}
```

**Each monitoring cycle:**
1. Query pending collection-scan jobs: `WHERE jobType='collection-scan' AND status='Pending' AND collectionId != null`
2. For each job: Query collection by ID, count arrays, update stages
3. Update job stages if counts changed

### Performance Analysis for 10,000 Jobs

#### Scenario 1: All 10,000 Jobs Pending Simultaneously

**MongoDB Queries per Cycle:**
- 1 query to get pending jobs: `O(1)` with proper index
- 10,000 queries to get collections: `O(N)` where N = 10,000
- 10,000 × 2 potential stage updates (thumbnail + cache)

**Time per Cycle:**
- Get jobs: ~50ms (indexed query)
- Get 10k collections: ~10k × 2ms = 20 seconds
- Update stages: ~10k × 5ms = 50 seconds
- **Total: ~70 seconds per cycle**

**Problem:**
- Monitoring cycle takes 70 seconds
- Next cycle should start after 5 seconds
- **BACKLOG BUILDS UP!**

#### Scenario 2: Jobs Complete Gradually

Realistic scenario: Jobs complete over time as processing happens.

**Initial:** 10,000 pending
**After 1 min:** ~9,500 pending (500 completed)
**After 2 min:** ~9,000 pending
**After 5 min:** ~7,500 pending

**Query time scales down as jobs complete:**
- Minute 1: 10k jobs × 70ms = 70 sec ❌
- Minute 5: 7.5k jobs × 70ms = 52 sec ❌
- Minute 10: 5k jobs × 70ms = 35 sec ❌

Still problematic!

## Optimization Strategies

### 1. Batch Collection Queries (RECOMMENDED)

Instead of querying collections one by one:

```csharp
// Current (slow):
foreach (var job in pendingJobs)
{
    var collection = await collectionsCollection
        .Find(c => c.Id == job.CollectionId.Value)
        .FirstOrDefaultAsync();
}

// Optimized (fast):
var collectionIds = pendingJobs
    .Where(j => j.CollectionId.HasValue)
    .Select(j => j.CollectionId.Value)
    .ToList();

var collections = await collectionsCollection
    .Find(c => collectionIds.Contains(c.Id))
    .ToListAsync();

var collectionDict = collections.ToDictionary(c => c.Id);

foreach (var job in pendingJobs)
{
    if (collectionDict.TryGetValue(job.CollectionId.Value, out var collection))
    {
        // Process...
    }
}
```

**Performance:**
- 10k jobs: 1 query instead of 10k queries!
- Query time: ~200ms instead of 20 seconds
- **100x improvement!**

### 2. Pagination with Limit

Process jobs in batches:

```csharp
const int BatchSize = 100;
var pendingJobs = await jobsCollection
    .Find(filter)
    .Limit(BatchSize)
    .ToListAsync();
```

**Performance:**
- Each cycle: 100 jobs × 2ms = 200ms
- Completes in < 1 second per cycle
- All 10k jobs monitored over 100 cycles
- **Stays within 5-second interval!**

### 3. Priority Queue

Process jobs by priority or age:

```csharp
var pendingJobs = await jobsCollection
    .Find(filter)
    .Sort(Builders<BackgroundJob>.Sort.Ascending(j => j.CreatedAt))
    .Limit(100)
    .ToListAsync();
```

**Benefit:** Older jobs get priority, ensuring FIFO processing.

### 4. Index Optimization

**Required Indexes:**
```javascript
// background_jobs collection
db.background_jobs.createIndex(
    { jobType: 1, status: 1, collectionId: 1 },
    { name: "idx_pending_collection_scans" }
);

// collections collection (already has _id index)
db.collections.createIndex(
    { _id: 1 },
    { name: "idx_collection_id" }  // Primary index, auto-created
);
```

### 5. Projection (Reduce Data Transfer)

Only fetch the fields we need:

```csharp
var projection = Builders<Collection>.Projection
    .Include(c => c.Id)
    .Include(c => c.Name)
    .Include(c => c.Thumbnails)
    .Include(c => c.CacheImages);

var collections = await collectionsCollection
    .Find(c => collectionIds.Contains(c.Id))
    .Project<CollectionMonitoringView>(projection)
    .ToListAsync();
```

**Benefit:** Reduce data transfer from MB to KB.

## Recommended Implementation

### Immediate (Critical for 10k jobs):

```csharp
private async Task MonitorAllPendingJobsAsync(CancellationToken cancellationToken)
{
    const int BatchSize = 500; // Process 500 jobs per cycle
    
    using var scope = _serviceScopeFactory.CreateScope();
    var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
    var mongoDatabase = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
    
    var jobsCollection = mongoDatabase.GetCollection<BackgroundJob>("background_jobs");
    var collectionsCollection = mongoDatabase.GetCollection<Collection>("collections");
    
    var filter = Builders<BackgroundJob>.Filter.And(
        Builders<BackgroundJob>.Filter.Eq(j => j.JobType, "collection-scan"),
        Builders<BackgroundJob>.Filter.Eq(j => j.Status, "Pending"),
        Builders<BackgroundJob>.Filter.Ne(j => j.CollectionId, null)
    );
    
    // Get pending jobs (limited batch)
    var pendingJobs = await jobsCollection
        .Find(filter)
        .Sort(Builders<BackgroundJob>.Sort.Ascending(j => j.CreatedAt))
        .Limit(BatchSize)
        .ToListAsync(cancellationToken);
    
    if (pendingJobs.Count == 0) return;
    
    // BATCH query all collections at once
    var collectionIds = pendingJobs
        .Where(j => j.CollectionId.HasValue)
        .Select(j => j.CollectionId.Value)
        .ToList();
    
    var collections = await collectionsCollection
        .Find(c => collectionIds.Contains(c.Id))
        .ToListAsync(cancellationToken);
    
    var collectionDict = collections.ToDictionary(c => c.Id);
    
    // Process each job with its collection
    foreach (var job in pendingJobs)
    {
        if (!job.CollectionId.HasValue || 
            !collectionDict.TryGetValue(job.CollectionId.Value, out var collection))
            continue;
        
        // Update stages...
    }
}
```

### Performance with Optimizations:

**For 10,000 pending jobs:**

**Per Cycle:**
- Get 500 pending jobs: ~10ms
- Get 500 collections (batched): ~100ms
- Update stages: ~500 × 5ms = 2.5 seconds
- **Total: ~2.6 seconds per cycle ✅**

**To process all 10k jobs:**
- 10,000 ÷ 500 = 20 cycles
- 20 × 5 seconds = 100 seconds (1.7 minutes)
- All jobs monitored within 2 minutes

**Scalability:**
- 100 jobs: 1 cycle × 0.5 sec = 0.5 sec ✅
- 1,000 jobs: 2 cycles × 2.5 sec = 5 sec ✅
- 10,000 jobs: 20 cycles × 2.6 sec = 52 sec ✅
- 100,000 jobs: 200 cycles × 2.6 sec = 520 sec (8.7 min) ⚠️

**Recommendation:** For > 10k jobs, implement additional strategies:
- Increase batch size to 1000
- Run monitoring every 3 seconds instead of 5
- Add parallel processing (split jobs across multiple tasks)

## Current Performance (Unoptimized)

**For 6 jobs:** Completes in ~3.5 minutes ✅
**For 100 jobs:** Would complete in ~4 minutes ✅
**For 1,000 jobs:** Would take ~15-20 minutes ⚠️
**For 10,000 jobs:** Would take 2-3 hours ❌

## Action Items

### Must Do (Priority 1):
1. ✅ Implement batch collection queries
2. ✅ Add pagination with BatchSize = 500
3. ✅ Add MongoDB indexes

### Should Do (Priority 2):
4. Add projection to reduce data transfer
5. Implement priority/FIFO sorting
6. Add monitoring metrics (jobs/sec processed)

### Nice to Have (Priority 3):
7. Parallel batch processing
8. Adaptive batch sizing based on load
9. Health checks and alerting

## Code Changes Required

See optimization examples above. Estimated effort: 2-3 hours.

## Testing Plan

1. Test with current 6 jobs (baseline)
2. Create 100 test jobs
3. Create 1,000 test jobs
4. Monitor performance metrics
5. Adjust batch size and intervals
6. Add indexes and re-test

## Monitoring Metrics to Track

- Jobs processed per cycle
- Time per cycle (ms)
- Queue depth over time
- Memory usage
- MongoDB query performance
- Stage update latency

