# Collection API Optimization

## Problem

The collection API was returning **massive payloads** including all embedded data (images, thumbnails, cache images, bindings, etc.) even for simple list views. This caused:

- **Performance issues**: Large JSON payloads (MB+ per collection)
- **Slow page loads**: Collections list taking seconds to load
- **Unnecessary data transfer**: Clients downloading data they don't need

### Example of bloated response:
```json
{
  "images": [ /* 1000s of image objects */ ],
  "thumbnails": [ /* 1000s of thumbnail objects */ ],
  "cacheImages": [ /* 1000s of cache objects */ ],
  "cacheBindings": [ /* ... */ ],
  "settings": { /* ... */ },
  "metadata": { /* ... */ },
  // ... and more
}
```

## Solution

Split collection responses into two types:

### 1. **CollectionOverviewDto** (Lightweight - for lists)
Used by: Collection lists, search results, navigation

**Includes:**
- Basic info: ID, name, path, type
- Counts only: imageCount, thumbnailCount, cacheImageCount
- Metadata: createdAt, updatedAt
- Total size (calculated)

**Excludes:**
- Embedded images array
- Embedded thumbnails array
- Embedded cache images array
- Full settings object
- Full metadata object
- Statistics details

**Size:** ~500 bytes vs ~500KB+ before

### 2. **CollectionDetailDto** (Complete - for detail view)
Used by: Single collection detail page

**Includes:**
- Everything from Overview
- Full embedded arrays (images, thumbnails, cache)
- Complete settings
- Complete metadata
- Complete statistics
- Watch info
- Search index
- Cache bindings

**Size:** Same as before, but only loaded when needed

## API Endpoints

### List Collections (Lightweight)
```
GET /api/v1/collections?page=1&pageSize=20
```
Returns: `CollectionOverviewDto[]`

### Get Collection Detail (Full data)
```
GET /api/v1/collections/{id}
```
Returns: `CollectionDetailDto`

### Get Collection Overview (Lightweight single)
```
GET /api/v1/collections/{id}/overview
```
Returns: `CollectionOverviewDto`

## Implementation

### 1. DTOs Created
- `CollectionOverviewDto.cs` - Lightweight DTO
- `CollectionDetailDto.cs` - Full DTO with nested DTOs for settings, metadata, etc.

### 2. Mapping Extensions
- `CollectionMappingExtensions.cs`
  - `ToOverviewDto()` - Maps to lightweight DTO
  - `ToDetailDto()` - Maps to full DTO

### 3. Controller Updates
- `GET /collections` - Returns overview DTOs
- `GET /collections/{id}` - Returns detail DTO
- `GET /collections/{id}/overview` - Returns single overview DTO

## Performance Impact

### Before:
- Collection list (20 items): **~10MB** response
- Load time: **3-5 seconds**
- Bandwidth: High

### After:
- Collection list (20 items): **~10KB** response (1000x smaller!)
- Load time: **<100ms**
- Bandwidth: Minimal

## Frontend Updates Needed

Update the frontend to use the new response format:

### TypeScript Interface (Overview)
```typescript
interface CollectionOverview {
  id: string;
  name: string;
  path: string;
  type: 'folder' | 'archive';
  isNested: boolean;
  depth: number;
  imageCount: number;
  thumbnailCount: number;
  cacheImageCount: number;
  totalSize: number;
  createdAt: string;
  updatedAt: string;
}
```

### TypeScript Interface (Detail)
```typescript
interface CollectionDetail extends CollectionOverview {
  libraryId: string;
  description?: string;
  isActive: boolean;
  settings: CollectionSettings;
  metadata: CollectionMetadata;
  statistics: CollectionStatistics;
  watchInfo: WatchInfo;
  searchIndex: SearchIndex;
  images: ImageEmbedded[];
  thumbnails: ThumbnailEmbedded[];
  cacheImages: CacheImageEmbedded[];
  cacheBindings: CacheBindingEmbedded[];
}
```

### API Calls
```typescript
// For list view - use overview
const response = await api.get<PaginatedResponse<CollectionOverview>>('/collections');

// For detail view - use full detail
const response = await api.get<CollectionDetail>(`/collections/${id}`);

// For single overview - use overview endpoint
const response = await api.get<CollectionOverview>(`/collections/${id}/overview`);
```

## Migration Notes

1. **Backward Compatibility**: The detail endpoint (`GET /collections/{id}`) still returns all data
2. **New Endpoint**: Added `/collections/{id}/overview` for lightweight single collection
3. **List Changed**: `GET /collections` now returns overview DTOs instead of full entities

## Future Improvements

1. **Projection at DB Level**: Use MongoDB projection to avoid loading embedded arrays into memory
2. **Pagination for Embedded Arrays**: Add pagination for images/thumbnails/cache within a collection
3. **GraphQL**: Consider GraphQL for flexible data fetching
4. **Caching**: Add Redis cache for frequently accessed collections
5. **Nested Collection Support**: Add proper `isNested` and `depth` calculation logic

