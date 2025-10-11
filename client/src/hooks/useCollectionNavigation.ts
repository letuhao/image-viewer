import { useQuery } from '@tanstack/react-query';
import { api } from '../services/api';

/**
 * Collection navigation info
 */
export interface CollectionNavigationInfo {
  previousCollectionId: string | null;
  nextCollectionId: string | null;
  currentPosition: number;
  totalCollections: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

/**
 * Collection siblings response
 */
export interface CollectionSiblingsResponse {
  siblings: any[]; // CollectionOverviewDto
  currentPosition: number;
  totalCount: number;
}

/**
 * Hook to get collection navigation info
 */
export const useCollectionNavigation = (
  collectionId: string | undefined,
  sortBy: string = 'updatedAt',
  sortDirection: string = 'desc'
) => {
  return useQuery<CollectionNavigationInfo, Error>({
    queryKey: ['collectionNavigation', collectionId, sortBy, sortDirection],
    queryFn: async () => {
      const response = await api.get<CollectionNavigationInfo>(
        `/collections/${collectionId}/navigation`,
        {
          params: { sortBy, sortDirection },
        }
      );
      return response.data;
    },
    enabled: !!collectionId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Hook to get collection siblings
 */
export const useCollectionSiblings = (
  collectionId: string | undefined,
  page: number = 1,
  pageSize: number = 20,
  sortBy: string = 'updatedAt',
  sortDirection: string = 'desc'
) => {
  return useQuery<CollectionSiblingsResponse, Error>({
    queryKey: ['collectionSiblings', collectionId, page, pageSize, sortBy, sortDirection],
    queryFn: async () => {
      const response = await api.get<CollectionSiblingsResponse>(
        `/collections/${collectionId}/siblings`,
        {
          params: { page, pageSize, sortBy, sortDirection },
        }
      );
      return response.data;
    },
    enabled: !!collectionId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

