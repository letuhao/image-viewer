import { create } from 'zustand';
import { devtools } from 'zustand/middleware';

export interface Collection {
  id: string;
  name: string;
  path: string;
  type: 'folder' | 'zip';
  created_at: string;
  updated_at: string;
  settings: Record<string, any>;
}

export interface Image {
  id: string;
  collection_id: string;
  filename: string;
  relative_path: string;
  file_size: number;
  width: number;
  height: number;
  thumbnail_path: string;
  created_at: string;
}

export interface ViewerState {
  currentCollection: Collection | null;
  currentImage: Image | null;
  images: Image[];
  isFullscreen: boolean;
  isPlaying: boolean;
  playInterval: number;
  sortBy: 'filename' | 'date' | 'size';
  sortOrder: 'asc' | 'desc';
  filter: string;
  preloadEnabled: boolean;
  preloadBatchSize: number;
  preloadedThumbnails: Record<string, string>; // imageId -> base64 thumbnail
  
  // New viewer settings
  slideshowSpeed: number;
  enableCollectionNavigation: boolean;
  autoHideControls: boolean;
  currentCollectionIndex: number;
  allCollections: Collection[];
}

export interface AppState {
  collections: Collection[];
  selectedCollectionId: string | null;
  viewer: ViewerState;
  isLoading: boolean;
  error: string | null;
  
  // Actions
  setCollections: (collections: Collection[]) => void;
  addCollection: (collection: Collection) => void;
  updateCollection: (id: string, updates: Partial<Collection>) => void;
  removeCollection: (id: string) => void;
  selectCollection: (id: string | null) => void;
  
  // Viewer actions
  setCurrentImage: (image: Image | null) => void;
  setImages: (images: Image[]) => void;
  setFullscreen: (isFullscreen: boolean) => void;
  setPlaying: (isPlaying: boolean) => void;
  setPlayInterval: (interval: number) => void;
  setSortBy: (sortBy: ViewerState['sortBy']) => void;
  setSortOrder: (sortOrder: ViewerState['sortOrder']) => void;
  setFilter: (filter: string) => void;
  
  // Utility actions
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  
  // Navigation
  goToNextImage: () => void;
  goToPreviousImage: () => void;
  goToRandomImage: () => void;
  
  // Preload actions
  setPreloadEnabled: (enabled: boolean) => void;
  setPreloadBatchSize: (size: number) => void;
  preloadThumbnails: (collectionId: string, imageIds: string[]) => Promise<void>;
  getPreloadedThumbnail: (imageId: string) => string | null;
  
  // New viewer settings actions
  setSlideshowSpeed: (speed: number) => void;
  setEnableCollectionNavigation: (enabled: boolean) => void;
  setAutoHideControls: (enabled: boolean) => void;
  setAllCollections: (collections: Collection[]) => void;
  setCurrentCollectionIndex: (index: number) => void;
  goToPreviousCollection: () => string | null;
  goToNextCollection: () => string | null;
}

