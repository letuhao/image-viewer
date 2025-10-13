import { useQuery } from '@tanstack/react-query';
import { api } from '../services/api';
import type { Image, PaginatedResponse } from '../services/types';

interface UseImagesOptions {
  collectionId: string;
  page?: number;
  limit?: number;
  enabled?: boolean;
}

export const useImages = ({ collectionId, page = 1, limit = 100, enabled = true }: UseImagesOptions) => {
  return useQuery<PaginatedResponse<Image>, Error>({
    queryKey: ['images', collectionId, { page, limit, filterValidOnly: true }],
    queryFn: async () => {
      const response = await api.get<PaginatedResponse<Image>>(
        `/images/collection/${collectionId}`,
        {
          params: { page, limit, filterValidOnly: true },
        }
      );
      return response.data;
    },
    enabled: enabled && !!collectionId,
  });
};

export const useImage = (collectionId: string, imageId: string) => {
  return useQuery<Image, Error>({
    queryKey: ['image', collectionId, imageId],
    queryFn: async () => {
      const response = await api.get<Image>(`/images/${collectionId}/${imageId}`);
      return response.data;
    },
    enabled: !!collectionId && !!imageId,
  });
};

