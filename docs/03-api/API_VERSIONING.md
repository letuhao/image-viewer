# API Versioning Strategy - Image Viewer System

## Tổng quan API Versioning

### Versioning Strategy
- **URL Path Versioning**: `/api/v1/`, `/api/v2/`
- **Header Versioning**: `Accept: application/vnd.imageviewer.v1+json`
- **Query Parameter Versioning**: `?version=1`
- **Content Negotiation**: Multiple formats support

### Version Lifecycle
```
v1.0 → v1.1 → v1.2 → v2.0 → v2.1 → v3.0
  │      │      │      │      │      │
  │      │      │      │      │      └─ Major (Breaking Changes)
  │      │      │      │      └─ Minor (New Features)
  │      │      │      └─ Major (Breaking Changes)
  │      │      └─ Minor (New Features)
  │      └─ Minor (New Features)
  └─ Initial Release
```

## Versioning Implementation

### 1. URL Path Versioning (Primary)

#### Controller Structure
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0", "1.1", "1.2")]
public class CollectionsController : ControllerBase
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    public async Task<ActionResult<PagedResult<CollectionDto>>> GetCollectionsV1(
        [FromQuery] GetCollectionsQuery query)
    {
        // v1.0 implementation
    }
    
    [HttpGet]
    [MapToApiVersion("1.1")]
    public async Task<ActionResult<PagedResult<CollectionDtoV11>>> GetCollectionsV11(
        [FromQuery] GetCollectionsQuery query)
    {
        // v1.1 implementation with additional fields
    }
    
    [HttpGet]
    [MapToApiVersion("1.2")]
    public async Task<ActionResult<PagedResult<CollectionDtoV12>>> GetCollectionsV12(
        [FromQuery] GetCollectionsQuery query)
    {
        // v1.2 implementation with new features
    }
}

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("2.0")]
public class CollectionsV2Controller : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CollectionDtoV2>>> GetCollections(
        [FromQuery] GetCollectionsQueryV2 query)
    {
        // v2.0 implementation with breaking changes
    }
}
```

#### Startup Configuration
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-Version"),
            new QueryStringApiVersionReader("version")
        );
        options.ReportApiVersions = true;
    });
    
    services.AddVersionedApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });
}
```

### 2. Header Versioning

#### Request Headers
```http
GET /api/collections HTTP/1.1
Host: api.imageviewer.com
Accept: application/vnd.imageviewer.v1+json
X-Version: 1.0
```

#### Response Headers
```http
HTTP/1.1 200 OK
Content-Type: application/vnd.imageviewer.v1+json
X-Version: 1.0
API-Version: 1.0
```

### 3. Content Negotiation

#### Supported Media Types
```csharp
public static class MediaTypes
{
    public const string V1Json = "application/vnd.imageviewer.v1+json";
    public const string V2Json = "application/vnd.imageviewer.v2+json";
    public const string V1Xml = "application/vnd.imageviewer.v1+xml";
    public const string V2Xml = "application/vnd.imageviewer.v2+xml";
}
```

## Version Evolution

### Version 1.0 (Initial Release)

#### Collections API
```csharp
public class CollectionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Path { get; set; }
    public string Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// GET /api/v1/collections
{
  "success": true,
  "data": {
    "collections": [
      {
        "id": "col_123456789",
        "name": "My Manga Collection",
        "path": "D:\\Manga\\Collection1",
        "type": "folder",
        "createdAt": "2024-01-01T00:00:00Z",
        "updatedAt": "2024-01-01T00:00:00Z"
      }
    ],
    "pagination": {
      "page": 1,
      "limit": 20,
      "total": 150,
      "totalPages": 8
    }
  }
}
```

### Version 1.1 (Minor Update)

#### Added Features
- Collection statistics
- Cache status information
- Tag support

