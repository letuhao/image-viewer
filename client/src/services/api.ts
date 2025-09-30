import axios from 'axios';

const API_BASE_URL = '/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
});

// Collections API
export const collectionsApi = {
  getAll: (params?: { page?: number; limit?: number; filter?: string }) => 
    api.get('/collections', { params }),
  getById: (id: string) => api.get(`/collections/${id}`),
  create: (data: { name: string; path: string; type: 'folder' | 'zip' }) => 
    api.post('/collections', data),
  update: (id: string, data: Partial<{ name: string; path: string; settings: Record<string, any> }>) => 
    api.put(`/collections/${id}`, data),
  delete: (id: string) => api.delete(`/collections/${id}`),
  scan: (id: string) => api.post(`/collections/${id}/scan`),
  getImages: (id: string, params?: { page?: number; limit?: number; sort?: string; order?: string }) => 
    api.get(`/collections/${id}/images`, { params }),
  getRandom: () => api.get('/random'),
};

// Images API
export const imagesApi = {
  getAll: (collectionId: string, params?: { page?: number; limit?: number; sort?: string; order?: string }) => 
    api.get(`/images/${collectionId}`, { params }),
  getById: (id: string, collectionId: string) => 
    api.get(`/images/${id}`, { params: { collectionId } }),
  getFile: (collectionId: string, imageId: string, params?: { width?: number; height?: number; quality?: number }) => 
    api.get(`/images/${collectionId}/${imageId}/file`, { 
      params,
      responseType: 'blob'
    }),
  getThumbnail: (collectionId: string, imageId: string) => 
    api.get(`/images/${collectionId}/${imageId}/thumbnail`, { 
      responseType: 'blob'
    }),
  getBatchThumbnails: (collectionId: string, imageIds: string[], params?: { width?: number; height?: number; quality?: number }) => 
    api.get(`/images/${collectionId}/batch-thumbnails`, { 
      params: { 
        ids: imageIds.join(','), 
        ...params 
      } 
    }),
  navigate: (collectionId: string, imageId: string, direction: 'next' | 'previous') => 
    api.get(`/images/${collectionId}/${imageId}/navigate`, { params: { direction } }),
  getRandom: (collectionId: string) => 
    api.get(`/images/${collectionId}/random`),
  search: (collectionId: string, query: string, params?: { page?: number; limit?: number }) => 
    api.get(`/images/${collectionId}/search`, { params: { query, ...params } }),
};

// Cache API
export const cacheApi = {
  clear: () => api.delete('/cache'),
  getStats: () => api.get('/cache/stats'),
};

// Health check
export const healthApi = {
  check: () => api.get('/health'),
};

// Bulk operations API
export const bulkApi = {
  preview: (parentPath: string, collectionPrefix: string = '', includeSubfolders: boolean = false) => 
    api.post('/bulk/preview', { parentPath, collectionPrefix, includeSubfolders }),
  addCollections: (parentPath: string, collectionPrefix: string = '', includeSubfolders: boolean = false, autoAdd: boolean = false) => 
    api.post('/bulk/collections', { parentPath, collectionPrefix, includeSubfolders, autoAdd }),
};

// Background Jobs API
export const backgroundApi = {
  startBulkAdd: async (data: {
    parentPath: string;
    collectionPrefix?: string;
    includeSubfolders?: boolean;
  }) => {
    const response = await fetch('/api/background/bulk-add/start', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to start bulk add job');
    return response.json();
  },

  getJobStatus: async (jobId: string) => {
    const response = await fetch(`/api/background/jobs/${jobId}`);
    if (!response.ok) throw new Error('Failed to get job status');
    return response.json();
  },

  getAllJobs: async (status?: string) => {
    const url = status ? `/api/background/jobs?status=${status}` : '/api/background/jobs';
    const response = await fetch(url);
    if (!response.ok) throw new Error('Failed to get jobs');
    return response.json();
  },

  cancelJob: async (jobId: string) => {
    const response = await fetch(`/api/background/jobs/${jobId}/cancel`, {
      method: 'POST'
    });
    if (!response.ok) throw new Error('Failed to cancel job');
    return response.json();
  }
};

// Statistics and Tags API
export const statsApi = {
  getCollectionStats: (collectionId: string) => api.get(`/stats/collection/${collectionId}`),
  trackView: (collectionId: string, sessionId?: string) => 
    api.post(`/stats/collection/${collectionId}/view`, { session_id: sessionId }),
  endViewSession: (collectionId: string, sessionId: string, viewTimeSeconds?: number) => 
    api.post(`/stats/collection/${collectionId}/view/end`, { session_id: sessionId, view_time_seconds: viewTimeSeconds }),
  trackSearch: (collectionId: string, query: string) => 
    api.post(`/stats/collection/${collectionId}/search`, { query }),
  addTag: (collectionId: string, tag: string, addedBy?: string) => 
    api.post(`/stats/collection/${collectionId}/tags`, { tag, added_by: addedBy }),
  removeTag: (collectionId: string, tag: string, addedBy?: string) => 
    api.delete(`/stats/collection/${collectionId}/tags/${encodeURIComponent(tag)}${addedBy ? `?added_by=${addedBy}` : ''}`),
  getPopularCollections: (limit?: number) => api.get('/stats/popular', { params: { limit } }),
  getPopularTags: (limit?: number) => api.get('/stats/tags/popular', { params: { limit } }),
  getAnalytics: () => api.get('/stats/analytics'),
  
  // Tag search and filtering
  searchTags: (query: string, limit?: number) => 
    api.get('/stats/tags/search', { params: { q: query, limit } }),
  getTagSuggestions: (query: string, limit?: number) => 
    api.get('/stats/tags/suggestions', { params: { q: query, limit } }),
  getCollectionsByTags: (tags: string[], operator?: 'AND' | 'OR', limit?: number, offset?: number) => 
    api.get('/stats/collections/by-tags', { 
      params: { 
        tags: tags.join(','), 
        operator, 
        limit, 
        offset 
      } 
    }),

  // Tag service API
  getAvailableLanguages: () => api.get('/stats/tags/languages'),
  getTagCategories: (language?: string) => 
    api.get('/stats/tags/categories', { params: { language } }),
  getCategoryTags: (categoryKey: string, language?: string) => 
    api.get(`/stats/tags/category/${categoryKey}`, { params: { language } }),
  serviceSearchTags: (query: string, language?: string, limit?: number) => 
    api.get('/stats/tags/service/search', { params: { q: query, language, limit } }),
  getServicePopularTags: (language?: string, limit?: number) => 
    api.get('/stats/tags/service/popular', { params: { language, limit } }),
  getServiceTagSuggestions: (existingTags?: string[], language?: string, limit?: number) => 
    api.get('/stats/tags/service/suggestions', { 
      params: { 
        tags: existingTags?.join(','), 
        language, 
        limit 
      } 
    }),
  translateTag: (tagKey: string, fromLanguage?: string) => 
    api.get(`/stats/tags/service/translate/${tagKey}`, { params: { fromLanguage } }),
};

// Error handling interceptor
api.interceptors.response.use(
  (response) => response,
  (error) => {
    console.error('API Error:', error);
    
    if (error.response) {
      // Server responded with error status
      const message = error.response.data?.error || 'An error occurred';
      throw new Error(message);
    } else if (error.request) {
      // Request was made but no response received
      throw new Error('Network error - please check your connection');
    } else {
      // Something else happened
      throw new Error('An unexpected error occurred');
    }
  }
);

export default api;