const useStore = create<AppState>()(
  devtools(
    (set, get) => ({
      collections: [],
      selectedCollectionId: null,
      viewer: {
        currentCollection: null,
        currentImage: null,
        images: [],
        isFullscreen: false,
        isPlaying: false,
        playInterval: 3000,
        sortBy: 'filename',
        sortOrder: 'asc',
        filter: '',
        preloadEnabled: true,
        preloadBatchSize: 50,
        preloadedThumbnails: {},
        
        // New viewer settings
        slideshowSpeed: 3000,
        enableCollectionNavigation: false,
        autoHideControls: true,
        currentCollectionIndex: -1,
        allCollections: [],
      },
      isLoading: false,
      error: null,
      
      setCollections: (collections) => set({ collections }),
      
      addCollection: (collection) => set((state) => ({
        collections: [...state.collections, collection]
      })),
      
      updateCollection: (id, updates) => set((state) => ({
        collections: state.collections.map(col => 
          col.id === id ? { ...col, ...updates } : col
        )
      })),
      
      removeCollection: (id) => set((state) => ({
        collections: state.collections.filter(col => col.id !== id),
        selectedCollectionId: state.selectedCollectionId === id ? null : state.selectedCollectionId
      })),
      
      selectCollection: (id) => set((state) => {
        const collection = state.collections.find(col => col.id === id);
        return {
          selectedCollectionId: id,
          viewer: {
            ...state.viewer,
            currentCollection: collection || null,
            currentImage: null,
            images: []
          }
        };
      }),
      
      setCurrentImage: (image) => set((state) => ({
        viewer: { ...state.viewer, currentImage: image }
      })),
      
      setImages: (images) => set((state) => ({
        viewer: { ...state.viewer, images }
      })),
      
      setFullscreen: (isFullscreen) => set((state) => ({
        viewer: { ...state.viewer, isFullscreen }
      })),
      
      setPlaying: (isPlaying) => set((state) => ({
        viewer: { ...state.viewer, isPlaying }
      })),
      
      setPlayInterval: (playInterval) => set((state) => ({
        viewer: { ...state.viewer, playInterval }
      })),
      
      setSortBy: (sortBy) => set((state) => ({
        viewer: { ...state.viewer, sortBy }
      })),
      
      setSortOrder: (sortOrder) => set((state) => ({
        viewer: { ...state.viewer, sortOrder }
      })),
      
      setFilter: (filter) => set((state) => ({
        viewer: { ...state.viewer, filter }
      })),
      
      setLoading: (isLoading) => set({ isLoading }),
      
      setError: (error) => set({ error }),
      
      goToNextImage: () => {
        const { viewer } = get();
        if (!viewer.currentImage || viewer.images.length === 0) return;
        
        const currentIndex = viewer.images.findIndex(img => img.id === viewer.currentImage!.id);
        const nextIndex = (currentIndex + 1) % viewer.images.length;
        set((state) => ({
          viewer: { ...state.viewer, currentImage: viewer.images[nextIndex] }
        }));
      },
      
      goToPreviousImage: () => {
        const { viewer } = get();
        if (!viewer.currentImage || viewer.images.length === 0) return;
        
        const currentIndex = viewer.images.findIndex(img => img.id === viewer.currentImage!.id);
        const prevIndex = (currentIndex - 1 + viewer.images.length) % viewer.images.length;
        set((state) => ({
          viewer: { ...state.viewer, currentImage: viewer.images[prevIndex] }
        }));
      },
      
      goToRandomImage: () => {
        const { viewer } = get();
        if (viewer.images.length === 0) return;
        
        const randomIndex = Math.floor(Math.random() * viewer.images.length);
        set((state) => ({
          viewer: { ...state.viewer, currentImage: viewer.images[randomIndex] }
        }));
      },
      
      setPreloadEnabled: (enabled) => set((state) => ({
        viewer: { ...state.viewer, preloadEnabled: enabled }
      })),
      
      setPreloadBatchSize: (size) => set((state) => ({
        viewer: { ...state.viewer, preloadBatchSize: size }
      })),
      
      preloadThumbnails: async (collectionId, imageIds) => {
        const { viewer } = get();
        if (!viewer.preloadEnabled || imageIds.length === 0) return;
        
        try {
          // Import the API function dynamically to avoid circular dependencies
          const { imagesApi } = await import('../services/api');
          const response = await imagesApi.getBatchThumbnails(collectionId, imageIds);
          
          const newThumbnails: Record<string, string> = {};
          response.data.thumbnails.forEach((thumb: any) => {
            newThumbnails[thumb.id] = `data:image/jpeg;base64,${thumb.thumbnail}`;
          });
          
          set((state) => ({
            viewer: {
              ...state.viewer,
              preloadedThumbnails: {
                ...state.viewer.preloadedThumbnails,
                ...newThumbnails
              }
            }
          }));
        } catch (error) {
          console.error('Failed to preload thumbnails:', error);
        }
      },
      
      getPreloadedThumbnail: (imageId) => {
        const { viewer } = get();
        return viewer.preloadedThumbnails[imageId] || null;
      },

      // New viewer settings actions
      setSlideshowSpeed: (speed) => set((state) => ({
        viewer: { ...state.viewer, slideshowSpeed: speed }
      })),

      setEnableCollectionNavigation: (enabled) => set((state) => ({
        viewer: { ...state.viewer, enableCollectionNavigation: enabled }
      })),

      setAutoHideControls: (enabled) => set((state) => ({
        viewer: { ...state.viewer, autoHideControls: enabled }
      })),

      setAllCollections: (collections) => set((state) => ({
        viewer: { ...state.viewer, allCollections: collections }
      })),

      setCurrentCollectionIndex: (index) => set((state) => ({
        viewer: { ...state.viewer, currentCollectionIndex: index }
      })),

      goToPreviousCollection: () => {
        const { viewer } = get();
        if (viewer.currentCollectionIndex > 0) {
          const prevCollection = viewer.allCollections[viewer.currentCollectionIndex - 1];
          if (prevCollection) {
            set((state) => ({
              viewer: { 
                ...state.viewer, 
                currentCollectionIndex: viewer.currentCollectionIndex - 1,
                currentCollection: prevCollection
              }
            }));
            // Return the collection ID for navigation
            return prevCollection.id;
          }
        }
        return null;
      },

      goToNextCollection: () => {
        const { viewer } = get();
        if (viewer.currentCollectionIndex < viewer.allCollections.length - 1) {
          const nextCollection = viewer.allCollections[viewer.currentCollectionIndex + 1];
          if (nextCollection) {
            set((state) => ({
              viewer: { 
                ...state.viewer, 
                currentCollectionIndex: viewer.currentCollectionIndex + 1,
                currentCollection: nextCollection
              }
            }));
            // Return the collection ID for navigation
            return nextCollection.id;
          }
        }
        return null;
      },
    }),
    {
      name: 'image-viewer-store',
    }
  )
);

export default useStore;
