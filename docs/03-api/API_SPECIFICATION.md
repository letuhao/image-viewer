# Image Viewer System - API Specification

## Tổng quan API

### Base URL
```
Production: https://api.imageviewer.com/v1
Development: https://localhost:7001/api/v1
```

### Authentication
- **JWT Bearer Token**: Required for all endpoints except public ones
- **API Key**: Alternative authentication for external services
- **Rate Limiting**: 1000 requests per hour per user, 10000 requests per hour per API key

### Response Format
All API responses follow a consistent format:

```json
{
  "success": true,
  "data": { ... },
  "message": "Operation completed successfully",
  "timestamp": "2024-01-01T00:00:00Z",
  "requestId": "req_123456789"
}
```

### Error Format
```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid input data",
    "details": [
      {
        "field": "name",
        "message": "Name is required"
      }
    ]
  },
  "timestamp": "2024-01-01T00:00:00Z",
  "requestId": "req_123456789"
}
```

## Collections API

### Get Collections
```http
GET /collections
```

**Query Parameters:**
- `page` (int, optional): Page number (default: 1)
- `limit` (int, optional): Items per page (default: 20, max: 100)
- `sortBy` (string, optional): Sort field (name, createdAt, updatedAt, imageCount)
- `sortOrder` (string, optional): Sort order (asc, desc)
- `filter` (string, optional): Filter by name or tags
- `type` (string, optional): Filter by collection type (folder, zip, sevenZip, rar, tar)
- `hasImages` (boolean, optional): Filter collections with/without images
- `tags` (string, optional): Comma-separated list of tags to filter by

**Response:**
```json
{
  "success": true,
  "data": {
    "collections": [
      {
        "id": "col_123456789",
        "name": "My Manga Collection",
        "path": "D:\\Manga\\Collection1",
        "type": "folder",
        "settings": {
          "totalImages": 1500,
          "lastScanned": "2024-01-01T00:00:00Z",
          "autoScan": true,
          "thumbnailQuality": 80
        },
        "statistics": {
          "viewCount": 250,
          "totalViewTime": 3600,
          "searchCount": 45,
          "lastViewed": "2024-01-01T00:00:00Z",
          "lastSearched": "2024-01-01T00:00:00Z"
        },
        "tags": [
          {
            "tag": "manga",
            "count": 1,
            "addedBy": "user123"
          }
        ],
        "cacheStatus": {
          "hasCache": true,
          "cachedImages": 1200,
          "totalImages": 1500,
          "cachePercentage": 80,
          "lastGenerated": "2024-01-01T00:00:00Z"
        },
        "createdAt": "2024-01-01T00:00:00Z",
        "updatedAt": "2024-01-01T00:00:00Z"
      }
    ],
    "pagination": {
      "page": 1,
      "limit": 20,
      "total": 150,
      "totalPages": 8,
      "hasNext": true,
      "hasPrevious": false
    }
  }
}
```

### Get Collection by ID
```http
GET /collections/{id}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "col_123456789",
    "name": "My Manga Collection",
    "path": "D:\\Manga\\Collection1",
    "type": "folder",
    "settings": { ... },
    "statistics": { ... },
    "tags": [ ... ],
    "cacheStatus": { ... },
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-01T00:00:00Z"
  }
}
```

### Create Collection
```http
POST /collections
```

**Request Body:**
```json
{
  "name": "New Collection",
  "path": "D:\\Images\\NewCollection",
  "type": "folder",
  "settings": {
    "autoScan": true,
    "thumbnailQuality": 80,
    "cacheEnabled": true
  },
  "tags": ["manga", "comics"]
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "col_123456789",
    "name": "New Collection",
    "path": "D:\\Images\\NewCollection",
    "type": "folder",
    "settings": { ... },
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-01T00:00:00Z"
  },
  "message": "Collection created successfully. Scanning started in background."
}
```

### Update Collection
```http
PUT /collections/{id}
```

**Request Body:**
```json
{
  "name": "Updated Collection Name",
  "settings": {
    "thumbnailQuality": 90,
    "cacheEnabled": true
  }
}
```

