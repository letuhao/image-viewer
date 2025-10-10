# Layout Design Guide: 3-Part Architecture

## 🎯 Design Goal

**Create a consistent layout with NO multiple vertical scrollbars**

The entire application uses a **single-scroll context** where only the body content scrolls, while header and footer remain fixed.

---

## 📐 Layout Structure

### Visual Diagram

```
┌─────────────────────────────────────────────────────────┐
│                                                         │
│  HEADER (h-14 = 56px, flex-shrink-0)                   │ ← Fixed, No Scroll
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  [Logo] [Dashboard] [Collections] [Settings]           │
│                                                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│                                                         │
│  BODY (flex-1, overflow-y-auto)                        │ ← ONLY THIS SCROLLS
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                                         │
│  Page Content:                                         │
│  - Dashboard                                           │
│  - Collections Grid                                    │
│  - Image Viewer                                        │
│  - Settings                                            │
│                                                         │
│  [Scrolls when content > viewport height]             │
│                                                         │
│  ↓ ↓ ↓ ↓ ↓ ↓ ↓ ↓ ↓ ↓ ↓ ↓ ↓ ↓ ↓ ↓ ↓                    │
│                                                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  FOOTER (h-12 = 48px, flex-shrink-0)                   │ ← Fixed, No Scroll
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  [Stats: Collections | Images | Jobs | Status]         │
│                                                         │
└─────────────────────────────────────────────────────────┘

Total Height: 100vh (h-screen)
Scroll Context: 1 (body only)
```

---

## 💻 Implementation

### Root Layout Component

```typescript
// src/components/layout/Layout.tsx

const Layout: React.FC = () => {
  return (
    <div className="h-screen flex flex-col bg-slate-950">
      {/* 
        h-screen: 100vh (full viewport height)
        flex flex-col: Stack children vertically
        bg-slate-950: Dark background
      */}
      
      <Header />
      {/* 
        h-14 (56px): Fixed height
        flex-shrink-0: Never shrinks
        No overflow: Never scrolls
      */}
      
      <main className="flex-1 overflow-y-auto">
        {/* 
          flex-1: Takes ALL remaining space
          overflow-y-auto: Scrolls vertically when content overflows
          This is the ONLY scroll context in the app!
        */}
        <Outlet />
      </main>
      
      <Footer />
      {/* 
        h-12 (48px): Fixed height
        flex-shrink-0: Never shrinks
        No overflow: Never scrolls
      */}
    </div>
  );
};
```

---

### Height Calculation

```
Total Viewport Height: 100vh (h-screen)

├─ Header:  56px (h-14)
├─ Body:    calc(100vh - 56px - 48px) = calc(100vh - 104px) [auto via flex-1]
└─ Footer:  48px (h-12)

Total: 100vh ✅
```

**Note:** We don't manually calculate `calc(100vh - 104px)`. The `flex-1` utility automatically fills remaining space!

---

## 🔧 Header Design (Icon-Based, Fixed Height)

### Requirements
- Fixed height: **56px** (`h-14`)
- Icon-based navigation (compact)
- Always visible (no scroll)
- Mobile responsive

### Implementation

```typescript
// src/components/layout/Header.tsx

const Header: React.FC = () => {
  return (
    <header className="h-14 border-b border-slate-800 bg-slate-900 flex-shrink-0">
      {/* 
        h-14: 56px height
        flex-shrink-0: Never shrinks
        border-b: Bottom border for separation
      */}
      
      <div className="h-full px-4 flex items-center justify-between">
        {/* Logo */}
        <Link to="/" className="flex items-center space-x-2">
          <ActivityIcon className="h-6 w-6 text-blue-500" />
          <span className="font-semibold text-lg text-white">
            ImageViewer
          </span>
        </Link>

        {/* Navigation (Icon-based) */}
        <nav className="flex items-center space-x-1">
          <NavLink to="/" icon={<HomeIcon />} label="Dashboard" />
          <NavLink to="/collections" icon={<FolderIcon />} label="Collections" />
          <NavLink to="/settings" icon={<SettingsIcon />} label="Settings" />
        </nav>
      </div>
    </header>
  );
};
```

