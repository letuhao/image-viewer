import React from 'react';
import { Dialog } from '@headlessui/react';
import { XMarkIcon } from '@heroicons/react/24/outline';

interface ViewerSettingsProps {
  onClose: () => void;
  playInterval: number;
  onPlayIntervalChange: (interval: number) => void;
}

const ViewerSettings: React.FC<ViewerSettingsProps> = ({
  onClose,
  playInterval,
  onPlayIntervalChange
}) => {
  const handleIntervalChange = (value: string) => {
    const interval = parseInt(value) * 1000; // Convert to milliseconds
    if (!isNaN(interval) && interval >= 1000) {
      onPlayIntervalChange(interval);
    }
  };

  return (
    <Dialog open={true} onClose={onClose} className="relative z-50">
      <div className="fixed inset-0 bg-black/50" aria-hidden="true" />
      
      <div className="fixed inset-0 flex items-center justify-center p-4">
        <Dialog.Panel className="bg-dark-800 rounded-lg shadow-xl max-w-md w-full p-6">
          <div className="flex items-center justify-between mb-6">
            <Dialog.Title className="text-xl font-semibold text-white">
              Viewer Settings
            </Dialog.Title>
            <button
              onClick={onClose}
              className="text-dark-400 hover:text-white transition-colors"
            >
              <XMarkIcon className="h-6 w-6" />
            </button>
          </div>

          <div className="space-y-6">
            {/* Auto-play Settings */}
            <div>
              <h3 className="text-lg font-medium text-white mb-4">Auto-play Settings</h3>
              
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-dark-300 mb-2">
                    Slideshow Interval
                  </label>
                  <div className="flex items-center space-x-3">
                    <input
                      type="number"
                      min="1"
                      max="60"
                      value={playInterval / 1000}
                      onChange={(e) => handleIntervalChange(e.target.value)}
                      className="input w-20"
                    />
                    <span className="text-dark-400">seconds</span>
                  </div>
                  <p className="text-xs text-dark-500 mt-1">
                    How long to wait between images in slideshow mode
                  </p>
                </div>
              </div>
            </div>

            {/* Keyboard Shortcuts */}
            <div>
              <h3 className="text-lg font-medium text-white mb-4">Keyboard Shortcuts</h3>
              
              <div className="space-y-2 text-sm">
                <div className="flex justify-between items-center">
                  <span className="text-dark-300">Previous Image</span>
                  <kbd className="px-2 py-1 bg-dark-700 text-dark-300 rounded text-xs">←</kbd>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-dark-300">Next Image</span>
                  <kbd className="px-2 py-1 bg-dark-700 text-dark-300 rounded text-xs">→</kbd>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-dark-300">Play/Pause</span>
                  <kbd className="px-2 py-1 bg-dark-700 text-dark-300 rounded text-xs">Space</kbd>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-dark-300">Random Image</span>
                  <kbd className="px-2 py-1 bg-dark-700 text-dark-300 rounded text-xs">R</kbd>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-dark-300">Fullscreen</span>
                  <kbd className="px-2 py-1 bg-dark-700 text-dark-300 rounded text-xs">F</kbd>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-dark-300">Exit</span>
                  <kbd className="px-2 py-1 bg-dark-700 text-dark-300 rounded text-xs">Esc</kbd>
                </div>
              </div>
            </div>

            {/* Display Settings */}
            <div>
              <h3 className="text-lg font-medium text-white mb-4">Display Settings</h3>
              
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-dark-300">Fit to Screen</p>
                    <p className="text-xs text-dark-500">Automatically resize images to fit screen</p>
                  </div>
                  <input
                    type="checkbox"
                    defaultChecked
                    className="w-4 h-4 text-primary-600 bg-dark-700 border-dark-600 rounded focus:ring-primary-500"
                  />
                </div>
                
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-dark-300">Show Image Info</p>
                    <p className="text-xs text-dark-500">Display image metadata overlay</p>
                  </div>
                  <input
                    type="checkbox"
                    defaultChecked
                    className="w-4 h-4 text-primary-600 bg-dark-700 border-dark-600 rounded focus:ring-primary-500"
                  />
                </div>
              </div>
            </div>
          </div>

          <div className="flex items-center justify-end pt-6">
            <button
              onClick={onClose}
              className="btn btn-primary"
            >
              Close
            </button>
          </div>
        </Dialog.Panel>
      </div>
    </Dialog>
  );
};

export default ViewerSettings;
