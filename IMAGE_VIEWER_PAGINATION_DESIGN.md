# Image Viewer Pagination - Comprehensive Design

## 🎯 Problem

**Current Implementation:**
```typescript
const { data: imagesData } = useImages({ collectionId: collectionId!, limit: 1000 });
```

**Issues:**
- Hardcoded limit of 1000 images
- Collections with 10k+ images will fail or be very slow
- All images loaded at once (poor performance)
- No pagination strategy

---

## 💡 Solution: Smart Pagination System

### **Core Concept: Progressive Loading**

1. **Initial Load**: Load first `imageViewerPageSize` images (e.g., 200)
2. **On-Demand Loading**: Load more pages as user navigates
3. **Smart Preloading**: Preload next/previous pages based on direction
4. **Memory Management**: Unload images far from current position

---

## 📐 Pagination Strategy

### **1. Page-Based Loading**

```typescript
interface ImageViewerState {
  loadedPages: Set<number>;           // Which pages are loaded
  currentPage: number;                // Current page user is viewing
  pageSize: number;                   // Images per page (e.g., 200)
  totalImages: number;                // Total images in collection
  images: Map<string, Image>;         // All loaded images by ID
}
```

### **2. Loading Logic**

**Initial Load:**
```typescript
// Load first page on mount
loadPage(1); // Loads images 0-199

// If user wants to go to last image
if (goToLast) {
  const lastPage = Math.ceil(totalImages / pageSize);
  loadPage(lastPage);
}

// If user wants specific image by ID
if (initialImageId) {
  const imageIndex = await getImageIndex(initialImageId);
  const page = Math.ceil((imageIndex + 1) / pageSize);
  loadPage(page);
}
```

**On Navigation:**
```typescript
function navigateToImage(imageId: string) {
  const imageIndex = getImageIndex(imageId);
  const page = Math.ceil((imageIndex + 1) / pageSize);
  
  // Load current page if not loaded
  if (!loadedPages.has(page)) {
    await loadPage(page);
  }
  
  // Preload adjacent pages
  preloadAdjacentPages(page);
  
  setCurrentImageId(imageId);
}
```

### **3. Preloading Strategy**

```typescript
function preloadAdjacentPages(currentPage: number) {
  const totalPages = Math.ceil(totalImages / pageSize);
  
  // Preload next page (high priority)
  if (currentPage < totalPages && !loadedPages.has(currentPage + 1)) {
    loadPage(currentPage + 1);
  }
  
  // Preload previous page (medium priority)
  if (currentPage > 1 && !loadedPages.has(currentPage - 1)) {
    loadPage(currentPage - 1);
  }
}
```

### **4. Memory Management**

```typescript
const MAX_LOADED_PAGES = 5; // Keep max 5 pages in memory

function unloadDistantPages(currentPage: number) {
  if (loadedPages.size > MAX_LOADED_PAGES) {
    // Unload pages that are far from current
    loadedPages.forEach(page => {
      const distance = Math.abs(page - currentPage);
      if (distance > 2) {
        unloadPage(page);
      }
    });
  }
}
```

---

## 🎨 UI Components

### **1. Loading Indicator**

```tsx
{isLoadingPage && (
  <div className="fixed top-4 right-4 bg-slate-800/90 px-4 py-2 rounded">
    <LoadingSpinner size="sm" text={`Loading page ${loadingPage}...`} />
  </div>
)}
```

### **2. Page Progress Indicator**

```tsx
<div className="fixed bottom-4 left-1/2 -translate-x-1/2 bg-slate-800/90 px-4 py-2 rounded">
  <span className="text-sm text-white">
    Loaded {loadedImages.length} / {totalImages} images
    ({loadedPages.size} / {totalPages} pages)
  </span>
</div>
```

### **3. Image Preview Sidebar Enhancement**

```tsx
// Show current page + load more on scroll
<ImagePreviewSidebar
  images={getCurrentPageImages()}
  totalImages={totalImages}
  onScrollNearEnd={() => loadNextPage()}
  onScrollNearStart={() => loadPreviousPage()}
/>
```

