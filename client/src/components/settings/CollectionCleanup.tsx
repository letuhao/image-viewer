import { useState } from 'react';
import { Trash2, Archive, AlertTriangle, CheckCircle, Clock } from 'lucide-react';
import Button from '../ui/Button';
import LoadingSpinner from '../ui/LoadingSpinner';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '../../services/api';
import toast from 'react-hot-toast';

interface CollectionCleanupResult {
  message: string;
  totalCollectionsChecked: number;
  nonExistentCollectionsFound: number;
  collectionsDeleted: number;
  errors: number;
  duration: string;
  deletedPaths: string[];
  errorMessages: string[];
}

const CollectionCleanup: React.FC = () => {
  const [isRunning, setIsRunning] = useState(false);
  const [result, setResult] = useState<CollectionCleanupResult | null>(null);
  const queryClient = useQueryClient();

  const cleanupMutation = useMutation({
    mutationFn: async () => {
      const response = await api.post('/collections/cleanup');
      return response.data as CollectionCleanupResult;
    },
    onSuccess: (data) => {
      setResult(data);
      setIsRunning(false);
      toast.success(`Cleanup completed! Archived ${data.collectionsDeleted} collections.`);
      
      // Invalidate collections query to refresh the list
      queryClient.invalidateQueries({ queryKey: ['collections'] });
    },
    onError: (error: any) => {
      setIsRunning(false);
      toast.error('Collection cleanup failed: ' + (error.response?.data?.message || error.message));
    },
  });

  const handleCleanup = async () => {
    const confirmed = window.confirm(
      'This will check all collections and archive those whose files/folders no longer exist on disk.\n\n' +
      '⚠️ WARNING: This action cannot be undone! Collections will be moved to archive for backup.\n\n' +
      'Are you sure you want to proceed?'
    );

    if (!confirmed) return;

    setIsRunning(true);
    setResult(null);
    cleanupMutation.mutate();
  };

  const formatDuration = (duration: string) => {
    // Parse duration string (e.g., "00:00:05.1234567") and format it nicely
    const parts = duration.split(':');
    if (parts.length === 3) {
      const hours = parseInt(parts[0]);
      const minutes = parseInt(parts[1]);
      const seconds = parseFloat(parts[2]);
      
      if (hours > 0) {
        return `${hours}h ${minutes}m ${seconds.toFixed(1)}s`;
      } else if (minutes > 0) {
        return `${minutes}m ${seconds.toFixed(1)}s`;
      } else {
        return `${seconds.toFixed(1)}s`;
      }
    }
    return duration;
  };

  return (
    <div className="space-y-4">
      <div className="bg-slate-800/50 border border-slate-700 rounded-lg p-4">
        <div className="flex items-start space-x-3">
          <div className="flex-shrink-0">
            <Archive className="h-5 w-5 text-blue-400" />
          </div>
          <div className="flex-1">
            <h3 className="text-sm font-medium text-white">Collection Cleanup</h3>
            <p className="text-sm text-slate-400 mt-1">
              Remove collections whose files or folders no longer exist on disk. 
              Collections are archived for backup instead of being permanently deleted.
            </p>
          </div>
        </div>

        <div className="mt-4">
          <Button
            variant="danger"
            onClick={handleCleanup}
            disabled={isRunning}
            className="w-full sm:w-auto"
          >
            {isRunning ? (
              <>
                <LoadingSpinner size="sm" />
                <span className="ml-2">Running Cleanup...</span>
              </>
            ) : (
              <>
                <Trash2 className="h-4 w-4 mr-2" />
                Clean Up Collections
              </>
            )}
          </Button>
        </div>

        {/* Results */}
        {result && (
          <div className="mt-6 bg-slate-900/50 border border-slate-600 rounded-lg p-4">
            <div className="flex items-center space-x-2 mb-4">
              <CheckCircle className="h-5 w-5 text-green-400" />
              <h4 className="text-sm font-medium text-white">Cleanup Results</h4>
            </div>

            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
              <div className="bg-slate-800/50 rounded-lg p-3">
                <div className="text-xs text-slate-400 uppercase tracking-wide">Checked</div>
                <div className="text-lg font-semibold text-white">{result.totalCollectionsChecked}</div>
              </div>
              <div className="bg-slate-800/50 rounded-lg p-3">
                <div className="text-xs text-slate-400 uppercase tracking-wide">Non-Existent</div>
                <div className="text-lg font-semibold text-orange-400">{result.nonExistentCollectionsFound}</div>
              </div>
              <div className="bg-slate-800/50 rounded-lg p-3">
                <div className="text-xs text-slate-400 uppercase tracking-wide">Archived</div>
                <div className="text-lg font-semibold text-green-400">{result.collectionsDeleted}</div>
              </div>
              <div className="bg-slate-800/50 rounded-lg p-3">
                <div className="text-xs text-slate-400 uppercase tracking-wide">Errors</div>
                <div className="text-lg font-semibold text-red-400">{result.errors}</div>
              </div>
            </div>

            <div className="flex items-center space-x-2 mb-3">
              <Clock className="h-4 w-4 text-slate-400" />
              <span className="text-sm text-slate-400">Duration: {formatDuration(result.duration)}</span>
            </div>

            {/* Archived Paths */}
            {result.deletedPaths.length > 0 && (
              <div className="mb-4">
                <h5 className="text-sm font-medium text-white mb-2">
                  Archived Collections ({result.deletedPaths.length}):
                </h5>
                <div className="bg-slate-900/50 rounded-lg p-3 max-h-32 overflow-y-auto">
                  <div className="space-y-1">
                    {result.deletedPaths.map((path, index) => (
                      <div key={index} className="text-xs text-slate-300 font-mono">
                        {path}
                      </div>
                    ))}
                    {result.deletedPaths.length === 10 && (
                      <div className="text-xs text-slate-500 italic">
                        ... showing first 10 results
                      </div>
                    )}
                  </div>
                </div>
              </div>
            )}

            {/* Error Messages */}
            {result.errorMessages.length > 0 && (
              <div>
                <h5 className="text-sm font-medium text-red-400 mb-2">
                  Errors ({result.errorMessages.length}):
                </h5>
                <div className="bg-red-900/20 border border-red-800 rounded-lg p-3 max-h-32 overflow-y-auto">
                  <div className="space-y-1">
                    {result.errorMessages.map((error, index) => (
                      <div key={index} className="text-xs text-red-300">
                        {error}
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            )}
          </div>
        )}

        {/* Warning */}
        <div className="mt-4 bg-orange-900/20 border border-orange-800 rounded-lg p-3">
          <div className="flex items-start space-x-2">
            <AlertTriangle className="h-4 w-4 text-orange-400 flex-shrink-0 mt-0.5" />
            <div className="text-sm text-orange-200">
              <strong>Important:</strong> This operation will check all collections against the file system. 
              Collections with missing files or folders will be archived to the backup table. 
              This helps maintain a clean database while preserving data for potential recovery.
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default CollectionCleanup;