### Delete Collection
```http
DELETE /collections/{id}
```

**Response:**
```json
{
  "success": true,
  "message": "Collection deleted successfully"
}
```

### Scan Collection
```http
POST /collections/{id}/scan
```

**Request Body:**
```json
{
  "forceRescan": false,
  "generateThumbnails": true,
  "generateCache": false
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "jobId": "job_123456789",
    "status": "queued",
    "estimatedDuration": "5 minutes"
  },
  "message": "Collection scan started"
}
```

### Get Collection Images
```http
GET /collections/{id}/images
```

**Query Parameters:**
- `page` (int, optional): Page number (default: 1)
- `limit` (int, optional): Items per page (default: 50, max: 200)
- `sortBy` (string, optional): Sort field (filename, createdAt, fileSize, width, height)
- `sortOrder` (string, optional): Sort order (asc, desc)
- `search` (string, optional): Search by filename
- `format` (string, optional): Filter by image format (jpg, png, gif, webp, etc.)
- `minWidth` (int, optional): Minimum width filter
- `maxWidth` (int, optional): Maximum width filter
- `minHeight` (int, optional): Minimum height filter
- `maxHeight` (int, optional): Maximum height filter

**Response:**
```json
{
  "success": true,
  "data": {
    "images": [
      {
        "id": "img_123456789",
        "filename": "page_001.jpg",
        "relativePath": "chapter1/page_001.jpg",
        "fileSize": 2048576,
        "width": 1920,
        "height": 1080,
        "format": "jpeg",
        "metadata": {
          "quality": 95,
          "colorSpace": "RGB",
          "createdAt": "2024-01-01T00:00:00Z",
          "modifiedAt": "2024-01-01T00:00:00Z"
        },
        "cacheInfo": {
          "hasCache": true,
          "cachePath": "cache/col_123456789/img_123456789_q85_jpeg.jpg",
          "thumbnailPath": "cache/col_123456789/img_123456789_thumb.jpg",
          "cacheSize": 1024288,
          "quality": 85,
          "format": "jpeg",
          "cachedAt": "2024-01-01T00:00:00Z"
        },
        "createdAt": "2024-01-01T00:00:00Z"
      }
    ],
    "pagination": {
      "page": 1,
      "limit": 50,
      "total": 1500,
      "totalPages": 30,
      "hasNext": true,
      "hasPrevious": false
    }
  }
}
```

## Images API

### Get Image by ID
```http
GET /images/{id}
```

**Query Parameters:**
- `collectionId` (string, required): Collection ID

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "img_123456789",
    "collectionId": "col_123456789",
    "filename": "page_001.jpg",
    "relativePath": "chapter1/page_001.jpg",
    "fileSize": 2048576,
    "width": 1920,
    "height": 1080,
    "format": "jpeg",
    "metadata": { ... },
    "cacheInfo": { ... },
    "createdAt": "2024-01-01T00:00:00Z"
  }
}
```

### Get Image File
```http
GET /images/{collectionId}/{imageId}/file
```

**Query Parameters:**
- `width` (int, optional): Resize width
- `height` (int, optional): Resize height
- `quality` (int, optional): JPEG quality (1-100, default: 90)
- `format` (string, optional): Output format (jpeg, png, webp, original)
- `fit` (string, optional): Resize fit mode (contain, cover, fill, inside, outside)

**Response:**
- **Content-Type**: `image/jpeg`, `image/png`, `image/webp`, etc.
- **Content-Length**: File size in bytes
- **Cache-Control**: `public, max-age=31536000` (for cached images)
- **X-Cache**: `HIT`, `MISS`, `HIT-PROCESSED`
- **X-Processing**: `resized-from-cache`, `fallback-from-original`

### Get Thumbnail
```http
GET /images/{collectionId}/{imageId}/thumbnail
```

**Query Parameters:**
- `width` (int, optional): Thumbnail width (default: 300)
- `height` (int, optional): Thumbnail height (default: 300)
- `quality` (int, optional): JPEG quality (1-100, default: 80)

**Response:**
- **Content-Type**: `image/jpeg`
- **Content-Length**: Thumbnail size in bytes
- **Cache-Control**: `public, max-age=31536000`

### Get Batch Thumbnails
```http
GET /images/{collectionId}/batch-thumbnails
```

**Query Parameters:**
- `ids` (string, required): Comma-separated image IDs
- `width` (int, optional): Thumbnail width (default: 300)
- `height` (int, optional): Thumbnail height (default: 300)
- `quality` (int, optional): JPEG quality (1-100, default: 80)

**Response:**
```json
{
  "success": true,
  "data": {
    "thumbnails": [
      {
        "id": "img_123456789",
        "thumbnail": "base64_encoded_image_data",
        "filename": "page_001.jpg"
      }
    ],
    "requested": 10,
    "found": 8
  }
}
```

### Navigate Images
```http
GET /images/{collectionId}/{imageId}/navigate
```

**Query Parameters:**
- `direction` (string, required): Navigation direction (next, previous)

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "img_123456790",
    "filename": "page_002.jpg",
    "relativePath": "chapter1/page_002.jpg",
    "fileSize": 2048576,
    "width": 1920,
    "height": 1080,
    "format": "jpeg",
    "metadata": { ... },
    "cacheInfo": { ... },
    "createdAt": "2024-01-01T00:00:00Z"
  }
}
```

