# System Settings - Configuration Guide

## Overview

The ImageViewer platform uses a database-driven configuration system that automatically initializes on API startup. All settings are stored in MongoDB's `system_settings` collection.

## Auto-Initialization

When the API starts for the first time, it automatically:
1. Checks if system settings exist in MongoDB
2. Creates default settings if they don't exist
3. Logs the initialization status
4. Continues even if initialization fails (graceful degradation)

**You don't need to manually configure anything!**

## Default Settings

### Cache Settings

| Setting | Default | Type | Description |
|---------|---------|------|-------------|
| `Cache.DefaultQuality` | **100** | Integer | JPEG quality for cache generation (0-100). Perfect quality default. |
| `Cache.DefaultFormat` | `jpeg` | String | Default format for cache images (jpeg, webp, original) |
| `Cache.DefaultWidth` | `1920` | Integer | Maximum width for cache images (FHD resolution) |
| `Cache.DefaultHeight` | `1080` | Integer | Maximum height for cache images (FHD resolution) |
| `Cache.PreserveOriginal` | `false` | Boolean | If true, skip resizing and keep original file |

### Thumbnail Settings

| Setting | Default | Type | Description |
|---------|---------|------|-------------|
| `Thumbnail.DefaultSize` | `300` | Integer | Default thumbnail size in pixels (300x300) |
| `Thumbnail.Quality` | `95` | Integer | Thumbnail JPEG quality (0-100) |
| `Thumbnail.Format` | `jpeg` | String | Thumbnail format (jpeg, webp) |

### Bulk Operation Settings

| Setting | Default | Type | Description |
|---------|---------|------|-------------|
| `BulkAdd.DefaultQuality` | **100** | Integer | Default quality for bulk add operations - Perfect quality for best results |
| `BulkAdd.DefaultFormat` | `jpeg` | String | Default format for bulk add cache generation |
| `BulkAdd.AutoScan` | `true` | Boolean | Automatically scan collections after bulk add |
| `BulkAdd.GenerateCache` | `true` | Boolean | Automatically generate cache for bulk added collections |
| `BulkAdd.GenerateThumbnails` | `true` | Boolean | Automatically generate thumbnails for bulk added collections |

## Quality Presets

The system includes 8 predefined quality presets stored in `Cache.QualityPresets`:

### 1. Perfect (100%) ⭐ **DEFAULT**
- **Quality:** 100
- **Format:** JPEG
- **Description:** Maximum quality, preserve original details
- **Best for:** Archival, professional work, bulk operations
- **File size:** Largest

### 2. High Quality (95%)
- **Quality:** 95
- **Format:** JPEG
- **Description:** Best quality with minimal compression
- **Best for:** High-quality viewing, printing
- **File size:** Very large

### 3. Optimized (85%)
- **Quality:** 85
- **Format:** JPEG
- **Description:** Balanced quality and file size
- **Best for:** General web viewing, fast loading
- **File size:** Medium

### 4. Medium (75%)
- **Quality:** 75
- **Format:** JPEG
- **Description:** Good quality, smaller file size
- **Best for:** Mobile viewing, slower connections
- **File size:** Small

### 5. Low (60%)
- **Quality:** 60
- **Format:** JPEG
- **Description:** Acceptable quality, fast loading
- **Best for:** Previews, very slow connections
- **File size:** Very small

### 6. WebP (85%)
- **Quality:** 85
- **Format:** WebP
- **Description:** Modern format with excellent compression
- **Best for:** Modern browsers, bandwidth optimization
- **File size:** Smaller than JPEG at same quality

### 7. WebP High (95%)
- **Quality:** 95
- **Format:** WebP
- **Description:** Modern format with high quality
- **Best for:** Best of both worlds (quality + size)
- **File size:** Smaller than JPEG 95%

### 8. Original (No Resize)
- **Quality:** 100
- **Format:** Original
- **Description:** Keep original quality and format, no processing
- **Best for:** Preserving exact original files
- **File size:** Same as original

## How Settings Are Used

### 1. On API Startup
```
API Starts
├─ Check if settings exist in MongoDB
├─ If not exists:
│  ├─ Create Cache.DefaultQuality = 100
│  ├─ Create Cache.DefaultFormat = jpeg
│  ├─ Create Cache.DefaultWidth = 1920
│  ├─ Create Cache.DefaultHeight = 1080
│  ├─ Create all other default settings
│  └─ Log: "✅ System settings initialized successfully"
└─ Continue API startup
```

### 2. During Image Processing
```
Image Processing Consumer
├─ Load SystemSettingService
├─ Get Cache.DefaultQuality (fallback: 100)
├─ Get Cache.DefaultFormat (fallback: jpeg)
├─ Get Cache.DefaultWidth (fallback: 1920)
├─ Get Cache.DefaultHeight (fallback: 1080)
├─ Get Cache.PreserveOriginal (fallback: false)
├─ Create CacheGenerationMessage with these settings
└─ Queue message for cache generation
```

### 3. During Cache Generation
```
Cache Generation Consumer
├─ Receive CacheGenerationMessage
├─ Check PreserveOriginal or Format == "original"
├─ If true:
│  ├─ Copy original file (no resize)
│  └─ Preserve original quality and format
├─ If false:
│  ├─ Resize to cache dimensions
│  └─ Apply quality setting
└─ Save cache file
```

