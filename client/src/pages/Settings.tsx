import { Settings as SettingsIcon } from 'lucide-react';

/**
 * Settings Page
 * 
 * Application settings and configuration
 * Placeholder for now
 */
const Settings: React.FC = () => {
  return (
    <div className="container mx-auto px-4 py-6">
      <div className="mb-6">
        <h1 className="text-3xl font-bold text-white mb-2">Settings</h1>
        <p className="text-slate-400">Configure your ImageViewer preferences</p>
      </div>

      <div className="border-2 border-dashed border-slate-800 rounded-lg p-12 text-center">
        <SettingsIcon className="h-16 w-16 text-slate-600 mx-auto mb-4" />
        <p className="text-slate-400 mb-2">Settings Coming Soon</p>
        <p className="text-sm text-slate-500">
          Application configuration will be available here
        </p>
      </div>
    </div>
  );
};

export default Settings;

