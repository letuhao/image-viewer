# Database Migration Guide: Legacy to Embedded Design
**Date**: October 9, 2025  
**Migration Type**: Breaking Change  
**Estimated Duration**: 2-4 hours (depending on data size)

## ⚠️ Important Warnings

### **Breaking Change**
This migration is a **breaking change** and is **NOT backward compatible**. Once migrated, you cannot roll back to the old version without restoring from backup.

### **Backup Required**
**CRITICAL**: You MUST create a full MongoDB backup before proceeding!

### **Downtime Required**
This migration requires application downtime. Plan accordingly.

---

## 📋 Pre-Migration Checklist

- [ ] **Backup MongoDB database** (see backup section below)
- [ ] **Stop all application instances** (API + Worker)
- [ ] **Stop all background jobs**
- [ ] **Verify backup integrity**
- [ ] **Review migration script**
- [ ] **Test migration on staging environment**
- [ ] **Notify users of maintenance window**
- [ ] **Prepare rollback plan**

---

## 💾 Step 1: Backup Database

### **Using mongodump**
```bash
# Full database backup
mongodump --uri="mongodb://localhost:27017/imageviewer" --out="/backup/imageviewer-$(date +%Y%m%d-%H%M%S)"

# Verify backup
ls -lh /backup/imageviewer-*
```

### **Using MongoDB Compass**
1. Connect to database
2. Click "..." menu → "Export Collection"
3. Export each collection individually
4. Verify exported files

### **Cloud-Based Backup (Atlas)**
1. Navigate to Atlas console
2. Go to "Backup" tab
3. Create manual snapshot
4. Wait for completion
5. Verify snapshot status

---

## 🛑 Step 2: Stop Application

### **Stop API and Worker**
```bash
# PowerShell (Windows)
.\stop-api.ps1

# Linux/Mac
./stop.sh
```

### **Verify Services Stopped**
```bash
# Check no processes running
Get-Process | Where-Object {$_.ProcessName -like "*ImageViewer*"}

# Should return empty
```

---

## 📦 Step 3: Run Migration Script

### **Migration Script (JavaScript for mongosh)**

Create file: `migrate-to-embedded-design.js`

