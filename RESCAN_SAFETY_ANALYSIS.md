# Rescan Safety Analysis

## Your Scenario
You want to:
1. **Purge** all RabbitMQ message queues
2. **Purge** all background jobs from MongoDB
3. **Keep** all collections in MongoDB
4. **Manually scan** from Library screen (no overwrite mode)
5. **Rescan** existing collections and **re-generate** cache/thumbnails
6. **NOT overwrite** existing files on disk

---

## Analysis of Current Logic

### 1. Can Bulk Logic Skip Existing Collections?

**✅ FIXED** - Bulk logic now **ALWAYS processes existing collections**:

**Code Location**: `src/ImageViewer.Application/Services/BulkService.cs:111-165`

```csharp
if (existingCollection != null)
{
    // Always update metadata and queue scan for existing collections
    // The difference is: OverwriteExisting=true clears image arrays, false keeps them
    
    // Update existing collection metadata
    var updateRequest = new UpdateCollectionRequest { ... };
    collection = await _collectionService.UpdateCollectionAsync(existingCollection.Id, updateRequest);
    
    // Apply collection settings with forceRescan flag
    collection = await _collectionService.UpdateSettingsAsync(
        collection.Id, 
        settingsRequest, 
        triggerScan: true, 
        forceRescan: request.OverwriteExisting); // OverwriteExisting controls ForceRescan
    
    if (request.OverwriteExisting)
    {
        // Will CLEAR image arrays and rescan from scratch
    }
    else
    {
        // Will KEEP existing images and discover new ones
    }
}
```

**NEW Behavior**:
- `OverwriteExisting = false` (default): 
  - ✅ **Processes** existing collections (NOT skipped)
  - ✅ **Keeps** existing image/thumbnail/cache metadata
  - ✅ **Queues** new scan job with `ForceRescan=false`
  - ✅ **Discovers** new images (adds them)
  - ✅ **Skips** duplicate images
  - ✅ **Skips** existing thumbnail/cache files on disk
  - ✅ **Perfect for your scenario!** ✨

- `OverwriteExisting = true`:
  - ✅ **Processes** existing collections
  - 🔥 **CLEARS** all image arrays (Images, Thumbnails, CacheImages)
  - ✅ **Queues** new scan job with `ForceRescan=true`
  - ✅ **Rescans** from scratch
  - ✅ **Regenerates** all thumbnails and cache
  - ⚠️ **Use only when you want a clean slate**

**✅ SOLUTION FOR YOUR SCENARIO**:
- **Use `OverwriteExisting = false`** (default) when scanning from Library screen ✅
- This will:
  1. ✅ Update collection metadata (name, path, settings)
  2. ✅ Queue a new collection scan job
  3. ✅ **Keep existing cache/thumbnail metadata** (no loss!)
  4. ✅ Discover new images (add to image arrays)
  5. ✅ Skip existing files on disk (no regeneration)

---

### 2. Can Scan Job Rescan and Update Image List Without Breaking Cache/Thumbnail Arrays?

**YES** ✅ - Collection scan handles duplicates safely:

**Code Location**: `src/ImageViewer.Application/Services/ImageService.cs:446-455`

```csharp
public async Task<ImageEmbedded> CreateEmbeddedImageAsync(...)
{
    // Check if image already exists (prevent duplicates from double-scans)
    var existingImage = collection.Images?.FirstOrDefault(img => 
        img.Filename == filename && img.RelativePath == relativePath);
    
    if (existingImage != null)
    {
        _logger.LogInformation("⚠️ Image {Filename} already exists in collection {CollectionId} with ID {ExistingId}, skipping duplicate creation", 
            filename, collectionId, existingImage.Id);
        return existingImage; // Return existing image
    }

    // Create new image only if it doesn't exist
    var embeddedImage = new ImageEmbedded(filename, relativePath, fileSize, width, height, format);
    var added = await _collectionRepository.AtomicAddImageAsync(collectionId, embeddedImage);
}
```

