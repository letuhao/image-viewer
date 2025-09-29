import React, { useState, useEffect } from 'react';
import useStore from '../store/useStore';
import { formatBytes } from '../utils/formatUtils';

interface Image {
  id: number;
  filename: string;
  thumbnail_path: string;
  file_size: number;
  width: number;
  height: number;
  created_at: string;
}

interface ImageListProps {
  images: Image[];
  onImageClick: (image: Image) => void;
  collectionId: number;
}

const ImageListItem: React.FC<{ image: Image; onImageClick: (image: Image) => void; collectionId: number }> = ({ 
  image, 
  onImageClick,
  collectionId
}) => {
  const [isLoading, setIsLoading] = useState(true);
  const [hasError, setHasError] = useState(false);
  const [thumbnailUrl, setThumbnailUrl] = useState<string | null>(null);

  // Get preloaded thumbnail from store
  const { getPreloadedThumbnail } = useStore();
  const preloadedThumbnail = getPreloadedThumbnail(image.id);

  useEffect(() => {
    if (preloadedThumbnail) {
      setThumbnailUrl(preloadedThumbnail);
      setIsLoading(false);
    } else {
      // Fallback to individual thumbnail loading
      setThumbnailUrl(`/api/images/${collectionId}/${image.id}/thumbnail`);
    }
  }, [preloadedThumbnail, collectionId, image.id]);

  const handleImageLoad = () => {
    setIsLoading(false);
  };

  const handleImageError = () => {
    setIsLoading(false);
    setHasError(true);
  };

  const handleClick = () => {
    onImageClick(image);
  };

  return (
    <div
      className="flex items-center space-x-4 p-4 bg-dark-800 rounded-lg hover:bg-dark-700 cursor-pointer transition-colors"
      onClick={handleClick}
    >
      {/* Thumbnail */}
      <div className="relative w-16 h-16 flex-shrink-0 overflow-hidden rounded-lg bg-dark-700">
        {isLoading && (
          <div className="absolute inset-0 flex items-center justify-center bg-dark-700 animate-pulse">
            <div className="loading-spinner h-4 w-4"></div>
          </div>
        )}
        
        {hasError ? (
          <div className="absolute inset-0 flex items-center justify-center bg-dark-700 text-dark-500">
            <svg className="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
          </div>
        ) : (
          thumbnailUrl && (
            <img
              src={thumbnailUrl}
              alt={image.filename}
              onLoad={handleImageLoad}
              onError={handleImageError}
              className="w-full h-full object-cover"
              style={{ display: isLoading ? 'none' : 'block' }}
            />
          )
        )}
      </div>
      
      {/* Image Info */}
      <div className="flex-1 min-w-0">
        <h3 className="text-white font-medium truncate">{image.filename}</h3>
        <div className="flex items-center space-x-4 mt-1 text-sm text-dark-400">
          <span>{image.width} × {image.height}</span>
          <span>{formatBytes(image.file_size)}</span>
          <span>{new Date(image.created_at).toLocaleDateString()}</span>
        </div>
      </div>
      
      {/* Actions */}
      <div className="flex items-center space-x-2">
        <button
          onClick={(e) => {
            e.stopPropagation();
            onImageClick(image);
          }}
          className="p-2 text-dark-400 hover:text-white transition-colors"
          title="View image"
        >
          <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
          </svg>
        </button>
      </div>
    </div>
  );
};

const ImageList: React.FC<ImageListProps> = ({ images, onImageClick, collectionId }) => {
  if (images.length === 0) {
    return (
      <div className="text-center py-12">
        <p className="text-dark-400">No images to display</p>
      </div>
    );
  }

  return (
    <div className="space-y-2 max-h-[calc(100vh-16rem)] overflow-y-auto scrollbar-hide">
      {images.map((image) => (
        <ImageListItem
          key={image.id}
          image={image}
          onImageClick={onImageClick}
          collectionId={collectionId}
        />
      ))}
    </div>
  );
};

export default ImageList;
