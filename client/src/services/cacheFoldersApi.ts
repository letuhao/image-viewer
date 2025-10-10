import api from './api';

// ============================================================================
// Types
// ============================================================================

export interface CacheFolder {
  id: string;
  name: string;
  path: string;
  priority: number;
  maxSizeBytes: number | null;
  currentSize: number;
  fileCount: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  diskInfo: DiskInfo;
}

export interface DiskInfo {
  available: boolean;
  totalSpace?: number;
  freeSpace?: number;
  usedSpace?: number;
  usedPercentage?: number;
  driveFormat?: string;
  driveType?: string;
  error?: string;
}

export interface CacheFoldersSummary {
  totalFolders: number;
  totalSize: number;
  totalFiles: number;
  avgPriority: number;
}

export interface CacheFoldersResponse {
  folders: CacheFolder[];
  summary: CacheFoldersSummary;
}

export interface ValidatePathRequest {
  path: string;
}

export interface ValidatePathResponse {
  valid: boolean;
  exists?: boolean;
  writable?: boolean;
  diskInfo?: DiskInfo;
  error?: string;
}

export interface CreateCacheFolderRequest {
  name: string;
  path: string;
  priority: number;
  maxSizeBytes: number | null;
}

export interface UpdateCacheFolderRequest {
  name?: string;
  path?: string;
  priority?: number;
  maxSizeBytes?: number | null;
}

// ============================================================================
// Cache Folders API
// ============================================================================

/**
 * Get all cache folders with disk information
 */
export const getAllCacheFolders = async (): Promise<CacheFoldersResponse> => {
  const response = await api.get<CacheFoldersResponse>('/cachefolders');
  return response.data;
};

/**
 * Validate a path for cache folder
 */
export const validatePath = async (path: string): Promise<ValidatePathResponse> => {
  const response = await api.post<ValidatePathResponse>('/cachefolders/validate-path', { path });
  return response.data;
};

/**
 * Create a new cache folder
 */
export const createCacheFolder = async (
  request: CreateCacheFolderRequest
): Promise<CacheFolder> => {
  const response = await api.post<CacheFolder>('/cachefolders', request);
  return response.data;
};

/**
 * Update cache folder
 */
export const updateCacheFolder = async (
  id: string,
  request: UpdateCacheFolderRequest
): Promise<CacheFolder> => {
  const response = await api.put<CacheFolder>(`/cachefolders/${id}`, request);
  return response.data;
};

/**
 * Delete cache folder
 */
export const deleteCacheFolder = async (id: string): Promise<void> => {
  await api.delete(`/cachefolders/${id}`);
};

