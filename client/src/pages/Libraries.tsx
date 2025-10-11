import React, { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { libraryApi, Library } from '../services/libraryApi';
import { schedulerApi, ScheduledJob } from '../services/schedulerApi';
import { useAuth } from '../contexts/AuthContext';
import toast from 'react-hot-toast';
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
  AlertCircle,
  X,
  RefreshCw
} from 'lucide-react';
import { formatDistanceToNow } from 'date-fns';

export default function Libraries() {
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [selectedLibrary, setSelectedLibrary] = useState<Library | null>(null);
  const [showSchedulerDetails, setShowSchedulerDetails] = useState<Record<string, boolean>>({});
  const [editingCron, setEditingCron] = useState<Record<string, boolean>>({});
  const [cronInput, setCronInput] = useState<Record<string, string>>({});
  
  // Form state for creating library
  const [formData, setFormData] = useState({
    name: '',
    path: '',
    description: '',
    autoScan: true
  });
  
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});

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

  // Create library mutation
  const createMutation = useMutation({
    mutationFn: libraryApi.create,
    onSuccess: () => {
      toast.success('Library created successfully!');
      queryClient.invalidateQueries({ queryKey: ['libraries'] });
      queryClient.invalidateQueries({ queryKey: ['scheduledJobs'] });
      setShowCreateModal(false);
      // Reset form
      setFormData({ name: '', path: '', description: '', autoScan: true });
      setFormErrors({});
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to create library');
    },
  });

  // Delete library mutation
  const deleteMutation = useMutation({
    mutationFn: libraryApi.delete,
    onSuccess: () => {
      toast.success('Library deleted successfully');
      queryClient.invalidateQueries({ queryKey: ['libraries'] });
      queryClient.invalidateQueries({ queryKey: ['scheduledJobs'] });
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to delete library');
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
    onSuccess: (_, variables) => {
      toast.success(variables.enable ? 'Job enabled' : 'Job disabled');
      queryClient.invalidateQueries({ queryKey: ['scheduledJobs'] });
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to update job status');
    },
  });

  // Trigger manual scan
  const triggerScanMutation = useMutation({
    mutationFn: libraryApi.triggerScan,
    onSuccess: (data) => {
      toast.success(`Scan triggered for ${data.libraryName}`);
      queryClient.invalidateQueries({ queryKey: ['libraries'] });
      queryClient.invalidateQueries({ queryKey: ['scheduledJobs'] });
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to trigger scan');
    },
  });

  // Delete orphaned job
  const deleteOrphanedJobMutation = useMutation({
    mutationFn: (jobId: string) => libraryApi.removeOrphanedJob(jobId),
    onSuccess: () => {
      toast.success('Orphaned job deleted successfully');
      queryClient.invalidateQueries({ queryKey: ['scheduledJobs'] });
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to delete orphaned job');
    },
  });

  // Recreate Hangfire job binding
  const recreateJobMutation = useMutation({
    mutationFn: (libraryId: string) => libraryApi.recreateJob(libraryId),
    onSuccess: () => {
      toast.success('Job recreation triggered. Please wait 5-10 seconds for binding...');
      // Refresh after delay to see the updated HangfireJobId
      setTimeout(() => {
        queryClient.invalidateQueries({ queryKey: ['scheduledJobs'] });
      }, 5000);
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to recreate job');
    },
  });

  // Update cron expression
  const updateCronMutation = useMutation({
    mutationFn: ({ jobId, cronExpression }: { jobId: string; cronExpression: string }) =>
      schedulerApi.updateCron(jobId, cronExpression),
    onSuccess: (updatedJob) => {
      toast.success('Schedule updated successfully');
      queryClient.invalidateQueries({ queryKey: ['scheduledJobs'] });
      setEditingCron({});
      setCronInput({});
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to update schedule');
    },
  });

  // Toggle AutoScan setting
  const toggleAutoScanMutation = useMutation({
    mutationFn: async ({ libraryId, autoScan }: { libraryId: string; autoScan: boolean }) => {
      await libraryApi.updateSettings(libraryId, { autoScan });
    },
    onSuccess: (_, variables) => {
      toast.success(variables.autoScan ? 'Auto-scan enabled' : 'Auto-scan disabled');
      queryClient.invalidateQueries({ queryKey: ['libraries'] });
      queryClient.invalidateQueries({ queryKey: ['scheduledJobs'] });
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to update setting');
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
      if (minute === '0' && hour === '*' && dayOfMonth === '*' && month === '*' && dayOfWeek === '*') {
        return 'Every hour';
      }
      if (minute === '*/30' && hour === '*' && dayOfMonth === '*' && month === '*' && dayOfWeek === '*') {
        return 'Every 30 minutes';
      }
      if (minute === '0' && hour === '*/6' && dayOfMonth === '*' && month === '*' && dayOfWeek === '*') {
        return 'Every 6 hours';
      }
      if (minute === '0' && hour === '0' && dayOfMonth === '*' && month === '*' && dayOfWeek === '0') {
        return 'Weekly on Sunday at midnight';
      }
      if (minute === '0' && hour === '0' && dayOfMonth === '1' && month === '*' && dayOfWeek === '*') {
        return 'Monthly on the 1st at midnight';
      }
    }
    
    return cron; // Return raw cron if can't parse
  };

  // Common cron expressions
  const commonCronPatterns = [
    { label: 'Every 30 minutes', cron: '*/30 * * * *' },
    { label: 'Every hour', cron: '0 * * * *' },
    { label: 'Every 6 hours', cron: '0 */6 * * *' },
    { label: 'Daily at 2 AM', cron: '0 2 * * *' },
    { label: 'Daily at midnight', cron: '0 0 * * *' },
    { label: 'Weekly (Sunday midnight)', cron: '0 0 * * 0' },
    { label: 'Monthly (1st midnight)', cron: '0 0 1 * *' },
  ];

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

  // Form validation
  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};
    
    if (!formData.name.trim()) {
      errors.name = 'Library name is required';
    } else if (formData.name.length > 100) {
      errors.name = 'Name must be 100 characters or less';
    }
    
    if (!formData.path.trim()) {
      errors.path = 'Library path is required';
    }
    
    if (formData.description && formData.description.length > 500) {
      errors.description = 'Description must be 500 characters or less';
    }
    
    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  // Form submission
  const handleCreateLibrary = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) {
      toast.error('Please fix form errors');
      return;
    }
    
    if (!user?.id) {
      toast.error('User not authenticated');
      return;
    }

    createMutation.mutate({
      name: formData.name,
      path: formData.path,
      ownerId: user.id,
      description: formData.description || '',
      autoScan: formData.autoScan
    });
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
    <div className="h-full flex flex-col">
      {/* Header Toolbar - matches Collections page style */}
      <div className="flex-shrink-0 border-b border-slate-800 bg-slate-900/50 backdrop-blur">
        <div className="px-6 py-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <h1 className="text-xl font-bold text-white">Libraries</h1>
              <span className="text-sm text-slate-400 bg-slate-800 px-2 py-1 rounded-full">
                {libraries.length}
              </span>
            </div>
            <button
              onClick={() => setShowCreateModal(true)}
              className="flex items-center gap-2 px-4 py-2 bg-primary-500 text-white rounded-lg hover:bg-primary-600 transition-colors"
            >
              <Plus className="w-5 h-5" />
              <span>Add Library</span>
            </button>
          </div>
        </div>
      </div>

      {/* Content Area */}
      <div className="flex-1 overflow-y-auto">
        <div className="px-6 py-6 space-y-4">
          {/* Loading State */}
          {(librariesLoading || jobsLoading) && (
            <div className="text-center py-12">
              <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-primary-500"></div>
              <p className="mt-4 text-slate-400">Loading libraries...</p>
            </div>
          )}

          {/* Libraries List */}
          {!librariesLoading && !jobsLoading && (
            <>
              {libraries.length === 0 ? (
                <div className="text-center py-12 bg-slate-900 rounded-lg border border-slate-800">
                  <FolderOpen className="w-16 h-16 mx-auto text-slate-600 mb-4" />
                  <h3 className="text-lg font-medium text-white mb-2">No libraries yet</h3>
                  <p className="text-slate-400 mb-4">Create your first library to get started</p>
                  <button
                    onClick={() => setShowCreateModal(true)}
                    className="inline-flex items-center gap-2 px-4 py-2 bg-primary-500 text-white rounded-lg hover:bg-primary-600"
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
                    <div key={library.id} className="bg-slate-900 rounded-lg border border-slate-800 hover:border-slate-700 transition-colors">
                      {/* Library Header */}
                      <div className="p-6">
                        <div className="flex items-start justify-between">
                          <div className="flex-1">
                            <div className="flex items-center gap-3">
                              <FolderOpen className="w-6 h-6 text-primary-500" />
                              <div>
                                <h3 className="text-xl font-semibold text-white">{library.name}</h3>
                                <p className="text-sm text-slate-400 mt-1">{library.path}</p>
                                {library.description && (
                                  <p className="text-sm text-slate-500 mt-1">{library.description}</p>
                                )}
                              </div>
                            </div>

                            {/* Statistics */}
                            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mt-4">
                              <div className="bg-slate-800 px-3 py-2 rounded-lg">
                                <div className="text-xs text-slate-400">Collections</div>
                                <div className="text-lg font-semibold text-white">
                                  {library.statistics?.totalCollections || 0}
                                </div>
                              </div>
                              <div className="bg-slate-800 px-3 py-2 rounded-lg">
                                <div className="text-xs text-slate-400">Media Items</div>
                                <div className="text-lg font-semibold text-white">
                                  {library.statistics?.totalMediaItems || 0}
                                </div>
                              </div>
                              <div className="bg-slate-800 px-3 py-2 rounded-lg">
                                <div className="text-xs text-slate-400">Total Size</div>
                                <div className="text-lg font-semibold text-white">
                                  {((library.statistics?.totalSize || 0) / 1024 / 1024 / 1024).toFixed(2)} GB
                                </div>
                              </div>
                              <div className="bg-slate-800 px-3 py-2 rounded-lg">
                                <div className="text-xs text-slate-400">Auto Scan</div>
                                <div className="text-lg font-semibold">
                                  <button
                                    onClick={() => handleToggleAutoScan(library.id, library.settings.autoScan)}
                                    className={`px-2 py-1 rounded text-xs font-medium transition-colors ${
                                      library.settings.autoScan
                                        ? 'bg-green-500/20 text-green-400 hover:bg-green-500/30'
                                        : 'bg-slate-700 text-slate-400 hover:bg-slate-600'
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
                              onClick={() => triggerScanMutation.mutate(library.id)}
                              disabled={triggerScanMutation.isPending}
                              className="p-2 text-primary-400 hover:bg-primary-500/10 rounded transition-colors disabled:opacity-50"
                              title="Scan now"
                            >
                              <RefreshCw className={`w-5 h-5 ${triggerScanMutation.isPending ? 'animate-spin' : ''}`} />
                            </button>
                            <button
                              onClick={() => handleDeleteLibrary(library.id)}
                              className="p-2 text-red-400 hover:bg-red-500/10 rounded transition-colors"
                              title="Delete library"
                            >
                              <Trash2 className="w-5 h-5" />
                            </button>
                          </div>
                        </div>

                        {/* Hangfire Scheduler Dashboard */}
                        {job ? (
                          <div className="mt-4 border-t border-slate-800 pt-4">
                            <div className="flex items-center justify-between mb-3">
                              <div className="flex items-center gap-3">
                                <Calendar className="w-5 h-5 text-purple-400" />
                                <div>
                                  <div className="flex items-center gap-2">
                                    <span className="font-semibold text-white">Hangfire Scheduled Job</span>
                                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${
                                      job.isEnabled ? 'bg-green-500/20 text-green-400' : 'bg-slate-700 text-slate-400'
                                    }`}>
                                      {job.isEnabled ? 'Active' : 'Paused'}
                                    </span>
                                    {/* Orphaned Job Warning */}
                                    {!job.hangfireJobId && (
                                      <span className="px-2 py-0.5 rounded text-xs font-medium bg-yellow-500/20 text-yellow-400 flex items-center gap-1">
                                        <AlertCircle className="w-3 h-3" />
                                        Orphaned
                                      </span>
                                    )}
                                    {job.hangfireJobId && (
                                      <span className="px-2 py-0.5 rounded text-xs font-medium bg-blue-500/20 text-blue-400 flex items-center gap-1">
                                        <CheckCircle className="w-3 h-3" />
                                        Bound
                                      </span>
                                    )}
                                  </div>
                                  <div className="text-xs text-slate-500 mt-0.5">
                                    Job ID: {job.id}
                                    {job.hangfireJobId && (
                                      <span className="ml-2">• Hangfire: {job.hangfireJobId}</span>
                                    )}
                                  </div>
                                </div>
                              </div>

                              <div className="flex items-center gap-2">
                                {/* Orphaned Job Actions */}
                                {!job.hangfireJobId && (
                                  <>
                                    <button
                                      onClick={() => recreateJobMutation.mutate(library.id)}
                                      disabled={recreateJobMutation.isPending}
                                      className="px-3 py-1 text-xs bg-yellow-500/20 text-yellow-300 hover:bg-yellow-500/30 rounded transition-colors disabled:opacity-50 flex items-center gap-1"
                                      title="Recreate Hangfire binding"
                                    >
                                      <RefreshCw className={`w-3 h-3 ${recreateJobMutation.isPending ? 'animate-spin' : ''}`} />
                                      Recreate
                                    </button>
                                    <button
                                      onClick={() => {
                                        if (confirm('Delete this orphaned job? You can recreate it later.')) {
                                          deleteOrphanedJobMutation.mutate(job.id);
                                        }
                                      }}
                                      disabled={deleteOrphanedJobMutation.isPending}
                                      className="px-3 py-1 text-xs bg-red-500/20 text-red-400 hover:bg-red-500/30 rounded transition-colors disabled:opacity-50 flex items-center gap-1"
                                      title="Delete orphaned job"
                                    >
                                      <Trash2 className="w-3 h-3" />
                                      Delete Job
                                    </button>
                                  </>
                                )}
                                
                                <button
                                  onClick={() => setShowSchedulerDetails({
                                    ...showSchedulerDetails,
                                    [library.id]: !showDetails
                                  })}
                                  className="px-3 py-1 text-sm text-primary-400 hover:bg-primary-500/10 rounded transition-colors"
                                >
                                  {showDetails ? 'Hide Dashboard' : 'Show Dashboard'}
                                </button>
                              </div>
                            </div>

                            {/* Orphaned Job Warning */}
                            {!job.hangfireJobId && (
                              <div className="mt-3 p-3 bg-yellow-500/10 border border-yellow-500/30 rounded-lg">
                                <div className="flex items-start gap-2">
                                  <AlertCircle className="w-4 h-4 text-yellow-400 mt-0.5 flex-shrink-0" />
                                  <div className="text-xs text-yellow-300">
                                    <p className="font-medium">Orphaned Job Detected</p>
                                    <p className="text-yellow-400/80 mt-1">
                                      This job is not bound to Hangfire yet. It will auto-bind within 5 minutes, or you can:
                                    </p>
                                    <ul className="list-disc list-inside mt-1 text-yellow-400/80">
                                      <li>Click <strong>Recreate</strong> to force immediate binding</li>
                                      <li>Click <strong>Delete Job</strong> to remove and start fresh</li>
                                    </ul>
                                  </div>
                                </div>
                              </div>
                            )}

                            {/* Always Visible: Cron Schedule */}
                            <div className="bg-slate-800/30 p-3 rounded-lg border border-slate-700/50">
                              <div className="flex items-center justify-between">
                                <div className="flex-1">
                                  <div className="text-xs text-slate-400 mb-1">Schedule (Cron Expression)</div>
                                  {editingCron[job.id] ? (
                                    <div className="space-y-2">
                                      <div className="flex items-center gap-2">
                                        <input
                                          type="text"
                                          value={cronInput[job.id] || job.cronExpression || ''}
                                          onChange={(e) => setCronInput({ ...cronInput, [job.id]: e.target.value })}
                                          className="flex-1 px-2 py-1 bg-slate-900 border border-slate-600 rounded text-white text-sm font-mono focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                                          placeholder="0 2 * * *"
                                        />
                                        <button
                                          onClick={() => {
                                            updateCronMutation.mutate({ 
                                              jobId: job.id, 
                                              cronExpression: cronInput[job.id] 
                                            });
                                          }}
                                          disabled={updateCronMutation.isPending}
                                          className="px-3 py-1 bg-primary-500 text-white text-xs rounded hover:bg-primary-600 transition-colors disabled:opacity-50"
                                        >
                                          Save
                                        </button>
                                        <button
                                          onClick={() => {
                                            setEditingCron({ ...editingCron, [job.id]: false });
                                            setCronInput({ ...cronInput, [job.id]: '' });
                                          }}
                                          className="px-3 py-1 bg-slate-700 text-slate-300 text-xs rounded hover:bg-slate-600 transition-colors"
                                        >
                                          Cancel
                                        </button>
                                      </div>
                                      {/* Quick Select Common Patterns */}
                                      <div className="flex flex-wrap gap-1">
                                        {commonCronPatterns.map((pattern) => (
                                          <button
                                            key={pattern.cron}
                                            type="button"
                                            onClick={() => setCronInput({ ...cronInput, [job.id]: pattern.cron })}
                                            className="px-2 py-0.5 text-xs bg-slate-700 text-slate-300 rounded hover:bg-slate-600 transition-colors"
                                          >
                                            {pattern.label}
                                          </button>
                                        ))}
                                      </div>
                                      <div className="text-xs text-slate-500">
                                        Format: minute hour day month dayofweek (e.g., "0 2 * * *" = Daily at 2 AM)
                                      </div>
                                    </div>
                                  ) : (
                                    <div className="flex items-center justify-between">
                                      <div>
                                        <div className="font-mono text-sm text-primary-400">{job.cronExpression || 'Not set'}</div>
                                        <div className="text-xs text-slate-500 mt-0.5">{formatCronExpression(job.cronExpression)}</div>
                                      </div>
                                      <button
                                        onClick={() => {
                                          setEditingCron({ ...editingCron, [job.id]: true });
                                          setCronInput({ ...cronInput, [job.id]: job.cronExpression || '' });
                                        }}
                                        className="px-2 py-1 text-xs text-slate-400 hover:text-white hover:bg-slate-700 rounded transition-colors"
                                      >
                                        Edit
                                      </button>
                                    </div>
                                  )}
                                </div>
                              </div>
                              
                              {/* Next Run Time */}
                              {job.nextRunAt && (
                                <div className="mt-2 flex items-center gap-2 text-xs">
                                  <Clock className="w-3 h-3 text-primary-400" />
                                  <span className="text-slate-400">Next run:</span>
                                  <span className="font-medium text-primary-400">
                                    {formatDistanceToNow(new Date(job.nextRunAt), { addSuffix: true })}
                                  </span>
                                </div>
                              )}
                            </div>

                            {/* Expanded Scheduler Details */}
                            {showDetails && (
                              <div className="mt-4 grid grid-cols-1 md:grid-cols-2 gap-4 bg-slate-800/50 p-4 rounded-lg border border-slate-700">
                                {/* Execution Statistics */}
                                <div>
                                  <h4 className="font-medium text-white mb-2">Execution Statistics</h4>
                                  <div className="space-y-2 text-sm">
                                    <div className="flex justify-between">
                                      <span className="text-slate-400">Total Runs:</span>
                                      <span className="font-medium text-white">{job.runCount}</span>
                                    </div>
                                    <div className="flex justify-between">
                                      <span className="text-slate-400">Successful:</span>
                                      <span className="font-medium text-green-400">{job.successCount}</span>
                                    </div>
                                    <div className="flex justify-between">
                                      <span className="text-slate-400">Failed:</span>
                                      <span className="font-medium text-red-400">{job.failureCount}</span>
                                    </div>
                                    <div className="flex justify-between">
                                      <span className="text-slate-400">Success Rate:</span>
                                      <span className="font-medium text-white">
                                        {job.runCount > 0 
                                          ? ((job.successCount / job.runCount) * 100).toFixed(1) + '%'
                                          : 'N/A'}
                                      </span>
                                    </div>
                                  </div>
                                </div>

                                {/* Last Run Information */}
                                <div>
                                  <h4 className="font-medium text-white mb-2">Last Run</h4>
                                  <div className="space-y-2 text-sm">
                                    {job.lastRunAt ? (
                                      <>
                                        <div className="flex justify-between">
                                          <span className="text-slate-400">Time:</span>
                                          <span className="font-medium text-white">
                                            {formatDistanceToNow(new Date(job.lastRunAt), { addSuffix: true })}
                                          </span>
                                        </div>
                                        <div className="flex justify-between items-center">
                                          <span className="text-slate-400">Status:</span>
                                          <span className={`flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium ${
                                            getStatusColor(job.lastRunStatus)
                                          }`}>
                                            {getStatusIcon(job.lastRunStatus)}
                                            {job.lastRunStatus || 'Unknown'}
                                          </span>
                                        </div>
                                        {job.lastRunDuration && (
                                          <div className="flex justify-between">
                                            <span className="text-slate-400">Duration:</span>
                                            <span className="font-medium text-white">
                                              {(job.lastRunDuration / 1000).toFixed(2)}s
                                            </span>
                                          </div>
                                        )}
                                        {job.lastErrorMessage && (
                                          <div className="col-span-2 mt-2">
                                            <span className="text-slate-400 block mb-1">Error:</span>
                                            <div className="text-red-400 text-xs bg-red-500/10 border border-red-500/20 p-2 rounded">
                                              {job.lastErrorMessage}
                                            </div>
                                          </div>
                                        )}
                                      </>
                                    ) : (
                                      <p className="text-slate-500">Never executed</p>
                                    )}
                                  </div>
                                </div>

                                {/* Next Run */}
                                {job.nextRunAt && (
                                  <div className="col-span-full">
                                    <div className="flex items-center gap-2 text-sm">
                                      <Clock className="w-4 h-4 text-primary-400" />
                                      <span className="text-slate-400">Next scheduled run:</span>
                                      <span className="font-medium text-primary-400">
                                        {formatDistanceToNow(new Date(job.nextRunAt), { addSuffix: true })}
                                      </span>
                                    </div>
                                  </div>
                                )}

                                {/* Actions */}
                                <div className="col-span-full flex gap-2 pt-2 border-t border-slate-700">
                                  <button
                                    onClick={() => handleToggleJob(job.id, job.isEnabled)}
                                    disabled={toggleJobMutation.isPending}
                                    className={`flex items-center gap-2 px-3 py-1.5 rounded text-sm font-medium transition-colors ${
                                      job.isEnabled
                                        ? 'bg-orange-500/20 text-orange-400 hover:bg-orange-500/30'
                                        : 'bg-green-500/20 text-green-400 hover:bg-green-500/30'
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
                        ) : library.settings.autoScan ? (
                          <div className="mt-4 border-t border-slate-800 pt-4">
                            <div className="bg-yellow-500/10 border border-yellow-500/20 p-4 rounded-lg">
                              <div className="flex items-start gap-3">
                                <AlertCircle className="w-5 h-5 text-yellow-400 mt-0.5" />
                                <div>
                                  <p className="text-sm font-medium text-yellow-300">No Scheduled Job Found</p>
                                  <p className="text-xs text-yellow-400/80 mt-1">
                                    AutoScan is enabled but no Hangfire job was created. This might be a registration issue.
                                  </p>
                                  <div className="mt-2">
                                    <button
                                      onClick={() => triggerScanMutation.mutate(library.id)}
                                      disabled={triggerScanMutation.isPending}
                                      className="text-xs px-3 py-1.5 bg-yellow-500/20 text-yellow-300 hover:bg-yellow-500/30 rounded transition-colors"
                                    >
                                      Trigger Manual Scan
                                    </button>
                                  </div>
                                </div>
                              </div>
                            </div>
                          </div>
                        ) : null}
                      </div>
                    </div>
                  );
                })
              )}
            </>
          )}
        </div>
      </div>

      {/* Create Library Modal */}
      {showCreateModal && (
        <div className="fixed inset-0 bg-black/70 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-lg shadow-2xl max-w-md w-full max-h-[90vh] overflow-y-auto">
            {/* Modal Header */}
            <div className="flex justify-between items-center p-6 border-b border-slate-800 sticky top-0 bg-slate-900">
              <h2 className="text-2xl font-bold text-white">Create New Library</h2>
              <button
                onClick={() => {
                  setShowCreateModal(false);
                  setFormData({ name: '', path: '', description: '', autoScan: true });
                  setFormErrors({});
                }}
                className="p-2 text-slate-400 hover:text-white hover:bg-slate-800 rounded-lg transition-colors"
                disabled={createMutation.isPending}
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Modal Body */}
            <form onSubmit={handleCreateLibrary} className="p-6 space-y-4">
              {/* Library Name */}
              <div>
                <label htmlFor="name" className="block text-sm font-medium text-slate-300 mb-1">
                  Library Name <span className="text-red-400">*</span>
                </label>
                <input
                  type="text"
                  id="name"
                  value={formData.name}
                  onChange={(e) => {
                    setFormData({ ...formData, name: e.target.value });
                    setFormErrors({ ...formErrors, name: '' });
                  }}
                  className={`w-full px-3 py-2 bg-slate-800 border rounded-lg text-white placeholder-slate-500 focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
                    formErrors.name ? 'border-red-500' : 'border-slate-700'
                  }`}
                  placeholder="My Media Library"
                  disabled={createMutation.isPending}
                  maxLength={100}
                />
                {formErrors.name && (
                  <p className="mt-1 text-sm text-red-400">{formErrors.name}</p>
                )}
              </div>

              {/* Library Path */}
              <div>
                <label htmlFor="path" className="block text-sm font-medium text-slate-300 mb-1">
                  Library Path <span className="text-red-400">*</span>
                </label>
                <input
                  type="text"
                  id="path"
                  value={formData.path}
                  onChange={(e) => {
                    setFormData({ ...formData, path: e.target.value });
                    setFormErrors({ ...formErrors, path: '' });
                  }}
                  className={`w-full px-3 py-2 bg-slate-800 border rounded-lg text-white placeholder-slate-500 focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
                    formErrors.path ? 'border-red-500' : 'border-slate-700'
                  }`}
                  placeholder="D:\Media\Photos or /media/photos"
                  disabled={createMutation.isPending}
                />
                {formErrors.path && (
                  <p className="mt-1 text-sm text-red-400">{formErrors.path}</p>
                )}
                <p className="mt-1 text-xs text-slate-500">
                  Absolute path to the directory containing your media files
                </p>
              </div>

              {/* Description */}
              <div>
                <label htmlFor="description" className="block text-sm font-medium text-slate-300 mb-1">
                  Description
                </label>
                <textarea
                  id="description"
                  value={formData.description}
                  onChange={(e) => {
                    setFormData({ ...formData, description: e.target.value });
                    setFormErrors({ ...formErrors, description: '' });
                  }}
                  className={`w-full px-3 py-2 bg-slate-800 border rounded-lg text-white placeholder-slate-500 focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ${
                    formErrors.description ? 'border-red-500' : 'border-slate-700'
                  }`}
                  placeholder="Optional description of this library"
                  rows={3}
                  disabled={createMutation.isPending}
                  maxLength={500}
                />
                {formErrors.description && (
                  <p className="mt-1 text-sm text-red-400">{formErrors.description}</p>
                )}
                <p className="mt-1 text-xs text-slate-500 text-right">
                  {formData.description.length}/500
                </p>
              </div>

              {/* Auto Scan Toggle */}
              <div className="flex items-center justify-between p-4 bg-slate-800/50 rounded-lg border border-slate-700">
                <div className="flex-1">
                  <label htmlFor="autoScan" className="text-sm font-medium text-white">
                    Enable Automatic Scanning
                  </label>
                  <p className="text-xs text-slate-400 mt-1">
                    Automatically scan this library daily at 2:00 AM for new content
                  </p>
                </div>
                <label className="relative inline-flex items-center cursor-pointer ml-4">
                  <input
                    type="checkbox"
                    id="autoScan"
                    checked={formData.autoScan}
                    onChange={(e) => setFormData({ ...formData, autoScan: e.target.checked })}
                    className="sr-only peer"
                    disabled={createMutation.isPending}
                  />
                  <div className="w-11 h-6 bg-slate-700 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary-500/30 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-600 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary-500"></div>
                </label>
              </div>

              {/* Info Box */}
              {formData.autoScan && (
                <div className="p-4 bg-primary-500/10 border border-primary-500/20 rounded-lg">
                  <div className="flex items-start gap-3">
                    <Calendar className="w-5 h-5 text-primary-400 mt-0.5" />
                    <div>
                      <p className="text-sm font-medium text-primary-300">Scheduled Scan Enabled</p>
                      <p className="text-xs text-primary-400/80 mt-1">
                        A scheduled job will be automatically created to scan this library daily at 2:00 AM.
                        You can modify the schedule later in the library settings.
                      </p>
                    </div>
                  </div>
                </div>
              )}

              {/* Form Actions */}
              <div className="flex gap-3 pt-4 border-t border-slate-800">
                <button
                  type="button"
                  onClick={() => {
                    setShowCreateModal(false);
                    setFormData({ name: '', path: '', description: '', autoScan: true });
                    setFormErrors({});
                  }}
                  className="flex-1 px-4 py-2 border border-slate-700 text-slate-300 rounded-lg hover:bg-slate-800 transition-colors"
                  disabled={createMutation.isPending}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="flex-1 px-4 py-2 bg-primary-500 text-white rounded-lg hover:bg-primary-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                  disabled={createMutation.isPending}
                >
                  {createMutation.isPending ? (
                    <span className="flex items-center justify-center gap-2">
                      <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                      Creating...
                    </span>
                  ) : (
                    'Create Library'
                  )}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

