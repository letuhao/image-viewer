import React, { useState, useCallback, useEffect } from 'react';
import { FixedSizeGrid as Grid } from 'react-window';
import AutoSizer from 'react-virtualized-auto-sizer';
import useStore from '../store/useStore';

interface Image {
  id: number;
  filename: string;
  thumbnail_path: string;
  width: number;
  height: number;
}

interface ImageGridProps {
  images: Image[];
  onImageClick: (image: Image) => void;
  collectionId: number;
}

interface ImageItemProps {
  columnIndex: number;
  rowIndex: number;
  style: React.CSSProperties;
  data: {
    images: Image[];
    onImageClick: (image: Image) => void;
    columnCount: number;
    collectionId: number;
  };
}

const ImageItem: React.FC<ImageItemProps> = ({ columnIndex, rowIndex, style, data }) => {
  const { images, onImageClick, columnCount, collectionId } = data;
  const index = rowIndex * columnCount + columnIndex;
  const image = images[index];

  if (!image) {
    return <div style={style} />;
  }

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
    <div style={style} className="p-1">
      <div
        className="relative aspect-square overflow-hidden rounded-lg cursor-pointer transition-transform hover:scale-105 bg-dark-700"
        onClick={handleClick}
      >
        {isLoading && (
          <div className="absolute inset-0 flex items-center justify-center bg-dark-700 animate-pulse">
            <div className="loading-spinner h-6 w-6"></div>
          </div>
        )}
        
        {hasError ? (
          <div className="absolute inset-0 flex items-center justify-center bg-dark-700 text-dark-500">
            <svg className="h-8 w-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
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
        
        {/* Image info overlay */}
        <div className="absolute bottom-0 left-0 right-0 bg-gradient-to-t from-black/80 to-transparent p-2 opacity-0 hover:opacity-100 transition-opacity">
          <p className="text-white text-xs truncate">{image.filename}</p>
          <p className="text-gray-300 text-xs">
            {image.width} × {image.height}
          </p>
        </div>
      </div>
    </div>
  );
};

const ImageGrid: React.FC<ImageGridProps> = ({ images, onImageClick, collectionId }) => {
  const [containerWidth, setContainerWidth] = useState(0);
  const itemSize = 200; // Fixed size for each image item
  const columnCount = Math.max(1, Math.floor(containerWidth / itemSize));
  const rowCount = Math.ceil(images.length / columnCount);

  const handleResize = useCallback((width: number) => {
    setContainerWidth(width);
  }, []);

  if (images.length === 0) {
    return (
      <div className="text-center py-12">
        <p className="text-dark-400">No images to display</p>
      </div>
    );
  }

  return (
    <div className="h-[calc(100vh-16rem)]">
      <AutoSizer onResize={({ width }) => handleResize(width)}>
        {({ width, height }) => (
          <Grid
            columnCount={columnCount}
            columnWidth={itemSize}
            height={height}
            rowCount={rowCount}
            rowHeight={itemSize}
            width={width}
            itemData={{
              images,
              onImageClick,
              columnCount,
              collectionId
            }}
          >
            {ImageItem}
          </Grid>
        )}
      </AutoSizer>
    </div>
  );
};

export default ImageGrid;
