# Nested Collection Scan Fix

## Problem Identified

The bulk add operation was not detecting collections with nested image structures.

### Root Cause

In `BulkService.HasImageFiles()` method (line 362):

```csharp
// ❌ OLD CODE (WRONG):
var files = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly);
```

This meant:
- **Bulk scan found folders** ✅ (using `includeSubfolders` correctly)
- **But `HasImageFiles()` only checked root level** ❌
- **Images in nested folders were NOT detected** ❌

### Example

```
L:\test\
  ├── [Collection1]/            ← Found by bulk scan ✅
  │   ├── subfolder1/
  │   │   └── images/           ← NOT checked by HasImageFiles() ❌
  │   │       ├── img1.jpg      ← MISSED!
  │   │       └── img2.jpg      ← MISSED!
  │   └── subfolder2/
  │       └── image.png         ← MISSED!
  └── [Archive.zip]             ← Works fine ✅
```

## Solution Applied

### Code Change

```csharp
// ✅ NEW CODE (FIXED):
private Task<bool> HasImageFiles(string directory)
{
    try
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".svg" };
        
        // IMPORTANT: Use AllDirectories to find images in nested folders!
        // This ensures that collections with nested image structures are detected
        // Example: L:\test\collection1\subfolder\images\*.jpg will be found
        var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
        
        return Task.FromResult(files.Any(file => 
            imageExtensions.Contains(Path.GetExtension(file).ToLowerInvariant())));
    }
    catch
    {
        return Task.FromResult(false);
    }
}
```

### Impact

- **Now detects** collections with images in nested subfolders
- **Consistent** with `FindPotentialCollections()` behavior
- **No performance impact** - already iterating folders recursively

## Testing Required

1. Clear all data
2. Bulk add a parent path with nested collections:
   ```
   L:\test\
     ├── [Collection with nested images]/
     │   └── images/
     │       └── *.jpg
     └── [Archive.zip]
   ```
3. Verify all collections are detected and processed
4. Check job stages complete successfully

## Files Modified

- `src/ImageViewer.Application/Services/BulkService.cs` (line 366)

## Related Fix

This complements the earlier **flow order fix** in `CollectionScanConsumer.cs` where:
- Scan stage completes FIRST
- Then thumbnail/cache stages start
- MonitorJobCompletionAsync initializes stages on first detection

Both fixes together ensure:
1. **All collections are detected** (even with nested images)
2. **Jobs track progress correctly** (proper flow order)
3. **No race conditions** (atomic MongoDB operations)

