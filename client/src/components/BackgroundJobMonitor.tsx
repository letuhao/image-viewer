import { useState, useEffect } from 'react';
import { backgroundApi } from '../services/api';
import { 
  StopIcon, 
  ClockIcon, 
  CheckCircleIcon, 
  XCircleIcon,
  ExclamationTriangleIcon,
  ArrowPathIcon
} from '@heroicons/react/24/outline';
import toast from 'react-hot-toast';

interface BackgroundJob {
  id: string;
  type: string;
  data: any;
  status: 'pending' | 'running' | 'completed' | 'failed' | 'cancelled';
  progress: number;
  total: number;
  current: number;
  results?: any;
  error?: string;
  message?: string;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
}

interface BackgroundJobMonitorProps {
  onJobCompleted?: (job: BackgroundJob) => void;
}

export default function BackgroundJobMonitor({ onJobCompleted }: BackgroundJobMonitorProps) {
  const [jobs, setJobs] = useState<BackgroundJob[]>([]);
  const [refreshing, setRefreshing] = useState(false);

  const loadJobs = async () => {
    try {
      const response = await backgroundApi.getAllJobs();
      setJobs(response.jobs || []);
    } catch (error) {
      console.error('Failed to load jobs:', error);
      toast.error('Failed to load background jobs');
    }
  };

  const refreshJob = async (jobId: string) => {
    try {
      const response = await backgroundApi.getJobStatus(jobId);
      if (response.job) {
        setJobs(prev => {
          const updatedJobs = prev.map(job => 
            job.id === jobId ? response.job : job
          );
          
          // Check if job just completed
          const updatedJob = response.job;
          if (updatedJob.status === 'completed' && onJobCompleted) {
            const oldJob = prev.find(j => j.id === jobId);
            if (oldJob && oldJob.status !== 'completed') {
              onJobCompleted(updatedJob);
            }
          }
          
          return updatedJobs;
        });
      }
    } catch (error) {
      console.error('Failed to refresh job:', error);
    }
  };

  const cancelJob = async (jobId: string) => {
    try {
      await backgroundApi.cancelJob(jobId);
      await refreshJob(jobId);
      toast.success('Job cancelled successfully');
    } catch (error) {
      console.error('Failed to cancel job:', error);
      toast.error('Failed to cancel job');
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'pending':
        return <ClockIcon className="h-5 w-5 text-yellow-500" />;
      case 'running':
        return <ArrowPathIcon className="h-5 w-5 text-blue-500 animate-spin" />;
      case 'completed':
        return <CheckCircleIcon className="h-5 w-5 text-green-500" />;
      case 'failed':
        return <XCircleIcon className="h-5 w-5 text-red-500" />;
      case 'cancelled':
        return <StopIcon className="h-5 w-5 text-gray-500" />;
      default:
        return <ClockIcon className="h-5 w-5 text-gray-500" />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'pending':
        return 'bg-yellow-100 text-yellow-800';
      case 'running':
        return 'bg-blue-100 text-blue-800';
      case 'completed':
        return 'bg-green-100 text-green-800';
      case 'failed':
        return 'bg-red-100 text-red-800';
      case 'cancelled':
        return 'bg-gray-100 text-gray-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  const formatDuration = (startedAt?: string, completedAt?: string) => {
    if (!startedAt) return 'Not started';
    
    const start = new Date(startedAt);
    const end = completedAt ? new Date(completedAt) : new Date();
    const diff = end.getTime() - start.getTime();
    
    const seconds = Math.floor(diff / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    
    if (hours > 0) {
      return `${hours}h ${minutes % 60}m ${seconds % 60}s`;
    } else if (minutes > 0) {
      return `${minutes}m ${seconds % 60}s`;
    } else {
      return `${seconds}s`;
    }
  };

  useEffect(() => {
    loadJobs();
    
    // Refresh running jobs every 2 seconds
    const interval = setInterval(() => {
      setJobs(currentJobs => {
        const runningJobs = currentJobs.filter(job => job.status === 'running');
        if (runningJobs.length > 0) {
          runningJobs.forEach(job => refreshJob(job.id));
        }
        return currentJobs;
      });
    }, 2000);

    return () => clearInterval(interval);
  }, []); // Empty dependency array - only run once on mount

  const handleRefresh = async () => {
    setRefreshing(true);
    await loadJobs();
    setRefreshing(false);
  };

  if (jobs.length === 0) {
    return (
      <div className="bg-white rounded-lg shadow p-6">
        <div className="text-center text-gray-500">
          <ClockIcon className="h-12 w-12 mx-auto mb-4 text-gray-300" />
          <p>No background jobs</p>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg shadow">
      <div className="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
        <h3 className="text-lg font-medium text-gray-900">Background Jobs</h3>
        <button
          onClick={handleRefresh}
          disabled={refreshing}
          className="inline-flex items-center px-3 py-2 border border-gray-300 shadow-sm text-sm leading-4 font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50"
        >
          <ArrowPathIcon className={`h-4 w-4 mr-2 ${refreshing ? 'animate-spin' : ''}`} />
          Refresh
        </button>
      </div>
      
      <div className="divide-y divide-gray-200">
        {jobs.map((job) => (
          <div key={job.id} className="px-6 py-4">
            <div className="flex items-center justify-between mb-2">
              <div className="flex items-center space-x-3">
                {getStatusIcon(job.status)}
                <div>
                  <h4 className="text-sm font-medium text-gray-900">
                    {job.type === 'bulk_add_collections' ? 'Bulk Add Collections' : 
                     job.type === 'cache-generation' ? 'Cache Generation' : 
                     job.type}
                  </h4>
                  <p className="text-xs text-gray-500">
                    {job.data?.parentPath && `Path: ${job.data.parentPath}`}
                    {job.data?.collectionIds && `Collections: ${job.data.collectionIds.length}`}
                  </p>
                </div>
              </div>
              
              <div className="flex items-center space-x-2">
                <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getStatusColor(job.status)}`}>
                  {job.status}
                </span>
                
                {(job.status === 'running' || job.status === 'pending') && (
                  <button
                    onClick={() => cancelJob(job.id)}
                    className="inline-flex items-center px-2 py-1 border border-red-300 text-xs font-medium rounded text-red-700 bg-red-50 hover:bg-red-100 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500"
                  >
                    <StopIcon className="h-3 w-3 mr-1" />
                    Cancel
                  </button>
                )}
              </div>
            </div>
            
            {job.status === 'running' && job.total > 0 && (
              <div className="mb-2">
                <div className="flex justify-between text-sm text-gray-600 mb-1">
                  <span>{job.message || `Processing ${job.current}/${job.total}`}</span>
                  <span>{Math.round(job.progress)}%</span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2">
                  <div 
                    className="bg-blue-600 h-2 rounded-full transition-all duration-300"
                    style={{ width: `${job.progress}%` }}
                  ></div>
                </div>
              </div>
            )}
            
            <div className="flex justify-between text-xs text-gray-500">
              <span>Created: {formatDate(job.createdAt)}</span>
              <span>Duration: {formatDuration(job.startedAt, job.completedAt)}</span>
            </div>
            
            {job.error && (
              <div className="mt-2 p-2 bg-red-50 border border-red-200 rounded text-xs text-red-700">
                <div className="flex items-center">
                  <ExclamationTriangleIcon className="h-4 w-4 mr-1" />
                  Error: {job.error}
                </div>
              </div>
            )}
            
            {job.results && (
              <div className="mt-2 p-2 bg-green-50 border border-green-200 rounded text-xs">
                <div className="flex items-center text-green-700">
                  <CheckCircleIcon className="h-4 w-4 mr-1" />
                  {job.results.message}
                </div>
                {job.results.added > 0 && (
                  <div className="mt-1 text-green-600">
                    Added: {job.results.added} collections
                  </div>
                )}
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