```javascript
// migrate-to-embedded-design.js
// MongoDB Migration Script: Legacy to Embedded Design
// Date: 2025-10-09

print("=== ImageViewer Database Migration ===");
print("Migration: Legacy Collections to Embedded Design");
print("Start Time: " + new Date());
print("");

// Database connection
const db = db.getSiblingDB('imageviewer');

// Step 1: Verify collections exist
print("Step 1: Verifying source collections...");
const collections = db.getCollectionNames();
const requiredCollections = ['collections', 'images', 'thumbnail_info'];
const missingCollections = requiredCollections.filter(c => !collections.includes(c));

if (missingCollections.length > 0) {
    print("ERROR: Missing required collections: " + missingCollections.join(', '));
    quit(1);
}
print("✓ All required collections found");
print("");

// Step 2: Get counts
print("Step 2: Analyzing data...");
const collectionCount = db.collections.countDocuments({deleted_at: null});
const imageCount = db.images.countDocuments({deleted_at: null});
const thumbnailCount = db.thumbnail_info.countDocuments({});

print("Collections: " + collectionCount);
print("Images: " + imageCount);
print("Thumbnails: " + thumbnailCount);
print("");

// Step 3: Migrate images into collections
print("Step 3: Migrating images to embedded design...");
let migratedCollections = 0;
let migratedImages = 0;
let migratedThumbnails = 0;

db.collections.find({deleted_at: null}).forEach(function(collection) {
    print("Processing collection: " + collection.name + " (ID: " + collection._id + ")");
    
    // Get all images for this collection
    const images = db.images.find({
        collection_id: collection._id,
        deleted_at: null
    }).toArray();
    
    if (images.length === 0) {
        print("  No images found for this collection");
        return;
    }
    
    print("  Found " + images.length + " images");
    
    // Transform images to embedded format
    const embeddedImages = images.map(function(img) {
        // Get cache info if exists
        let cacheInfo = null;
        if (db.image_cache_info) {
            const cache = db.image_cache_info.findOne({image_id: img._id});
            if (cache) {
                cacheInfo = {
                    cache_folder_id: cache.cache_folder_id,
                    cache_size_preset: cache.cache_size_preset,
                    cached_path: cache.cached_path,
                    cached_size: cache.cached_size,
                    cached_at: cache.cached_at
                };
            }
        }
        
        return {
            _id: img._id,
            filename: img.filename,
            relative_path: img.relative_path,
            file_size: img.file_size,
            width: img.width,
            height: img.height,
            format: img.format,
            cache_info: cacheInfo,
            metadata: img.metadata || null,
            created_at: img.created_at,
            updated_at: img.updated_at,
            deleted_at: null,
            view_count: img.view_count || 0
        };
    });
    
    // Get thumbnails for this collection's images
    const imageIds = images.map(img => img._id);
    const thumbnails = db.thumbnail_info.find({
        image_id: {$in: imageIds}
    }).toArray();
    
    print("  Found " + thumbnails.length + " thumbnails");
    
    // Transform thumbnails to embedded format
    const embeddedThumbnails = thumbnails.map(function(thumb) {
        return {
            _id: thumb._id,
            image_id: thumb.image_id,
            thumbnail_path: thumb.thumbnail_path,
            width: thumb.width,
            height: thumb.height,
            file_size: thumb.file_size,
            created_at: thumb.created_at
        };
    });
    
    // Calculate statistics
    const totalSize = embeddedImages.reduce((sum, img) => sum + (img.file_size || 0), 0);
    const cachedCount = embeddedImages.filter(img => img.cache_info !== null).length;
    const totalViews = embeddedImages.reduce((sum, img) => sum + (img.view_count || 0), 0);
    
    const statistics = {
        total_images: embeddedImages.length,
        total_size: totalSize,
        average_file_size: totalSize / embeddedImages.length,
        cached_images: cachedCount,
        total_views: totalViews
    };
    
    // Update collection with embedded data
    const result = db.collections.updateOne(
        {_id: collection._id},
        {
            $set: {
                images: embeddedImages,
                thumbnails: embeddedThumbnails,
                statistics: statistics,
                updated_at: new Date()
            }
        }
    );
    
    if (result.modifiedCount === 1) {
        print("  ✓ Successfully migrated collection");
        migratedCollections++;
        migratedImages += embeddedImages.length;
        migratedThumbnails += embeddedThumbnails.length;
    } else {
        print("  ✗ ERROR: Failed to update collection");
    }
});

print("");
print("Migration Summary:");
print("- Collections migrated: " + migratedCollections + " / " + collectionCount);
print("- Images migrated: " + migratedImages + " / " + imageCount);
print("- Thumbnails migrated: " + migratedThumbnails + " / " + thumbnailCount);
print("");

// Step 4: Verify migration
print("Step 4: Verifying migration...");
const verifyCount = db.collections.countDocuments({
    "images.0": {$exists: true},
    deleted_at: null
});
print("Collections with embedded images: " + verifyCount);

if (verifyCount === migratedCollections) {
    print("✓ Verification passed");
} else {
    print("✗ WARNING: Verification count mismatch!");
    print("  Expected: " + migratedCollections);
    print("  Actual: " + verifyCount);
}
print("");

// Step 5: Create indexes for embedded queries
print("Step 5: Creating indexes for embedded design...");
db.collections.createIndex({"images._id": 1});
db.collections.createIndex({"images.filename": 1});
db.collections.createIndex({"images.cache_info.cached_at": 1});
db.collections.createIndex({"thumbnails.image_id": 1});
print("✓ Indexes created");
print("");

// Step 6: Rename old collections (don't delete yet!)
print("Step 6: Archiving old collections...");
db.images.renameCollection("_ARCHIVED_images_" + Date.now());
db.thumbnail_info.renameCollection("_ARCHIVED_thumbnail_info_" + Date.now());
if (db.image_cache_info) {
    db.image_cache_info.renameCollection("_ARCHIVED_image_cache_info_" + Date.now());
}
print("✓ Old collections archived (prefixed with _ARCHIVED_)");
print("");

print("=== Migration Complete ===");
print("End Time: " + new Date());
print("");
print("IMPORTANT: Verify the application works before dropping archived collections!");
print("To rollback: Rename archived collections back to original names");
```

