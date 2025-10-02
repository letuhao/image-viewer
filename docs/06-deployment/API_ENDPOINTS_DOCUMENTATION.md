# ImageViewer API Endpoints Documentation

## 📋 **Tổng quan**
Hệ thống ImageViewer có **12 Controllers** với **50+ endpoints** được implement đầy đủ.

---

## 🔐 **1. AuthController** - Authentication & Authorization
**Base Route:** `/api/auth`

| Method | Endpoint | Chức năng | Status |
|--------|----------|-----------|---------|
| POST | `/login` | Đăng nhập với username/password | ✅ |
| GET | `/me` | Lấy thông tin user hiện tại | ✅ |
| POST | `/logout` | Đăng xuất | ✅ |

**DTOs:**
- `LoginRequestDto`: Username, Password
- `LoginResponseDto`: Token, UserId, Username, Roles, ExpiresAt
- `UserInfoDto`: UserId, Username, Roles

---

## 📁 **2. CollectionsController** - Collection Management
**Base Route:** `/api/collections`

| Method | Endpoint | Chức năng | Status |
|--------|----------|-----------|---------|
| GET | `/` | Lấy danh sách collections (pagination, search) | ✅ |
| GET | `/{id}` | Lấy collection theo ID | ✅ |
| POST | `/` | Tạo collection mới | ✅ |
| PUT | `/{id}` | Cập nhật collection | ✅ |
| DELETE | `/{id}` | Xóa collection | ✅ |
| POST | `/{id}/scan` | Quét collection để tìm images | ✅ |
| GET | `/search` | Tìm kiếm collections | ✅ |

**DTOs:**
- `CreateCollectionRequest`: Name, Path, Type, ThumbnailWidth, ThumbnailHeight, CacheWidth, CacheHeight, Quality, EnableCache, AutoScan
- `UpdateCollectionRequest`: Name, Path, Settings
- `CollectionSettingsRequest`: ThumbnailWidth, ThumbnailHeight, CacheWidth, CacheHeight, Quality, EnableCache, AutoScan

---

## 🖼️ **3. ImagesController** - Image Management
**Base Route:** `/api/images`

| Method | Endpoint | Chức năng | Status |
|--------|----------|-----------|---------|
| GET | `/random` | Lấy ảnh ngẫu nhiên | ✅ |
| GET | `/collection/{collectionId}/random` | Lấy ảnh ngẫu nhiên trong collection | ✅ |
| GET | `/collection/{collectionId}` | Lấy danh sách ảnh trong collection (pagination) | ✅ |
| GET | `/{id}` | Lấy ảnh theo ID | ✅ |
| GET | `/{id}/file` | Lấy file ảnh (có thể resize) | ✅ |
| GET | `/{id}/thumbnail` | Lấy thumbnail ảnh | ✅ |
| DELETE | `/{id}` | Xóa ảnh | ✅ |

---

## 📊 **4. StatisticsController** - Statistics & Analytics
**Base Route:** `/api/statistics`

| Method | Endpoint | Chức năng | Status |
|--------|----------|-----------|---------|
| GET | `/overall` | Lấy thống kê tổng quan hệ thống | ✅ |
| GET | `/collections/{collectionId}` | Lấy thống kê collection cụ thể | ✅ |
| GET | `/images/{imageId}` | Lấy thống kê ảnh cụ thể | ✅ |

**DTOs:**
- `SystemStatisticsDto`: TotalCollections, TotalImages, TotalSize, TotalCacheSize, TotalViewSessions, TotalViewTime, AverageImagesPerCollection, AverageViewTimePerSession
- `CollectionStatisticsDto`: CollectionId, ViewCount, TotalViewTime, SearchCount, LastViewed, LastSearched, AverageViewTime, TotalImages, TotalSize, AverageFileSize, CachedImages, CachePercentage, PopularImages
- `ImageStatisticsDto`: ImageId, ViewCount, TotalViewTime, LastViewed, AverageViewTime, FileSize, Format, Dimensions

---

## 🏷️ **5. TagsController** - Tag Management
**Base Route:** `/api/tags`

| Method | Endpoint | Chức năng | Status |
|--------|----------|-----------|---------|
| GET | `/` | Lấy tất cả tags | ✅ |
| GET | `/collections/{collectionId}` | Lấy tags của collection | ✅ |
| POST | `/collections/{collectionId}` | Thêm tag vào collection | ✅ |
| PUT | `/{id}` | Cập nhật tag | ✅ |
| DELETE | `/{id}` | Xóa tag | ✅ |

**DTOs:**
- `TagDto`: Id, Name, Description, Color, CreatedAt, UpdatedAt
- `TagColorDto`: R, G, B
- `CollectionTagDto`: Tag, Count, AddedBy, AddedAt
- `AddTagToCollectionDto`: TagName, Description, Color
- `UpdateTagDto`: Name, Description, Color

---

## 🖼️ **6. ThumbnailsController** - Advanced Thumbnail Operations
**Base Route:** `/api/thumbnails`

| Method | Endpoint | Chức năng | Status |
|--------|----------|-----------|---------|
| POST | `/collections/{collectionId}/generate` | Tạo thumbnail cho collection | ✅ |
| GET | `/collections/{collectionId}` | Lấy thumbnail collection (có thể resize) | ✅ |
| POST | `/collections/batch-regenerate` | Tạo lại thumbnail cho nhiều collections | ✅ |
| DELETE | `/collections/{collectionId}` | Xóa thumbnail collection | ✅ |

**DTOs:**
- `ThumbnailGenerationResponse`: CollectionId, ThumbnailPath, GeneratedAt
- `BatchThumbnailRequest`: CollectionIds
- `BatchThumbnailResult`: Total, Success, Failed, FailedCollections, Errors

