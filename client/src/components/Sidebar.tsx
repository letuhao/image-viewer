import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import useStore from '../store/useStore';
import { 
  FolderIcon, 
  ArchiveBoxIcon,
  TrashIcon,
  EyeIcon 
} from '@heroicons/react/24/outline';

const Sidebar: React.FC = () => {
  const { collections } = useStore();
  const location = useLocation();

  const getCollectionIcon = (type: 'folder' | 'zip') => {
    return type === 'zip' ? ArchiveBoxIcon : FolderIcon;
  };

  const getCollectionStats = (_collectionId: number) => {
    // This would typically come from the store or API
    // For now, we'll return placeholder data
    return { imageCount: 0, lastViewed: null };
  };

  return (
    <aside className="fixed left-0 top-16 w-64 h-[calc(100vh-4rem)] bg-dark-800 border-r border-dark-700 overflow-y-auto">
      <div className="p-4">
        <h2 className="text-lg font-semibold text-white mb-4">Collections</h2>
        
        {collections.length === 0 ? (
          <div className="text-center py-8">
            <FolderIcon className="h-12 w-12 text-dark-500 mx-auto mb-2" />
            <p className="text-dark-400 text-sm">No collections yet</p>
            <p className="text-dark-500 text-xs mt-1">
              Add your first collection to get started
            </p>
          </div>
        ) : (
          <div className="space-y-2">
            {collections.map((collection) => {
              const Icon = getCollectionIcon(collection.type);
              const stats = getCollectionStats(collection.id);
              const isActive = location.pathname.includes(`/collection/${collection.id}`);
              
              return (
                <Link
                  key={collection.id}
                  to={`/collection/${collection.id}`}
                  className={`group flex items-center space-x-3 p-3 rounded-lg transition-all ${
                    isActive
                      ? 'bg-primary-600 text-white'
                      : 'text-dark-300 hover:bg-dark-700 hover:text-white'
                  }`}
                >
                  <Icon className="h-5 w-5 flex-shrink-0" />
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium truncate">
                      {collection.name}
                    </p>
                    <div className="flex items-center space-x-2 text-xs opacity-75">
                      <span>{collection.type}</span>
                      {stats.imageCount > 0 && (
                        <>
                          <span>•</span>
                          <span>{stats.imageCount} images</span>
                        </>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center space-x-1 opacity-0 group-hover:opacity-100 transition-opacity">
                    <button
                      onClick={(e) => {
                        e.preventDefault();
                        // Handle view action
                      }}
                      className="p-1 hover:bg-dark-600 rounded"
                    >
                      <EyeIcon className="h-4 w-4" />
                    </button>
                    <button
                      onClick={(e) => {
                        e.preventDefault();
                        // Handle delete action
                      }}
                      className="p-1 hover:bg-red-600 rounded text-red-400"
                    >
                      <TrashIcon className="h-4 w-4" />
                    </button>
                  </div>
                </Link>
              );
            })}
          </div>
        )}
        
        <div className="mt-6 pt-4 border-t border-dark-700">
          <div className="text-xs text-dark-500 space-y-1">
            <p>Total Collections: {collections.length}</p>
            <p>Last Updated: {new Date().toLocaleDateString()}</p>
          </div>
        </div>
      </div>
    </aside>
  );
};

export default Sidebar;
