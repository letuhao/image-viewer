import { useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import useStore from './store/useStore';
import { collectionsApi } from './services/api';
import Layout from './components/Layout';
import CollectionsPage from './pages/CollectionsPage';
import CollectionViewerPage from './pages/CollectionViewerPage';
import ImageViewerPage from './pages/ImageViewerPage';
import TagSearchPage from './pages/TagSearchPage';

function App() {
  const { setCollections, setLoading, setError } = useStore();

  useEffect(() => {
    loadCollections();
  }, []);

  const loadCollections = async () => {
    try {
      setLoading(true);
      const response = await collectionsApi.getAll();
      setCollections(response.data);
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Failed to load collections');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Router>
      <div className="App">
        <Routes>
          <Route path="/" element={<Layout />}>
            <Route index element={<CollectionsPage />} />
            <Route path="collection/:id" element={<CollectionViewerPage />} />
            <Route path="collection/:id/viewer" element={<ImageViewerPage />} />
            <Route path="search" element={<TagSearchPage />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Route>
        </Routes>
        <Toaster
          position="top-right"
          toastOptions={{
            duration: 4000,
            style: {
              background: '#1e293b',
              color: '#f1f5f9',
              border: '1px solid #334155',
            },
          }}
        />
      </div>
    </Router>
  );
}

export default App;
