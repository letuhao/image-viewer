import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { 
  HomeIcon, 
  FolderIcon, 
  PlusIcon,
  MagnifyingGlassIcon,
  CogIcon,
  SparklesIcon
} from '@heroicons/react/24/outline';
import SettingsScreen from './SettingsScreen';
import { collectionsApi } from '../services/api';
import toast from 'react-hot-toast';

const Header: React.FC = () => {
  const [showSettings, setShowSettings] = useState(false);
  const navigate = useNavigate();

  const handleRandomCollection = async () => {
    try {
      // Get random collection directly from database API
      const response = await collectionsApi.getRandom();
      const randomCollection = response.data;
      
      toast.success(`Opening random collection: ${randomCollection.name}`);
      navigate(`/collection/${randomCollection.id}`);
    } catch (error: any) {
      if (error.response?.status === 404) {
        toast.error('No collections with images found to pick randomly.');
      } else {
        toast.error('Failed to get random collection');
      }
      console.error('Error getting random collection:', error);
    }
  };

  return (
    <header className="bg-dark-800 border-b border-dark-700 px-6 py-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <Link 
            to="/" 
            className="flex items-center space-x-2 text-white hover:text-primary-400 transition-colors"
          >
            <FolderIcon className="h-8 w-8" />
            <span className="text-xl font-bold">Image Viewer</span>
          </Link>
        </div>
        
        <nav className="flex items-center space-x-6">
          <Link
            to="/"
            className="flex items-center space-x-2 text-dark-300 hover:text-white transition-colors"
          >
            <HomeIcon className="h-5 w-5" />
            <span>Collections</span>
          </Link>
          
          <Link
            to="/search"
            className="flex items-center space-x-2 text-dark-300 hover:text-white transition-colors"
          >
            <MagnifyingGlassIcon className="h-5 w-5" />
            <span>Tag Search</span>
          </Link>
          
          <button 
            onClick={handleRandomCollection}
            className="flex items-center space-x-2 text-dark-300 hover:text-white transition-colors"
            title="Open Random Collection"
          >
            <SparklesIcon className="h-5 w-5" />
            <span>Random</span>
          </button>
          
          <button 
            onClick={() => setShowSettings(true)}
            className="flex items-center space-x-2 text-dark-300 hover:text-white transition-colors"
          >
            <CogIcon className="h-5 w-5" />
            <span>Settings</span>
          </button>
          
          <button className="btn btn-primary">
            <PlusIcon className="h-4 w-4 mr-2" />
            Add Collection
          </button>
        </nav>
      </div>

      {/* Settings Screen Modal */}
      <SettingsScreen 
        isOpen={showSettings} 
        onClose={() => setShowSettings(false)} 
      />
    </header>
  );
};

export default Header;
