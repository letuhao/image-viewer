import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { 
  TagIcon, 
  AdjustmentsHorizontalIcon,
  EyeIcon,
  ClockIcon,
  MagnifyingGlassIcon as SearchIcon
} from '@heroicons/react/24/outline';
import TagAutocomplete from '../components/TagAutocomplete';
import { statsApi } from '../services/api';

const TagSearchPage: React.FC = () => {
  const [selectedTags, setSelectedTags] = useState<string[]>([]);
  const [operator, setOperator] = useState<'AND' | 'OR'>('AND');
  const [searchResults, setSearchResults] = useState<any[]>([]);
  const [popularTags, setPopularTags] = useState<Array<{
    tag: string;
    usage_count: number;
    collection_count: number;
  }>>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [hasSearched, setHasSearched] = useState(false);

  useEffect(() => {
    fetchPopularTags();
  }, []);

  useEffect(() => {
    if (selectedTags.length > 0) {
      searchCollections();
    } else {
      setSearchResults([]);
      setHasSearched(false);
    }
  }, [selectedTags, operator]);

  const fetchPopularTags = async () => {
    try {
      const response = await statsApi.getPopularTags(50);
      setPopularTags(response.data.tags);
    } catch (error) {
      console.error('Error fetching popular tags:', error);
    }
  };

  const searchCollections = async () => {
    if (selectedTags.length === 0) return;

    setIsLoading(true);
    try {
      const response = await statsApi.getCollectionsByTags(selectedTags, operator, 100);
      setSearchResults(response.data.collections);
      setHasSearched(true);
    } catch (error) {
      console.error('Error searching collections:', error);
      setSearchResults([]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleTagSelect = (tag: string) => {
    if (!selectedTags.includes(tag)) {
      setSelectedTags([...selectedTags, tag]);
    }
  };


  const clearAllTags = () => {
    setSelectedTags([]);
  };

  const formatTime = (seconds: number) => {
    if (seconds < 60) return `${seconds}s`;
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m`;
    return `${Math.floor(seconds / 3600)}h ${Math.floor((seconds % 3600) / 60)}m`;
  };

  const getCollectionIcon = (type: string) => {
    switch (type) {
      case 'folder':
        return '📁';
      case 'zip':
      case 'cbz':
        return '📦';
      case '7z':
        return '🗜️';
      case 'rar':
      case 'cbr':
        return '📚';
      case 'tar':
        return '📄';
      default:
        return '📁';
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header */}
        <div className="mb-8">
          <div className="flex items-center space-x-3 mb-4">
            <TagIcon className="h-8 w-8 text-blue-600" />
            <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Tag Search</h1>
          </div>
          <p className="text-gray-600 dark:text-gray-400">
            Discover collections by searching and filtering with tags. Use multiple tags to find exactly what you're looking for.
          </p>
        </div>

        {/* Search Section */}
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6 mb-8">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
            Search by Tags
          </h2>
          
          {/* Tag Input */}
          <div className="mb-4">
            <TagAutocomplete
              selectedTags={selectedTags}
              onTagsChange={setSelectedTags}
              placeholder="Type to search and add tags..."
              maxTags={10}
              className="w-full"
            />
          </div>

          {/* Operator Selection */}
          {selectedTags.length > 1 && (
            <div className="mb-4">
              <h3 className="text-sm font-medium text-gray-900 dark:text-white mb-2">
                Search Logic
              </h3>
              <div className="flex space-x-4">
                <label className="flex items-center">
                  <input
                    type="radio"
                    value="AND"
                    checked={operator === 'AND'}
                    onChange={(e) => setOperator(e.target.value as 'AND' | 'OR')}
                    className="mr-2"
                  />
                  <span className="text-sm text-gray-700 dark:text-gray-300">
                    <strong>AND</strong> - Collections must have ALL selected tags
                  </span>
                </label>
                <label className="flex items-center">
                  <input
                    type="radio"
                    value="OR"
                    checked={operator === 'OR'}
                    onChange={(e) => setOperator(e.target.value as 'AND' | 'OR')}
                    className="mr-2"
                  />
                  <span className="text-sm text-gray-700 dark:text-gray-300">
                    <strong>OR</strong> - Collections must have ANY of the selected tags
                  </span>
                </label>
              </div>
            </div>
          )}

          {/* Active Search Summary */}
          {selectedTags.length > 0 && (
            <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-2">
                  <AdjustmentsHorizontalIcon className="h-5 w-5 text-blue-600" />
                  <span className="text-sm text-blue-800 dark:text-blue-200">
                    Searching for collections with: <strong>{selectedTags.join(` ${operator} `)}</strong>
                  </span>
                </div>
                <button
                  onClick={clearAllTags}
                  className="text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-200"
                >
                  Clear all
                </button>
              </div>
            </div>
          )}
        </div>

        {/* Popular Tags */}
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6 mb-8">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
            Popular Tags
          </h2>
          <div className="flex flex-wrap gap-2">
            {popularTags.map((tagData, index) => (
              <button
                key={index}
                onClick={() => handleTagSelect(tagData.tag)}
                disabled={selectedTags.includes(tagData.tag)}
                className={`inline-flex items-center px-3 py-2 rounded-full text-sm font-medium transition-colors ${
                  selectedTags.includes(tagData.tag)
                    ? 'bg-gray-200 text-gray-500 dark:bg-gray-600 dark:text-gray-400 cursor-not-allowed'
                    : 'bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'
                }`}
                title={`Used ${tagData.usage_count} times in ${tagData.collection_count} collections`}
              >
                <TagIcon className="h-4 w-4 mr-1" />
                {tagData.tag}
                <span className="ml-1 text-xs opacity-75">({tagData.usage_count})</span>
              </button>
            ))}
          </div>
        </div>

        {/* Search Results */}
        {hasSearched && (
          <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
            <div className="flex items-center justify-between mb-6">
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white">
                Search Results
                {isLoading ? (
                  <span className="ml-2 text-sm text-gray-500">Loading...</span>
                ) : (
                  <span className="ml-2 text-sm text-gray-500">({searchResults.length} found)</span>
                )}
              </h2>
              
              {selectedTags.length > 0 && (
                <button
                  onClick={clearAllTags}
                  className="text-sm text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-200"
                >
                  Clear search
                </button>
              )}
            </div>

            {isLoading ? (
              <div className="flex items-center justify-center py-12">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                <span className="ml-3 text-gray-600 dark:text-gray-400">Searching...</span>
              </div>
            ) : searchResults.length === 0 ? (
              <div className="text-center py-12">
                <TagIcon className="h-16 w-16 text-gray-400 mx-auto mb-4" />
                <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
                  No collections found
                </h3>
                <p className="text-gray-500 dark:text-gray-400 mb-4">
                  Try using different tags or changing the search logic (AND/OR)
                </p>
                <button
                  onClick={clearAllTags}
                  className="text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-200"
                >
                  Clear search and try again
                </button>
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {searchResults.map((collection) => (
                  <div key={collection.id} className="bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
                    <div className="flex items-start justify-between mb-3">
                      <div className="flex items-center space-x-3">
                        <span className="text-2xl">{getCollectionIcon(collection.type)}</span>
                        <div>
                          <h3 className="font-semibold text-gray-900 dark:text-white truncate">
                            {collection.name}
                          </h3>
                          <p className="text-sm text-gray-500 dark:text-gray-400 truncate">
                            {collection.path}
                          </p>
                        </div>
                      </div>
                    </div>

                    {/* Statistics */}
                    <div className="flex items-center justify-between text-xs text-gray-500 dark:text-gray-400 mb-3">
                      <div className="flex items-center space-x-3">
                        <span className="flex items-center">
                          <EyeIcon className="h-3 w-3 mr-1" />
                          {collection.view_count || 0}
                        </span>
                        <span className="flex items-center">
                          <ClockIcon className="h-3 w-3 mr-1" />
                          {formatTime(collection.total_view_time || 0)}
                        </span>
                        <span className="flex items-center">
                          <SearchIcon className="h-3 w-3 mr-1" />
                          {collection.search_count || 0}
                        </span>
                        <span className="flex items-center">
                          <TagIcon className="h-3 w-3 mr-1" />
                          {collection.tag_count || 0}
                        </span>
                      </div>
                    </div>

                    {/* Actions */}
                    <div className="flex space-x-2">
                      <Link
                        to={`/collection/${collection.id}`}
                        className="flex-1 bg-blue-600 text-white px-3 py-2 rounded-md text-sm font-medium text-center hover:bg-blue-700 transition-colors"
                      >
                        View Images
                      </Link>
                      <Link
                        to={`/collection/${collection.id}/viewer`}
                        className="flex-1 bg-gray-600 text-white px-3 py-2 rounded-md text-sm font-medium text-center hover:bg-gray-700 transition-colors"
                      >
                        Open Viewer
                      </Link>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default TagSearchPage;
