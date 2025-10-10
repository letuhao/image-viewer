import { useState } from 'react';
import { Dialog } from '@headlessui/react';
import { FolderPlus, X, Zap } from 'lucide-react';
import Button from '../ui/Button';
import SettingItem from '../settings/SettingItem';
import Toggle from '../ui/Toggle';
import toast from 'react-hot-toast';
import api from '../../services/api';

interface BulkAddCollectionsDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess?: () => void;
}

/**
 * Bulk Add Collections Dialog
 * 
 * Dialog for bulk adding collections from a parent directory
 */
const BulkAddCollectionsDialog: React.FC<BulkAddCollectionsDialogProps> = ({
  isOpen,
  onClose,
  onSuccess,
}) => {
  const [formData, setFormData] = useState({
    parentPath: '',
    collectionPrefix: '',
    includeSubfolders: true,
    autoAdd: true,
    overwriteExisting: false,
    createdAfter: '',
    createdBefore: '',
    modifiedAfter: '',
    modifiedBefore: '',
  });
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    try {
      setIsSubmitting(true);
      
      // Prepare request data - convert empty strings to null for dates
      const requestData = {
        parentPath: formData.parentPath,
        collectionPrefix: formData.collectionPrefix || null,
        includeSubfolders: formData.includeSubfolders,
        autoAdd: formData.autoAdd,
        overwriteExisting: formData.overwriteExisting,
        createdAfter: formData.createdAfter ? new Date(formData.createdAfter).toISOString() : null,
        createdBefore: formData.createdBefore ? new Date(formData.createdBefore).toISOString() : null,
        modifiedAfter: formData.modifiedAfter ? new Date(formData.modifiedAfter).toISOString() : null,
        modifiedBefore: formData.modifiedBefore ? new Date(formData.modifiedBefore).toISOString() : null,
      };
      
      const response = await api.post('/bulk/collections', requestData);
      
      toast.success('Bulk operation started! Check Background Jobs for progress.');
      onSuccess?.();
      onClose();
      
      // Reset form
      setFormData({
        parentPath: '',
        collectionPrefix: '',
        includeSubfolders: true,
        autoAdd: true,
        overwriteExisting: false,
        createdAfter: '',
        createdBefore: '',
        modifiedAfter: '',
        modifiedBefore: '',
      });
    } catch (error: any) {
      const message = error.response?.data?.error || 'Failed to start bulk operation';
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
        <Dialog.Panel className="bg-slate-800 rounded-lg border border-slate-700 w-full max-w-2xl max-h-[90vh] flex flex-col">
          {/* Header */}
          <div className="flex items-center justify-between px-6 py-4 border-b border-slate-700">
            <div className="flex items-center space-x-3">
              <Zap className="h-6 w-6 text-yellow-500" />
              <Dialog.Title className="text-xl font-semibold text-white">
                Bulk Add Collections
              </Dialog.Title>
            </div>
            <button
              onClick={onClose}
              className="p-2 text-slate-400 hover:text-white hover:bg-slate-700 rounded-lg transition-colors"
            >
              <X className="h-5 w-5" />
            </button>
          </div>

          {/* Form - Scrollable Content */}
          <form onSubmit={handleSubmit} className="flex flex-col flex-1 overflow-hidden">
            <div className="flex-1 overflow-y-auto px-6 py-6 space-y-6">
            <SettingItem label="Parent Folder Path" description="Parent directory containing collection folders" vertical>
              <input
                type="text"
                value={formData.parentPath}
                onChange={(e) => setFormData({ ...formData, parentPath: e.target.value })}
                className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500 font-mono text-sm"
                placeholder="D:\Photos"
                required
              />
            </SettingItem>

            <SettingItem label="Collection Prefix (Optional)" description="Prefix to add to collection names" vertical>
              <input
                type="text"
                value={formData.collectionPrefix}
                onChange={(e) => setFormData({ ...formData, collectionPrefix: e.target.value })}
                className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="e.g., 2024-"
              />
            </SettingItem>

            {/* Date Range Filters */}
            <div className="bg-slate-900/50 rounded-lg p-4 border border-slate-700 space-y-4">
              <h4 className="text-sm font-semibold text-white mb-3">Date Range Filters (Optional)</h4>
              
              <div className="grid grid-cols-2 gap-4">
                <SettingItem label="Created After" vertical>
                  <input
                    type="datetime-local"
                    value={formData.createdAfter}
                    onChange={(e) => setFormData({ ...formData, createdAfter: e.target.value })}
                    className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                  />
                </SettingItem>

                <SettingItem label="Created Before" vertical>
                  <input
                    type="datetime-local"
                    value={formData.createdBefore}
                    onChange={(e) => setFormData({ ...formData, createdBefore: e.target.value })}
                    className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                  />
                </SettingItem>

                <SettingItem label="Modified After" vertical>
                  <input
                    type="datetime-local"
                    value={formData.modifiedAfter}
                    onChange={(e) => setFormData({ ...formData, modifiedAfter: e.target.value })}
                    className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                  />
                </SettingItem>

                <SettingItem label="Modified Before" vertical>
                  <input
                    type="datetime-local"
                    value={formData.modifiedBefore}
                    onChange={(e) => setFormData({ ...formData, modifiedBefore: e.target.value })}
                    className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                  />
                </SettingItem>
              </div>

              <p className="text-xs text-slate-500">
                Filter folders/files by creation or modification date. Leave empty to include all.
              </p>
            </div>

            <div className="space-y-4 bg-slate-900/50 rounded-lg p-4 border border-slate-700">
              <SettingItem
                label="Include Subfolders"
                description="Scan subfolders recursively"
              >
                <Toggle
                  enabled={formData.includeSubfolders}
                  onChange={(enabled) => setFormData({ ...formData, includeSubfolders: enabled })}
                />
              </SettingItem>

              <SettingItem
                label="Auto Add"
                description="Automatically add collections without confirmation"
              >
                <Toggle
                  enabled={formData.autoAdd}
                  onChange={(enabled) => setFormData({ ...formData, autoAdd: enabled })}
                />
              </SettingItem>

              <SettingItem
                label="Overwrite Existing"
                description="Overwrite collections if they already exist"
              >
                <Toggle
                  enabled={formData.overwriteExisting}
                  onChange={(enabled) => setFormData({ ...formData, overwriteExisting: enabled })}
                />
              </SettingItem>
            </div>

            {/* Info Box */}
            <div className="bg-blue-500/10 border border-blue-500/30 rounded-lg p-4">
              <div className="flex items-start space-x-3">
                <Zap className="h-5 w-5 text-blue-500 mt-0.5" />
                <div className="text-sm text-blue-300">
                  <p className="font-medium mb-1">Background Processing</p>
                  <p className="text-blue-400">
                    This operation will run in the background. You can monitor progress in the Background Jobs page.
                    The system will scan for folders, create collections, generate thumbnails, and create cache images automatically.
                  </p>
                </div>
              </div>
            </div>
            </div>

            {/* Actions - Fixed Footer */}
            <div className="flex-shrink-0 border-t border-slate-700 px-6 py-4 bg-slate-900/50">
              <div className="flex justify-end space-x-3">
                <Button type="button" variant="ghost" onClick={onClose}>
                  Cancel
                </Button>
                <Button type="submit" variant="primary" disabled={isSubmitting}>
                  {isSubmitting ? 'Starting...' : 'Start Bulk Add'}
                </Button>
              </div>
            </div>
          </form>
        </Dialog.Panel>
      </div>
    </Dialog>
  );
};

export default BulkAddCollectionsDialog;

