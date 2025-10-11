import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://localhost:11001/api/v1';

export interface Library {
  id: string;
  name: string;
  description: string;
  path: string;
  ownerId: string;
  settings: LibrarySettings;
  metadata: any;
  statistics: LibraryStatistics;
  createdAt: string;
  updatedAt: string;
  isDeleted: boolean;
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
    const response = await axios.get(`${API_BASE_URL}/libraries`);
    return response.data;
  },

  // Get library by ID
  getById: async (id: string): Promise<Library> => {
    const response = await axios.get(`${API_BASE_URL}/libraries/${id}`);
    return response.data;
  },

  // Create library
  create: async (request: CreateLibraryRequest): Promise<Library> => {
    const response = await axios.post(`${API_BASE_URL}/libraries`, request);
    return response.data;
  },

  // Update library
  update: async (id: string, request: UpdateLibraryRequest): Promise<Library> => {
    const response = await axios.put(`${API_BASE_URL}/libraries/${id}`, request);
    return response.data;
  },

  // Update library settings
  updateSettings: async (id: string, request: UpdateLibrarySettingsRequest): Promise<Library> => {
    const response = await axios.put(`${API_BASE_URL}/libraries/${id}/settings`, request);
    return response.data;
  },

  // Delete library
  delete: async (id: string): Promise<void> => {
    await axios.delete(`${API_BASE_URL}/libraries/${id}`);
  },
};

