import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { 
  FolderIcon, 
  ArchiveBoxIcon, 
  PlusIcon,
  MagnifyingGlassIcon,
  FunnelIcon,
  ArrowPathIcon,
  Squares2X2Icon,
  ChartBarIcon,
  EyeIcon,
  TagIcon,
  ClockIcon,
  SparklesIcon
} from '@heroicons/react/24/outline';
// import useStore from '../store/useStore'; // Not needed for pagination
import { collectionsApi, statsApi } from '../services/api';
import AddCollectionModal from '../components/AddCollectionModal';
import BulkAddCollectionsModal from '../components/BulkAddCollectionsModal';
import AnalyticsDashboard from '../components/AnalyticsDashboard';
import TagFilter from '../components/TagFilter';
import BackgroundJobMonitor from '../components/BackgroundJobMonitor';
import toast from 'react-hot-toast';

const CollectionsPage: React.FC = () => {
  const [collections, setLocalCollections] = useState<any[]>([]);
  const [showAddModal, setShowAddModal] = useState(false);
  const [showBulkAddModal, setShowBulkAddModal] = useState(false);
  const [showAnalytics, setShowAnalytics] = useState(false);
  const [showJobMonitor, setShowJobMonitor] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [filterType, setFilterType] = useState<'all' | 'folder' | 'zip' | '7z' | 'rar' | 'tar'>('all');
  const [isScanning, setIsScanning] = useState<string | null>(null);
  const [tagFilters, setTagFilters] = useState<string[]>([]);
  const [filteredByTags, setFilteredByTags] = useState<any[]>([]);
  const [isLoadingTagFilter, setIsLoadingTagFilter] = useState(false);
  
  // Pagination state with sessionStorage
  const [currentPage, setCurrentPage] = useState(() => {
    const savedPage = sessionStorage.getItem('collectionsCurrentPage');
    return savedPage ? parseInt(savedPage) : 1;
  });
  const [totalPages, setTotalPages] = useState(1);
  const [totalCollections, setTotalCollections] = useState(0);
  const [isLoadingCollections, setIsLoadingCollections] = useState(false);
  const [collectionsPerPage, setCollectionsPerPage] = useState(() => {
    const saved = localStorage.getItem('collectionsPerPage');
    return saved ? parseInt(saved) : 20;
  });
  
  // View settings with localStorage
  const [viewMode, setViewMode] = useState<'grid' | 'list' | 'detail'>(() => {
    return (localStorage.getItem('collectionViewMode') as 'grid' | 'list' | 'detail') || 'grid';
  });
  const [cardSize, setCardSize] = useState<'mini' | 'micro-tiny' | 'tiny' | 'micro' | 'small' | 'medium' | 'large' | 'extra-large'>(() => {
    return (localStorage.getItem('collectionCardSize') as any) || 'medium';
  });
  const [compactMode, setCompactMode] = useState(() => {
    return localStorage.getItem('collectionCompactMode') === 'true';
  });
  
  const navigate = useNavigate();
  

  // Load collections with pagination
  const loadCollections = async (page: number = currentPage) => {
    try {
      setIsLoadingCollections(true);
      const response = await collectionsApi.getAll({ 
        page, 
        limit: collectionsPerPage 
      });
      
        if (response.data.pagination) {
          setLocalCollections(response.data.collections || []);
          setTotalPages(response.data.pagination.totalPages);
          setTotalCollections(response.data.pagination.total);
          // Only save page to sessionStorage, don't update state to avoid infinite loop
          sessionStorage.setItem('collectionsCurrentPage', page.toString());
        } else {
          // Fallback for old API format
          setLocalCollections(response.data || []);
          setTotalPages(1);
          setTotalCollections(response.data?.length || 0);
        }
    } catch (error) {
      toast.error('Failed to load collections');
      console.error('Error loading collections:', error);
    } finally {
      setIsLoadingCollections(false);
    }
  };


  // Load collections on component mount and page change
  useEffect(() => {
    loadCollections(currentPage);
  }, [currentPage, collectionsPerPage]);


  // Handle items per page change
  const handleItemsPerPageChange = (newItemsPerPage: number) => {
    setCollectionsPerPage(newItemsPerPage);
    localStorage.setItem('collectionsPerPage', newItemsPerPage.toString()); // Save to localStorage
    setCurrentPage(1); // Reset to first page
    sessionStorage.setItem('collectionsCurrentPage', '1'); // Save to sessionStorage
    loadCollections(1); // Reload with new page size
  };

  // Handle page jump
  const handlePageJump = (page: number) => {
    if (page >= 1 && page <= totalPages) {
      setCurrentPage(page); // Update state
      loadCollections(page);
    }
  };

  // Save view preferences to localStorage
  const saveViewMode = (mode: 'grid' | 'list' | 'detail') => {
    setViewMode(mode);
    localStorage.setItem('collectionViewMode', mode);
  };

  const saveCardSize = (size: 'mini' | 'micro-tiny' | 'tiny' | 'micro' | 'small' | 'medium' | 'large' | 'extra-large') => {
    setCardSize(size);
    localStorage.setItem('collectionCardSize', size);
  };


  const saveCompactMode = (compact: boolean) => {
    setCompactMode(compact);
    localStorage.setItem('collectionCompactMode', compact.toString());
  };

  // Handle tag filter changes
  const handleTagFilterChange = async (tags: string[], operator: 'AND' | 'OR') => {
    setTagFilters(tags);
    
    if (tags.length === 0) {
      setFilteredByTags([]);
      return;
    }

    setIsLoadingTagFilter(true);
    try {
      const response = await statsApi.getCollectionsByTags(tags, operator, 100);
      setFilteredByTags(response.data.collections);
    } catch (error) {
      console.error('Error filtering by tags:', error);
      toast.error('Failed to filter by tags');
      setFilteredByTags([]);
    } finally {
      setIsLoadingTagFilter(false);
    }
  };

  // Get the collections to display
  const getDisplayCollections = () => {
    // If tag filtering is active, use tag-filtered results
    if (tagFilters.length > 0) {
      return filteredByTags.filter(collection => {
        const matchesSearch = collection.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
                             collection.path.toLowerCase().includes(searchQuery.toLowerCase());
        const matchesFilter = filterType === 'all' || collection.type === filterType;
        return matchesSearch && matchesFilter;
      });
    }
    
    // Otherwise use regular collection filtering
    return collections.filter(collection => {
      const matchesSearch = collection.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
                           collection.path.toLowerCase().includes(searchQuery.toLowerCase());
      const matchesFilter = filterType === 'all' || collection.type === filterType;
      return matchesSearch && matchesFilter;
    });
  };

  const filteredCollections = getDisplayCollections();

  const handleScanCollection = async (collectionId: string) => {
    try {
      setIsScanning(collectionId);
      await collectionsApi.scan(collectionId);
      toast.success('Collection scan started');
      
      // Refresh collections after a delay
      setTimeout(() => {
        loadCollections(currentPage);
      }, 2000);
    } catch (error) {
      toast.error('Failed to scan collection');
    } finally {
      setIsScanning(null);
    }
  };

  const handleDeleteCollection = async (collectionId: string) => {
    if (!window.confirm('Are you sure you want to delete this collection?')) {
      return;
    }

    try {
      await collectionsApi.delete(collectionId);
      setLocalCollections(collections.filter(col => col.id !== collectionId.toString()));
      toast.success('Collection deleted successfully');
    } catch (error) {
      toast.error('Failed to delete collection');
    }
  };

  const getCollectionIcon = (type: 'folder' | 'zip') => {
    return type === 'zip' ? ArchiveBoxIcon : FolderIcon;
  };

  // Get CSS classes for different view modes and card sizes
  const getViewClasses = () => {
    if (viewMode === 'grid') {
      const gridClasses = {
        container: 'grid gap-4',
        item: compactMode 
          ? 'card p-3 cursor-pointer transition-all hover:scale-105 hover:shadow-xl'
          : 'card p-4 cursor-pointer transition-all hover:scale-105 hover:shadow-xl'
      };
      
      if (compactMode) {
        // Compact mode - more columns, smaller cards
        switch (cardSize) {
          case 'mini':
            gridClasses.container += ' grid-cols-8 sm:grid-cols-10 md:grid-cols-12 lg:grid-cols-14 xl:grid-cols-16';
            gridClasses.item += ' min-h-[70px]';
            break;
          case 'micro-tiny':
            gridClasses.container += ' grid-cols-7 sm:grid-cols-9 md:grid-cols-11 lg:grid-cols-13 xl:grid-cols-15';
            gridClasses.item += ' min-h-[75px]';
            break;
          case 'tiny':
            gridClasses.container += ' grid-cols-6 sm:grid-cols-8 md:grid-cols-10 lg:grid-cols-12';
            gridClasses.item += ' min-h-[80px]';
            break;
          case 'micro':
            gridClasses.container += ' grid-cols-5 sm:grid-cols-7 md:grid-cols-9 lg:grid-cols-10';
            gridClasses.item += ' min-h-[90px]';
            break;
          case 'small':
            gridClasses.container += ' grid-cols-4 sm:grid-cols-6 md:grid-cols-8';
            gridClasses.item += ' min-h-[100px]';
            break;
          case 'medium':
            gridClasses.container += ' grid-cols-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-6';
            gridClasses.item += ' min-h-[120px]';
            break;
          case 'large':
            gridClasses.container += ' grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5';
            gridClasses.item += ' min-h-[140px]';
            break;
          case 'extra-large':
            gridClasses.container += ' grid-cols-2 sm:grid-cols-3 md:grid-cols-4';
            gridClasses.item += ' min-h-[160px]';
            break;
        }
      } else {
        // Normal mode
        switch (cardSize) {
          case 'mini':
            gridClasses.container += ' grid-cols-6 sm:grid-cols-8 md:grid-cols-10 lg:grid-cols-12';
            gridClasses.item += ' min-h-[90px]';
            break;
          case 'micro-tiny':
            gridClasses.container += ' grid-cols-5 sm:grid-cols-7 md:grid-cols-9 lg:grid-cols-10';
            gridClasses.item += ' min-h-[95px]';
            break;
          case 'tiny':
            gridClasses.container += ' grid-cols-4 sm:grid-cols-6 md:grid-cols-8';
            gridClasses.item += ' min-h-[100px]';
            break;
          case 'micro':
            gridClasses.container += ' grid-cols-4 sm:grid-cols-5 md:grid-cols-6 lg:grid-cols-7';
            gridClasses.item += ' min-h-[110px]';
            break;
          case 'small':
            gridClasses.container += ' grid-cols-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-6';
            gridClasses.item += ' min-h-[120px]';
            break;
          case 'medium':
            gridClasses.container += ' grid-cols-2 sm:grid-cols-3 md:grid-cols-4';
            gridClasses.item += ' min-h-[180px]';
            break;
          case 'large':
            gridClasses.container += ' grid-cols-2 sm:grid-cols-3';
            gridClasses.item += ' min-h-[240px]';
            break;
          case 'extra-large':
            gridClasses.container += ' grid-cols-1 sm:grid-cols-2';
            gridClasses.item += ' min-h-[300px]';
            break;
        }
      }
      return gridClasses;
    } else if (viewMode === 'list') {
      return {
        container: 'space-y-1',
        item: 'flex items-center p-2 bg-dark-700 rounded-lg hover:bg-dark-600 transition-colors cursor-pointer min-h-[48px]'
      };
    } else { // detail
      return {
        container: 'space-y-2',
        item: 'flex items-start p-3 bg-dark-700 rounded-lg hover:bg-dark-600 transition-colors cursor-pointer min-h-[80px]'
      };
    }
  };

  const handleRandomCollection = () => {
    const availableCollections = filteredCollections.filter(col => 
      col.settings && col.settings.total_images > 0
    );
    
    if (availableCollections.length === 0) {
      toast.error('No collections with images available');
      return;
    }
    
    const randomIndex = Math.floor(Math.random() * availableCollections.length);
    const randomCollection = availableCollections[randomIndex];
    
    toast.success(`Opening random collection: ${randomCollection.name}`);
    navigate(`/collection/${randomCollection.id}`);
  };

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-white">Collections</h1>
          <p className="text-dark-400 mt-1">
            Manage your image collections from folders and ZIP files
          </p>
        </div>
        
        <div className="flex items-center space-x-4">
          {/* View Mode Selector */}
          <div className="flex items-center space-x-2">
            <label className="text-sm text-gray-400">View:</label>
            <div className="flex items-center space-x-1">
              <button
                onClick={() => saveViewMode('grid')}
                className={`p-2 rounded ${viewMode === 'grid' ? 'bg-primary-600 text-white' : 'bg-dark-600 text-gray-400 hover:bg-dark-500'}`}
                title="Grid View"
              >
                <Squares2X2Icon className="h-4 w-4" />
              </button>
              <button
                onClick={() => saveViewMode('list')}
                className={`p-2 rounded ${viewMode === 'list' ? 'bg-primary-600 text-white' : 'bg-dark-600 text-gray-400 hover:bg-dark-500'}`}
                title="List View"
              >
                <div className="h-4 w-4 flex flex-col space-y-0.5">
                  <div className="h-1 bg-current rounded"></div>
                  <div className="h-1 bg-current rounded"></div>
                  <div className="h-1 bg-current rounded"></div>
                </div>
              </button>
              <button
                onClick={() => saveViewMode('detail')}
                className={`p-2 rounded ${viewMode === 'detail' ? 'bg-primary-600 text-white' : 'bg-dark-600 text-gray-400 hover:bg-dark-500'}`}
                title="Detail View"
              >
                <div className="h-4 w-4 flex flex-col space-y-0.5">
                  <div className="h-1 bg-current rounded w-3/4"></div>
                  <div className="h-1 bg-current rounded w-1/2"></div>
                  <div className="h-1 bg-current rounded w-2/3"></div>
                </div>
              </button>
            </div>
          </div>

          {/* Card Size Selector (only for grid view) */}
          {viewMode === 'grid' && (
            <div className="flex items-center space-x-2">
              <label className="text-sm text-gray-400">Size:</label>
              <select
                value={cardSize}
                onChange={(e) => saveCardSize(e.target.value as any)}
                className="px-2 py-1 border border-gray-600 rounded bg-dark-600 text-white text-sm"
              >
                <option value="mini">Mini</option>
                <option value="micro-tiny">Micro-Tiny</option>
                <option value="tiny">Tiny</option>
                <option value="micro">Micro</option>
                <option value="small">Small</option>
                <option value="medium">Medium</option>
                <option value="large">Large</option>
                <option value="extra-large">Extra Large</option>
              </select>
            </div>
          )}

          {/* Compact Mode Toggle (only for grid view) */}
          {viewMode === 'grid' && (
            <div className="flex items-center space-x-2">
              <label className="text-sm text-gray-400">Compact:</label>
              <button
                onClick={() => saveCompactMode(!compactMode)}
                className={`px-3 py-1 rounded text-sm transition-colors ${
                  compactMode 
                    ? 'bg-primary-600 text-white' 
                    : 'bg-dark-600 text-gray-400 hover:bg-dark-500'
                }`}
                title={compactMode ? 'Disable compact mode' : 'Enable compact mode'}
              >
                {compactMode ? 'ON' : 'OFF'}
              </button>
            </div>
          )}

          {/* Items per page selector */}
          <div className="flex items-center space-x-2">
            <label className="text-sm text-gray-400">Show:</label>
            <input
              type="number"
              min="1"
              max="500"
              value={collectionsPerPage}
              onChange={(e) => {
                const value = parseInt(e.target.value);
                if (value >= 1 && value <= 500) {
                  handleItemsPerPageChange(value);
                }
              }}
              onBlur={(e) => {
                const value = parseInt(e.target.value);
                if (!value || value < 1) {
                  handleItemsPerPageChange(20); // Reset to default if invalid
                }
              }}
              className="w-20 px-2 py-1 border border-gray-600 rounded-md bg-dark-600 text-white text-sm text-center"
              placeholder="20"
            />
            <span className="text-sm text-gray-400">per page</span>
          </div>

          <div className="flex items-center space-x-2">
            <button
              onClick={handleRandomCollection}
              className="btn btn-secondary"
              title="Open a random collection"
            >
              <SparklesIcon className="h-5 w-5 mr-2" />
              Random
            </button>
            <button
              onClick={() => setShowBulkAddModal(true)}
              className="btn btn-secondary"
            >
              <Squares2X2Icon className="h-5 w-5 mr-2" />
              Bulk Add
            </button>
            <button
              onClick={() => setShowAddModal(true)}
              className="btn btn-primary"
            >
              <PlusIcon className="h-5 w-5 mr-2" />
              Add Collection
            </button>
          </div>
        </div>
      </div>

      {/* Search and Filter */}
      <div className="flex items-center space-x-4 mb-6">
        <div className="relative flex-1 max-w-md">
          <MagnifyingGlassIcon className="absolute left-3 top-1/2 transform -translate-y-1/2 h-5 w-5 text-dark-400" />
          <input
            type="text"
            placeholder="Search collections..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="input pl-10"
          />
        </div>
        
        <div className="flex items-center space-x-2">
          <FunnelIcon className="h-5 w-5 text-dark-400" />
          <select
            value={filterType}
            onChange={(e) => setFilterType(e.target.value as 'all' | 'folder' | 'zip' | '7z' | 'rar' | 'tar')}
            className="input w-32"
          >
            <option value="all">All Types</option>
            <option value="folder">Folders</option>
            <option value="zip">ZIP Files</option>
            <option value="7z">7-Zip Files</option>
            <option value="rar">RAR Files</option>
            <option value="tar">TAR Files</option>
          </select>
        </div>
        
        <button
          onClick={() => setShowAnalytics(!showAnalytics)}
          className={`btn ${showAnalytics ? 'btn-primary' : 'btn-secondary'}`}
        >
          <ChartBarIcon className="h-5 w-5 mr-2" />
          {showAnalytics ? 'Hide Analytics' : 'Show Analytics'}
        </button>
        
        <button
          onClick={() => setShowJobMonitor(!showJobMonitor)}
          className={`btn ${showJobMonitor ? 'btn-primary' : 'btn-secondary'}`}
        >
          <ClockIcon className="h-5 w-5 mr-2" />
          {showJobMonitor ? 'Hide Jobs' : 'Background Jobs'}
        </button>
      </div>

      {/* Analytics Dashboard */}
      {showAnalytics && (
        <div className="mb-8">
          <AnalyticsDashboard />
        </div>
      )}

      {/* Background Job Monitor */}
      {showJobMonitor && (
        <div className="mb-8">
          <BackgroundJobMonitor 
            onJobCompleted={(job) => {
              toast.success(`Background job completed: ${job.results?.message || 'Job finished'}`);
              // Refresh collections when job completes
              loadCollections(currentPage);
            }}
          />
        </div>
      )}

      {/* Tag Filter */}
      <TagFilter onFilterChange={handleTagFilterChange} />

      {/* Loading indicator for tag filtering */}
      {isLoadingTagFilter && (
        <div className="flex items-center justify-center py-4">
          <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600"></div>
          <span className="ml-2 text-sm text-gray-600 dark:text-gray-400">Filtering by tags...</span>
        </div>
      )}

      {/* Collections Grid */}
      {filteredCollections.length === 0 ? (
        <div className="text-center py-12">
          <FolderIcon className="h-16 w-16 text-dark-500 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-dark-300 mb-2">
            {searchQuery || filterType !== 'all' || tagFilters.length > 0 ? 'No collections found' : 'No collections yet'}
          </h3>
          <p className="text-dark-500 mb-6">
            {searchQuery || filterType !== 'all' || tagFilters.length > 0
              ? 'Try adjusting your search, filter criteria, or tag selection'
              : 'Add your first collection to get started'
            }
          </p>
          {!searchQuery && filterType === 'all' && tagFilters.length === 0 && (
            <div className="flex items-center space-x-2">
              <button
                onClick={handleRandomCollection}
                className="btn btn-secondary"
                title="Open a random collection"
              >
                <SparklesIcon className="h-5 w-5 mr-2" />
                Random
              </button>
              <button
                onClick={() => setShowBulkAddModal(true)}
                className="btn btn-secondary"
              >
                <Squares2X2Icon className="h-5 w-5 mr-2" />
                Bulk Add
              </button>
              <button
                onClick={() => setShowAddModal(true)}
                className="btn btn-primary"
              >
                <PlusIcon className="h-5 w-5 mr-2" />
                Add Collection
              </button>
            </div>
          )}
        </div>
      ) : (
        <div className={getViewClasses().container}>
          {filteredCollections.map((collection) => {
            const Icon = getCollectionIcon(collection.type);
            
            if (viewMode === 'grid') {
              return (
                <div key={collection.id} className={getViewClasses().item}>
                {/* Collection Thumbnail - Clickable */}
                <div 
                  className={`${compactMode ? 'mb-2' : 'mb-4'} cursor-pointer relative`}
                  onClick={() => navigate(`/collection/${collection.id}`)}
                  title={`Open ${collection.name}`}
                >
                  {collection.thumbnail_url ? (
                    <div className={`relative ${compactMode ? 'aspect-square' : 'aspect-video'} w-full overflow-hidden rounded-lg bg-dark-700`}>
                      <img 
                        src={collection.thumbnail_url} 
                        alt={`${collection.name} thumbnail`}
                        className="w-full h-full object-cover"
                        onError={(e) => {
                          e.currentTarget.style.display = 'none';
                          const nextElement = e.currentTarget.nextElementSibling as HTMLElement;
                          if (nextElement) {
                            nextElement.style.display = 'flex';
                          }
                        }}
                      />
                      <div className="hidden absolute inset-0 items-center justify-center bg-dark-700">
                        <Icon className={`${compactMode ? 'h-8 w-8' : 'h-16 w-16'} text-dark-500`} />
                      </div>
                      
                      {/* Open Viewer Icon Overlay - Only in normal mode */}
                      {!compactMode && (
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            navigate(`/collection/${collection.id}/viewer`);
                          }}
                          className="absolute top-2 right-2 p-1.5 bg-black/70 hover:bg-black/90 rounded-full transition-colors opacity-0 hover:opacity-100"
                          title="Open in Viewer"
                        >
                          <svg className="h-3 w-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                          </svg>
                        </button>
                      )}
                    </div>
                  ) : (
                    <div className={`${compactMode ? 'aspect-square' : 'aspect-video'} w-full flex items-center justify-center bg-dark-700 rounded-lg relative`}>
                      <Icon className={`${compactMode ? 'h-8 w-8' : 'h-16 w-16'} text-dark-500`} />
                      
                      {/* Open Viewer Icon Overlay - Only in normal mode */}
                      {!compactMode && (
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            navigate(`/collection/${collection.id}/viewer`);
                          }}
                          className="absolute top-2 right-2 p-1.5 bg-black/70 hover:bg-black/90 rounded-full transition-colors opacity-0 hover:opacity-100"
                          title="Open in Viewer"
                        >
                          <svg className="h-3 w-3 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                          </svg>
                        </button>
                      )}
                    </div>
                  )}
                </div>

                {/* Collection Info */}
                <div className={`${compactMode ? 'mb-2' : 'mb-3'}`}>
                  <div className="flex items-start justify-between">
                    <div className={`flex items-center ${compactMode ? 'space-x-2' : 'space-x-3'} flex-1 min-w-0`}>
                      <Icon className={`${compactMode ? 'h-5 w-5' : 'h-8 w-8'} text-primary-500 flex-shrink-0`} />
                      <div className="flex-1 min-w-0">
                        <h3 className={`${compactMode ? 'text-sm' : 'font-semibold'} text-white leading-tight`} style={{ 
                          display: '-webkit-box',
                          WebkitLineClamp: compactMode ? 2 : 3,
                          WebkitBoxOrient: 'vertical',
                          overflow: 'hidden'
                        }}>
                          {collection.name}
                        </h3>
                        {!compactMode && (
                          <p className="text-sm text-dark-400 truncate mt-1">
                            {collection.path}
                          </p>
                        )}
                      </div>
                    </div>
                    
                    {!compactMode && (
                      <div className="flex items-center space-x-1">
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            handleScanCollection(collection.id);
                          }}
                          disabled={isScanning === collection.id}
                          className="p-1 hover:bg-dark-700 rounded text-dark-400 hover:text-white transition-colors"
                          title="Rescan collection"
                        >
                          <ArrowPathIcon className={`h-4 w-4 ${isScanning === collection.id ? 'animate-spin' : ''}`} />
                        </button>
                        
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            handleDeleteCollection(collection.id);
                          }}
                          className="p-1 hover:bg-red-600 rounded text-dark-400 hover:text-red-300 transition-colors"
                          title="Delete collection"
                        >
                          <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                          </svg>
                        </button>
                      </div>
                    )}
                  </div>
                </div>
                
                {/* Date and Type - Only show in normal mode */}
                {!compactMode && (
                  <div className="flex items-center justify-between text-sm text-dark-400 mb-4">
                    <span className="capitalize">{collection.type}</span>
                    <span>{new Date(collection.created_at).toLocaleDateString()}</span>
                  </div>
                )}
                
                {/* Statistics - Only show in normal mode */}
                {!compactMode && (collection.statistics || collection.tags) && (
                  <div className="mb-4 p-3 bg-dark-700 rounded-lg">
                    <div className="flex items-center justify-between text-xs text-dark-300 mb-2">
                      <div className="flex items-center space-x-4">
                        {collection.statistics && (
                          <>
                            <span className="flex items-center">
                              <EyeIcon className="h-3 w-3 mr-1" />
                              {collection.statistics.view_count}
                            </span>
                            <span className="flex items-center">
                              <ClockIcon className="h-3 w-3 mr-1" />
                              {Math.floor((collection.statistics.total_view_time || 0) / 60)}m
                            </span>
                            <span className="flex items-center">
                              <MagnifyingGlassIcon className="h-3 w-3 mr-1" />
                              {collection.statistics.search_count}
                            </span>
                          </>
                        )}
                        {collection.tags && collection.tags.length > 0 && (
                          <span className="flex items-center">
                            <TagIcon className="h-3 w-3 mr-1" />
                            {collection.tags.length}
                          </span>
                        )}
                      </div>
                    </div>
                    
                    {/* Tags Preview */}
                    {collection.tags && collection.tags.length > 0 && (
                      <div className="flex flex-wrap gap-1">
                        {collection.tags.slice(0, 3).map((tagData: any, index: number) => (
                          <span
                            key={index}
                            className="px-2 py-1 bg-primary-600 text-primary-100 text-xs rounded-full"
                          >
                            {tagData.tag}
                          </span>
                        ))}
                        {collection.tags.length > 3 && (
                          <span className="px-2 py-1 bg-dark-600 text-dark-300 text-xs rounded-full">
                            +{collection.tags.length - 3}
                          </span>
                        )}
                      </div>
                    )}
                  </div>
                )}
                
                {/* Action Buttons - Only show in normal mode */}
                {!compactMode && (
                  <div className="flex items-center justify-between">
                    <button
                      onClick={() => navigate(`/collection/${collection.id}`)}
                      className="btn btn-primary flex-1"
                    >
                      View Images
                    </button>
                  </div>
                )}
              </div>
              );
            } else if (viewMode === 'list') {
              return (
                <div key={collection.id} className={getViewClasses().item} onClick={() => navigate(`/collection/${collection.id}`)}>
                  <div className="flex items-center space-x-3 w-full">
                    {/* Icon */}
                    <Icon className="h-6 w-6 text-primary-500 flex-shrink-0" />
                    
                    {/* Name */}
                    <div className="flex-1 min-w-0">
                      <h3 className="font-medium text-white text-sm leading-tight" style={{ 
                        display: '-webkit-box',
                        WebkitLineClamp: 2,
                        WebkitBoxOrient: 'vertical',
                        overflow: 'hidden'
                      }}>
                        {collection.name}
                      </h3>
                      <p className="text-xs text-dark-400 truncate mt-1">
                        {collection.path}
                      </p>
                    </div>
                    
                    {/* Type */}
                    <div className="text-xs text-dark-400 capitalize w-12 text-center">
                      {collection.type}
                    </div>
                    
                    {/* Date */}
                    <div className="text-xs text-dark-400 w-20 text-center">
                      {new Date(collection.created_at).toLocaleDateString()}
                    </div>
                    
                    {/* Statistics */}
                    <div className="text-xs text-dark-400 w-16 text-center">
                      {collection.statistics ? collection.statistics.view_count : 0} views
                    </div>
                    
                    {/* Actions */}
                    <div className="flex items-center space-x-1">
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          navigate(`/collection/${collection.id}/viewer`);
                        }}
                        className="p-1 text-dark-400 hover:text-primary-400 transition-colors"
                        title="Open Viewer"
                      >
                        <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                        </svg>
                      </button>
                      
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          handleScanCollection(collection.id);
                        }}
                        disabled={isScanning === collection.id}
                        className="p-1 text-dark-400 hover:text-primary-400 transition-colors"
                        title="Rescan collection"
                      >
                        <ArrowPathIcon className={`h-4 w-4 ${isScanning === collection.id ? 'animate-spin' : ''}`} />
                      </button>
                      
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          handleDeleteCollection(collection.id);
                        }}
                        className="p-1 text-dark-400 hover:text-red-400 transition-colors"
                        title="Delete collection"
                      >
                        <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                        </svg>
                      </button>
                    </div>
                  </div>
                </div>
              );
            } else { // detail view
              return (
                <div key={collection.id} className={getViewClasses().item} onClick={() => navigate(`/collection/${collection.id}`)}>
                  <div className="flex items-start space-x-3 w-full">
                    {/* Thumbnail */}
                    <div className="flex-shrink-0">
                      {collection.thumbnail_url ? (
                        <div className="relative w-16 h-16 overflow-hidden rounded-lg bg-dark-700">
                          <img 
                            src={collection.thumbnail_url} 
                            alt={`${collection.name} thumbnail`}
                            className="w-full h-full object-cover"
                            onError={(e) => {
                              e.currentTarget.style.display = 'none';
                              const nextElement = e.currentTarget.nextElementSibling as HTMLElement;
                              if (nextElement) {
                                nextElement.style.display = 'flex';
                              }
                            }}
                          />
                          <div className="hidden absolute inset-0 items-center justify-center bg-dark-700">
                            <Icon className="h-6 w-6 text-dark-500" />
                          </div>
                        </div>
                      ) : (
                        <div className="w-16 h-16 flex items-center justify-center bg-dark-700 rounded-lg">
                          <Icon className="h-6 w-6 text-dark-500" />
                        </div>
                      )}
                    </div>
                    
                    {/* Main Content */}
                    <div className="flex-1 min-w-0">
                      <h3 className="text-sm font-semibold text-white leading-tight mb-1" style={{ 
                        display: '-webkit-box',
                        WebkitLineClamp: 2,
                        WebkitBoxOrient: 'vertical',
                        overflow: 'hidden'
                      }}>
                        {collection.name}
                      </h3>
                      <p className="text-xs text-dark-400 truncate mb-2">
                        {collection.path}
                      </p>
                      
                      {/* Details Grid */}
                      <div className="grid grid-cols-3 gap-3 text-xs">
                        <div>
                          <span className="text-dark-400">Type:</span>
                          <span className="text-white ml-1 capitalize">{collection.type}</span>
                        </div>
                        <div>
                          <span className="text-dark-400">Created:</span>
                          <span className="text-white ml-1">{new Date(collection.created_at).toLocaleDateString()}</span>
                        </div>
                        <div>
                          <span className="text-dark-400">Views:</span>
                          <span className="text-white ml-1">{collection.statistics ? collection.statistics.view_count : 0}</span>
                        </div>
                      </div>
                      
                      {/* Tags */}
                      {collection.tags && collection.tags.length > 0 && (
                        <div className="flex flex-wrap gap-1 mt-2">
                          {collection.tags.slice(0, 3).map((tagData: any, index: number) => (
                            <span
                              key={index}
                              className="px-1.5 py-0.5 bg-primary-600 text-primary-100 text-xs rounded"
                            >
                              {tagData.tag}
                            </span>
                          ))}
                          {collection.tags.length > 3 && (
                            <span className="px-1.5 py-0.5 bg-dark-600 text-dark-300 text-xs rounded">
                              +{collection.tags.length - 3}
                            </span>
                          )}
                        </div>
                      )}
                    </div>
                    
                    {/* Actions */}
                    <div className="flex flex-col space-y-1">
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          navigate(`/collection/${collection.id}/viewer`);
                        }}
                        className="btn btn-sm btn-secondary"
                        title="Open Viewer"
                      >
                        <svg className="h-3 w-3 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                        </svg>
                        Viewer
                      </button>
                      
                      <div className="flex items-center space-x-1">
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            handleScanCollection(collection.id);
                          }}
                          disabled={isScanning === collection.id}
                          className="p-1 text-dark-400 hover:text-primary-400 transition-colors"
                          title="Rescan collection"
                        >
                          <ArrowPathIcon className={`h-3 w-3 ${isScanning === collection.id ? 'animate-spin' : ''}`} />
                        </button>
                        
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            handleDeleteCollection(collection.id);
                          }}
                          className="p-1 text-dark-400 hover:text-red-400 transition-colors"
                          title="Delete collection"
                        >
                          <svg className="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                          </svg>
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              );
            }
          })}
        </div>
      )}

      {/* Spacer for fixed pagination footer */}
      {totalPages > 1 && <div className="h-24"></div>}

      {/* Add Collection Modal */}
      {showAddModal && (
        <AddCollectionModal
          onClose={() => setShowAddModal(false)}
          onSuccess={(collection) => {
            setLocalCollections([...collections, collection]);
            setShowAddModal(false);
          }}
        />
      )}

      {/* Bulk Add Collections Modal */}
      {showBulkAddModal && (
        <BulkAddCollectionsModal
          onClose={() => setShowBulkAddModal(false)}
          onSuccess={() => {
            // Refresh collections list
            loadCollections(currentPage);
            setShowBulkAddModal(false);
          }}
          onJobStarted={(jobId) => {
            setShowJobMonitor(true); // Show job monitor when job starts
            toast.success(`Background job started with ID: ${jobId}`);
          }}
        />
      )}

      {/* Fixed Pagination Footer */}
      {totalPages > 1 && (
        <div className="fixed bottom-0 left-0 right-0 bg-dark-800 border-t border-gray-600 p-4 z-40">
          <div className="max-w-7xl mx-auto">
            <div className="flex flex-col items-center space-y-3">
              {/* Page Info */}
              <div className="flex items-center space-x-4">
                <span className="text-sm text-gray-400">
                  Page {currentPage} of {totalPages}
                </span>
                <span className="text-sm text-gray-400">
                  {totalCollections} total collections
                </span>
                
                {/* Page Jump Input */}
                <div className="flex items-center space-x-2">
                  <label className="text-sm text-gray-400">Go to:</label>
                  <input
                    type="number"
                    min="1"
                    max={totalPages}
                    value={currentPage}
                    onChange={(e) => {
                      const page = parseInt(e.target.value);
                      if (page >= 1 && page <= totalPages) {
                        handlePageJump(page);
                      }
                    }}
                    className="w-16 px-2 py-1 border border-gray-600 rounded bg-dark-600 text-white text-sm text-center"
                  />
                </div>

                {isLoadingCollections && (
                  <div className="flex items-center space-x-2 text-sm text-gray-400">
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-primary-500"></div>
                    <span>Loading...</span>
                  </div>
                )}
              </div>

              {/* Navigation Controls */}
              <div className="flex items-center space-x-2">
                {/* Previous Button */}
                <button
                  onClick={() => handlePageJump(currentPage - 1)}
                  disabled={currentPage === 1 || isLoadingCollections}
                  className="btn btn-secondary px-3 py-2"
                >
                  Previous
                </button>

                {/* Page Number Buttons */}
                <div className="flex items-center space-x-1">
                  {(() => {
                    const pages = [];
                    const maxVisiblePages = 7; // Show max 7 page buttons
                    let startPage = 1;
                    let endPage = totalPages;

                    if (totalPages > maxVisiblePages) {
                      if (currentPage <= 4) {
                        // Show first 5 pages + ... + last page
                        startPage = 1;
                        endPage = Math.min(5, totalPages);
                      } else if (currentPage >= totalPages - 3) {
                        // Show first page + ... + last 5 pages
                        startPage = Math.max(1, totalPages - 4);
                        endPage = totalPages;
                      } else {
                        // Show first page + ... + current-1, current, current+1 + ... + last page
                        startPage = currentPage - 1;
                        endPage = currentPage + 1;
                      }
                    }

                    // First page button
                    if (startPage > 1) {
                      pages.push(
                        <button
                          key={1}
                          onClick={() => handlePageJump(1)}
                          disabled={isLoadingCollections}
                          className="btn btn-secondary px-3 py-2"
                        >
                          1
                        </button>
                      );
                      
                      if (startPage > 2) {
                        pages.push(
                          <span key="ellipsis1" className="px-2 text-gray-400">
                            ...
                          </span>
                        );
                      }
                    }

                    // Page number buttons
                    for (let i = startPage; i <= endPage; i++) {
                      pages.push(
                        <button
                          key={i}
                          onClick={() => handlePageJump(i)}
                          disabled={isLoadingCollections}
                          className={`btn px-3 py-2 ${
                            i === currentPage 
                              ? 'btn-primary' 
                              : 'btn-secondary'
                          }`}
                        >
                          {i}
                        </button>
                      );
                    }

                    // Last page button
                    if (endPage < totalPages) {
                      if (endPage < totalPages - 1) {
                        pages.push(
                          <span key="ellipsis2" className="px-2 text-gray-400">
                            ...
                          </span>
                        );
                      }
                      
                      pages.push(
                        <button
                          key={totalPages}
                          onClick={() => handlePageJump(totalPages)}
                          disabled={isLoadingCollections}
                          className="btn btn-secondary px-3 py-2"
                        >
                          {totalPages}
                        </button>
                      );
                    }

                    return pages;
                  })()}
                </div>

                {/* Next Button */}
                <button
                  onClick={() => handlePageJump(currentPage + 1)}
                  disabled={currentPage === totalPages || isLoadingCollections}
                  className="btn btn-secondary px-3 py-2"
                >
                  Next
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default CollectionsPage;
