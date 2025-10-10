import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useCollection } from '../hooks/useCollections';
import { useImages } from '../hooks/useImages';
import Button from '../components/ui/Button';
import LoadingSpinner from '../components/ui/LoadingSpinner';
import ImageGrid from '../components/ImageGrid';
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
  Info
} from 'lucide-react';
import { formatDistanceToNow } from 'date-fns';

/**
 * Collection Detail Page
 * 
 * Shows collection metadata and paginated thumbnail grid
 */
const CollectionDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [limit] = useState(50); // Items per page
  
  const { data: collection, isLoading } = useCollection(id!);
  const { data: imagesData, isLoading: imagesLoading } = useImages({
    collectionId: id!,
    page,
    limit,
  });

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
  const pagination = imagesData?.pagination;

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

  return (
    <div className="h-full flex flex-col">
      {/* Header */}
      <div className="flex-shrink-0 border-b border-slate-800 bg-slate-900/50 backdrop-blur">
        <div className="px-6 py-4">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center space-x-4">
              <Button
                variant="ghost"
                icon={<ArrowLeft className="h-5 w-5" />}
                onClick={() => navigate('/collections')}
              >
                Back
              </Button>
              <div className="flex items-center space-x-3">
                {collection.type === 'archive' ? (
                  <Archive className="h-8 w-8 text-purple-500" />
                ) : (
                  <Folder className="h-8 w-8 text-blue-500" />
                )}
                <div>
                  <h1 className="text-2xl font-bold text-white">{collection.name}</h1>
                  <p className="text-slate-400 text-sm">{collection.path}</p>
                </div>
              </div>
            </div>

            <div className="flex items-center space-x-2">
              <Button variant="secondary" icon={<RotateCw className="h-4 w-4" />}>
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
              >
                Open Viewer
              </Button>
            </div>
          </div>

          {/* Metadata Cards */}
          <div className="grid grid-cols-4 gap-4">
            {/* Images Card */}
            <div className="bg-slate-800/50 border border-slate-700 rounded-lg p-4">
              <div className="flex items-center justify-between mb-2">
                <ImageIcon className="h-5 w-5 text-blue-400" />
                <span className="text-xs text-slate-500">IMAGES</span>
              </div>
              <div className="text-2xl font-bold text-white">
                {(collection.imageCount ?? 0).toLocaleString()}
              </div>
              <div className="text-xs text-slate-400 mt-1">
                {formatBytes(collection.totalSize ?? 0)} total
              </div>
            </div>

            {/* Thumbnails Card */}
            <div className="bg-slate-800/50 border border-slate-700 rounded-lg p-4">
              <div className="flex items-center justify-between mb-2">
                <FileImage className="h-5 w-5 text-green-400" />
                <span className="text-xs text-slate-500">THUMBNAILS</span>
              </div>
              <div className="text-2xl font-bold text-white">
                {(collection.thumbnailCount ?? 0).toLocaleString()}
              </div>
              <div className="text-xs text-slate-400 mt-1">
                {Math.round(((collection.thumbnailCount ?? 0) / Math.max(collection.imageCount ?? 1, 1)) * 100)}% generated
              </div>
            </div>

            {/* Cache Card */}
            <div className="bg-slate-800/50 border border-slate-700 rounded-lg p-4">
              <div className="flex items-center justify-between mb-2">
                <HardDrive className="h-5 w-5 text-purple-400" />
                <span className="text-xs text-slate-500">CACHE</span>
              </div>
              <div className="text-2xl font-bold text-white">
                {(collection.cacheImageCount ?? 0).toLocaleString()}
              </div>
              <div className="text-xs text-slate-400 mt-1">
                {Math.round(((collection.cacheImageCount ?? 0) / Math.max(collection.imageCount ?? 1, 1)) * 100)}% cached
              </div>
            </div>

            {/* Info Card */}
            <div className="bg-slate-800/50 border border-slate-700 rounded-lg p-4">
              <div className="flex items-center justify-between mb-2">
                <Calendar className="h-5 w-5 text-orange-400" />
                <span className="text-xs text-slate-500">UPDATED</span>
              </div>
              <div className="text-sm font-medium text-white">
                {collection.updatedAt ? formatDate(collection.updatedAt) : 'Unknown'}
              </div>
              <div className="text-xs text-slate-400 mt-1">
                Type: {collection.type ?? 'unknown'}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Image Grid */}
      <div className="flex-1 overflow-y-auto px-6 py-4">
        {imagesLoading ? (
          <LoadingSpinner text="Loading images..." />
        ) : images.length > 0 ? (
          <ImageGrid
            collectionId={id!}
            images={images}
            isLoading={false}
          />
        ) : (
          <div className="text-center py-12">
            <ImageIcon className="h-12 w-12 text-slate-600 mx-auto mb-4" />
            <p className="text-slate-400">No images found in this collection</p>
            <p className="text-sm text-slate-500 mt-1">Try rescanning the collection</p>
          </div>
        )}
      </div>

      {/* Pagination */}
      {pagination && pagination.totalPages > 1 && (
        <div className="flex-shrink-0 border-t border-slate-800 px-6 py-4 bg-slate-900/30">
          <div className="flex items-center justify-between">
            <div className="text-sm text-slate-400">
              Showing {((pagination.page - 1) * limit) + 1}-{Math.min(pagination.page * limit, pagination.total)} of {pagination.total} images
            </div>
            <div className="flex items-center space-x-3">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setPage(1)}
                disabled={!pagination.hasPrevious}
              >
                First
              </Button>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setPage(page - 1)}
                disabled={!pagination.hasPrevious}
              >
                Previous
              </Button>
              
              {/* Page Input */}
              <div className="flex items-center space-x-2">
                <span className="text-sm text-slate-400">Page</span>
                <input
                  type="number"
                  min={1}
                  max={pagination.totalPages}
                  value={page}
                  onChange={(e) => {
                    const newPage = parseInt(e.target.value);
                    if (newPage >= 1 && newPage <= pagination.totalPages) {
                      setPage(newPage);
                    }
                  }}
                  className="w-16 px-2 py-1 bg-slate-800 border border-slate-700 rounded text-white text-sm text-center focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
                <span className="text-sm text-slate-400">of {pagination.totalPages}</span>
              </div>
              
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setPage(page + 1)}
                disabled={!pagination.hasNext}
              >
                Next
              </Button>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setPage(pagination.totalPages)}
                disabled={!pagination.hasNext}
              >
                Last
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default CollectionDetail;

