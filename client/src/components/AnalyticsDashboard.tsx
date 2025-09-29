import React, { useState, useEffect } from 'react';
import {
  ChartBarIcon,
  EyeIcon,
  ClockIcon,
  MagnifyingGlassIcon,
  TagIcon,
  StarIcon,
  TrophyIcon
} from '@heroicons/react/24/outline';
import { statsApi } from '../services/api';
import { Link } from 'react-router-dom';

interface AnalyticsData {
  summary: {
    total_collections: number;
    total_images: number;
    most_viewed_collection: any;
    most_tagged_collection: any;
  };
  popular_collections: Array<{
    id: number;
    name: string;
    view_count: number;
    total_view_time: number;
    search_count: number;
    tag_count: number;
  }>;
  popular_tags: Array<{
    tag: string;
    usage_count: number;
    collection_count: number;
  }>;
}

const AnalyticsDashboard: React.FC = () => {
  const [analytics, setAnalytics] = useState<AnalyticsData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchAnalytics();
  }, []);

  const fetchAnalytics = async () => {
    try {
      setLoading(true);
      const response = await statsApi.getAnalytics();
      setAnalytics(response.data);
      setError(null);
    } catch (err) {
      setError('Failed to load analytics data');
      console.error('Analytics fetch error:', err);
    } finally {
      setLoading(false);
    }
  };

  const formatTime = (seconds: number) => {
    if (seconds < 60) return `${seconds}s`;
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m`;
    return `${Math.floor(seconds / 3600)}h ${Math.floor((seconds % 3600) / 60)}m`;
  };

  const formatNumber = (num: number) => {
    if (num >= 1000000) return `${(num / 1000000).toFixed(1)}M`;
    if (num >= 1000) return `${(num / 1000).toFixed(1)}K`;
    return num.toString();
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
        <p className="text-red-800 dark:text-red-200">{error}</p>
      </div>
    );
  }

  if (!analytics) return null;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="bg-gradient-to-r from-blue-600 to-purple-600 rounded-lg p-6 text-white">
        <div className="flex items-center space-x-3 mb-4">
          <ChartBarIcon className="h-8 w-8" />
          <h1 className="text-2xl font-bold">Analytics Dashboard</h1>
        </div>
        <p className="text-blue-100">Insights into collection usage and popularity</p>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
          <div className="flex items-center">
            <div className="p-2 bg-blue-100 dark:bg-blue-900 rounded-lg">
              <TrophyIcon className="h-6 w-6 text-blue-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm text-gray-500 dark:text-gray-400">Total Collections</p>
              <p className="text-2xl font-bold text-gray-900 dark:text-white">
                {formatNumber(analytics.summary.total_collections)}
              </p>
            </div>
          </div>
        </div>

        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
          <div className="flex items-center">
            <div className="p-2 bg-green-100 dark:bg-green-900 rounded-lg">
              <EyeIcon className="h-6 w-6 text-green-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm text-gray-500 dark:text-gray-400">Total Images</p>
              <p className="text-2xl font-bold text-gray-900 dark:text-white">
                {formatNumber(analytics.summary.total_images)}
              </p>
            </div>
          </div>
        </div>

        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
          <div className="flex items-center">
            <div className="p-2 bg-purple-100 dark:bg-purple-900 rounded-lg">
              <ChartBarIcon className="h-6 w-6 text-purple-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm text-gray-500 dark:text-gray-400">Most Viewed</p>
              <p className="text-lg font-semibold text-gray-900 dark:text-white truncate">
                {analytics.summary.most_viewed_collection?.name || 'N/A'}
              </p>
            </div>
          </div>
        </div>

        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
          <div className="flex items-center">
            <div className="p-2 bg-orange-100 dark:bg-orange-900 rounded-lg">
              <TagIcon className="h-6 w-6 text-orange-600" />
            </div>
            <div className="ml-4">
              <p className="text-sm text-gray-500 dark:text-gray-400">Most Tagged</p>
              <p className="text-lg font-semibold text-gray-900 dark:text-white truncate">
                {analytics.summary.most_tagged_collection?.name || 'N/A'}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Popular Collections */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow">
        <div className="px-6 py-4 border-b border-gray-200 dark:border-gray-700">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-white">Popular Collections</h2>
        </div>
        <div className="p-6">
          <div className="space-y-4">
            {analytics.popular_collections.slice(0, 10).map((collection, index) => (
              <div key={collection.id} className="flex items-center justify-between p-4 bg-gray-50 dark:bg-gray-700 rounded-lg">
                <div className="flex items-center space-x-4">
                  <div className="flex-shrink-0">
                    {index === 0 && <TrophyIcon className="h-6 w-6 text-yellow-500" />}
                    {index === 1 && <StarIcon className="h-6 w-6 text-gray-400" />}
                    {index === 2 && <StarIcon className="h-6 w-6 text-orange-500" />}
                    {index > 2 && <span className="text-lg font-bold text-gray-500">#{index + 1}</span>}
                  </div>
                  <div>
                    <Link 
                      to={`/collections/${collection.id}`}
                      className="text-lg font-medium text-gray-900 dark:text-white hover:text-blue-600 dark:hover:text-blue-400"
                    >
                      {collection.name}
                    </Link>
                    <div className="flex items-center space-x-4 text-sm text-gray-500 dark:text-gray-400 mt-1">
                      <span className="flex items-center">
                        <EyeIcon className="h-4 w-4 mr-1" />
                        {collection.view_count} views
                      </span>
                      <span className="flex items-center">
                        <ClockIcon className="h-4 w-4 mr-1" />
                        {formatTime(collection.total_view_time)}
                      </span>
                      <span className="flex items-center">
                        <MagnifyingGlassIcon className="h-4 w-4 mr-1" />
                        {collection.search_count} searches
                      </span>
                      <span className="flex items-center">
                        <TagIcon className="h-4 w-4 mr-1" />
                        {collection.tag_count} tags
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Popular Tags */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow">
        <div className="px-6 py-4 border-b border-gray-200 dark:border-gray-700">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-white">Popular Tags</h2>
        </div>
        <div className="p-6">
          <div className="flex flex-wrap gap-2">
            {analytics.popular_tags.slice(0, 20).map((tagData, index) => (
              <span
                key={index}
                className={`inline-flex items-center px-3 py-2 rounded-full text-sm font-medium ${
                  index < 5 
                    ? 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200' 
                    : index < 10 
                    ? 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200'
                    : 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200'
                }`}
              >
                <TagIcon className="h-4 w-4 mr-1" />
                {tagData.tag} ({tagData.usage_count} uses, {tagData.collection_count} collections)
              </span>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};

export default AnalyticsDashboard;
