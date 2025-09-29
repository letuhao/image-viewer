import React, { useState } from 'react';
import { Dialog } from '@headlessui/react';
import { XMarkIcon, FolderIcon, ArchiveBoxIcon } from '@heroicons/react/24/outline';
import { collectionsApi } from '../services/api';
import toast from 'react-hot-toast';

interface AddCollectionModalProps {
  onClose: () => void;
  onSuccess: (collection: any) => void;
}

const AddCollectionModal: React.FC<AddCollectionModalProps> = ({ onClose, onSuccess }) => {
  const [name, setName] = useState('');
  const [path, setPath] = useState('');
  const [type, setType] = useState<'folder' | 'zip'>('folder');
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!name.trim() || !path.trim()) {
      toast.error('Please fill in all fields');
      return;
    }

    try {
      setIsLoading(true);
      const response = await collectionsApi.create({ name: name.trim(), path: path.trim(), type });
      
      // The API returns the collection ID, so we need to fetch the full collection
      const collectionResponse = await collectionsApi.getById(response.data.id);
      onSuccess(collectionResponse.data);
      
      toast.success('Collection added successfully');
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to add collection');
    } finally {
      setIsLoading(false);
    }
  };

  const handlePathChange = (newPath: string) => {
    setPath(newPath);
    
    // Auto-detect type based on file extension
    const lowerPath = newPath.toLowerCase();
    if (lowerPath.endsWith('.zip') || lowerPath.endsWith('.cbz') || lowerPath.endsWith('.cbr')) {
      setType('zip');
    } else {
      setType('folder');
    }
    
    // Auto-generate name from path if name is empty
    if (!name.trim()) {
      const pathParts = newPath.split(/[/\\]/);
      const lastPart = pathParts[pathParts.length - 1];
      setName(lastPart.replace(/\.[^/.]+$/, '')); // Remove file extension
    }
  };

  return (
    <Dialog open={true} onClose={onClose} className="relative z-50">
      <div className="fixed inset-0 bg-black/50" aria-hidden="true" />
      
      <div className="fixed inset-0 flex items-center justify-center p-4">
        <Dialog.Panel className="bg-dark-800 rounded-lg shadow-xl max-w-md w-full p-6">
          <div className="flex items-center justify-between mb-6">
            <Dialog.Title className="text-xl font-semibold text-white">
              Add New Collection
            </Dialog.Title>
            <button
              onClick={onClose}
              className="text-dark-400 hover:text-white transition-colors"
            >
              <XMarkIcon className="h-6 w-6" />
            </button>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-dark-300 mb-2">
                Collection Name
              </label>
              <input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="input"
                placeholder="Enter collection name"
                required
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-dark-300 mb-2">
                Path
              </label>
              <input
                type="text"
                value={path}
                onChange={(e) => handlePathChange(e.target.value)}
                className="input"
                placeholder="Enter folder path or ZIP file path"
                required
              />
              <p className="text-xs text-dark-500 mt-1">
                Enter the full path to a folder or ZIP file containing images
              </p>
            </div>

            <div>
              <label className="block text-sm font-medium text-dark-300 mb-2">
                Type
              </label>
              <div className="grid grid-cols-2 gap-3">
                <button
                  type="button"
                  onClick={() => setType('folder')}
                  className={`flex items-center space-x-2 p-3 rounded-lg border transition-colors ${
                    type === 'folder'
                      ? 'border-primary-500 bg-primary-500/10 text-primary-400'
                      : 'border-dark-600 text-dark-300 hover:border-dark-500'
                  }`}
                >
                  <FolderIcon className="h-5 w-5" />
                  <span>Folder</span>
                </button>
                
                <button
                  type="button"
                  onClick={() => setType('zip')}
                  className={`flex items-center space-x-2 p-3 rounded-lg border transition-colors ${
                    type === 'zip'
                      ? 'border-primary-500 bg-primary-500/10 text-primary-400'
                      : 'border-dark-600 text-dark-300 hover:border-dark-500'
                  }`}
                >
                  <ArchiveBoxIcon className="h-5 w-5" />
                  <span>ZIP File</span>
                </button>
              </div>
            </div>

            <div className="flex items-center justify-end space-x-3 pt-4">
              <button
                type="button"
                onClick={onClose}
                className="btn btn-ghost"
                disabled={isLoading}
              >
                Cancel
              </button>
              <button
                type="submit"
                className="btn btn-primary"
                disabled={isLoading}
              >
                {isLoading ? 'Adding...' : 'Add Collection'}
              </button>
            </div>
          </form>
        </Dialog.Panel>
      </div>
    </Dialog>
  );
};

export default AddCollectionModal;