#### Collections API
```csharp
public class CollectionDtoV11 : CollectionDto
{
    public CollectionStatisticsDto Statistics { get; set; }
    public CacheStatusDto CacheStatus { get; set; }
    public List<TagDto> Tags { get; set; }
}

public class CollectionStatisticsDto
{
    public int ViewCount { get; set; }
    public long TotalViewTime { get; set; }
    public int SearchCount { get; set; }
    public DateTime? LastViewed { get; set; }
    public DateTime? LastSearched { get; set; }
}

public class CacheStatusDto
{
    public bool HasCache { get; set; }
    public int CachedImages { get; set; }
    public int TotalImages { get; set; }
    public int CachePercentage { get; set; }
    public DateTime? LastGenerated { get; set; }
}

public class TagDto
{
    public string Tag { get; set; }
    public int Count { get; set; }
    public string AddedBy { get; set; }
    public DateTime AddedAt { get; set; }
}
```

### Version 1.2 (Minor Update)

#### Added Features
- Advanced filtering
- Sorting options
- Search functionality

#### Collections API
```csharp
public class CollectionDtoV12 : CollectionDtoV11
{
    public List<string> SearchTags { get; set; }
    public string SortBy { get; set; }
    public string SortOrder { get; set; }
    public string Filter { get; set; }
}

// GET /api/v1.2/collections?filter=manga&sortBy=name&sortOrder=asc&tags=manga,comics
{
  "success": true,
  "data": {
    "collections": [
      {
        "id": "col_123456789",
        "name": "My Manga Collection",
        "path": "D:\\Manga\\Collection1",
        "type": "folder",
        "statistics": {
          "viewCount": 250,
          "totalViewTime": 3600,
          "searchCount": 45,
          "lastViewed": "2024-01-01T00:00:00Z",
          "lastSearched": "2024-01-01T00:00:00Z"
        },
        "cacheStatus": {
          "hasCache": true,
          "cachedImages": 1200,
          "totalImages": 1500,
          "cachePercentage": 80,
          "lastGenerated": "2024-01-01T00:00:00Z"
        },
        "tags": [
          {
            "tag": "manga",
            "count": 1,
            "addedBy": "user123",
            "addedAt": "2024-01-01T00:00:00Z"
          }
        ],
        "searchTags": ["manga", "comics"],
        "sortBy": "name",
        "sortOrder": "asc",
        "filter": "manga",
        "createdAt": "2024-01-01T00:00:00Z",
        "updatedAt": "2024-01-01T00:00:00Z"
      }
    ],
    "pagination": {
      "page": 1,
      "limit": 20,
      "total": 150,
      "totalPages": 8
    }
  }
}
```

### Version 2.0 (Major Update)

#### Breaking Changes
- Response format changes
- New authentication system
- Different error handling
- New field names

#### Collections API
```csharp
public class CollectionDtoV2
{
    public string Id { get; set; } // Changed from Guid to string
    public string Name { get; set; }
    public string Path { get; set; }
    public CollectionTypeV2 Type { get; set; } // Changed enum values
    public CollectionSettingsV2 Settings { get; set; } // New structure
    public CollectionMetricsV2 Metrics { get; set; } // Renamed from Statistics
    public CacheInfoV2 Cache { get; set; } // Renamed from CacheStatus
    public List<CollectionTagV2> Tags { get; set; } // New structure
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Version { get; set; } // New field
}

public enum CollectionTypeV2
{
    Directory = 0, // Changed from Folder
    Archive = 1,   // Changed from Zip
    Compressed = 2 // Changed from SevenZip
}

public class CollectionMetricsV2
{
    public long Views { get; set; } // Renamed from ViewCount
    public TimeSpan TotalViewTime { get; set; } // Changed from long to TimeSpan
    public long Searches { get; set; } // Renamed from SearchCount
    public DateTime? LastViewed { get; set; }
    public DateTime? LastSearched { get; set; }
    public double AverageViewTime { get; set; } // New field
}

public class CacheInfoV2
{
    public bool IsCached { get; set; } // Renamed from HasCache
    public int CachedCount { get; set; } // Renamed from CachedImages
    public int TotalCount { get; set; } // Renamed from TotalImages
    public double CacheRatio { get; set; } // Renamed from CachePercentage
    public DateTime? LastGenerated { get; set; }
    public long CacheSize { get; set; } // New field
}

public class CollectionTagV2
{
    public string Name { get; set; } // Renamed from Tag
    public int UsageCount { get; set; } // Renamed from Count
    public string CreatedBy { get; set; } // Renamed from AddedBy
    public DateTime CreatedAt { get; set; } // Renamed from AddedAt
    public TagColor Color { get; set; } // New field
}

public class TagColor
{
    public string Hex { get; set; }
    public string Name { get; set; }
}
```

