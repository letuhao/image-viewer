import React, { useState, useMemo } from 'react';
import { HardDrive, FolderOpen, Activity, RefreshCw, Trash2, Play, Clock, CheckCircle2, XCircle, AlertCircle, Image as ImageIcon, Layers } from 'lucide-react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '../services/api';
import LoadingSpinner from '../components/ui/LoadingSpinner';
import Button from '../components/ui/Button';
import { formatDistanceToNow } from 'date-fns';

interface CacheFolderStatistics {
  id: string;
  name: string;
  path: string;
  maxSizeBytes: number;
  currentSizeBytes: number;
  priority: number;
  isActive: boolean;
  totalCollections: number;
  totalFiles: number;
  cachedCollectionIds: string[];
  lastCacheGeneratedAt?: string;
  lastCleanupAt?: string;
  availableSpaceBytes: number;
  usagePercentage: number;
  isFull: boolean;
  isNearFull: boolean;
  createdAt: string;
  updatedAt: string;
}

interface FileProcessingJobState {
  id: string;
  jobId: string;
  jobType: string; // "cache", "thumbnail", "both"
  collectionId: string;
  collectionName?: string;
  status: string;
  totalImages: number;
  completedImages: number;
  failedImages: number;
  skippedImages: number;
  remainingImages: number;
  progress: number;
  outputFolderId?: string;
  outputFolderPath?: string;
  totalSizeBytes: number;
  jobSettings: string; // JSON string
  startedAt?: string;
  completedAt?: string;
  lastProgressAt?: string;
  createdAt: string;
  updatedAt: string;
  errorMessage?: string;
  canResume: boolean;
}

