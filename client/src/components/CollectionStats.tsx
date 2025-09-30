import React, { useState } from 'react';
import { 
  EyeIcon, 
  ClockIcon, 
  MagnifyingGlassIcon, 
  TagIcon,
  HeartIcon,
  StarIcon
} from '@heroicons/react/24/outline';
import { statsApi } from '../services/api';
import toast from 'react-hot-toast';

interface CollectionStatsProps {
  collectionId: string;
  statistics?: {
    view_count: number;
    total_view_time: number;
    search_count: number;
    last_viewed: string | null;
    last_searched: string | null;
  };
  tags?: Array<{
    tag: string;
    count: number;
    added_by_list: string;
  }>;
  showTags?: boolean;
  showAddTag?: boolean;
}

const CollectionStats: React.FC<CollectionStatsProps> = ({
  collectionId,
  statistics,
  tags = [],
  showTags = true,
  showAddTag = true
}) => {
  const [newTag, setNewTag] = useState('');
  const [isAddingTag, setIsAddingTag] = useState(false);
  const [collectionTags, setCollectionTags] = useState(tags);

  const formatTime = (seconds: number) => {
    if (seconds < 60) return `${seconds}s`;
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m`;
    return `${Math.floor(seconds / 3600)}h ${Math.floor((seconds % 3600) / 60)}m`;
  };

  const formatDate = (dateString: string | null | undefined) => {
    if (!dateString) return 'Never';
    return new Date(dateString).toLocaleDateString();
  };

  const handleAddTag = async () => {
    if (!newTag.trim()) return;

    setIsAddingTag(true);
    try {
      const response = await statsApi.addTag(collectionId, newTag.trim());
      setCollectionTags(response.data.tags);
      setNewTag('');
      toast.success('Tag added successfully');
    } catch (error) {
      toast.error('Failed to add tag');
    } finally {
      setIsAddingTag(false);
    }
  };

  const handleRemoveTag = async (tag: string) => {
    try {
      const response = await statsApi.removeTag(collectionId, tag);
      setCollectionTags(response.data.tags);
      toast.success('Tag removed successfully');
    } catch (error) {
      toast.error('Failed to remove tag');
    }
  };

  const getTagReliability = (count: number) => {
    if (count >= 10) return { level: 'Very High', color: 'text-green-600', icon: StarIcon };
    if (count >= 5) return { level: 'High', color: 'text-blue-600', icon: HeartIcon };
    if (count >= 3) return { level: 'Medium', color: 'text-yellow-600', icon: TagIcon };
    return { level: 'Low', color: 'text-gray-500', icon: TagIcon };
  };

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-4">
      <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
        Collection Statistics
      </h3>
      
      {/* Statistics Grid */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <div className="flex items-center space-x-2">
          <EyeIcon className="h-5 w-5 text-blue-500" />
          <div>
            <p className="text-sm text-gray-500 dark:text-gray-400">Views</p>
            <p className="text-lg font-semibold text-gray-900 dark:text-white">
              {statistics?.view_count || 0}
            </p>
          </div>
        </div>
        
        <div className="flex items-center space-x-2">
          <ClockIcon className="h-5 w-5 text-green-500" />
          <div>
            <p className="text-sm text-gray-500 dark:text-gray-400">View Time</p>
            <p className="text-lg font-semibold text-gray-900 dark:text-white">
              {formatTime(statistics?.total_view_time || 0)}
            </p>
          </div>
        </div>
        
        <div className="flex items-center space-x-2">
          <MagnifyingGlassIcon className="h-5 w-5 text-purple-500" />
          <div>
            <p className="text-sm text-gray-500 dark:text-gray-400">Searches</p>
            <p className="text-lg font-semibold text-gray-900 dark:text-white">
              {statistics?.search_count || 0}
            </p>
          </div>
        </div>
        
        <div className="flex items-center space-x-2">
          <TagIcon className="h-5 w-5 text-orange-500" />
          <div>
            <p className="text-sm text-gray-500 dark:text-gray-400">Tags</p>
            <p className="text-lg font-semibold text-gray-900 dark:text-white">
              {collectionTags.length}
            </p>
          </div>
        </div>
      </div>

      {/* Last Activity */}
      <div className="mb-6">
        <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-2">Last Activity</h4>
        <div className="text-sm text-gray-600 dark:text-gray-300 space-y-1">
          <p>Last viewed: {formatDate(statistics?.last_viewed)}</p>
          <p>Last searched: {formatDate(statistics?.last_searched)}</p>
        </div>
      </div>

      {/* Tags Section */}
      {showTags && (
        <div>
          <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-3">Tags</h4>
          
          {collectionTags.length > 0 ? (
            <div className="flex flex-wrap gap-2 mb-4">
              {collectionTags.map((tagData, index) => {
                const reliability = getTagReliability(tagData.count);
                const IconComponent = reliability.icon;
                
                return (
                  <span
                    key={index}
                    className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-gray-100 dark:bg-gray-700 ${reliability.color} cursor-pointer hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors`}
                    onClick={() => handleRemoveTag(tagData.tag)}
                    title={`Added by ${tagData.count} user(s): ${tagData.added_by_list}. Reliability: ${reliability.level}`}
                  >
                    <IconComponent className="h-3 w-3 mr-1" />
                    {tagData.tag} ({tagData.count})
                  </span>
                );
              })}
            </div>
          ) : (
            <p className="text-sm text-gray-500 dark:text-gray-400 mb-4">No tags yet</p>
          )}

          {/* Add Tag Form */}
          {showAddTag && (
            <div className="flex space-x-2">
              <input
                type="text"
                value={newTag}
                onChange={(e) => setNewTag(e.target.value)}
                placeholder="Add a tag..."
                className="flex-1 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                onKeyPress={(e) => e.key === 'Enter' && handleAddTag()}
              />
              <button
                onClick={handleAddTag}
                disabled={isAddingTag || !newTag.trim()}
                className="px-4 py-2 bg-blue-600 text-white rounded-md text-sm font-medium hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isAddingTag ? 'Adding...' : 'Add'}
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default CollectionStats;
