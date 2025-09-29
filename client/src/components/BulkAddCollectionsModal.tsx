import React, { useState } from 'react';
import { Dialog } from '@headlessui/react';
import { XMarkIcon, FolderIcon, ArchiveBoxIcon, ExclamationTriangleIcon } from '@heroicons/react/24/outline';
import { bulkApi, backgroundApi } from '../services/api';
import toast from 'react-hot-toast';

interface BulkAddCollectionsModalProps {
  onClose: () => void;
  onSuccess: () => void;
  onJobStarted?: (jobId: string) => void;
}

interface PreviewCollection {
  name: string;
  path: string;
  type: 'folder' | 'zip';
  imageCount: number | string;
  alreadyExists: boolean;
  size: number | string;
  metadata?: {
    total_images: number;
    total_size: number;
    image_formats: string[];
    has_subfolders: boolean;
    average_image_size: number;
    collection_stats: any;
    auto_tags?: string[];
  };
}

const BulkAddCollectionsModal: React.FC<BulkAddCollectionsModalProps> = ({ onClose, onSuccess, onJobStarted }) => {
  const [parentPath, setParentPath] = useState('');
  const [collectionPrefix, setCollectionPrefix] = useState('');
  const [includeSubfolders, setIncludeSubfolders] = useState(false);
  const [autoAdd, setAutoAdd] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [previewData, setPreviewData] = useState<PreviewCollection[] | null>(null);
  const [selectedCollections, setSelectedCollections] = useState<Set<number>>(new Set());

  const handlePreview = async () => {
    if (!parentPath.trim()) {
      toast.error('Please enter a parent directory path');
      return;
    }

    try {
      setIsLoading(true);
      const response = await bulkApi.preview(parentPath.trim(), collectionPrefix.trim(), includeSubfolders);
      setPreviewData(response.data.potentialCollections);
      
      // Select all collections by default
      const allIndices = response.data.potentialCollections.map((_: any, index: number) => index);
      setSelectedCollections(new Set(allIndices));
      
      toast.success(`Found ${response.data.potentialCollections.length} potential collections`);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to preview collections');
    } finally {
      setIsLoading(false);
    }
  };

  const handleBulkAdd = async () => {
    if (autoAdd) {
      // Auto add mode - start background job
      try {
        setIsLoading(true);
        const response = await backgroundApi.startBulkAdd({
          parentPath: parentPath.trim(),
          collectionPrefix: collectionPrefix.trim(),
          includeSubfolders
        });
        
        toast.success('Bulk add job started in background. You can monitor progress in the job monitor.');
        if (onJobStarted) {
          onJobStarted(response.jobId);
        }
        onClose();
      } catch (error) {
        toast.error(error instanceof Error ? error.message : 'Failed to start bulk add job');
      } finally {
        setIsLoading(false);
      }
      return;
    }

    if (!previewData || selectedCollections.size === 0) {
      toast.error('Please select collections to add');
      return;
    }

    try {
      setIsLoading(true);
      
      // Add collections using the bulk API
      const response = await bulkApi.addCollections(parentPath.trim(), collectionPrefix.trim(), includeSubfolders, autoAdd);
      const results = response.data.collections;
      
      toast.success(`Successfully added ${results.length} collections`);
      onSuccess();
      onClose();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to add collections');
    } finally {
      setIsLoading(false);
    }
  };

  const toggleSelection = (index: number) => {
    const newSelected = new Set(selectedCollections);
    if (newSelected.has(index)) {
      newSelected.delete(index);
    } else {
      newSelected.add(index);
    }
    setSelectedCollections(newSelected);
  };

  const selectAll = () => {
    if (previewData) {
      const allIndices = previewData.map((_, index) => index);
      setSelectedCollections(new Set(allIndices));
    }
  };

  const selectNone = () => {
    setSelectedCollections(new Set());
  };

  const selectNew = () => {
    if (previewData) {
      const newIndices = previewData
        .map((col, index) => col.alreadyExists ? -1 : index)
        .filter(index => index !== -1);
      setSelectedCollections(new Set(newIndices));
    }
  };

  const formatSize = (size: number | string) => {
    if (typeof size === 'string') return size;
    if (size < 1024) return `${size} B`;
    if (size < 1024 * 1024) return `${(size / 1024).toFixed(1)} KB`;
    if (size < 1024 * 1024 * 1024) return `${(size / (1024 * 1024)).toFixed(1)} MB`;
    return `${(size / (1024 * 1024 * 1024)).toFixed(1)} GB`;
  };

  return (
    <Dialog open={true} onClose={onClose} className="relative z-50">
      <div className="fixed inset-0 bg-black/50" aria-hidden="true" />
      
      <div className="fixed inset-0 flex items-center justify-center p-4">
        <Dialog.Panel className="bg-dark-800 rounded-lg shadow-xl max-w-4xl w-full max-h-[90vh] overflow-hidden">
          <div className="flex items-center justify-between p-6 border-b border-dark-700">
            <Dialog.Title className="text-xl font-semibold text-white">
              Bulk Add Collections
            </Dialog.Title>
            <button
              onClick={onClose}
              className="text-dark-400 hover:text-white transition-colors"
            >
              <XMarkIcon className="h-6 w-6" />
            </button>
          </div>

          <div className="p-6 overflow-y-auto max-h-[calc(90vh-8rem)]">
            {/* Input Section */}
            <div className="space-y-4 mb-6">
              <div>
                <label className="block text-sm font-medium text-dark-300 mb-2">
                  Parent Directory Path
                </label>
                <input
                  type="text"
                  value={parentPath}
                  onChange={(e) => setParentPath(e.target.value)}
                  className="input"
                  placeholder="Enter path to directory containing collections (e.g., C:\Manga\Collections)"
                  disabled={isLoading}
                />
                <p className="text-xs text-dark-500 mt-1">
                  Each ZIP file or folder in this directory will be treated as a separate collection
                </p>
              </div>

              <div>
                <label className="block text-sm font-medium text-dark-300 mb-2">
                  Collection Name Prefix (Optional)
                </label>
                <input
                  type="text"
                  value={collectionPrefix}
                  onChange={(e) => setCollectionPrefix(e.target.value)}
                  className="input"
                  placeholder="e.g., 'Manga - ' or 'Comics - '"
                  disabled={isLoading}
                />
                <p className="text-xs text-dark-500 mt-1">
                  This will be added to the beginning of each collection name
                </p>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="includeSubfolders"
                    checked={includeSubfolders}
                    onChange={(e) => setIncludeSubfolders(e.target.checked)}
                    disabled={isLoading}
                    className="w-4 h-4 text-primary-600 bg-dark-700 border-dark-600 rounded focus:ring-primary-500"
                  />
                  <label htmlFor="includeSubfolders" className="text-sm text-dark-300">
                    Include Subfolders
                  </label>
                </div>
                
                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="autoAdd"
                    checked={autoAdd}
                    onChange={(e) => setAutoAdd(e.target.checked)}
                    disabled={isLoading}
                    className="w-4 h-4 text-primary-600 bg-dark-700 border-dark-600 rounded focus:ring-primary-500"
                  />
                  <label htmlFor="autoAdd" className="text-sm text-dark-300">
                    Auto Add All
                  </label>
                </div>
              </div>

              <div className="text-xs text-dark-500 space-y-1">
                <p><strong>Include Subfolders:</strong> Scan nested folders for additional collections</p>
                <p><strong>Auto Add All:</strong> Automatically add all found collections without preview</p>
              </div>

              {autoAdd ? (
                <button
                  onClick={handleBulkAdd}
                  disabled={isLoading || !parentPath.trim()}
                  className="btn btn-primary"
                >
                  {isLoading ? 'Adding Collections...' : 'Add All Collections'}
                </button>
              ) : (
                <button
                  onClick={handlePreview}
                  disabled={isLoading || !parentPath.trim()}
                  className="btn btn-primary"
                >
                  {isLoading ? 'Scanning...' : 'Preview Collections'}
                </button>
              )}
            </div>

            {/* Preview Results */}
            {previewData && (
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <h3 className="text-lg font-medium text-white">
                    Found Collections ({previewData.length})
                  </h3>
                  <div className="flex items-center space-x-2">
                    <button
                      onClick={selectAll}
                      className="btn btn-ghost text-sm"
                    >
                      Select All
                    </button>
                    <button
                      onClick={selectNew}
                      className="btn btn-ghost text-sm"
                    >
                      Select New Only
                    </button>
                    <button
                      onClick={selectNone}
                      className="btn btn-ghost text-sm"
                    >
                      Select None
                    </button>
                  </div>
                </div>

                <div className="space-y-2 max-h-96 overflow-y-auto">
                  {previewData.map((collection, index) => {
                    const Icon = collection.type === 'zip' ? ArchiveBoxIcon : FolderIcon;
                    const isSelected = selectedCollections.has(index);
                    
                    return (
                      <div
                        key={index}
                        className={`p-4 rounded-lg border transition-colors cursor-pointer ${
                          collection.alreadyExists
                            ? 'bg-dark-700 border-dark-600 opacity-60'
                            : isSelected
                            ? 'bg-primary-600 border-primary-500'
                            : 'bg-dark-700 border-dark-600 hover:border-dark-500'
                        }`}
                        onClick={() => !collection.alreadyExists && toggleSelection(index)}
                      >
                        <div className="flex items-center space-x-3">
                          <input
                            type="checkbox"
                            checked={isSelected}
                            onChange={() => !collection.alreadyExists && toggleSelection(index)}
                            disabled={collection.alreadyExists}
                            className="w-4 h-4 text-primary-600 bg-dark-700 border-dark-600 rounded focus:ring-primary-500 disabled:opacity-50"
                          />
                          
                          <Icon className="h-6 w-6 text-primary-500 flex-shrink-0" />
                          
                          <div className="flex-1 min-w-0">
                            <div className="flex items-center space-x-2">
                              <h4 className="text-white font-medium truncate">
                                {collection.name}
                              </h4>
                              {collection.alreadyExists && (
                                <span className="inline-flex items-center px-2 py-1 rounded-full text-xs bg-yellow-600 text-yellow-100">
                                  Already Exists
                                </span>
                              )}
                            </div>
                            <p className="text-sm text-dark-400 truncate">
                              {collection.path}
                            </p>
                            <div className="flex items-center space-x-4 mt-1 text-xs text-dark-500">
                              <span>{collection.type}</span>
                              <span>•</span>
                              <span>{collection.imageCount} images</span>
                              <span>•</span>
                              <span>{formatSize(collection.size)}</span>
                              {collection.metadata && (
                                <>
                                  {collection.metadata.has_subfolders && (
                                    <>
                                      <span>•</span>
                                      <span>Has subfolders</span>
                                    </>
                                  )}
                                  {collection.metadata.image_formats && collection.metadata.image_formats.length > 0 && (
                                    <>
                                      <span>•</span>
                                      <span>{collection.metadata.image_formats.join(', ')}</span>
                                    </>
                                  )}
                                </>
                              )}
                            </div>
                            
                            {/* Auto-generated tags */}
                            {collection.metadata?.auto_tags && collection.metadata.auto_tags.length > 0 && (
                              <div className="mt-2">
                                <div className="text-xs text-gray-400 mb-1">Auto-detected tags:</div>
                                <div className="flex flex-wrap gap-1">
                                  {collection.metadata.auto_tags.map((tag, tagIndex) => (
                                    <span
                                      key={tagIndex}
                                      className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200"
                                    >
                                      {tag}
                                    </span>
                                  ))}
                                </div>
                              </div>
                            )}
                          </div>
                        </div>
                      </div>
                    );
                  })}
                </div>

                {previewData.length === 0 && (
                  <div className="text-center py-8">
                    <ExclamationTriangleIcon className="h-12 w-12 text-dark-500 mx-auto mb-2" />
                    <p className="text-dark-400">No valid collections found in this directory</p>
                    <p className="text-dark-500 text-sm mt-1">
                      Make sure the directory contains ZIP files or folders with images
                    </p>
                  </div>
                )}
              </div>
            )}
          </div>

          {/* Footer */}
          <div className="flex items-center justify-between p-6 border-t border-dark-700">
            <div className="text-sm text-dark-400">
              {previewData && (
                <>
                  {selectedCollections.size} of {previewData.length} collections selected
                  {previewData.filter(col => col.alreadyExists).length > 0 && (
                    <span className="ml-2">
                      ({previewData.filter(col => col.alreadyExists).length} already exist)
                    </span>
                  )}
                </>
              )}
            </div>
            
            <div className="flex items-center space-x-3">
              <button
                onClick={onClose}
                className="btn btn-ghost"
                disabled={isLoading}
              >
                Cancel
              </button>
              <button
                onClick={handleBulkAdd}
                className="btn btn-primary"
                disabled={isLoading || !previewData || selectedCollections.size === 0}
              >
                {isLoading ? 'Adding...' : `Add ${selectedCollections.size} Collections`}
              </button>
            </div>
          </div>
        </Dialog.Panel>
      </div>
    </Dialog>
  );
};

export default BulkAddCollectionsModal;
