import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ChevronLeft, ChevronRight, Folder, Archive } from 'lucide-react';
import { useCollectionNavigation, useCollectionSiblings } from '../../hooks/useCollectionNavigation';
import LoadingSpinner from '../ui/LoadingSpinner';

interface CollectionNavigationSidebarProps {
  collectionId: string;
  sortBy?: string;
  sortDirection?: string;
  onNavigate?: (collectionId: string) => void;
}

/**
 * Shared collection navigation sidebar
 * Shows vertical list of sibling collections with navigation
 */
const CollectionNavigationSidebar: React.FC<CollectionNavigationSidebarProps> = ({
  collectionId,
  sortBy = 'updatedAt',
  sortDirection = 'desc',
  onNavigate,
}) => {
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const { data: navigationData, isLoading: navLoading } = useCollectionNavigation(
    collectionId,
    sortBy,
    sortDirection
  );

  const { data: siblingsData, isLoading: siblingsLoading } = useCollectionSiblings(
    collectionId,
    page,
    pageSize,
    sortBy,
    sortDirection
  );

  const handleCollectionClick = (id: string) => {
    if (onNavigate) {
      onNavigate(id);
    } else {
      navigate(`/collections/${id}`);
    }
  };

  const handlePrevious = () => {
    if (navigationData?.previousCollectionId) {
      handleCollectionClick(navigationData.previousCollectionId);
    }
  };

  const handleNext = () => {
    if (navigationData?.nextCollectionId) {
      handleCollectionClick(navigationData.nextCollectionId);
    }
  };

  if (navLoading || siblingsLoading) {
    return (
      <div className="w-64 border-r border-slate-800 bg-slate-900/50 flex items-center justify-center">
        <LoadingSpinner text="Loading..." />
      </div>
    );
  }

  return (
    <div className="w-64 border-r border-slate-800 bg-slate-900/50 flex flex-col">
      {/* Header with Position */}
      <div className="flex-shrink-0 border-b border-slate-800 p-4">
        <h3 className="text-sm font-semibold text-white mb-2">Collections</h3>
        {navigationData && (
          <div className="flex items-center justify-between text-xs text-slate-400">
            <span>
              {navigationData.currentPosition} / {navigationData.totalCollections}
            </span>
            {/* Quick Navigation Buttons */}
            <div className="flex items-center gap-1">
              <button
                onClick={handlePrevious}
                disabled={!navigationData.hasPrevious}
                className="p-1 rounded text-slate-400 hover:text-white hover:bg-slate-700 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
                title="Previous Collection"
              >
                <ChevronLeft className="h-4 w-4" />
              </button>
              <button
                onClick={handleNext}
                disabled={!navigationData.hasNext}
                className="p-1 rounded text-slate-400 hover:text-white hover:bg-slate-700 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
                title="Next Collection"
              >
                <ChevronRight className="h-4 w-4" />
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Siblings List */}
      <div className="flex-1 overflow-y-auto">
        {siblingsData?.siblings.map((collection: any) => {
          const isActive = collection.id === collectionId;
          return (
            <button
              key={collection.id}
              onClick={() => handleCollectionClick(collection.id)}
              className={`w-full p-3 border-b border-slate-800 hover:bg-slate-800/50 transition-colors text-left ${
                isActive ? 'bg-primary-500/20 border-l-4 border-l-primary-500' : ''
              }`}
            >
              <div className="flex items-start gap-2">
                {/* Collection Icon */}
                <div className="flex-shrink-0 mt-0.5">
                  {collection.type === 'archive' ? (
                    <Archive className="h-4 w-4 text-purple-400" />
                  ) : (
                    <Folder className="h-4 w-4 text-blue-400" />
                  )}
                </div>

                {/* Collection Info */}
                <div className="flex-1 min-w-0">
                  <p
                    className={`text-sm font-medium truncate ${
                      isActive ? 'text-primary-400' : 'text-white'
                    }`}
                  >
                    {collection.name}
                  </p>
                  <p className="text-xs text-slate-400 mt-0.5">
                    {(collection.imageCount ?? 0).toLocaleString()} images
                  </p>
                </div>
              </div>

              {/* Thumbnail Preview (if available) */}
              {collection.thumbnailBase64 && (
                <div className="mt-2">
                  <img
                    src={collection.thumbnailBase64}
                    alt={collection.name}
                    className="w-full h-20 object-cover rounded border border-slate-700"
                  />
                </div>
              )}
            </button>
          );
        })}
      </div>

      {/* Pagination */}
      {siblingsData && siblingsData.totalCount > pageSize && (
        <div className="flex-shrink-0 border-t border-slate-800 p-2 flex items-center justify-between">
          <button
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={page === 1}
            className="p-1 rounded text-slate-400 hover:text-white hover:bg-slate-700 disabled:opacity-30 disabled:cursor-not-allowed text-xs"
          >
            <ChevronLeft className="h-4 w-4" />
          </button>
          <span className="text-xs text-slate-400">
            {page} / {Math.ceil(siblingsData.totalCount / pageSize)}
          </span>
          <button
            onClick={() => setPage(p => p + 1)}
            disabled={page >= Math.ceil(siblingsData.totalCount / pageSize)}
            className="p-1 rounded text-slate-400 hover:text-white hover:bg-slate-700 disabled:opacity-30 disabled:cursor-not-allowed text-xs"
          >
            <ChevronRight className="h-4 w-4" />
          </button>
        </div>
      )}
    </div>
  );
};

export default CollectionNavigationSidebar;

