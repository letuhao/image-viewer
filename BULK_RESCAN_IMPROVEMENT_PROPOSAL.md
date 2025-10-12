# Bulk Rescan Improvement Proposal

## Current Problem

User has 25,000 collections:
- 2,500 collections (10%) are **99% complete** (scanned, most thumbnails/cache generated)
- After RabbitMQ accident, some collections have incomplete thumbnail/cache generation
- User wants to **resume/continue** incomplete collections, NOT re-scan them

### Current Bulk Logic (PROBLEM)

With `OverwriteExisting=false`:
- ✅ Keeps existing image arrays
- ✅ Skips duplicate images
- ✅ Skips existing thumbnail/cache files on disk
- ❌ **BUT**: Always queues NEW scan job for ALL collections
- ❌ Result: Re-scans 2,500 already-scanned collections (wasted processing)

### What User Actually Needs

**3 Different Scenarios**:

1. **Fully Scanned + 99% Complete**: 
   - Images: ✅ All discovered
   - Thumbnails: ✅ 99% generated
   - Cache: ✅ 99% generated
   - **Action**: **SKIP** (no processing needed)

2. **Fully Scanned + Incomplete Thumbnail/Cache**:
   - Images: ✅ All discovered
   - Thumbnails: ⚠️ Some missing
   - Cache: ⚠️ Some missing
   - **Action**: **RESUME** (generate only missing thumbnail/cache, no re-scan)

3. **Never Scanned or Failed Scan**:
   - Images: ❌ Empty or incomplete
   - **Action**: **SCAN** (discover images + generate thumbnail/cache)

---

## Proposed Solution: 3-Mode Bulk System

### **MODE 1: Skip Existing** (`OverwriteExisting=false`, `ResumeIncomplete=false`) ✅
- **When to use**: Collections already scanned, don't want to re-process
- **Behavior**:
  - Check if collection has images (Images.Count > 0)
  - If yes: **SKIP** (return "Already scanned")
  - If no: **SCAN** (queue scan job)
- **Result**: 2,500 scanned collections SKIPPED, only truly new/failed ones scanned

### **MODE 2: Resume Incomplete** (`OverwriteExisting=false`, `ResumeIncomplete=true`) 🔄
- **When to use**: Collections scanned but thumbnail/cache incomplete (YOUR SCENARIO!)
- **Behavior**:
  - Check if collection has images (Images.Count > 0)
  - If no: **SCAN** (queue scan job)
  - If yes: 
    - Count missing thumbnails: `Images.Count - Thumbnails.Count`
    - Count missing cache: `Images.Count - CacheImages.Count`
    - If missing > 0: **RESUME** (queue ONLY thumbnail/cache jobs for missing images)
    - If complete: **SKIP**
- **Result**: 2,500 collections resume only missing thumbnail/cache, no re-scan!

### **MODE 3: Force Rescan** (`OverwriteExisting=true`) 🔥
- **When to use**: Want to start from scratch
- **Behavior**:
  - Clear all image arrays
  - Queue scan job with ForceRescan=true
- **Result**: Complete rebuild

---

## Implementation Plan

### 1. Add `ResumeIncomplete` flag to `BulkAddCollectionsRequest`

```csharp
public class BulkAddCollectionsRequest
{
    // Existing properties...
    public bool OverwriteExisting { get; set; }
    
    // NEW: Resume incomplete collections
    public bool ResumeIncomplete { get; set; } = false;
}
```

### 2. Update `BulkService.ProcessPotentialCollection` logic

```csharp
if (existingCollection != null)
{
    // Check if collection has images
    var hasImages = existingCollection.Images?.Count > 0;
    
    if (request.OverwriteExisting)
    {
        // MODE 3: Force rescan
        // Clear arrays + queue full scan
    }
    else if (!hasImages)
    {
        // MODE 1 & 2: Never scanned
        // Queue scan job
    }
    else if (request.ResumeIncomplete)
    {
        // MODE 2: Resume incomplete
        var missingThumbnails = existingCollection.Images.Count - (existingCollection.Thumbnails?.Count ?? 0);
        var missingCache = existingCollection.Images.Count - (existingCollection.CacheImages?.Count ?? 0);
        
        if (missingThumbnails > 0 || missingCache > 0)
        {
            // Queue ONLY thumbnail/cache jobs for missing images
            await QueueResumeThumbnailCacheJobsAsync(existingCollection);
            return new BulkCollectionResult
            {
                Status = "Resumed",
                Message = $"Resumed: {missingThumbnails} thumbnails, {missingCache} cache"
            };
        }
        else
        {
            // Collection is complete
            return new BulkCollectionResult
            {
                Status = "Skipped",
                Message = "Collection already complete"
            };
        }
    }
    else
    {
        // MODE 1: Skip existing
        return new BulkCollectionResult
        {
            Status = "Skipped",
            Message = "Collection already scanned"
        };
    }
}
```

