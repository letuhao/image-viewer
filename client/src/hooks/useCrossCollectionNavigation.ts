import { useMutation } from '@tanstack/react-query';
import { api } from '../services/api';

/**
 * Cross-collection navigation result
 */
export interface CrossCollectionNavigationResult {
  targetImageId?: string;
  targetCollectionId?: string;
  isCrossCollection: boolean;
  targetCollectionName?: string;
  targetImagePosition: number;
  totalImagesInTargetCollection: number;
  hasTarget: boolean;
  errorMessage?: string;
}

/**
 * Hook for cross-collection image navigation
 */
export const useCrossCollectionNavigation = () => {
  const navigateMutation = useMutation({
    mutationFn: async ({
      collectionId,
      imageId,
      direction,
      sortBy = 'updatedAt',
      sortDirection = 'desc'
    }: {
      collectionId: string;
      imageId: string;
      direction: 'next' | 'prev';
      sortBy?: string;
      sortDirection?: string;
    }) => {
      const response = await api.get<CrossCollectionNavigationResult>(
        `/images/collection/${collectionId}/navigation/${imageId}`,
        {
          params: { direction, sortBy, sortDirection }
        }
      );
      return response.data;
    },
  });

  return {
    navigate: navigateMutation.mutateAsync,
    isNavigating: navigateMutation.isPending,
    error: navigateMutation.error,
    reset: navigateMutation.reset,
  };
};
