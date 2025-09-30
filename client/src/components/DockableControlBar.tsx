import React, { useState, useEffect } from 'react';
import {
  ArrowLeftIcon,
  ArrowRightIcon,
  PlayIcon,
  PauseIcon,
  ArrowsPointingOutIcon,
  ArrowsPointingInIcon,
  ArrowPathIcon,
  Cog6ToothIcon,
  HomeIcon,
  ChevronLeftIcon,
  ChevronRightIcon
} from '@heroicons/react/24/outline';

interface DockableControlBarProps {
  // Navigation
  onPrevious: () => void;
  onNext: () => void;
  onPlay: () => void;
  onRandom: () => void;
  onFullscreen: () => void;
  onSettings: () => void;
  onHome: () => void;
  
  // Collection navigation
  onPreviousCollection: () => void;
  onNextCollection: () => void;
  hasPreviousCollection: boolean;
  hasNextCollection: boolean;
  
  // State
  isPlaying: boolean;
  isFullscreen: boolean;
  
  // Slideshow settings
  slideshowSpeed: number;
  onSlideshowSpeedChange: (speed: number) => void;
  enableCollectionNavigation: boolean;
  onCollectionNavigationToggle: (enabled: boolean) => void;
  
  // Auto-hide settings
  autoHideControls: boolean;
  onAutoHideToggle: (enabled: boolean) => void;
}

