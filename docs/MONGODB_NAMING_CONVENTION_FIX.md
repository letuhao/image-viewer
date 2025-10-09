# MongoDB Naming Convention Standardization

## Problem Statement

The ImageViewer codebase has **inconsistent MongoDB naming conventions**:

### Collection Names
- ✅ **Correct (snake_case):** `background_jobs`, `cache_folders`, `system_settings`
- ❌ **Incorrect (camelCase):** Some entities were using camelCase in repository definitions

### Property Names  
- ✅ **Correct (camelCase with [BsonElement]):** `SystemSetting`, `Collection`, `Library`, etc.
- ❌ **Incorrect (PascalCase, no [BsonElement]):** 9 entities missing attributes

## MongoDB Standard Conventions

### 1. Collection Names: **snake_case**
```
users
background_jobs
cache_folders
system_settings
collection_statistics
```

### 2. Property Names: **camelCase** (via [BsonElement])
```csharp
[BsonElement("jobType")]
public string JobType { get; private set; }

[BsonElement("settingKey")]
public string SettingKey { get; private set; }
```

### 3. Document Structure Example
```json
{
  "_id": ObjectId("..."),
  "jobType": "collection-scan",         // camelCase
  "status": "InProgress",                // camelCase  
  "progress": 50,                        // camelCase
  "createdAt": ISODate("..."),          // camelCase
  "updatedAt": ISODate("...")           // camelCase
}
```

## Current Status

### ✅ Fixed Collection Names (snake_case)
- `system_settings` ✓ (was `systemSettings`)
- `background_jobs` ✓
- `cache_folders` ✓
- `view_sessions` ✓
- `user_settings` ✓
- `refresh_tokens` ✓

### ❌ Entities Missing [BsonElement] Attributes

These **9 entities** are storing properties with **PascalCase** names in MongoDB instead of **camelCase**:

1. **BackgroundJob.cs** ⚠️ **CRITICAL** - Actively used
   - Properties: `JobType`, `Status`, `Progress`, `TotalItems`, etc.
   - Should be: `jobType`, `status`, `progress`, `totalItems`
   
2. **CacheFolder.cs** ⚠️ **CRITICAL** - Actively used
   - Properties: `Name`, `Path`, `MaxSizeBytes`, `CurrentSizeBytes`
   - Should be: `name`, `path`, `maxSizeBytes`, `currentSizeBytes`

3. **ViewSession.cs** ⚠️ **CRITICAL** - Actively used
   - Properties: `UserId`, `CollectionId`, `Settings`
   - Should be: `userId`, `collectionId`, `settings`

4. **CollectionCacheBinding.cs**
   - Properties need camelCase mapping

5. **CollectionSettingsEntity.cs**
   - Properties need camelCase mapping

6. **CollectionStatisticsEntity.cs**
   - Properties need camelCase mapping

7. **CollectionTag.cs**
   - Properties need camelCase mapping

8. **ImageMetadataEntity.cs**
   - Properties need camelCase mapping

9. **Tag.cs**
   - Properties need camelCase mapping

## Impact Analysis

### Current Database State
The MongoDB collections currently have **MIXED** property naming:
- 55 entities use camelCase (correct) ✅
- 9 entities use PascalCase (incorrect) ❌

### Migration Required
**YES** - Existing data will need migration because property names are changing.

**Example:**
```json
// BEFORE (PascalCase - WRONG)
{
  "_id": ObjectId("..."),
  "JobType": "collection-scan",    // ❌ PascalCase
  "Status": "InProgress",          // ❌ PascalCase
  "Progress": 50                   // ❌ PascalCase
}

// AFTER (camelCase - CORRECT)
{
  "_id": ObjectId("..."),
  "jobType": "collection-scan",    // ✅ camelCase
  "status": "InProgress",          // ✅ camelCase
  "progress": 50                   // ✅ camelCase
}
```

## Fix Plan

### Phase 1: Add [BsonElement] Attributes ✅ (Recommended to do NOW)

