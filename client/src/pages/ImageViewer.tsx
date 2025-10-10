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
  Maximize2,
  Monitor,
  Layout,
  Settings,
  Info,
  HelpCircle,
  Shuffle,
  Expand,
  Scan,
  ArrowDownUp,
} from 'lucide-react';

/**
 * View modes for the image viewer
 */
type ViewMode = 'single' | 'double' | 'triple' | 'quad';

/**
 * Navigation modes
 */
type NavigationMode = 'paging' | 'scroll';

/**
 * Image Viewer
 * 
 * Full-screen image viewer with:
 * - Multiple view modes (single, double, triple, quad)
 * - Keyboard navigation (Arrow keys, Esc)
 * - Zoom controls
 * - Slideshow mode
 * - Image info overlay
 * - Fullscreen support
 */
const ImageViewer: React.FC = () => {
  const { id: collectionId } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const initialImageId = searchParams.get('imageId');
  const { data: collection } = useCollection(collectionId!);
  const { data: imagesData } = useImages({ collectionId: collectionId!, limit: 1000 });
  
  // Use local state for current image ID to avoid URL changes on every navigation
  const [currentImageId, setCurrentImageId] = useState(initialImageId || '');
  
  // Only fetch initial image for metadata, then use cached list
  const { data: initialImage } = useImage(collectionId!, initialImageId!);

  const [zoom, setZoom] = useState(1);
  const [rotation, setRotation] = useState(0);
  const [isSlideshow, setIsSlideshow] = useState(false);
  const [showInfo, setShowInfo] = useState(false);
  const [showHelp, setShowHelp] = useState(false);
  const [viewMode, setViewMode] = useState<ViewMode>(() => 
    (localStorage.getItem('imageViewerViewMode') as ViewMode) || 'single'
  );
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [slideshowInterval, setSlideshowInterval] = useState(() => 
    parseInt(localStorage.getItem('slideshowInterval') || '3000')
  );
  const [isShuffleMode, setIsShuffleMode] = useState(false);
  const [panPosition, setPanPosition] = useState({ x: 0, y: 0 });
  const [imageLoading, setImageLoading] = useState(true);
  const [imageError, setImageError] = useState(false);
  const [fitToScreen, setFitToScreen] = useState(() => 
    localStorage.getItem('imageViewerFitToScreen') === 'true'
  );
  const [navigationMode, setNavigationMode] = useState<NavigationMode>(() => 
    (localStorage.getItem('imageViewerNavigationMode') as NavigationMode) || 'paging'
  );
  const slideshowRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const imageContainerRef = useRef<HTMLDivElement>(null);

  const images = imagesData?.data || [];
  const currentIndex = images.findIndex((img) => img.id === currentImageId);
  const currentImage = currentIndex >= 0 ? images[currentIndex] : null;
  
  // Sync currentImageId with URL parameter
  useEffect(() => {
    if (initialImageId && initialImageId !== currentImageId) {
      setCurrentImageId(initialImageId);
    }
  }, [initialImageId]);
  
  // Handle invalid currentIndex
  useEffect(() => {
    if (images.length > 0 && currentIndex === -1 && currentImageId) {
      // Image not found, set to first image
      setCurrentImageId(images[0].id);
    }
  }, [currentIndex, images, currentImageId]);

  // Reset loading/error state when image changes
  useEffect(() => {
    setImageLoading(true);
    setImageError(false);
  }, [currentImageId]);

  // Save view mode to localStorage
  const saveViewMode = useCallback((mode: ViewMode) => {
    setViewMode(mode);
    localStorage.setItem('imageViewerViewMode', mode);
  }, []);

  // Toggle fit to screen mode
  const toggleFitToScreen = useCallback(() => {
    const newValue = !fitToScreen;
    setFitToScreen(newValue);
    localStorage.setItem('imageViewerFitToScreen', newValue.toString());
  }, [fitToScreen]);

  // Toggle navigation mode
  const toggleNavigationMode = useCallback(() => {
    const newMode: NavigationMode = navigationMode === 'paging' ? 'scroll' : 'paging';
    setNavigationMode(newMode);
    localStorage.setItem('imageViewerNavigationMode', newMode);
  }, [navigationMode]);

  // Get image class based on fit mode and screen orientation
  const getImageClass = useCallback(() => {
    if (!fitToScreen) {
      return "max-w-full max-h-[calc(100vh-8rem)] object-contain transition-transform duration-200";
    }

    // Fit to screen mode: use screen orientation, not image orientation
    // Landscape screen (like 32:9 ultrawide): use 100vh (fill height)
    // Portrait screen (like vertical monitor): use 100vw (fill width)
    const isLandscapeScreen = window.innerWidth > window.innerHeight;
    if (isLandscapeScreen) {
      return "h-[100vh] w-auto object-contain transition-transform duration-200";
    } else {
      return "w-[100vw] h-auto object-contain transition-transform duration-200";
    }
  }, [fitToScreen]);

  // Toggle fullscreen
  const toggleFullscreen = useCallback(() => {
    if (!document.fullscreenElement) {
      document.documentElement.requestFullscreen();
      setIsFullscreen(true);
    } else {
      document.exitFullscreen();
      setIsFullscreen(false);
    }
  }, []);

  // Get images for current view mode
  const getVisibleImages = useCallback(() => {
    if (images.length === 0) {
      return [];
    }

    const imagesPerView = {
      single: 1,
      double: 2,
      triple: 3,
      quad: 4,
    }[viewMode];

    const visibleImages = [];
    for (let i = 0; i < imagesPerView; i++) {
      const index = (currentIndex + i) % images.length;
      const image = images[index];
      if (image) {
        visibleImages.push(image);
      }
    }
    return visibleImages;
  }, [images, currentIndex, viewMode]);

  // Navigate to next/previous image
  const navigateToImage = useCallback(
    (direction: 'next' | 'prev') => {
      if (images.length === 0) return;

      const imagesPerView = {
        single: 1,
        double: 2,
        triple: 3,
        quad: 4,
      }[viewMode];

      let newIndex = currentIndex;
      if (direction === 'next') {
        newIndex = (currentIndex + imagesPerView) % images.length;
      } else {
        newIndex = (currentIndex - imagesPerView + images.length) % images.length;
      }

      const newImageId = images[newIndex].id;
      
      // Update local state instead of URL for seamless navigation
      setCurrentImageId(newImageId);
      
      // Reset zoom, rotation, and pan when changing images
      setZoom(1);
      setRotation(0);
      setPanPosition({ x: 0, y: 0 });
    },
    [images, currentIndex, viewMode]
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
        case '?':
        case 'h':
          setShowHelp((s) => !s);
          break;
        case ' ': 
          e.preventDefault();
          setIsSlideshow((s) => !s);
          break;
        case '1':
          saveViewMode('single');
          break;
        case '2':
          saveViewMode('double');
          break;
        case '3':
          saveViewMode('triple');
          break;
        case '4':
          saveViewMode('quad');
          break;
        case 'f':
        case 'F11':
          e.preventDefault();
          toggleFullscreen();
          break;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [collectionId, navigate, navigateToImage, saveViewMode, toggleFullscreen]);

  // Slideshow
  useEffect(() => {
    if (isSlideshow) {
      slideshowRef.current = setInterval(() => {
        if (isShuffleMode && images.length > 0) {
          // Navigate to random image
          const randomIndex = Math.floor(Math.random() * images.length);
          const randomImage = images[randomIndex];
          if (randomImage) {
            setCurrentImageId(randomImage.id);
          }
        } else {
          navigateToImage('next');
        }
      }, slideshowInterval);
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
  }, [isSlideshow, slideshowInterval, isShuffleMode, navigateToImage, images, collectionId, navigate]);

  // Listen for fullscreen changes
  useEffect(() => {
    const handleFullscreenChange = () => {
      setIsFullscreen(!!document.fullscreenElement);
    };

    document.addEventListener('fullscreenchange', handleFullscreenChange);
    return () => document.removeEventListener('fullscreenchange', handleFullscreenChange);
  }, []);

  // Scroll to current image in scroll mode
  useEffect(() => {
    if (navigationMode === 'scroll' && currentImageId && currentIndex >= 0) {
      // Small delay to ensure DOM is ready
      setTimeout(() => {
        const imageElement = document.getElementById(`image-${currentImageId}`);
        if (imageElement) {
          imageElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
      }, 100);
    }
  }, [navigationMode, currentImageId, currentIndex]);

  // Image preloading (only in paging mode)
  useEffect(() => {
    if (navigationMode === 'scroll' || currentIndex === -1 || images.length === 0) {
      return;
    }

    const maxPreload = parseInt(localStorage.getItem('maxPreloadImages') || '20');
    const preloadImages: HTMLImageElement[] = [];

    for (let i = 1; i <= Math.min(maxPreload, images.length - 1); i++) {
      const nextIndex = (currentIndex + i) % images.length;
      const nextImage = images[nextIndex];
      if (nextImage) {
        const img = new Image();
        img.src = `/api/v1/images/${collectionId}/${nextImage.id}/file`;
        preloadImages.push(img);
      }
    }

    // Cleanup - properly remove image references
    return () => {
      preloadImages.forEach(img => {
        img.onload = null;
        img.onerror = null;
        img.src = 'data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7'; // 1x1 transparent gif
      });
      preloadImages.length = 0;
    };
  }, [currentIndex, images, collectionId]);

  if (!currentImage && images.length === 0) {
    return <LoadingSpinner fullScreen text="Loading images..." />;
  }
  
  if (!currentImage && images.length > 0) {
    // Images loaded but current not found, will auto-redirect
    return <LoadingSpinner fullScreen text="Loading..." />;
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
            {/* Navigation Mode Toggle */}
            <button
              onClick={toggleNavigationMode}
              className={`p-2 hover:bg-white/10 rounded-lg transition-colors ${
                navigationMode === 'scroll' ? 'bg-primary-500' : ''
              }`}
              title={navigationMode === 'paging' ? 'Switch to Scroll Mode' : 'Switch to Paging Mode'}
            >
              <ArrowDownUp className="h-5 w-5 text-white" />
            </button>

            {/* View Mode Controls */}
            <div className="flex items-center gap-1 bg-black/20 rounded-lg p-1">
              <button
                onClick={() => saveViewMode('single')}
                className={`p-1.5 rounded transition-colors ${
                  viewMode === 'single'
                    ? 'bg-primary-500 text-white'
                    : 'text-slate-300 hover:text-white hover:bg-white/10'
                }`}
                title="Single View (1)"
              >
                <Monitor className="h-4 w-4" />
              </button>
              <button
                onClick={() => saveViewMode('double')}
                className={`p-1.5 rounded transition-colors ${
                  viewMode === 'double'
                    ? 'bg-primary-500 text-white'
                    : 'text-slate-300 hover:text-white hover:bg-white/10'
                }`}
                title="Double Page View (2)"
              >
                <Layout className="h-4 w-4" />
              </button>
              <button
                onClick={() => saveViewMode('triple')}
                className={`p-1.5 rounded transition-colors ${
                  viewMode === 'triple'
                    ? 'bg-primary-500 text-white'
                    : 'text-slate-300 hover:text-white hover:bg-white/10'
                }`}
                title="Triple Page View (3)"
              >
                <Grid className="h-4 w-4" />
              </button>
              <button
                onClick={() => saveViewMode('quad')}
                className={`p-1.5 rounded transition-colors ${
                  viewMode === 'quad'
                    ? 'bg-primary-500 text-white'
                    : 'text-slate-300 hover:text-white hover:bg-white/10'
                }`}
                title="Quad View (4)"
              >
                <Maximize2 className="h-4 w-4" />
              </button>
            </div>

            <button
              onClick={() => setShowInfo(!showInfo)}
              className="p-2 hover:bg-white/10 rounded-lg transition-colors"
              title="Toggle Info (I)"
            >
              <Info className="h-5 w-5 text-white" />
            </button>
            <button
              onClick={toggleFitToScreen}
              className={`p-2 hover:bg-white/10 rounded-lg transition-colors ${
                fitToScreen ? 'bg-primary-500' : ''
              }`}
              title={fitToScreen ? "Fit to Viewport" : "Fit to Screen (100vh/100vw)"}
            >
              <Expand className="h-5 w-5 text-white" />
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
              onClick={toggleFullscreen}
              className="p-2 hover:bg-white/10 rounded-lg transition-colors"
              title="Fullscreen (F)"
            >
              <Maximize2 className="h-5 w-5 text-white" />
            </button>

            {/* Slideshow Controls */}
            <div className="flex items-center gap-1 bg-black/20 rounded-lg p-1">
              <input
                type="number"
                min="500"
                max="60000"
                step="500"
                value={slideshowInterval}
                onChange={(e) => {
                  const newInterval = parseInt(e.target.value) || 3000;
                  setSlideshowInterval(newInterval);
                  localStorage.setItem('slideshowInterval', newInterval.toString());
                }}
                className="w-16 px-2 py-1 bg-slate-700 border border-slate-600 rounded text-white text-xs text-center focus:outline-none focus:ring-1 focus:ring-primary-500"
                title="Slideshow interval (ms)"
              />
              <button
                onClick={() => setIsShuffleMode(!isShuffleMode)}
                className={`p-1.5 rounded transition-colors ${
                  isShuffleMode ? 'bg-primary-500 text-white' : 'text-slate-300 hover:text-white hover:bg-white/10'
                }`}
                title="Shuffle Mode"
              >
                <Shuffle className="h-4 w-4" />
              </button>
              <button
                onClick={() => setIsSlideshow(!isSlideshow)}
                className={`p-1.5 rounded transition-colors ${
                  isSlideshow ? 'bg-primary-500 text-white' : 'text-slate-300 hover:text-white hover:bg-white/10'
                }`}
                title="Slideshow (Space)"
              >
                {isSlideshow ? (
                  <Pause className="h-4 w-4 text-white" />
                ) : (
                  <Play className="h-4 w-4 text-white" />
                )}
              </button>
            </div>

            {/* Help Button */}
            <button
              onClick={() => setShowHelp(!showHelp)}
              className="p-2 hover:bg-white/10 rounded-lg transition-colors"
              title="Help (? or H)"
            >
              <HelpCircle className="h-5 w-5 text-white" />
            </button>
          </div>
        </div>
      </div>

      {/* Main Images */}
      <div 
        ref={imageContainerRef}
        className={`flex-1 overflow-auto ${navigationMode === 'scroll' ? 'flex items-start justify-center' : 'flex items-center justify-center'}`}
      >
        {navigationMode === 'scroll' ? (
          // Scroll Mode: Show all images in a vertical list
          <div className="flex flex-col gap-4 py-4 w-full max-w-[100vw] items-center">
            {images.map((image, index) => (
              <div 
                key={image.id} 
                id={`image-${image.id}`}
                className="flex flex-col items-center relative"
              >
                {/* Highlight current image */}
                {currentIndex === index && (
                  <div className="absolute -inset-2 border-2 border-primary-500 rounded-lg pointer-events-none"></div>
                )}
                <img
                  src={`/api/v1/images/${collectionId}/${image.id}/file`}
                  alt={image.filename}
                  className={getImageClass()}
                  style={{
                    transform: `rotate(${rotation}deg)`,
                  }}
                  loading="lazy"
                />
                <div className="mt-2 text-white text-sm text-center">
                  {index + 1} / {images.length} - {image.filename}
                </div>
              </div>
            ))}
          </div>
        ) : (
          // Paging Mode: Show images based on view mode
          <div 
            className={`flex items-center justify-center gap-2 ${
              viewMode === 'single' ? 'flex-col' : 
              viewMode === 'double' ? 'flex-row' :
              viewMode === 'triple' ? 'flex-row' :
              'grid grid-cols-2 gap-2'
            }`}
            style={{
              transform: `scale(${zoom})`,
              transformOrigin: 'center center',
              maxWidth: '100%',
              maxHeight: '100%',
            }}
          >
            {getVisibleImages().map((image, index) => (
            <div
              key={`${image.id}-${index}`}
              className={`relative ${
                viewMode === 'single' ? 'w-full h-full' :
                viewMode === 'double' ? 'w-1/2 h-full' :
                viewMode === 'triple' ? 'w-1/3 h-full' :
                'w-full h-full'
              }`}
            >
              {imageLoading && index === 0 && (
                <div className="absolute inset-0 flex items-center justify-center bg-black/50">
                  <LoadingSpinner text="Loading..." />
                </div>
              )}
              {imageError && index === 0 && (
                <div className="absolute inset-0 flex items-center justify-center bg-black/50 text-white">
                  <div className="text-center">
                    <p className="text-red-500 text-lg mb-2">Failed to load image</p>
                    <button
                      onClick={() => {
                        setImageError(false);
                        setImageLoading(true);
                      }}
                      className="px-4 py-2 bg-primary-500 rounded-lg hover:bg-primary-600"
                    >
                      Retry
                    </button>
                  </div>
                </div>
              )}
              <img
                src={`/api/v1/images/${collectionId}/${image.id}/file`}
                alt={image.filename}
                className={getImageClass()}
                style={{
                  transform: `rotate(${rotation}deg)`,
                }}
                onLoad={() => index === 0 && setImageLoading(false)}
                onError={() => {
                  if (index === 0) {
                    setImageLoading(false);
                    setImageError(true);
                  }
                }}
              />
              {/* Image index indicator for multi-view */}
              {viewMode !== 'single' && (
                <div className="absolute top-2 left-2 bg-black/70 text-white text-xs px-2 py-1 rounded">
                  {images.findIndex(img => img.id === image.id) + 1}
                </div>
              )}
            </div>
            ))}
          </div>
        )}
      </div>

      {/* Navigation Buttons (Only in paging mode) */}
      {navigationMode === 'paging' && (
        <>
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
        </>
      )}

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

      {/* Keyboard Shortcuts Help (Only show when help button clicked) */}
      {showHelp && (
        <div className="absolute bottom-4 left-1/2 -translate-x-1/2 bg-black/90 rounded-lg p-4 max-w-2xl">
          <div className="text-white text-sm space-y-2">
            <p className="font-bold text-center mb-2">Keyboard Shortcuts</p>
            <div className="grid grid-cols-2 gap-x-6 gap-y-1">
              <p>← → : Navigate</p>
              <p>Esc : Close</p>
              <p>+/- : Zoom</p>
              <p>R : Rotate</p>
              <p>I : Info</p>
              <p>Space : Slideshow</p>
              <p>1-4 : View Modes</p>
              <p>F : Fullscreen</p>
            </div>
            <p className="text-center text-xs text-slate-400 mt-2">Press ? or click Help icon to toggle</p>
          </div>
        </div>
      )}
    </div>
  );
};

export default ImageViewer;