### Get Random Image
```http
GET /images/{collectionId}/random
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "img_123456789",
    "filename": "page_001.jpg",
    "relativePath": "chapter1/page_001.jpg",
    "fileSize": 2048576,
    "width": 1920,
    "height": 1080,
    "format": "jpeg",
    "metadata": { ... },
    "cacheInfo": { ... },
    "createdAt": "2024-01-01T00:00:00Z"
  }
}
```

### Search Images
```http
GET /images/{collectionId}/search
```

**Query Parameters:**
- `q` (string, required): Search query
- `page` (int, optional): Page number (default: 1)
- `limit` (int, optional): Items per page (default: 50, max: 200)
- `sortBy` (string, optional): Sort field (relevance, filename, createdAt, fileSize)
- `sortOrder` (string, optional): Sort order (asc, desc)

**Response:**
```json
{
  "success": true,
  "data": {
    "images": [ ... ],
    "pagination": { ... },
    "searchMetadata": {
      "query": "page_001",
      "totalResults": 25,
      "searchTime": "0.045s"
    }
  }
}
```

## Cache API

### Get Cache Statistics
```http
GET /cache/statistics
```

**Response:**
```json
{
  "success": true,
  "data": {
    "summary": {
      "totalCollections": 150,
      "collectionsWithCache": 120,
      "totalImages": 150000,
      "cachedImages": 120000,
      "totalCacheSize": 10737418240,
      "cachePercentage": 80
    },
    "cacheFolders": [
      {
        "id": "cf_123456789",
        "name": "Primary Cache",
        "path": "D:\\Cache",
        "priority": 1,
        "maxSize": 10737418240,
        "currentSize": 8589934592,
        "fileCount": 120000,
        "isActive": true,
        "usagePercentage": 80
      }
    ],
    "performance": {
      "averageCacheHitRate": 0.85,
      "averageResponseTime": "0.045s",
      "cacheGenerationRate": "150 images/minute"
    }
  }
}
```

### Clear Cache
```http
DELETE /cache
```

**Query Parameters:**
- `collectionId` (string, optional): Clear cache for specific collection
- `type` (string, optional): Cache type to clear (thumbnails, images, all)

**Response:**
```json
{
  "success": true,
  "data": {
    "clearedItems": 120000,
    "freedSpace": 8589934592,
    "clearedCollections": 120
  },
  "message": "Cache cleared successfully"
}
```

### Generate Cache
```http
POST /cache/generate
```

