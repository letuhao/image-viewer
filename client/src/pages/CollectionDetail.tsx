import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useCollection } from '../hooks/useCollections';
import { useImages } from '../hooks/useImages';
import Button from '../components/ui/Button';
import LoadingSpinner from '../components/ui/LoadingSpinner';
import ImageGrid from '../components/ImageGrid';
import CollectionNavigationSidebar from '../components/collections/CollectionNavigationSidebar';
import { Pagination, PaginationSettings } from '../components/common/Pagination';
import { useUserSettings } from '../hooks/useSettings';
import { 
  ArrowLeft, 
  Play, 
  RotateCw, 
  Folder, 
  Archive, 
  Image as ImageIcon, 
  FileImage, 
  HardDrive,
  Calendar,
  Info,
  Grid3x3,
  List,
  ListTree
} from 'lucide-react';
import { formatDistanceToNow } from 'date-fns';

type ViewMode = 'grid' | 'list' | 'detail';
type CardSize = 'mini' | 'tiny' | 'small' | 'medium' | 'large' | 'xlarge';

/**
 * Collection Detail Page
 * 
 * Shows collection metadata and paginated thumbnail grid
 */
const CollectionDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  
  // Restore page from sessionStorage for THIS specific collection
  const sessionKey = `collectionDetail_${id}_page`;
  const [page, setPage] = useState(() => {
    const savedPage = sessionStorage.getItem(sessionKey);
    return savedPage ? parseInt(savedPage) : 1;
  });
  
  const [limit, setLimit] = useState(() => 
    parseInt(localStorage.getItem('collectionDetailPageSize') || '20')
  );
  const [viewMode, setViewMode] = useState<ViewMode>(() => 
    (localStorage.getItem('collectionDetailViewMode') as ViewMode) || 'grid'
  );
  const [cardSize, setCardSize] = useState<CardSize>(() => 
    (localStorage.getItem('collectionDetailCardSize') as CardSize) || 'medium'
  );
  
  // Get pagination settings from user settings (backend)
  const { data: userSettingsData } = useUserSettings();
  const paginationSettings: PaginationSettings = {
    showFirstLast: userSettingsData?.pagination?.showFirstLast ?? true,
    showPageNumbers: userSettingsData?.pagination?.showPageNumbers ?? true,
    pageNumbersToShow: userSettingsData?.pagination?.pageNumbersToShow ?? 5,
  };
  
  const { data: collection, isLoading } = useCollection(id!);
  const { data: imagesData, isLoading: imagesLoading } = useImages({
    collectionId: id!,
    page,
    limit,
  });

  // Save current page to sessionStorage whenever it changes (per collection)
  useEffect(() => {
    sessionStorage.setItem(sessionKey, page.toString());
  }, [page, sessionKey]);

  // Restore scroll position when returning to this collection
  useEffect(() => {
    const scrollKey = `collectionDetail_${id}_scroll`;
    const savedScrollPosition = sessionStorage.getItem(scrollKey);
    if (savedScrollPosition) {
      // Delay scroll restoration to ensure DOM is ready
      setTimeout(() => {
        window.scrollTo(0, parseInt(savedScrollPosition));
      }, 100);
    }

    // Save scroll position before leaving the page
    const handleScroll = () => {
      sessionStorage.setItem(scrollKey, window.scrollY.toString());
    };

    window.addEventListener('scroll', handleScroll, { passive: true });
    
    return () => {
      window.removeEventListener('scroll', handleScroll);
    };
  }, [id]);

  if (isLoading) {
    return <LoadingSpinner text="Loading collection..." />;
  }

  if (!collection) {
    return (
      <div className="container mx-auto px-4 py-6">
        <div className="text-center py-12">
          <p className="text-slate-400 mb-4">Collection not found</p>
          <Button onClick={() => navigate('/collections')}>
            Back to Collections
          </Button>
        </div>
      </div>
    );
  }

  const images = imagesData?.data || [];
  const pagination = imagesData ? {
    totalPages: imagesData.totalPages,
    hasPrevious: imagesData.hasPreviousPage,
    hasNext: imagesData.hasNextPage,
    page: imagesData.page,
    total: imagesData.totalCount
  } : null;

  const formatBytes = (bytes: number) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  };

  const formatDate = (dateString: string) => {
    try {
      const date = new Date(dateString);
      return isNaN(date.getTime()) ? 'Unknown' : formatDistanceToNow(date, { addSuffix: true });
    } catch {
      return 'Unknown';
    }
  };

  // Save view preferences to localStorage
  const saveViewMode = (mode: ViewMode) => {
    setViewMode(mode);
    localStorage.setItem('collectionDetailViewMode', mode);
  };

  const saveCardSize = (size: CardSize) => {
    setCardSize(size);
    localStorage.setItem('collectionDetailCardSize', size);
  };

  const savePageSize = (size: number) => {
    setLimit(size);
    localStorage.setItem('collectionDetailPageSize', size.toString());
    setPage(1); // Reset to first page when changing page size
  };

  // Get grid classes based on card size (same as Collections page)
  const getGridClasses = () => {
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
  };

  return (
    <div className="h-full flex">
      {/* Collection Navigation Sidebar */}
      <CollectionNavigationSidebar 
        collectionId={id!}
        sortBy="updatedAt"
        sortDirection="desc"
      />
      
      {/* Main Content */}
      <div className="flex-1 flex flex-col">
        {/* Compact Header */}
        <div className="flex-shrink-0 border-b border-slate-800 bg-slate-900/50 backdrop-blur">
          <div className="px-6 py-3">
            {/* Main Header Row */}
            <div className="flex items-center justify-between mb-3">
              <div className="flex items-center space-x-3">
                <Button
                  variant="ghost"
                  icon={<ArrowLeft className="h-4 w-4" />}
                  onClick={() => navigate('/collections')}
                  size="sm"
                >
                  Back
                </Button>
                <div className="flex items-center space-x-2">
                  {collection.type === 'archive' ? (
                    <Archive className="h-6 w-6 text-purple-500" />
                  ) : (
                    <Folder className="h-6 w-6 text-blue-500" />
                  )}
                  <div>
                    <h1 className="text-lg font-bold text-white">{collection.name}</h1>
                    <p className="text-slate-400 text-xs truncate max-w-md">{collection.path}</p>
                  </div>
                </div>
              </div>

              <div className="flex items-center space-x-2">
                <Button variant="secondary" icon={<RotateCw className="h-4 w-4" />} size="sm">
                  Rescan
                </Button>
                <Button
                  icon={<Play className="h-4 w-4" />}
                  onClick={() => {
                    if (images && images.length > 0) {
                      navigate(`/collections/${id}/viewer?imageId=${images[0].id}`);
                    }
                  }}
                  disabled={!images || images.length === 0}
                  size="sm"
                >
                  Open Viewer
                </Button>
              </div>
            </div>

            {/* Compact Metadata Row + Controls */}
            <div className="flex items-center justify-between">
              {/* Compact Metadata */}
              <div className="flex items-center space-x-6">
                <div className="flex items-center space-x-2">
                  <ImageIcon className="h-4 w-4 text-blue-400" />
                  <span className="text-sm font-medium text-white">{(collection.imageCount ?? 0).toLocaleString()}</span>
                  <span className="text-xs text-slate-400">images</span>
                </div>
                <div className="flex items-center space-x-2">
                  <FileImage className="h-4 w-4 text-green-400" />
                  <span className="text-sm font-medium text-white">{(collection.thumbnailCount ?? 0).toLocaleString()}</span>
                  <span className="text-xs text-slate-400">thumbs</span>
                </div>
                <div className="flex items-center space-x-2">
                  <HardDrive className="h-4 w-4 text-purple-400" />
                  <span className="text-sm font-medium text-white">{(collection.cacheImageCount ?? 0).toLocaleString()}</span>
                  <span className="text-xs text-slate-400">cached</span>
                </div>
                <div className="flex items-center space-x-2">
                  <Calendar className="h-4 w-4 text-orange-400" />
                  <span className="text-xs text-slate-400">{collection.updatedAt ? formatDate(collection.updatedAt) : 'Unknown'}</span>
                </div>
              </div>

              {/* View Mode + Pagination Controls */}
              <div className="flex items-center gap-2">
                {/* Pagination Controls */}
                <div className="flex items-center gap-2 bg-slate-800 rounded-lg px-2 py-1">
                    <Pagination
                      currentPage={page}
                      totalPages={pagination?.totalPages || 1}
                      onPageChange={setPage}
                      hasPrevious={pagination?.hasPrevious}
                      hasNext={pagination?.hasNext}
                      settings={paginationSettings}
                      compact={true}
                    />
                </div>

                {/* View Mode Controls */}
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

                {/* Card Size Selector (only for grid mode) */}
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

                {/* Page Size Input */}
                <input
                  type="number"
                  min="1"
                  max="1000"
                  value={limit}
                  onChange={(e) => {
                    const newValue = parseInt(e.target.value) || 20;
                    if (newValue >= 1 && newValue <= 1000) {
                      savePageSize(newValue);
                    }
                  }}
                  className="px-2 py-1.5 bg-slate-800 border border-slate-700 rounded text-white text-xs focus:outline-none focus:ring-1 focus:ring-primary-500 w-16 lg:w-20 text-center"
                  title="Items Per Page"
                  placeholder="20"
                />
              </div>
            </div>
          </div>
        </div>

      {/* Image Content */}
      <div className="flex-1 overflow-y-auto px-6 py-4">
        {imagesLoading ? (
          <LoadingSpinner text="Loading images..." />
        ) : images.length > 0 ? (
          <div className="space-y-4">
            {/* View Mode Content */}
            {viewMode === 'grid' && (
              <ImageGrid
                collectionId={id!}
                images={images}
                isLoading={false}
                gridClasses={getGridClasses()}
              />
            )}
            
            {viewMode === 'list' && (
              <div className="space-y-2">
                {images.map((image) => (
                  <div key={image.id} className="flex items-center space-x-4 p-3 bg-slate-800/50 border border-slate-700 rounded-lg hover:bg-slate-800/70 transition-colors">
                    <div className="flex-shrink-0">
                      <img
                        src={`/api/v1/images/${id}/${image.id}/thumbnail`}
                        alt={image.fileName}
                        className="w-16 h-16 object-cover rounded border border-slate-600"
                      />
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-white truncate">{image.fileName}</p>
                      <p className="text-xs text-slate-400">
                        {formatBytes(image.fileSize)} • {image.width}×{image.height}
                      </p>
                    </div>
                    <div className="flex-shrink-0 text-xs text-slate-500">
                      {image.createdAt ? formatDate(image.createdAt) : 'Unknown'}
                    </div>
                  </div>
                ))}
              </div>
            )}
            
            {viewMode === 'detail' && (
              <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-4">
                {images.map((image) => (
                  <div key={image.id} className="bg-slate-800/50 border border-slate-700 rounded-lg p-4 hover:bg-slate-800/70 transition-colors">
                    <div className="mb-3">
                      <img
                        src={`/api/v1/images/${id}/${image.id}/thumbnail`}
                        alt={image.fileName}
                        className="w-full h-48 object-cover rounded border border-slate-600"
                      />
                    </div>
                    <div className="space-y-2">
                      <p className="text-sm font-medium text-white truncate">{image.fileName}</p>
                      <div className="grid grid-cols-2 gap-2 text-xs">
                        <div>
                          <span className="text-slate-500">Size:</span>
                          <span className="text-white ml-1">{formatBytes(image.fileSize)}</span>
                        </div>
                        <div>
                          <span className="text-slate-500">Dimensions:</span>
                          <span className="text-white ml-1">{image.width}×{image.height}</span>
                        </div>
                        <div>
                          <span className="text-slate-500">Format:</span>
                          <span className="text-white ml-1">{image.format?.toUpperCase()}</span>
                        </div>
                        <div>
                          <span className="text-slate-500">Created:</span>
                          <span className="text-white ml-1">{image.createdAt ? formatDate(image.createdAt) : 'Unknown'}</span>
                        </div>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        ) : (
          <div className="text-center py-12">
            <ImageIcon className="h-12 w-12 text-slate-600 mx-auto mb-4" />
            <p className="text-slate-400">No images found in this collection</p>
            <p className="text-sm text-slate-500 mt-1">Try rescanning the collection</p>
          </div>
        )}
      </div>

        {/* Status Bar */}
        {pagination && (
          <div className="flex-shrink-0 border-t border-slate-800 px-6 py-2 bg-slate-900/30">
            <div className="text-xs text-slate-400">
              Showing {((pagination.page - 1) * limit) + 1}-{Math.min(pagination.page * limit, pagination.total)} of {pagination.total} images
            </div>
          </div>
        )}
      </div>
      {/* End Main Content */}
    </div>
  );
};

export default CollectionDetail;