---

## 🔧 Implementation Details

### **1. State Management**

```typescript
const [imagePages, setImagePages] = useState<Map<number, Image[]>>(new Map());
const [loadedPages, setLoadedPages] = useState<Set<number>>(new Set());
const [currentPage, setCurrentPage] = useState(1);
const [isLoadingPage, setIsLoadingPage] = useState(false);
const [pageSize] = useState(() => 
  userSettings?.imageViewerPageSize || 
  parseInt(localStorage.getItem('imageViewerPageSize') || '200')
);
```

### **2. Load Page Function**

```typescript
const loadPage = useCallback(async (page: number) => {
  if (loadedPages.has(page) || isLoadingPage) return;
  
  setIsLoadingPage(true);
  setLoadingPage(page);
  
  try {
    const offset = (page - 1) * pageSize;
    const { data } = await useImages({
      collectionId,
      page,
      limit: pageSize,
    });
    
    setImagePages(prev => new Map(prev).set(page, data.images));
    setLoadedPages(prev => new Set(prev).add(page));
    
    // Unload distant pages
    unloadDistantPages(page);
  } catch (error) {
    console.error(`Failed to load page ${page}:`, error);
  } finally {
    setIsLoadingPage(false);
  }
}, [collectionId, pageSize, loadedPages, isLoadingPage]);
```

### **3. Get Current Images**

```typescript
const getCurrentImages = useMemo(() => {
  const images: Image[] = [];
  const sortedPages = Array.from(loadedPages).sort((a, b) => a - b);
  
  sortedPages.forEach(page => {
    const pageImages = imagePages.get(page);
    if (pageImages) {
      images.push(...pageImages);
    }
  });
  
  return images;
}, [imagePages, loadedPages]);
```

---

## 🎮 Edge Cases

### **1. Go To Last Image**

```typescript
if (goToLast) {
  const lastPage = Math.ceil(totalImages / pageSize);
  await loadPage(lastPage);
  const lastImage = imagePages.get(lastPage)?.slice(-1)[0];
  if (lastImage) {
    setCurrentImageId(lastImage.id);
  }
}
```

### **2. Shuffle Mode with Pagination**

```typescript
// Shuffle mode: still paginated, but randomize order
const [shuffledIndices, setShuffledIndices] = useState<number[]>([]);

function enableShuffleMode() {
  // Generate shuffled indices
  const indices = Array.from({ length: totalImages }, (_, i) => i);
  const shuffled = indices.sort(() => Math.random() - 0.5);
  setShuffledIndices(shuffled);
}

function getShuffledImage(index: number) {
  const shuffledIndex = shuffledIndices[index];
  const page = Math.ceil((shuffledIndex + 1) / pageSize);
  
  if (!loadedPages.has(page)) {
    await loadPage(page);
  }
  
  return findImageByIndex(shuffledIndex);
}
```

### **3. Cross-Collection Navigation**

```typescript
// When moving to next collection
function goToNextCollection() {
  // Clear loaded pages
  setImagePages(new Map());
  setLoadedPages(new Set());
  
  // Load first page of new collection
  navigate(`/viewer/${nextCollectionId}?imageId=${firstImageId}`);
}
```

### **4. Search/Filter Images**

```typescript
// If user searches/filters, need to reload
useEffect(() => {
  if (searchQuery || filter) {
    // Clear cache and reload
    clearAllPages();
    loadPage(1);
  }
}, [searchQuery, filter]);
```

---

## 📊 Performance Optimizations

### **1. Virtual Scrolling for Preview Sidebar**

```typescript
// Only render visible thumbnails in sidebar
import { FixedSizeList } from 'react-window';

<FixedSizeList
  height={800}
  itemCount={totalImages}
  itemSize={120}
  onItemsRendered={({ visibleStartIndex, visibleStopIndex }) => {
    // Load pages for visible items
    const startPage = Math.ceil((visibleStartIndex + 1) / pageSize);
    const endPage = Math.ceil((visibleStopIndex + 1) / pageSize);
    
    for (let page = startPage; page <= endPage; page++) {
      if (!loadedPages.has(page)) {
        loadPage(page);
      }
    }
  }}
>
  {({ index, style }) => <ImageThumbnail image={getImageAt(index)} style={style} />}
</FixedSizeList>
```

