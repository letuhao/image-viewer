import React, { useState, useEffect } from 'react';
import {
  CheckCircleIcon,
  ClockIcon,
  FolderIcon,
  PhotoIcon,
} from '@heroicons/react/24/outline';
import { collectionsApi } from '../services/api';
import JobProgressMonitor from './JobProgressMonitor';
import toast from 'react-hot-toast';

interface CacheGenerationSectionProps {
  isOpen: boolean;
  onClose: () => void;
}

interface CacheQualityOption {
  id: string;
  name: string;
  description: string;
  quality: number;
  maxWidth?: number;
  maxHeight?: number;
  format: 'jpeg' | 'webp' | 'original';
}

interface Collection {
  id: string;
  name: string;
  path: string;
  type: 'folder' | 'zip';
  settings?: {
    total_images?: number;
    total_size?: number;
  };
  statistics?: {
    view_count: number;
    total_view_time: number;
  };
}

const CacheGenerationSection: React.FC<CacheGenerationSectionProps> = ({ isOpen, onClose }) => {
  const [collections, setCollections] = useState<Collection[]>([]);
  const [selectedCollections, setSelectedCollections] = useState<string[]>([]);
  const [selectedQuality, setSelectedQuality] = useState<string>('optimize');
  const [overwriteExisting, setOverwriteExisting] = useState(false);
  const [isGenerating, setIsGenerating] = useState(false);
  const [currentJobId, setCurrentJobId] = useState<string | null>(() => {
    // Load job ID from localStorage on component mount
    return localStorage.getItem('cacheGenerationJobId');
  });
  const [isLoadingCollections, setIsLoadingCollections] = useState(true);
  
  // Pagination state
  const [currentPage, setCurrentPage] = useState(1);
  const [itemsPerPage, setItemsPerPage] = useState(50);
  const [totalCollections, setTotalCollections] = useState(0);

  const qualityOptions: CacheQualityOption[] = [
    {
      id: 'perfect',
      name: 'Perfect (100%)',
      description: 'Maximum quality, preserve original details',
      quality: 100,
      format: 'jpeg',
    },
    {
      id: 'high',
      name: 'High Quality (95%)',
      description: 'Best quality, larger file size',
      quality: 95,
      format: 'jpeg',
    },
    {
      id: 'optimize',
      name: 'Optimized (85%)',
      description: 'Balanced quality and file size - Recommended',
      quality: 85,
      format: 'jpeg',
    },
    {
      id: 'medium',
      name: 'Medium (75%)',
      description: 'Good quality, smaller file size',
      quality: 75,
      format: 'jpeg',
    },
    {
      id: 'low',
      name: 'Low (60%)',
      description: 'Smaller file size, faster loading',
      quality: 60,
      format: 'jpeg',
    },
    {
      id: 'webp',
      name: 'WebP (85%)',
      description: 'Modern format, excellent compression',
      quality: 85,
      format: 'webp',
    },
    {
      id: 'webp-high',
      name: 'WebP High (95%)',
      description: 'Modern format with high quality',
      quality: 95,
      format: 'webp',
    },
    {
      id: 'original',
      name: 'Original Quality',
      description: 'Keep original quality and format',
      quality: 100,
      format: 'original',
    },
  ];

  useEffect(() => {
    if (isOpen) {
      loadCollections();
    }
  }, [isOpen]);

  // Save job ID to localStorage when it changes
  useEffect(() => {
    if (currentJobId) {
      localStorage.setItem('cacheGenerationJobId', currentJobId);
    } else {
      localStorage.removeItem('cacheGenerationJobId');
    }
  }, [currentJobId]);

  const loadCollections = async (page = 1, limit = 50) => {
    try {
      setIsLoadingCollections(true);
      const response = await collectionsApi.getAll({ 
        page, 
        limit,
        // Add any additional filters here
      });
      
      setCollections(response.data.collections || []);
      setTotalCollections(response.data.pagination?.total || response.data.collections?.length || 0);
      setCurrentPage(page);
    } catch (error) {
      toast.error('Failed to load collections');
      console.error('Error loading collections:', error);
    } finally {
      setIsLoadingCollections(false);
    }
  };

  const handleSelectAll = () => {
    if (selectedCollections.length === collections.length) {
      setSelectedCollections([]);
    } else {
      setSelectedCollections(collections.map(col => col.id));
    }
  };

  const handleSelectPage = () => {
    setSelectedCollections(collections.map(col => col.id));
  };

  const handleGenerateAll = async () => {
    if (!confirm(`Are you sure you want to generate cache for ALL ${totalCollections} collections? This may take a very long time.`)) {
      return;
    }

    try {
      setIsGenerating(true);
      
      const qualityOption = qualityOptions.find(q => q.id === selectedQuality);
      if (!qualityOption) {
        toast.error('Invalid quality option selected');
        return;
      }

      const response = await collectionsApi.generateCache({
        collectionIds: [], // Empty array means generate for all collections
        quality: qualityOption.quality,
        format: qualityOption.format,
        overwrite: overwriteExisting,
      });

      setCurrentJobId(response.data.jobId);
      toast.success(`Cache generation started for ALL ${totalCollections} collections`);
    } catch (error: any) {
      toast.error(error.response?.data?.error || 'Failed to start cache generation');
      console.error('Error starting cache generation:', error);
    } finally {
      setIsGenerating(false);
    }
  };

  const handleCollectionSelect = (collectionId: string) => {
    setSelectedCollections(prev =>
      prev.includes(collectionId)
        ? prev.filter(id => id !== collectionId)
        : [...prev, collectionId]
    );
  };

  const startCacheGeneration = async () => {
    if (selectedCollections.length === 0) {
      toast.error('Please select at least one collection');
      return;
    }

    try {
      setIsGenerating(true);
      
      const qualityOption = qualityOptions.find(q => q.id === selectedQuality);
      if (!qualityOption) {
        toast.error('Invalid quality option selected');
        return;
      }

      const response = await collectionsApi.generateCache({
        collectionIds: selectedCollections,
        quality: qualityOption.quality,
        format: qualityOption.format,
        overwrite: overwriteExisting,
      });

      setCurrentJobId(response.data.jobId);
      toast.success(`Cache generation started for ${selectedCollections.length} collections`);
    } catch (error: any) {
      toast.error(error.response?.data?.error || 'Failed to start cache generation');
      console.error('Error starting cache generation:', error);
    } finally {
      setIsGenerating(false);
    }
  };

  const [collectionCacheStatus, setCollectionCacheStatus] = useState<Map<string, any>>(new Map());

  // Load cache status for all collections
  useEffect(() => {
    const loadCacheStatus = async () => {
      if (collections.length === 0) return;
      
      const statusMap = new Map();
      
      // Load cache status for all collections in parallel
      const promises = collections.map(async (collection) => {
        try {
          const response = await collectionsApi.getCacheStatus(collection.id);
          statusMap.set(collection.id, response.data);
        } catch (error) {
          console.error(`Failed to load cache status for collection ${collection.id}:`, error);
          // Set default status for failed requests
          statusMap.set(collection.id, {
            hasCache: false,
            cachedImages: 0,
            totalImages: 0,
            cachePercentage: 0,
            lastGenerated: null
          });
        }
      });
      
      await Promise.all(promises);
      setCollectionCacheStatus(statusMap);
    };

    loadCacheStatus();
  }, [collections]);

  const getCollectionStatus = (collection: Collection) => {
    // Get cache status from state (loaded from API)
    const status = collectionCacheStatus.get(collection.id);
    if (status) {
      return {
        hasCache: status.hasCache,
        cacheSize: status.cachedImages || 0,
        lastGenerated: status.lastGenerated ? new Date(status.lastGenerated) : null,
        cachePercentage: status.cachePercentage || 0
      };
    }
    
    // Fallback to mock data if not loaded yet
    return {
      hasCache: false,
      cacheSize: 0,
      lastGenerated: null,
      cachePercentage: 0
    };
  };

  const getTotalCacheSize = () => {
    return collections.reduce((total, collection) => {
      const status = getCollectionStatus(collection);
      return total + (status.hasCache ? status.cacheSize : 0);
    }, 0);
  };

  const getCachedCollectionsCount = () => {
    return collections.filter(collection => getCollectionStatus(collection).hasCache).length;
  };

  if (!isOpen) return null;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold text-white">Cache Generation</h2>
          <p className="text-dark-400 text-sm mt-1">
            Pre-generate image cache for fast navigation and viewing
          </p>
        </div>
      </div>

      {/* Statistics */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-dark-800 rounded-lg p-4">
          <div className="flex items-center space-x-3">
            <FolderIcon className="h-8 w-8 text-blue-500" />
            <div>
              <p className="text-sm text-dark-400">Total Collections</p>
              <p className="text-lg font-semibold text-white">{totalCollections.toLocaleString()}</p>
            </div>
          </div>
        </div>
        <div className="bg-dark-800 rounded-lg p-4">
          <div className="flex items-center space-x-3">
            <CheckCircleIcon className="h-8 w-8 text-green-500" />
            <div>
              <p className="text-sm text-dark-400">Cached Collections</p>
              <p className="text-lg font-semibold text-white">{getCachedCollectionsCount()}</p>
            </div>
          </div>
        </div>
        <div className="bg-dark-800 rounded-lg p-4">
          <div className="flex items-center space-x-3">
            <PhotoIcon className="h-8 w-8 text-purple-500" />
            <div>
              <p className="text-sm text-dark-400">Total Cache Size</p>
              <p className="text-lg font-semibold text-white">{getTotalCacheSize()} MB</p>
            </div>
          </div>
        </div>
      </div>

      {/* Generation Options */}
      <div className="bg-dark-800 rounded-lg p-6">
        <h3 className="text-lg font-medium text-white mb-4">Generation Options</h3>
        
        <div className="space-y-4">
          {/* Quality Selection */}
          <div>
            <label className="block text-sm font-medium text-dark-300 mb-2">
              Cache Quality
            </label>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
              {qualityOptions.map((option) => (
                <label
                  key={option.id}
                  className={`relative flex items-start space-x-3 p-3 rounded-lg border cursor-pointer transition-colors ${
                    selectedQuality === option.id
                      ? 'border-primary-500 bg-primary-500/10'
                      : 'border-dark-600 hover:border-dark-500'
                  }`}
                >
                  <input
                    type="radio"
                    name="quality"
                    value={option.id}
                    checked={selectedQuality === option.id}
                    onChange={(e) => setSelectedQuality(e.target.value)}
                    className="mt-1 h-4 w-4 text-primary-600 border-dark-600 focus:ring-primary-500"
                  />
                  <div className="flex-1">
                    <p className="text-sm font-medium text-white">{option.name}</p>
                    <p className="text-xs text-dark-400">{option.description}</p>
                    <p className="text-xs text-dark-500 mt-1">
                      Quality: {option.quality}% | Format: {option.format.toUpperCase()} | Preserves original dimensions
                    </p>
                  </div>
                </label>
              ))}
            </div>
          </div>

          {/* Overwrite Option */}
          <div className="flex items-center space-x-3">
            <input
              type="checkbox"
              id="overwrite"
              checked={overwriteExisting}
              onChange={(e) => setOverwriteExisting(e.target.checked)}
              className="h-4 w-4 text-primary-600 rounded border-dark-600 focus:ring-primary-500"
            />
            <label htmlFor="overwrite" className="text-sm text-dark-300">
              Overwrite existing cache files
            </label>
          </div>
        </div>
      </div>

      {/* Collection Selection */}
      <div className="bg-dark-800 rounded-lg p-6">
        <div className="flex items-center justify-between mb-4">
          <div>
            <h3 className="text-lg font-medium text-white">Select Collections</h3>
            <p className="text-sm text-dark-400">
              Showing {collections.length} of {totalCollections.toLocaleString()} collections
            </p>
          </div>
          <div className="flex items-center space-x-2">
            <button
              onClick={handleSelectPage}
              className="text-sm text-primary-500 hover:text-primary-400"
            >
              Select Page
            </button>
            <span className="text-dark-500">|</span>
            <button
              onClick={handleSelectAll}
              className="text-sm text-primary-500 hover:text-primary-400"
            >
              Select All
            </button>
          </div>
        </div>

        {isLoadingCollections ? (
          <div className="text-center py-8">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-500 mx-auto"></div>
            <p className="text-dark-400 mt-2">Loading collections...</p>
          </div>
        ) : (
          <div className="space-y-2 max-h-96 overflow-y-auto">
            {collections.map((collection) => {
              const status = getCollectionStatus(collection);
              const isSelected = selectedCollections.includes(collection.id);
              
              return (
                <div
                  key={collection.id}
                  className={`flex items-center space-x-3 p-3 rounded-lg border cursor-pointer transition-colors ${
                    isSelected
                      ? 'border-primary-500 bg-primary-500/10'
                      : 'border-dark-600 hover:border-dark-500'
                  }`}
                  onClick={() => handleCollectionSelect(collection.id)}
                >
                  <input
                    type="checkbox"
                    checked={isSelected}
                    onChange={() => handleCollectionSelect(collection.id)}
                    className="h-4 w-4 text-primary-600 rounded border-dark-600 focus:ring-primary-500"
                  />
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-white truncate">{collection.name}</p>
                    <p className="text-xs text-dark-400 truncate">{collection.path}</p>
                    <div className="flex items-center space-x-4 mt-1">
                      <span className="text-xs text-dark-500">
                        {collection.settings?.total_images || 0} images
                      </span>
                      {status.hasCache ? (
                        <span className="text-xs text-green-500 flex items-center">
                          <CheckCircleIcon className="h-3 w-3 mr-1" />
                          Cached ({status.cacheSize} images, {status.cachePercentage}%)
                        </span>
                      ) : (
                        <span className="text-xs text-orange-500 flex items-center">
                          <ClockIcon className="h-3 w-3 mr-1" />
                          Not cached
                        </span>
                      )}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )}
        
        {/* Pagination */}
        {totalCollections > itemsPerPage && (
          <div className="flex items-center justify-between mt-6 pt-4 border-t border-dark-600">
            <div className="flex items-center space-x-2">
              <span className="text-sm text-dark-400">Items per page:</span>
              <select
                value={itemsPerPage}
                onChange={(e) => {
                  setItemsPerPage(Number(e.target.value));
                  setCurrentPage(1);
                  loadCollections(1, Number(e.target.value));
                }}
                className="bg-dark-700 text-white text-sm rounded px-2 py-1 focus:ring-primary-500 focus:border-primary-500"
              >
                <option value={25}>25</option>
                <option value={50}>50</option>
                <option value={100}>100</option>
                <option value={200}>200</option>
              </select>
            </div>
            
            <div className="flex items-center space-x-2">
              <button
                onClick={() => loadCollections(currentPage - 1, itemsPerPage)}
                disabled={currentPage === 1}
                className="btn btn-ghost btn-sm disabled:opacity-50"
              >
                Previous
              </button>
              
              <span className="text-sm text-dark-400">
                Page {currentPage} of {Math.ceil(totalCollections / itemsPerPage)}
              </span>
              
              <button
                onClick={() => loadCollections(currentPage + 1, itemsPerPage)}
                disabled={currentPage >= Math.ceil(totalCollections / itemsPerPage)}
                className="btn btn-ghost btn-sm disabled:opacity-50"
              >
                Next
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Action Buttons */}
      <div className="flex items-center justify-between">
        <div className="text-sm text-dark-400">
          {selectedCollections.length > 0 && (
            <span>
              {selectedCollections.length} collection{selectedCollections.length > 1 ? 's' : ''} selected
            </span>
          )}
        </div>
        <div className="flex items-center space-x-3">
          <button
            onClick={onClose}
            className="btn btn-ghost"
          >
            Close
          </button>
          <button
            onClick={handleGenerateAll}
            disabled={isGenerating}
            className="btn btn-secondary"
          >
            {isGenerating ? (
              <>
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                Starting...
              </>
            ) : (
              <>
                <svg className="h-4 w-4 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                </svg>
                Generate All ({totalCollections.toLocaleString()})
              </>
            )}
          </button>
          <button
            onClick={startCacheGeneration}
            disabled={selectedCollections.length === 0 || isGenerating}
            className="btn btn-primary"
          >
            {isGenerating ? (
              <>
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                Starting...
              </>
            ) : (
              <>
                <svg className="h-4 w-4 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                </svg>
                Generate Selected ({selectedCollections.length})
              </>
            )}
          </button>
        </div>
      </div>

      {/* Background Job Monitor */}
      {currentJobId && (
        <JobProgressMonitor 
          jobId={currentJobId} 
          onJobCompleted={() => {
            setCurrentJobId(null);
            // Refresh collections to show updated cache status
            loadCollections(currentPage, itemsPerPage);
          }}
          onJobFailed={() => {
            setCurrentJobId(null);
          }}
        />
      )}
    </div>
  );
};

export default CacheGenerationSection;
