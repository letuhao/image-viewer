# Image Viewer Infinite Loading Bug - Complete Fix

## 🐛 **The Bug**

**User Flow:**
```
1. Collection Detail → Image Viewer ✅ Works
2. Image Viewer → Back to Collection Detail ✅ Works
3. Collection Detail → Image Viewer again ❌ STUCK "Loading images..." forever
4. Press F5 (refresh) → ✅ Works again
```

---

## 🔍 **Root Causes Identified**

### **Issue 1: Component Not Resetting on Remount**

**Problem:**
```typescript
// BEFORE (broken)
useEffect(() => {
  setAllLoadedImages([]);
  setCurrentPage(1);
}, [collectionId]); // Only fires when collectionId CHANGES
```

**Scenario:**
- Visit Collection A Image Viewer (`collectionId = "abc123"`)
- Navigate back to Collection Detail
- Visit Collection A Image Viewer again (`collectionId = "abc123"`)
- **collectionId hasn't changed!** → useEffect doesn't fire → State not reset

**Fix:**
```typescript
// AFTER (fixed)
// Reset on mount
useEffect(() => {
  console.log('[ImageViewer] Mounting, resetting state');
  setAllLoadedImages([]);
  setCurrentPage(1);
  setTotalImagesCount(0);
}, []); // Empty deps = runs on every mount

// Also reset when switching collections
useEffect(() => {
  console.log('[ImageViewer] Collection changed, resetting state');
  setAllLoadedImages([]);
  setCurrentPage(1);
  setTotalImagesCount(0);
}, [collectionId]);
```

---

### **Issue 2: Merge Logic with Stale State**

**Problem:**
```typescript
// BEFORE (broken)
useEffect(() => {
  if (imagesData?.data) {
    setAllLoadedImages(prev => {
      // Always merge with previous
      const existingIds = new Set(prev.map(img => img.id));
      const newImages = imagesData.data.filter(img => !existingIds.has(img.id));
      return [...prev, ...newImages];
    });
  }
}, [imagesData]);
```

**Scenario:**
- `allLoadedImages` has stale/corrupted data from previous visit
- TanStack Query returns cached `imagesData` for page 1
- useEffect merges: `stale data + cached data`
- Result might be empty or corrupted
- `images.length === 0` → "Loading images..."

**Fix:**
```typescript
// AFTER (fixed)
useEffect(() => {
  if (imagesData?.data) {
    if (currentPage === 1) {
      // Page 1: REPLACE (fresh start)
      console.log('[ImageViewer] Page 1 - replacing with fresh data');
      setAllLoadedImages(imagesData.data);
    } else {
      // Page 2+: MERGE (accumulate)
      setAllLoadedImages(prev => {
        const existingIds = new Set(prev.map(img => img.id));
        const newImages = imagesData.data.filter(img => !existingIds.has(img.id));
        return [...prev, ...newImages];
      });
    }
  }
}, [imagesData, currentPage]);
```

---

## 🔧 **Complete Fix Applied**

### **Changes Made:**

1. **Mount Reset** (runs every time component mounts)
2. **Collection Change Reset** (runs when switching collections)
3. **Page 1 Replace** (instead of merge for fresh start)
4. **Page 2+ Merge** (accumulate additional pages)
5. **Better Logging** (debug console output)

---

## 🧪 **Testing & Verification**

### **Test Scenario:**
```
Step 1: Go to Collection Detail
Step 2: Click "View Images" button
  → Image Viewer opens
  → Console: "[ImageViewer] Mounting, resetting state"
  → Console: "[ImageViewer] Loaded page 1: 200 images (total: 10000)"
  → Console: "[ImageViewer] Page 1 - replacing with fresh data"
  → ✅ Images display

Step 3: Click X (back to Collection Detail)
  → Returns to Collection Detail
  → ✅ Works

Step 4: Click "View Images" again
  → Image Viewer mounts AGAIN
  → Console: "[ImageViewer] Mounting, resetting state" ← KEY!
  → Console: "[ImageViewer] Loaded page 1: 200 images"
  → Console: "[ImageViewer] Page 1 - replacing with fresh data" ← KEY!
  → ✅ Should work now!

Step 5: Repeat 10 times
  → All should work ✅
```

