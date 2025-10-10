import { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useCollections } from '../hooks/useCollections';
import { Card, CardContent } from '../components/ui/Card';
import Button from '../components/ui/Button';
import LoadingSpinner from '../components/ui/LoadingSpinner';
import AddCollectionDialog from '../components/collections/AddCollectionDialog';
import BulkAddCollectionsDialog from '../components/collections/BulkAddCollectionsDialog';
import { 
  FolderOpen, 
  Archive, 
  Plus, 
  Search, 
  ChevronLeft, 
  ChevronRight,
  Grid3x3,
  List,
  ListTree,
  Maximize2,
  Minimize2,
  Zap
} from 'lucide-react';
import type { Collection } from '../services/types';

type ViewMode = 'grid' | 'list' | 'detail';
type CardSize = 'mini' | 'tiny' | 'small' | 'medium' | 'large' | 'xlarge';

/**
 * Collections Page
 * 
 * Enhanced with:
 * - 3 view modes (grid, list, detail)
 * - 6 card sizes (mini to xlarge)
 * - Compact mode
 * - Ultrawide support (32:9) - up to 12+ columns
 * - Full-width layout (no container constraint)
 */
const Collections: React.FC = () => {
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [limit, setLimit] = useState(() => 
    parseInt(localStorage.getItem('collectionsPageSize') || '100')
  );

  // View preferences (persisted to localStorage)
  const [viewMode, setViewMode] = useState<ViewMode>(() => 
    (localStorage.getItem('viewMode') as ViewMode) || 'grid'
  );
  const [cardSize, setCardSize] = useState<CardSize>(() => 
    (localStorage.getItem('cardSize') as CardSize) || 'medium'
  );
  const [compactMode, setCompactMode] = useState(() => 
    localStorage.getItem('compactMode') === 'true'
  );

  // Dialog states
  const [showAddDialog, setShowAddDialog] = useState(false);
  const [showBulkAddDialog, setShowBulkAddDialog] = useState(false);

  const { data, isLoading, refetch } = useCollections({ page, limit });

  // Filter collections by search query
  const filteredCollections = data?.data?.filter((collection) =>
    collection.name.toLowerCase().includes(search.toLowerCase()) ||
    collection.path.toLowerCase().includes(search.toLowerCase())
  ) ?? [];

  // Save preferences to localStorage
  const saveViewMode = useCallback((mode: ViewMode) => {
    setViewMode(mode);
    localStorage.setItem('viewMode', mode);
  }, []);

  const saveCardSize = useCallback((size: CardSize) => {
    setCardSize(size);
    localStorage.setItem('cardSize', size);
  }, []);

  const saveCompactMode = useCallback((compact: boolean) => {
    setCompactMode(compact);
    localStorage.setItem('compactMode', compact.toString());
  }, []);

  const savePageSize = useCallback((size: number) => {
    setLimit(size);
    localStorage.setItem('collectionsPageSize', size.toString());
    setPage(1); // Reset to first page when changing page size
  }, []);

  const handleCollectionClick = (collection: Collection) => {
    navigate(`/collections/${collection.id}`);
  };

  // Get grid column classes based on card size and compact mode
  const getGridColumns = () => {
    if (compactMode) {
      switch (cardSize) {
        case 'mini':
          return 'grid-cols-6 sm:grid-cols-8 md:grid-cols-10 lg:grid-cols-12 xl:grid-cols-14 2xl:grid-cols-16';
        case 'tiny':
          return 'grid-cols-5 sm:grid-cols-7 md:grid-cols-9 lg:grid-cols-10 xl:grid-cols-12 2xl:grid-cols-14';
        case 'small':
          return 'grid-cols-4 sm:grid-cols-6 md:grid-cols-8 lg:grid-cols-9 xl:grid-cols-10 2xl:grid-cols-12';
        case 'medium':
          return 'grid-cols-3 sm:grid-cols-4 md:grid-cols-6 lg:grid-cols-7 xl:grid-cols-8 2xl:grid-cols-10';
        case 'large':
          return 'grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 2xl:grid-cols-8';
        case 'xlarge':
          return 'grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 2xl:grid-cols-7';
      }
    } else {
      switch (cardSize) {
        case 'mini':
          return 'grid-cols-4 sm:grid-cols-6 md:grid-cols-8 lg:grid-cols-10 xl:grid-cols-12 2xl:grid-cols-14';
        case 'tiny':
          return 'grid-cols-4 sm:grid-cols-5 md:grid-cols-6 lg:grid-cols-8 xl:grid-cols-9 2xl:grid-cols-10';
        case 'small':
          return 'grid-cols-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-6 xl:grid-cols-7 2xl:grid-cols-8';
        case 'medium':
          return 'grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 2xl:grid-cols-7';
        case 'large':
          return 'grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-4 xl:grid-cols-5 2xl:grid-cols-6';
        case 'xlarge':
          return 'grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-3 xl:grid-cols-4 2xl:grid-cols-5';
      }
    }
  };

  return (
    <div className="h-full flex flex-col">
      {/* Optimized Toolbar - Single Row for Ultrawide */}
      <div className="flex-shrink-0 border-b border-slate-800 bg-slate-900/50 backdrop-blur">
        <div className="px-6 py-3">
          {/* Single Row: Title, Search, Controls, Actions - All in One Line */}
          <div className="flex items-center gap-4 lg:gap-6">
            {/* Left: Title & Count */}
            <div className="flex-shrink-0">
              <div className="flex items-center gap-2 lg:gap-3">
                <h1 className="text-lg lg:text-xl font-bold text-white">Collections</h1>
                <span className="text-xs lg:text-sm text-slate-400 bg-slate-800 px-2 py-1 rounded-full">
                  {data?.total || 0}
                </span>
              </div>
            </div>

            {/* Center: Search Bar - Takes available space */}
            <div className="relative flex-1 max-w-sm lg:max-w-md">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-slate-400" />
              <input
                type="text"
                placeholder="Search collections..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="w-full pl-9 pr-4 py-2 bg-slate-800 border border-slate-700 rounded-lg text-white placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent text-sm"
              />
            </div>

            {/* Action Button - Single with Dropdown */}
            <div className="relative flex-shrink-0">
              <div className="flex items-center gap-1 bg-slate-800 rounded-lg p-1">
                <Button
                  variant="primary"
                  size="sm"
                  onClick={() => setShowAddDialog(true)}
                  className="flex items-center space-x-1.5 rounded-md"
                >
                  <Plus className="h-4 w-4" />
                  <span className="hidden sm:inline">Add</span>
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => setShowBulkAddDialog(true)}
                  className="flex items-center space-x-1.5 rounded-md"
                  title="Bulk Add Collections"
                >
                  <Zap className="h-4 w-4" />
                  <span className="hidden lg:inline text-xs">Bulk</span>
                </Button>
              </div>
            </div>

            {/* Right: View Controls + Pagination - Compact & Efficient */}
            <div className="flex items-center gap-2 lg:gap-3 flex-shrink-0">
              {/* Pagination Controls - Compact */}
              <div className="flex items-center gap-2 bg-slate-800 rounded-lg px-2 py-1">
                  {/* Previous Button */}
                  <button
                    onClick={() => setPage(p => Math.max(1, p - 1))}
                    disabled={!data?.hasPrevious}
                    className="p-1 rounded text-slate-400 hover:text-white hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                    title="Previous Page"
                  >
                    <ChevronLeft className="h-4 w-4" />
                  </button>

                  {/* Page Number Input */}
                  <div className="flex items-center gap-1">
                    <input
                      type="number"
                      min="1"
                      max={data?.totalPages || 1}
                      value={page}
                      onChange={(e) => {
                        const newPage = parseInt(e.target.value);
                        if (newPage >= 1 && newPage <= (data?.totalPages || 1)) {
                          setPage(newPage);
                        }
                      }}
                      className="w-12 px-1 py-1 bg-slate-700 border border-slate-600 rounded text-white text-xs text-center focus:outline-none focus:ring-1 focus:ring-primary-500"
                      title="Go to page"
                    />
                    <span className="text-xs text-slate-400">/</span>
                    <span className="text-xs text-slate-400 w-6 text-center">{data?.totalPages || 1}</span>
                  </div>

                  {/* Next Button */}
                  <button
                    onClick={() => setPage(p => p + 1)}
                    disabled={!data?.hasNext}
                    className="p-1 rounded text-slate-400 hover:text-white hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                    title="Next Page"
                  >
                    <ChevronRight className="h-4 w-4" />
                  </button>
                </div>

              {/* View Mode - Compact Icons Only */}
              <div className="flex items-center gap-1 bg-slate-800 rounded-lg p-1">
                <button
                  onClick={() => saveViewMode('grid')}
                  className={`p-1.5 rounded transition-colors ${
                    viewMode === 'grid' 
                      ? 'bg-primary-500 text-white' 
                      : 'text-slate-400 hover:text-white hover:bg-slate-700'
                  }`}
                  title="Grid View"
                >
                  <Grid3x3 className="h-4 w-4" />
                </button>
                <button
                  onClick={() => saveViewMode('list')}
                  className={`p-1.5 rounded transition-colors ${
                    viewMode === 'list' 
                      ? 'bg-primary-500 text-white' 
                      : 'text-slate-400 hover:text-white hover:bg-slate-700'
                  }`}
                  title="List View"
                >
                  <List className="h-4 w-4" />
                </button>
                <button
                  onClick={() => saveViewMode('detail')}
                  className={`p-1.5 rounded transition-colors ${
                    viewMode === 'detail' 
                      ? 'bg-primary-500 text-white' 
                      : 'text-slate-400 hover:text-white hover:bg-slate-700'
                  }`}
                  title="Detail View"
                >
                  <ListTree className="h-4 w-4" />
                </button>
              </div>

              {/* Card Size - Compact Dropdown (only for grid mode) */}
              {viewMode === 'grid' && (
                <select
                  value={cardSize}
                  onChange={(e) => saveCardSize(e.target.value as CardSize)}
                  className="px-2 py-1.5 bg-slate-800 border border-slate-700 rounded text-white text-xs focus:outline-none focus:ring-1 focus:ring-primary-500 w-16 lg:w-20"
                  title="Card Size"
                >
                  <option value="mini">Mini</option>
                  <option value="tiny">Tiny</option>
                  <option value="small">Small</option>
                  <option value="medium">Medium</option>
                  <option value="large">Large</option>
                  <option value="xlarge">XLarge</option>
                </select>
              )}

              {/* Compact Mode Toggle (only for grid mode) */}
              {viewMode === 'grid' && (
                <button
                  onClick={() => saveCompactMode(!compactMode)}
                  className={`p-1.5 rounded transition-colors ${
                    compactMode 
                      ? 'bg-primary-500 text-white' 
                      : 'bg-slate-800 text-slate-400 hover:text-white hover:bg-slate-700'
                  }`}
                  title={compactMode ? 'Disable Compact Mode' : 'Enable Compact Mode'}
                >
                  {compactMode ? <Minimize2 className="h-4 w-4" /> : <Maximize2 className="h-4 w-4" />}
                </button>
              )}

              {/* Page Size Input */}
              <input
                type="number"
                min="1"
                max="1000"
                value={limit}
                onChange={(e) => {
                  const newValue = parseInt(e.target.value) || 100;
                  if (newValue >= 1 && newValue <= 1000) {
                    savePageSize(newValue);
                  }
                }}
                className="px-2 py-1.5 bg-slate-800 border border-slate-700 rounded text-white text-xs focus:outline-none focus:ring-1 focus:ring-primary-500 w-16 lg:w-20 text-center"
                title="Items Per Page"
                placeholder="100"
              />
            </div>
          </div>
        </div>
      </div>

      {/* Content Area - Full width, scrollable */}
      <div className="flex-1 overflow-y-auto">
        <div className="px-6 py-6">
          {/* Loading State */}
          {isLoading ? (
            <div className="flex items-center justify-center py-20">
              <LoadingSpinner text="Loading collections..." />
            </div>
          ) : filteredCollections.length === 0 ? (
            /* Empty State */
            <Card>
              <CardContent className="text-center py-12">
                <FolderOpen className="h-16 w-16 text-slate-600 mx-auto mb-4" />
                <p className="text-slate-400 mb-4">
                  {search ? 'No collections found matching your search' : 'No collections yet'}
                </p>
                {!search && (
                  <div className="flex items-center gap-3">
                    <Button 
                      variant="primary"
                      onClick={() => setShowAddDialog(true)}
                      className="flex items-center space-x-2"
                    >
                      <Plus className="h-4 w-4" />
                      <span>Add Your First Collection</span>
                    </Button>
                    <Button 
                      variant="ghost"
                      onClick={() => setShowBulkAddDialog(true)}
                      className="flex items-center space-x-2"
                    >
                      <Zap className="h-4 w-4" />
                      <span>Or Bulk Add</span>
                    </Button>
                  </div>
                )}
              </CardContent>
            </Card>
          ) : (
            /* Collections Display */
            <>
              {viewMode === 'grid' && (
                <div className={`grid ${getGridColumns()} gap-4`}>
                  {filteredCollections.map((collection) => (
                    <Card
                      key={collection.id}
                      hover
                      onClick={() => handleCollectionClick(collection)}
                      className="cursor-pointer overflow-hidden"
                    >
                      <CardContent className={compactMode ? 'p-3' : 'p-4'}>
                        {/* Thumbnail - Instant display with Base64 (no additional HTTP request) */}
                        <div className={`${compactMode ? 'mb-2' : 'mb-3'} relative bg-slate-800 rounded-lg overflow-hidden ${compactMode ? 'aspect-square' : 'aspect-video'}`}>
                          {collection.thumbnailBase64 ? (
                            <img 
                              src={collection.thumbnailBase64}
                              alt={`${collection.name} thumbnail`}
                              className="w-full h-full object-cover"
                              onError={(e) => {
                                // Fallback to icon if base64 decode fails (rare)
                                e.currentTarget.style.display = 'none';
                                const nextElement = e.currentTarget.nextElementSibling as HTMLElement;
                                if (nextElement) {
                                  nextElement.style.display = 'flex';
                                }
                              }}
                            />
                          ) : null}
                          
                          {/* Fallback Icon (always present but hidden when thumbnail exists) */}
                          <div 
                            className={`absolute inset-0 flex items-center justify-center ${collection.thumbnailBase64 ? 'hidden' : 'flex'}`}
                          >
                            {collection.type === 'archive' ? (
                              <Archive className={`${compactMode ? 'h-8 w-8' : 'h-12 w-12'} text-purple-500`} />
                            ) : (
                              <FolderOpen className={`${compactMode ? 'h-8 w-8' : 'h-12 w-12'} text-blue-500`} />
                            )}
                          </div>
                        </div>

                        {/* Info */}
                        <div className="space-y-1">
                          <h3 
                            className={`font-semibold text-white ${compactMode ? 'text-xs' : 'text-sm'} leading-tight`}
                            style={{
                              display: '-webkit-box',
                              WebkitLineClamp: compactMode ? 2 : 2,
                              WebkitBoxOrient: 'vertical',
                              overflow: 'hidden',
                            }}
                            title={collection.name}
                          >
                            {collection.name}
                          </h3>
                          
                          {!compactMode && (
                            <p className="text-xs text-slate-500 truncate" title={collection.path}>
                              {collection.path}
                            </p>
                          )}

                          <div className={`flex items-center justify-between ${compactMode ? 'text-xs' : 'text-sm'}`}>
                            <span className="text-slate-400">
                              <span className="text-white font-medium">{collection.imageCount.toLocaleString()}</span> images
                            </span>
                            {!compactMode && (
                              <span className="text-xs text-slate-500 capitalize">{collection.type}</span>
                            )}
                          </div>
                        </div>
                      </CardContent>
                    </Card>
                  ))}
                </div>
              )}

              {viewMode === 'list' && (
                <div className="space-y-2">
                  {filteredCollections.map((collection) => (
                    <div
                      key={collection.id}
                      onClick={() => handleCollectionClick(collection)}
                      className="flex items-center gap-4 p-3 bg-slate-900 hover:bg-slate-800 rounded-lg cursor-pointer transition-colors border border-slate-800"
                    >
                      {/* Icon */}
                      {collection.type === 'archive' ? (
                        <Archive className="h-6 w-6 text-purple-500 flex-shrink-0" />
                      ) : (
                        <FolderOpen className="h-6 w-6 text-blue-500 flex-shrink-0" />
                      )}

                      {/* Name & Path */}
                      <div className="flex-1 min-w-0">
                        <h3 className="font-medium text-white text-sm truncate">{collection.name}</h3>
                        <p className="text-xs text-slate-500 truncate">{collection.path}</p>
                      </div>

                      {/* Stats */}
                      <div className="flex items-center gap-6 text-sm text-slate-400">
                        <div className="text-right">
                          <span className="text-white font-medium">{collection.imageCount.toLocaleString()}</span> images
                        </div>
                        <div className="text-right">
                          <span className="text-white font-medium">{collection.thumbnailCount.toLocaleString()}</span> thumbs
                        </div>
                        <div className="text-xs text-slate-500 capitalize w-20 text-right">
                          {collection.type}
                        </div>
                        <div className="text-xs text-slate-500 w-24 text-right">
                          {new Date(collection.createdAt).toLocaleDateString()}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}

              {viewMode === 'detail' && (
                <div className="space-y-3">
                  {filteredCollections.map((collection) => (
                    <div
                      key={collection.id}
                      onClick={() => handleCollectionClick(collection)}
                      className="flex items-start gap-4 p-4 bg-slate-900 hover:bg-slate-800 rounded-lg cursor-pointer transition-colors border border-slate-800"
                    >
                      {/* Thumbnail */}
                      <div className="flex-shrink-0">
                        <div className="w-20 h-20 bg-slate-800 rounded-lg flex items-center justify-center">
                          {collection.type === 'archive' ? (
                            <Archive className="h-10 w-10 text-purple-500" />
                          ) : (
                            <FolderOpen className="h-10 w-10 text-blue-500" />
                          )}
                        </div>
                      </div>

                      {/* Info */}
                      <div className="flex-1 min-w-0">
                        <h3 className="font-semibold text-white text-base mb-1">{collection.name}</h3>
                        <p className="text-sm text-slate-500 truncate mb-3">{collection.path}</p>

                        {/* Stats Grid */}
                        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-sm">
                          <div>
                            <span className="text-slate-400">Images:</span>
                            <span className="text-white ml-2 font-medium">{collection.imageCount.toLocaleString()}</span>
                          </div>
                          <div>
                            <span className="text-slate-400">Thumbnails:</span>
                            <span className="text-white ml-2 font-medium">{collection.thumbnailCount.toLocaleString()}</span>
                          </div>
                          <div>
                            <span className="text-slate-400">Cache:</span>
                            <span className="text-white ml-2 font-medium">{collection.cacheImageCount.toLocaleString()}</span>
                          </div>
                          <div>
                            <span className="text-slate-400">Size:</span>
                            <span className="text-white ml-2 font-medium">
                              {(collection.totalSize / 1024 / 1024).toFixed(1)} MB
                            </span>
                          </div>
                        </div>

                        {/* Metadata */}
                        <div className="flex items-center gap-4 mt-3 text-xs text-slate-500">
                          <span className="capitalize">{collection.type}</span>
                          <span>•</span>
                          <span>Created {new Date(collection.createdAt).toLocaleDateString()}</span>
                          <span>•</span>
                          <span>Updated {new Date(collection.updatedAt).toLocaleDateString()}</span>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}

            </>
          )}
        </div>
      </div>

      {/* Dialogs */}
      <AddCollectionDialog
        isOpen={showAddDialog}
        onClose={() => setShowAddDialog(false)}
        onSuccess={() => refetch()}
      />
      <BulkAddCollectionsDialog
        isOpen={showBulkAddDialog}
        onClose={() => setShowBulkAddDialog(false)}
        onSuccess={() => refetch()}
      />
    </div>
  );
};

export default Collections;
