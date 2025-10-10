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
import { useUserSettings, useUpdateUserSettings, useResetUserSettings } from '../hooks/useSettings';

const Settings: React.FC = () => {
  const { isAdmin } = useAuth();
  
  // Fetch user settings from API
  const { data: apiSettings, isLoading, error } = useUserSettings();
  const updateSettingsMutation = useUpdateUserSettings();
  const resetSettingsMutation = useResetUserSettings();
  
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
  });

  // Sync API data with local state when it loads
  useEffect(() => {
    if (apiSettings) {
      setUserSettings({
        theme: apiSettings.displaySettings.theme,
        viewMode: apiSettings.displaySettings.viewMode,
        itemsPerPage: apiSettings.displaySettings.itemsPerPage,
        cardSize: apiSettings.displaySettings.cardSize,
        compactMode: apiSettings.displaySettings.compactMode,
        language: apiSettings.language,
        enableAnimations: apiSettings.displaySettings.enableAnimations,
        emailNotifications: apiSettings.notificationSettings.emailNotifications,
        pushNotifications: apiSettings.notificationSettings.pushNotifications,
        profilePublic: apiSettings.privacySettings.profilePublic,
        analytics: apiSettings.privacySettings.allowAnalytics,
      });
    }
  }, [apiSettings]);

  const tabs = [
    { name: 'User Preferences', icon: User, visible: true },
    { name: 'System Settings', icon: SettingsIcon, visible: isAdmin },
  ].filter(tab => tab.visible);

  const handleSaveSettings = () => {
    updateSettingsMutation.mutate({
      displaySettings: {
        theme: userSettings.theme,
        viewMode: userSettings.viewMode,
        itemsPerPage: userSettings.itemsPerPage,
        cardSize: userSettings.cardSize,
        compactMode: userSettings.compactMode,
        enableAnimations: userSettings.enableAnimations,
      },
      notificationSettings: {
        emailNotifications: userSettings.emailNotifications,
        pushNotifications: userSettings.pushNotifications,
      },
      privacySettings: {
        profilePublic: userSettings.profilePublic,
        allowAnalytics: userSettings.analytics,
      },
      language: userSettings.language,
    });
  };

  const handleResetSettings = () => {
    if (window.confirm('Are you sure you want to reset all settings to default?')) {
      resetSettingsMutation.mutate();
    }
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
                    description="Number of items to show per page"
                    vertical
                  >
                    <select
                      value={userSettings.itemsPerPage}
                      onChange={(e) => setUserSettings({ ...userSettings, itemsPerPage: parseInt(e.target.value) })}
                      className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                    >
                      <option value="10">10</option>
                      <option value="20">20</option>
                      <option value="50">50</option>
                      <option value="100">100</option>
                    </select>
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
                      label="Cache Default Quality"
                      description="Default JPEG quality for cache generation (0-100)"
                      vertical
                    >
                      <div className="space-y-2">
                        <input
                          type="range"
                          min="0"
                          max="100"
                          defaultValue="85"
                          className="w-full h-2 bg-slate-700 rounded-lg appearance-none cursor-pointer accent-blue-600"
                        />
                        <div className="text-sm text-slate-400 text-right">85%</div>
                      </div>
                    </SettingItem>

                    <SettingItem
                      label="Thumbnail Default Size"
                      description="Default thumbnail size in pixels"
                      vertical
                    >
                      <input
                        type="number"
                        defaultValue="300"
                        className="w-full px-3 py-2 bg-slate-700 border border-slate-600 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                      />
                    </SettingItem>
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
                    <Button variant="ghost">
                      Reset to Defaults
                    </Button>
                    <Button variant="primary">
                      Save System Settings
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
