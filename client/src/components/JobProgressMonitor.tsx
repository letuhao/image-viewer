import React, { useState, useEffect } from 'react';
import {
  CheckCircleIcon,
  ExclamationTriangleIcon,
  XCircleIcon,
  ClockIcon,
  ArrowPathIcon,
} from '@heroicons/react/24/outline';

interface JobProgressMonitorProps {
  jobId: string;
  onJobCompleted?: () => void;
  onJobFailed?: () => void;
}

interface JobProgress {
  total: number;
  completed: number;
  percentage: number;
  isRunning: boolean;
  isCancelled: boolean;
  status: 'running' | 'completed' | 'failed' | 'cancelled';
  currentCollection: string | null;
  currentImage: string | null;
  errors: Array<{
    message: string;
    timestamp: string;
    collection?: string;
    image?: string;
  }>;
}

const JobProgressMonitor: React.FC<JobProgressMonitorProps> = ({ jobId, onJobCompleted, onJobFailed }) => {
  const [progress, setProgress] = useState<JobProgress | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchProgress = async () => {
      try {
        const response = await fetch(`/api/jobs/${jobId}`);
        if (!response.ok) {
          throw new Error('Failed to fetch job progress');
        }
        const data = await response.json();
        setProgress(data);
        setError(null);
        
        // Call callbacks when job status changes
        if (data.status === 'completed' && onJobCompleted) {
          onJobCompleted();
        } else if (data.status === 'failed' && onJobFailed) {
          onJobFailed();
        }
      } catch (err: any) {
        setError(err.message);
        console.error('Error fetching job progress:', err);
      } finally {
        setIsLoading(false);
      }
    };

    // Fetch immediately
    fetchProgress();

    // Set up polling every 2 seconds
    const interval = setInterval(fetchProgress, 2000);

    return () => clearInterval(interval);
  }, [jobId]);

  const handleCancel = async () => {
    try {
      const response = await fetch(`/api/jobs/${jobId}/cancel`, {
        method: 'POST',
      });
      if (!response.ok) {
        throw new Error('Failed to cancel job');
      }
      // Refresh progress after cancellation
      setTimeout(() => {
        const event = new Event('refresh');
        window.dispatchEvent(event);
      }, 1000);
    } catch (err: any) {
      console.error('Error cancelling job:', err);
    }
  };

  if (isLoading) {
    return (
      <div className="bg-dark-800 rounded-lg p-4">
        <div className="flex items-center space-x-3">
          <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-primary-500"></div>
          <span className="text-white">Loading job progress...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-dark-800 rounded-lg p-4">
        <div className="flex items-center space-x-3">
          <XCircleIcon className="h-5 w-5 text-red-500" />
          <span className="text-red-400">Error loading progress: {error}</span>
        </div>
      </div>
    );
  }

  if (!progress) {
    return null;
  }

  const getStatusIcon = () => {
    switch (progress.status) {
      case 'completed':
        return <CheckCircleIcon className="h-5 w-5 text-green-500" />;
      case 'failed':
        return <XCircleIcon className="h-5 w-5 text-red-500" />;
      case 'cancelled':
        return <XCircleIcon className="h-5 w-5 text-yellow-500" />;
      default:
        return <ClockIcon className="h-5 w-5 text-blue-500" />;
    }
  };

  const getStatusText = () => {
    switch (progress.status) {
      case 'completed':
        return 'Completed';
      case 'failed':
        return 'Failed';
      case 'cancelled':
        return 'Cancelled';
      default:
        return 'Running';
    }
  };

  const getStatusColor = () => {
    switch (progress.status) {
      case 'completed':
        return 'text-green-400';
      case 'failed':
        return 'text-red-400';
      case 'cancelled':
        return 'text-yellow-400';
      default:
        return 'text-blue-400';
    }
  };

  return (
    <div className="bg-dark-800 rounded-lg p-6">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center space-x-3">
          {getStatusIcon()}
          <div>
            <h3 className="text-lg font-medium text-white">Cache Generation Progress</h3>
            <p className={`text-sm ${getStatusColor()}`}>{getStatusText()}</p>
          </div>
        </div>
        <div className="text-sm text-dark-400">
          Job ID: {jobId}
        </div>
      </div>

      {/* Progress Bar */}
      <div className="mb-4">
        <div className="flex items-center justify-between mb-2">
          <span className="text-sm text-dark-300">Progress</span>
          <span className="text-sm text-dark-300">
            {progress.completed} / {progress.total} images ({progress.percentage}%)
          </span>
        </div>
        <div className="w-full bg-dark-700 rounded-full h-2">
          <div
            className="bg-primary-500 h-2 rounded-full transition-all duration-300"
            style={{ width: `${progress.percentage}%` }}
          />
        </div>
      </div>

      {/* Current Activity */}
      {progress.isRunning && (progress.currentCollection || progress.currentImage) && (
        <div className="mb-4 p-3 bg-dark-700 rounded-lg">
          <div className="flex items-center space-x-2 mb-2">
            <ArrowPathIcon className="h-4 w-4 text-primary-500 animate-spin" />
            <span className="text-sm text-dark-300">Currently processing:</span>
          </div>
          {progress.currentCollection && (
            <div className="text-sm text-white">
              Collection: {progress.currentCollection}
            </div>
          )}
          {progress.currentImage && (
            <div className="text-sm text-dark-400">
              Image: {progress.currentImage}
            </div>
          )}
        </div>
      )}

      {/* Errors */}
      {progress.errors.length > 0 && (
        <div className="mb-4">
          <div className="flex items-center space-x-2 mb-2">
            <ExclamationTriangleIcon className="h-4 w-4 text-yellow-500" />
            <span className="text-sm text-yellow-400">
              {progress.errors.length} error{progress.errors.length > 1 ? 's' : ''}
            </span>
          </div>
          <div className="max-h-32 overflow-y-auto space-y-1">
            {progress.errors.slice(-5).map((error, index) => (
              <div key={index} className="text-xs text-red-400 bg-red-900/20 p-2 rounded">
                <div className="font-medium">{error.message}</div>
                {error.collection && (
                  <div className="text-red-300">Collection: {error.collection}</div>
                )}
                {error.image && (
                  <div className="text-red-300">Image: {error.image}</div>
                )}
                <div className="text-red-500">
                  {new Date(error.timestamp).toLocaleTimeString()}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Actions */}
      {progress.isRunning && (
        <div className="flex items-center space-x-3">
          <button
            onClick={handleCancel}
            className="btn btn-ghost btn-sm text-red-400 hover:text-red-300"
          >
            Cancel Job
          </button>
          <div className="text-xs text-dark-500">
            Progress updates every 2 seconds
          </div>
        </div>
      )}

      {/* Completion Message */}
      {progress.status === 'completed' && (
        <div className="mt-4 p-3 bg-green-900/20 border border-green-500/50 rounded-lg">
          <div className="flex items-center space-x-2">
            <CheckCircleIcon className="h-5 w-5 text-green-500" />
            <span className="text-green-400 font-medium">
              Cache generation completed successfully!
            </span>
          </div>
          <div className="text-sm text-green-300 mt-1">
            Processed {progress.completed} images across all selected collections.
          </div>
        </div>
      )}
    </div>
  );
};

export default JobProgressMonitor;