**Request Body:**
```json
{
  "collectionIds": ["col_123456789", "col_123456790"],
  "options": {
    "quality": 85,
    "format": "jpeg",
    "overwrite": false,
    "generateThumbnails": true,
    "maxConcurrency": 5
  }
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "jobId": "job_123456789",
    "status": "queued",
    "estimatedDuration": "30 minutes",
    "totalImages": 3000,
    "estimatedProgress": "0%"
  },
  "message": "Cache generation started"
}
```

## Background Jobs API

### Get Job Status
```http
GET /jobs/{jobId}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "jobId": "job_123456789",
    "type": "cache-generation",
    "status": "running",
    "progress": {
      "total": 3000,
      "completed": 1500,
      "percentage": 50,
      "currentItem": "page_0750.jpg",
      "errors": []
    },
    "startedAt": "2024-01-01T00:00:00Z",
    "estimatedCompletion": "2024-01-01T00:15:00Z"
  }
}
```

### Get All Jobs
```http
GET /jobs
```

**Query Parameters:**
- `status` (string, optional): Filter by status (queued, running, completed, failed, cancelled)
- `type` (string, optional): Filter by job type
- `page` (int, optional): Page number (default: 1)
- `limit` (int, optional): Items per page (default: 20, max: 100)

**Response:**
```json
{
  "success": true,
  "data": {
    "jobs": [
      {
        "jobId": "job_123456789",
        "type": "cache-generation",
        "status": "running",
        "progress": { ... },
        "startedAt": "2024-01-01T00:00:00Z",
        "estimatedCompletion": "2024-01-01T00:15:00Z"
      }
    ],
    "pagination": { ... }
  }
}
```

### Cancel Job
```http
POST /jobs/{jobId}/cancel
```

**Response:**
```json
{
  "success": true,
  "message": "Job cancelled successfully"
}
```

## Statistics API

### Get Collection Statistics
```http
GET /statistics/collections/{id}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "collectionId": "col_123456789",
    "viewCount": 250,
    "totalViewTime": 3600,
    "searchCount": 45,
    "lastViewed": "2024-01-01T00:00:00Z",
    "lastSearched": "2024-01-01T00:00:00Z",
    "averageViewTime": 14.4,
    "popularImages": [
      {
        "id": "img_123456789",
        "filename": "page_001.jpg",
        "viewCount": 25
      }
    ]
  }
}
```

### Track View
```http
POST /statistics/collections/{id}/view
```

**Request Body:**
```json
{
  "sessionId": "session_123456789",
  "imageId": "img_123456789",
  "viewTimeSeconds": 30
}
```

### Track Search
```http
POST /statistics/collections/{id}/search
```

**Request Body:**
```json
{
  "query": "page_001",
  "resultCount": 25,
  "searchTime": 0.045
}
```

### Get Popular Collections
```http
GET /statistics/popular
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 10, max: 100)
- `timeframe` (string, optional): Timeframe (day, week, month, year, all)

**Response:**
```json
{
  "success": true,
  "data": {
    "collections": [
      {
        "id": "col_123456789",
        "name": "My Manga Collection",
        "viewCount": 250,
        "totalViewTime": 3600,
        "searchCount": 45,
        "rank": 1
      }
    ],
    "timeframe": "month",
    "generatedAt": "2024-01-01T00:00:00Z"
  }
}
```

### Get Analytics
```http
GET /statistics/analytics
```

**Response:**
```json
{
  "success": true,
  "data": {
    "summary": {
      "totalCollections": 150,
      "totalImages": 150000,
      "totalViews": 25000,
      "totalViewTime": 360000,
      "totalSearches": 5000,
      "averageSessionTime": 14.4,
      "cacheHitRate": 0.85
    },
    "trends": {
      "dailyViews": [
        {
          "date": "2024-01-01",
          "views": 250,
          "uniqueUsers": 45
        }
      ],
      "popularTags": [
        {
          "tag": "manga",
          "count": 50,
          "trend": "up"
        }
      ]
    },
    "performance": {
      "averageResponseTime": "0.045s",
      "cacheHitRate": 0.85,
      "errorRate": 0.001
    }
  }
}
```

## Tags API

### Get Collection Tags
```http
GET /collections/{id}/tags
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "tag": "manga",
      "count": 1,
      "addedBy": "user123",
      "addedAt": "2024-01-01T00:00:00Z"
    }
  ]
}
```

### Add Tag to Collection
```http
POST /collections/{id}/tags
```

**Request Body:**
```json
{
  "tag": "comics",
  "addedBy": "user123"
}
```

### Remove Tag from Collection
```http
DELETE /collections/{id}/tags/{tag}
```

**Query Parameters:**
- `addedBy` (string, optional): Only remove tags added by specific user

### Search Tags
```http
GET /tags/search
```

**Query Parameters:**
- `q` (string, required): Search query
- `limit` (int, optional): Number of results (default: 10, max: 100)

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "tag": "manga",
      "count": 50,
      "collections": 25
    }
  ]
}
```