**Behavior**:
- Checks if image with same `filename` + `relativePath` already exists
- If exists: **Returns existing image** (no duplicate added) ✅
- If new: **Adds new image** atomically ✅
- **Existing thumbnails/cache arrays are NOT touched** ✅

**Result**:
- Rescan will **NOT** break existing cache/thumbnail arrays
- Rescan will **ADD** any new images discovered
- Rescan will **SKIP** images that already exist in the collection

---

### 3. Can Cache/Thumbnail Processors Skip Existing Files on Disk?

**YES** ✅ - Both processors check for existing files:

#### **Thumbnail Generation** (`src/ImageViewer.Worker/Services/ThumbnailGenerationConsumer.cs:155-161`)

```csharp
var existingThumbnail = collection.Thumbnails?.FirstOrDefault(t =>
    t.ImageId == thumbnailMessage.ImageId &&
    t.Width == thumbnailMessage.ThumbnailWidth &&
    t.Height == thumbnailMessage.ThumbnailHeight
);

if (existingThumbnail != null && File.Exists(existingThumbnail.ThumbnailPath))
{
    _logger.LogDebug("📁 Thumbnail already exists for image {ImageId}, skipping generation", thumbnailMessage.ImageId);
    // SKIP - thumbnail file exists on disk
    return;
}
```

**Behavior**:
- Checks if thumbnail metadata exists in `collection.Thumbnails`
- Checks if thumbnail **file exists on disk** (`File.Exists(...)`)
- If both true: **Skips generation** ✅
- If either false: **Generates thumbnail** ✅

#### **Cache Generation** (`src/ImageViewer.Worker/Services/CacheGenerationConsumer.cs:173-176`)

```csharp
// Check if cache already exists and force regeneration is disabled
if (!cacheMessage.ForceRegenerate && File.Exists(cachePath))
{
    _logger.LogDebug("📁 Cache already exists for image {ImageId}, skipping generation", cacheMessage.ImageId);
    // SKIP - cache file exists on disk
    return;
}
```

**Behavior**:
- `ForceRegenerate = false` (default): Checks if cache **file exists on disk**
- If exists: **Skips generation** ✅
- If missing: **Generates cache** ✅
- `ForceRegenerate = true`: **Always regenerates**, overwrites existing ⚠️

**Result**:
- With `ForceRegenerate = false` (default), existing files on disk are **NOT overwritten** ✅
- Only missing cache/thumbnail files are generated ✅

---

## Summary for Your Scenario

| Action | Will It Work? | Details |
|--------|---------------|---------|
| **Purge RabbitMQ queues** | ✅ Safe | All pending messages lost, but collections remain in DB |
| **Purge background jobs** | ✅ Safe | Job tracking lost, but collections remain in DB |
| **Keep collections in DB** | ✅ Safe | Collections with images/thumbnails/cache metadata remain |
| **Manual scan with OverwriteExisting=false** | ✅ **PERFECT!** ✨ | Keeps existing metadata + discovers new images + regenerates missing files only |
| **Manual scan with OverwriteExisting=true** | ⚠️ **DESTRUCTIVE** | Clears all image arrays + rescans from scratch + regenerates everything |
| **Rescan adds new images only** | ✅ Safe | Duplicate images are detected and skipped |
| **Rescan preserves cache/thumbnail arrays** | ✅ Safe | Existing metadata is preserved (when ForceRescan=false) |
| **Skip existing thumbnails on disk** | ✅ Safe | File existence checked, regeneration skipped |
| **Skip existing cache on disk** | ✅ Safe | File existence checked, regeneration skipped (if ForceRegenerate=false) |

---

## Recommended Steps

### ✅ **Safe Rescan Procedure**

1. **Purge RabbitMQ Queues** (optional, for clean slate)
   - Go to RabbitMQ Management UI
   - Purge all queues: `collection_scan_queue`, `image_processing_queue`, `thumbnail_generation_queue`, `cache_generation_queue`, `dlq`