const DockableControlBar: React.FC<DockableControlBarProps> = ({
  onPrevious,
  onNext,
  onPlay,
  onRandom,
  onFullscreen,
  onSettings,
  onHome,
  onPreviousCollection,
  onNextCollection,
  hasPreviousCollection,
  hasNextCollection,
  isPlaying,
  isFullscreen,
  slideshowSpeed,
  onSlideshowSpeedChange,
  enableCollectionNavigation,
  onCollectionNavigationToggle,
  autoHideControls,
  onAutoHideToggle
}) => {
  const [isVisible, setIsVisible] = useState(!autoHideControls);
  const [mouseInArea, setMouseInArea] = useState(false);
  const [showAdvancedControls, setShowAdvancedControls] = useState(false);

  // Auto-hide logic
  useEffect(() => {
    if (!autoHideControls) {
      setIsVisible(true);
      return;
    }

    let timeout: NodeJS.Timeout;
    
    if (mouseInArea) {
      setIsVisible(true);
    } else {
      timeout = setTimeout(() => {
        setIsVisible(false);
      }, 2000); // Hide after 2 seconds
    }

    return () => {
      if (timeout) clearTimeout(timeout);
    };
  }, [mouseInArea, autoHideControls]);

  const handleMouseEnter = () => setMouseInArea(true);
  const handleMouseLeave = () => setMouseInArea(false);

  const speedOptions = [
    { value: 1000, label: '1s' },
    { value: 2000, label: '2s' },
    { value: 3000, label: '3s' },
    { value: 5000, label: '5s' },
    { value: 10000, label: '10s' }
  ];

  return (
    <>
      {/* Hover area to show controls when hidden */}
      {autoHideControls && (
        <div
          className="fixed bottom-0 left-0 right-0 h-16 z-40"
          onMouseEnter={handleMouseEnter}
          onMouseLeave={handleMouseLeave}
        />
      )}

      {/* Main Control Bar */}
      <div
        className={`fixed bottom-4 left-1/2 transform -translate-x-1/2 z-50 transition-all duration-300 ${
          isVisible ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-4 pointer-events-none'
        }`}
        onMouseEnter={handleMouseEnter}
        onMouseLeave={handleMouseLeave}
      >
        <div className="bg-dark-800 bg-opacity-95 backdrop-blur-sm rounded-lg px-4 py-3 shadow-xl border border-dark-700">
          {/* Primary Controls */}
          <div className="flex items-center space-x-2">
            {/* Collection Navigation */}
            {enableCollectionNavigation && (
              <>
                <button
                  onClick={onPreviousCollection}
                  disabled={!hasPreviousCollection}
                  className="btn btn-ghost btn-sm"
                  title="Previous Collection"
                >
                  <ChevronLeftIcon className="h-4 w-4" />
                </button>
                <div className="h-6 w-px bg-dark-600"></div>
              </>
            )}

            {/* Image Navigation */}
            <button
              onClick={onPrevious}
              className="btn btn-ghost btn-sm"
              title="Previous Image (←)"
            >
              <ArrowLeftIcon className="h-4 w-4" />
            </button>
            
            <button
              onClick={onPlay}
              className="btn btn-ghost btn-sm"
              title={`${isPlaying ? 'Pause' : 'Play'} (Space)`}
            >
              {isPlaying ? (
                <PauseIcon className="h-4 w-4" />
              ) : (
                <PlayIcon className="h-4 w-4" />
              )}
            </button>
            
            <button
              onClick={onNext}
              className="btn btn-ghost btn-sm"
              title="Next Image (→)"
            >
              <ArrowRightIcon className="h-4 w-4" />
            </button>

            {/* Collection Navigation */}
            {enableCollectionNavigation && (
              <>
                <div className="h-6 w-px bg-dark-600"></div>
                <button
                  onClick={onNextCollection}
                  disabled={!hasNextCollection}
                  className="btn btn-ghost btn-sm"
                  title="Next Collection"
                >
                  <ChevronRightIcon className="h-4 w-4" />
                </button>
              </>
            )}

            <div className="h-6 w-px bg-dark-600"></div>
            
            {/* Utility Controls */}
            <button
              onClick={onRandom}
              className="btn btn-ghost btn-sm"
              title="Random (R)"
            >
              <ArrowPathIcon className="h-4 w-4" />
            </button>
            
            <button
              onClick={onFullscreen}
              className="btn btn-ghost btn-sm"
              title={`${isFullscreen ? 'Exit Fullscreen' : 'Fullscreen'} (F)`}
            >
              {isFullscreen ? (
                <ArrowsPointingInIcon className="h-4 w-4" />
              ) : (
                <ArrowsPointingOutIcon className="h-4 w-4" />
              )}
            </button>

            <button
              onClick={onHome}
              className="btn btn-ghost btn-sm"
              title="Back to Collections"
            >
              <HomeIcon className="h-4 w-4" />
            </button>

            <button
              onClick={() => setShowAdvancedControls(!showAdvancedControls)}
              className="btn btn-ghost btn-sm"
              title="Advanced Settings"
            >
              <Cog6ToothIcon className="h-4 w-4" />
            </button>
          </div>

          {/* Advanced Controls */}
          {showAdvancedControls && (
            <div className="mt-3 pt-3 border-t border-dark-600">
              <div className="flex items-center justify-between space-x-4">
                {/* Auto-hide toggle */}
                <div className="flex items-center space-x-2">
                  <label className="flex items-center space-x-2 text-sm">
                    <input
                      type="checkbox"
                      checked={autoHideControls}
                      onChange={(e) => onAutoHideToggle(e.target.checked)}
                      className="rounded"
                    />
                    <span className="text-dark-300">Auto-hide controls</span>
                  </label>
                </div>

                {/* Slideshow speed */}
                <div className="flex items-center space-x-2">
                  <span className="text-sm text-dark-300">Speed:</span>
                  <select
                    value={slideshowSpeed}
                    onChange={(e) => onSlideshowSpeedChange(Number(e.target.value))}
                    className="bg-dark-700 border border-dark-600 rounded px-2 py-1 text-sm"
                  >
                    {speedOptions.map(option => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </div>

                {/* Collection navigation toggle */}
                <div className="flex items-center space-x-2">
                  <label className="flex items-center space-x-2 text-sm">
                    <input
                      type="checkbox"
                      checked={enableCollectionNavigation}
                      onChange={(e) => onCollectionNavigationToggle(e.target.checked)}
                      className="rounded"
                    />
                    <span className="text-dark-300">Navigate collections</span>
                  </label>
                </div>

                {/* Settings button */}
                <button
                  onClick={onSettings}
                  className="btn btn-ghost btn-sm"
                  title="Viewer Settings"
                >
                  <Cog6ToothIcon className="h-4 w-4" />
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </>
  );
};

export default DockableControlBar;
