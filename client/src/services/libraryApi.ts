import { api } from './api';

export interface Library {
  id: string;
  name: string;
  description: string;
  path: string;
  ownerId: string;
  isPublic: boolean;
  isActive: boolean;
  settings: LibrarySettings;
  metadata: LibraryMetadata;
  statistics: LibraryStatistics;
  watchInfo: WatchInfo;
  createdAt: string;
  updatedAt: string;
  isDeleted: boolean;
  createdBy?: string;
  updatedBy?: string;
}

export interface LibraryMetadata {
  description?: string;
  tags: string[];
  categories: string[];
  customFields: Record<string, any>;
}

export interface WatchInfo {
  isWatching: boolean;
  watchPath?: string;
  watchFilters: string[];
  lastWatchEvent?: string;
}

export interface LibrarySettings {
  autoScan: boolean;
  scanInterval: number;
  generateThumbnails: boolean;
  generateCache: boolean;
  enableWatching: boolean;
  maxFileSize: number;
  allowedFormats: string[];
  excludedPaths: string[];
}

export interface LibraryStatistics {
  totalCollections: number;
  totalMediaItems: number;
  totalSize: number;
  totalViews: number;
  totalDownloads: number;
  lastScannedAt?: string;
}

export interface CreateLibraryRequest {
  name: string;
  path: string;
  ownerId: string;  // Required by backend
  description?: string;
  autoScan?: boolean;
}

export interface UpdateLibraryRequest {
  name?: string;
  description?: string;
  path?: string;
}

export interface UpdateLibrarySettingsRequest {
  autoScan?: boolean;
  scanInterval?: number;
  generateThumbnails?: boolean;
  generateCache?: boolean;
  enableWatching?: boolean;
  maxFileSize?: number;
  allowedFormats?: string[];
  excludedPaths?: string[];
}

export const libraryApi = {
  // Get all libraries
  getAll: async (): Promise<Library[]> => {
    const response = await api.get('/libraries');
    return response.data;
  },

  // Get library by ID
  getById: async (id: string): Promise<Library> => {
    const response = await api.get(`/libraries/${id}`);
    return response.data;
  },

  // Create library
  create: async (request: CreateLibraryRequest): Promise<Library> => {
    const response = await api.post('/libraries', request);
    return response.data;
  },

  // Update library
  update: async (id: string, request: UpdateLibraryRequest): Promise<Library> => {
    const response = await api.put(`/libraries/${id}`, request);
    return response.data;
  },

  // Update library settings
  updateSettings: async (id: string, request: UpdateLibrarySettingsRequest): Promise<Library> => {
    const response = await api.put(`/libraries/${id}/settings`, request);
    return response.data;
  },

  // Delete library
  delete: async (id: string): Promise<void> => {
    await api.delete(`/libraries/${id}`);
  },
};

