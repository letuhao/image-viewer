# Siblings API - Smart Centering Algorithm

## 🎯 Goal

Show collections AROUND the current collection, always returning consistent number of items (pageSize or pageSize+1), with intelligent edge handling.

---

## 📐 Algorithm

### **Input**:
- `currentPosition` (rank): Position of current collection (0-based)
- `pageSize`: Number of siblings to show (e.g., 20)
- `totalCount`: Total collections in index

### **Output**:
- List of `pageSize` or `pageSize+1` collections centered on current
- Current collection is in the middle (or offset if near edges)

---

## 🧮 Calculation Steps

### **Step 1: Calculate Ideal Range**
```
halfPageSize = pageSize / 2

idealStart = currentPosition - halfPageSize
idealEnd = currentPosition + halfPageSize

Example (pageSize=20, currentPosition=12000):
  halfPageSize = 10
  idealStart = 12000 - 10 = 11,990
  idealEnd = 12000 + 10 = 12,010
  Range: 11,990 to 12,010 (21 items: 10 before + current + 10 after)
```

### **Step 2: Handle Start Edge (Near Beginning)**
```
if (idealStart < 0) {
    deficit = -idealStart
    idealStart = 0
    idealEnd = Math.Min(totalCount - 1, idealEnd + deficit)
}

Example (pageSize=20, currentPosition=5):
  halfPageSize = 10
  idealStart = 5 - 10 = -5 ❌
  idealEnd = 5 + 10 = 15
  
  Adjustment:
    deficit = -(-5) = 5
    idealStart = 0 ✅
    idealEnd = Min(24423, 15 + 5) = 20 ✅
  
  Final Range: 0 to 20 (21 items: 0 before + current at 5 + 15 after)
```

### **Step 3: Handle End Edge (Near End)**
```
if (idealEnd >= totalCount) {
    deficit = idealEnd - totalCount + 1
    idealEnd = totalCount - 1
    idealStart = Math.Max(0, idealStart - deficit)
}

Example (pageSize=20, currentPosition=24419, totalCount=24424):
  halfPageSize = 10
  idealStart = 24419 - 10 = 24,409
  idealEnd = 24419 + 10 = 24,429 ❌ (exceeds 24423)
  
  Adjustment:
    deficit = 24429 - 24424 + 1 = 6
    idealEnd = 24423 ✅
    idealStart = Max(0, 24409 - 6) = 24,403 ✅
  
  Final Range: 24,403 to 24,423 (21 items: 16 before + current at 24419 + 4 after)
```

---

## ✅ Test Cases

### **Test 1: Middle Position**
```
Input:
  currentPosition = 12,000
  pageSize = 20
  totalCount = 24,424

Calculation:
  halfPageSize = 10
  idealStart = 11,990
  idealEnd = 12,010
  
  Check start edge: 11,990 >= 0 ✅ (no adjustment)
  Check end edge: 12,010 < 24,424 ✅ (no adjustment)

Output:
  startRank = 11,990
  endRank = 12,010
  Items returned: 21 (10 before + current + 10 after)
  ✅ CORRECT
```

### **Test 2: Near Start (Position 5)**
```
Input:
  currentPosition = 5
  pageSize = 20
  totalCount = 24,424

Calculation:
  halfPageSize = 10
  idealStart = -5 ❌
  idealEnd = 15
  
  Start edge adjustment:
    deficit = 5
    idealStart = 0 ✅
    idealEnd = Min(24423, 15 + 5) = 20 ✅

Output:
  startRank = 0
  endRank = 20
  Items returned: 21 (ranks 0-20)
  Current at rank 5 (offset from center, but visible)
  ✅ CORRECT
```

### **Test 3: Near End (Position 24,419)**
```
Input:
  currentPosition = 24,419
  pageSize = 20
  totalCount = 24,424

Calculation:
  halfPageSize = 10
  idealStart = 24,409
  idealEnd = 24,429 ❌
  
  End edge adjustment:
    deficit = 24,429 - 24,424 + 1 = 6
    idealEnd = 24,423 ✅
    idealStart = Max(0, 24,409 - 6) = 24,403 ✅

Output:
  startRank = 24,403
  endRank = 24,423
  Items returned: 21 (ranks 24,403-24,423)
  Current at rank 24,419 (offset from center, but visible)
  ✅ CORRECT
```

