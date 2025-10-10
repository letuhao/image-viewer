import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'react-hot-toast';
import {
  getAllCacheFolders,
  validatePath,
  createCacheFolder,
  updateCacheFolder,
  deleteCacheFolder,
  type CacheFoldersResponse,
  type ValidatePathResponse,
  type CreateCacheFolderRequest,
  type UpdateCacheFolderRequest,
  type CacheFolder,
} from '../services/cacheFoldersApi';

/**
 * Hook to fetch all cache folders with disk info
 */
export const useCacheFolders = () => {
  return useQuery<CacheFoldersResponse, Error>({
    queryKey: ['cacheFolders'],
    queryFn: getAllCacheFolders,
    staleTime: 30 * 1000, // 30 seconds (disk info changes frequently)
  });
};

/**
 * Hook to validate a path
 */
export const useValidatePath = () => {
  return useMutation<ValidatePathResponse, Error, string>({
    mutationFn: (path: string) => validatePath(path),
    onSuccess: (data) => {
      if (data.valid) {
        const diskInfo = data.diskInfo;
        if (diskInfo?.available) {
          const freeGB = diskInfo.freeSpace ? (diskInfo.freeSpace / (1024 * 1024 * 1024)).toFixed(2) : '?';
          const totalGB = diskInfo.totalSpace ? (diskInfo.totalSpace / (1024 * 1024 * 1024)).toFixed(2) : '?';
          toast.success(`Path is valid! Free: ${freeGB} GB / Total: ${totalGB} GB`);
        } else {
          toast.success('Path is valid and writable');
        }
      } else {
        toast.error(data.error || 'Path validation failed');
      }
    },
    onError: (error) => {
      toast.error(`Validation failed: ${error.message}`);
    },
  });
};

/**
 * Hook to create cache folder
 */
export const useCreateCacheFolder = () => {
  const queryClient = useQueryClient();

  return useMutation<CacheFolder, Error, CreateCacheFolderRequest>({
    mutationFn: createCacheFolder,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cacheFolders'] });
      toast.success('Cache folder created successfully!');
    },
    onError: (error) => {
      toast.error(`Failed to create cache folder: ${error.message}`);
    },
  });
};

/**
 * Hook to update cache folder
 */
export const useUpdateCacheFolder = () => {
  const queryClient = useQueryClient();

  return useMutation<CacheFolder, Error, { id: string; request: UpdateCacheFolderRequest }>({
    mutationFn: ({ id, request }) => updateCacheFolder(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cacheFolders'] });
      toast.success('Cache folder updated successfully!');
    },
    onError: (error) => {
      toast.error(`Failed to update cache folder: ${error.message}`);
    },
  });
};

/**
 * Hook to delete cache folder
 */
export const useDeleteCacheFolder = () => {
  const queryClient = useQueryClient();

  return useMutation<void, Error, string>({
    mutationFn: deleteCacheFolder,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cacheFolders'] });
      toast.success('Cache folder deleted successfully!');
    },
    onError: (error) => {
      toast.error(`Failed to delete cache folder: ${error.message}`);
    },
  });
};