### Get Popular Tags
```http
GET /tags/popular
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 20, max: 100)
- `timeframe` (string, optional): Timeframe (day, week, month, year, all)

## Health Check API

### Health Check
```http
GET /health
```

**Response:**
```json
{
  "success": true,
  "data": {
    "status": "healthy",
    "timestamp": "2024-01-01T00:00:00Z",
    "version": "1.0.0",
    "uptime": "7d 12h 30m 45s",
    "services": {
      "database": {
        "status": "healthy",
        "responseTime": "0.012s"
      },
      "cache": {
        "status": "healthy",
        "responseTime": "0.003s"
      },
      "storage": {
        "status": "healthy",
        "responseTime": "0.045s"
      }
    }
  }
}
```

## Error Codes

### HTTP Status Codes
- `200 OK`: Success
- `201 Created`: Resource created successfully
- `202 Accepted`: Request accepted for processing
- `400 Bad Request`: Invalid request data
- `401 Unauthorized`: Authentication required
- `403 Forbidden`: Insufficient permissions
- `404 Not Found`: Resource not found
- `409 Conflict`: Resource conflict
- `422 Unprocessable Entity`: Validation error
- `429 Too Many Requests`: Rate limit exceeded
- `500 Internal Server Error`: Server error
- `503 Service Unavailable`: Service temporarily unavailable

### Error Codes
- `VALIDATION_ERROR`: Input validation failed
- `RESOURCE_NOT_FOUND`: Requested resource not found
- `DUPLICATE_RESOURCE`: Resource already exists
- `INSUFFICIENT_PERMISSIONS`: User lacks required permissions
- `RATE_LIMIT_EXCEEDED`: Too many requests
- `STORAGE_ERROR`: File system or storage error
- `PROCESSING_ERROR`: Image processing error
- `CACHE_ERROR`: Cache operation error
- `DATABASE_ERROR`: Database operation error
- `EXTERNAL_SERVICE_ERROR`: External service error

## Rate Limiting

### Limits
- **Authenticated Users**: 1000 requests per hour
- **API Keys**: 10000 requests per hour
- **Anonymous Users**: 100 requests per hour

### Headers
- `X-RateLimit-Limit`: Request limit per hour
- `X-RateLimit-Remaining`: Remaining requests in current hour
- `X-RateLimit-Reset`: Time when rate limit resets

### Exceeded Response
```json
{
  "success": false,
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Rate limit exceeded. Try again later.",
    "retryAfter": 3600
  },
  "timestamp": "2024-01-01T00:00:00Z",
  "requestId": "req_123456789"
}
```

## WebSocket Events

### Connection
```javascript
const ws = new WebSocket('wss://api.imageviewer.com/v1/ws');
```

### Events
- `job.progress`: Background job progress updates
- `collection.scan.completed`: Collection scan completed
- `cache.generation.completed`: Cache generation completed
- `image.processed`: Image processing completed
- `error`: Error occurred

### Example Event
```json
{
  "type": "job.progress",
  "data": {
    "jobId": "job_123456789",
    "progress": {
      "total": 3000,
      "completed": 1500,
      "percentage": 50,
      "currentItem": "page_0750.jpg"
    }
  },
  "timestamp": "2024-01-01T00:00:00Z"
}
```

## SDK Examples

### JavaScript/TypeScript
```typescript
import { ImageViewerClient } from '@imageviewer/sdk';

