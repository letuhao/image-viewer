import React, { useEffect, useState, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useHotkeys } from 'react-hotkeys-hook';
import {
  ArrowLeftIcon,
  ArrowRightIcon,
  ArrowPathIcon,
  Cog6ToothIcon,
  HomeIcon
} from '@heroicons/react/24/outline';
import useStore from '../store/useStore';
import { imagesApi, collectionsApi } from '../services/api';
import LoadingSpinner from '../components/LoadingSpinner';
import ViewerSettings from '../components/ViewerSettings';
import DockableControlBar from '../components/DockableControlBar';
import toast from 'react-hot-toast';

const ImageViewerPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const {
    viewer,
    setCurrentImage,
    setImages,
    setFullscreen,
    setPlaying,
    setPlayInterval,
    goToNextImage,
    goToPreviousImage,
    goToRandomImage,
    setSlideshowSpeed,
    setEnableCollectionNavigation,
    setAutoHideControls,
    setAllCollections,
    setCurrentCollectionIndex,
    goToPreviousCollection,
    goToNextCollection
  } = useStore();

  const [currentImage, setCurrentImageState] = useState<any>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [hasError, setHasError] = useState(false);
  const [showSettings, setShowSettings] = useState(false);
  const [imageUrl, setImageUrl] = useState('');
  const [imageIndex, setImageIndex] = useState(0);
  const [playTimer, setPlayTimer] = useState<ReturnType<typeof setInterval> | null>(null);

  const collectionId = id || '';

  useEffect(() => {
    if (viewer.currentImage) {
      setCurrentImageState(viewer.currentImage);
      loadImage(viewer.currentImage);
    }
  }, [viewer.currentImage]);

  // Load all collections and set current collection index
  useEffect(() => {
    const loadCollectionsAndSetIndex = async () => {
      try {
        const response = await collectionsApi.getAll({ limit: 10000 });
        const allCollections = response.data.collections;
        setAllCollections(allCollections);
        
        // Find current collection index
        const currentIndex = allCollections.findIndex((col: any) => col.id === id);
        if (currentIndex !== -1) {
          setCurrentCollectionIndex(currentIndex);
        }
      } catch (error) {
        console.error('Failed to load collections:', error);
      }
    };

    if (id) {
      loadCollectionsAndSetIndex();
    }
  }, [id, setAllCollections, setCurrentCollectionIndex]);

  // Load images when collection changes
  useEffect(() => {
    if (id && viewer.currentCollection?.id !== id) {
      // Collection changed, load images for new collection
      const loadImagesForCollection = async () => {
        try {
          setIsLoading(true);
          const response = await imagesApi.getAll(id, { limit: 1000 });
          setImages(response.data.images);
          
          if (response.data.images.length > 0) {
            setCurrentImage(response.data.images[0]);
          }
        } catch (error) {
          console.error('Failed to load images for collection:', error);
          setHasError(true);
        } finally {
          setIsLoading(false);
        }
      };

      loadImagesForCollection();
    }
  }, [id, viewer.currentCollection?.id]);

  useEffect(() => {
    if (viewer.images.length > 0 && viewer.currentImage) {
      const index = viewer.images.findIndex(img => img.id === viewer.currentImage!.id);
      setImageIndex(index);
    }
  }, [viewer.images, viewer.currentImage]);

  useEffect(() => {
    return () => {
      if (playTimer) {
        clearInterval(playTimer);
      }
    };
  }, [playTimer]);

  const loadImage = async (image: any) => {
    if (!collectionId || !image) return;
    
    try {
      setIsLoading(true);
      setHasError(false);
      
      // Load full resolution image
      const response = await imagesApi.getFile(collectionId, image.id, {
        quality: 95
      });
      
      const blob = new Blob([response.data], { type: 'image/jpeg' });
      const url = URL.createObjectURL(blob);
      setImageUrl(url);
      
      // Clean up previous URL
      if (imageUrl) {
        URL.revokeObjectURL(imageUrl);
      }
    } catch (error) {
      setHasError(true);
      toast.error('Failed to load image');
    } finally {
      setIsLoading(false);
    }
  };

  const handleNext = useCallback(async () => {
    if (!collectionId || viewer.images.length === 0) return;
    
    try {
      const response = await imagesApi.navigate(collectionId, viewer.currentImage!.id, 'next');
      setCurrentImage(response.data);
    } catch (error) {
      // If at the end of current collection and collection navigation is enabled
      if (viewer.enableCollectionNavigation && viewer.currentCollectionIndex < viewer.allCollections.length - 1) {
        const nextCollectionId = goToNextCollection();
        if (nextCollectionId) {
          navigate(`/collection/${nextCollectionId}`);
        }
      } else {
        goToNextImage();
      }
    }
  }, [collectionId, viewer.currentImage, viewer.images.length, viewer.enableCollectionNavigation, viewer.currentCollectionIndex, viewer.allCollections.length, goToNextImage, goToNextCollection]);

  const handlePrevious = useCallback(async () => {
    if (!collectionId || viewer.images.length === 0) return;
    
    try {
      const response = await imagesApi.navigate(collectionId, viewer.currentImage!.id, 'previous');
      setCurrentImage(response.data);
    } catch (error) {
      // If at the beginning of current collection and collection navigation is enabled
      if (viewer.enableCollectionNavigation && viewer.currentCollectionIndex > 0) {
        const prevCollectionId = goToPreviousCollection();
        if (prevCollectionId) {
          navigate(`/collection/${prevCollectionId}`);
        }
      } else {
        goToPreviousImage();
      }
    }
  }, [collectionId, viewer.currentImage, viewer.images.length, viewer.enableCollectionNavigation, viewer.currentCollectionIndex, goToPreviousImage, goToPreviousCollection]);

  const handleRandom = useCallback(async () => {
    if (!collectionId) return;
    
    try {
      const response = await imagesApi.getRandom(collectionId);
      setCurrentImage(response.data);
    } catch (error) {
      goToRandomImage();
    }
  }, [collectionId, goToRandomImage]);

  const toggleFullscreen = () => {
    setFullscreen(!viewer.isFullscreen);
  };

  const togglePlay = () => {
    if (viewer.isPlaying) {
      if (playTimer) {
        clearInterval(playTimer);
        setPlayTimer(null);
      }
      setPlaying(false);
    } else {
      const timer = setInterval(() => {
        handleNext();
      }, viewer.slideshowSpeed);
      setPlayTimer(timer);
      setPlaying(true);
    }
  };

  const handleKeyPress = useCallback((event: KeyboardEvent) => {
    switch (event.key) {
      case 'ArrowLeft':
      case 'a':
        event.preventDefault();
        handlePrevious();
        break;
      case 'ArrowRight':
      case 'd':
        event.preventDefault();
        handleNext();
        break;
      case ' ':
        event.preventDefault();
        togglePlay();
        break;
      case 'f':
        event.preventDefault();
        toggleFullscreen();
        break;
      case 'r':
        event.preventDefault();
        handleRandom();
        break;
      case 'Escape':
        event.preventDefault();
        if (viewer.isFullscreen) {
          setFullscreen(false);
        } else {
          navigate(`/collection/${collectionId}`);
        }
        break;
      case 'Home':
        event.preventDefault();
        navigate('/');
        break;
    }
  }, [handleNext, handlePrevious, togglePlay, toggleFullscreen, handleRandom, viewer.isFullscreen, setFullscreen, navigate, collectionId]);

  useEffect(() => {
    document.addEventListener('keydown', handleKeyPress);
    return () => document.removeEventListener('keydown', handleKeyPress);
  }, [handleKeyPress]);

  // Hotkeys for better UX
  useHotkeys('left,a', () => handlePrevious(), { preventDefault: true });
  useHotkeys('right,d', () => handleNext(), { preventDefault: true });
  useHotkeys('space', () => togglePlay(), { preventDefault: true });
  useHotkeys('f', () => toggleFullscreen(), { preventDefault: true });
  useHotkeys('r', () => handleRandom(), { preventDefault: true });
  useHotkeys('escape', () => {
    if (viewer.isFullscreen) {
      setFullscreen(false);
    } else {
      navigate(`/collection/${collectionId}`);
    }
  }, { preventDefault: true });

  if (!currentImage) {
    return (
      <div className="min-h-screen bg-dark-900 flex items-center justify-center">
        <div className="text-center">
          <p className="text-dark-400 mb-4">No image selected</p>
          <button
            onClick={() => navigate(`/collection/${collectionId}`)}
            className="btn btn-primary"
          >
            Back to Collection
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className={`min-h-screen bg-dark-900 ${viewer.isFullscreen ? 'fixed inset-0 z-50' : ''}`}>
      {/* Top Navigation */}
      {!viewer.isFullscreen && (
        <div className="bg-dark-800 border-b border-dark-700 px-4 py-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-4">
              <button
                onClick={() => navigate(`/collection/${collectionId}`)}
                className="btn btn-ghost"
              >
                <ArrowLeftIcon className="h-5 w-5 mr-2" />
                Back
              </button>
              
              <div className="text-white">
                <h1 className="font-semibold">{currentImage.filename}</h1>
                <p className="text-sm text-dark-400">
                  {imageIndex + 1} of {viewer.images.length}
                </p>
              </div>
            </div>
            
            <div className="flex items-center space-x-2">
              <button
                onClick={() => setShowSettings(true)}
                className="btn btn-ghost"
              >
                <Cog6ToothIcon className="h-5 w-5" />
              </button>
              
              <button
                onClick={() => navigate('/')}
                className="btn btn-ghost"
              >
                <HomeIcon className="h-5 w-5" />
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Main Viewer */}
      <div className="relative flex items-center justify-center min-h-[calc(100vh-4rem)]">
        {isLoading && (
          <div className="absolute inset-0 flex items-center justify-center bg-dark-900/50">
            <LoadingSpinner size="lg" text="Loading image..." />
          </div>
        )}
        
        {hasError && (
          <div className="absolute inset-0 flex items-center justify-center">
            <div className="text-center">
              <p className="text-red-400 mb-4">Failed to load image</p>
              <button
                onClick={() => loadImage(currentImage)}
                className="btn btn-primary"
              >
                <ArrowPathIcon className="h-4 w-4 mr-2" />
                Retry
              </button>
            </div>
          </div>
        )}
        
        {imageUrl && !isLoading && !hasError && (
          <div className="relative max-w-full max-h-full">
            <img
              src={imageUrl}
              alt={currentImage.filename}
              className="max-w-full max-h-full object-contain"
              style={{
                maxHeight: viewer.isFullscreen ? '100vh' : 'calc(100vh - 8rem)'
              }}
            />
          </div>
        )}
        
        {/* Navigation Overlay */}
        <div className="absolute inset-0 pointer-events-none">
          {/* Left/Previous */}
          <button
            onClick={handlePrevious}
            className="absolute left-4 top-1/2 transform -translate-y-1/2 pointer-events-auto btn btn-secondary opacity-0 hover:opacity-100 transition-opacity"
          >
            <ArrowLeftIcon className="h-6 w-6" />
          </button>
          
          {/* Right/Next */}
          <button
            onClick={handleNext}
            className="absolute right-4 top-1/2 transform -translate-y-1/2 pointer-events-auto btn btn-secondary opacity-0 hover:opacity-100 transition-opacity"
          >
            <ArrowRightIcon className="h-6 w-6" />
          </button>
        </div>
      </div>

      {/* Dockable Control Bar */}
      <DockableControlBar
        onPrevious={handlePrevious}
        onNext={handleNext}
        onPlay={togglePlay}
        onRandom={handleRandom}
        onFullscreen={toggleFullscreen}
        onSettings={() => setShowSettings(true)}
        onHome={() => navigate(`/collection/${collectionId}`)}
        onPreviousCollection={() => {
          const prevCollectionId = goToPreviousCollection();
          if (prevCollectionId) {
            navigate(`/collection/${prevCollectionId}`);
          }
        }}
        onNextCollection={() => {
          const nextCollectionId = goToNextCollection();
          if (nextCollectionId) {
            navigate(`/collection/${nextCollectionId}`);
          }
        }}
        hasPreviousCollection={viewer.currentCollectionIndex > 0}
        hasNextCollection={viewer.currentCollectionIndex < viewer.allCollections.length - 1}
        isPlaying={viewer.isPlaying}
        isFullscreen={viewer.isFullscreen}
        slideshowSpeed={viewer.slideshowSpeed}
        onSlideshowSpeedChange={setSlideshowSpeed}
        enableCollectionNavigation={viewer.enableCollectionNavigation}
        onCollectionNavigationToggle={setEnableCollectionNavigation}
        autoHideControls={viewer.autoHideControls}
        onAutoHideToggle={setAutoHideControls}
      />

      {/* Image Info Overlay */}
      {!viewer.isFullscreen && (
        <div className="fixed top-20 right-4 bg-dark-800 bg-opacity-90 backdrop-blur-sm rounded-lg p-4 shadow-lg max-w-xs">
          <h3 className="text-white font-semibold mb-2">Image Info</h3>
          <div className="text-sm text-dark-300 space-y-1">
            <p><span className="text-dark-400">File:</span> {currentImage.filename}</p>
            <p><span className="text-dark-400">Size:</span> {currentImage.width} × {currentImage.height}</p>
            <p><span className="text-dark-400">Format:</span> {currentImage.filename.split('.').pop()?.toUpperCase()}</p>
          </div>
        </div>
      )}

      {/* Settings Modal */}
      {showSettings && (
        <ViewerSettings
          onClose={() => setShowSettings(false)}
          playInterval={viewer.playInterval}
          onPlayIntervalChange={setPlayInterval}
        />
      )}
    </div>
  );
};

export default ImageViewerPage;
