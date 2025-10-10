import { useState, useEffect, useCallback, useRef } from 'react';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import { useImages, useImage } from '../hooks/useImages';
import { useCollection } from '../hooks/useCollections';
import LoadingSpinner from '../components/ui/LoadingSpinner';
import {
  X,
  ChevronLeft,
  ChevronRight,
  ZoomIn,
  ZoomOut,
  RotateCw,
  Play,
  Pause,
  Grid,
} from 'lucide-react';

/**
 * Image Viewer
 * 
 * Full-screen image viewer with:
 * - Keyboard navigation (Arrow keys, Esc)
 * - Zoom controls
 * - Slideshow mode
 * - Image info overlay
 */
const ImageViewer: React.FC = () => {
  const { id: collectionId } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const imageId = searchParams.get('imageId');
  const { data: collection } = useCollection(collectionId!);
  const { data: imagesData } = useImages({ collectionId: collectionId!, limit: 1000 });
  const { data: currentImage } = useImage(collectionId!, imageId!);

  const [zoom, setZoom] = useState(1);
  const [rotation, setRotation] = useState(0);
  const [isSlideshow, setIsSlideshow] = useState(false);
  const [showInfo, setShowInfo] = useState(false);
  const slideshowRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const images = imagesData?.data || [];
  const currentIndex = images.findIndex((img) => img.id === imageId);

  // Navigate to next/previous image
  const navigateToImage = useCallback(
    (direction: 'next' | 'prev') => {
      if (images.length === 0) return;

      let newIndex = currentIndex;
      if (direction === 'next') {
        newIndex = (currentIndex + 1) % images.length;
      } else {
        newIndex = (currentIndex - 1 + images.length) % images.length;
      }

      const newImageId = images[newIndex].id;
      navigate(`/collections/${collectionId}/viewer?imageId=${newImageId}`, { replace: true });
      
      // Reset zoom and rotation when changing images
      setZoom(1);
      setRotation(0);
    },
    [images, currentIndex, collectionId, navigate]
  );

  // Keyboard navigation
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      switch (e.key) {
        case 'Escape':
          navigate(`/collections/${collectionId}`);
          break;
        case 'ArrowLeft':
          navigateToImage('prev');
          break;
        case 'ArrowRight':
          navigateToImage('next');
          break;
        case '+':
        case '=':
          setZoom((z) => Math.min(z + 0.25, 5));
          break;
        case '-':
          setZoom((z) => Math.max(z - 0.25, 0.25));
          break;
        case '0':
          setZoom(1);
          setRotation(0);
          break;
        case 'r':
          setRotation((r) => (r + 90) % 360);
          break;
        case 'i':
          setShowInfo((s) => !s);
          break;
        case ' ':
          e.preventDefault();
          setIsSlideshow((s) => !s);
          break;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [collectionId, navigate, navigateToImage]);

  // Slideshow
  useEffect(() => {
    if (isSlideshow) {
      slideshowRef.current = setInterval(() => {
        navigateToImage('next');
      }, 3000);
    } else {
      if (slideshowRef.current) {
        clearInterval(slideshowRef.current);
      }
    }

    return () => {
      if (slideshowRef.current) {
        clearInterval(slideshowRef.current);
      }
    };
  }, [isSlideshow, navigateToImage]);

  if (!currentImage) {
    return <LoadingSpinner fullScreen text="Loading image..." />;
  }

  return (
    <div className="fixed inset-0 bg-black z-50 flex flex-col">
      {/* Header */}
      <div className="absolute top-0 left-0 right-0 z-10 bg-gradient-to-b from-black/80 to-transparent p-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-4">
            <button
              onClick={() => navigate(`/collections/${collectionId}`)}
              className="p-2 hover:bg-white/10 rounded-lg transition-colors"
            >
              <X className="h-6 w-6 text-white" />
            </button>
            <div className="text-white">
              <h2 className="font-semibold">{currentImage.filename}</h2>
              <p className="text-sm text-slate-300">
                {currentIndex + 1} of {images.length} • {currentImage.width} × {currentImage.height}
              </p>
            </div>
          </div>

          <div className="flex items-center space-x-2">
            <button
              onClick={() => setShowInfo(!showInfo)}
              className="p-2 hover:bg-white/10 rounded-lg transition-colors"
              title="Toggle Info (I)"
            >
              <Grid className="h-5 w-5 text-white" />
            </button>
            <button
              onClick={() => setRotation((r) => (r + 90) % 360)}
              className="p-2 hover:bg-white/10 rounded-lg transition-colors"
              title="Rotate (R)"
            >
              <RotateCw className="h-5 w-5 text-white" />
            </button>
            <button
              onClick={() => setZoom((z) => Math.max(z - 0.25, 0.25))}
              className="p-2 hover:bg-white/10 rounded-lg transition-colors"
              title="Zoom Out (-)"
            >
              <ZoomOut className="h-5 w-5 text-white" />
            </button>
            <span className="text-white text-sm px-2">{Math.round(zoom * 100)}%</span>
            <button
              onClick={() => setZoom((z) => Math.min(z + 0.25, 5))}
              className="p-2 hover:bg-white/10 rounded-lg transition-colors"
              title="Zoom In (+)"
            >
              <ZoomIn className="h-5 w-5 text-white" />
            </button>
            <button
              onClick={() => setIsSlideshow(!isSlideshow)}
              className={`p-2 hover:bg-white/10 rounded-lg transition-colors ${
                isSlideshow ? 'bg-primary-500' : ''
              }`}
              title="Slideshow (Space)"
            >
              {isSlideshow ? (
                <Pause className="h-5 w-5 text-white" />
              ) : (
                <Play className="h-5 w-5 text-white" />
              )}
            </button>
          </div>
        </div>
      </div>

      {/* Main Image */}
      <div className="flex-1 flex items-center justify-center p-4">
        <img
          src={`/api/v1/images/${collectionId}/${imageId}/file`}
          alt={currentImage.filename}
          className="max-w-full max-h-full object-contain transition-transform duration-200"
          style={{
            transform: `scale(${zoom}) rotate(${rotation}deg)`,
          }}
        />
      </div>

      {/* Navigation Buttons */}
      <button
        onClick={() => navigateToImage('prev')}
        className="absolute left-4 top-1/2 -translate-y-1/2 p-3 bg-black/50 hover:bg-black/70 rounded-full transition-colors"
        title="Previous (←)"
      >
        <ChevronLeft className="h-8 w-8 text-white" />
      </button>
      <button
        onClick={() => navigateToImage('next')}
        className="absolute right-4 top-1/2 -translate-y-1/2 p-3 bg-black/50 hover:bg-black/70 rounded-full transition-colors"
        title="Next (→)"
      >
        <ChevronRight className="h-8 w-8 text-white" />
      </button>

      {/* Info Panel */}
      {showInfo && (
        <div className="absolute bottom-0 left-0 right-0 bg-gradient-to-t from-black/80 to-transparent p-6">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-white">
            <div>
              <p className="text-sm text-slate-400">Filename</p>
              <p className="font-medium">{currentImage.filename}</p>
            </div>
            <div>
              <p className="text-sm text-slate-400">Dimensions</p>
              <p className="font-medium">
                {currentImage.width} × {currentImage.height}
              </p>
            </div>
            <div>
              <p className="text-sm text-slate-400">Size</p>
              <p className="font-medium">{(currentImage.fileSize / 1024 / 1024).toFixed(2)} MB</p>
            </div>
            <div>
              <p className="text-sm text-slate-400">Path</p>
              <p className="font-medium truncate">{currentImage.relativePath}</p>
            </div>
          </div>
        </div>
      )}

      {/* Keyboard Shortcuts Hint */}
      <div className="absolute bottom-4 left-1/2 -translate-x-1/2 text-center text-white/60 text-sm">
        <p>← → Navigate • Esc Close • +/- Zoom • R Rotate • I Info • Space Slideshow</p>
      </div>
    </div>
  );
};

export default ImageViewer;
