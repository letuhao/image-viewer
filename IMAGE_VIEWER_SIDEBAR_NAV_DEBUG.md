# Image Viewer Sidebar Navigation - Debug Analysis

## 🐛 **The Problem**

**Symptom:**
- Navigate via sidebar to different collection in Image Viewer
- Get 404 errors for thumbnails
- URL shows: `/api/v1/images/OLD_COLLECTION_ID/NEW_IMAGE_ID/thumbnail`
- F5 refresh works (uses correct collection ID)

**Evidence:**
```
404: /images/68ead03e9c465c81b74cd43d/68ead04a9c465c81b74d5daf/thumbnail
     └─ Old collection ID ────────┘ └─ New image ID ────────┘

200: /images/68ead03e9c465c81b74cd43d/68ead0459c465c81b74d1a43/thumbnail
     └─ Old collection ID ────────┘ └─ Old image ID ────────┘
```

---

## 🔍 **Root Cause Analysis**

### **Why F5 Works:**
```
F5 Refresh:
  → Browser navigates to URL: /collections/NEW_ID/viewer?imageId=...
  → React Router destroys old component completely
  → Creates NEW component instance from scratch
  → useParams returns NEW_ID
  → Everything uses NEW_ID
  → ✅ Works!
```

### **Why Sidebar Navigation Fails:**
```
Sidebar Click:
  → navigate('/collections/NEW_ID/viewer?imageId=...')
  → React Router sees: Same route pattern (/collections/:id/viewer)
  → Reuses SAME component instance (performance optimization)
  → Updates params: id changes from OLD_ID to NEW_ID
  → useParams returns NEW_ID ✅
  → BUT state (allLoadedImages) still has OLD images ❌
  → TanStack Query might return cached data ❌
  → Race conditions between reset and data load ❌
```

---

## 🎯 **The Real Problem: React Router Component Reuse**

When navigating between routes with the **same component**, React Router doesn't unmount/remount. It just updates the params.

**This causes:**
1. **State persists** between navigations
2. **useEffect timing issues** (which fires first?)
3. **TanStack Query cache confusion** (old query vs new query)
4. **Closure issues** (old collectionId in event handlers)

---

## 🔧 **Current Fixes Applied**

### **Fix 1: Reset on collectionId change**
```typescript
useEffect(() => {
  console.log(`Resetting for collectionId: ${collectionId}`);
  setAllLoadedImages([]);
  setCurrentPage(1);
  setCurrentImageId(initialImageId || '');
  // ... reset all state
}, [collectionId, initialImageId]);
```

**Issue:** TanStack Query might still have cached data from old collection

### **Fix 2: Replace on Page 1**
```typescript
useEffect(() => {
  if (imagesData?.data) {
    if (currentPage === 1) {
      setAllLoadedImages(imagesData.data); // Replace
    } else {
      // Merge for page 2+
    }
  }
}, [imagesData, currentPage, collectionId]);
```

**Issue:** If cached data arrives before reset completes, still uses old data

---

## 🚀 **Additional Fix Needed**

### **Option 1: Force Component Remount (BEST)**

Add a **key** to ImageViewer based on collectionId. This forces React to destroy and recreate the component.

**In App.tsx:**
```tsx
<Route 
  path="collections/:id/viewer" 
  element={<ImageViewer key={`viewer-${id}`} />}  // Use id as key
/>
```

**Problem:** Can't access `id` in Route element easily

**Better approach - In parent component or use location.key:**
```typescript
// In ImageViewer.tsx
const location = useLocation();

return (
  <div key={`${collectionId}-${initialImageId}`} className="...">
    {/* All content */}
  </div>
);
```

### **Option 2: Invalidate TanStack Query Cache**

When collectionId changes, invalidate all queries for the old collection:

```typescript
const queryClient = useQueryClient();

useEffect(() => {
  console.log(`Collection changed to ${collectionId}, invalidating old queries`);
  queryClient.invalidateQueries({ queryKey: ['images'] });
  queryClient.removeQueries({ queryKey: ['images'] }); // Nuclear option
}, [collectionId]);
```

### **Option 3: Add refetch on collectionId change**

```typescript
useEffect(() => {
  console.log(`Collection changed, refetching images`);
  refetchImages(); // Force refetch
}, [collectionId]);
```

---

## 🧪 **Debugging Steps**

Check console logs when navigating via sidebar:

**Expected order:**
```
1. [ImageViewer Sidebar] Navigating to collection NEW_ID, image IMG_ID
2. [ImageViewer] Collection or image changed - collectionId: NEW_ID
3. [ImageViewer] Resetting all state
4. [ImageViewer] Loaded page 1: 100 images
5. [ImageViewer] First image ID: ..., Current collectionId: NEW_ID
6. [ImageViewer] Page 1 - replacing with 100 fresh images
```

**If you see:**
```
1. [ImageViewer] Loaded page 1: 100 images (BEFORE reset!)
2. [ImageViewer] Collection changed...
3. [ImageViewer] Resetting all state (TOO LATE!)
```

**Then:** useEffect order is wrong (reset fires after data load)

**If you see:**
```
1. [ImageViewer] Collection changed to NEW_ID
2. [ImageViewer] Resetting all state
3. [ImageViewer] Loaded page 1: 100 images
4. [ImageViewer] First image ID: OLD_IMAGE_ID (WRONG!)
```

**Then:** TanStack Query returning cached data from old collection

---

## 💡 **Recommended Fix: Force Remount**

The cleanest solution is to force component remount when collectionId changes:

```typescript
// In ImageViewer.tsx
const ImageViewer: React.FC = () => {
  const { id: collectionId } = useParams();
  
  // Force remount when collectionId changes
  useEffect(() => {
    return () => {
      console.log('ImageViewer unmounting');
    };
  }, []);

  // Wrap everything in a div with key
  return (
    <div key={collectionId} className="...">
      {/* All content */}
    </div>
  );
};
```

**This forces React to:**
1. Destroy old component when key changes
2. Create new component instance
3. All state resets automatically
4. All useEffects run fresh
5. No race conditions!

---

## 🎯 **Next Steps**

1. Test current fix and share console logs
2. If still failing, apply force remount fix
3. Share exact sequence of logs to pinpoint issue

**Ready to apply force remount fix if needed!**

