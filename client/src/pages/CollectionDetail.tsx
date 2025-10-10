import { useParams, useNavigate } from 'react-router-dom';
import { useCollection } from '../hooks/useCollections';
import { useImages } from '../hooks/useImages';
import Button from '../components/ui/Button';
import LoadingSpinner from '../components/ui/LoadingSpinner';
import ImageGrid from '../components/ImageGrid';
import { ArrowLeft, Play, RotateCw } from 'lucide-react';

/**
 * Collection Detail Page
 * 
 * Shows collection details and image grid
 * Placeholder for now - will add virtual scrolling image grid
 */
const CollectionDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: collection, isLoading } = useCollection(id!);
  const { data: imagesData, isLoading: imagesLoading } = useImages({
    collectionId: id!,
    limit: 1000, // Load many images at once for virtual scrolling
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

  return (
    <div className="container mx-auto px-4 py-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <Button
            variant="ghost"
            icon={<ArrowLeft className="h-5 w-5" />}
            onClick={() => navigate('/collections')}
          >
            Back
          </Button>
          <div>
            <h1 className="text-3xl font-bold text-white">{collection.name}</h1>
            <p className="text-slate-400 text-sm">{collection.path}</p>
          </div>
        </div>

        <div className="flex items-center space-x-2">
          <Button variant="secondary" icon={<RotateCw className="h-4 w-4" />}>
            Rescan
          </Button>
          <Button
            icon={<Play className="h-4 w-4" />}
            onClick={() => {
              if (imagesData?.data && imagesData.data.length > 0) {
                navigate(`/collections/${id}/viewer?imageId=${imagesData.data[0].id}`);
              }
            }}
            disabled={!imagesData?.data || imagesData.data.length === 0}
          >
            Open Viewer
          </Button>
        </div>
      </div>

      {/* Stats */}
      <div className="flex items-center space-x-6 text-sm">
        <div className="text-slate-400">
          <span className="text-white font-medium">
            {collection.imageCount.toLocaleString()}
          </span>
          {' '}Images
        </div>
        <div className="text-slate-400">
          <span className="text-white font-medium">
            {collection.thumbnailCount.toLocaleString()}
          </span>
          {' '}Thumbnails
        </div>
        <div className="text-slate-400">
          <span className="text-white font-medium">
            {(collection.totalSize / 1024 / 1024).toFixed(2)} MB
          </span>
          {' '}Total Size
        </div>
      </div>

      {/* Image Grid */}
      <div className="flex-1 min-h-0">
        <ImageGrid
          collectionId={id!}
          images={imagesData?.data || []}
          isLoading={imagesLoading}
        />
      </div>
    </div>
  );
};

export default CollectionDetail;