const client = new ImageViewerClient({
  baseUrl: 'https://api.imageviewer.com/v1',
  apiKey: 'your-api-key'
});

// Get collections
const collections = await client.collections.getAll({
  page: 1,
  limit: 20,
  sortBy: 'name',
  sortOrder: 'asc'
});

// Get image file
const imageBlob = await client.images.getFile('col_123', 'img_123', {
  width: 1920,
  height: 1080,
  quality: 90
});

// Generate cache
const job = await client.cache.generate({
  collectionIds: ['col_123'],
  options: {
    quality: 85,
    format: 'jpeg',
    overwrite: false
  }
});
```

### C#
```csharp
using ImageViewer.Sdk;

var client = new ImageViewerClient("https://api.imageviewer.com/v1", "your-api-key");

// Get collections
var collections = await client.Collections.GetAllAsync(new GetCollectionsRequest
{
    Page = 1,
    Limit = 20,
    SortBy = "name",
    SortOrder = "asc"
});

// Get image file
var imageStream = await client.Images.GetFileAsync("col_123", "img_123", new GetImageFileRequest
{
    Width = 1920,
    Height = 1080,
    Quality = 90
});

// Generate cache
var job = await client.Cache.GenerateAsync(new GenerateCacheRequest
{
    CollectionIds = new[] { "col_123" },
    Options = new CacheOptions
    {
        Quality = 85,
        Format = "jpeg",
        Overwrite = false
    }
});
```

## Missing Features API

### Content Moderation API

#### Flag Content
```http
POST /api/v1/moderation/flag
Authorization: Bearer {token}
Content-Type: application/json

{
  "contentId": "col_123",
  "contentType": "collection",
  "reason": "inappropriate_content",
  "details": "Contains explicit material",
  "category": "adult_content"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "moderationId": "mod_123",
    "status": "flagged",
    "flaggedAt": "2024-01-01T00:00:00Z",
    "flaggedBy": "user_123"
  }
}
```

#### Moderate Content
```http
POST /api/v1/moderation/{moderationId}/moderate
Authorization: Bearer {token}
Content-Type: application/json

{
  "status": "approved",
  "notes": "Content is appropriate",
  "action": "approve"
}
```

#### Get Moderation Queue
```http
GET /api/v1/moderation/queue?status=pending&page=1&limit=20
Authorization: Bearer {token}
```

### Copyright Management API

#### Claim Ownership
```http
POST /api/v1/copyright/claim
Authorization: Bearer {token}
Content-Type: application/json

{
  "contentId": "col_123",
  "contentType": "collection",
  "verificationMethod": "email_verification",
  "ownershipProof": "proof_document_url"
}
```

#### Report DMCA
```http
POST /api/v1/copyright/dmca
Authorization: Bearer {token}
Content-Type: application/json

{
  "contentId": "col_123",
  "contentType": "collection",
  "reportId": "dmca_123",
  "reason": "copyright_infringement",
  "description": "Unauthorized use of copyrighted material"
}
```

#### Grant Permission
```http
POST /api/v1/copyright/permissions
Authorization: Bearer {token}
Content-Type: application/json

{
  "contentId": "col_123",
  "userId": "user_456",
  "permission": "read",
  "expiresAt": "2024-12-31T23:59:59Z"
}
```

### User Security API

#### Enable Two-Factor Authentication
```http
POST /api/v1/security/2fa/enable
Authorization: Bearer {token}
Content-Type: application/json

{
  "method": "totp",
  "phoneNumber": "+1234567890"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "qrCode": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA...",
    "backupCodes": ["12345678", "87654321", "11223344"],
    "secretKey": "JBSWY3DPEHPK3PXP"
  }
}
```

#### Verify Two-Factor Authentication
```http
POST /api/v1/security/2fa/verify
Authorization: Bearer {token}
Content-Type: application/json

