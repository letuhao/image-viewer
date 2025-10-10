import { useRef, useEffect, useState } from 'react';
import { useVirtualizer } from '@tanstack/react-virtual';
import { useNavigate } from 'react-router-dom';
import type { Image } from '../services/types';

interface ImageGridProps {
  collectionId: string;
  images: Image[];
  isLoading?: boolean;
  gridClasses?: string;
}

const COLUMN_COUNT = 4;
const GAP = 16;
const ITEM_HEIGHT = 200;

const ImageGrid: React.FC<ImageGridProps> = ({ collectionId, images, isLoading, gridClasses }) => {
  const navigate = useNavigate();
  const parentRef = useRef<HTMLDivElement>(null);
  const [columnCount, setColumnCount] = useState(COLUMN_COUNT);

  // Calculate column count based on viewport width (fallback when no gridClasses provided)
  useEffect(() => {
    if (gridClasses) return; // Skip auto-calculation when gridClasses provided
    
    const updateColumnCount = () => {
      const width = window.innerWidth;
      if (width < 640) setColumnCount(1);
      else if (width < 768) setColumnCount(2);
      else if (width < 1024) setColumnCount(3);
      else if (width < 1536) setColumnCount(4);
      else setColumnCount(5);
    };

    updateColumnCount();
    window.addEventListener('resize', updateColumnCount);
    return () => window.removeEventListener('resize', updateColumnCount);
  }, [gridClasses]);

  // Group images into rows
  const rows: Image[][] = [];
  for (let i = 0; i < images.length; i += columnCount) {
    rows.push(images.slice(i, i + columnCount));
  }

  // Virtual scrolling
  const rowVirtualizer = useVirtualizer({
    count: rows.length,
    getScrollElement: () => parentRef.current,
    estimateSize: () => ITEM_HEIGHT + GAP,
    overscan: 3,
  });

  const handleImageClick = (imageId: string) => {
    navigate(`/collections/${collectionId}/viewer?imageId=${imageId}`);
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-slate-400">Loading images...</div>
      </div>
    );
  }

  if (images.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center h-64 text-center">
        <p className="text-slate-400 mb-2">No images in this collection</p>
        <p className="text-sm text-slate-500">Add some images to get started</p>
      </div>
    );
  }

  // If gridClasses provided, use CSS Grid instead of virtual scrolling
  if (gridClasses) {
    return (
      <div className={`grid gap-4 ${gridClasses}`}>
        {images.map((image) => (
          <div
            key={image.id}
            onClick={() => handleImageClick(image.id)}
            className="relative bg-slate-900 rounded-lg overflow-hidden cursor-pointer group hover:ring-2 hover:ring-primary-500 transition-all aspect-square"
          >
            <img
              src={`/api/v1/images/${collectionId}/${image.id}/thumbnail`}
              alt={image.fileName}
              className="w-full h-full object-cover"
              loading="lazy"
            />
            <div className="absolute inset-0 bg-gradient-to-t from-black/60 to-transparent opacity-0 group-hover:opacity-100 transition-opacity">
              <div className="absolute bottom-0 left-0 right-0 p-3">
                <p className="text-white text-sm font-medium truncate">
                  {image.fileName}
                </p>
                <p className="text-slate-300 text-xs">
                  {image.width} × {image.height}
                </p>
              </div>
            </div>
          </div>
        ))}
      </div>
    );
  }

  // Fallback to virtual scrolling (original behavior)
  return (
    <div
      ref={parentRef}
      className="h-full overflow-auto"
      style={{
        height: '100%',
      }}
    >
      <div
        style={{
          height: `${rowVirtualizer.getTotalSize()}px`,
          width: '100%',
          position: 'relative',
        }}
      >
        {rowVirtualizer.getVirtualItems().map((virtualRow) => {
          const row = rows[virtualRow.index];
          return (
            <div
              key={virtualRow.index}
              style={{
                position: 'absolute',
                top: 0,
                left: 0,
                width: '100%',
                height: `${virtualRow.size}px`,
                transform: `translateY(${virtualRow.start}px)`,
              }}
            >
              <div
                className="grid gap-4"
                style={{
                  gridTemplateColumns: `repeat(${columnCount}, 1fr)`,
                  height: `${ITEM_HEIGHT}px`,
                }}
              >
                {row.map((image) => (
                  <div
                    key={image.id}
                    onClick={() => handleImageClick(image.id)}
                    className="relative bg-slate-900 rounded-lg overflow-hidden cursor-pointer group hover:ring-2 hover:ring-primary-500 transition-all"
                  >
                    <img
                      src={`/api/v1/images/${collectionId}/${image.id}/thumbnail`}
                      alt={image.fileName}
                      className="w-full h-full object-cover"
                      loading="lazy"
                    />
                    <div className="absolute inset-0 bg-gradient-to-t from-black/60 to-transparent opacity-0 group-hover:opacity-100 transition-opacity">
                      <div className="absolute bottom-0 left-0 right-0 p-3">
                        <p className="text-white text-sm font-medium truncate">
                          {image.fileName}
                        </p>
                        <p className="text-slate-300 text-xs">
                          {image.width} × {image.height}
                        </p>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};

export default ImageGrid;