### 3. Implement `QueueResumeThumbnailCacheJobsAsync`

```csharp
private async Task QueueResumeThumbnailCacheJobsAsync(Collection collection)
{
    // Get images that don't have thumbnails
    var imagesNeedingThumbnails = collection.Images
        .Where(img => !collection.Thumbnails.Any(t => t.ImageId == img.Id))
        .ToList();
    
    // Get images that don't have cache
    var imagesNeedingCache = collection.Images
        .Where(img => !collection.CacheImages.Any(c => c.ImageId == img.Id))
        .ToList();
    
    // Queue thumbnail generation jobs
    foreach (var image in imagesNeedingThumbnails)
    {
        var thumbnailMessage = new ThumbnailGenerationMessage
        {
            ImageId = image.Id,
            CollectionId = collection.Id.ToString(),
            ImagePath = image.FullPath,
            // ... other properties
        };
        await _messageQueueService.PublishAsync(thumbnailMessage);
    }
    
    // Queue cache generation jobs
    foreach (var image in imagesNeedingCache)
    {
        var cacheMessage = new CacheGenerationMessage
        {
            ImageId = image.Id,
            CollectionId = collection.Id.ToString(),
            ImagePath = image.FullPath,
            // ... other properties
        };
        await _messageQueueService.PublishAsync(cacheMessage);
    }
}
```

### 4. Update Frontend (Library Screen)

Add a new checkbox:
- ☐ Overwrite Existing
- ☑ Resume Incomplete (NEW!)

---

## Benefits

✅ **MODE 1** (Skip): Fast bulk add, skips 2,500 scanned collections
✅ **MODE 2** (Resume): Resumes 2,500 incomplete collections without re-scan
✅ **MODE 3** (Force): Clean slate when needed

**For Your Scenario**:
- Select "Resume Incomplete"
- Result: 
  - 2,500 collections analyzed
  - Only missing thumbnail/cache queued
  - **NO re-scanning!**
  - **NO wasted processing!**
  - Resumes from 99% to 100%! 🎉

---

## Comparison

| Mode | OverwriteExisting | ResumeIncomplete | Scanned Collections | Incomplete Collections |
|------|-------------------|------------------|---------------------|------------------------|
| **Skip** | false | false | **SKIP** | **SKIP** |
| **Resume** (YOUR NEED!) | false | true | **SKIP** | **RESUME** (no re-scan) |
| **Force** | true | (ignored) | **RESCAN** | **RESCAN** |

---

## Current Workaround (Without Implementation)

**If you DON'T want to implement this now**, you can:

1. **Manually query MongoDB** for incomplete collections:
   ```javascript
   db.collections.find({
       $expr: {
           $or: [
               { $lt: [{ $size: { $ifNull: ["$thumbnails", []] } }, { $size: "$images" }] },
               { $lt: [{ $size: { $ifNull: ["$cacheImages", []] } }, { $size: "$images" }] }
           ]
       }
   })
   ```

2. **Write a script** to queue thumbnail/cache jobs for incomplete collections only

3. **OR**: Accept that bulk scan will re-queue jobs, but workers will skip existing files (just wasted queue processing, not file regeneration)

---

## Recommendation

**OPTION A**: Implement MODE 2 (Resume) - Best for your scenario! ✅
- Clean solution
- Fast processing
- No wasted resources

**OPTION B**: Use current logic with `OverwriteExisting=false` - Works but not optimal ⚠️
- Re-queues all jobs
- Workers skip existing files
- Wasted queue processing, but files are safe
- **Your 99% complete collections won't lose progress**

**OPTION C**: Manual script - Quick but not reusable 🔧
- One-time solution
- No UI integration
- Good for emergency recovery

---

## Your Choice?

What do you prefer?
1. **Implement MODE 2 (Resume)** - I can do this now (15-20 minutes)
2. **Use current logic** - Works, just not optimal (your files are safe)
3. **Manual script** - I can write a PowerShell/Node.js script to resume incomplete collections

Let me know! 🚀

