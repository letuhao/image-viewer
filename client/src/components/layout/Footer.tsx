import { Activity, Clock, Database, HardDrive } from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import { api } from '../../services/api';

/**
 * Footer Component
 * 
 * Design:
 * - Fixed height (h-12 = 48px) to prevent window scroll
 * - Icon-based status indicators for compact design
 * - Shows real-time system stats
 * - Responsive: hides labels on mobile, shows only icons
 */
const Footer: React.FC = () => {
  // Fetch system stats (optional, non-blocking)
  const { data: stats } = useQuery({
    queryKey: ['systemStats'],
    queryFn: async () => {
      try {
        const response = await api.get('/statistics/overall');
        return response.data;
      } catch {
        return null;
      }
    },
    refetchInterval: 10000, // Refresh every 10 seconds
    retry: false,
  });

  const footerItems = [
    {
      icon: Database,
      label: 'Collections',
      value: stats?.totalCollections ?? '-',
      color: 'text-blue-400',
    },
    {
      icon: HardDrive,
      label: 'Images',
      value: stats?.totalImages ?? '-',
      color: 'text-green-400',
    },
    {
      icon: Activity,
      label: 'Jobs',
      value: stats?.activeJobs ?? '-',
      color: 'text-yellow-400',
    },
    {
      icon: Clock,
      label: 'Updated',
      value: stats?.lastUpdate ? new Date(stats.lastUpdate).toLocaleTimeString() : '-',
      color: 'text-slate-400',
    },
  ];

  return (
    <footer className="h-12 border-t border-slate-800 bg-slate-900 flex-shrink-0">
      <div className="h-full px-4 flex items-center justify-between">
        {/* Left: Status Indicators */}
        <div className="flex items-center space-x-4">
          {footerItems.map((item, index) => {
            const Icon = item.icon;
            return (
              <div
                key={index}
                className="flex items-center space-x-1.5"
                title={`${item.label}: ${item.value}`}
              >
                <Icon className={`h-4 w-4 ${item.color}`} />
                <span className="hidden sm:inline text-xs text-slate-400">
                  {item.label}:
                </span>
                <span className="text-xs font-medium text-white">
                  {item.value}
                </span>
              </div>
            );
          })}
        </div>

        {/* Right: Version & Status */}
        <div className="flex items-center space-x-3">
          {/* Connection Status */}
          <div className="flex items-center space-x-1.5">
            <div className={`h-2 w-2 rounded-full ${stats ? 'bg-green-500' : 'bg-red-500'} animate-pulse`} />
            <span className="hidden sm:inline text-xs text-slate-400">
              {stats ? 'Connected' : 'Disconnected'}
            </span>
          </div>

          {/* Version */}
          <div className="hidden md:flex items-center text-xs text-slate-500">
            v1.0.0
          </div>
        </div>
      </div>
    </footer>
  );
};

export default Footer;

