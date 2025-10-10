import { useState } from 'react';
import { Activity, Clock, CheckCircle2, XCircle, AlertCircle, Loader2, Trash2, BarChart3 } from 'lucide-react';
import { useBackgroundJobs, useJobStatistics, useCancelJob, useCleanupJobs } from '../hooks/useBackgroundJobs';
import LoadingSpinner from '../components/ui/LoadingSpinner';
import Button from '../components/ui/Button';
import { formatDistanceToNow } from 'date-fns';

const BackgroundJobs: React.FC = () => {
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [jobTypeFilter, setJobTypeFilter] = useState<string>('');

  const { data, isLoading, error } = useBackgroundJobs(page, 20, statusFilter || undefined, jobTypeFilter || undefined);
  const { data: stats } = useJobStatistics();
  const cancelMutation = useCancelJob();
  const cleanupMutation = useCleanupJobs();

  const getStatusIcon = (status: string) => {
    switch (status.toLowerCase()) {
      case 'pending':
        return <Clock className="h-5 w-5 text-slate-400" />;
      case 'running':
      case 'inprogress':
        return <Loader2 className="h-5 w-5 text-blue-500 animate-spin" />;
      case 'completed':
        return <CheckCircle2 className="h-5 w-5 text-green-500" />;
      case 'failed':
        return <XCircle className="h-5 w-5 text-red-500" />;
      case 'cancelled':
        return <AlertCircle className="h-5 w-5 text-orange-500" />;
      default:
        return <Activity className="h-5 w-5 text-slate-400" />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'pending':
        return 'bg-slate-500/20 text-slate-400 border-slate-500/30';
      case 'running':
      case 'inprogress':
        return 'bg-blue-500/20 text-blue-400 border-blue-500/30';
      case 'completed':
        return 'bg-green-500/20 text-green-400 border-green-500/30';
      case 'failed':
        return 'bg-red-500/20 text-red-400 border-red-500/30';
      case 'cancelled':
        return 'bg-orange-500/20 text-orange-400 border-orange-500/30';
      default:
        return 'bg-slate-500/20 text-slate-400 border-slate-500/30';
    }
  };

  const formatDuration = (seconds?: number) => {
    if (!seconds) return 'N/A';
    if (seconds < 60) return `${seconds.toFixed(1)}s`;
    if (seconds < 3600) return `${(seconds / 60).toFixed(1)}m`;
    return `${(seconds / 3600).toFixed(1)}h`;
  };

  if (isLoading) {
    return <LoadingSpinner text="Loading background jobs..." />;
  }

  if (error) {
    return (
      <div className="h-full flex items-center justify-center">
        <div className="text-center">
          <XCircle className="h-12 w-12 text-red-500 mx-auto mb-4" />
          <p className="text-red-500 text-lg">Failed to load background jobs</p>
          <p className="text-slate-400 text-sm mt-2">{error.message}</p>
        </div>
      </div>
    );
  }

  const jobs = data?.data || [];
  const pagination = data?.pagination;

  return (
    <div className="h-full flex flex-col">
      {/* Header */}
      <div className="flex-shrink-0 border-b border-slate-800 bg-slate-900/50 backdrop-blur">
        <div className="px-6 py-4">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold text-white flex items-center space-x-3">
                <Activity className="h-7 w-7 text-blue-500" />
                <span>Background Jobs</span>
              </h1>
              <p className="text-sm text-slate-400 mt-1">Monitor and manage background processing tasks</p>
            </div>
            <Button
              variant="ghost"
              onClick={() => cleanupMutation.mutate(7)}
              disabled={cleanupMutation.isPending}
            >
              <Trash2 className="h-4 w-4 mr-2" />
              {cleanupMutation.isPending ? 'Cleaning...' : 'Cleanup Old Jobs'}
            </Button>
          </div>
        </div>
      </div>

      {/* Statistics Cards */}
      {stats && (
        <div className="flex-shrink-0 border-b border-slate-800 bg-slate-900/30 px-6 py-4">
          <div className="grid grid-cols-2 md:grid-cols-6 gap-4">
            <div className="bg-slate-800/50 rounded-lg p-4 border border-slate-700">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-slate-400">Total</p>
                  <p className="text-2xl font-bold text-white">{stats.total}</p>
                </div>
                <Activity className="h-6 w-6 text-slate-500" />
              </div>
            </div>

            <div className="bg-slate-500/10 rounded-lg p-4 border border-slate-500/20">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-slate-400">Pending</p>
                  <p className="text-2xl font-bold text-slate-300">{stats.pending}</p>
                </div>
                <Clock className="h-6 w-6 text-slate-400" />
              </div>
            </div>

            <div className="bg-blue-500/10 rounded-lg p-4 border border-blue-500/20">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-blue-400">Running</p>
                  <p className="text-2xl font-bold text-blue-300">{stats.running}</p>
                </div>
                <Loader2 className="h-6 w-6 text-blue-500 animate-spin" />
              </div>
            </div>

            <div className="bg-green-500/10 rounded-lg p-4 border border-green-500/20">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-green-400">Completed</p>
                  <p className="text-2xl font-bold text-green-300">{stats.completed}</p>
                </div>
                <CheckCircle2 className="h-6 w-6 text-green-500" />
              </div>
            </div>

            <div className="bg-red-500/10 rounded-lg p-4 border border-red-500/20">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-red-400">Failed</p>
                  <p className="text-2xl font-bold text-red-300">{stats.failed}</p>
                </div>
                <XCircle className="h-6 w-6 text-red-500" />
              </div>
            </div>

            <div className="bg-purple-500/10 rounded-lg p-4 border border-purple-500/20">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-purple-400">Avg Time</p>
                  <p className="text-2xl font-bold text-purple-300">{formatDuration(stats.avgDuration)}</p>
                </div>
                <BarChart3 className="h-6 w-6 text-purple-500" />
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Filters */}
      <div className="flex-shrink-0 border-b border-slate-800 px-6 py-3 bg-slate-900/20">
        <div className="flex items-center space-x-4">
          <select
            value={statusFilter}
            onChange={(e) => {
              setStatusFilter(e.target.value);
              setPage(1);
            }}
            className="px-3 py-2 bg-slate-800 border border-slate-700 rounded-lg text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="">All Statuses</option>
            <option value="Pending">Pending</option>
            <option value="Running">Running</option>
            <option value="Completed">Completed</option>
            <option value="Failed">Failed</option>
            <option value="Cancelled">Cancelled</option>
          </select>

          <select
            value={jobTypeFilter}
            onChange={(e) => {
              setJobTypeFilter(e.target.value);
              setPage(1);
            }}
            className="px-3 py-2 bg-slate-800 border border-slate-700 rounded-lg text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="">All Types</option>
            <option value="collection-scan">Collection Scan</option>
            <option value="thumbnail-generation">Thumbnail Generation</option>
            <option value="cache-generation">Cache Generation</option>
          </select>
        </div>
      </div>

      {/* Jobs List */}
      <div className="flex-1 overflow-y-auto px-6 py-4">
        {jobs.length > 0 ? (
          <div className="space-y-3">
            {jobs.map((job) => (
              <div
                key={job.id}
                className="bg-slate-800 border border-slate-700 rounded-lg p-4 hover:border-slate-600 transition-colors"
              >
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    {/* Header */}
                    <div className="flex items-center space-x-3 mb-2">
                      {getStatusIcon(job.status)}
                      <h3 className="text-base font-semibold text-white">{job.jobType}</h3>
                      <span className={`px-2 py-0.5 text-xs rounded border ${getStatusColor(job.status)}`}>
                        {job.status}
                      </span>
                      {job.currentStage && (
                        <span className="px-2 py-0.5 text-xs bg-purple-500/20 text-purple-400 rounded border border-purple-500/30">
                          {job.currentStage}
                        </span>
                      )}
                    </div>

                    {/* Progress Bar */}
                    {job.status === 'Running' && (
                      <div className="mb-3">
                        <div className="flex items-center justify-between text-xs text-slate-400 mb-1">
                          <span>{job.completedItems} / {job.totalItems} items</span>
                          <span>{job.progress}%</span>
                        </div>
                        <div className="w-full bg-slate-700 rounded-full h-2">
                          <div
                            className="bg-blue-500 h-2 rounded-full transition-all duration-300"
                            style={{ width: `${job.progress}%` }}
                          />
                        </div>
                      </div>
                    )}

                    {/* Stages */}
                    {job.stages && Object.keys(job.stages).length > 0 && (
                      <div className="mb-3 grid grid-cols-3 gap-2">
                        {Object.entries(job.stages).map(([stageName, stage]) => (
                          <div key={stageName} className="bg-slate-700/50 rounded px-2 py-1.5 border border-slate-600/50">
                            <div className="flex items-center justify-between">
                              <span className="text-xs font-medium text-slate-300 capitalize">{stageName}</span>
                              <span className="text-xs text-slate-400">{stage.progress}%</span>
                            </div>
                            <div className="w-full bg-slate-600 rounded-full h-1 mt-1">
                              <div
                                className="bg-blue-400 h-1 rounded-full transition-all"
                                style={{ width: `${stage.progress}%` }}
                              />
                            </div>
                          </div>
                        ))}
                      </div>
                    )}

                    {/* Message */}
                    {job.message && (
                      <p className="text-sm text-slate-400 mb-2">{job.message}</p>
                    )}

                    {/* Error */}
                    {job.errorMessage && (
                      <p className="text-sm text-red-400 mb-2">❌ {job.errorMessage}</p>
                    )}

                    {/* Meta Info */}
                    <div className="flex items-center space-x-4 text-xs text-slate-500">
                      {job.createdAt && (
                        <span>
                          Created: {
                            (() => {
                              try {
                                const date = new Date(job.createdAt);
                                return isNaN(date.getTime()) ? 'Unknown' : formatDistanceToNow(date, { addSuffix: true });
                              } catch {
                                return 'Unknown';
                              }
                            })()
                          }
                        </span>
                      )}
                      {job.duration && <span>Duration: {formatDuration(job.duration)}</span>}
                      {job.currentItem && <span className="text-slate-400">Current: {job.currentItem}</span>}
                    </div>
                  </div>

                  {/* Actions */}
                  <div className="ml-4">
                    {(job.status === 'Pending' || job.status === 'Running') && (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => cancelMutation.mutate(job.id)}
                        disabled={cancelMutation.isPending}
                      >
                        Cancel
                      </Button>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        ) : (
          <div className="text-center py-12">
            <Activity className="h-12 w-12 text-slate-600 mx-auto mb-4" />
            <p className="text-slate-400">No background jobs found</p>
            <p className="text-sm text-slate-500 mt-1">Jobs will appear here when processing starts</p>
          </div>
        )}
      </div>

      {/* Pagination */}
      {pagination && pagination.totalPages > 1 && (
        <div className="flex-shrink-0 border-t border-slate-800 px-6 py-4 bg-slate-900/30">
          <div className="flex items-center justify-between">
            <div className="text-sm text-slate-400">
              Showing {((pagination.page - 1) * 20) + 1}-{Math.min(pagination.page * 20, pagination.total)} of {pagination.total} jobs
            </div>
            <div className="flex items-center space-x-3">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setPage(1)}
                disabled={!pagination.hasPrevious}
              >
                First
              </Button>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setPage(page - 1)}
                disabled={!pagination.hasPrevious}
              >
                Previous
              </Button>
              
              {/* Page Input */}
              <div className="flex items-center space-x-2">
                <span className="text-sm text-slate-400">Page</span>
                <input
                  type="number"
                  min={1}
                  max={pagination.totalPages}
                  value={page}
                  onChange={(e) => {
                    const newPage = parseInt(e.target.value);
                    if (newPage >= 1 && newPage <= pagination.totalPages) {
                      setPage(newPage);
                    }
                  }}
                  className="w-16 px-2 py-1 bg-slate-800 border border-slate-700 rounded text-white text-sm text-center focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
                <span className="text-sm text-slate-400">of {pagination.totalPages}</span>
              </div>
              
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setPage(page + 1)}
                disabled={!pagination.hasNext}
              >
                Next
              </Button>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setPage(pagination.totalPages)}
                disabled={!pagination.hasNext}
              >
                Last
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default BackgroundJobs;

