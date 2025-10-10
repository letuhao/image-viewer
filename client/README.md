# ImageViewer Frontend

Modern React frontend for ImageViewer Platform with TypeScript, Vite, and TailwindCSS.

## ✨ Features

- **3-Part Layout**: Header (fixed) / Body (scrollable) / Footer (fixed)
- **Single Scroll Design**: Only the body scrolls - no nested scrollbars
- **Type-Safe API**: Full TypeScript integration with backend DTOs
- **React Query**: Smart caching, automatic refetching, optimistic updates
- **Modern UI**: Dark theme, responsive design, icon-based navigation
- **Performance**: Virtual scrolling, code splitting, optimized bundle

## 🏗️ Architecture

### Layout Design

```
┌─────────────────────────────────────┐
│  Header (h-14, fixed, no scroll)    │ ← Icon navigation, compact
├─────────────────────────────────────┤
│                                     │
│  Body (flex-1, overflow-y-auto)     │ ← ONLY THIS SCROLLS
│                                     │
│  - Dashboard                        │
│  - Collections Grid                 │
│  - Image Viewer                     │
│                                     │
├─────────────────────────────────────┤
│  Footer (h-12, fixed, no scroll)    │ ← System stats, icons only
└─────────────────────────────────────┘
```

**Key Principles:**
- Root element: `h-screen` (100vh)
- Header: `h-14` (56px) - Fixed height
- Body: `flex-1 overflow-y-auto` - Takes remaining space, scrolls
- Footer: `h-12` (48px) - Fixed height
- **NO nested scrollbars** - single scroll context

### State Management

- **React Query**: Server state (collections, images, jobs)
  - Automatic caching (5 min stale time)
  - Background refetching
  - Optimistic updates
  - Request deduplication

- **Zustand** (future): UI state only
  - Theme preferences
  - Viewer settings
  - Sidebar state

- **Local State**: Component-specific state
  - Form inputs
  - Modal visibility
  - Hover states

### File Structure

```
client/
├── src/
│   ├── components/
│   │   ├── layout/          # Layout components
│   │   │   ├── Layout.tsx   # 3-part layout (H/B/F)
│   │   │   ├── Header.tsx   # Icon navigation
│   │   │   └── Footer.tsx   # System stats
│   │   └── ui/              # Reusable UI components
│   │       ├── Button.tsx
│   │       ├── Card.tsx
│   │       └── LoadingSpinner.tsx
│   ├── hooks/
│   │   └── useCollections.ts  # React Query hooks
│   ├── pages/
│   │   ├── Dashboard.tsx
│   │   ├── Collections.tsx
│   │   ├── CollectionDetail.tsx
│   │   ├── ImageViewer.tsx
│   │   └── Settings.tsx
│   ├── services/
│   │   ├── api.ts           # Axios instance
│   │   └── types.ts         # TypeScript types
│   ├── App.tsx              # Routes & providers
│   ├── main.tsx             # Entry point
│   └── index.css            # Global styles
├── public/
├── index.html
├── package.json
├── vite.config.ts
├── tailwind.config.js
└── tsconfig.json
```

## 🚀 Getting Started

### Prerequisites

- Node.js 18+ 
- npm or pnpm

### Installation

```bash
cd client
npm install
```

### Development

```bash
npm run dev
```

App runs at `http://localhost:3000`

API proxy: `http://localhost:3000/api` → `http://localhost:11000/api`

### Build

```bash
npm run build
```

Output: `dist/` folder (optimized, minified, tree-shaken)

### Preview Production Build

```bash
npm run preview
```

## 🎨 Design System

### Colors (Dark Theme)

- **Background**: slate-950, slate-900
- **Borders**: slate-800
- **Text**: white, slate-400
- **Primary**: blue-500/600
- **Success**: green-500
- **Warning**: yellow-500
- **Danger**: red-500

### Typography

- **Headings**: Bold, white
- **Body**: Regular, slate-100
- **Muted**: slate-400, slate-500

### Spacing

- **Container**: mx-auto px-4 py-6
- **Cards**: p-4
- **Gaps**: space-x-4, space-y-6

