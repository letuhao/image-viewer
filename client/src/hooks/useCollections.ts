import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '../services/api';
import type { 
  Collection, 
  PaginatedResponse, 
  PaginationParams,
  CreateCollectionRequest,
  BulkAddCollectionsRequest 
} from '../services/types';
import toast from 'react-hot-toast';

/**
 * Collections API Hooks
 * 
 * React Query hooks for managing collections
 * Handles caching, loading states, and optimistic updates
 */

// Query Keys
export const collectionKeys = {
  all: ['collections'] as const,
  lists: () => [...collectionKeys.all, 'list'] as const,
  list: (params?: PaginationParams) => [...collectionKeys.lists(), params] as const,
  details: () => [...collectionKeys.all, 'detail'] as const,
  detail: (id: string) => [...collectionKeys.details(), id] as const,
  stats: () => [...collectionKeys.all, 'stats'] as const,
};

// Fetch all collections with pagination
export const useCollections = (params?: PaginationParams) => {
  return useQuery({
    queryKey: collectionKeys.list(params),
    queryFn: async () => {
      const response = await api.get<PaginatedResponse<Collection>>('/collections', {
        params,
      });
      return response.data;
    },
  });
};

// Fetch single collection by ID
export const useCollection = (id: string) => {
  return useQuery({
    queryKey: collectionKeys.detail(id),
    queryFn: async () => {
      const response = await api.get<Collection>(`/collections/${id}`);
      return response.data;
    },
    enabled: !!id,
  });
};

// Fetch collection stats
export const useCollectionStats = () => {
  return useQuery({
    queryKey: collectionKeys.stats(),
    queryFn: async () => {
      const response = await api.get('/collections/statistics');
      return response.data;
    },
    refetchInterval: 30000, // Refresh every 30 seconds
  });
};

// Create new collection
export const useCreateCollection = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: CreateCollectionRequest) => {
      const response = await api.post<Collection>('/collections', data);
      return response.data;
    },
    onSuccess: (newCollection) => {
      // Invalidate collections list to refetch
      queryClient.invalidateQueries({ queryKey: collectionKeys.lists() });
      queryClient.invalidateQueries({ queryKey: collectionKeys.stats() });
      toast.success(`Collection "${newCollection.name}" created successfully`);
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to create collection');
    },
  });
};

// Bulk add collections
export const useBulkAddCollections = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: BulkAddCollectionsRequest) => {
      const response = await api.post('/collections/bulk-add', data);
      return response.data;
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: collectionKeys.lists() });
      queryClient.invalidateQueries({ queryKey: collectionKeys.stats() });
      toast.success(`Bulk add started: ${data.jobId}`);
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to bulk add collections');
    },
  });
};

// Delete collection
export const useDeleteCollection = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/collections/${id}`);
      return id;
    },
    onSuccess: (id) => {
      queryClient.invalidateQueries({ queryKey: collectionKeys.lists() });
      queryClient.invalidateQueries({ queryKey: collectionKeys.stats() });
      queryClient.removeQueries({ queryKey: collectionKeys.detail(id) });
      toast.success('Collection deleted successfully');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to delete collection');
    },
  });
};

// Scan collection
export const useScanCollection = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      const response = await api.post(`/collections/${id}/scan`);
      return response.data;
    },
    onSuccess: (data, id) => {
      queryClient.invalidateQueries({ queryKey: collectionKeys.detail(id) });
      toast.success(`Scan started: ${data.jobId}`);
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to start scan');
    },
  });
};