## Updating Settings

### Via API (Recommended)
```csharp
// C# example
var systemSettingService = serviceProvider.GetRequiredService<ISystemSettingService>();

// Update quality to 95
await systemSettingService.UpdateSettingAsync("Cache.DefaultQuality", "95");

// Update format to WebP
await systemSettingService.UpdateSettingAsync("Cache.DefaultFormat", "webp");

// Enable preserve original
await systemSettingService.UpdateSettingAsync("Cache.PreserveOriginal", "true");
```

### Via MongoDB Shell
```javascript
// Connect to MongoDB
use image_viewer;

// Update cache quality to 95
db.system_settings.updateOne(
  { settingKey: "Cache.DefaultQuality" },
  { 
    $set: { 
      settingValue: "95",
      updatedAt: new Date()
    }
  }
);

// Update bulk add quality to 100
db.system_settings.updateOne(
  { settingKey: "BulkAdd.DefaultQuality" },
  { 
    $set: { 
      settingValue: "100",
      updatedAt: new Date()
    }
  }
);

// View all settings
db.system_settings.find().pretty();
```

## Fallback Behavior

If a setting is not found or cannot be loaded:
1. The system uses hard-coded fallback values
2. Logs a debug message
3. Continues processing without errors

**Fallback values:**
- Quality: **100** (Perfect)
- Format: `jpeg`
- Width: `1920`
- Height: `1080`
- PreserveOriginal: `false`

## MongoDB Collection Structure

### system_settings Collection
```json
{
  "_id": ObjectId("..."),
  "settingKey": "Cache.DefaultQuality",
  "settingValue": "100",
  "settingType": "Integer",
  "category": "Cache",
  "description": "Default JPEG quality for cache generation (0-100) - Perfect quality",
  "isEncrypted": false,
  "isSensitive": false,
  "isReadOnly": false,
  "defaultValue": "100",
  "validationRules": {},
  "lastModifiedBy": null,
  "source": "System",
  "version": 1,
  "changeHistory": [],
  "environment": "All",
  "isActive": true,
  "createdAt": ISODate("2025-10-09T..."),
  "updatedAt": ISODate("2025-10-09T...")
}
```

## Best Practices

### For Bulk Operations
- Use **Perfect (100%)** quality for best results
- Current default: **100** ✅
- Reason: Bulk operations are typically one-time, so prioritize quality over speed

### For Individual Cache
- Use **Optimized (85%)** for balanced performance
- Can be changed via settings
- Reason: Individual caches are regenerated frequently, so balance quality vs. disk space

### For Thumbnails
- Use **High (95%)** quality for crisp previews
- Current default: **95** ✅
- Reason: Thumbnails are small, so the file size difference is minimal

### For Archival
- Use **Original** preset (no resize)
- Set `Cache.PreserveOriginal = true`
- Reason: Preserves exact original files for archival purposes

## Performance Considerations

### Quality vs. File Size

| Quality | Avg. Size (1920x1080) | Relative Size | Quality Loss |
|---------|----------------------|---------------|--------------|
| 100 (Perfect) | ~500 KB | 100% | None |
| 95 (High) | ~350 KB | 70% | Minimal (imperceptible) |
| 85 (Optimized) | ~200 KB | 40% | Very low |
| 75 (Medium) | ~150 KB | 30% | Low |
| 60 (Low) | ~100 KB | 20% | Noticeable |

### WebP Advantages
- ~30% smaller than JPEG at same quality
- Better for web delivery
- Requires modern browser support

### Original Preset
- **Pros:** Exact preservation, no quality loss
- **Cons:** Largest file size, no optimization
- **Use case:** Archival, legal requirements, professional work

## Troubleshooting

### Settings Not Applying
1. Check MongoDB connection
2. Verify settings exist: `db.system_settings.find()`
3. Check logs for initialization errors
4. Restart API to re-initialize

### Cache Quality Issues
1. Check `Cache.DefaultQuality` value
2. Verify `Cache.PreserveOriginal` is `false` (unless you want original)
3. Check worker logs for quality being used
4. Regenerate cache with `ForceRegenerate = true`

### Settings Not Persisting
1. Check MongoDB write permissions
2. Verify `IsReadOnly = false` for the setting
3. Check `changeHistory` for update records
4. Look for validation errors in logs

## API Endpoints (Future Implementation)

### Get All Settings
```
GET /api/v1/system-settings
```

### Get Setting by Key
```
GET /api/v1/system-settings/{key}
```

### Update Setting
```
PUT /api/v1/system-settings/{key}
Body: { "value": "100" }
```

### Reset to Default
```
POST /api/v1/system-settings/{key}/reset
```

## Summary

✅ **Automatic initialization** - No manual setup required  
✅ **Perfect quality default** - 100% for best results  
✅ **Flexible presets** - 8 options from Perfect to Original  
✅ **Database-driven** - Easy to update without code changes  
✅ **Graceful fallbacks** - System continues even if settings fail  
✅ **Change tracking** - Full audit trail of setting changes  

**Result:** A robust, user-friendly configuration system that "just works" out of the box! 🎉