#### Response Format Changes
```json
{
  "status": "success", // Changed from "success"
  "payload": { // Changed from "data"
    "items": [ // Changed from "collections"
      {
        "id": "col_123456789",
        "name": "My Manga Collection",
        "path": "D:\\Manga\\Collection1",
        "type": "directory",
        "settings": {
          "autoScan": true,
          "thumbnailQuality": 80,
          "cacheEnabled": true
        },
        "metrics": {
          "views": 250,
          "totalViewTime": "01:00:00",
          "searches": 45,
          "lastViewed": "2024-01-01T00:00:00Z",
          "lastSearched": "2024-01-01T00:00:00Z",
          "averageViewTime": 14.4
        },
        "cache": {
          "isCached": true,
          "cachedCount": 1200,
          "totalCount": 1500,
          "cacheRatio": 80.0,
          "lastGenerated": "2024-01-01T00:00:00Z",
          "cacheSize": 1073741824
        },
        "tags": [
          {
            "name": "manga",
            "usageCount": 1,
            "createdBy": "user123",
            "createdAt": "2024-01-01T00:00:00Z",
            "color": {
              "hex": "#6B7280",
              "name": "Gray"
            }
          }
        ],
        "createdAt": "2024-01-01T00:00:00Z",
        "updatedAt": "2024-01-01T00:00:00Z",
        "version": "2.0"
      }
    ],
    "pagination": {
      "currentPage": 1, // Changed from "page"
      "pageSize": 20, // Changed from "limit"
      "totalItems": 150, // Changed from "total"
      "totalPages": 8,
      "hasNext": true,
      "hasPrevious": false
    }
  },
  "metadata": { // New field
    "apiVersion": "2.0",
    "timestamp": "2024-01-01T00:00:00Z",
    "requestId": "req_123456789"
  }
}
```

## Migration Strategy

### 1. Backward Compatibility

#### Version 1.x Support
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0", "1.1", "1.2")]
public class CollectionsController : ControllerBase
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    public async Task<ActionResult<PagedResult<CollectionDto>>> GetCollectionsV1(
        [FromQuery] GetCollectionsQuery query)
    {
        var collections = await _mediator.Send(query);
        return Ok(collections);
    }
    
    [HttpGet]
    [MapToApiVersion("1.1")]
    public async Task<ActionResult<PagedResult<CollectionDtoV11>>> GetCollectionsV11(
        [FromQuery] GetCollectionsQuery query)
    {
        var collections = await _mediator.Send(query);
        var v11Collections = collections.Items.Select(c => c.ToV11Dto()).ToList();
        return Ok(new PagedResult<CollectionDtoV11>(v11Collections, collections.Total, collections.Page, collections.PageSize));
    }
    
    [HttpGet]
    [MapToApiVersion("1.2")]
    public async Task<ActionResult<PagedResult<CollectionDtoV12>>> GetCollectionsV12(
        [FromQuery] GetCollectionsQuery query)
    {
        var collections = await _mediator.Send(query);
        var v12Collections = collections.Items.Select(c => c.ToV12Dto()).ToList();
        return Ok(new PagedResult<CollectionDtoV12>(v12Collections, collections.Total, collections.Page, collections.PageSize));
    }
}
```

### 2. Deprecation Strategy

#### Deprecation Headers
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0", Deprecated = true)]
public class CollectionsController : ControllerBase
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    public async Task<ActionResult<PagedResult<CollectionDto>>> GetCollectionsV1(
        [FromQuery] GetCollectionsQuery query)
    {
        Response.Headers.Add("Deprecation", "true");
        Response.Headers.Add("Sunset", "2024-12-31T23:59:59Z");
        Response.Headers.Add("Link", "</api/v2/collections>; rel=\"successor-version\"");
        
        var collections = await _mediator.Send(query);
        return Ok(collections);
    }
}
```

