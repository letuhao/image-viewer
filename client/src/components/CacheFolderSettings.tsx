import React, { useState, useEffect } from 'react';
import { Dialog, Transition } from '@headlessui/react';
import {
  FolderIcon,
  PlusIcon,
  TrashIcon,
  PencilIcon,
  ComputerDesktopIcon,
  ChartBarIcon,
  ExclamationTriangleIcon,
} from '@heroicons/react/24/outline';
import cacheFoldersApi from '../services/cacheFoldersApi';
import toast from 'react-hot-toast';

interface CacheFolder {
  id?: string;
  _id?: string; // MongoDB compatibility
  name: string;
  path: string;
  priority: number;
  max_size: number | null;
  current_size: number;
  file_count: number;
  is_active: boolean;
  created_at: string;
  updated_at: string;
}

interface CacheFolderSettingsProps {
  isOpen: boolean;
  onClose: () => void;
}

interface CacheFolderStats {
  summary: {
    total_folders: number;
    total_size: number;
    total_files: number;
    avg_priority: number;
  };
  folders: CacheFolder[];
}

const CacheFolderSettings: React.FC<CacheFolderSettingsProps> = ({ isOpen, onClose }) => {
  const [stats, setStats] = useState<CacheFolderStats | null>(null);
  const [loading, setLoading] = useState(false);
  const [showAddForm, setShowAddForm] = useState(false);
  const [editingFolder, setEditingFolder] = useState<CacheFolder | null>(null);
  const [formData, setFormData] = useState({
    name: '',
    path: '',
    priority: 0,
    maxSize: '',
  });

  const loadStats = async () => {
    try {
      setLoading(true);
      const response = await cacheFoldersApi.getStats();
      setStats(response.data);
    } catch (error) {
      toast.error('Failed to load cache folder statistics');
      console.error('Error loading cache stats:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (isOpen) {
      loadStats();
    }
  }, [isOpen]);

  const handleAddFolder = async (e: React.FormEvent) => {
    e.preventDefault();
    
    try {
      setLoading(true);
      await cacheFoldersApi.addFolder({
        name: formData.name,
        path: formData.path,
        priority: formData.priority,
        maxSize: formData.maxSize ? parseInt(formData.maxSize) * 1024 * 1024 * 1024 : null, // Convert GB to bytes
      });
      
      toast.success('Cache folder added successfully');
      setShowAddForm(false);
      setFormData({ name: '', path: '', priority: 0, maxSize: '' });
      await loadStats();
    } catch (error) {
      toast.error('Failed to add cache folder');
      console.error('Error adding cache folder:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleEditFolder = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!editingFolder) return;
    
    // Use _id if id is not available (MongoDB compatibility)
    const folderId = editingFolder.id || (editingFolder as any)._id;
    
    if (!folderId) {
      toast.error('Invalid cache folder ID');
      return;
    }
    
    try {
      setLoading(true);
      await cacheFoldersApi.updateFolder(folderId, {
        name: formData.name,
        path: formData.path,
        priority: formData.priority,
        maxSize: formData.maxSize ? parseInt(formData.maxSize) * 1024 * 1024 * 1024 : null,
      });
      
      toast.success('Cache folder updated successfully');
      setEditingFolder(null);
      setFormData({ name: '', path: '', priority: 0, maxSize: '' });
      await loadStats();
    } catch (error) {
      toast.error('Failed to update cache folder');
      console.error('Error updating cache folder:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteFolder = async (folder: CacheFolder) => {
    if (!window.confirm(`Are you sure you want to delete the cache folder "${folder.name}"? This will also remove all cached files in this folder.`)) {
      return;
    }
    
    // Use _id if id is not available (MongoDB compatibility)
    const folderId = folder.id || (folder as any)._id;
    
    if (!folderId) {
      toast.error('Invalid cache folder ID');
      return;
    }
    
    try {
      setLoading(true);
      await cacheFoldersApi.deleteFolder(folderId);
      toast.success('Cache folder deleted successfully');
      await loadStats();
    } catch (error) {
      toast.error('Failed to delete cache folder');
      console.error('Error deleting cache folder:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleValidatePath = async (path: string) => {
    if (!path) return;
    
    try {
      const response = await cacheFoldersApi.validatePath(path);
      if (response.data.valid) {
        toast.success('Path is valid and writable');
      } else {
        toast.error(`Path validation failed: ${response.data.error}`);
      }
    } catch (error) {
      toast.error('Failed to validate path');
    }
  };

  const formatBytes = (bytes: number) => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const formatPercentage = (used: number, total: number | null) => {
    if (!total) return 'N/A';
    return ((used / total) * 100).toFixed(1) + '%';
  };

  const resetForm = () => {
    setFormData({ name: '', path: '', priority: 0, maxSize: '' });
    setShowAddForm(false);
    setEditingFolder(null);
  };

  return (
    <Transition appear show={isOpen} as={React.Fragment}>
      <Dialog as="div" className="relative z-50" onClose={onClose}>
        <Transition.Child
          as={React.Fragment}
          enter="ease-out duration-300"
          enterFrom="opacity-0"
          enterTo="opacity-100"
          leave="ease-in duration-200"
          leaveFrom="opacity-100"
          leaveTo="opacity-0"
        >
          <div className="fixed inset-0 bg-black bg-opacity-25" />
        </Transition.Child>

        <div className="fixed inset-0 overflow-y-auto">
          <div className="flex min-h-full items-center justify-center p-4 text-center">
            <Transition.Child
              as={React.Fragment}
              enter="ease-out duration-300"
              enterFrom="opacity-0 scale-95"
              enterTo="opacity-100 scale-100"
              leave="ease-in duration-200"
              leaveFrom="opacity-100 scale-100"
              leaveTo="opacity-0 scale-95"
            >
              <Dialog.Panel className="w-full max-w-4xl transform overflow-hidden rounded-2xl bg-white dark:bg-dark-800 p-6 text-left align-middle shadow-xl transition-all">

                {/* Summary Stats */}
                {stats && (
                  <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
                    <div className="bg-blue-50 dark:bg-blue-900/20 p-4 rounded-lg">
                      <div className="flex items-center">
                        <FolderIcon className="h-8 w-8 text-blue-600 dark:text-blue-400" />
                        <div className="ml-3">
                          <p className="text-sm font-medium text-blue-600 dark:text-blue-400">Total Folders</p>
                          <p className="text-2xl font-bold text-blue-900 dark:text-blue-100">{stats.summary.total_folders}</p>
                        </div>
                      </div>
                    </div>
                    
                    <div className="bg-green-50 dark:bg-green-900/20 p-4 rounded-lg">
                      <div className="flex items-center">
                        <ChartBarIcon className="h-8 w-8 text-green-600 dark:text-green-400" />
                        <div className="ml-3">
                          <p className="text-sm font-medium text-green-600 dark:text-green-400">Total Size</p>
                          <p className="text-2xl font-bold text-green-900 dark:text-green-100">{formatBytes(stats.summary.total_size)}</p>
                        </div>
                      </div>
                    </div>
                    
                    <div className="bg-purple-50 dark:bg-purple-900/20 p-4 rounded-lg">
                      <div className="flex items-center">
                        <ComputerDesktopIcon className="h-8 w-8 text-purple-600 dark:text-purple-400" />
                        <div className="ml-3">
                          <p className="text-sm font-medium text-purple-600 dark:text-purple-400">Total Files</p>
                          <p className="text-2xl font-bold text-purple-900 dark:text-purple-100">{stats.summary.total_files}</p>
                        </div>
                      </div>
                    </div>
                    
                    <div className="bg-orange-50 dark:bg-orange-900/20 p-4 rounded-lg">
                      <div className="flex items-center">
                        <ExclamationTriangleIcon className="h-8 w-8 text-orange-600 dark:text-orange-400" />
                        <div className="ml-3">
                          <p className="text-sm font-medium text-orange-600 dark:text-orange-400">Avg Priority</p>
                          <p className="text-2xl font-bold text-orange-900 dark:text-orange-100">{stats.summary.avg_priority.toFixed(1)}</p>
                        </div>
                      </div>
                    </div>
                  </div>
                )}

                {/* Add/Edit Form */}
                {(showAddForm || editingFolder) && (
                  <div className="bg-gray-50 dark:bg-dark-700 p-4 rounded-lg mb-6">
                    <h4 className="text-lg font-medium text-gray-900 dark:text-white mb-4">
                      {editingFolder ? 'Edit Cache Folder' : 'Add New Cache Folder'}
                    </h4>
                    
                    <form onSubmit={editingFolder ? handleEditFolder : handleAddFolder} className="space-y-4">
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <div>
                          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                            Folder Name
                          </label>
                          <input
                            type="text"
                            value={formData.name}
                            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                            className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-dark-600 dark:text-white"
                            placeholder="e.g., SSD Cache 1"
                            required
                          />
                        </div>
                        
                        <div>
                          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                            Priority
                          </label>
                          <input
                            type="number"
                            value={formData.priority}
                            onChange={(e) => setFormData({ ...formData, priority: parseInt(e.target.value) || 0 })}
                            className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-dark-600 dark:text-white"
                            placeholder="0"
                            min="0"
                          />
                        </div>
                      </div>
                      
                      <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                          Cache Path
                        </label>
                        <div className="flex">
                          <input
                            type="text"
                            value={formData.path}
                            onChange={(e) => setFormData({ ...formData, path: e.target.value })}
                            className="flex-1 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-l-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-dark-600 dark:text-white"
                            placeholder="e.g., D:\Cache\ImageCache"
                            required
                          />
                          <button
                            type="button"
                            onClick={() => handleValidatePath(formData.path)}
                            className="px-4 py-2 bg-gray-500 text-white rounded-r-md hover:bg-gray-600 focus:outline-none focus:ring-2 focus:ring-gray-500"
                          >
                            Validate
                          </button>
                        </div>
                      </div>
                      
                      <div>
                        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                          Max Size (GB) - Optional
                        </label>
                        <input
                          type="number"
                          value={formData.maxSize}
                          onChange={(e) => setFormData({ ...formData, maxSize: e.target.value })}
                          className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-dark-600 dark:text-white"
                          placeholder="Leave empty for unlimited"
                          min="1"
                        />
                      </div>
                      
                      <div className="flex justify-end space-x-3">
                        <button
                          type="button"
                          onClick={resetForm}
                          className="px-4 py-2 text-gray-700 dark:text-gray-300 bg-gray-200 dark:bg-gray-600 rounded-md hover:bg-gray-300 dark:hover:bg-gray-500 focus:outline-none focus:ring-2 focus:ring-gray-500"
                        >
                          Cancel
                        </button>
                        <button
                          type="submit"
                          disabled={loading}
                          className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50"
                        >
                          {loading ? 'Saving...' : (editingFolder ? 'Update' : 'Add Folder')}
                        </button>
                      </div>
                    </form>
                  </div>
                )}

                {/* Cache Folders List */}
                <div className="space-y-4">
                  <div className="flex justify-between items-center">
                    <h4 className="text-lg font-medium text-gray-900 dark:text-white">Cache Folders</h4>
                    <button
                      onClick={() => setShowAddForm(true)}
                      className="flex items-center px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
                    >
                      <PlusIcon className="h-4 w-4 mr-2" />
                      Add Folder
                    </button>
                  </div>

                  {stats?.folders && stats.folders.length > 0 ? (
                    <div className="space-y-3">
                      {stats.folders.map((folder) => (
                        <div key={folder.id} className="bg-white dark:bg-dark-700 border border-gray-200 dark:border-gray-600 rounded-lg p-4">
                          <div className="flex items-center justify-between">
                            <div className="flex-1">
                              <div className="flex items-center mb-2">
                                <FolderIcon className="h-5 w-5 text-gray-400 mr-2" />
                                <h5 className="text-lg font-medium text-gray-900 dark:text-white">{folder.name}</h5>
                                <span className="ml-2 px-2 py-1 text-xs bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 rounded">
                                  Priority: {folder.priority}
                                </span>
                              </div>
                              
                              <p className="text-sm text-gray-600 dark:text-gray-400 mb-2">{folder.path}</p>
                              
                              <div className="grid grid-cols-3 gap-4 text-sm">
                                <div>
                                  <span className="text-gray-500 dark:text-gray-400">Size: </span>
                                  <span className="font-medium text-gray-900 dark:text-white">
                                    {formatBytes(folder.current_size)}
                                    {folder.max_size && (
                                      <span className="text-gray-500 dark:text-gray-400">
                                        {' '}({formatPercentage(folder.current_size, folder.max_size)})
                                      </span>
                                    )}
                                  </span>
                                </div>
                                <div>
                                  <span className="text-gray-500 dark:text-gray-400">Files: </span>
                                  <span className="font-medium text-gray-900 dark:text-white">{folder.file_count}</span>
                                </div>
                                <div>
                                  <span className="text-gray-500 dark:text-gray-400">Status: </span>
                                  <span className={`font-medium ${folder.is_active ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'}`}>
                                    {folder.is_active ? 'Active' : 'Inactive'}
                                  </span>
                                </div>
                              </div>
                            </div>
                            
                            <div className="flex space-x-2 ml-4">
                              <button
                                onClick={() => {
                                  setEditingFolder(folder);
                                  setFormData({
                                    name: folder.name,
                                    path: folder.path,
                                    priority: folder.priority,
                                    maxSize: folder.max_size ? (folder.max_size / (1024 * 1024 * 1024)).toString() : '',
                                  });
                                  setShowAddForm(false);
                                }}
                                className="p-2 text-gray-400 hover:text-blue-600 dark:hover:text-blue-400 focus:outline-none"
                              >
                                <PencilIcon className="h-4 w-4" />
                              </button>
                              <button
                                onClick={() => handleDeleteFolder(folder)}
                                className="p-2 text-gray-400 hover:text-red-600 dark:hover:text-red-400 focus:outline-none"
                              >
                                <TrashIcon className="h-4 w-4" />
                              </button>
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div className="text-center py-8 text-gray-500 dark:text-gray-400">
                      <FolderIcon className="h-12 w-12 mx-auto mb-4 opacity-50" />
                      <p>No cache folders configured yet.</p>
                      <p className="text-sm">Add your first cache folder to get started with distributed caching.</p>
                    </div>
                  )}
                </div>

              </Dialog.Panel>
            </Transition.Child>
          </div>
        </div>
      </Dialog>
    </Transition>
  );
};

export default CacheFolderSettings;