{
  "code": "123456",
  "method": "totp"
}
```

#### Manage Devices
```http
GET /api/v1/security/devices
Authorization: Bearer {token}
```

```http
DELETE /api/v1/security/devices/{deviceId}
Authorization: Bearer {token}
```

#### Get Security Events
```http
GET /api/v1/security/events?page=1&limit=20&type=login
Authorization: Bearer {token}
```

### System Health API

#### Get System Health
```http
GET /api/v1/health
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "overallStatus": "healthy",
    "components": [
      {
        "name": "database",
        "status": "healthy",
        "responseTime": 15,
        "lastChecked": "2024-01-01T00:00:00Z"
      },
      {
        "name": "storage",
        "status": "warning",
        "responseTime": 250,
        "lastChecked": "2024-01-01T00:00:00Z",
        "alerts": [
          {
            "id": "alert_123",
            "severity": "warning",
            "message": "High disk usage: 85%"
          }
        ]
      }
    ],
    "metrics": {
      "cpuUsage": 45.2,
      "memoryUsage": 67.8,
      "diskUsage": 85.1,
      "activeUsers": 1250
    }
  }
}
```

#### Get Component Health
```http
GET /api/v1/health/{component}
Authorization: Bearer {token}
```

#### Resolve Health Alert
```http
POST /api/v1/health/alerts/{alertId}/resolve
Authorization: Bearer {token}
Content-Type: application/json

{
  "resolution": "Disk space freed up",
  "notes": "Cleaned up temporary files"
}
```

### Notification Templates API

#### Get Notification Templates
```http
GET /api/v1/notifications/templates?type=email&category=system
Authorization: Bearer {token}
```

#### Create Notification Template
```http
POST /api/v1/notifications/templates
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Welcome Email",
  "description": "Welcome email for new users",
  "type": "email",
  "category": "user",
  "language": "en",
  "subject": "Welcome to ImageViewer!",
  "content": "Hello {{userName}}, welcome to ImageViewer!",
  "htmlContent": "<h1>Welcome {{userName}}!</h1><p>Welcome to ImageViewer!</p>",
  "variables": [
    {
      "name": "userName",
      "type": "string",
      "required": true,
      "defaultValue": "User"
    }
  ]
}
```

#### Update Notification Template
```http
PUT /api/v1/notifications/templates/{templateId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Welcome Email Updated",
  "content": "Hello {{userName}}, welcome to ImageViewer! We're excited to have you."
}
```

#### Activate/Deactivate Template
```http
POST /api/v1/notifications/templates/{templateId}/activate
Authorization: Bearer {token}
```

```http
POST /api/v1/notifications/templates/{templateId}/deactivate
Authorization: Bearer {token}
```

### File Versioning API

#### Get File Versions
```http
GET /api/v1/files/{fileId}/versions
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "fileId": "file_123",
    "versions": [
      {
        "version": 3,
        "versionName": "Latest",
        "isActive": true,
        "createdAt": "2024-01-01T00:00:00Z",
        "createdBy": "user_123",
        "fileSize": 2048576,
        "changes": "Updated metadata"
      },
      {
        "version": 2,
        "versionName": "Previous",
        "isActive": false,
        "createdAt": "2023-12-31T00:00:00Z",
        "createdBy": "user_123",
        "fileSize": 2048000,
        "changes": "Fixed image quality"
      }
    ]
  }
}
```

#### Create File Version
```http
POST /api/v1/files/{fileId}/versions
Authorization: Bearer {token}
Content-Type: multipart/form-data

