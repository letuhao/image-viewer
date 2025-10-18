import React from 'react';
import { isVideoFile, isAnimatedImage, getMimeType } from '../../utils/mediaUtils';

interface MediaDisplayProps {
  src: string;
  alt: string;
  className?: string;
  style?: React.CSSProperties;
  loading?: 'lazy' | 'eager';
  onLoad?: () => void;
  onError?: () => void;
  controls?: boolean; // For video elements
  autoPlay?: boolean; // For video elements
  muted?: boolean; // For video elements
  loop?: boolean; // For video elements
}

/**
 * Smart media display component that renders images or videos appropriately
 * based on the file type
 */
export const MediaDisplay: React.FC<MediaDisplayProps> = ({
  src,
  alt,
  className = '',
  style = {},
  loading = 'lazy',
  onLoad,
  onError,
  controls = true,
  autoPlay = false,
  muted = true,
  loop = false,
}) => {
  // Extract filename from the src URL for type detection
  const filename = src.split('/').pop() || '';
  
  if (isVideoFile(filename)) {
    return (
      <video
        src={src}
        className={className}
        style={style}
        controls={controls}
        autoPlay={autoPlay}
        muted={muted}
        loop={loop}
        onLoadedData={onLoad}
        onError={onError}
        preload="metadata"
      >
        Your browser does not support the video tag.
      </video>
    );
  }
  
  // For images (including animated GIFs, WebP, etc.)
  return (
    <img
      src={src}
      alt={alt}
      className={className}
      style={style}
      loading={loading}
      onLoad={onLoad}
      onError={onError}
    />
  );
};

export default MediaDisplay;