### **Run Migration**
```bash
# Using mongosh
mongosh "mongodb://localhost:27017/imageviewer" migrate-to-embedded-design.js

# Expected output:
# === ImageViewer Database Migration ===
# Step 1: Verifying source collections...
# ✓ All required collections found
# Step 2: Analyzing data...
# Collections: 50
# Images: 7500
# Thumbnails: 7500
# Step 3: Migrating images to embedded design...
# ...
# === Migration Complete ===
```

---

## ✅ Step 4: Verify Migration

### **Check MongoDB**
```javascript
// In mongosh
use imageviewer

// Verify collections have embedded images
db.collections.findOne({name: "Test Collection"})
// Should show: images: [...], thumbnails: [...], statistics: {...}

// Count collections with images
db.collections.countDocuments({"images.0": {$exists: true}})
// Should equal total non-deleted collections

// Verify old collections are archived
db.getCollectionNames().filter(c => c.startsWith("_ARCHIVED_"))
// Should show: _ARCHIVED_images_*, _ARCHIVED_thumbnail_info_*, etc.
```

### **Verification Checklist**
- [ ] All collections have `images` array
- [ ] All collections have `thumbnails` array
- [ ] All collections have `statistics` object
- [ ] Image counts match original
- [ ] Thumbnail counts match original
- [ ] Old collections are archived (renamed with _ARCHIVED_ prefix)
- [ ] Indexes created successfully

---

## 🚀 Step 5: Deploy New Application

### **Deploy Updated Code**
```bash
# Pull latest code
git pull origin main

# Build application
dotnet build src/ImageViewer.sln --configuration Release

# Start services
.\start-api.ps1
```

### **Verify Application**
```bash
# Check API health
curl http://localhost:5000/health

# Check worker health
Get-Process | Where-Object {$_.ProcessName -like "*ImageViewer*"}
```

### **Test Endpoints**
```bash
# Get collections
curl http://localhost:5000/api/v1/collections

# Get collection with images
curl http://localhost:5000/api/v1/collections/{id}

# Get image
curl http://localhost:5000/api/v1/collections/{collectionId}/images/{imageId}

# Get statistics
curl http://localhost:5000/api/v1/statistics/collections/{id}
```

---

## 🧹 Step 6: Cleanup (After Verification)

### **Wait Period**
⏰ **IMPORTANT**: Wait at least 1 week with the application running in production before dropping archived collections!

### **Drop Archived Collections** (Only after thorough testing)
```javascript
// In mongosh - ONLY AFTER 1 WEEK OF SUCCESSFUL OPERATION
use imageviewer

// List archived collections
db.getCollectionNames().filter(c => c.startsWith("_ARCHIVED_"))

// Drop each one (IRREVERSIBLE!)
db._ARCHIVED_images_1728483600000.drop()
db._ARCHIVED_thumbnail_info_1728483600000.drop()
db._ARCHIVED_image_cache_info_1728483600000.drop()

print("✓ Archived collections dropped");
```

---

## 🔄 Rollback Plan

### **If Migration Fails**

#### **During Migration**
1. Stop migration script (Ctrl+C)
2. Restore from backup:
   ```bash
   mongorestore --uri="mongodb://localhost:27017" --drop /backup/imageviewer-YYYYMMDD-HHMMSS
   ```
3. Verify data restored
4. Investigate issue before retrying

#### **After Migration (Before Cleanup)**
1. Stop new application
2. Rename archived collections back:
   ```javascript
   db._ARCHIVED_images_1728483600000.renameCollection("images")
   db._ARCHIVED_thumbnail_info_1728483600000.renameCollection("thumbnail_info")
   db._ARCHIVED_image_cache_info_1728483600000.renameCollection("image_cache_info")
   ```