### Components

All components follow **shadcn/ui** design principles:
- Composable
- Accessible
- Type-safe
- Customizable

## 📦 Dependencies

### Core
- `react` 18 - UI library
- `react-dom` 18 - DOM rendering
- `react-router-dom` 6 - Routing
- `typescript` 5 - Type safety

### State & Data
- `@tanstack/react-query` - Server state management
- `axios` - HTTP client

### UI
- `tailwindcss` 3 - Utility-first CSS
- `lucide-react` - Icon library
- `react-hot-toast` - Notifications

### Build Tools
- `vite` 4 - Build tool & dev server
- `@vitejs/plugin-react` - React support

## 🔧 Configuration

### Environment Variables

Create `.env` file:

```env
VITE_API_URL=http://localhost:11000/api/v1
VITE_ENV=development
```

### Vite Config

```typescript
{
  server: {
    port: 3000,
    proxy: {
      '/api': 'http://localhost:11000'  // Backend proxy
    }
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          'react-vendor': ['react', 'react-dom'],
          'query-vendor': ['@tanstack/react-query']
        }
      }
    }
  }
}
```

### Tailwind Config

```javascript
{
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        // Custom colors if needed
      }
    }
  }
}
```

## 🧪 Testing (Future)

```bash
# Unit tests
npm run test

# E2E tests
npm run test:e2e

# Coverage
npm run test:coverage
```

## 📝 Code Style

### Component Pattern

```typescript
import { useState } from 'react';

interface MyComponentProps {
  title: string;
  onAction?: () => void;
}

const MyComponent: React.FC<MyComponentProps> = ({ title, onAction }) => {
  const [state, setState] = useState('');

  return (
    <div className="space-y-4">
      <h1>{title}</h1>
    </div>
  );
};

export default MyComponent;
```

### React Query Hook Pattern

```typescript
export const useMyData = (id: string) => {
  return useQuery({
    queryKey: ['myData', id],
    queryFn: async () => {
      const response = await api.get(`/data/${id}`);
      return response.data;
    },
    staleTime: 5 * 60 * 1000,  // 5 minutes
  });
};
```

## 🐛 Common Issues

### Issue: Multiple Scrollbars

**Problem**: Nested scrolling contexts

**Solution**: Only use `overflow-y-auto` on main body
```typescript
// ❌ Bad
<div className="h-full overflow-y-auto">
  <div className="h-[calc(100vh-200px)] overflow-y-auto">
    {/* content */}
  </div>
</div>

// ✅ Good
<div className="h-screen flex flex-col">
  <header className="h-14" />
  <main className="flex-1 overflow-y-auto">
    {/* content */}
  </main>
  <footer className="h-12" />
</div>
```

### Issue: Infinite Renders

**Problem**: Missing useCallback/useMemo

**Solution**: Memoize callbacks used in dependencies
```typescript
// ❌ Bad
const loadData = async () => { /* ... */ };
useEffect(() => { loadData(); }, [loadData]); // Creates new function every render

// ✅ Good
const loadData = useCallback(async () => { /* ... */ }, [deps]);
useEffect(() => { loadData(); }, [loadData]);
```

### Issue: Stale Data

**Problem**: React Query cache not invalidating

**Solution**: Invalidate queries after mutations
```typescript
const mutation = useMutation({
  mutationFn: updateData,
  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: ['myData'] });
  }
});
```

## 📚 Resources

- [React Query Docs](https://tanstack.com/query/latest)
- [TailwindCSS Docs](https://tailwindcss.com)
- [Vite Docs](https://vitejs.dev)
- [Lucide Icons](https://lucide.dev)

## 🚧 Roadmap

- [x] 3-part layout with single scroll
- [x] React Query integration
- [x] Collections page with pagination
- [ ] Virtual scrolling image grid
- [ ] Image viewer with keyboard navigation
- [ ] Settings page
- [ ] Background jobs monitoring
- [ ] Real-time updates (SignalR)
- [ ] PWA support
- [ ] E2E tests

## 📄 License

MIT

