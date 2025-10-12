import { useState, useEffect, useCallback, useRef } from 'react';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import { useImages, useImage } from '../hooks/useImages';
import { useCollection } from '../hooks/useCollections';
import { useCollectionNavigation } from '../hooks/useCollectionNavigation';
import { useUserSettings } from '../hooks/useSettings';
import LoadingSpinner from '../components/ui/LoadingSpinner';
import CollectionNavigationSidebar from '../components/collections/CollectionNavigationSidebar';
import ImagePreviewSidebar from '../components/viewer/ImagePreviewSidebar';
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
  PanelLeft,
  PanelRight,
  Images,
  Link2,
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
  const goToLast = searchParams.get('goToLast') === 'true';
  const { data: collection } = useCollection(collectionId!);
  
  // Get user settings for pageSize
  const { data: userSettingsData } = useUserSettings();
  const [imageViewerPageSize, setImageViewerPageSize] = useState(() => 
    userSettingsData?.imageViewerPageSize || parseInt(localStorage.getItem('imageViewerPageSize') || '200')
  );
  
  // Sync imageViewerPageSize when backend settings change
  useEffect(() => {
    if (userSettingsData?.imageViewerPageSize && userSettingsData.imageViewerPageSize !== imageViewerPageSize) {
      console.log(`[ImageViewer] Syncing pageSize from backend: ${userSettingsData.imageViewerPageSize}`);
      setImageViewerPageSize(userSettingsData.imageViewerPageSize);
      localStorage.setItem('imageViewerPageSize', userSettingsData.imageViewerPageSize.toString());
      // Reset to page 1 when pageSize changes
      setCurrentPage(1);
      setAllLoadedImages([]);
    }
  }, [userSettingsData?.imageViewerPageSize, imageViewerPageSize]);
  
  // Paginated image loading
  const [currentPage, setCurrentPage] = useState(1);
  const [allLoadedImages, setAllLoadedImages] = useState<any[]>([]);
  const [totalImagesCount, setTotalImagesCount] = useState(0);
  
  // Load initial page or page containing specific image
  const { data: imagesData, isLoading: imagesLoading, refetch: refetchImages } = useImages({ 
    collectionId: collectionId!, 
    page: currentPage,
    limit: imageViewerPageSize 
  });
  
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
  const [showCollectionSidebar, setShowCollectionSidebar] = useState(() => 
    localStorage.getItem('imageViewerShowSidebar') === 'true' // Default: hidden
  );
  const [showImagePreviewSidebar, setShowImagePreviewSidebar] = useState(() => 
    localStorage.getItem('imageViewerShowPreviewSidebar') === 'true' // Default: hidden
  );
  const [crossCollectionNav, setCrossCollectionNav] = useState(() => 
    localStorage.getItem('imageViewerCrossCollectionNav') === 'true' // Default: disabled
  );
  const slideshowRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const imageContainerRef = useRef<HTMLDivElement>(null);
  const preloadedImagesRef = useRef<Map<string, HTMLImageElement>>(new Map());

  // Fetch collection navigation info for cross-collection navigation
  const { data: collectionNav } = useCollectionNavigation(
    crossCollectionNav ? collectionId : undefined
  );

  // Reset state when collectionId changes (MUST run before data loads)
  useEffect(() => {
    console.log(`[ImageViewer] Collection or image changed - collectionId: ${collectionId}, initialImageId: ${initialImageId}`);
    console.log(`[ImageViewer] Resetting all state`);
    
    // Reset all state immediately
    setAllLoadedImages([]);
    setCurrentPage(1);
    setTotalImagesCount(0);
    setCurrentImageId(initialImageId || '');
    
    // Clear preloaded images cache (CRITICAL for sidebar navigation!)
    console.log(`[ImageViewer] Clearing ${preloadedImagesRef.current.size} preloaded images`);
    preloadedImagesRef.current.forEach((img) => {
      img.onload = null;
      img.onerror = null;
      img.src = '';
    });
    preloadedImagesRef.current.clear();
    
    // Also reset UI state
    setZoom(1);
    setRotation(0);
    setPanPosition({ x: 0, y: 0 });
    setImageLoading(true);
    setImageError(false);
  }, [collectionId, initialImageId]); // Fire whenever these change
  
  // Update loaded images when new page arrives
  useEffect(() => {
    if (imagesData?.data && imagesData.data.length > 0) {
      // Verify images belong to current collection (prevent stale data)
      const firstImage = imagesData.data[0];
      console.log(`[ImageViewer] Loaded page ${currentPage}: ${imagesData.data.length} images (total: ${imagesData.totalCount})`);
      console.log(`[ImageViewer] First image ID: ${firstImage.id}, Current collectionId: ${collectionId}`);
      
      setTotalImagesCount(imagesData.totalCount || 0);
      
      // For page 1, replace instead of merge (fresh start)
      if (currentPage === 1) {
        console.log(`[ImageViewer] Page 1 - replacing with ${imagesData.data.length} fresh images`);
        setAllLoadedImages(imagesData.data);
      } else {
        // For page 2+, merge with existing images (avoid duplicates)
        setAllLoadedImages(prev => {
          const existingIds = new Set(prev.map(img => img.id));
          const newImages = imagesData.data.filter(img => !existingIds.has(img.id));
          console.log(`[ImageViewer] Page ${currentPage} - merging ${newImages.length} new images (had ${prev.length}, now ${prev.length + newImages.length})`);
          return [...prev, ...newImages];
        });
      }
    }
  }, [imagesData, currentPage, collectionId]);
  
  const images = allLoadedImages;
  const currentIndex = images.findIndex((img) => img.id === currentImageId);
  const currentImage = currentIndex >= 0 ? images[currentIndex] : null;
  
  // Load more pages function
  const loadMorePages = useCallback((direction: 'next' | 'previous' = 'next') => {
    const totalPages = Math.ceil(totalImagesCount / imageViewerPageSize);
    
    if (direction === 'next' && currentPage < totalPages) {
      console.log(`[ImageViewer] Loading next page: ${currentPage + 1}`);
      setCurrentPage(prev => prev + 1);
    } else if (direction === 'previous' && currentPage > 1) {
      console.log(`[ImageViewer] Loading previous page: ${currentPage - 1}`);
      setCurrentPage(prev => prev - 1);
    }
  }, [currentPage, totalImagesCount, imageViewerPageSize]);

  // Auto-load more pages when navigating near edges
  useEffect(() => {
    if (images.length === 0 || totalImagesCount === 0) return;
    
    const totalPages = Math.ceil(totalImagesCount / imageViewerPageSize);
    const threshold = 20; // Load more when within 20 images of edge
    
    // Check if near end of loaded images
    if (currentIndex >= 0 && currentIndex >= images.length - threshold && currentPage < totalPages) {
      console.log(`[ImageViewer] Near end (${currentIndex}/${images.length}), auto-loading page ${currentPage + 1}`);
      setCurrentPage(prev => prev + 1);
    }
  }, [currentIndex, images.length, currentPage, totalImagesCount, imageViewerPageSize]);
  
  // Handle goToLast parameter for cross-collection navigation from previous collection
  useEffect(() => {
    if (goToLast && totalImagesCount > 0 && !currentImageId) {
      // Calculate which page contains the last image
      const lastPage = Math.ceil(totalImagesCount / imageViewerPageSize);
      
      if (currentPage !== lastPage) {
        console.log(`[ImageViewer] goToLast: loading page ${lastPage}`);
        setCurrentPage(lastPage);
      } else if (images.length > 0) {
        // Navigate to last image once it's loaded
        const lastImage = images[images.length - 1];
        setCurrentImageId(lastImage.id);
      }
    }
  }, [goToLast, images, currentImageId, totalImagesCount, imageViewerPageSize, currentPage]);
  
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

  // Toggle cross-collection navigation
  const toggleCrossCollectionNav = useCallback(() => {
    const newValue = !crossCollectionNav;
    setCrossCollectionNav(newValue);
    localStorage.setItem('imageViewerCrossCollectionNav', newValue.toString());
  }, [crossCollectionNav]);

  // Toggle collection sidebar
  const toggleCollectionSidebar = useCallback(() => {
    const newValue = !showCollectionSidebar;
    setShowCollectionSidebar(newValue);
    localStorage.setItem('imageViewerShowSidebar', newValue.toString());
  }, [showCollectionSidebar]);

  // Toggle image preview sidebar
  const toggleImagePreviewSidebar = useCallback(() => {
    const newValue = !showImagePreviewSidebar;
    setShowImagePreviewSidebar(newValue);
    localStorage.setItem('imageViewerShowPreviewSidebar', newValue.toString());
  }, [showImagePreviewSidebar]);

  // Handle mouse wheel for zoom (Ctrl+Wheel)
  const handleWheel = useCallback((e: WheelEvent) => {
    // Only zoom when Ctrl key is pressed
    if (e.ctrlKey || e.metaKey) {
      e.preventDefault();
      
      const zoomDelta = e.deltaY > 0 ? -0.1 : 0.1;
      setZoom((prevZoom) => {
        const newZoom = Math.max(0.5, Math.min(5, prevZoom + zoomDelta));
        return Math.round(newZoom * 10) / 10; // Round to 1 decimal
      });
    }
  }, []);

  // Add wheel event listener for zoom in scroll mode
  useEffect(() => {
    const container = imageContainerRef.current;
    if (!container) return;

    // Add passive: false to allow preventDefault for Ctrl+Wheel
    container.addEventListener('wheel', handleWheel, { passive: false });

    return () => {
      container.removeEventListener('wheel', handleWheel);
    };
  }, [handleWheel]);

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

      // Cross-collection navigation disabled in scroll mode and shuffle mode
      const canCrossNavigate = crossCollectionNav && 
                               navigationMode === 'paging' && 
                               !isShuffleMode && 
                               collectionNav;

      const imagesPerView = {
        single: 1,
        double: 2,
        triple: 3,
        quad: 4,
      }[viewMode];

      let newIndex = currentIndex;
      if (direction === 'next') {
        newIndex = currentIndex + imagesPerView;
        
        // Check if we need to move to next collection
        if (newIndex >= images.length) {
          if (canCrossNavigate && collectionNav.hasNext && collectionNav.nextCollectionId) {
            // Navigate to first image of next collection
            navigate(`/viewer/${collectionNav.nextCollectionId}`);
            return;
          } else {
            // Wrap to beginning of current collection
            newIndex = 0;
          }
        }
      } else {
        newIndex = currentIndex - imagesPerView;
        
        // Check if we need to move to previous collection
        if (newIndex < 0) {
          if (canCrossNavigate && collectionNav.hasPrevious && collectionNav.previousCollectionId) {
            // Navigate to last image of previous collection
            // We don't know the last image ID yet, so navigate to collection and let it load
            navigate(`/viewer/${collectionNav.previousCollectionId}?goToLast=true`);
            return;
          } else {
            // Wrap to end of current collection
            newIndex = Math.max(0, images.length - imagesPerView);
          }
        }
      }

      const newImageId = images[newIndex].id;
      
      // Update local state instead of URL for seamless navigation
      setCurrentImageId(newImageId);
      
      // Reset zoom, rotation, and pan when changing images
      setZoom(1);
      setRotation(0);
      setPanPosition({ x: 0, y: 0 });
    },
    [images, currentIndex, viewMode, crossCollectionNav, navigationMode, isShuffleMode, collectionNav, navigate]
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
        case 't':
        case 'T':
          toggleImagePreviewSidebar();
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
  }, [collectionId, navigate, navigateToImage, saveViewMode, toggleFullscreen, toggleImagePreviewSidebar]);

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

  // Image preloading with persistent cache (only in paging mode)
  useEffect(() => {
    if (navigationMode === 'scroll' || currentIndex === -1 || images.length === 0) {
      return;
    }

    const maxPreload = parseInt(localStorage.getItem('maxPreloadImages') || '20');
    const preloadCache = preloadedImagesRef.current;

    // Determine which images to preload (next N images from current position)
    const imagesToPreload: string[] = [];
    for (let i = 1; i <= Math.min(maxPreload, images.length - 1); i++) {
      const nextIndex = (currentIndex + i) % images.length;
      const nextImage = images[nextIndex];
      if (nextImage && !preloadCache.has(nextImage.id)) {
        imagesToPreload.push(nextImage.id);
      }
    }

    // Preload only new images (not already in cache)
    imagesToPreload.forEach(imageId => {
      const img = new Image();
      img.src = `/api/v1/images/${collectionId}/${imageId}/file`;
      
      img.onload = () => {
        // console.log(`Preloaded image ${imageId}`);
      };
      
      img.onerror = () => {
        console.warn(`Failed to preload image ${imageId}`);
        preloadCache.delete(imageId);
      };
      
      preloadCache.set(imageId, img);
    });

    // Cleanup: Remove old images that are too far from current position
    const minKeepIndex = Math.max(0, currentIndex - 10);
    const maxKeepIndex = Math.min(images.length - 1, currentIndex + maxPreload + 10);
    
    preloadCache.forEach((img, imageId) => {
      const imgIndex = images.findIndex(i => i.id === imageId);
      if (imgIndex !== -1 && (imgIndex < minKeepIndex || imgIndex > maxKeepIndex)) {
        img.onload = null;
        img.onerror = null;
        img.src = '';
        preloadCache.delete(imageId);
      }
    });
  }, [currentIndex, images, collectionId, navigationMode]);

  if (!currentImage && images.length === 0) {
    return <LoadingSpinner fullScreen text="Loading images..." />;
  }
  
  if (!currentImage && images.length > 0) {
    // Images loaded but current not found, will auto-redirect
    return <LoadingSpinner fullScreen text="Loading..." />;
  }

  return (
    <div key={`${collectionId}-${initialImageId}`} className="fixed inset-0 bg-black z-50 flex">
      {/* Collection Navigation Sidebar (toggleable) */}
      {showCollectionSidebar && (
        <CollectionNavigationSidebar
          key={`collection-sidebar-${collectionId}`}
          collectionId={collectionId!}
          sortBy="updatedAt"
          sortDirection="desc"
          onNavigate={(newCollectionId, firstImageId) => {
            console.log(`[ImageViewer Sidebar] Navigating to collection ${newCollectionId}, image ${firstImageId}`);
            // Navigate directly to viewer with first image if available
            if (firstImageId) {
              navigate(`/collections/${newCollectionId}/viewer?imageId=${firstImageId}`);
            } else {
              // Fallback to collection detail if no firstImageId
              navigate(`/collections/${newCollectionId}`);
            }
          }}
        />
      )}
      
      {/* Main Viewer Area */}
      <div className="flex-1 flex">
        {/* Image Display Area */}
        <div className="flex-1 flex flex-col">
      {/* Header */}
      <div className="absolute top-0 left-0 right-0 z-10 bg-gradient-to-b from-black/80 to-transparent p-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-4">
            <button
              onClick={toggleCollectionSidebar}
              className={`p-2 hover:bg-white/10 rounded-lg transition-colors ${
                showCollectionSidebar ? 'bg-primary-500' : ''
              }`}
              title={showCollectionSidebar ? 'Hide Collections' : 'Show Collections'}
            >
              <PanelLeft className="h-6 w-6 text-white" />
            </button>
            <button
              onClick={() => navigate(`/collections/${collectionId}`)}
              className="p-2 hover:bg-white/10 rounded-lg transition-colors"
            >
              <X className="h-6 w-6 text-white" />
            </button>
            <div className="text-white">
              <h2 className="font-semibold">{currentImage.filename}</h2>
              <p className="text-sm text-slate-300">
                {currentIndex + 1} of {totalImagesCount > 0 ? totalImagesCount : images.length}
                {totalImagesCount > images.length && (
                  <span className="text-primary-400"> (Loaded: {images.length})</span>
                )}
                {' • '}{currentImage.width} × {currentImage.height}
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

            {/* Cross-Collection Navigation Toggle */}
            <button
              onClick={toggleCrossCollectionNav}
              disabled={navigationMode === 'scroll' || isShuffleMode}
              className={`p-2 hover:bg-white/10 rounded-lg transition-colors disabled:opacity-30 disabled:cursor-not-allowed ${
                crossCollectionNav ? 'bg-purple-500' : ''
              }`}
              title={
                navigationMode === 'scroll' 
                  ? 'Cross-collection navigation not available in scroll mode' 
                  : isShuffleMode
                  ? 'Cross-collection navigation not available in shuffle mode'
                  : crossCollectionNav 
                  ? 'Disable cross-collection navigation (wrap within current collection)' 
                  : 'Enable cross-collection navigation (move to prev/next collection at boundaries)'
              }
            >
              <Link2 className="h-5 w-5 text-white" />
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
            <span 
              className="text-white text-sm px-2" 
              title={navigationMode === 'scroll' ? 'Ctrl+Wheel to zoom in scroll mode' : 'Zoom level'}
            >
              {Math.round(zoom * 100)}%
            </span>
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

            {/* Image Preview Sidebar Toggle */}
            <button
              onClick={toggleImagePreviewSidebar}
              className={`p-2 hover:bg-white/10 rounded-lg transition-colors ${
                showImagePreviewSidebar ? 'bg-primary-500' : ''
              }`}
              title={showImagePreviewSidebar ? 'Hide Thumbnails (T)' : 'Show Thumbnails (T)'}
            >
              <Images className="h-5 w-5 text-white" />
            </button>

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
                    transform: `scale(${zoom}) rotate(${rotation}deg)`,
                    transformOrigin: 'center center',
                    transition: 'transform 0.2s ease-out',
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
              <p>Ctrl+Wheel : Zoom (Scroll Mode)</p>
              <p>R : Rotate</p>
              <p>I : Info</p>
              <p>T : Thumbnails</p>
              <p>Space : Slideshow</p>
              <p>1-4 : View Modes</p>
              <p>F : Fullscreen</p>
            </div>
            <p className="text-center text-xs text-slate-400 mt-2">
              Press ? or click Help icon to toggle
            </p>
            {crossCollectionNav && navigationMode === 'paging' && !isShuffleMode && (
              <div className="mt-2 pt-2 border-t border-slate-700">
                <p className="text-center text-xs text-purple-400">
                  🔗 Cross-Collection Mode: Navigate to prev/next collection at boundaries
                </p>
              </div>
            )}
          </div>
        </div>
      )}
        </div>
        {/* End Image Display Area */}
        
        {/* Image Preview Sidebar (thumbnails strip on right) */}
        {showImagePreviewSidebar && (
          <ImagePreviewSidebar
            key={`preview-${collectionId}`}
            images={images}
            currentImageId={currentImageId}
            collectionId={collectionId!}
            onImageClick={(imageId) => setCurrentImageId(imageId)}
          />
        )}
      </div>
      {/* End Main Viewer Area */}
    </div>
  );
};

export default ImageViewer;