const CacheManagement: React.FC = () => {
  const [selectedTab, setSelectedTab] = useState<'folders' | 'jobs'>('folders');
  const [jobTypeFilter, setJobTypeFilter] = useState<string>(''); // '', 'cache', 'thumbnail'
  const queryClient = useQueryClient();

  // Parse job settings once and memoize for performance
  const parseJobSettings = useMemo(() => {
    return (jobSettings: string) => {
      try {
        return JSON.parse(jobSettings || '{}');
      } catch {
        return {};
      }
    };
  }, []);

  // Query cache folder statistics
  const { data: cacheFolders, isLoading: foldersLoading } = useQuery<CacheFolderStatistics[]>({
    queryKey: ['cacheFolderStatistics'],
    queryFn: async () => {
      const response = await api.get('/cache/folders/statistics');
      return response.data;
    },
    refetchInterval: 30000, // Refresh every 30 seconds
  });

  // Query file processing job states (unified: cache + thumbnail)
  const { data: processingJobs, isLoading: jobsLoading } = useQuery<FileProcessingJobState[]>({
    queryKey: ['fileProcessingJobs', jobTypeFilter],
    queryFn: async () => {
      const params = jobTypeFilter ? { jobType: jobTypeFilter } : {};
      const response = await api.get('/cache/processing-jobs', { params });
      return response.data;
    },
    refetchInterval: 5000, // Refresh every 5 seconds
  });

  // Query resumable jobs
  const { data: resumableJobs } = useQuery<string[]>({
    queryKey: ['resumableJobs', jobTypeFilter],
    queryFn: async () => {
      const params = jobTypeFilter ? { jobType: jobTypeFilter } : {};
      const response = await api.get('/cache/processing-jobs/resumable', { params });
      return response.data;
    },
    refetchInterval: 10000,
  });

  // Mutation to resume a job
  const resumeJobMutation = useMutation({
    mutationFn: async (jobId: string) => {
      const response = await api.post(`/cache/processing-jobs/${jobId}/resume`);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['fileProcessingJobs'] });
      queryClient.invalidateQueries({ queryKey: ['resumableJobs'] });
    },
  });

  // Mutation to recover all incomplete jobs
  const recoverJobsMutation = useMutation({
    mutationFn: async () => {
      const params = jobTypeFilter ? { jobType: jobTypeFilter } : {};
      const response = await api.post('/cache/processing-jobs/recover', null, { params });
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['fileProcessingJobs'] });
      queryClient.invalidateQueries({ queryKey: ['resumableJobs'] });
    },
  });

  // Mutation to cleanup old jobs
  const cleanupJobsMutation = useMutation({
    mutationFn: async (olderThanDays: number) => {
      const response = await api.delete(`/cache/processing-jobs/cleanup?olderThanDays=${olderThanDays}`);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['fileProcessingJobs'] });
    },
  });

  const formatBytes = (bytes: number): string => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${(bytes / Math.pow(k, i)).toFixed(2)} ${sizes[i]}`;
  };

  const getStatusIcon = (status: string) => {
    switch (status.toLowerCase()) {
      case 'pending':
        return <Clock className="h-4 w-4 text-slate-400" />;
      case 'running':
        return <Activity className="h-4 w-4 text-blue-500 animate-spin" />;
      case 'completed':
        return <CheckCircle2 className="h-4 w-4 text-green-500" />;
      case 'failed':
        return <XCircle className="h-4 w-4 text-red-500" />;
      case 'paused':
        return <AlertCircle className="h-4 w-4 text-orange-500" />;
      default:
        return <Activity className="h-4 w-4 text-slate-400" />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'pending':
        return 'bg-slate-500/20 text-slate-400 border-slate-500/30';
      case 'running':
        return 'bg-blue-500/20 text-blue-400 border-blue-500/30';
      case 'completed':
        return 'bg-green-500/20 text-green-400 border-green-500/30';
      case 'failed':
        return 'bg-red-500/20 text-red-400 border-red-500/30';
      case 'paused':
        return 'bg-orange-500/20 text-orange-400 border-orange-500/30';
      default:
        return 'bg-slate-500/20 text-slate-400 border-slate-500/30';
    }
  };

  const getJobTypeIcon = (jobType: string) => {
    switch (jobType.toLowerCase()) {
      case 'cache':
        return <HardDrive className="h-4 w-4 text-blue-400" />;
      case 'thumbnail':
        return <ImageIcon className="h-4 w-4 text-purple-400" />;
      case 'both':
        return <Layers className="h-4 w-4 text-cyan-400" />;
      default:
        return <Activity className="h-4 w-4 text-slate-400" />;
    }
  };

  const getJobTypeColor = (jobType: string) => {
    switch (jobType.toLowerCase()) {
      case 'cache':
        return 'bg-blue-500/20 text-blue-400 border-blue-500/30';
      case 'thumbnail':
        return 'bg-purple-500/20 text-purple-400 border-purple-500/30';
      case 'both':
        return 'bg-cyan-500/20 text-cyan-400 border-cyan-500/30';
      default:
        return 'bg-slate-500/20 text-slate-400 border-slate-500/30';
    }
  };

  const getJobTypeLabel = (jobType: string) => {
    switch (jobType.toLowerCase()) {
      case 'cache':
        return 'Cache';
      case 'thumbnail':
        return 'Thumbnail';
      case 'both':
        return 'Cache + Thumbnail';
      default:
        return jobType;
    }
  };

  if (foldersLoading && jobsLoading) {
    return <LoadingSpinner text="Loading cache management..." />;
  }

  return (
    <div className="h-full flex flex-col">
      {/* Header */}
      <div className="flex-shrink-0 border-b border-slate-800 bg-slate-900/50 backdrop-blur">
        <div className="px-6 py-4">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold text-white flex items-center space-x-3">
                <HardDrive className="h-7 w-7 text-blue-500" />
                <span>Cache Management</span>
              </h1>
              <p className="text-sm text-slate-400 mt-1">Monitor cache folders and job progress</p>
            </div>
            <div className="flex gap-2">
              <Button
                variant="ghost"
                onClick={() => cleanupJobsMutation.mutate(30)}
                disabled={cleanupJobsMutation.isPending}
              >
                <Trash2 className="h-4 w-4 mr-2" />
                {cleanupJobsMutation.isPending ? 'Cleaning...' : 'Cleanup Old Jobs'}
              </Button>
              <Button
                variant="primary"
                onClick={() => recoverJobsMutation.mutate()}
                disabled={recoverJobsMutation.isPending}
              >
                <RefreshCw className={`h-4 w-4 mr-2 ${recoverJobsMutation.isPending ? 'animate-spin' : ''}`} />
                {recoverJobsMutation.isPending ? 'Recovering...' : 'Recover Jobs'}
              </Button>
            </div>
          </div>

          {/* Tabs */}
          <div className="flex gap-2 mt-4">
            <button
              onClick={() => setSelectedTab('folders')}
              className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                selectedTab === 'folders'
                  ? 'bg-blue-500/20 text-blue-400 border border-blue-500/30'
                  : 'bg-slate-800 text-slate-400 hover:text-white'
              }`}
            >
              <FolderOpen className="h-4 w-4 inline mr-2" />
              Cache Folders ({cacheFolders?.length || 0})
            </button>
            <button
              onClick={() => setSelectedTab('jobs')}
              className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                selectedTab === 'jobs'
                  ? 'bg-blue-500/20 text-blue-400 border border-blue-500/30'
                  : 'bg-slate-800 text-slate-400 hover:text-white'
              }`}
            >
              <Activity className="h-4 w-4 inline mr-2" />
              Processing Jobs ({processingJobs?.length || 0})
              {resumableJobs && resumableJobs.length > 0 && (
                <span className="ml-2 px-1.5 py-0.5 text-xs bg-orange-500/20 text-orange-400 rounded">
                  {resumableJobs.length} resumable
                </span>
              )}
            </button>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-y-auto px-6 py-4">
        {selectedTab === 'folders' && (
          <div className="space-y-4">
            {cacheFolders?.map((folder) => (
              <div
                key={folder.id}
                className="bg-slate-800 border border-slate-700 rounded-lg p-6 hover:border-slate-600 transition-colors"
              >
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-3">
                      <HardDrive className="h-6 w-6 text-blue-400" />
                      <h3 className="text-lg font-semibold text-white">{folder.name}</h3>
                      <span
                        className={`px-2 py-0.5 text-xs rounded border ${
                          folder.isActive
                            ? 'bg-green-500/20 text-green-400 border-green-500/30'
                            : 'bg-slate-500/20 text-slate-400 border-slate-500/30'
                        }`}
                      >
                        {folder.isActive ? 'Active' : 'Inactive'}
                      </span>
                      {folder.isNearFull && (
                        <span className="px-2 py-0.5 text-xs bg-orange-500/20 text-orange-400 rounded border border-orange-500/30">
                          Near Full
                        </span>
                      )}
                      {folder.isFull && (
                        <span className="px-2 py-0.5 text-xs bg-red-500/20 text-red-400 rounded border border-red-500/30">
                          Full
                        </span>
                      )}
                    </div>

                    <p className="text-sm text-slate-400 mb-4 font-mono">{folder.path}</p>

                    {/* Statistics Grid */}
                    <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
                      <div className="bg-slate-700/50 rounded px-3 py-2">
                        <div className="text-xs text-slate-400 mb-1">Total Collections</div>
                        <div className="text-lg font-semibold text-white">{folder.totalCollections}</div>
                      </div>
                      <div className="bg-slate-700/50 rounded px-3 py-2">
                        <div className="text-xs text-slate-400 mb-1">Total Files</div>
                        <div className="text-lg font-semibold text-white">{folder.totalFiles.toLocaleString()}</div>
                      </div>
                      <div className="bg-slate-700/50 rounded px-3 py-2">
                        <div className="text-xs text-slate-400 mb-1">Used Space</div>
                        <div className="text-lg font-semibold text-white">{formatBytes(folder.currentSizeBytes)}</div>
                      </div>
                      <div className="bg-slate-700/50 rounded px-3 py-2">
                        <div className="text-xs text-slate-400 mb-1">Available</div>
                        <div className="text-lg font-semibold text-white">{formatBytes(folder.availableSpaceBytes)}</div>
                      </div>
                    </div>

                    {/* Progress Bar */}
                    <div className="mb-3">
                      <div className="flex items-center justify-between text-xs text-slate-400 mb-1">
                        <span>Storage Usage</span>
                        <span>{folder.usagePercentage.toFixed(1)}%</span>
                      </div>
                      <div className="w-full bg-slate-700 rounded-full h-2">
                        <div
                          className={`h-2 rounded-full transition-all duration-300 ${
                            folder.isFull
                              ? 'bg-red-500'
                              : folder.isNearFull
                              ? 'bg-orange-500'
                              : 'bg-blue-500'
                          }`}
                          style={{ width: `${Math.min(folder.usagePercentage, 100)}%` }}
                        />
                      </div>
                    </div>

                    {/* Timestamps */}
                    <div className="flex items-center gap-4 text-xs text-slate-500">
                      {folder.lastCacheGeneratedAt && (
                        <span>
                          Last Generated: {formatDistanceToNow(new Date(folder.lastCacheGeneratedAt), { addSuffix: true })}
                        </span>
                      )}
                      {folder.lastCleanupAt && (
                        <span>
                          Last Cleanup: {formatDistanceToNow(new Date(folder.lastCleanupAt), { addSuffix: true })}
                        </span>
                      )}
                      <span>Priority: {folder.priority}</span>
                    </div>
                  </div>
                </div>
              </div>
            ))}

            {!cacheFolders || cacheFolders.length === 0 && (
              <div className="text-center py-12">
                <FolderOpen className="h-12 w-12 text-slate-600 mx-auto mb-4" />
                <p className="text-slate-400">No cache folders configured</p>
              </div>
            )}
          </div>
        )}

        {selectedTab === 'jobs' && (
          <div className="space-y-4">
            {/* Job Type Filter */}
            <div className="flex items-center gap-4 mb-4">
              <label className="text-sm text-slate-400">Filter by Job Type:</label>
              <select
                value={jobTypeFilter}
                onChange={(e) => setJobTypeFilter(e.target.value)}
                className="px-3 py-2 bg-slate-800 border border-slate-700 rounded-lg text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="">All Types</option>
                <option value="cache">Cache Only</option>
                <option value="thumbnail">Thumbnail Only</option>
                <option value="both">Both</option>
              </select>
            </div>

            {processingJobs?.map((job) => (
              <div
                key={job.id}
                className="bg-slate-800 border border-slate-700 rounded-lg p-6 hover:border-slate-600 transition-colors"
              >
                <div className="flex items-start justify-between mb-4">
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-2">
                      {getStatusIcon(job.status)}
                      <h3 className="text-lg font-semibold text-white">
                        {job.collectionName || `Collection ${job.collectionId.substring(0, 8)}`}
                      </h3>
                      <span className={`px-2 py-0.5 text-xs rounded border flex items-center gap-1 ${getJobTypeColor(job.jobType)}`}>
                        {getJobTypeIcon(job.jobType)}
                        {getJobTypeLabel(job.jobType)}
                      </span>
                      <span className={`px-2 py-0.5 text-xs rounded border ${getStatusColor(job.status)}`}>
                        {job.status}
                      </span>
                      {job.canResume && job.status !== 'Completed' && (
                        <span className="px-2 py-0.5 text-xs bg-orange-500/20 text-orange-400 rounded border border-orange-500/30">
                          Resumable
                        </span>
                      )}
                    </div>

                    <div className="text-xs text-slate-500 mb-3">
                      Job ID: <span className="font-mono">{job.jobId}</span>
                    </div>

                    {/* Progress Statistics */}
                    <div className="grid grid-cols-2 md:grid-cols-5 gap-3 mb-4">
                      <div className="bg-slate-700/50 rounded px-3 py-2">
                        <div className="text-xs text-slate-400 mb-1">Total</div>
                        <div className="text-base font-semibold text-white">{job.totalImages}</div>
                      </div>
                      <div className="bg-green-500/10 border border-green-500/30 rounded px-3 py-2">
                        <div className="text-xs text-green-400 mb-1">Completed</div>
                        <div className="text-base font-semibold text-green-400">{job.completedImages}</div>
                      </div>
                      <div className="bg-blue-500/10 border border-blue-500/30 rounded px-3 py-2">
                        <div className="text-xs text-blue-400 mb-1">Skipped</div>
                        <div className="text-base font-semibold text-blue-400">{job.skippedImages}</div>
                      </div>
                      <div className="bg-red-500/10 border border-red-500/30 rounded px-3 py-2">
                        <div className="text-xs text-red-400 mb-1">Failed</div>
                        <div className="text-base font-semibold text-red-400">{job.failedImages}</div>
                      </div>
                      <div className="bg-slate-700/50 rounded px-3 py-2">
                        <div className="text-xs text-slate-400 mb-1">Remaining</div>
                        <div className="text-base font-semibold text-white">{job.remainingImages}</div>
                      </div>
                    </div>

                    {/* Progress Bar */}
                    <div className="mb-3">
                      <div className="flex items-center justify-between text-xs text-slate-400 mb-1">
                        <span>Progress</span>
                        <span>{job.progress}%</span>
                      </div>
                      <div className="w-full bg-slate-700 rounded-full h-2">
                        <div
                          className="bg-blue-500 h-2 rounded-full transition-all duration-300"
                          style={{ width: `${job.progress}%` }}
                        />
                      </div>
                    </div>

                    {/* Additional Info */}
                    <div className="flex items-center gap-4 text-xs text-slate-500 mb-3">
                      <span>Size: {formatBytes(job.totalSizeBytes)}</span>
                      {(() => {
                        const settings = parseJobSettings(job.jobSettings);
                        return (
                          <>
                            {settings.width && settings.height && (
                              <span>Dimensions: {settings.width}x{settings.height}</span>
                            )}
                            {settings.quality && (
                              <span>Quality: {settings.quality}%</span>
                            )}
                            {settings.format && (
                              <span>Format: {settings.format}</span>
                            )}
                          </>
                        );
                      })()}
                    </div>

                    {/* Timestamps */}
                    <div className="flex items-center gap-4 text-xs text-slate-500">
                      {job.startedAt && (
                        <span>
                          Started: {formatDistanceToNow(new Date(job.startedAt), { addSuffix: true })}
                        </span>
                      )}
                      {job.lastProgressAt && (
                        <span>
                          Last Progress: {formatDistanceToNow(new Date(job.lastProgressAt), { addSuffix: true })}
                        </span>
                      )}
                      {job.completedAt && (
                        <span>
                          Completed: {formatDistanceToNow(new Date(job.completedAt), { addSuffix: true })}
                        </span>
                      )}
                    </div>

                    {/* Error Message */}
                    {job.errorMessage && (
                      <div className="mt-3 p-2 bg-red-500/10 border border-red-500/30 rounded text-xs text-red-400">
                        Error: {job.errorMessage}
                      </div>
                    )}
                  </div>

                  {/* Actions */}
                  {job.canResume && job.status !== 'Completed' && (
                    <div>
                      <Button
                        variant="primary"
                        size="sm"
                        onClick={() => resumeJobMutation.mutate(job.jobId)}
                        disabled={resumeJobMutation.isPending}
                      >
                        <Play className="h-3 w-3 mr-1" />
                        Resume
                      </Button>
                    </div>
                  )}
                </div>
              </div>
            ))}

            {!processingJobs || processingJobs.length === 0 ? (
              <div className="text-center py-12">
                <Activity className="h-12 w-12 text-slate-600 mx-auto mb-4" />
                <p className="text-slate-400">
                  {jobTypeFilter 
                    ? `No ${jobTypeFilter} jobs found` 
                    : 'No processing jobs found'}
                </p>
              </div>
            ) : null}
          </div>
        )}
      </div>
    </div>
  );
};

export default CacheManagement;

