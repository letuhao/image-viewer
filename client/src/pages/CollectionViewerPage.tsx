import React, { useEffect, useState } from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
import {
  ArrowLeftIcon,
  PlayIcon,
  ArrowPathIcon,
  MagnifyingGlassIcon,
  FunnelIcon,
  Squares2X2Icon,
  ListBulletIcon,
  EyeIcon,
  Cog6ToothIcon
} from '@heroicons/react/24/outline';
import useStore from '../store/useStore';
import { collectionsApi, imagesApi } from '../services/api';
import LoadingSpinner from '../components/LoadingSpinner';
import ImageGrid from '../components/ImageGrid';
import ImageList from '../components/ImageList';
import PreloadSettings from '../components/PreloadSettings';
import toast from 'react-hot-toast';

const CollectionViewerPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const location = useLocation();
  const passedCollection = location.state?.collection;
  
  // Debug logs
  console.log('[DEBUG] CollectionViewerPage - id:', id);
  console.log('[DEBUG] CollectionViewerPage - location.state:', location.state);
  console.log('[DEBUG] CollectionViewerPage - passedCollection:', passedCollection);
  const { 
    collections, 
    selectCollection, 
    setImages, 
    setCurrentImage,
    viewer,
    setSortBy,
    setSortOrder,
    preloadThumbnails
  } = useStore();
  
  const [isLoading, setIsLoading] = useState(true);
  const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');
  const [searchQuery, setSearchQuery] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [showPreloadSettings, setShowPreloadSettings] = useState(false);
  const [collection, setCollection] = useState<any>(null);
  const [hasAutoScanned, setHasAutoScanned] = useState(false);

  // Try to find collection in store first, if not found, load from API
  const storeCollection = collections.find(col => col.id === id);

  useEffect(() => {
    if (id) {
      // Priority: 1. Passed collection, 2. Store collection, 3. API
      if (passedCollection) {
        // Collection passed from navigation
        setCollection(passedCollection);
        selectCollection(id);
        loadImages();
        // Auto-scan if needed (only once)
        if (passedCollection.settings?.total_images === 0 && !hasAutoScanned) {
          setHasAutoScanned(true);
          handleRescan();
        }
      } else if (storeCollection) {
        // Collection found in store
        setCollection(storeCollection);
        selectCollection(id);
        loadImages();
        // Auto-scan if needed (only once)
        if (storeCollection.settings?.total_images === 0 && !hasAutoScanned) {
          setHasAutoScanned(true);
          handleRescan();
        }
      } else {
        // Collection not in store, load from API first
        loadCollectionFromAPI();
      }
    }
  }, [id, passedCollection]);

  const loadCollectionFromAPI = async () => {
    if (!id) return;
    
    try {
      setIsLoading(true);
      const response = await collectionsApi.getById(id);
      setCollection(response.data);
      selectCollection(id);
      loadImages();
      
      // Auto-scan if collection has no images (only once)
      if (response.data.settings?.total_images === 0 && !hasAutoScanned) {
        setHasAutoScanned(true);
        handleRescan();
      }
    } catch (error) {
      toast.error('Collection not found');
      navigate('/collections');
    } finally {
      setIsLoading(false);
    }
  };

  const loadImages = async (page = 1) => {
    if (!id) return;
    
    try {
      setIsLoading(true);
      const response = await collectionsApi.getImages(id, {
        page,
        limit: 50,
        sort: viewer.sortBy,
        order: viewer.sortOrder
      });
      
      setImages(response.data.images);
      setCurrentPage(page);
      setTotalPages(response.data.pagination.pages);
      
      // Trigger preloading for the current page
      if (viewer.preloadEnabled && response.data.images.length > 0) {
        const imageIds = response.data.images.map((img: any) => img.id);
        preloadThumbnails(id, imageIds);
      }
    } catch (error) {
      toast.error('Failed to load images');
    } finally {
      setIsLoading(false);
    }
  };

  const handleSearch = async (query: string) => {
    if (!id) return;
    
    try {
      if (query.trim()) {
        const response = await imagesApi.search(id, query.trim());
        setImages(response.data.images);
        setCurrentPage(1);
        setTotalPages(response.data.pagination.pages);
      } else {
        loadImages();
      }
    } catch (error) {
      toast.error('Failed to search images');
    }
  };

  const handleImageClick = (image: any) => {
    setCurrentImage(image);
    navigate(`/collection/${id}/viewer`);
  };

  const handleRandomImage = async () => {
    if (!id) return;
    
    try {
      const response = await imagesApi.getRandom(id);
      setCurrentImage(response.data);
      navigate(`/collection/${id}/viewer`);
    } catch (error) {
      toast.error('Failed to get random image');
    }
  };

  const handleRescan = async () => {
    if (!id) return;
    
    try {
      await collectionsApi.scan(id);
      toast.success('Collection scan started');
      setTimeout(() => loadImages(), 2000);
    } catch (error) {
      toast.error('Failed to rescan collection');
    }
  };

  if (!collection) {
    return (
      <div className="p-6">
        <div className="text-center py-12">
          <p className="text-dark-400">Collection not found</p>
          <button
            onClick={() => navigate('/')}
            className="btn btn-primary mt-4"
          >
            Back to Collections
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="p-6 h-full flex flex-col">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center space-x-4">
          <button
            onClick={() => navigate('/')}
            className="btn btn-ghost"
          >
            <ArrowLeftIcon className="h-5 w-5 mr-2" />
            Back
          </button>
          
          <div>
            <h1 className="text-2xl font-bold text-white">{collection.name}</h1>
            <p className="text-dark-400 text-sm">{collection.path}</p>
          </div>
        </div>
        
        <div className="flex items-center space-x-2">
          <button
            onClick={handleRescan}
            className="btn btn-secondary"
            title="Rescan collection"
          >
            <ArrowPathIcon className="h-4 w-4 mr-2" />
            Rescan
          </button>
          
          <button
            onClick={handleRandomImage}
            className="btn btn-secondary"
            title="Random image"
          >
            <PlayIcon className="h-4 w-4 mr-2" />
            Random
          </button>
          
          <button
            onClick={() => navigate(`/collection/${id}/viewer`)}
            className="btn btn-primary"
          >
            <EyeIcon className="h-4 w-4 mr-2" />
            Open Viewer
          </button>
        </div>
      </div>

      {/* Controls */}
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center space-x-4">
          {/* Search */}
          <div className="relative">
            <MagnifyingGlassIcon className="absolute left-3 top-1/2 transform -translate-y-1/2 h-5 w-5 text-dark-400" />
            <input
              type="text"
              placeholder="Search images..."
              value={searchQuery}
              onChange={(e) => {
                setSearchQuery(e.target.value);
                handleSearch(e.target.value);
              }}
              className="input pl-10 w-64"
            />
          </div>
          
          {/* Sort */}
          <div className="flex items-center space-x-2">
            <FunnelIcon className="h-5 w-5 text-dark-400" />
            <select
              value={viewer.sortBy}
              onChange={(e) => setSortBy(e.target.value as 'filename' | 'date' | 'size')}
              className="input w-32"
            >
              <option value="filename">Name</option>
              <option value="date">Date</option>
              <option value="size">Size</option>
            </select>
            
            <select
              value={viewer.sortOrder}
              onChange={(e) => setSortOrder(e.target.value as 'asc' | 'desc')}
              className="input w-20"
            >
              <option value="asc">↑</option>
              <option value="desc">↓</option>
            </select>
          </div>
        </div>
        
        {/* View Mode */}
        <div className="flex items-center space-x-2">
          <button
            onClick={() => setViewMode('grid')}
            className={`p-2 rounded ${viewMode === 'grid' ? 'bg-primary-600 text-white' : 'text-dark-400 hover:text-white'}`}
            title="Grid View"
          >
            <Squares2X2Icon className="h-5 w-5" />
          </button>
          <button
            onClick={() => setViewMode('list')}
            className={`p-2 rounded ${viewMode === 'list' ? 'bg-primary-600 text-white' : 'text-dark-400 hover:text-white'}`}
            title="List View"
          >
            <ListBulletIcon className="h-5 w-5" />
          </button>
          
          <div className="w-px h-6 bg-dark-600 mx-2"></div>
          
          <button
            onClick={() => setShowPreloadSettings(true)}
            className="p-2 rounded text-dark-400 hover:text-white"
            title="Preload Settings"
          >
            <Cog6ToothIcon className="h-5 w-5" />
          </button>
        </div>
      </div>

      {/* Content */}
      {isLoading ? (
        <LoadingSpinner text="Loading images..." />
      ) : viewer.images.length === 0 ? (
        <div className="text-center py-12">
          <EyeIcon className="h-16 w-16 text-dark-500 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-dark-300 mb-2">No images found</h3>
          <p className="text-dark-500 mb-6">
            {searchQuery ? 'Try adjusting your search query' : 'This collection appears to be empty'}
          </p>
          {!searchQuery && (
            <button
              onClick={handleRescan}
              className="btn btn-primary"
            >
              <ArrowPathIcon className="h-4 w-4 mr-2" />
              Rescan Collection
            </button>
          )}
        </div>
      ) : (
        <>
          <div className="flex-1">
            {viewMode === 'grid' ? (
              <ImageGrid images={viewer.images} onImageClick={handleImageClick} collectionId={id!} />
            ) : (
              <ImageList images={viewer.images} onImageClick={handleImageClick} collectionId={id!} />
            )}
          </div>
            
          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-center mt-8 space-x-2">
              <button
                onClick={() => loadImages(currentPage - 1)}
                disabled={currentPage === 1}
                className="btn btn-secondary disabled:opacity-50"
              >
                Previous
              </button>
              
              <span className="text-dark-400 px-4">
                Page {currentPage} of {totalPages}
              </span>
              
              <button
                onClick={() => loadImages(currentPage + 1)}
                disabled={currentPage === totalPages}
                className="btn btn-secondary disabled:opacity-50"
              >
                Next
              </button>
            </div>
          )}
        </>
      )}
      
      {/* Preload Settings Modal */}
      <PreloadSettings
        isOpen={showPreloadSettings}
        onClose={() => setShowPreloadSettings(false)}
      />
    </div>
  );
};

export default CollectionViewerPage;
