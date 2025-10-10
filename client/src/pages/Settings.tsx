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
    itemsPerPage: 20,
    cardSize: 'medium',
    compactMode: false,
    language: 'en',
    enableAnimations: true,
    emailNotifications: true,
    pushNotifications: true,
    profilePublic: false,
    analytics: true,
    // Collection Detail settings
    collectionDetailViewMode: 'grid',
    collectionDetailCardSize: 'medium',
    collectionDetailPageSize: 20,
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
        itemsPerPage: apiSettings.itemsPerPage,
        cardSize: 'medium', // Not in backend, use default
        compactMode: false, // Not in backend, use default
        language: apiSettings.language,
        enableAnimations: true, // Not in backend, use default
        emailNotifications: apiSettings.notifications.email,
        pushNotifications: apiSettings.notifications.push,
        profilePublic: apiSettings.privacy.profileVisibility === 'public',
        analytics: apiSettings.privacy.analytics,
        // Collection Detail settings (from localStorage for now)
        collectionDetailViewMode: localStorage.getItem('collectionDetailViewMode') || 'grid',
        collectionDetailCardSize: localStorage.getItem('collectionDetailCardSize') || 'medium',
        collectionDetailPageSize: parseInt(localStorage.getItem('collectionDetailPageSize') || '20'),
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
      itemsPerPage: userSettings.itemsPerPage,
      language: userSettings.language,
      notifications: {
        email: userSettings.emailNotifications,
        push: userSettings.pushNotifications,
      },
      privacy: {
        profileVisibility: userSettings.profilePublic ? 'public' : 'private',
        analytics: userSettings.analytics,
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
                    label="Items Per Page"
                    description="Number of items to show per page (1-1000)"
                    vertical
                  >
                    <input
                      type="number"
                      min="1"
                      max="1000"
                      value={userSettings.itemsPerPage}
                      onChange={(e) => setUserSettings({ ...userSettings, itemsPerPage: parseInt(e.target.value) || 20 })}
                      className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                    />
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

                  <SettingItem
                    label="Default Page Size"
                    description="Default number of items per page in collection detail pages"
                    vertical
                  >
                    <select
                      value={userSettings.collectionDetailPageSize}
                      onChange={(e) => {
                        const newValue = parseInt(e.target.value);
                        setUserSettings({ ...userSettings, collectionDetailPageSize: newValue });
                        localStorage.setItem('collectionDetailPageSize', newValue.toString());
                      }}
                      className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                    >
                      <option value={5}>5 items</option>
                      <option value={10}>10 items</option>
                      <option value={20}>20 items</option>
                      <option value={50}>50 items</option>
                      <option value={100}>100 items</option>
                    </select>
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
