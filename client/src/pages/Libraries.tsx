import React, { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { libraryApi, Library } from '../services/libraryApi';
import { schedulerApi, ScheduledJob } from '../services/schedulerApi';
import { 
  FolderOpen, 
  Plus, 
  Trash2, 
  Settings, 
  Calendar, 
  PlayCircle, 
  PauseCircle,
  Clock,
  CheckCircle,
  XCircle,
  AlertCircle
} from 'lucide-react';
import { formatDistanceToNow } from 'date-fns';

export default function Libraries() {
  const queryClient = useQueryClient();
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [selectedLibrary, setSelectedLibrary] = useState<Library | null>(null);
  const [showSchedulerDetails, setShowSchedulerDetails] = useState<Record<string, boolean>>({});

  // Fetch libraries
  const { data: libraries = [], isLoading: librariesLoading } = useQuery({
    queryKey: ['libraries'],
    queryFn: libraryApi.getAll,
  });

  // Fetch all scheduled jobs
  const { data: scheduledJobs = [], isLoading: jobsLoading } = useQuery({
    queryKey: ['scheduledJobs'],
    queryFn: schedulerApi.getAllJobs,
    refetchInterval: 30000, // Refresh every 30 seconds
  });

  // Delete library mutation
  const deleteMutation = useMutation({
    mutationFn: libraryApi.delete,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['libraries'] });
      queryClient.invalidateQueries({ queryKey: ['scheduledJobs'] });
    },
  });

  // Toggle job enabled/disabled
  const toggleJobMutation = useMutation({
    mutationFn: async ({ jobId, enable }: { jobId: string; enable: boolean }) => {
      if (enable) {
        await schedulerApi.enableJob(jobId);
      } else {
        await schedulerApi.disableJob(jobId);
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['scheduledJobs'] });
    },
  });

  // Toggle AutoScan setting
  const toggleAutoScanMutation = useMutation({
    mutationFn: async ({ libraryId, autoScan }: { libraryId: string; autoScan: boolean }) => {
      await libraryApi.updateSettings(libraryId, { autoScan });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['libraries'] });
      queryClient.invalidateQueries({ queryKey: ['scheduledJobs'] });
    },
  });

  // Get job for a library
  const getJobForLibrary = (libraryId: string): ScheduledJob | undefined => {
    return scheduledJobs.find(job => 
      job.parameters?.LibraryId === libraryId
    );
  };

  // Format cron expression to human-readable text
  const formatCronExpression = (cron?: string): string => {
    if (!cron) return 'Not scheduled';
    
    // Simple cron parser for common patterns
    const parts = cron.split(' ');
    if (parts.length >= 5) {
      const [minute, hour, dayOfMonth, month, dayOfWeek] = parts;
      
      if (minute === '0' && hour === '2' && dayOfMonth === '*' && month === '*' && dayOfWeek === '*') {
        return 'Daily at 2:00 AM';
      }
      if (minute === '0' && hour === '*') {
        return 'Every hour';
      }
      if (minute === '*/30') {
        return 'Every 30 minutes';
      }
    }
    
    return cron; // Return raw cron if can't parse
  };

  // Get status badge color
  const getStatusColor = (status?: string): string => {
    switch (status?.toLowerCase()) {
      case 'completed':
      case 'succeeded':
        return 'bg-green-100 text-green-800';
      case 'failed':
        return 'bg-red-100 text-red-800';
      case 'running':
        return 'bg-blue-100 text-blue-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  // Get status icon
  const getStatusIcon = (status?: string) => {
    switch (status?.toLowerCase()) {
      case 'completed':
      case 'succeeded':
        return <CheckCircle className="w-4 h-4" />;
      case 'failed':
        return <XCircle className="w-4 h-4" />;
      case 'running':
        return <Clock className="w-4 h-4 animate-spin" />;
      default:
        return <AlertCircle className="w-4 h-4" />;
    }
  };

  const handleDeleteLibrary = async (libraryId: string) => {
    if (confirm('Are you sure you want to delete this library? This will also delete the associated scheduled job.')) {
      deleteMutation.mutate(libraryId);
    }
  };

  const handleToggleAutoScan = async (libraryId: string, currentValue: boolean) => {
    toggleAutoScanMutation.mutate({ libraryId, autoScan: !currentValue });
  };

  const handleToggleJob = async (jobId: string, currentEnabled: boolean) => {
    toggleJobMutation.mutate({ jobId, enable: !currentEnabled });
  };

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex justify-between items-center mb-6">
          <div>
            <h1 className="text-3xl font-bold text-gray-900">Libraries</h1>
            <p className="text-gray-600 mt-1">Manage your media libraries and scheduled scans</p>
          </div>
          <button
            onClick={() => setShowCreateModal(true)}
            className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
          >
            <Plus className="w-5 h-5" />
            Add Library
          </button>
        </div>

        {/* Loading State */}
        {(librariesLoading || jobsLoading) && (
          <div className="text-center py-12">
            <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
            <p className="mt-4 text-gray-600">Loading libraries...</p>
          </div>
        )}

        {/* Libraries List */}
        {!librariesLoading && !jobsLoading && (
          <div className="space-y-4">
            {libraries.length === 0 ? (
              <div className="text-center py-12 bg-white rounded-lg shadow">
                <FolderOpen className="w-16 h-16 mx-auto text-gray-400 mb-4" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">No libraries yet</h3>
                <p className="text-gray-600 mb-4">Create your first library to get started</p>
                <button
                  onClick={() => setShowCreateModal(true)}
                  className="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
                >
                  <Plus className="w-5 h-5" />
                  Add Library
                </button>
              </div>
            ) : (
              libraries.map((library) => {
                const job = getJobForLibrary(library.id);
                const showDetails = showSchedulerDetails[library.id] || false;

                return (
                  <div key={library.id} className="bg-white rounded-lg shadow hover:shadow-md transition-shadow">
                    {/* Library Header */}
                    <div className="p-6">
                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <div className="flex items-center gap-3">
                            <FolderOpen className="w-6 h-6 text-blue-600" />
                            <div>
                              <h3 className="text-xl font-semibold text-gray-900">{library.name}</h3>
                              <p className="text-sm text-gray-600 mt-1">{library.path}</p>
                              {library.description && (
                                <p className="text-sm text-gray-500 mt-1">{library.description}</p>
                              )}
                            </div>
                          </div>

                          {/* Statistics */}
                          <div className="grid grid-cols-4 gap-4 mt-4">
                            <div className="bg-gray-50 px-3 py-2 rounded">
                              <div className="text-xs text-gray-600">Collections</div>
                              <div className="text-lg font-semibold text-gray-900">
                                {library.statistics?.totalCollections || 0}
                              </div>
                            </div>
                            <div className="bg-gray-50 px-3 py-2 rounded">
                              <div className="text-xs text-gray-600">Media Items</div>
                              <div className="text-lg font-semibold text-gray-900">
                                {library.statistics?.totalMediaItems || 0}
                              </div>
                            </div>
                            <div className="bg-gray-50 px-3 py-2 rounded">
                              <div className="text-xs text-gray-600">Total Size</div>
                              <div className="text-lg font-semibold text-gray-900">
                                {((library.statistics?.totalSize || 0) / 1024 / 1024 / 1024).toFixed(2)} GB
                              </div>
                            </div>
                            <div className="bg-gray-50 px-3 py-2 rounded">
                              <div className="text-xs text-gray-600">Auto Scan</div>
                              <div className="text-lg font-semibold">
                                <button
                                  onClick={() => handleToggleAutoScan(library.id, library.settings.autoScan)}
                                  className={`px-2 py-1 rounded text-xs font-medium ${
                                    library.settings.autoScan
                                      ? 'bg-green-100 text-green-800'
                                      : 'bg-gray-200 text-gray-600'
                                  }`}
                                >
                                  {library.settings.autoScan ? 'Enabled' : 'Disabled'}
                                </button>
                              </div>
                            </div>
                          </div>
                        </div>

                        {/* Actions */}
                        <div className="flex items-center gap-2">
                          <button
                            onClick={() => handleDeleteLibrary(library.id)}
                            className="p-2 text-red-600 hover:bg-red-50 rounded transition-colors"
                            title="Delete library"
                          >
                            <Trash2 className="w-5 h-5" />
                          </button>
                        </div>
                      </div>

                      {/* Scheduler Job Information */}
                      {job && (
                        <div className="mt-4 border-t pt-4">
                          <div className="flex items-center justify-between">
                            <div className="flex items-center gap-3">
                              <Calendar className="w-5 h-5 text-purple-600" />
                              <div>
                                <div className="flex items-center gap-2">
                                  <span className="font-medium text-gray-900">Scheduled Scan</span>
                                  <span className={`px-2 py-0.5 rounded text-xs font-medium ${
                                    job.isEnabled ? 'bg-green-100 text-green-800' : 'bg-gray-200 text-gray-600'
                                  }`}>
                                    {job.isEnabled ? 'Active' : 'Inactive'}
                                  </span>
                                </div>
                                <div className="text-sm text-gray-600 mt-1">
                                  {formatCronExpression(job.cronExpression)}
                                </div>
                              </div>
                            </div>

                            <button
                              onClick={() => setShowSchedulerDetails({
                                ...showSchedulerDetails,
                                [library.id]: !showDetails
                              })}
                              className="px-3 py-1 text-sm text-blue-600 hover:bg-blue-50 rounded transition-colors"
                            >
                              {showDetails ? 'Hide Details' : 'Show Details'}
                            </button>
                          </div>

                          {/* Expanded Scheduler Details */}
                          {showDetails && (
                            <div className="mt-4 grid grid-cols-2 gap-4 bg-gray-50 p-4 rounded-lg">
                              {/* Execution Statistics */}
                              <div>
                                <h4 className="font-medium text-gray-900 mb-2">Execution Statistics</h4>
                                <div className="space-y-2 text-sm">
                                  <div className="flex justify-between">
                                    <span className="text-gray-600">Total Runs:</span>
                                    <span className="font-medium">{job.runCount}</span>
                                  </div>
                                  <div className="flex justify-between">
                                    <span className="text-gray-600">Successful:</span>
                                    <span className="font-medium text-green-600">{job.successCount}</span>
                                  </div>
                                  <div className="flex justify-between">
                                    <span className="text-gray-600">Failed:</span>
                                    <span className="font-medium text-red-600">{job.failureCount}</span>
                                  </div>
                                  <div className="flex justify-between">
                                    <span className="text-gray-600">Success Rate:</span>
                                    <span className="font-medium">
                                      {job.runCount > 0 
                                        ? ((job.successCount / job.runCount) * 100).toFixed(1) + '%'
                                        : 'N/A'}
                                    </span>
                                  </div>
                                </div>
                              </div>

                              {/* Last Run Information */}
                              <div>
                                <h4 className="font-medium text-gray-900 mb-2">Last Run</h4>
                                <div className="space-y-2 text-sm">
                                  {job.lastRunAt ? (
                                    <>
                                      <div className="flex justify-between">
                                        <span className="text-gray-600">Time:</span>
                                        <span className="font-medium">
                                          {formatDistanceToNow(new Date(job.lastRunAt), { addSuffix: true })}
                                        </span>
                                      </div>
                                      <div className="flex justify-between items-center">
                                        <span className="text-gray-600">Status:</span>
                                        <span className={`flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium ${
                                          getStatusColor(job.lastRunStatus)
                                        }`}>
                                          {getStatusIcon(job.lastRunStatus)}
                                          {job.lastRunStatus || 'Unknown'}
                                        </span>
                                      </div>
                                      {job.lastRunDuration && (
                                        <div className="flex justify-between">
                                          <span className="text-gray-600">Duration:</span>
                                          <span className="font-medium">
                                            {(job.lastRunDuration / 1000).toFixed(2)}s
                                          </span>
                                        </div>
                                      )}
                                      {job.lastErrorMessage && (
                                        <div className="col-span-2">
                                          <span className="text-gray-600">Error:</span>
                                          <div className="text-red-600 text-xs mt-1 bg-red-50 p-2 rounded">
                                            {job.lastErrorMessage}
                                          </div>
                                        </div>
                                      )}
                                    </>
                                  ) : (
                                    <p className="text-gray-500">Never executed</p>
                                  )}
                                </div>
                              </div>

                              {/* Next Run */}
                              {job.nextRunAt && (
                                <div className="col-span-2">
                                  <div className="flex items-center gap-2 text-sm">
                                    <Clock className="w-4 h-4 text-blue-600" />
                                    <span className="text-gray-600">Next scheduled run:</span>
                                    <span className="font-medium text-blue-600">
                                      {formatDistanceToNow(new Date(job.nextRunAt), { addSuffix: true })}
                                    </span>
                                  </div>
                                </div>
                              )}

                              {/* Actions */}
                              <div className="col-span-2 flex gap-2 pt-2 border-t">
                                <button
                                  onClick={() => handleToggleJob(job.id, job.isEnabled)}
                                  disabled={toggleJobMutation.isPending}
                                  className={`flex items-center gap-2 px-3 py-1.5 rounded text-sm font-medium transition-colors ${
                                    job.isEnabled
                                      ? 'bg-orange-100 text-orange-800 hover:bg-orange-200'
                                      : 'bg-green-100 text-green-800 hover:bg-green-200'
                                  }`}
                                >
                                  {job.isEnabled ? (
                                    <>
                                      <PauseCircle className="w-4 h-4" />
                                      Pause Job
                                    </>
                                  ) : (
                                    <>
                                      <PlayCircle className="w-4 h-4" />
                                      Resume Job
                                    </>
                                  )}
                                </button>
                              </div>
                            </div>
                          )}
                        </div>
                      )}
                    </div>
                  </div>
                );
              })
            )}
          </div>
        )}
      </div>

      {/* Create Library Modal (TODO: Implement) */}
      {showCreateModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 max-w-md w-full">
            <h2 className="text-xl font-bold mb-4">Create Library</h2>
            <p className="text-gray-600">Library creation UI coming soon...</p>
            <button
              onClick={() => setShowCreateModal(false)}
              className="mt-4 px-4 py-2 bg-gray-200 text-gray-800 rounded hover:bg-gray-300"
            >
              Close
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