#### Deprecation Response
```http
HTTP/1.1 200 OK
Content-Type: application/json
Deprecation: true
Sunset: 2024-12-31T23:59:59Z
Link: </api/v2/collections>; rel="successor-version"
Warning: 299 - "This API version is deprecated. Please migrate to v2.0 by 2024-12-31."

{
  "success": true,
  "data": { ... },
  "deprecation": {
    "deprecated": true,
    "sunset": "2024-12-31T23:59:59Z",
    "successorVersion": "2.0",
    "migrationGuide": "https://docs.imageviewer.com/migration/v1-to-v2"
  }
}
```

### 3. Migration Guide

#### Version 1.x to 2.0 Migration
```markdown
# Migration Guide: v1.x to v2.0

## Breaking Changes

### 1. Response Format Changes
- `success` → `status`
- `data` → `payload`
- `collections` → `items`
- `page` → `currentPage`
- `limit` → `pageSize`
- `total` → `totalItems`

### 2. Field Name Changes
- `statistics` → `metrics`
- `cacheStatus` → `cache`
- `viewCount` → `views`
- `searchCount` → `searches`
- `cachePercentage` → `cacheRatio`

### 3. Data Type Changes
- `totalViewTime`: `long` → `TimeSpan`
- `cachePercentage`: `int` → `double`

### 4. New Fields
- `version`: API version string
- `metadata`: Request metadata
- `averageViewTime`: Average view time calculation
- `cacheSize`: Cache size in bytes
- `tag.color`: Tag color information

## Migration Steps

### Step 1: Update API Endpoints
```javascript
// Before (v1.x)
const response = await fetch('/api/v1/collections');
const data = await response.json();
const collections = data.data.collections;

// After (v2.0)
const response = await fetch('/api/v2/collections');
const data = await response.json();
const collections = data.payload.items;
```

### Step 2: Update Field Mappings
```javascript
// Before (v1.x)
const viewCount = collection.statistics.viewCount;
const cachePercentage = collection.cacheStatus.cachePercentage;

// After (v2.0)
const viewCount = collection.metrics.views;
const cacheRatio = collection.cache.cacheRatio;
```

### Step 3: Handle New Fields
```javascript
// New fields in v2.0
const version = collection.version;
const averageViewTime = collection.metrics.averageViewTime;
const cacheSize = collection.cache.cacheSize;
const tagColor = collection.tags[0].color;
```
```

## Version Testing

### 1. Version-Specific Tests
```csharp
[TestClass]
public class CollectionsApiV1Tests
{
    [TestMethod]
    public async Task GetCollectionsV1_ReturnsCorrectFormat()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept", "application/vnd.imageviewer.v1+json");
        
        // Act
        var response = await client.GetAsync("/api/v1/collections");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<ApiResponseV1>(content);
        
        Assert.IsTrue(data.Success);
        Assert.IsNotNull(data.Data);
        Assert.IsNotNull(data.Data.Collections);
    }
}

[TestClass]
public class CollectionsApiV2Tests
{
    [TestMethod]
    public async Task GetCollectionsV2_ReturnsCorrectFormat()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Accept", "application/vnd.imageviewer.v2+json");
        
        // Act
        var response = await client.GetAsync("/api/v2/collections");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<ApiResponseV2>(content);
        
        Assert.AreEqual("success", data.Status);
        Assert.IsNotNull(data.Payload);
        Assert.IsNotNull(data.Payload.Items);
    }
}
```

