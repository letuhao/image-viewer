# API Restart Required for DTO Changes

## Issue

The frontend is still receiving **full Collection entities** with all embedded images instead of the lightweight **CollectionOverviewDto**.

### Current Behavior (Wrong):
```json
// Response from GET /api/v1/collections
[
  {
    "id": "...",
    "name": "Collection 1",
    "images": [ /* 1000s of image objects */ ],
    "thumbnails": [ /* ... */ ],
    "cacheImages": [ /* ... */ ],
    // ... all embedded data
  }
]
```

### Expected Behavior (After Restart):
```json
// Response from GET /api/v1/collections  
[
  {
    "id": "...",
    "name": "Collection 1",
    "path": "...",
    "type": "folder",
    "isNested": false,
    "depth": 0,
    "imageCount": 120,
    "thumbnailCount": 120,
    "cacheImageCount": 100,
    "totalSize": 52428800,
    "createdAt": "2025-10-10T...",
    "updatedAt": "2025-10-10T..."
  }
]
```

## Root Cause

The API code has been updated to return DTOs, but the **running API process** is still using the old compiled code. The changes won't take effect until you **restart the API**.

## Solution

### Step 1: Stop the API (if running)

**Option A: PowerShell Script**
```powershell
.\scripts\deployment\stop-api.ps1
```

**Option B: Manual (if running in terminal)**
```
Press Ctrl+C in the terminal where API is running
```

**Option C: Kill Process**
```powershell
Get-Process -Name "ImageViewer.Api" | Stop-Process -Force
```

### Step 2: Start the API

```powershell
.\scripts\deployment\start-api.ps1
```

**OR** if you want to run it manually:

```powershell
cd src/ImageViewer.Api
dotnet run --configuration Release
```

### Step 3: Verify

**Test the endpoint:**
```bash
curl http://localhost:11000/api/v1/collections?page=1&limit=20
```

**Expected:** Lightweight DTOs (~10KB for 20 items)  
**Wrong:** Full entities with images (~10MB for 20 items)

## Verification Checklist

After restarting the API, verify:

- [ ] API is running on port 11000
- [ ] GET `/api/v1/collections` returns **CollectionOverviewDto[]** (no embedded images)
- [ ] GET `/api/v1/collections/{id}` returns **CollectionDetailDto** (with all embedded data)
- [ ] Frontend loads collections list quickly (<100ms)
- [ ] Network tab shows small payloads (~500 bytes per collection)

## What Changed

### Controllers Updated:
- `CollectionsController.GetCollections()` - Now returns `overviewDtos.Select(c => c.ToOverviewDto())`

### DTOs Created:
- `CollectionOverviewDto` - Lightweight for lists
- `CollectionDetailDto` - Complete for detail view

### Mapping Extensions:
- `CollectionMappingExtensions.ToOverviewDto()` - Converts to lightweight DTO
- `CollectionMappingExtensions.ToDetailDto()` - Converts to full DTO

## Performance Impact (After Restart)

| Endpoint | Before | After | Improvement |
|----------|--------|-------|-------------|
| `GET /collections` (20 items) | ~10MB | ~10KB | **1000x smaller** |
| Load time | 3-5 sec | <100ms | **50x faster** |
| Bandwidth | High | Minimal | **99% less** |

## Troubleshooting

### API won't start?
```powershell
# Check if port 11000 is in use
netstat -ano | findstr :11000

# Kill process using the port
Stop-Process -Id <PID> -Force
```

### Still seeing large payloads?
1. Hard refresh browser: `Ctrl+F5`
2. Clear browser cache
3. Check API logs for errors
4. Verify API is running the Release build

### Frontend still calling wrong endpoint?
The frontend should call:
- `GET /api/v1/collections` - For lists (returns overview DTOs)
- `GET /api/v1/collections/{id}` - For detail (returns full DTO)

## Next Steps

After restarting the API:

1. ✅ Verify lightweight DTOs are returned
2. ✅ Check frontend loads quickly
3. ✅ Monitor network tab for small payloads
4. ✅ Test pagination still works
5. ✅ Test collection detail page still loads all data