### **2. Image Caching**

```typescript
// Use browser cache for images
const imageCache = new Map<string, string>();

async function getImageUrl(imageId: string) {
  if (imageCache.has(imageId)) {
    return imageCache.get(imageId);
  }
  
  const url = await fetchImageUrl(imageId);
  imageCache.set(imageId, url);
  return url;
}
```

### **3. Intersection Observer for Preloading**

```typescript
// Preload images as they come into view
useEffect(() => {
  const observer = new IntersectionObserver(
    (entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          const imageId = entry.target.getAttribute('data-image-id');
          preloadImage(imageId);
        }
      });
    },
    { rootMargin: '200px' } // Preload 200px before visible
  );
  
  // Observe all image elements
  imageElements.forEach(el => observer.observe(el));
  
  return () => observer.disconnect();
}, [imageElements]);
```

---

## 🎯 Default Settings

```typescript
const DEFAULT_PAGE_SIZES = {
  collectionsPageSize: 100,        // Collections list
  collectionDetailPageSize: 20,    // Collection detail images
  sidebarPageSize: 20,             // Collection sidebar
  imageViewerPageSize: 200,        // Image viewer initial load
};
```

---

## 📱 User Settings UI

```tsx
<SettingsSection title="Image Viewer" description="Image viewer pagination">
  <SettingItem
    label="Images Per Page"
    description="Number of images to load at once (50-500)"
    vertical
  >
    <input
      type="number"
      min="50"
      max="500"
      value={settings.imageViewerPageSize}
      onChange={(e) => updateSetting('imageViewerPageSize', parseInt(e.value))}
      className="..."
    />
  </SettingItem>
  
  <SettingItem
    label="Max Loaded Pages"
    description="Maximum number of pages to keep in memory (3-10)"
  >
    <input
      type="number"
      min="3"
      max="10"
      value={settings.maxLoadedPages}
      onChange={(e) => updateSetting('maxLoadedPages', parseInt(e.value))}
    />
  </SettingItem>
</SettingsSection>
```

---

## ✅ Benefits

1. **Performance**: Only load what's needed
2. **Scalability**: Handle 100k+ image collections
3. **User Experience**: Fast initial load, smooth navigation
4. **Memory Efficient**: Unload distant pages
5. **Configurable**: User can adjust pageSize based on their needs
6. **Smart Preloading**: Predictive loading based on navigation

---

## 🚀 Implementation Phases

### **Phase 1: Basic Pagination**
- Add pageSize state
- Implement loadPage function
- Load initial page on mount

### **Phase 2: Navigation Integration**
- Update navigation to load pages as needed
- Handle edge cases (first/last image)

### **Phase 3: Preloading**
- Implement adjacent page preloading
- Add memory management (unload distant pages)

### **Phase 4: UI Enhancement**
- Add loading indicators
- Update preview sidebar with virtual scrolling
- Add page progress display

### **Phase 5: Advanced Features**
- Shuffle mode with pagination
- Cross-collection navigation
- Search/filter support

---

## 📈 Expected Performance

| Collection Size | Current (limit 1000) | With Pagination (200/page) |
|----------------|---------------------|---------------------------|
| 100 images | ✅ Fast (100 loaded) | ✅ Fast (100 loaded) |
| 1,000 images | ⚠️ Slow (1000 loaded) | ✅ Fast (200 loaded) |
| 10,000 images | ❌ Very Slow (1000 loaded) | ✅ Fast (200 loaded) |
| 100,000 images | ❌ FAIL (can't load all) | ✅ Fast (200 loaded) |

**Memory Usage:**
- Current: ~100MB for 1000 images
- With pagination: ~20MB for 200 images (5x less!)

---

## 🎉 Conclusion

This comprehensive pagination system will make the Image Viewer **scalable, performant, and user-friendly** for collections of any size!

**Ready to implement?** 🚀

