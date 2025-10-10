import { useState } from 'react';
import { Dialog } from '@headlessui/react';
import { FolderPlus, X } from 'lucide-react';
import Button from '../ui/Button';
import SettingItem from '../settings/SettingItem';
import toast from 'react-hot-toast';
import api from '../../services/api';

interface AddCollectionDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess?: () => void;
}

/**
 * Add Collection Dialog
 * 
 * Dialog for adding a single collection
 */
const AddCollectionDialog: React.FC<AddCollectionDialogProps> = ({
  isOpen,
  onClose,
  onSuccess,
}) => {
  const [formData, setFormData] = useState({
    libraryId: '000000000000000000000000', // Default library ID - TODO: Make dynamic
    name: '',
    path: '',
    type: 'Regular',
    description: '',
  });
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    try {
      setIsSubmitting(true);
      
      await api.post('/collections', formData);
      
      toast.success('Collection created successfully!');
      onSuccess?.();
      onClose();
      
      // Reset form
      setFormData({
        libraryId: '000000000000000000000000',
        name: '',
        path: '',
        type: 'Regular',
        description: '',
      });
    } catch (error: any) {
      const message = error.response?.data?.message || 'Failed to create collection';
      toast.error(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={isOpen} onClose={onClose} className="relative z-50">
      {/* Backdrop */}
      <div className="fixed inset-0 bg-black/60 backdrop-blur-sm" aria-hidden="true" />

      {/* Full-screen container */}
      <div className="fixed inset-0 flex items-center justify-center p-4">
        <Dialog.Panel className="bg-slate-800 rounded-lg border border-slate-700 w-full max-w-2xl">
          {/* Header */}
          <div className="flex items-center justify-between px-6 py-4 border-b border-slate-700">
            <div className="flex items-center space-x-3">
              <FolderPlus className="h-6 w-6 text-blue-500" />
              <Dialog.Title className="text-xl font-semibold text-white">
                Add Collection
              </Dialog.Title>
            </div>
            <button
              onClick={onClose}
              className="p-2 text-slate-400 hover:text-white hover:bg-slate-700 rounded-lg transition-colors"
            >
              <X className="h-5 w-5" />
            </button>
          </div>

          {/* Form */}
          <form onSubmit={handleSubmit} className="p-6 space-y-6">
            <SettingItem label="Collection Name" description="Unique name for this collection" vertical>
              <input
                type="text"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="e.g., My Photo Collection"
                required
              />
            </SettingItem>

            <SettingItem label="Folder Path" description="Path to the folder containing images" vertical>
              <input
                type="text"
                value={formData.path}
                onChange={(e) => setFormData({ ...formData, path: e.target.value })}
                className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500 font-mono text-sm"
                placeholder="D:\Photos\MyCollection"
                required
              />
            </SettingItem>

            <SettingItem label="Collection Type" vertical>
              <select
                value={formData.type}
                onChange={(e) => setFormData({ ...formData, type: e.target.value })}
                className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="Regular">Regular</option>
                <option value="Favorite">Favorite</option>
                <option value="Archive">Archive</option>
              </select>
            </SettingItem>

            <SettingItem label="Description (Optional)" vertical>
              <textarea
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Add a description..."
                rows={3}
              />
            </SettingItem>

            {/* Actions */}
            <div className="flex justify-end space-x-3 pt-4">
              <Button type="button" variant="ghost" onClick={onClose}>
                Cancel
              </Button>
              <Button type="submit" variant="primary" disabled={isSubmitting}>
                {isSubmitting ? 'Creating...' : 'Create Collection'}
              </Button>
            </div>
          </form>
        </Dialog.Panel>
      </div>
    </Dialog>
  );
};

export default AddCollectionDialog;

