import React, { useState, useEffect } from 'react';
import { 
  TagIcon, 
  FunnelIcon, 
  XMarkIcon, 
  AdjustmentsHorizontalIcon
} from '@heroicons/react/24/outline';
import TagAutocomplete from './TagAutocomplete';
import { statsApi } from '../services/api';

interface TagFilterProps {
  onFilterChange: (tags: string[], operator: 'AND' | 'OR') => void;
  className?: string;
}

const TagFilter: React.FC<TagFilterProps> = ({ onFilterChange, className = "" }) => {
  const [selectedTags, setSelectedTags] = useState<string[]>([]);
  const [operator, setOperator] = useState<'AND' | 'OR'>('AND');
  const [showFilter, setShowFilter] = useState(false);
  const [popularTags, setPopularTags] = useState<Array<{
    tag: string;
    usage_count: number;
    collection_count: number;
  }>>([]);

  useEffect(() => {
    fetchPopularTags();
  }, []);

  useEffect(() => {
    onFilterChange(selectedTags, operator);
  }, [selectedTags, operator, onFilterChange]);

  const fetchPopularTags = async () => {
    try {
      const response = await statsApi.getPopularTags(20);
      setPopularTags(response.data.tags);
    } catch (error) {
      console.error('Error fetching popular tags:', error);
    }
  };

  const handleTagSelect = (tag: string) => {
    if (!selectedTags.includes(tag)) {
      setSelectedTags([...selectedTags, tag]);
    }
  };

  const handleTagRemove = (tagToRemove: string) => {
    setSelectedTags(selectedTags.filter(tag => tag !== tagToRemove));
  };

  const clearAllTags = () => {
    setSelectedTags([]);
  };

  const getOperatorDescription = () => {
    return operator === 'AND' 
      ? 'Show collections that have ALL selected tags'
      : 'Show collections that have ANY of the selected tags';
  };

  return (
    <div className={`${className}`}>
      {/* Filter Toggle */}
      <div className="flex items-center space-x-2 mb-4">
        <button
          onClick={() => setShowFilter(!showFilter)}
          className={`flex items-center px-3 py-2 rounded-md text-sm font-medium transition-colors ${
            showFilter || selectedTags.length > 0
              ? 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-200'
              : 'bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'
          }`}
        >
          <FunnelIcon className="h-4 w-4 mr-2" />
          Filter by Tags
          {selectedTags.length > 0 && (
            <span className="ml-2 px-2 py-0.5 bg-blue-600 text-white text-xs rounded-full">
              {selectedTags.length}
            </span>
          )}
        </button>

        {selectedTags.length > 0 && (
          <button
            onClick={clearAllTags}
            className="flex items-center px-2 py-2 text-sm text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"
            title="Clear all tags"
          >
            <XMarkIcon className="h-4 w-4" />
          </button>
        )}
      </div>

      {/* Filter Panel */}
      {showFilter && (
        <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4 mb-4">
          <div className="space-y-4">
            {/* Selected Tags */}
            {selectedTags.length > 0 && (
              <div>
                <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-2">
                  Selected Tags ({selectedTags.length})
                </h4>
                <div className="flex flex-wrap gap-2">
                  {selectedTags.map((tag, index) => (
                    <span
                      key={index}
                      className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200"
                    >
                      <TagIcon className="h-3 w-3 mr-1" />
                      {tag}
                      <button
                        onClick={() => handleTagRemove(tag)}
                        className="ml-2 hover:bg-blue-200 dark:hover:bg-blue-800 rounded-full p-0.5"
                      >
                        <XMarkIcon className="h-3 w-3" />
                      </button>
                    </span>
                  ))}
                </div>
              </div>
            )}

            {/* Tag Search */}
            <div>
              <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-2">
                Add Tags
              </h4>
              <TagAutocomplete
                selectedTags={selectedTags}
                onTagsChange={setSelectedTags}
                placeholder="Search and add tags..."
                maxTags={10}
              />
            </div>

            {/* Operator Selection */}
            {selectedTags.length > 1 && (
              <div>
                <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-2">
                  Filter Logic
                </h4>
                <div className="space-y-2">
                  <div className="flex items-center space-x-2">
                    <input
                      type="radio"
                      id="operator-and"
                      name="operator"
                      value="AND"
                      checked={operator === 'AND'}
                      onChange={(e) => setOperator(e.target.value as 'AND' | 'OR')}
                      className="focus:ring-blue-500 h-4 w-4 text-blue-600"
                    />
                    <label htmlFor="operator-and" className="text-sm text-gray-700 dark:text-gray-300">
                      <strong>AND</strong> - {getOperatorDescription()}
                    </label>
                  </div>
                  <div className="flex items-center space-x-2">
                    <input
                      type="radio"
                      id="operator-or"
                      name="operator"
                      value="OR"
                      checked={operator === 'OR'}
                      onChange={(e) => setOperator(e.target.value as 'AND' | 'OR')}
                      className="focus:ring-blue-500 h-4 w-4 text-blue-600"
                    />
                    <label htmlFor="operator-or" className="text-sm text-gray-700 dark:text-gray-300">
                      <strong>OR</strong> - {getOperatorDescription()}
                    </label>
                  </div>
                </div>
              </div>
            )}

            {/* Popular Tags */}
            {popularTags.length > 0 && (
              <div>
                <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-2">
                  Popular Tags
                </h4>
                <div className="flex flex-wrap gap-2">
                  {popularTags.slice(0, 10).map((tagData, index) => (
                    <button
                      key={index}
                      onClick={() => handleTagSelect(tagData.tag)}
                      disabled={selectedTags.includes(tagData.tag)}
                      className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium transition-colors ${
                        selectedTags.includes(tagData.tag)
                          ? 'bg-gray-200 text-gray-500 dark:bg-gray-600 dark:text-gray-400 cursor-not-allowed'
                          : 'bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'
                      }`}
                    >
                      <TagIcon className="h-3 w-3 mr-1" />
                      {tagData.tag} ({tagData.usage_count})
                    </button>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Active Filter Summary */}
      {selectedTags.length > 0 && (
        <div className="flex items-center justify-between bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-3 mb-4">
          <div className="flex items-center space-x-2">
            <AdjustmentsHorizontalIcon className="h-4 w-4 text-blue-600" />
            <span className="text-sm text-blue-800 dark:text-blue-200">
              Filtering by: <strong>{selectedTags.join(` ${operator} `)}</strong>
            </span>
          </div>
          <button
            onClick={clearAllTags}
            className="text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-200"
          >
            <XMarkIcon className="h-4 w-4" />
          </button>
        </div>
      )}
    </div>
  );
};

export default TagFilter;