For each of the 9 entities, add `[BsonElement("camelCaseName")]` to all properties.

**Example for BackgroundJob.cs:**
```csharp
public class BackgroundJob : BaseEntity
{
    [BsonElement("jobType")]
    public string JobType { get; private set; }
    
    [BsonElement("status")]
    public string Status { get; private set; }
    
    [BsonElement("progress")]
    public int Progress { get; private set; }
    
    [BsonElement("totalItems")]
    public int TotalItems { get; private set; }
    
    // ... etc
}
```

### Phase 2: Data Migration (Optional)

If you want to migrate existing data from PascalCase to camelCase:

```javascript
// MongoDB migration script
use image_viewer;

// Migrate background_jobs
db.background_jobs.find().forEach(doc => {
  db.background_jobs.updateOne(
    { _id: doc._id },
    { 
      $set: {
        jobType: doc.JobType,
        status: doc.Status,
        progress: doc.Progress,
        totalItems: doc.TotalItems,
        // ... map all fields
      },
      $unset: {
        JobType: "",
        Status: "",
        Progress: "",
        TotalItems: "",
        // ... unset old fields
      }
    }
  );
});

// Repeat for other collections
```

### Phase 3: Verification

After adding [BsonElement] attributes:

1. **Test Data Insert:**
   ```csharp
   var job = new BackgroundJob("test-job");
   await repository.CreateAsync(job);
   ```

2. **Verify in MongoDB:**
   ```javascript
   db.background_jobs.findOne()
   // Should show: { "jobType": "test-job", ... }
   ```

3. **Confirm all 9 entities:** Run the verification script again

## Benefits After Fix

### ✅ Consistency
- All entities follow same naming convention
- Easier to maintain and understand

### ✅ MongoDB Best Practices
- Follows official MongoDB naming guidelines
- camelCase for fields (JavaScript/JSON standard)
- snake_case for collections (database standard)

### ✅ Developer Experience
- No confusion about property names
- Consistent casing across entire codebase
- Better interoperability with MongoDB tools

### ✅ Future-Proof
- Compatible with MongoDB aggregation pipeline
- Works well with MongoDB Compass queries
- Easier to write raw MongoDB queries

## Immediate Action Required

### Priority 1: Critical Entities (Actively Used)
1. **BackgroundJob.cs** - Used in worker and API
2. **CacheFolder.cs** - Used in cache system
3. **ViewSession.cs** - Used in viewer

### Priority 2: Supporting Entities
4. CollectionCacheBinding.cs
5. CollectionSettingsEntity.cs
6. CollectionStatisticsEntity.cs
7. CollectionTag.cs
8. ImageMetadataEntity.cs
9. Tag.cs

## Verification Script

Run this after fixes to confirm all entities have [BsonElement]:

```powershell
Get-ChildItem "src/ImageViewer.Domain/Entities" -Filter "*.cs" | 
  Where-Object { $_.Name -ne "BaseEntity.cs" } | 
  ForEach-Object { 
    $content = Get-Content $_.FullName -Raw
    $hasBsonElement = $content -match '\[BsonElement\('
    if (-not $hasBsonElement) {
      Write-Host "❌ MISSING: $($_.Name)" -ForegroundColor Red
    }
  }
```

## Recommendation

**Recommended approach:**

1. ✅ **Keep existing data as-is** (PascalCase will still work)
2. ✅ **Add [BsonElement] attributes NOW** (new data will use camelCase)
3. ⏭️ **Migrate data later** when convenient (optional, not urgent)

**Why?** MongoDB's C# driver can read both PascalCase and camelCase if you have `[BsonElement]` attributes. This allows gradual migration without breaking existing data.

## Conclusion

The naming inconsistency affects **9 out of 64 entities (14%)**. While not critical for functionality, it creates:
- Maintenance burden
- Developer confusion  
- Non-standard MongoDB documents

**Action:** Add `[BsonElement]` attributes to the 9 entities to ensure all future data follows MongoDB conventions.