3. Remove embedded data from collections:
   ```javascript
   db.collections.updateMany(
       {},
       {
           $unset: {
               images: "",
               thumbnails: "",
               statistics: ""
           }
       }
   )
   ```
4. Deploy old application version
5. Verify functionality
6. Investigate issue before retrying

#### **After Cleanup (Archived Collections Dropped)**
1. Stop application
2. Restore from backup:
   ```bash
   mongorestore --uri="mongodb://localhost:27017" --drop /backup/imageviewer-YYYYMMDD-HHMMSS
   ```
3. Deploy old application version
4. Verify data integrity
5. **This is why we wait 1 week before cleanup!**

---

## 📊 Performance Testing After Migration

### **Database Queries**
```javascript
// Test query performance
use imageviewer

// Baseline: Get collection with all images (should be fast - single query)
db.collections.find({_id: ObjectId("...")}).explain("executionStats")

// Should show: 
// - executionTimeMillis: < 50ms
// - totalDocsExamined: 1
// - nReturned: 1
```

### **API Performance**
```bash
# Test API response times
time curl http://localhost:5000/api/v1/collections/{id}

# Should be < 200ms for typical collection with 100-500 images
```

### **Monitoring**
- Monitor MongoDB query performance
- Monitor API response times
- Monitor memory usage (larger documents)
- Monitor disk usage

---

## 🐛 Troubleshooting

### **Problem: Migration Script Fails**
**Symptoms**: Error messages during migration, partial data migration

**Solutions**:
1. Check MongoDB connection
2. Verify MongoDB version (7.0+ required)
3. Check disk space
4. Review error messages in script output
5. Restore from backup and retry

### **Problem: Application Won't Start**
**Symptoms**: API/Worker crashes on startup

**Solutions**:
1. Check application logs
2. Verify DI registrations
3. Verify MongoDB connection string
4. Check embedded services are registered
5. Review recent code changes

### **Problem: Images Not Displaying**
**Symptoms**: API returns empty image arrays

**Solutions**:
1. Check migration completed successfully
2. Verify `collections.images[]` exists
3. Check API queries use correct repository methods
4. Review application logs for errors

### **Problem: Performance Degradation**
**Symptoms**: Slow queries, high memory usage

**Solutions**:
1. Check document sizes (should be < 2MB typical)
2. Verify indexes are created
3. Monitor MongoDB query execution plans
4. Consider pagination for large collections

---

## 📈 Expected Results

### **Database Changes**
| Metric | Before | After | Change |
|--------|--------|-------|--------|
| Collections | 97 | 94 | -3 (removed legacy) |
| Queries per Page Load | 3-5 | 1-2 | -60% |
| Avg Query Time | 150ms | 50ms | -67% |
| Data Duplication | None | None | Same |

### **Code Changes**
| Metric | Before | After | Change |
|--------|--------|-------|--------|
| Services | 15 | 15 | Refactored |
| Repositories | 12 | 9 | -3 (removed) |
| Lines of Code | ~8000 | ~3500 | -56% |

---

## 📝 Post-Migration Tasks

- [ ] Update monitoring dashboards
- [ ] Update documentation with new queries
- [ ] Train team on new data model
- [ ] Update backup procedures
- [ ] Schedule performance review (1 week)
- [ ] Schedule cleanup of archived collections (1 week)
- [ ] Document any issues encountered
- [ ] Update runbooks and procedures

---

## 📞 Support

### **If You Need Help**
1. Check this migration guide thoroughly
2. Review application logs
3. Check MongoDB logs
4. Consult architecture documentation
5. Test in staging environment first

### **Emergency Rollback**
If critical issues occur in production:
1. Immediately stop application
2. Follow rollback plan above
3. Restore from backup if needed
4. Investigate root cause before retrying

---

**Last Updated**: October 9, 2025  
**Migration Version**: 1.0  
**Application Version**: 2.0 (Embedded Design)  
**Tested On**: MongoDB 7.0, .NET 8