### **Test 4: First Item (Position 0)**
```
Input:
  currentPosition = 0
  pageSize = 20
  totalCount = 24,424

Calculation:
  halfPageSize = 10
  idealStart = -10 ❌
  idealEnd = 10
  
  Start edge adjustment:
    deficit = 10
    idealStart = 0 ✅
    idealEnd = Min(24423, 10 + 10) = 20 ✅

Output:
  startRank = 0
  endRank = 20
  Items returned: 21 (ranks 0-20)
  Current at rank 0 (first item)
  ✅ CORRECT
```

### **Test 5: Last Item (Position 24,423)**
```
Input:
  currentPosition = 24,423
  pageSize = 20
  totalCount = 24,424

Calculation:
  halfPageSize = 10
  idealStart = 24,413
  idealEnd = 24,433 ❌
  
  End edge adjustment:
    deficit = 24,433 - 24,424 + 1 = 10
    idealEnd = 24,423 ✅
    idealStart = Max(0, 24,413 - 10) = 24,403 ✅

Output:
  startRank = 24,403
  endRank = 24,423
  Items returned: 21 (ranks 24,403-24,423)
  Current at rank 24,423 (last item)
  ✅ CORRECT
```

### **Test 6: Page 1217 (Your Case!)**
```
Input:
  currentPosition = 24,339 (from page 1217)
  pageSize = 20
  totalCount = 24,424

Calculation:
  halfPageSize = 10
  idealStart = 24,329
  idealEnd = 24,349
  
  Check start edge: 24,329 >= 0 ✅
  Check end edge: 24,349 < 24,424 ✅ (no adjustment needed)

Output:
  startRank = 24,329
  endRank = 24,349
  Items returned: 21 collections
  
  Siblings shown:
    Rank 24,329: Collection A
    Rank 24,330: Collection B
    ...
    Rank 24,339: CURRENT COLLECTION ← Middle!
    ...
    Rank 24,348: Collection Y
    Rank 24,349: Collection Z
  
  ✅ PERFECT! Shows 10 before + current + 10 after
```

---

## 🎯 Edge Case Matrix

| Current Position | PageSize | Total | Start | End | Items | Current Offset |
|-----------------|----------|-------|-------|-----|-------|----------------|
| 5 | 20 | 24,424 | 0 | 20 | 21 | 5 (offset left) ✅ |
| 50 | 20 | 24,424 | 40 | 60 | 21 | 50 (center) ✅ |
| 12,000 | 20 | 24,424 | 11,990 | 12,010 | 21 | 12,000 (center) ✅ |
| 24,339 | 20 | 24,424 | 24,329 | 24,349 | 21 | 24,339 (center) ✅ |
| 24,419 | 20 | 24,424 | 24,403 | 24,423 | 21 | 24,419 (offset right) ✅ |
| 0 | 20 | 24,424 | 0 | 20 | 21 | 0 (first) ✅ |
| 24,423 | 20 | 24,424 | 24,403 | 24,423 | 21 | 24,423 (last) ✅ |

**ALL CASES CORRECT!** ✅

---

## 🎨 Visual Representation

### **Normal Case (Middle)**:
```
Collections: [..., A, B, C, D, E, F, G, H, I, J, *CURRENT*, K, L, M, N, O, P, Q, R, S, T, ...]
                   ├─────────── 10 before ──────────┤  ├─────────── 10 after ──────────┤
Returned:          [A, B, C, D, E, F, G, H, I, J, CURRENT, K, L, M, N, O, P, Q, R, S, T]
                                                    ↑ Center
```

### **Near Start**:
```
Collections: [*CURRENT*, A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, ...]
              ↑                  ├────────────────── 20 after ──────────────────┤
Returned:     [CURRENT, A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S]
              ↑ Offset to start, but still visible
```

### **Near End**:
```
Collections: [..., A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, *CURRENT*]
                   ├────────────────── 20 before ──────────────────┤              ↑
Returned:          [A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, CURRENT]
                                                                                  ↑ Offset to end
```

---

## ✅ Benefits

1. **Always Consistent Count**: Returns pageSize or pageSize+1 items (with current)
2. **Maximizes Context**: At edges, gets more from available side
3. **Current Always Visible**: Never excluded, just offset from center at edges
4. **User-Friendly**: Shows position information clearly
5. **Performance**: Single ZRANGE call, very fast

---

## 🎊 Final Verification

**Your Scenario** (Position 24,340, pageSize=20):
- Sidebar will show **21 collections**
- **10 before** current (ranks 24,329-24,338)
- **Current** (rank 24,339)
- **10 after** current (ranks 24,340-24,348)
- All with **thumbnails** ✅
- **Centered perfectly** on your collection ✅

**READY TO TEST!** 🚀

