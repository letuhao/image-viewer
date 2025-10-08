# 🔧 Legacy Code Refactoring Plan

## Overview
The ImageViewer application has been refactored to use MongoDB's embedded document design. Legacy entities and repositories are marked as `[Obsolete]` but still exist for backward compatibility with `CacheService` and `PerformanceService`.

## ✅ Completed Refactoring
- ✅ **ImageService** - Now uses embedded `ImageEmbedded` in `Collection`
- ✅ **All Consumers** - `ImageProcessingConsumer`, `ThumbnailGenerationConsumer`, `CacheGenerationConsumer`
- ✅ **ImagesController** - Updated API endpoints to use embedded design
- ✅ **CollectionService** - Triggers background jobs with embedded design
- ✅ **BackgroundJobService** - Uses embedded image methods

## ⚠️ Legacy Code (Marked as Obsolete)

### Entities (Keep for now - used by CacheService/PerformanceService)
- `Image.cs` - Use `ImageEmbedded` instead
- `ThumbnailInfo.cs` - Use `ThumbnailEmbedded` instead  
- `ImageCacheInfo.cs` - Use `ImageCacheInfoEmbedded` instead

### Interfaces (Keep for now)
- `IImageRepository.cs` - Use `ICollectionRepository` with embedded images
- `IThumbnailInfoRepository.cs` - Use embedded thumbnails
- `IImageCacheInfoRepository.cs` - Use embedded cache info
- `ICacheInfoRepository.cs` - Duplicate of `IImageCacheInfoRepository`, can be deleted
- `IUnitOfWork.cs` - Contains obsolete repositories

### Implementations (Keep for now)
- `MongoImageRepository.cs`
- `MongoThumbnailInfoRepository.cs`
- `MongoImageCacheInfoRepository.cs`
- `MongoUnitOfWork.cs`

## 📋 Step-by-Step Refactoring Plan

### Phase 1: Refactor CacheService ⏳
**Priority: Medium**

#### Current Dependencies:
```csharp
- ICacheFolderRepository
- ICollectionRepository  
- IImageRepository ❌ (obsolete)
- ICacheInfoRepository ❌ (obsolete)
- IImageProcessingService
- IUnitOfWork ❌ (obsolete)
```

#### Refactoring Steps:
1. Update `GetCachedImageAsync()` to use `Collection.Images[].CacheInfo`
2. Update `GenerateCacheAsync()` to use `ImageService.GenerateCacheAsync()`
3. Update `InvalidateCacheAsync()` to update embedded cache info
4. Update `CleanupExpiredCacheAsync()` to query `Collection` documents
5. Update `GetCacheStatisticsAsync()` to aggregate from `Collection.Images[]`
6. Remove dependencies on `IImageRepository`, `ICacheInfoRepository`, `IUnitOfWork`

#### Files to Modify:
- `src/ImageViewer.Application/Services/CacheService.cs`
- `src/ImageViewer.Application/Services/ICacheService.cs` (if needed)

### Phase 2: Refactor PerformanceService ⏳
**Priority: Medium**

#### Current Dependencies:
```csharp
- IUserRepository
- IPerformanceMetricRepository
- ICacheInfoRepository ❌ (obsolete)
- IMediaProcessingJobRepository
```

#### Refactoring Steps:
1. Update `GetCacheMetricsAsync()` to aggregate from `Collection.Images[].CacheInfo`
2. Update `ClearCacheAsync()` to use `ImageService` methods
3. Update `OptimizeCacheAsync()` to use embedded design
4. Remove dependency on `ICacheInfoRepository`

#### Files to Modify:
- `src/ImageViewer.Application/Services/PerformanceService.cs`

### Phase 3: Remove Legacy Code 🗑️
**Priority: Low - Only after Phases 1 & 2 are complete**

Once `CacheService` and `PerformanceService` are refactored, remove in this order:

1. **Remove DI Registrations:**
   ```csharp
   // In ServiceCollectionExtensions.cs
   - services.AddScoped<IImageRepository, MongoImageRepository>();
   - services.AddScoped<IImageCacheInfoRepository, MongoImageCacheInfoRepository>();
   - services.AddScoped<IThumbnailInfoRepository, MongoThumbnailInfoRepository>();
   ```

2. **Delete Implementation Files:**
   - `MongoImageRepository.cs`
   - `MongoThumbnailInfoRepository.cs`
   - `MongoImageCacheInfoRepository.cs`
   - `MongoCacheInfoRepository.cs` (if different from above)

3. **Delete Interface Files:**
   - `IImageRepository.cs`
   - `IThumbnailInfoRepository.cs`
   - `IImageCacheInfoRepository.cs`
   - `ICacheInfoRepository.cs`

4. **Delete Entity Files:**
   - `Image.cs`
   - `ThumbnailInfo.cs`
   - `ImageCacheInfo.cs`

5. **Refactor IUnitOfWork:**
   - Remove `Images` property
   - Remove `ImageCacheInfos` property
   - Remove `ThumbnailInfo` property

6. **Final Cleanup:**
   - Update all test fixtures that mock these repositories
   - Remove any remaining references in test files

## 🚀 Migration Strategy

### For New Features:
✅ **ALWAYS use the embedded design:**
- Use `ImageEmbedded` in `Collection.Images[]`
- Use `ThumbnailEmbedded` in `Collection.Thumbnails[]`
- Use `ImageCacheInfoEmbedded` in `ImageEmbedded.CacheInfo`
- Use `ICollectionRepository` and `IImageService`

### For Existing Features:
⚠️ **Temporary backward compatibility:**
- `CacheService` and `PerformanceService` still use legacy repositories
- These will be refactored in Phase 1 & 2
- Do NOT add new methods to legacy repositories

## 📊 Current Status

| Component | Status | Action Required |
|-----------|--------|----------------|
| **ImageService** | ✅ Refactored | None |
| **Consumers** | ✅ Refactored | None |
| **API Controllers** | ✅ Refactored | None |
| **CollectionService** | ✅ Refactored | None |
| **CacheService** | ⚠️ Legacy | Refactor (Phase 1) |
| **PerformanceService** | ⚠️ Legacy | Refactor (Phase 2) |
| **Legacy Repositories** | ⚠️ Obsolete | Remove (Phase 3) |

## 📝 Notes

- All legacy code is marked with `[Obsolete]` attributes
- Warnings are suppressed where legacy code is still legitimately used
- New code will get compile warnings if it tries to use obsolete APIs
- This prevents accidental usage while maintaining stability

## Timeline

- **Phase 1 (CacheService):** ~2-4 hours
- **Phase 2 (PerformanceService):** ~1-2 hours  
- **Phase 3 (Cleanup):** ~30 minutes

**Total Estimated Time:** ~4-7 hours

