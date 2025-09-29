import { create } from 'zustand';
import { devtools } from 'zustand/middleware';

export interface Collection {
  id: number;
  name: string;
  path: string;
  type: 'folder' | 'zip';
  created_at: string;
  updated_at: string;
  settings: Record<string, any>;
}

export interface Image {
  id: number;
  collection_id: number;
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
}

export interface AppState {
  collections: Collection[];
  selectedCollectionId: number | null;
  viewer: ViewerState;
  isLoading: boolean;
  error: string | null;
  
  // Actions
  setCollections: (collections: Collection[]) => void;
  addCollection: (collection: Collection) => void;
  updateCollection: (id: number, updates: Partial<Collection>) => void;
  removeCollection: (id: number) => void;
  selectCollection: (id: number | null) => void;
  
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
    }),
    {
      name: 'image-viewer-store',
    }
  )
);

export default useStore;
