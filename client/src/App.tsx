import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { Toaster } from 'react-hot-toast';
import { AuthProvider } from './contexts/AuthContext';
import { UIProvider, useUI } from './contexts/UIContext';
import ProtectedRoute from './components/auth/ProtectedRoute';
import Layout from './components/layout/Layout';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import Collections from './pages/Collections';
import CollectionDetail from './pages/CollectionDetail';
import ImageViewer from './pages/ImageViewer';
import Settings from './pages/Settings';
import BackgroundJobs from './pages/BackgroundJobs';
import CacheManagement from './pages/CacheManagement';
import Libraries from './pages/Libraries';

// Create React Query client
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
      staleTime: 5 * 60 * 1000, // 5 minutes
    },
  },
});

// Component to conditionally render DevTools based on UI state
function DevToolsWrapper() {
  const { hideDevTools } = useUI();
  
  if (hideDevTools) {
    return null;
  }
  
  return <ReactQueryDevtools initialIsOpen={false} />;
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <UIProvider>
        <AuthProvider>
        <Router>
          <Routes>
            {/* Public Routes */}
            <Route path="/login" element={<Login />} />

            {/* Protected Routes */}
            <Route
              path="/"
              element={
                <ProtectedRoute>
                  <Layout />
                </ProtectedRoute>
              }
            >
              <Route index element={<Dashboard />} />
              <Route path="libraries" element={<Libraries />} />
              <Route path="collections" element={<Collections />} />
              <Route path="collections/:id" element={<CollectionDetail />} />
              <Route path="collections/:id/viewer" element={<ImageViewer />} />
              <Route path="jobs" element={<BackgroundJobs />} />
              <Route path="cache" element={<CacheManagement />} />
              <Route path="settings" element={<Settings />} />
              <Route path="*" element={<Navigate to="/" replace />} />
            </Route>
          </Routes>
        </Router>
        </AuthProvider>
        
        <Toaster
          position="top-right"
          toastOptions={{
            duration: 3000,
            style: {
              background: '#1e293b',
              color: '#f1f5f9',
              border: '1px solid #334155',
            },
            success: {
              iconTheme: {
                primary: '#10b981',
                secondary: '#f1f5f9',
              },
            },
            error: {
              iconTheme: {
                primary: '#ef4444',
                secondary: '#f1f5f9',
              },
            },
          }}
        />
        
        {/* React Query DevTools (only in development) */}
        <DevToolsWrapper />
      </UIProvider>
    </QueryClientProvider>
  );
}

export default App;