**Benefits:**
- ✅ Fixed height prevents layout shift
- ✅ Icons are compact (no text wrapping)
- ✅ Always visible (important navigation)
- ✅ Responsive (icons scale well)

---

## 📜 Body Design (Single Scroll, Flexible Height)

### Requirements
- Takes remaining viewport space
- **ONLY element that scrolls**
- No height constraints on content
- Supports unlimited content

### Implementation

```typescript
// Layout.tsx
<main className="flex-1 overflow-y-auto">
  <Outlet />
</main>

// Page Component (Dashboard, Collections, etc.)
const MyPage: React.FC = () => {
  return (
    <div className="container mx-auto px-4 py-6 space-y-6">
      {/* 
        container: Max-width constraints
        mx-auto: Center horizontally
        px-4 py-6: Padding
        space-y-6: Vertical spacing between children
        
        NO HEIGHT CONSTRAINT!
        Content flows naturally, body provides scroll when needed
      */}
      
      <h1 className="text-3xl font-bold">Page Title</h1>
      
      <div className="grid grid-cols-3 gap-4">
        {/* Content... */}
      </div>
      
      {/* More content... */}
      {/* Body will scroll automatically when this exceeds viewport */}
    </div>
  );
};
```

**Key Points:**
- ✅ `flex-1`: Fills remaining space after header/footer
- ✅ `overflow-y-auto`: Scrolls when content > available space
- ✅ No `h-full` or `h-screen` on pages (let content determine height)
- ✅ Body scroll is smooth, native, accessible

---

## 📊 Footer Design (Icon-Based, Fixed Height)

### Requirements
- Fixed height: **48px** (`h-12`)
- Icon-based stats (compact)
- Always visible (system status)
- Real-time updates

### Implementation

```typescript
// src/components/layout/Footer.tsx

const Footer: React.FC = () => {
  const { data: stats } = useQuery({
    queryKey: ['systemStats'],
    queryFn: fetchSystemStats,
    refetchInterval: 10000, // Refresh every 10 seconds
  });

  return (
    <footer className="h-12 border-t border-slate-800 bg-slate-900 flex-shrink-0">
      {/* 
        h-12: 48px height
        flex-shrink-0: Never shrinks
        border-t: Top border for separation
      */}
      
      <div className="h-full px-4 flex items-center justify-between">
        {/* Left: Stats (Icons + Values) */}
        <div className="flex items-center space-x-4">
          <Stat icon={<DatabaseIcon />} label="Collections" value={stats?.totalCollections} />
          <Stat icon={<ImageIcon />} label="Images" value={stats?.totalImages} />
          <Stat icon={<ActivityIcon />} label="Jobs" value={stats?.activeJobs} />
        </div>

        {/* Right: Connection Status */}
        <div className="flex items-center space-x-2">
          <div className={`h-2 w-2 rounded-full ${stats ? 'bg-green-500' : 'bg-red-500'}`} />
          <span className="text-xs text-slate-400">
            {stats ? 'Connected' : 'Disconnected'}
          </span>
        </div>
      </div>
    </footer>
  );
};
```

**Benefits:**
- ✅ Fixed height (no layout shift)
- ✅ Icon-based (compact, no text wrapping)
- ✅ Always visible (system monitoring)
- ✅ Real-time updates via React Query

---

## ❌ Common Mistakes to Avoid

### ❌ Mistake 1: Nested Scrollbars

**BAD:**
```typescript
// Layout.tsx
<main className="flex-1 overflow-y-auto">
  <Outlet />
</main>

// Page.tsx
<div className="h-full overflow-y-auto">  {/* ❌ Creates 2nd scrollbar! */}
  <div className="h-[calc(100vh-200px)] overflow-y-auto">  {/* ❌ 3rd scrollbar! */}
    {content}
  </div>
</div>
```

**Result:** 3 scrollbars (body + page + nested div)

**GOOD:**
```typescript
// Layout.tsx
<main className="flex-1 overflow-y-auto">
  <Outlet />
</main>

// Page.tsx
<div className="container mx-auto px-4 py-6">  {/* ✅ No height, no overflow */}
  {content}
  {/* Body provides scroll automatically */}
</div>
```

**Result:** 1 scrollbar (body only)

---

### ❌ Mistake 2: Fixed Heights on Pages