---

## 🔄 **7. BulkController** - Bulk Operations
**Base Route:** `/api/bulk`

| Method | Endpoint | Chức năng | Status |
|--------|----------|-----------|---------|
| POST | `/collections` | Bulk add collections từ parent directory | ✅ |

**DTOs:**
- `BulkAddCollectionsRequest`: ParentPath, CollectionPrefix, IncludeSubfolders, AutoAdd, ThumbnailWidth, ThumbnailHeight, CacheWidth, CacheHeight, EnableCache, AutoScan
- `BulkOperationResult`: TotalProcessed, SuccessCount, SkippedCount, ErrorCount, Results, Errors
- `BulkCollectionResult`: Name, Path, Type, Status, Message, CollectionId

---

## 💾 **8. CacheController** - Cache Management
**Base Route:** `/api/cache`

| Method | Endpoint | Chức năng | Status |
|--------|----------|-----------|---------|
| GET | `/folders` | Lấy danh sách cache folders | ✅ |
| POST | `/folders` | Tạo cache folder mới | ✅ |
| PUT | `/folders/{id}` | Cập nhật cache folder | ✅ |
| DELETE | `/folders/{id}` | Xóa cache folder | ✅ |
| GET | `/statistics` | Lấy thống kê cache | ✅ |
| POST | `/clear` | Xóa cache | ✅ |
| POST | `/regenerate` | Tạo lại cache | ✅ |

**DTOs:**
- `CacheFolderDto`: Id, Name, Path, MaxSizeBytes, CurrentSizeBytes, CreatedAt, UpdatedAt
- `CacheStatisticsDto`: TotalImages, CachedImages, ExpiredImages, TotalCacheSize, AvailableSpace, CacheHitRate, LastUpdated

---

## 📦 **9. CompressedFilesController** - Compressed File Support
**Base Route:** `/api/compressed`

| Method | Endpoint | Chức năng | Status |
|--------|----------|-----------|---------|
| GET | `/formats` | Lấy danh sách formats hỗ trợ | ✅ |
| POST | `/extract` | Giải nén file | ✅ |
| GET | `/list/{fileId}` | Lấy danh sách files trong archive | ✅ |
| GET | `/preview/{fileId}` | Preview file trong archive | ✅ |

---

## 🎲 **10. RandomController** - Random Selection
**Base Route:** `/api/random`

| Method | Endpoint | Chức năng | Status |
|--------|----------|-----------|---------|
| GET | `/image` | Lấy ảnh ngẫu nhiên | ✅ |
| GET | `/collection` | Lấy collection ngẫu nhiên | ✅ |
| GET | `/images/{count}` | Lấy nhiều ảnh ngẫu nhiên | ✅ |

---

## ⚙️ **11. JobsController** - Background Jobs
**Base Route:** `/api/jobs`

| Method | Endpoint | Chức năng | Status |
|--------|----------|-----------|---------|
| GET | `/` | Lấy danh sách jobs | ✅ |
| GET | `/{id}` | Lấy job theo ID | ✅ |
| POST | `/start` | Bắt đầu job | ✅ |
| POST | `/{id}/cancel` | Hủy job | ✅ |
| DELETE | `/{id}` | Xóa job | ✅ |

---

## 🏥 **12. HealthController** - Health Check
**Base Route:** `/health`

| Method | Endpoint | Chức năng | Status |
|--------|----------|-----------|---------|
| GET | `/` | Health check | ✅ |

---

## 🚀 **SETUP DATABASE ĐÚNG QUY TRÌNH**

### **Bước 1: Tạo Collection từ Folder thực tế**
```bash
POST /api/bulk/collections
Content-Type: application/json

{
  "parentPath": "L:\\EMedia\\AI_Generated\\AiASAG",
  "collectionPrefix": "",
  "includeSubfolders": true,
  "autoAdd": true,
  "thumbnailWidth": 300,
  "thumbnailHeight": 300,
  "cacheWidth": 1920,
  "cacheHeight": 1080,
  "enableCache": true,
  "autoScan": true
}
```

### **Bước 2: Scan Collections để tìm Images**
```bash
POST /api/collections/{collectionId}/scan
```

### **Bước 3: Verify Data**
```bash
GET /api/collections
GET /api/statistics/overall
```

---

## 📊 **STATUS SUMMARY**

| Controller | Endpoints | Status | Notes |
|------------|-----------|---------|-------|
| AuthController | 3 | ✅ Complete | JWT Authentication |
| CollectionsController | 7 | ✅ Complete | CRUD + Search + Scan |
| ImagesController | 7 | ✅ Complete | CRUD + Random + File serving |
| StatisticsController | 3 | ✅ Complete | System + Collection + Image stats |
| TagsController | 5 | ✅ Complete | Tag management |
| ThumbnailsController | 4 | ✅ Complete | Advanced thumbnail operations |
| BulkController | 1 | ✅ Complete | Bulk add collections |
| CacheController | 7 | ✅ Complete | Cache management |
| CompressedFilesController | 4 | ✅ Complete | ZIP/RAR/7Z support |
| RandomController | 3 | ✅ Complete | Random selection |
| JobsController | 5 | ✅ Complete | Background jobs |
| HealthController | 1 | ✅ Complete | Health check |

**TOTAL: 50+ Endpoints - 100% Complete** ✅

---

## 🎯 **NEXT STEPS**

1. **Setup Database với Bulk API**
2. **Test Performance với Real Data**
3. **Verify All Endpoints hoạt động**
4. **Document Performance Results**
