/**
 * Media Display Utilities
 * 
 * Helper functions for determining how to display different media types
 */

// Video file extensions that should be displayed with <video> tags
const VIDEO_EXTENSIONS = new Set([
  'mp4', 'avi', 'mov', 'wmv', 'flv', 'mkv', 'webm'
]);

// Animated image extensions that should be displayed with <img> tags but preserve animation
const ANIMATED_IMAGE_EXTENSIONS = new Set([
  'gif', 'apng', 'webp' // WebP can be animated
]);

/**
 * Determines if a file should be displayed as a video element
 */
export function isVideoFile(filename: string): boolean {
  if (!filename) return false;
  
  const extension = filename.toLowerCase().split('.').pop();
  return extension ? VIDEO_EXTENSIONS.has(extension) : false;
}

/**
 * Determines if a file is an animated image that should preserve animation
 */
export function isAnimatedImage(filename: string): boolean {
  if (!filename) return false;
  
  const extension = filename.toLowerCase().split('.').pop();
  return extension ? ANIMATED_IMAGE_EXTENSIONS.has(extension) : false;
}

/**
 * Determines if a file is any type of animated media (video or animated image)
 */
export function isAnimatedMedia(filename: string): boolean {
  return isVideoFile(filename) || isAnimatedImage(filename);
}

/**
 * Gets the appropriate MIME type for a file based on its extension
 */
export function getMimeType(filename: string): string {
  if (!filename) return 'application/octet-stream';
  
  const extension = filename.toLowerCase().split('.').pop();
  
  switch (extension) {
    case 'jpg':
    case 'jpeg':
      return 'image/jpeg';
    case 'png':
      return 'image/png';
    case 'gif':
      return 'image/gif';
    case 'bmp':
      return 'image/bmp';
    case 'webp':
      return 'image/webp';
    case 'apng':
      return 'image/apng';
    case 'tiff':
    case 'tif':
      return 'image/tiff';
    case 'mp4':
      return 'video/mp4';
    case 'avi':
      return 'video/x-msvideo';
    case 'mov':
      return 'video/quicktime';
    case 'wmv':
      return 'video/x-ms-wmv';
    case 'flv':
      return 'video/x-flv';
    case 'mkv':
      return 'video/x-matroska';
    case 'webm':
      return 'video/webm';
    default:
      return 'application/octet-stream';
  }
}