### 2. Compatibility Tests
```csharp
[TestClass]
public class ApiCompatibilityTests
{
    [TestMethod]
    public async Task V1AndV2_ReturnSameData()
    {
        // Arrange
        var v1Client = _factory.CreateClient();
        v1Client.DefaultRequestHeaders.Add("Accept", "application/vnd.imageviewer.v1+json");
        
        var v2Client = _factory.CreateClient();
        v2Client.DefaultRequestHeaders.Add("Accept", "application/vnd.imageviewer.v2+json");
        
        // Act
        var v1Response = await v1Client.GetAsync("/api/v1/collections");
        var v2Response = await v2Client.GetAsync("/api/v2/collections");
        
        // Assert
        var v1Content = await v1Response.Content.ReadAsStringAsync();
        var v2Content = await v2Response.Content.ReadAsStringAsync();
        
        var v1Data = JsonSerializer.Deserialize<ApiResponseV1>(v1Content);
        var v2Data = JsonSerializer.Deserialize<ApiResponseV2>(v2Content);
        
        // Compare core data
        Assert.AreEqual(v1Data.Data.Collections.Count, v2Data.Payload.Items.Count);
        Assert.AreEqual(v1Data.Data.Pagination.Total, v2Data.Payload.Pagination.TotalItems);
    }
}
```

## Documentation

### 1. API Documentation
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0", "1.1", "1.2")]
public class CollectionsController : ControllerBase
{
    /// <summary>
    /// Get collections (v1.0)
    /// </summary>
    /// <param name="query">Query parameters</param>
    /// <returns>List of collections</returns>
    /// <response code="200">Returns collections</response>
    /// <response code="400">Bad request</response>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedResult<CollectionDto>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<ActionResult<PagedResult<CollectionDto>>> GetCollectionsV1(
        [FromQuery] GetCollectionsQuery query)
    {
        // Implementation
    }
    
    /// <summary>
    /// Get collections with statistics (v1.1)
    /// </summary>
    /// <param name="query">Query parameters</param>
    /// <returns>List of collections with statistics</returns>
    /// <response code="200">Returns collections with statistics</response>
    /// <response code="400">Bad request</response>
    [HttpGet]
    [MapToApiVersion("1.1")]
    [ProducesResponseType(typeof(PagedResult<CollectionDtoV11>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<ActionResult<PagedResult<CollectionDtoV11>>> GetCollectionsV11(
        [FromQuery] GetCollectionsQuery query)
    {
        // Implementation
    }
}
```

### 2. Changelog
```markdown
# API Changelog

## Version 2.0.0 (2024-06-01)

### Breaking Changes
- Changed response format structure
- Renamed several fields
- Changed data types for some fields
- Added new required fields

### New Features
- Enhanced metrics collection
- Improved cache information
- Tag color support
- Request metadata

### Deprecated
- Version 1.x APIs (will be removed on 2024-12-31)

## Version 1.2.0 (2024-03-01)

### New Features
- Advanced filtering
- Enhanced sorting options
- Search functionality
- Tag support

### Improvements
- Better error messages
- Performance optimizations

## Version 1.1.0 (2024-02-01)

### New Features
- Collection statistics
- Cache status information
- Tag support

### Improvements
- Better pagination
- Enhanced error handling

## Version 1.0.0 (2024-01-01)

### Initial Release
- Basic collection management
- Image viewing
- Cache generation
- Background job processing
```

## Conclusion

API versioning strategy đảm bảo:

1. **Backward Compatibility**: Hỗ trợ multiple versions cùng lúc
2. **Smooth Migration**: Hướng dẫn migration rõ ràng
3. **Clear Deprecation**: Thông báo deprecation và sunset dates
4. **Comprehensive Testing**: Test coverage cho tất cả versions
5. **Documentation**: Documentation đầy đủ cho mỗi version

Strategy này giúp hệ thống có thể evolve một cách an toàn và không breaking existing clients.
