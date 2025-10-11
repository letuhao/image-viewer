import React, { useEffect, useRef } from 'react';
import { Image } from '../../services/types';

interface ImagePreviewSidebarProps {
  images: Image[];
  currentImageId: string;
  collectionId: string;
  onImageClick: (imageId: string) => void;
}

/**
 * Image preview sidebar (thumbnail strip) for image viewer
 * Shows vertical list of thumbnails for quick navigation
 */
const ImagePreviewSidebar: React.FC<ImagePreviewSidebarProps> = ({
  images,
  currentImageId,
  collectionId,
  onImageClick,
}) => {
  const currentImageRef = useRef<HTMLButtonElement>(null);
  const sidebarRef = useRef<HTMLDivElement>(null);

  // Auto-scroll to current image
  useEffect(() => {
    if (currentImageRef.current && sidebarRef.current) {
      currentImageRef.current.scrollIntoView({
        behavior: 'smooth',
        block: 'center',
      });
    }
  }, [currentImageId]);

  return (
    <div
      ref={sidebarRef}
      className="w-32 border-r border-slate-800 bg-slate-900/50 overflow-y-auto"
    >
      <div className="p-2 space-y-2">
        {images.map((image, index) => {
          const isActive = image.id === currentImageId;
          return (
            <button
              key={image.id}
              ref={isActive ? currentImageRef : null}
              onClick={() => onImageClick(image.id)}
              className={`w-full group relative overflow-hidden rounded transition-all ${
                isActive
                  ? 'ring-2 ring-primary-500 shadow-lg shadow-primary-500/50'
                  : 'hover:ring-2 hover:ring-slate-600'
              }`}
            >
              {/* Thumbnail */}
              <div className="relative aspect-square bg-slate-800">
                <img
                  src={`/api/v1/images/${collectionId}/${image.id}/thumbnail`}
                  alt={image.filename}
                  className="w-full h-full object-cover transition-transform duration-200 group-hover:scale-110"
                  loading="lazy"
                />

                {/* Image number overlay */}
                <div className="absolute top-1 left-1 px-1.5 py-0.5 bg-black/80 text-white text-xs font-bold rounded">
                  {index + 1}
                </div>

                {/* Active indicator */}
                {isActive && (
                  <div className="absolute inset-0 border-2 border-primary-500 pointer-events-none"></div>
                )}
              </div>
            </button>
          );
        })}
      </div>
    </div>
  );
};

export default ImagePreviewSidebar;

