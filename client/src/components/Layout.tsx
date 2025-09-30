import React from 'react';
import { Outlet } from 'react-router-dom';
import Header from './Header';
import Sidebar from './Sidebar';
import LoadingSpinner from './LoadingSpinner';
import useStore from '../store/useStore';

const Layout: React.FC = () => {
  const { isLoading, error } = useStore();

  return (
    <div className="min-h-screen bg-dark-900">
      <Header />
      
      <div className="flex">
        <Sidebar />
        
        <main className="flex-1 ml-64">
          {isLoading && <LoadingSpinner />}
          {error && (
            <div className="bg-red-900/20 border border-red-500/50 text-red-300 px-4 py-3 m-4 rounded-lg">
              <p className="font-medium">Error: {error}</p>
            </div>
          )}
          <Outlet />
        </main>
      </div>
    </div>
  );
};

export default Layout;