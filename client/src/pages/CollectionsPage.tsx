import React, { useState } from 'react';
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
  ClockIcon
} from '@heroicons/react/24/outline';
import useStore from '../store/useStore';
import { collectionsApi, statsApi } from '../services/api';
import AddCollectionModal from '../components/AddCollectionModal';
import BulkAddCollectionsModal from '../components/BulkAddCollectionsModal';
import AnalyticsDashboard from '../components/AnalyticsDashboard';
import TagFilter from '../components/TagFilter';
import BackgroundJobMonitor from '../components/BackgroundJobMonitor';
import toast from 'react-hot-toast';

const CollectionsPage: React.FC = () => {
  const { collections, setCollections } = useStore();
  const [showAddModal, setShowAddModal] = useState(false);
  const [showBulkAddModal, setShowBulkAddModal] = useState(false);
  const [showAnalytics, setShowAnalytics] = useState(false);
  const [showJobMonitor, setShowJobMonitor] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [filterType, setFilterType] = useState<'all' | 'folder' | 'zip' | '7z' | 'rar' | 'tar'>('all');
  const [isScanning, setIsScanning] = useState<number | null>(null);
  const [tagFilters, setTagFilters] = useState<string[]>([]);
  const [filteredByTags, setFilteredByTags] = useState<any[]>([]);
  const [isLoadingTagFilter, setIsLoadingTagFilter] = useState(false);
  const navigate = useNavigate();

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

  const handleScanCollection = async (collectionId: number) => {
    try {
      setIsScanning(collectionId);
      await collectionsApi.scan(collectionId);
      toast.success('Collection scan started');
      
      // Refresh collections after a delay
      setTimeout(async () => {
        const response = await collectionsApi.getAll();
        setCollections(response.data);
      }, 2000);
    } catch (error) {
      toast.error('Failed to scan collection');
    } finally {
      setIsScanning(null);
    }
  };

  const handleDeleteCollection = async (collectionId: number) => {
    if (!window.confirm('Are you sure you want to delete this collection?')) {
      return;
    }

    try {
      await collectionsApi.delete(collectionId);
      setCollections(collections.filter(col => col.id !== collectionId));
      toast.success('Collection deleted successfully');
    } catch (error) {
      toast.error('Failed to delete collection');
    }
  };

  const getCollectionIcon = (type: 'folder' | 'zip') => {
    return type === 'zip' ? ArchiveBoxIcon : FolderIcon;
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
        
        <div className="flex items-center space-x-2">
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
              collectionsApi.getAll().then(response => {
                setCollections(response.data);
              }).catch(() => {
                toast.error('Failed to refresh collections');
              });
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
        <div className="collection-grid">
          {filteredCollections.map((collection) => {
            const Icon = getCollectionIcon(collection.type);
            
            return (
              <div key={collection.id} className="collection-item">
                <div className="flex items-start justify-between mb-3">
                  <div className="flex items-center space-x-3">
                    <Icon className="h-8 w-8 text-primary-500 flex-shrink-0" />
                    <div>
                      <h3 className="font-semibold text-white truncate">
                        {collection.name}
                      </h3>
                      <p className="text-sm text-dark-400 truncate">
                        {collection.path}
                      </p>
                    </div>
                  </div>
                  
                  <div className="flex items-center space-x-1">
                    <button
                      onClick={() => handleScanCollection(collection.id)}
                      disabled={isScanning === collection.id}
                      className="p-1 hover:bg-dark-700 rounded text-dark-400 hover:text-white transition-colors"
                      title="Rescan collection"
                    >
                      <ArrowPathIcon className={`h-4 w-4 ${isScanning === collection.id ? 'animate-spin' : ''}`} />
                    </button>
                    
                    <button
                      onClick={() => handleDeleteCollection(collection.id)}
                      className="p-1 hover:bg-red-600 rounded text-dark-400 hover:text-red-300 transition-colors"
                      title="Delete collection"
                    >
                      <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                      </svg>
                    </button>
                  </div>
                </div>
                
                <div className="flex items-center justify-between text-sm text-dark-400 mb-4">
                  <span className="capitalize">{collection.type}</span>
                  <span>{new Date(collection.created_at).toLocaleDateString()}</span>
                </div>
                
                {/* Statistics */}
                {(collection.statistics || collection.tags) && (
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
                
                <div className="flex items-center justify-between">
                  <button
                    onClick={() => navigate(`/collection/${collection.id}`)}
                    className="btn btn-primary flex-1 mr-2"
                  >
                    View Images
                  </button>
                  
                  <button
                    onClick={() => navigate(`/collection/${collection.id}/viewer`)}
                    className="btn btn-secondary"
                  >
                    Open Viewer
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Add Collection Modal */}
      {showAddModal && (
        <AddCollectionModal
          onClose={() => setShowAddModal(false)}
          onSuccess={(collection) => {
            setCollections([...collections, collection]);
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
            collectionsApi.getAll().then(response => {
              setCollections(response.data);
            }).catch(() => {
              toast.error('Failed to refresh collections');
            });
            setShowBulkAddModal(false);
          }}
          onJobStarted={(jobId) => {
            setShowJobMonitor(true); // Show job monitor when job starts
            toast.success(`Background job started with ID: ${jobId}`);
          }}
        />
      )}
    </div>
  );
};

export default CollectionsPage;
