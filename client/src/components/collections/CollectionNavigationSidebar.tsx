import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ChevronLeft, ChevronRight, Folder, Archive } from 'lucide-react';
import { useCollectionNavigation, useCollectionSiblings } from '../../hooks/useCollectionNavigation';
import LoadingSpinner from '../ui/LoadingSpinner';

interface CollectionNavigationSidebarProps {
  collectionId: string;
  sortBy?: string;
  sortDirection?: string;
  onNavigate?: (collectionId: string, firstImageId?: string) => void;
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

  const handleCollectionClick = (id: string, firstImageId?: string) => {
    if (onNavigate) {
      onNavigate(id, firstImageId);
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

      {/* Siblings List - Card View with Thumbnails */}
      <div className="flex-1 overflow-y-auto p-3 space-y-3">
        {siblingsData?.siblings.map((collection: any) => {
          const isActive = collection.id === collectionId;
          return (
            <button
              key={collection.id}
              onClick={() => handleCollectionClick(collection.id, collection.firstImageId)}
              className={`w-full group relative overflow-hidden rounded-lg transition-all ${
                isActive 
                  ? 'ring-2 ring-primary-500 shadow-lg shadow-primary-500/50' 
                  : 'hover:ring-2 hover:ring-slate-600'
              }`}
            >
              {/* Thumbnail Background */}
              <div className="relative aspect-video bg-slate-800">
                {collection.thumbnailBase64 ? (
                  <img
                    src={collection.thumbnailBase64}
                    alt={collection.name}
                    className="w-full h-full object-cover transition-transform duration-300 group-hover:scale-105"
                    loading="lazy"
                  />
                ) : (
                  <div className="absolute inset-0 flex items-center justify-center">
                    {collection.type === 'archive' ? (
                      <Archive className="h-10 w-10 text-slate-600" />
                    ) : (
                      <Folder className="h-10 w-10 text-slate-600" />
                    )}
                  </div>
                )}
                
                {/* Current Badge */}
                {isActive && (
                  <div className="absolute top-2 right-2 px-2 py-1 bg-primary-500 text-white text-xs font-bold rounded">
                    Current
                  </div>
                )}

                {/* Gradient Overlay */}
                <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-transparent to-transparent"></div>
                
                {/* Collection Info Overlay */}
                <div className="absolute bottom-0 left-0 right-0 p-2">
                  <div className="flex items-center gap-1 mb-1">
                    {collection.type === 'archive' ? (
                      <Archive className="h-3 w-3 text-purple-400 flex-shrink-0" />
                    ) : (
                      <Folder className="h-3 w-3 text-blue-400 flex-shrink-0" />
                    )}
                    <p
                      className={`text-xs font-semibold truncate ${
                        isActive ? 'text-primary-300' : 'text-white'
                      }`}
                      title={collection.name}
                    >
                      {collection.name}
                    </p>
                  </div>
                  <p className="text-xs text-slate-300">
                    {(collection.imageCount ?? 0).toLocaleString()} images
                  </p>
                </div>
              </div>
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

