import { useState, useEffect } from 'react';
import { Server, RefreshCw, CheckCircle, XCircle, Clock, Database, AlertCircle } from 'lucide-react';
import Button from '../ui/Button';
import SettingsSection from './SettingsSection';
import SettingItem from './SettingItem';
import LoadingSpinner from '../ui/LoadingSpinner';
import { api } from '../../services/api';
import toast from 'react-hot-toast';

interface RedisIndexStats {
  totalCollections: number;
  lastRebuildTime: string | null;
  isValid: boolean;
  redisConnected: boolean;
}

/**
 * Redis Index Management Component
 * 
 * Manage Redis collection index for fast search and sorting
 */
const RedisIndexManagement: React.FC = () => {
  const [stats, setStats] = useState<RedisIndexStats | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isRebuilding, setIsRebuilding] = useState(false);
  const [isValidating, setIsValidating] = useState(false);

  // Fetch index stats
  const fetchStats = async () => {
    setIsLoading(true);
    try {
      const response = await api.get<RedisIndexStats>('/collections/index/stats');
      setStats(response.data);
    } catch (error: any) {
      console.error('Failed to fetch index stats:', error);
      toast.error(error.response?.data?.message || 'Failed to fetch index statistics');
    } finally {
      setIsLoading(false);
    }
  };

  // Check if index is valid
  const validateIndex = async () => {
    setIsValidating(true);
    try {
      const response = await api.get<{ isValid: boolean }>('/collections/index/validate');
      if (response.data.isValid) {
        toast.success('✅ Redis index is valid and up-to-date');
      } else {
        toast.error('⚠️ Redis index is invalid or outdated');
      }
      // Refresh stats
      await fetchStats();
    } catch (error: any) {
      console.error('Failed to validate index:', error);
      toast.error(error.response?.data?.message || 'Failed to validate index');
    } finally {
      setIsValidating(false);
    }
  };

  // Rebuild index
  const rebuildIndex = async () => {
    if (!confirm('Are you sure you want to rebuild the Redis index? This may take a few minutes for large databases.')) {
      return;
    }

    setIsRebuilding(true);
    try {
      await api.post('/collections/index/rebuild');
      toast.success('✅ Index rebuild started! This will complete in the background.');
      
      // Wait a bit then refresh stats
      setTimeout(async () => {
        await fetchStats();
        setIsRebuilding(false);
      }, 3000);
    } catch (error: any) {
      console.error('Failed to rebuild index:', error);
      toast.error(error.response?.data?.message || 'Failed to rebuild index');
      setIsRebuilding(false);
    }
  };

  // Load stats on mount
  useEffect(() => {
    fetchStats();
  }, []);

  const formatDate = (dateString: string | null): string => {
    if (!dateString) return 'Never';
    try {
      return new Date(dateString).toLocaleString();
    } catch {
      return 'Invalid date';
    }
  };

  return (
    <SettingsSection
      title="Redis Index Management"
      description="Manage the Redis collection index for fast search and sorting"
    >
      {/* Statistics Cards */}
      {isLoading && !stats ? (
        <div className="py-8">
          <LoadingSpinner text="Loading index statistics..." />
        </div>
      ) : stats ? (
        <div className="space-y-4">
          {/* Stats Grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* Total Collections */}
            <div className="bg-slate-700/30 rounded-lg p-4 border border-slate-600/50">
              <div className="flex items-center justify-between mb-2">
                <span className="text-sm text-slate-400">Total Collections</span>
                <Database className="h-5 w-5 text-blue-400" />
              </div>
              <div className="text-2xl font-bold text-white">
                {stats.totalCollections.toLocaleString()}
              </div>
            </div>

            {/* Index Status */}
            <div className="bg-slate-700/30 rounded-lg p-4 border border-slate-600/50">
              <div className="flex items-center justify-between mb-2">
                <span className="text-sm text-slate-400">Index Status</span>
                {stats.isValid ? (
                  <CheckCircle className="h-5 w-5 text-green-400" />
                ) : (
                  <XCircle className="h-5 w-5 text-red-400" />
                )}
              </div>
              <div className={`text-xl font-semibold ${
                stats.isValid ? 'text-green-400' : 'text-red-400'
              }`}>
                {stats.isValid ? 'Valid' : 'Invalid'}
              </div>
            </div>

            {/* Last Rebuild */}
            <div className="bg-slate-700/30 rounded-lg p-4 border border-slate-600/50">
              <div className="flex items-center justify-between mb-2">
                <span className="text-sm text-slate-400">Last Rebuild</span>
                <Clock className="h-5 w-5 text-purple-400" />
              </div>
              <div className="text-sm font-medium text-white">
                {formatDate(stats.lastRebuildTime)}
              </div>
            </div>

            {/* Redis Connection */}
            <div className="bg-slate-700/30 rounded-lg p-4 border border-slate-600/50">
              <div className="flex items-center justify-between mb-2">
                <span className="text-sm text-slate-400">Redis Connection</span>
                <Server className={`h-5 w-5 ${
                  stats.redisConnected ? 'text-green-400' : 'text-red-400'
                }`} />
              </div>
              <div className={`text-xl font-semibold ${
                stats.redisConnected ? 'text-green-400' : 'text-red-400'
              }`}>
                {stats.redisConnected ? 'Connected' : 'Disconnected'}
              </div>
            </div>
          </div>

          {/* Action Buttons */}
          <SettingItem
            label="Actions"
            description="Manage the Redis collection index"
            vertical
          >
            <div className="flex flex-wrap gap-3">
              <Button
                variant="ghost"
                onClick={fetchStats}
                disabled={isLoading}
                className="flex items-center space-x-2"
              >
                <RefreshCw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
                <span>Refresh Stats</span>
              </Button>

              <Button
                variant="ghost"
                onClick={validateIndex}
                disabled={isValidating || isLoading}
                className="flex items-center space-x-2"
              >
                <CheckCircle className="h-4 w-4" />
                <span>{isValidating ? 'Validating...' : 'Check Validity'}</span>
              </Button>

              <Button
                variant="primary"
                onClick={rebuildIndex}
                disabled={isRebuilding || isLoading}
                className="flex items-center space-x-2"
              >
                <RefreshCw className={`h-4 w-4 ${isRebuilding ? 'animate-spin' : ''}`} />
                <span>{isRebuilding ? 'Rebuilding...' : 'Rebuild Index'}</span>
              </Button>
            </div>
          </SettingItem>

          {/* Info Box */}
          <div className="bg-blue-900/20 border border-blue-500/30 rounded-lg p-4">
            <div className="flex items-start space-x-3">
              <AlertCircle className="h-5 w-5 text-blue-400 flex-shrink-0 mt-0.5" />
              <div className="flex-1">
                <p className="text-blue-300 text-sm font-medium mb-2">
                  When to rebuild the index:
                </p>
                <ul className="text-blue-300/80 text-sm space-y-1 list-disc list-inside">
                  <li>After bulk importing many collections</li>
                  <li>If search/sorting is slow or showing incorrect results</li>
                  <li>After Redis was restarted or cleared</li>
                  <li>If the index statistics show mismatched counts</li>
                </ul>
              </div>
            </div>
          </div>
        </div>
      ) : (
        <div className="text-center py-8 text-slate-400">
          No statistics available
        </div>
      )}
    </SettingsSection>
  );
};

export default RedisIndexManagement;

