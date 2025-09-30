import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:8081/api';

export interface CacheFolder {
  id?: string;
  _id?: string; // MongoDB compatibility
  name: string;
  path: string;
  priority: number;
  max_size: number | null;
  current_size: number;
  file_count: number;
  is_active: boolean;
  created_at: string;
  updated_at: string;
}

export interface CacheFolderStats {
  summary: {
    total_folders: number;
    total_size: number;
    total_files: number;
    avg_priority: number;
  };
  folders: CacheFolder[];
}

export interface AddCacheFolderRequest {
  name: string;
  path: string;
  priority?: number;
  maxSize?: number | null;
}

export interface UpdateCacheFolderRequest {
  name?: string;
  path?: string;
  priority?: number;
  maxSize?: number | null;
  is_active?: boolean;
}

export interface ValidatePathRequest {
  path: string;
}

export interface ValidatePathResponse {
  valid: boolean;
  exists: boolean;
  writable: boolean;
  path: string;
  stats?: {
    isDirectory: boolean;
    size: number;
    freeSpace: number | null;
  };
  error?: string;
}

export const cacheFoldersApi = {
  // Get all cache folders with statistics
  getStats: async (): Promise<{ data: CacheFolderStats }> => {
    const response = await axios.get(`${API_BASE_URL}/cache-folders`);
    return response;
  },

  // Add a new cache folder
  addFolder: async (folder: AddCacheFolderRequest): Promise<{ data: { success: boolean; id: string; message: string } }> => {
    const response = await axios.post(`${API_BASE_URL}/cache-folders`, folder);
    return response;
  },

  // Update an existing cache folder
  updateFolder: async (id: string, updates: UpdateCacheFolderRequest): Promise<{ data: { success: boolean; message: string } }> => {
    const response = await axios.put(`${API_BASE_URL}/cache-folders/${id}`, updates);
    return response;
  },

  // Delete a cache folder
  deleteFolder: async (id: string): Promise<{ data: { success: boolean; message: string } }> => {
    const response = await axios.delete(`${API_BASE_URL}/cache-folders/${id}`);
    return response;
  },

  // Get cache folder for a specific collection
  getCollectionCacheFolder: async (collectionId: string): Promise<{ data: CacheFolder }> => {
    const response = await axios.get(`${API_BASE_URL}/cache-folders/collection/${collectionId}`);
    return response;
  },

  // Bind a cache folder to a collection
  bindToCollection: async (cacheFolderId: string, collectionId: string): Promise<{ data: { success: boolean; message: string } }> => {
    const response = await axios.post(`${API_BASE_URL}/cache-folders/${cacheFolderId}/bind/${collectionId}`);
    return response;
  },

  // Get usage statistics for a specific cache folder
  getFolderStats: async (id: string): Promise<{ data: CacheFolder }> => {
    const response = await axios.get(`${API_BASE_URL}/cache-folders/${id}/stats`);
    return response;
  },

  // Validate a cache folder path
  validatePath: async (path: string): Promise<{ data: ValidatePathResponse }> => {
    const response = await axios.post(`${API_BASE_URL}/cache-folders/validate`, { path });
    return response;
  },
};

export default cacheFoldersApi;
