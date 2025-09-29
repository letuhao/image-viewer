import React from 'react';
import { Cog6ToothIcon } from '@heroicons/react/24/outline';
import useStore from '../store/useStore';

interface PreloadSettingsProps {
  isOpen: boolean;
  onClose: () => void;
}

const PreloadSettings: React.FC<PreloadSettingsProps> = ({ isOpen, onClose }) => {
  const { viewer, setPreloadEnabled, setPreloadBatchSize } = useStore();

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-dark-800 rounded-lg p-6 w-full max-w-md">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xl font-semibold text-white flex items-center">
            <Cog6ToothIcon className="h-5 w-5 mr-2" />
            Preload Settings
          </h2>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-white"
          >
            ✕
          </button>
        </div>

        <div className="space-y-4">
          {/* Enable/Disable Preloading */}
          <div className="flex items-center justify-between">
            <label className="text-white">Enable Image Preloading</label>
            <input
              type="checkbox"
              checked={viewer.preloadEnabled}
              onChange={(e) => setPreloadEnabled(e.target.checked)}
              className="w-4 h-4 text-blue-600 bg-dark-700 border-dark-600 rounded focus:ring-blue-500 focus:ring-2"
            />
          </div>

          {/* Batch Size */}
          <div>
            <label className="block text-white mb-2">
              Preload Batch Size: {viewer.preloadBatchSize} images
            </label>
            <input
              type="range"
              min="10"
              max="100"
              step="10"
              value={viewer.preloadBatchSize}
              onChange={(e) => setPreloadBatchSize(parseInt(e.target.value))}
              className="w-full h-2 bg-dark-700 rounded-lg appearance-none cursor-pointer slider"
            />
            <div className="flex justify-between text-xs text-gray-400 mt-1">
              <span>10</span>
              <span>100</span>
            </div>
          </div>

          {/* Info */}
          <div className="bg-dark-700 rounded-lg p-3">
            <p className="text-sm text-gray-300">
              <strong>How it works:</strong> When enabled, thumbnails are loaded in batches 
              to improve performance. Larger batch sizes load more images at once but use more memory.
            </p>
          </div>
        </div>

        <div className="flex justify-end mt-6">
          <button
            onClick={onClose}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
          >
            Done
          </button>
        </div>
      </div>
    </div>
  );
};

export default PreloadSettings;
