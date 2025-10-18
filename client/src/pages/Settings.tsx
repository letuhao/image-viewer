import { useState, useEffect } from 'react';
import { Tab } from '@headlessui/react';
import { User, Settings as SettingsIcon } from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';
import { cn } from '../lib/utils';
import SettingsSection from '../components/settings/SettingsSection';
import SettingItem from '../components/settings/SettingItem';
import Toggle from '../components/ui/Toggle';
import Button from '../components/ui/Button';
import LoadingSpinner from '../components/ui/LoadingSpinner';
import CacheFolderManager from '../components/settings/CacheFolderManager';
import RedisIndexManagement from '../components/settings/RedisIndexManagement';
import CollectionCleanup from '../components/settings/CollectionCleanup';
import MacOSXCleanup from '../components/settings/MacOSXCleanup';
import { useUserSettings, useUpdateUserSettings, useResetUserSettings, useSystemSettings, useBatchUpdateSystemSettings } from '../hooks/useSettings';
import toast from 'react-hot-toast';

const Settings: React.FC = () => {
  const { isAdmin } = useAuth();
  
  // Fetch user settings from API
  const { data: apiSettings, isLoading, error } = useUserSettings();
  const updateSettingsMutation = useUpdateUserSettings();
  const resetSettingsMutation = useResetUserSettings();
  
  // Fetch system settings from API
  const { data: systemSettingsData, isLoading: systemSettingsLoading } = useSystemSettings();
  const batchUpdateSystemSettings = useBatchUpdateSystemSettings();
  
  // Local state for form (synced with API data)
  const [userSettings, setUserSettings] = useState({
    theme: 'dark',
    viewMode: 'grid',
    itemsPerPage: 20, // Keep for backward compatibility
    // 4 PageSize settings
    collectionsPageSize: 100,
    collectionDetailPageSize: 20,
    sidebarPageSize: 20,
    imageViewerPageSize: 200,
    cardSize: 'medium',
    compactMode: false,
    language: 'en',
    enableAnimations: true,
    emailNotifications: true,
    pushNotifications: true,
    profilePublic: false,
    analytics: true,
    // Collection Detail settings (view mode, card size)
    collectionDetailViewMode: 'grid',
    collectionDetailCardSize: 'medium',
    // Image Viewer settings
    maxPreloadImages: 20,
    // Pagination settings
    paginationShowFirstLast: true,
    paginationShowPageNumbers: true,
    paginationPageNumbersToShow: 5,
  });

  // System settings state
  const [systemSettings, setSystemSettings] = useState({
    cacheFormat: 'jpeg',
    cacheQuality: 85,
    thumbnailFormat: 'jpeg',
    thumbnailQuality: 90,
    thumbnailSize: 300,
  });

  // Sync API data with local state when it loads
  useEffect(() => {
    if (apiSettings) {
      setUserSettings({
        theme: apiSettings.theme,
        viewMode: apiSettings.displayMode,
        itemsPerPage: apiSettings.itemsPerPage, // Keep for backward compatibility
        // 4 PageSize settings from backend
        collectionsPageSize: apiSettings.collectionsPageSize,
        collectionDetailPageSize: apiSettings.collectionDetailPageSize,
        sidebarPageSize: apiSettings.sidebarPageSize,
        imageViewerPageSize: apiSettings.imageViewerPageSize,
        cardSize: 'medium', // Not in backend, use default
        compactMode: false, // Not in backend, use default
        language: apiSettings.language,
        enableAnimations: true, // Not in backend, use default
        emailNotifications: apiSettings.notifications.email,
        pushNotifications: apiSettings.notifications.push,
        profilePublic: apiSettings.privacy.profileVisibility === 'public',
        analytics: apiSettings.privacy.analytics,
        // Collection Detail settings (view mode, card size from localStorage)
        collectionDetailViewMode: localStorage.getItem('collectionDetailViewMode') || 'grid',
        collectionDetailCardSize: localStorage.getItem('collectionDetailCardSize') || 'medium',
        // Image Viewer settings
        maxPreloadImages: parseInt(localStorage.getItem('maxPreloadImages') || '20'),
        // Pagination settings
        paginationShowFirstLast: apiSettings.pagination?.showFirstLast ?? true,
        paginationShowPageNumbers: apiSettings.pagination?.showPageNumbers ?? true,
        paginationPageNumbersToShow: apiSettings.pagination?.pageNumbersToShow ?? 5,
      });
    }
  }, [apiSettings]);

  // Sync system settings from API
  useEffect(() => {
    if (systemSettingsData) {
      const settingsMap = systemSettingsData.reduce((acc, setting) => {
        acc[setting.settingKey] = setting.settingValue;
        return acc;
      }, {} as Record<string, string>);

      setSystemSettings({
        cacheFormat: settingsMap['cache.default.format'] || 'jpeg',
        cacheQuality: parseInt(settingsMap['cache.default.quality'] || '85'),
        thumbnailFormat: settingsMap['thumbnail.default.format'] || 'jpeg',
        thumbnailQuality: parseInt(settingsMap['thumbnail.default.quality'] || '90'),
        thumbnailSize: parseInt(settingsMap['thumbnail.default.size'] || '300'),
      });
    }
  }, [systemSettingsData]);

  const tabs = [
    { name: 'User Preferences', icon: User, visible: true },
    { name: 'System Settings', icon: SettingsIcon, visible: isAdmin },
  ].filter(tab => tab.visible);

  const handleSaveSettings = () => {
    updateSettingsMutation.mutate({
      theme: userSettings.theme,
      displayMode: userSettings.viewMode,
      itemsPerPage: userSettings.itemsPerPage, // Keep for backward compatibility
      collectionsPageSize: userSettings.collectionsPageSize,
      collectionDetailPageSize: userSettings.collectionDetailPageSize,
      sidebarPageSize: userSettings.sidebarPageSize,
      imageViewerPageSize: userSettings.imageViewerPageSize,
      language: userSettings.language,
      notifications: {
        email: userSettings.emailNotifications,
        push: userSettings.pushNotifications,
      },
      privacy: {
        profileVisibility: userSettings.profilePublic ? 'public' : 'private',
        analytics: userSettings.analytics,
      },
      pagination: {
        showFirstLast: userSettings.paginationShowFirstLast,
        showPageNumbers: userSettings.paginationShowPageNumbers,
        pageNumbersToShow: userSettings.paginationPageNumbersToShow,
      },
    });
  };

  const handleResetSettings = () => {
    if (window.confirm('Are you sure you want to reset all settings to default?')) {
      resetSettingsMutation.mutate();
    }
  };

  const handleSaveSystemSettings = () => {
    // Use batch update mutation for system settings
    const settings = [
      { key: 'cache.default.format', value: systemSettings.cacheFormat },
      { key: 'cache.default.quality', value: systemSettings.cacheQuality.toString() },
      { key: 'thumbnail.default.format', value: systemSettings.thumbnailFormat },
      { key: 'thumbnail.default.quality', value: systemSettings.thumbnailQuality.toString() },
      { key: 'thumbnail.default.size', value: systemSettings.thumbnailSize.toString() },
    ];

    batchUpdateSystemSettings.mutate({ settings });
  };

  if (isLoading) {
    return <LoadingSpinner text="Loading settings..." />;
  }

  if (error) {
    return (
      <div className="h-full flex items-center justify-center">
        <div className="text-center">
          <p className="text-red-500 text-lg">Failed to load settings</p>
          <p className="text-slate-400 text-sm mt-2">{error.message}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col">
      {/* Header */}
      <div className="flex-shrink-0 border-b border-slate-800 bg-slate-900/50 backdrop-blur">
        <div className="px-6 py-4">
          <h1 className="text-2xl font-bold text-white">Settings</h1>
          <p className="text-sm text-slate-400 mt-1">Manage your account and application preferences</p>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-y-auto">
        <div className="max-w-5xl mx-auto px-6 py-6">
          <Tab.Group>
            {/* Tabs */}
            <Tab.List className="flex space-x-2 bg-slate-800/50 p-1 rounded-lg mb-6">
              {tabs.map((tab) => {
                const Icon = tab.icon;
                return (
                  <Tab
                    key={tab.name}
                    className={({ selected }) =>
                      cn(
                        'flex items-center space-x-2 px-4 py-2.5 rounded-md text-sm font-medium transition-all',
                        'focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 focus:ring-offset-slate-900',
                        selected
                          ? 'bg-blue-600 text-white shadow-lg'
                          : 'text-slate-400 hover:text-white hover:bg-slate-700'
                      )
                    }
                  >
                    <Icon className="h-4 w-4" />
                    <span>{tab.name}</span>
                  </Tab>
                );
              })}
            </Tab.List>

            {/* Tab Panels */}
            <Tab.Panels>
              {/* User Preferences Tab */}
              <Tab.Panel className="space-y-6">
                {/* Display Settings */}
                <SettingsSection
                  title="Display Preferences"
                  description="Customize how you view and interact with content"
                >
                  <SettingItem
                    label="Theme"
                    description="Choose your preferred color scheme"
                    vertical
                  >
                    <select
                      value={userSettings.theme}
                      onChange={(e) => setUserSettings({ ...userSettings, theme: e.target.value })}
                      className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                    >
                      <option value="light">Light</option>
                      <option value="dark">Dark</option>
                      <option value="auto">Auto (System)</option>
                    </select>
                  </SettingItem>

                  <SettingItem
                    label="View Mode"
                    description="Default view mode for collections"
                    vertical
                  >
                    <div className="grid grid-cols-3 gap-2">
                      {['grid', 'list', 'detail'].map((mode) => (
                        <button
                          key={mode}
                          onClick={() => setUserSettings({ ...userSettings, viewMode: mode })}
                          className={cn(
                            'px-3 py-2 rounded-lg text-sm font-medium capitalize transition-all',
                            userSettings.viewMode === mode
                              ? 'bg-blue-600 text-white'
                              : 'bg-slate-700 text-slate-300 hover:bg-slate-600'
                          )}
                        >
                          {mode}
                        </button>
                      ))}
                    </div>
                  </SettingItem>

                  <SettingItem
                    label="Enable Animations"
                    description="Show smooth transitions and animations"
                  >
                    <Toggle
                      enabled={userSettings.enableAnimations}
                      onChange={(enabled) => setUserSettings({ ...userSettings, enableAnimations: enabled })}
                    />
                  </SettingItem>
                </SettingsSection>

                {/* Pagination Settings */}
                <SettingsSection
                  title="Page Sizes"
                  description="Configure number of items per page for different screens"
                >
                  <SettingItem
                    label="Collections List Page Size"
                    description="Number of collections to show per page in collections list (1-1000)"
                    vertical
                  >
                    <input
                      type="number"
                      min="1"
                      max="1000"
                      value={userSettings.collectionsPageSize}
                      onChange={(e) => setUserSettings({ ...userSettings, collectionsPageSize: parseInt(e.target.value) || 100 })}
                      className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                    />
                  </SettingItem>

                  <SettingItem
                    label="Collection Detail Page Size"
                    description="Number of images to show per page in collection detail (1-1000)"
                    vertical
                  >
                    <input
                      type="number"
                      min="1"
                      max="1000"
                      value={userSettings.collectionDetailPageSize}
                      onChange={(e) => setUserSettings({ ...userSettings, collectionDetailPageSize: parseInt(e.target.value) || 20 })}
                      className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                    />
                  </SettingItem>

                  <SettingItem
                    label="Sidebar Page Size"
                    description="Number of collections to show in sidebar (1-100)"
                    vertical
                  >
                    <input
                      type="number"
                      min="1"
                      max="100"
                      value={userSettings.sidebarPageSize}
                      onChange={(e) => setUserSettings({ ...userSettings, sidebarPageSize: parseInt(e.target.value) || 20 })}
                      className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                    />
                  </SettingItem>

                  <SettingItem
                    label="Image Viewer Page Size"
                    description="Number of images to load initially in image viewer (50-1000)"
                    vertical
                  >
                    <input
                      type="number"
                      min="50"
                      max="1000"
                      value={userSettings.imageViewerPageSize}
                      onChange={(e) => setUserSettings({ ...userSettings, imageViewerPageSize: parseInt(e.target.value) || 200 })}
                      className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                    />
                  </SettingItem>
                </SettingsSection>

                {/* Notification Settings */}
                <SettingsSection
                  title="Notifications"
                  description="Configure how you receive notifications"
                >
                  <SettingItem
                    label="Email Notifications"
                    description="Receive notifications via email"
                  >
                    <Toggle
                      enabled={userSettings.emailNotifications}
                      onChange={(enabled) => setUserSettings({ ...userSettings, emailNotifications: enabled })}
                    />
                  </SettingItem>

                  <SettingItem
                    label="Push Notifications"
                    description="Receive push notifications in browser"
                  >
                    <Toggle
                      enabled={userSettings.pushNotifications}
                      onChange={(enabled) => setUserSettings({ ...userSettings, pushNotifications: enabled })}
                    />
                  </SettingItem>
                </SettingsSection>

                {/* Privacy Settings */}
                <SettingsSection
                  title="Privacy & Security"
                  description="Control your privacy and data sharing preferences"
                >
                  <SettingItem
                    label="Public Profile"
                    description="Make your profile visible to other users"
                  >
                    <Toggle
                      enabled={userSettings.profilePublic}
                      onChange={(enabled) => setUserSettings({ ...userSettings, profilePublic: enabled })}
                    />
                  </SettingItem>

                  <SettingItem
                    label="Analytics"
                    description="Help improve the app by sharing anonymous usage data"
                  >
                    <Toggle
                      enabled={userSettings.analytics}
                      onChange={(enabled) => setUserSettings({ ...userSettings, analytics: enabled })}
                    />
                  </SettingItem>
                </SettingsSection>

                {/* Collection Detail Settings */}
                <SettingsSection
                  title="Collection Detail Page"
                  description="Customize how collection detail pages are displayed"
                >
                  <SettingItem
                    label="Default View Mode"
                    description="Default view mode for collection detail pages"
                    vertical
                  >
                    <select
                      value={userSettings.collectionDetailViewMode}
                      onChange={(e) => {
                        const newValue = e.target.value;
                        setUserSettings({ ...userSettings, collectionDetailViewMode: newValue });
                        localStorage.setItem('collectionDetailViewMode', newValue);
                      }}
                      className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                    >
                      <option value="grid">Grid View</option>
                      <option value="list">List View</option>
                      <option value="detail">Detail View</option>
                    </select>
                  </SettingItem>

                  <SettingItem
                    label="Default Card Size"
                    description="Default card size for grid view in collection detail pages"
                    vertical
                  >
                    <select
                      value={userSettings.collectionDetailCardSize}
                      onChange={(e) => {
                        const newValue = e.target.value;
                        setUserSettings({ ...userSettings, collectionDetailCardSize: newValue });
                        localStorage.setItem('collectionDetailCardSize', newValue);
                      }}
                      className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                    >
                      <option value="mini">Mini</option>
                      <option value="tiny">Tiny</option>
                      <option value="small">Small</option>
                      <option value="medium">Medium</option>
                      <option value="large">Large</option>
                      <option value="xlarge">XLarge</option>
                    </select>
                  </SettingItem>

                </SettingsSection>

                {/* Image Viewer Settings */}
                <SettingsSection
                  title="Image Viewer"
                  description="Configure image viewer behavior and performance"
                >
                  <SettingItem
                    label="Max Preload Images"
                    description="Number of images to preload in background (higher = faster navigation, more memory)"
                    vertical
                  >
                    <input
                      type="number"
                      min="1"
                      max="100"
                      value={userSettings.maxPreloadImages}
                      onChange={(e) => {
                        const newValue = parseInt(e.target.value) || 20;
                        if (newValue >= 1 && newValue <= 100) {
                          setUserSettings({ ...userSettings, maxPreloadImages: newValue });
                          localStorage.setItem('maxPreloadImages', newValue.toString());
                        }
                      }}
                      className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                      placeholder="20"
                    />
                  </SettingItem>
                </SettingsSection>

                {/* Pagination Settings */}
                <SettingsSection
                  title="Pagination Controls"
                  description="Customize pagination controls in collection lists and detail pages"
                >
                  <SettingItem
                    label="Show First/Last Buttons"
                    description="Display buttons to jump to first and last page"
                  >
                    <Toggle
                      enabled={userSettings.paginationShowFirstLast}
                      onChange={(enabled) => setUserSettings({ ...userSettings, paginationShowFirstLast: enabled })}
                    />
                  </SettingItem>

                  <SettingItem
                    label="Show Page Numbers"
                    description="Display clickable page numbers for quick navigation"
                  >
                    <Toggle
                      enabled={userSettings.paginationShowPageNumbers}
                      onChange={(enabled) => setUserSettings({ ...userSettings, paginationShowPageNumbers: enabled })}
                    />
                  </SettingItem>

                  <SettingItem
                    label="Page Numbers to Show"
                    description="How many page numbers to display on each side of current page (1-10)"
                    vertical
                  >
                    <input
                      type="number"
                      min="1"
                      max="10"
                      value={userSettings.paginationPageNumbersToShow}
                      onChange={(e) => {
                        const newValue = parseInt(e.target.value) || 5;
                        if (newValue >= 1 && newValue <= 10) {
                          setUserSettings({ ...userSettings, paginationPageNumbersToShow: newValue });
                        }
                      }}
                      className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                      placeholder="5"
                    />
                    <p className="text-xs text-slate-500 mt-1">
                      Example with 5: [1] [...] [45][46][47][48][49][50][51][52][53][54][55] [...] [100]
                    </p>
                  </SettingItem>
                </SettingsSection>

                {/* Save Button */}
                <div className="flex justify-end space-x-3">
                  <Button 
                    variant="ghost" 
                    onClick={handleResetSettings}
                    disabled={resetSettingsMutation.isPending}
                  >
                    {resetSettingsMutation.isPending ? 'Resetting...' : 'Reset to Defaults'}
                  </Button>
                  <Button 
                    variant="primary" 
                    onClick={handleSaveSettings}
                    disabled={updateSettingsMutation.isPending}
                  >
                    {updateSettingsMutation.isPending ? 'Saving...' : 'Save Changes'}
                  </Button>
                </div>
              </Tab.Panel>

              {/* System Settings Tab (Admin Only) */}
              {isAdmin && (
                <Tab.Panel className="space-y-6">
                  <SettingsSection
                    title="Image Processing"
                    description="Global image processing configuration"
                  >
                    <SettingItem
                      label="Cache Format"
                      description="Image format for cache generation (JPEG = smaller, PNG = lossless, WebP = modern)"
                      vertical
                    >
                      <select
                        value={systemSettings.cacheFormat}
                        onChange={(e) => setSystemSettings({ ...systemSettings, cacheFormat: e.target.value })}
                        className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                      >
                        <option value="jpeg">JPEG (Best compatibility)</option>
                        <option value="png">PNG (Lossless quality)</option>
                        <option value="webp">WebP (Best compression)</option>
                      </select>
                    </SettingItem>

                    <SettingItem
                      label="Cache Default Quality"
                      description="Quality for cache generation (0-100, higher = better quality but larger file)"
                      vertical
                    >
                      <div className="space-y-2">
                        <input
                          type="range"
                          min="0"
                          max="100"
                          value={systemSettings.cacheQuality}
                          onChange={(e) => setSystemSettings({ ...systemSettings, cacheQuality: parseInt(e.target.value) })}
                          className="w-full h-2 bg-slate-700 rounded-lg appearance-none cursor-pointer accent-blue-600"
                        />
                        <div className="text-sm text-slate-400 text-right">{systemSettings.cacheQuality}%</div>
                      </div>
                    </SettingItem>

                    <SettingItem
                      label="Thumbnail Format"
                      description="Image format for thumbnail generation"
                      vertical
                    >
                      <select
                        value={systemSettings.thumbnailFormat}
                        onChange={(e) => setSystemSettings({ ...systemSettings, thumbnailFormat: e.target.value })}
                        className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                      >
                        <option value="jpeg">JPEG (Best compatibility)</option>
                        <option value="png">PNG (Lossless quality)</option>
                        <option value="webp">WebP (Best compression)</option>
                      </select>
                    </SettingItem>

                    <SettingItem
                      label="Thumbnail Quality"
                      description="Quality for thumbnail generation (0-100)"
                      vertical
                    >
                      <div className="space-y-2">
                        <input
                          type="range"
                          min="0"
                          max="100"
                          value={systemSettings.thumbnailQuality}
                          onChange={(e) => setSystemSettings({ ...systemSettings, thumbnailQuality: parseInt(e.target.value) })}
                          className="w-full h-2 bg-slate-700 rounded-lg appearance-none cursor-pointer accent-blue-600"
                        />
                        <div className="text-sm text-slate-400 text-right">{systemSettings.thumbnailQuality}%</div>
                      </div>
                    </SettingItem>

                    <SettingItem
                      label="Thumbnail Default Size"
                      description="Default thumbnail size in pixels"
                      vertical
                    >
                      <input
                        type="number"
                        value={systemSettings.thumbnailSize}
                        onChange={(e) => setSystemSettings({ ...systemSettings, thumbnailSize: parseInt(e.target.value) })}
                        className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                      />
                    </SettingItem>
                  </SettingsSection>

                  <SettingsSection
                    title="Cache Folders"
                    description="Manage distributed cache storage locations with real-time disk health monitoring"
                  >
                    <CacheFolderManager />
                  </SettingsSection>

                  <SettingsSection
                    title="Redis Cache"
                    description="Redis caching configuration"
                  >
                    <SettingItem
                      label="Default Expiration"
                      description="Default cache expiration in minutes"
                      vertical
                    >
                      <input
                        type="number"
                        defaultValue="60"
                        className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                      />
                    </SettingItem>

                    <SettingItem
                      label="Image Cache Expiration"
                      description="Image cache expiration in minutes (longer for frequently accessed images)"
                      vertical
                    >
                      <input
                        type="number"
                        defaultValue="120"
                        className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                      />
                    </SettingItem>

                    <SettingItem
                      label="Enable Compression"
                      description="Compress images in Redis cache (GZip)"
                    >
                      <Toggle enabled={true} onChange={() => {}} />
                    </SettingItem>
                  </SettingsSection>

                  <RedisIndexManagement />

                  <SettingsSection
                    title="Collection Management"
                    description="Manage and maintain your collection database"
                  >
                    <CollectionCleanup />
                  </SettingsSection>

                  <SettingsSection
                    title="System Maintenance"
                    description="Clean up system files and metadata"
                  >
                    <MacOSXCleanup />
                  </SettingsSection>

                  <SettingsSection
                    title="Metadata Recalculation"
                    description="Recalculate cache folder and library metadata based on actual data"
                  >
                    <div className="space-y-4">
                      <div className="bg-slate-800/50 rounded-lg p-4 border border-slate-700">
                        <h4 className="text-lg font-semibold text-white mb-2">Cache Folder Metadata</h4>
                        <p className="text-sm text-slate-400 mb-4">
                          Recalculate cache folder statistics based on actual files and collection data. 
                          This will update file counts, sizes, and collection bindings.
                        </p>
                        <Button
                          variant="secondary"
                          onClick={async () => {
                            try {
                              const response = await fetch('/api/v1/admin/recalculate-cache-folder-metadata', {
                                method: 'POST',
                                headers: {
                                  'Authorization': `Bearer ${localStorage.getItem('auth_token')}`,
                                  'Content-Type': 'application/json',
                                },
                              });
                              
                              if (response.ok) {
                                const result = await response.json();
                                toast.success(`Cache folder metadata recalculated! Processed ${result.processedItems}/${result.totalItems} items in ${result.duration?.totalMilliseconds?.toFixed(0)}ms`);
                                if (result.errors?.length > 0) {
                                  console.warn('Recalculation completed with errors:', result.errors);
                                }
                              } else {
                                throw new Error('Failed to recalculate cache folder metadata');
                              }
                            } catch (error) {
                              console.error('Error recalculating cache folder metadata:', error);
                              toast.error('Failed to recalculate cache folder metadata');
                            }
                          }}
                          className="w-full"
                        >
                          Recalculate Cache Folder Metadata
                        </Button>
                      </div>

                      <div className="bg-slate-800/50 rounded-lg p-4 border border-slate-700">
                        <h4 className="text-lg font-semibold text-white mb-2">Library Metadata</h4>
                        <p className="text-sm text-slate-400 mb-4">
                          Recalculate library statistics based on collection data. 
                          This will update total collections, media items, sizes, and activity metrics.
                        </p>
                        <Button
                          variant="secondary"
                          onClick={async () => {
                            try {
                              const response = await fetch('/api/v1/admin/recalculate-library-metadata', {
                                method: 'POST',
                                headers: {
                                  'Authorization': `Bearer ${localStorage.getItem('auth_token')}`,
                                  'Content-Type': 'application/json',
                                },
                              });
                              
                              if (response.ok) {
                                const result = await response.json();
                                toast.success(`Library metadata recalculated! Processed ${result.processedItems}/${result.totalItems} items in ${result.duration?.totalMilliseconds?.toFixed(0)}ms`);
                                if (result.errors?.length > 0) {
                                  console.warn('Recalculation completed with errors:', result.errors);
                                }
                              } else {
                                throw new Error('Failed to recalculate library metadata');
                              }
                            } catch (error) {
                              console.error('Error recalculating library metadata:', error);
                              toast.error('Failed to recalculate library metadata');
                            }
                          }}
                          className="w-full"
                        >
                          Recalculate Library Metadata
                        </Button>
                      </div>

                      <div className="bg-slate-800/50 rounded-lg p-4 border border-slate-700">
                        <h4 className="text-lg font-semibold text-white mb-2">Complete Metadata Recalculation</h4>
                        <p className="text-sm text-slate-400 mb-4">
                          Recalculate both cache folder and library metadata. 
                          This is a comprehensive operation that may take several minutes.
                        </p>
                        <Button
                          variant="primary"
                          onClick={async () => {
                            if (!window.confirm('This will recalculate all metadata and may take several minutes. Continue?')) {
                              return;
                            }
                            
                            try {
                              const response = await fetch('/api/v1/admin/recalculate-all-metadata', {
                                method: 'POST',
                                headers: {
                                  'Authorization': `Bearer ${localStorage.getItem('auth_token')}`,
                                  'Content-Type': 'application/json',
                                },
                              });
                              
                              if (response.ok) {
                                const result = await response.json();
                                toast.success(`Complete metadata recalculation finished! Processed ${result.processedItems}/${result.totalItems} items in ${result.duration?.totalMilliseconds?.toFixed(0)}ms`);
                                if (result.errors?.length > 0) {
                                  console.warn('Recalculation completed with errors:', result.errors);
                                }
                              } else {
                                throw new Error('Failed to recalculate all metadata');
                              }
                            } catch (error) {
                              console.error('Error recalculating all metadata:', error);
                              toast.error('Failed to recalculate all metadata');
                            }
                          }}
                          className="w-full"
                        >
                          Recalculate All Metadata
                        </Button>
                      </div>
                    </div>
                  </SettingsSection>

                  <SettingsSection
                    title="Animated File Cache Repair"
                    description="Fix incorrectly cached animated files (GIF, WebP, etc.) and regenerate their caches"
                  >
                    <div className="space-y-4">
                      <div className="bg-slate-800/50 rounded-lg p-4 border border-slate-700">
                        <h4 className="text-lg font-semibold text-white mb-2">🎬 Scan for Incorrect Animated Caches</h4>
                        <p className="text-sm text-slate-400 mb-4">
                          Scan all collections to find animated files (GIF, WebP) that have been incorrectly converted to static cache images.
                          This will identify files that need to be regenerated.
                        </p>
                        <Button
                          onClick={async () => {
                            try {
                              toast.info('Scanning for incorrectly cached animated files...');
                              const response = await fetch(`${config.apiBaseUrl}/animatedcache/scan`, {
                                method: 'GET',
                                headers: {
                                  'Authorization': `Bearer ${localStorage.getItem('auth_token')}`,
                                },
                              });
                              
                              if (response.ok) {
                                const result = await response.json();
                                if (result.incorrectlyCachedFiles > 0) {
                                  toast.warning(
                                    `Found ${result.incorrectlyCachedFiles} incorrectly cached animated files out of ${result.animatedFilesFound} total animated files. ` +
                                    `Scanned ${result.totalImages} images in ${result.totalCollections} collections.`,
                                    { duration: 8000 }
                                  );
                                } else {
                                  toast.success(
                                    `All ${result.animatedFilesFound} animated files are correctly cached! ` +
                                    `Scanned ${result.totalImages} images in ${result.totalCollections} collections.`
                                  );
                                }
                              } else {
                                throw new Error('Failed to scan animated caches');
                              }
                            } catch (error) {
                              console.error('Error scanning animated caches:', error);
                              toast.error('Failed to scan for incorrectly cached animated files');
                            }
                          }}
                          className="w-full"
                        >
                          Scan Animated File Caches
                        </Button>
                      </div>

                      <div className="bg-slate-800/50 rounded-lg p-4 border border-slate-700">
                        <h4 className="text-lg font-semibold text-white mb-2">🔧 Repair Incorrect Animated Caches</h4>
                        <p className="text-sm text-slate-400 mb-4">
                          Automatically find and repair all animated files that have been incorrectly converted to static images.
                          This will queue the files for cache regeneration with their original format.
                        </p>
                        <Button
                          onClick={async () => {
                            try {
                              toast.info('Repairing incorrectly cached animated files...');
                              const response = await fetch(`${config.apiBaseUrl}/animatedcache/repair?forceRegenerate=true`, {
                                method: 'POST',
                                headers: {
                                  'Authorization': `Bearer ${localStorage.getItem('auth_token')}`,
                                },
                              });
                              
                              if (response.ok) {
                                const result = await response.json();
                                if (result.filesQueuedForRepair > 0) {
                                  toast.success(
                                    `Successfully queued ${result.filesQueuedForRepair} animated files for repair! ` +
                                    `Found ${result.incorrectlyCachedFiles} incorrect files out of ${result.animatedFilesFound} total animated files.`,
                                    { duration: 8000 }
                                  );
                                } else {
                                  toast.info('No incorrect animated caches found to repair.');
                                }
                              } else {
                                throw new Error('Failed to repair animated caches');
                              }
                            } catch (error) {
                              console.error('Error repairing animated caches:', error);
                              toast.error('Failed to repair incorrectly cached animated files');
                            }
                          }}
                          className="w-full"
                        >
                          Repair Incorrectly Cached Animated Files
                        </Button>
                      </div>

                      <div className="bg-slate-800/50 rounded-lg p-4 border border-slate-700">
                        <h4 className="text-lg font-semibold text-white mb-2">🔄 Regenerate All Animated File Caches</h4>
                        <p className="text-sm text-slate-400 mb-4">
                          Force regenerate cache files for ALL animated files (GIF, WebP, videos) in all collections.
                          Use this to ensure all animated files are cached correctly with their original format.
                          <span className="block mt-2 text-yellow-400">
                            ⚠️ Warning: This will queue all animated files for regeneration, which may take a long time.
                          </span>
                        </p>
                        <Button
                          onClick={async () => {
                            if (!window.confirm('Are you sure you want to regenerate all animated file caches? This may take a long time.')) {
                              return;
                            }
                            
                            try {
                              toast.info('Regenerating all animated file caches...');
                              const response = await fetch(`${config.apiBaseUrl}/animatedcache/regenerate-all`, {
                                method: 'POST',
                                headers: {
                                  'Authorization': `Bearer ${localStorage.getItem('auth_token')}`,
                                },
                              });
                              
                              if (response.ok) {
                                const result = await response.json();
                                toast.success(
                                  `Successfully queued ${result.queuedCount} animated files for cache regeneration!`,
                                  { duration: 8000 }
                                );
                              } else {
                                throw new Error('Failed to regenerate animated caches');
                              }
                            } catch (error) {
                              console.error('Error regenerating animated caches:', error);
                              toast.error('Failed to regenerate animated file caches');
                            }
                          }}
                          className="w-full bg-yellow-600 hover:bg-yellow-700"
                        >
                          Regenerate All Animated File Caches
                        </Button>
                      </div>
                    </div>
                  </SettingsSection>

                  {/* Save Button */}
                  <div className="flex justify-end space-x-3">
                    <Button 
                      variant="ghost" 
                      onClick={() => {
                        setSystemSettings({
                          cacheFormat: 'jpeg',
                          cacheQuality: 85,
                          thumbnailFormat: 'jpeg',
                          thumbnailQuality: 90,
                          thumbnailSize: 300,
                        });
                        toast.success('System settings reset to defaults!');
                      }}
                    >
                      Reset to Defaults
                    </Button>
                    <Button 
                      variant="primary" 
                      onClick={handleSaveSystemSettings}
                      disabled={batchUpdateSystemSettings.isPending}
                    >
                      {batchUpdateSystemSettings.isPending ? 'Saving...' : 'Save System Settings'}
                    </Button>
                  </div>
                </Tab.Panel>
              )}
            </Tab.Panels>
          </Tab.Group>
        </div>
      </div>
    </div>
  );
};

export default Settings;
