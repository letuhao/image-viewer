import { useState } from 'react';
import { FolderOpen, Plus, Trash2, Edit2, HardDrive, BarChart3, AlertTriangle } from 'lucide-react';
import Button from '../ui/Button';
import SettingItem from './SettingItem';
import LoadingSpinner from '../ui/LoadingSpinner';
import { useCacheFolders, useCreateCacheFolder, useUpdateCacheFolder, useDeleteCacheFolder, useValidatePath } from '../../hooks/useCacheFolders';
import type { CacheFolder } from '../../services/cacheFoldersApi';

/**
 * Cache Folder Manager Component
 * 
 * Manage cache folders for distributed caching
 */
const CacheFolderManager: React.FC = () => {
  const { data, isLoading } = useCacheFolders();
  const createMutation = useCreateCacheFolder();
  const updateMutation = useUpdateCacheFolder();
  const deleteMutation = useDeleteCacheFolder();
  const validateMutation = useValidatePath();

  const [showAddForm, setShowAddForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formData, setFormData] = useState({
    name: '',
    path: '',
    priority: 0,
    maxSizeGB: '',
  });

  const folders = data?.folders || [];
  const summary = data?.summary;

  const formatBytes = (bytes: number): string => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${(bytes / Math.pow(k, i)).toFixed(2)} ${sizes[i]}`;
  };

  const formatPercentage = (used: number, total: number | null): string => {
    if (!total) return 'N/A';
    return `${((used / total) * 100).toFixed(1)}%`;
  };

  const resetForm = () => {
    setFormData({ name: '', path: '', priority: 0, maxSizeGB: '' });
    setShowAddForm(false);
    setEditingId(null);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    const folderData = {
      name: formData.name,
      path: formData.path,
      priority: formData.priority,
      maxSizeBytes: formData.maxSizeGB ? parseInt(formData.maxSizeGB) * 1024 * 1024 * 1024 : null,
    };

    if (editingId) {
      updateMutation.mutate({ id: editingId, request: folderData }, {
        onSuccess: () => resetForm()
      });
    } else {
      createMutation.mutate(folderData, {
        onSuccess: () => resetForm()
      });
    }
  };

  const startEdit = (folder: CacheFolder) => {
    setEditingId(folder.id);
    setFormData({
      name: folder.name,
      path: folder.path,
      priority: folder.priority,
      maxSizeGB: folder.maxSizeBytes ? (folder.maxSizeBytes / (1024 * 1024 * 1024)).toString() : '',
    });
    setShowAddForm(false);
  };

  const handleValidate = () => {
    if (!formData.path) return;
    validateMutation.mutate(formData.path);
  };

  const handleDelete = (id: string, name: string) => {
    if (window.confirm(`Delete cache folder "${name}"? This will remove all cached files.`)) {
      deleteMutation.mutate(id);
    }
  };

  if (isLoading) {
    return <LoadingSpinner text="Loading cache folders..." />;
  }

  return (
    <div className="space-y-6">
      {/* Summary Stats */}
      {summary && folders.length > 0 && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div className="bg-blue-500/10 border border-blue-500/20 rounded-lg p-4">
            <div className="flex items-center space-x-3">
              <FolderOpen className="h-6 w-6 text-blue-500" />
              <div>
                <p className="text-xs text-slate-400">Total Folders</p>
                <p className="text-xl font-bold text-white">{summary.totalFolders}</p>
              </div>
            </div>
          </div>

          <div className="bg-green-500/10 border border-green-500/20 rounded-lg p-4">
            <div className="flex items-center space-x-3">
              <BarChart3 className="h-6 w-6 text-green-500" />
              <div>
                <p className="text-xs text-slate-400">Total Size</p>
                <p className="text-xl font-bold text-white">{formatBytes(summary.totalSize)}</p>
              </div>
            </div>
          </div>

          <div className="bg-purple-500/10 border border-purple-500/20 rounded-lg p-4">
            <div className="flex items-center space-x-3">
              <HardDrive className="h-6 w-6 text-purple-500" />
              <div>
                <p className="text-xs text-slate-400">Total Files</p>
                <p className="text-xl font-bold text-white">{summary.totalFiles.toLocaleString()}</p>
              </div>
            </div>
          </div>

          <div className="bg-orange-500/10 border border-orange-500/20 rounded-lg p-4">
            <div className="flex items-center space-x-3">
              <AlertTriangle className="h-6 w-6 text-orange-500" />
              <div>
                <p className="text-xs text-slate-400">Avg Priority</p>
                <p className="text-xl font-bold text-white">{summary.avgPriority.toFixed(1)}</p>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Add/Edit Form */}
      {(showAddForm || editingId) && (
        <div className="bg-slate-800 border border-slate-700 rounded-lg p-6">
          <h4 className="text-lg font-semibold text-white mb-4">
            {editingId ? 'Edit Cache Folder' : 'Add New Cache Folder'}
          </h4>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <SettingItem label="Folder Name" vertical>
                <input
                  type="text"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="e.g., SSD Cache 1"
                  required
                />
              </SettingItem>

              <SettingItem label="Priority" description="Higher priority folders are used first" vertical>
                <input
                  type="number"
                  value={formData.priority}
                  onChange={(e) => setFormData({ ...formData, priority: parseInt(e.target.value) || 0 })}
                  className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="0"
                  min="0"
                />
              </SettingItem>
            </div>

            <SettingItem label="Cache Path" vertical>
              <div className="flex space-x-2">
                <input
                  type="text"
                  value={formData.path}
                  onChange={(e) => setFormData({ ...formData, path: e.target.value })}
                  className="flex-1 px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="D:\Cache\ImageCache"
                  required
                />
                <Button 
                  type="button" 
                  variant="ghost" 
                  onClick={handleValidate}
                  disabled={validateMutation.isPending}
                >
                  {validateMutation.isPending ? 'Validating...' : 'Validate'}
                </Button>
              </div>
            </SettingItem>

            <SettingItem label="Max Size (GB)" description="Leave empty for unlimited" vertical>
              <input
                type="number"
                value={formData.maxSizeGB}
                onChange={(e) => setFormData({ ...formData, maxSizeGB: e.target.value })}
                className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Unlimited"
                min="1"
              />
            </SettingItem>

            <div className="flex justify-end space-x-3 pt-4">
              <Button type="button" variant="ghost" onClick={resetForm}>
                Cancel
              </Button>
              <Button type="submit" variant="primary">
                {editingId ? 'Update Folder' : 'Add Folder'}
              </Button>
            </div>
          </form>
        </div>
      )}

      {/* Folders List */}
      <div>
        <div className="flex justify-between items-center mb-4">
          <h4 className="text-lg font-semibold text-white">Cache Folders</h4>
          {!showAddForm && !editingId && (
            <Button
              variant="primary"
              onClick={() => setShowAddForm(true)}
              className="flex items-center space-x-2"
            >
              <Plus className="h-4 w-4" />
              <span>Add Folder</span>
            </Button>
          )}
        </div>

        {folders.length > 0 ? (
          <div className="space-y-3">
            {folders.map((folder) => (
              <div
                key={folder.id}
                className="bg-slate-800 border border-slate-700 rounded-lg p-4 hover:border-slate-600 transition-colors"
              >
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center space-x-3 mb-2">
                      <FolderOpen className="h-5 w-5 text-blue-500" />
                      <h5 className="text-base font-semibold text-white">{folder.name}</h5>
                      <span className="px-2 py-0.5 text-xs bg-blue-500/20 text-blue-400 rounded border border-blue-500/30">
                        Priority: {folder.priority}
                      </span>
                      {folder.isActive ? (
                        <span className="px-2 py-0.5 text-xs bg-green-500/20 text-green-400 rounded border border-green-500/30">
                          Active
                        </span>
                      ) : (
                        <span className="px-2 py-0.5 text-xs bg-red-500/20 text-red-400 rounded border border-red-500/30">
                          Inactive
                        </span>
                      )}
                    </div>

                    <p className="text-sm text-slate-400 mb-3 font-mono">{folder.path}</p>

                    <div className="grid grid-cols-3 gap-4 text-sm mb-3">
                      <div>
                        <span className="text-slate-500">Size: </span>
                        <span className="font-medium text-white">
                          {formatBytes(folder.currentSize)}
                          {folder.maxSizeBytes && (
                            <span className="text-slate-400 ml-1">
                              ({formatPercentage(folder.currentSize, folder.maxSizeBytes)})
                            </span>
                          )}
                        </span>
                      </div>
                      <div>
                        <span className="text-slate-500">Files: </span>
                        <span className="font-medium text-white">{folder.fileCount.toLocaleString()}</span>
                      </div>
                      <div>
                        <span className="text-slate-500">Max: </span>
                        <span className="font-medium text-white">
                          {folder.maxSizeBytes ? formatBytes(folder.maxSizeBytes) : 'Unlimited'}
                        </span>
                      </div>
                    </div>

                    {/* Disk Health Info */}
                    {folder.diskInfo?.available && (
                      <div className="mt-3 pt-3 border-t border-slate-700">
                        <div className="grid grid-cols-2 md:grid-cols-4 gap-3 text-xs">
                          <div>
                            <span className="text-slate-500">Drive: </span>
                            <span className="text-slate-300">{folder.diskInfo.driveType || 'Unknown'}</span>
                          </div>
                          <div>
                            <span className="text-slate-500">Format: </span>
                            <span className="text-slate-300">{folder.diskInfo.driveFormat || 'N/A'}</span>
                          </div>
                          <div>
                            <span className="text-slate-500">Disk Free: </span>
                            <span className="text-green-400">
                              {folder.diskInfo.freeSpace ? formatBytes(folder.diskInfo.freeSpace) : 'N/A'}
                            </span>
                          </div>
                          <div>
                            <span className="text-slate-500">Disk Used: </span>
                            <span className={folder.diskInfo.usedPercentage && folder.diskInfo.usedPercentage > 90 ? 'text-red-400' : 'text-slate-300'}>
                              {folder.diskInfo.usedPercentage?.toFixed(1) || '0'}%
                            </span>
                          </div>
                        </div>
                      </div>
                    )}
                  </div>

                  <div className="flex space-x-2 ml-4">
                    <button
                      onClick={() => startEdit(folder)}
                      className="p-2 text-slate-400 hover:text-blue-400 hover:bg-slate-700 rounded-lg transition-colors"
                      title="Edit"
                    >
                      <Edit2 className="h-4 w-4" />
                    </button>
                    <button
                      onClick={() => handleDelete(folder.id, folder.name)}
                      className="p-2 text-slate-400 hover:text-red-400 hover:bg-slate-700 rounded-lg transition-colors"
                      title="Delete"
                      disabled={deleteMutation.isPending}
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        ) : (
          <div className="text-center py-12 bg-slate-800 border border-slate-700 rounded-lg">
            <FolderOpen className="h-12 w-12 mx-auto mb-4 text-slate-600" />
            <p className="text-slate-400 mb-1">No cache folders configured yet</p>
            <p className="text-sm text-slate-500">Add your first cache folder to enable distributed caching</p>
          </div>
        )}
      </div>
    </div>
  );
};

export default CacheFolderManager;