### **Debug Checklist:**

If still stuck, check console for:

1. **Is mount effect firing?**
   ```
   Should see: "[ImageViewer] Mounting, resetting state"
   If not: Component not remounting (React issue)
   ```

2. **Is imagesData arriving?**
   ```
   Should see: "[ImageViewer] Loaded page 1: X images"
   If not: API call failing or TanStack Query issue
   ```

3. **Is allLoadedImages being set?**
   ```
   Should see: "[ImageViewer] Page 1 - replacing with fresh data"
   If not: useEffect not firing
   ```

4. **Check network tab:**
   ```
   Should see: GET /api/v1/images/collection/{id}?page=1&limit=200
   If not: Query not enabled or collectionId undefined
   ```

5. **Check React DevTools:**
   ```
   Look at ImageViewer state:
   - allLoadedImages: should be array with images
   - currentPage: should be 1
   - totalImagesCount: should be > 0
   ```

---

## 🎯 **Why This Fix Works**

### **Problem Analysis:**

**React Component Lifecycle:**
```
1st Visit:
  Mount → useEffect[] fires → Reset state
  → imagesData arrives → useEffect fires → Populate allLoadedImages
  → images.length > 0 → Render images ✅

Navigate Away:
  Unmount → Component destroyed

2nd Visit (BEFORE FIX):
  Mount → useEffect[collectionId] checks if changed → NO CHANGE → Doesn't fire ❌
  → allLoadedImages stays [] (from useState initialization)
  → imagesData arrives (cached) → useEffect tries to merge with [] → Might fail
  → images.length === 0 → "Loading images..." forever ❌

2nd Visit (AFTER FIX):
  Mount → useEffect[] fires → Reset state to [] ✅
  → imagesData arrives (cached or fresh) → useEffect fires → REPLACE with fresh data ✅
  → images.length > 0 → Render images ✅
```

### **Key Insights:**

1. **Mount effect with `[]` deps ALWAYS fires**
   - Doesn't matter if collectionId changed
   - Doesn't matter if props are same
   - Every mount = fresh reset

2. **Page 1 replace instead of merge**
   - Prevents accumulation of stale data
   - Ensures fresh start
   - Page 2+ still merges correctly

3. **Two separate useEffects**
   - One for mount (always)
   - One for collection change (when switching)
   - Covers all scenarios

---

## ✅ **Expected Behavior Now**

**Navigation Flow:**
```
Collection Detail → Image Viewer
  ↓
Component mounts → State resets → API call → Page 1 replaces → WORKS ✅

Image Viewer → Collection Detail
  ↓
Component unmounts → State destroyed

Collection Detail → Image Viewer (SAME collection)
  ↓
Component mounts AGAIN → State resets AGAIN → API call → Page 1 replaces → WORKS ✅

(Repeat infinitely, all work!)
```

---

## 🚀 **Verification Steps**

1. **Clear Browser Cache** (to start fresh)
2. **Open DevTools Console**
3. **Navigate:** Collection Detail → Image Viewer
4. **Check Console:** Should see mount + page 1 logs
5. **Navigate Back:** Image Viewer → Collection Detail
6. **Navigate Again:** Collection Detail → Image Viewer
7. **Check Console:** Should see mount + page 1 logs AGAIN
8. **Verify:** Images load correctly

**If still stuck, share console logs and I'll debug further!**

---

## 🎊 **Status**

**Fixes Applied:** 2/2
**Expected Result:** ✅ Works every time
**Confidence:** 95%

**If it still doesn't work, there might be another issue. Let me know and I'll investigate further!**