**BAD:**
```typescript
<div className="h-screen">  {/* ❌ Full viewport height */}
  <div className="h-full">  {/* ❌ 100% of parent */}
    {content}
  </div>
</div>
```

**Problem:** Content is constrained, can't scroll naturally

**GOOD:**
```typescript
<div className="container mx-auto px-4 py-6">  {/* ✅ No height */}
  {content}
  {/* Height determined by content */}
</div>
```

---

### ❌ Mistake 3: Using `calc()` for Body Height

**BAD:**
```typescript
<main className="h-[calc(100vh-104px)] overflow-y-auto">
  {/* ❌ Manual calculation, fragile */}
  <Outlet />
</main>
```

**Problem:** Breaks when header/footer heights change

**GOOD:**
```typescript
<main className="flex-1 overflow-y-auto">
  {/* ✅ Flexbox calculates automatically */}
  <Outlet />
</main>
```

---

## 📱 Responsive Behavior

### Mobile (< 768px)
- Header: Hamburger menu, icons only
- Body: Same scroll behavior
- Footer: Icons only, hide labels

### Tablet (768px - 1024px)
- Header: Icon + text labels
- Body: Same scroll behavior
- Footer: Icon + text labels

### Desktop (> 1024px)
- Header: Full navigation
- Body: Same scroll behavior
- Footer: Full stats display

**Key:** All screen sizes use **same scroll architecture** (body only)

---

## 🎨 Styling Guidelines

### Header Styling
```css
.header {
  height: 56px;              /* Fixed height */
  flex-shrink: 0;            /* Never shrinks */
  background: slate-900;     /* Dark background */
  border-bottom: 1px solid slate-800;  /* Separator */
}
```

### Body Styling
```css
.body {
  flex: 1;                   /* Fill remaining space */
  overflow-y: auto;          /* Scroll when needed */
  /* No height! Flexbox handles it */
}
```

### Footer Styling
```css
.footer {
  height: 48px;              /* Fixed height */
  flex-shrink: 0;            /* Never shrinks */
  background: slate-900;     /* Dark background */
  border-top: 1px solid slate-800;  /* Separator */
}
```

---

## 🧪 Testing Checklist

### Layout Tests
- [ ] Header always visible on scroll
- [ ] Footer always visible on scroll
- [ ] Only body scrolls (no nested scrollbars)
- [ ] Content taller than viewport scrolls correctly
- [ ] Content shorter than viewport shows no scrollbar
- [ ] Resize window maintains single scroll
- [ ] Mobile menu works on small screens

### Performance Tests
- [ ] No layout shift on page load
- [ ] Smooth 60 FPS scrolling
- [ ] No jank when toggling routes
- [ ] Footer stats update without scroll jump

---

## 📊 Performance Comparison

| Metric | Old Design | New Design |
|--------|-----------|------------|
| Scrollbars | 2-3 nested | 1 (body only) |
| Layout Shift | Yes | None |
| Scroll Jank | Frequent | None |
| Mobile UX | Poor | Excellent |
| Maintainability | Hard | Easy |

---

## ✅ Summary

### Layout Architecture
```
Root: h-screen flex flex-col
├─ Header: h-14 flex-shrink-0 (56px, fixed)
├─ Body: flex-1 overflow-y-auto (flexible, scrolls)
└─ Footer: h-12 flex-shrink-0 (48px, fixed)
```

### Key Principles
1. **Single Scroll**: Only body scrolls
2. **Fixed Heights**: Header (56px) + Footer (48px)
3. **Flexible Body**: `flex-1` fills remaining space
4. **No Nesting**: Pages don't set height/overflow
5. **Icon-Based**: Header & Footer use icons (compact)

### Benefits
✅ No multiple scrollbars
✅ Consistent UX across all pages
✅ Always-visible header/footer
✅ Smooth, native scrolling
✅ Responsive on all devices
✅ Easy to maintain

---

## 📚 References

- Flexbox Guide: https://css-tricks.com/snippets/css/a-guide-to-flexbox/
- TailwindCSS Flex: https://tailwindcss.com/docs/flex
- Scroll Behavior: https://developer.mozilla.org/en-US/docs/Web/CSS/scroll-behavior

---

**Last Updated:** October 10, 2025
**Version:** 1.0.0

