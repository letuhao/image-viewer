import React, { useState, useEffect } from 'react';
import { 
  PhotoIcon, 
  ArrowPathIcon, 
  CheckCircleIcon, 
  XCircleIcon,
  PlayIcon,
  StopIcon,
  ExclamationTriangleIcon
} from '@heroicons/react/24/outline';
import toast from 'react-hot-toast';

interface ThumbnailRegenerateSectionProps {
  className?: string;
  isOpen: boolean;
  onClose: () => void;
}

interface RegenerateJob {
  id: string;
  status: 'pending' | 'running' | 'completed' | 'failed';
  progress: {
    current: number;
    total: number;
    currentCollection?: string;
  };
  results: {
    success: number;
    failed: number;
    errors: Array<{
      collectionId: string;
      error: string;
    }>;
  };
  startedAt?: Date;
  completedAt?: Date;
}

const ThumbnailRegenerateSection: React.FC<ThumbnailRegenerateSectionProps> = ({ className = '' }) => {
  const [collections, setCollections] = useState<any[]>([]);
  const [selectedCollections, setSelectedCollections] = useState<string[]>([]);
  const [regenerateJob, setRegenerateJob] = useState<RegenerateJob | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [filter, setFilter] = useState<'all' | 'with-thumbnails' | 'without-thumbnails'>('all');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCollections, setTotalCollections] = useState(0);
  const collectionsPerPage = 50;

  useEffect(() => {
    loadCollections();
  }, [currentPage, filter]);

  const loadCollections = async () => {
    try {
      setIsLoading(true);
      const params = new URLSearchParams({
        page: currentPage.toString(),
        limit: collectionsPerPage.toString(),
        filter: filter
      });
      
      const response = await fetch(`/api/collections?${params}`);
      if (response.ok) {
        const data = await response.json();
        setCollections(data.collections || data); // Support both new and old API format
        if (data.pagination) {
          setTotalPages(data.pagination.totalPages);
          setTotalCollections(data.pagination.total);
        } else {
          // Fallback for old API format
          setTotalPages(1);
          setTotalCollections(data.length || 0);
        }
      } else {
        toast.error('Failed to load collections');
      }
    } catch (error) {
      toast.error('Error loading collections');
    } finally {
      setIsLoading(false);
    }
  };

  // Collections are already filtered by backend
  const filteredCollections = collections;

  const handleSelectAll = () => {
    if (selectedCollections.length === filteredCollections.length) {
      setSelectedCollections([]);
    } else {
      setSelectedCollections(filteredCollections.map(c => c.id));
    }
  };

  const handleSelectCollection = (collectionId: string) => {
    setSelectedCollections(prev => 
      prev.includes(collectionId) 
        ? prev.filter(id => id !== collectionId)
        : [...prev, collectionId]
    );
  };

  const startRegenerateJob = async () => {
    if (selectedCollections.length === 0) {
      toast.error('Please select at least one collection');
      return;
    }

    try {
      setIsLoading(true);
      const response = await fetch('/api/collections/batch-regenerate-thumbnails', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          collectionIds: selectedCollections
        }),
      });

      if (response.ok) {
        const job = await response.json();
        setRegenerateJob(job);
        toast.success(`Started regenerating thumbnails for ${selectedCollections.length} collections`);
        
        // Start polling for updates
        pollJobStatus(job.id);
      } else {
        const error = await response.json();
        toast.error(error.error || 'Failed to start regenerate job');
      }
    } catch (error) {
      toast.error('Error starting regenerate job');
    } finally {
      setIsLoading(false);
    }
  };

  const generateAllThumbnails = async () => {
    try {
      setIsLoading(true);
      const response = await fetch('/api/collections/generate-all-thumbnails', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        const job = await response.json();
        setRegenerateJob(job);
        toast.success(`Started generating thumbnails for ALL ${totalCollections} collections`);
        
        // Start polling for updates
        pollJobStatus(job.jobId);
      } else {
        const error = await response.json();
        toast.error(error.error || 'Failed to start generate all thumbnails job');
      }
    } catch (error) {
      toast.error('Error starting generate all thumbnails job');
    } finally {
      setIsLoading(false);
    }
  };

  const pollJobStatus = async (jobId: string) => {
    const poll = async () => {
      try {
        const response = await fetch(`/api/background/jobs/${jobId}`);
        if (response.ok) {
          const job = await response.json();
          setRegenerateJob(job);
          
          if (job.status === 'completed' || job.status === 'failed') {
            toast.success(`Regenerate job completed: ${job.results.success} success, ${job.results.failed} failed`);
            return;
          }
          
          // Continue polling if still running
          setTimeout(poll, 1000);
        }
      } catch (error) {
        console.error('Error polling job status:', error);
        setTimeout(poll, 5000); // Retry after 5 seconds
      }
    };
    
    poll();
  };

  const cancelJob = async () => {
    if (!regenerateJob) return;
    
    try {
      const response = await fetch(`/api/background/jobs/${regenerateJob.id}/cancel`, {
        method: 'POST',
      });
      
      if (response.ok) {
        setRegenerateJob(null);
        toast.success('Regenerate job cancelled');
      } else {
        toast.error('Failed to cancel job');
      }
    } catch (error) {
      toast.error('Error cancelling job');
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'completed':
        return <CheckCircleIcon className="h-5 w-5 text-green-500" />;
      case 'failed':
        return <XCircleIcon className="h-5 w-5 text-red-500" />;
      case 'running':
        return <ArrowPathIcon className="h-5 w-5 text-blue-500 animate-spin" />;
      default:
        return <ExclamationTriangleIcon className="h-5 w-5 text-yellow-500" />;
    }
  };

  const getStatusText = (status: string) => {
    switch (status) {
      case 'pending':
        return 'Pending';
      case 'running':
        return 'Running';
      case 'completed':
        return 'Completed';
      case 'failed':
        return 'Failed';
      default:
        return 'Unknown';
    }
  };

  return (
    <div className={`space-y-6 ${className}`}>
      {/* Header */}
      <div className="flex items-center space-x-3">
        <PhotoIcon className="h-6 w-6 text-primary-500" />
        <div>
          <h3 className="text-lg font-semibold text-white">Thumbnail Management</h3>
          <p className="text-sm text-gray-400">Regenerate collection thumbnails</p>
        </div>
      </div>

      {/* Filter and Selection */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-4">
            <div className="flex items-center space-x-2">
              <label className="text-sm font-medium text-gray-300">Filter:</label>
              <select
                value={filter}
                onChange={(e) => setFilter(e.target.value as any)}
                className="px-3 py-1 border border-gray-600 rounded-md bg-dark-600 text-white text-sm"
              >
                <option value="all">All Collections</option>
                <option value="with-thumbnails">With Thumbnails</option>
                <option value="without-thumbnails">Without Thumbnails</option>
              </select>
            </div>
            
            <button
              onClick={handleSelectAll}
              className="text-sm text-primary-400 hover:text-primary-300"
            >
              {selectedCollections.length === filteredCollections.length ? 'Deselect All' : 'Select All'}
            </button>
          </div>

          <div className="text-sm text-gray-400">
            {selectedCollections.length} of {filteredCollections.length} selected
          </div>
        </div>

        {/* Collections List */}
        <div className="h-[400px] overflow-y-auto border border-gray-600 rounded-lg bg-dark-800">
          {isLoading ? (
            <div className="p-4 text-center text-gray-400">
              <ArrowPathIcon className="h-6 w-6 animate-spin mx-auto mb-2" />
              Loading collections...
            </div>
          ) : filteredCollections.length === 0 ? (
            <div className="p-4 text-center text-gray-400">
              No collections found
            </div>
          ) : (
            <div className="divide-y divide-gray-600">
              {filteredCollections.map((collection) => (
                <div
                  key={collection.id}
                  className="p-4 hover:bg-dark-700 transition-colors min-h-[60px] flex items-center"
                >
                  <div className="flex items-center space-x-4 w-full">
                    <input
                      type="checkbox"
                      checked={selectedCollections.includes(collection.id)}
                      onChange={() => handleSelectCollection(collection.id)}
                      className="rounded border-gray-600 bg-dark-600 text-primary-500 focus:ring-primary-500 h-4 w-4"
                    />
                    
                    <div className="flex-1 min-w-0 space-y-1">
                      <div className="flex items-center space-x-3">
                        <h4 className="text-sm font-medium text-white truncate flex-1">
                          {collection.name}
                        </h4>
                        {collection.thumbnail_url ? (
                          <CheckCircleIcon className="h-5 w-5 text-green-500 flex-shrink-0" />
                        ) : (
                          <XCircleIcon className="h-5 w-5 text-red-500 flex-shrink-0" />
                        )}
                      </div>
                      <p className="text-xs text-gray-400 truncate">
                        {collection.path}
                      </p>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Action Buttons */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-3">
          <button
            onClick={generateAllThumbnails}
            disabled={isLoading || regenerateJob?.status === 'running'}
            className="btn btn-secondary flex items-center space-x-2"
          >
            <ArrowPathIcon className="h-4 w-4" />
            <span>Generate All ({totalCollections})</span>
          </button>

          <button
            onClick={startRegenerateJob}
            disabled={selectedCollections.length === 0 || isLoading || regenerateJob?.status === 'running'}
            className="btn btn-primary flex items-center space-x-2"
          >
            <PlayIcon className="h-4 w-4" />
            <span>Regenerate Selected ({selectedCollections.length})</span>
          </button>

          {regenerateJob?.status === 'running' && (
            <button
              onClick={cancelJob}
              className="btn btn-danger flex items-center space-x-2"
            >
              <StopIcon className="h-4 w-4" />
              <span>Cancel</span>
            </button>
          )}
        </div>

        {/* Pagination */}
        <div className="flex items-center space-x-2">
          <button
            onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
            disabled={currentPage === 1}
            className="btn btn-sm btn-secondary"
          >
            Previous
          </button>
          <span className="text-sm text-gray-400">
            Page {currentPage} of {totalPages} ({totalCollections} total)
          </span>
          <button
            onClick={() => setCurrentPage(prev => Math.min(totalPages, prev + 1))}
            disabled={currentPage === totalPages}
            className="btn btn-sm btn-secondary"
          >
            Next
          </button>
        </div>
      </div>

      {/* Job Progress */}
      {regenerateJob && (
        <div className="bg-dark-700 rounded-lg p-6 min-h-[200px]">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center space-x-3">
              {getStatusIcon(regenerateJob.status)}
              <span className="text-white font-medium text-lg">
                {getStatusText(regenerateJob.status)}
              </span>
            </div>
            
            {regenerateJob.progress && (
              <span className="text-sm text-gray-400 bg-dark-600 px-3 py-1 rounded-full">
                {regenerateJob.progress.current} / {regenerateJob.progress.total}
              </span>
            )}
          </div>

          {/* Progress Bar */}
          {regenerateJob.progress && (
            <div className="mb-4">
              <div className="bg-dark-600 rounded-full h-3">
                <div
                  className="bg-primary-500 h-3 rounded-full transition-all duration-300"
                  style={{
                    width: `${(regenerateJob.progress.current / regenerateJob.progress.total) * 100}%`
                  }}
                />
              </div>
              <div className="text-right text-xs text-gray-400 mt-1">
                {Math.round((regenerateJob.progress.current / regenerateJob.progress.total) * 100)}%
              </div>
            </div>
          )}

          {/* Current Collection */}
          {regenerateJob.progress?.currentCollection && (
            <div className="text-sm text-gray-400 mb-4 p-3 bg-dark-600 rounded-lg">
              <span className="font-medium">Processing:</span> {regenerateJob.progress.currentCollection}
            </div>
          )}

          {/* Results */}
          {regenerateJob.results && (
            <div className="grid grid-cols-2 gap-4 text-sm mb-4">
              <div className="flex items-center space-x-2 p-3 bg-green-900/20 rounded-lg">
                <CheckCircleIcon className="h-5 w-5 text-green-500" />
                <span className="text-green-400 font-medium">{regenerateJob.results.success} successful</span>
              </div>
              <div className="flex items-center space-x-2 p-3 bg-red-900/20 rounded-lg">
                <XCircleIcon className="h-5 w-5 text-red-500" />
                <span className="text-red-400 font-medium">{regenerateJob.results.failed} failed</span>
              </div>
            </div>
          )}

          {/* Error Details */}
          {regenerateJob.results?.errors && regenerateJob.results.errors.length > 0 && (
            <div className="mt-4">
              <details className="text-sm">
                <summary className="text-red-400 cursor-pointer font-medium">Show Errors ({regenerateJob.results.errors.length})</summary>
                <div className="mt-3 space-y-2 max-h-32 overflow-y-auto">
                  {regenerateJob.results.errors.map((error, index) => (
                    <div key={index} className="text-red-300 text-xs p-2 bg-red-900/20 rounded">
                      <span className="font-medium">{error.collectionId}:</span> {error.error}
                    </div>
                  ))}
                </div>
              </details>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default ThumbnailRegenerateSection;