2. **Purge Background Jobs** (optional, for clean slate)
   - In MongoDB, delete all documents from `background_jobs` collection:
     ```javascript
     db.background_jobs.deleteMany({});
     ```

3. **Manual Library Scan with OverwriteExisting=false (RECOMMENDED)** ✅
   - Go to Library screen
   - Click "Scan Library" button
   - **IMPORTANT**: **DISABLE** "Overwrite Existing" checkbox (default) ✅
   - This will:
     - Update all existing collection metadata
     - **Keep existing image/thumbnail/cache metadata** 🎉
     - Queue new scan jobs for all collections
     - Discover new images (add to arrays)
     - Skip duplicate images
     - Skip existing thumbnail/cache files on disk
     - Regenerate only missing files

4. **Wait for Processing**
   - Worker will process all scan jobs
   - Images: Duplicate detection → existing images skipped, new images added
   - Thumbnails: File existence check → existing thumbnails skipped, missing thumbnails generated
   - Cache: File existence check → existing cache skipped, missing cache generated

5. **Result**
   - All collections rescanned ✅
   - Image lists updated (new images added, duplicates skipped) ✅
   - Missing thumbnails/cache generated ✅
   - Existing files on disk preserved ✅
   - No data loss ✅

---

## ⚠️ What Could Go Wrong?

### **Scenario 1: Scan with OverwriteExisting=false**
- **Problem**: Existing collections are **SKIPPED**
- **Result**: No scan jobs queued, no cache/thumbnail regeneration
- **Solution**: Use `OverwriteExisting=true` ✅

### **Scenario 2: ForceRegenerate=true for Cache**
- **Problem**: Cache files will be **OVERWRITTEN** even if they exist
- **Result**: Regenerates all cache files, wastes time + disk I/O
- **Current Default**: `ForceRegenerate=false` (safe) ✅
- **Solution**: Keep default setting ✅

### **Scenario 3: Database Inconsistency**
- **Problem**: Collection metadata says thumbnails exist, but files are missing on disk
- **Cause**: Manual file deletion, disk corruption, etc.
- **Result**: Thumbnail/cache generation will be **SKIPPED** (metadata exists)
- **Solution**: 
  - Option A: Use `ForceRegenerate=true` to rebuild all
  - Option B: Write a cleanup script to remove metadata for missing files

---

## Code Quality Notes

### ✅ **What's Good**
1. **Duplicate Detection**: `CreateEmbeddedImageAsync` checks for existing images ✅
2. **Atomic Operations**: `AtomicAddImageAsync` prevents race conditions ✅
3. **File Existence Checks**: Both thumbnail and cache processors check disk before regenerating ✅
4. **Graceful Skipping**: Existing files are skipped with debug logs, not errors ✅

### ⚠️ **Potential Issues**
1. **No Database-Disk Sync**: If metadata says file exists but disk file is missing, no regeneration happens
2. **No Cleanup Logic**: Orphaned metadata (file deleted from disk) is not cleaned up automatically
3. **No Verification**: After rescan, no logic verifies that all expected files exist on disk

---

## Conclusion

**Your rescan scenario is SAFE** ✅, **BUT**:

1. **MUST use `OverwriteExisting=true`** when scanning from Library screen
2. **Keep default `ForceRegenerate=false`** to preserve existing files
3. **Existing files on disk will NOT be overwritten** ✅
4. **Missing files will be regenerated** ✅
5. **Duplicate images will be skipped** ✅
6. **Cache/thumbnail arrays will NOT be broken** ✅

**Final Recommendation**: ✅ **NOW PERFECT FOR YOUR SCENARIO!** 🎉

**What Changed**:
- **BEFORE**: `OverwriteExisting=false` skipped existing collections ❌
- **AFTER**: `OverwriteExisting=false` processes existing collections AND keeps metadata ✅

**Your 2500 cached collections are now SAFE!** 🛡️
- Use default scan (OverwriteExisting=false)
- Existing cache/thumbnail metadata preserved
- Only missing files regenerated
- No data loss! 🚀