{
  "file": "binary_file_data",
  "versionName": "Updated Version",
  "changes": "Updated image processing"
}
```

#### Activate File Version
```http
POST /api/v1/files/{fileId}/versions/{version}/activate
Authorization: Bearer {token}
```

#### Delete File Version
```http
DELETE /api/v1/files/{fileId}/versions/{version}
Authorization: Bearer {token}
```

#### Download File Version
```http
GET /api/v1/files/{fileId}/versions/{version}/download
Authorization: Bearer {token}
```

### User Groups API

#### Get User Groups
```http
GET /api/v1/groups?type=public&category=interest&page=1&limit=20
Authorization: Bearer {token}
```

#### Create User Group
```http
POST /api/v1/groups
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Photography Enthusiasts",
  "description": "A group for photography lovers",
  "type": "public",
  "category": "interest",
  "permissions": ["read", "comment"],
  "settings": {
    "allowMemberInvites": true,
    "requireApproval": false,
    "maxMembers": 1000
  }
}
```

#### Join User Group
```http
POST /api/v1/groups/{groupId}/join
Authorization: Bearer {token}
```

#### Leave User Group
```http
DELETE /api/v1/groups/{groupId}/leave
Authorization: Bearer {token}
```

#### Update Member Role
```http
PUT /api/v1/groups/{groupId}/members/{userId}/role
Authorization: Bearer {token}
Content-Type: application/json

{
  "role": "moderator"
}
```

#### Ban User from Group
```http
POST /api/v1/groups/{groupId}/ban
Authorization: Bearer {token}
Content-Type: application/json

{
  "userId": "user_456",
  "reason": "Spam behavior"
}
```

#### Get Group Members
```http
GET /api/v1/groups/{groupId}/members?role=admin&page=1&limit=20
Authorization: Bearer {token}
```

### Advanced Search API

#### Semantic Search
```http
POST /api/v1/search/semantic
Authorization: Bearer {token}
Content-Type: application/json

{
  "query": "sunset over mountains",
  "type": "image",
  "limit": 20,
  "filters": {
    "dateRange": {
      "from": "2023-01-01",
      "to": "2024-01-01"
    },
    "tags": ["nature", "landscape"]
  }
}
```

#### Visual Search
```http
POST /api/v1/search/visual
Authorization: Bearer {token}
Content-Type: multipart/form-data

{
  "image": "binary_image_data",
  "limit": 20,
  "similarityThreshold": 0.8
}
```

#### Get Search Suggestions
```http
GET /api/v1/search/suggestions?q=sunset&limit=10
Authorization: Bearer {token}
```

### Analytics API

#### Get User Analytics
```http
GET /api/v1/analytics/user?userId=user_123&period=30d
Authorization: Bearer {token}
```

#### Get Content Popularity
```http
GET /api/v1/analytics/popularity?contentId=col_123&period=7d
Authorization: Bearer {token}
```

#### Get Search Analytics
```http
GET /api/v1/analytics/search?period=30d&groupBy=day
Authorization: Bearer {token}
```

### Custom Reports API

#### Create Custom Report
```http
POST /api/v1/reports/custom
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Monthly User Activity",
  "description": "Report on user activity for the month",
  "type": "user_activity",
  "category": "analytics",
  "parameters": {
    "dateRange": {
      "from": "2024-01-01",
      "to": "2024-01-31"
    },
    "groupBy": "day"
  },
  "schedule": {
    "enabled": true,
    "frequency": "monthly",
    "dayOfMonth": 1
  }
}
```

#### Get Custom Reports
```http
GET /api/v1/reports/custom?type=user_activity&page=1&limit=20
Authorization: Bearer {token}
```

#### Generate Report
```http
POST /api/v1/reports/custom/{reportId}/generate
Authorization: Bearer {token}
```

#### Download Report
```http
GET /api/v1/reports/custom/{reportId}/download?format=pdf
Authorization: Bearer {token}
```

## Conclusion

API này được thiết kế để:

1. **Consistency**: Tất cả endpoints follow cùng một pattern
2. **Performance**: Optimized cho high throughput và low latency
3. **Scalability**: Support horizontal scaling và load balancing
4. **Developer Experience**: Clear documentation và easy-to-use SDKs
5. **Reliability**: Comprehensive error handling và monitoring
6. **Security**: Proper authentication, authorization, và rate limiting
7. **Enterprise Features**: Content moderation, copyright management, security, analytics
8. **Advanced Capabilities**: Semantic search, visual search, custom reports, system health

API được thiết kế để có thể evolve theo thời gian mà không breaking existing clients thông qua versioning strategy.
