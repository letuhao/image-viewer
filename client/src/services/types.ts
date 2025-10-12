/**
 * API Type Definitions
 * 
 * All types match the .NET backend DTOs
 * Keep in sync with ImageViewer.Application/DTOs
 */

// ============================================================================
// Collection Types
// ============================================================================

export interface Collection {
  id: string;
  name: string;
  path: string;
  type: 'folder' | 'archive';
  isNested: boolean;
  parentId?: string;
  depth: number;
  
  // Legacy fields (for backward compatibility with collection list)
  imageCount?: number;
  thumbnailCount?: number;
  cacheImageCount?: number;
  totalSize?: number;

  // New statistics object (from collection detail API)
  statistics?: {
    totalItems: number;
    totalThumbnails: number;
    totalCached: number;
    totalSize: number;
    lastScanDate?: string;
    lastViewed?: string;
  };

  // Thumbnail info for collection card display
  thumbnailPath?: string;
  thumbnailImageId?: string;
  hasThumbnail: boolean;
  
  // Base64 encoded thumbnail for instant display (no additional HTTP request)
  // Format: data:image/jpeg;base64,{base64Data}
  thumbnailBase64?: string;

  // First image ID for direct viewer navigation
  firstImageId?: string;

  createdAt: string;
  updatedAt: string;
}

export interface CreateCollectionRequest {
  path: string;
  name?: string;
}

export interface BulkAddCollectionsRequest {
  rootPath: string;
  recursive: boolean;
  includeArchives: boolean;
}

export interface CollectionStatsResponse {
  totalCollections: number;
  totalImages: number;
  totalSize: number;
  collectionsByType: Record<string, number>;
}

// ============================================================================
// Image Types
// ============================================================================

export interface Image {
  id: string;
  collectionId: string;
  filename: string;
  relativePath: string;
  fileSize: number;
  width: number;
  height: number;
  format: string;
  hashedBlurData?: string;
  createdAt: string;
  updatedAt: string;
}

export interface Thumbnail {
  id: string;
  imageId: string;
  width: number;
  height: number;
  fileSize: number;
  filePath: string;
  createdAt: string;
}

export interface CacheImage {
  id: string;
  imageId: string;
  width: number;
  height: number;
  quality: number;
  fileSize: number;
  filePath: string;
  createdAt: string;
}

// ============================================================================
// Background Job Types
// ============================================================================

export interface BackgroundJob {
  id: string;
  type: 'collection-scan' | 'thumbnail-generation' | 'cache-generation';
  status: 'pending' | 'in-progress' | 'completed' | 'failed' | 'cancelled';
  collectionId?: string;
  progress: number;
  totalItems: number;
  completedItems: number;
  stages: JobStage[];
  errorMessage?: string;
  startedAt?: string;
  completedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface JobStage {
  name: string;
  status: 'pending' | 'in-progress' | 'completed' | 'failed';
  progress: number;
  totalItems: number;
  completedItems: number;
  errorMessage?: string;
}

// ============================================================================
// Pagination Types
// ============================================================================

export interface PaginatedResponse<T> {
  data: T[];
  page: number;
  limit: number;
  total: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

export interface PaginationParams {
  page?: number;
  limit?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

// ============================================================================
// API Response Types
// ============================================================================

export interface ApiResponse<T> {
  data: T;
  message?: string;
  success: boolean;
}

export interface ApiError {
  message: string;
  errors?: Record<string, string[]>;
  statusCode: number;
}

// ============================================================================
// System Stats Types
// ============================================================================

export interface SystemStats {
  totalCollections: number;
  totalImages: number;
  totalThumbnails: number;
  totalCacheImages: number;
  activeJobs: number;
  pendingJobs: number;
  lastUpdate: string;
}

// ============================================================================
// Filter & Search Types
// ============================================================================

export interface ImageFilter {
  collectionId?: string;
  minWidth?: number;
  maxWidth?: number;
  minHeight?: number;
  maxHeight?: number;
  format?: string[];
  search?: string;
}

export interface CollectionFilter {
  type?: 'folder' | 'archive';
  isNested?: boolean;
  parentId?: string;
  search?: string;
}

