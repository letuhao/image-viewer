import { useCollectionStats } from '../hooks/useCollections';
import { Card, CardHeader, CardContent } from '../components/ui/Card';
import LoadingSpinner from '../components/ui/LoadingSpinner';
import { Database, HardDrive, Image as ImageIcon, Activity } from 'lucide-react';

/**
 * Dashboard Page
 * 
 * Overview of system stats and recent activity
 * Body scrolls if content exceeds viewport
 */
const Dashboard: React.FC = () => {
  const { data: stats, isLoading } = useCollectionStats();

  if (isLoading) {
    return <LoadingSpinner text="Loading dashboard..." />;
  }

  const statCards = [
    {
      title: 'Collections',
      value: stats?.totalCollections ?? 0,
      icon: Database,
      color: 'text-blue-500',
      bgColor: 'bg-blue-500/10',
    },
    {
      title: 'Images',
      value: stats?.totalImages ?? 0,
      icon: ImageIcon,
      color: 'text-green-500',
      bgColor: 'bg-green-500/10',
    },
    {
      title: 'Thumbnails',
      value: stats?.totalThumbnails ?? 0,
      icon: HardDrive,
      color: 'text-purple-500',
      bgColor: 'bg-purple-500/10',
    },
    {
      title: 'Active Jobs',
      value: stats?.activeJobs ?? 0,
      icon: Activity,
      color: 'text-yellow-500',
      bgColor: 'bg-yellow-500/10',
    },
  ];

  return (
    <div className="container mx-auto px-4 py-6 space-y-6">
      {/* Page Header */}
      <div>
        <h1 className="text-3xl font-bold text-white mb-2">Dashboard</h1>
        <p className="text-slate-400">Welcome to ImageViewer Platform</p>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {statCards.map((stat) => {
          const Icon = stat.icon;
          return (
            <Card key={stat.title} hover>
              <CardContent className="flex items-center space-x-4">
                <div className={`p-3 rounded-lg ${stat.bgColor}`}>
                  <Icon className={`h-6 w-6 ${stat.color}`} />
                </div>
                <div>
                  <p className="text-sm text-slate-400">{stat.title}</p>
                  <p className="text-2xl font-bold text-white">
                    {stat.value.toLocaleString()}
                  </p>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {/* Recent Activity (placeholder) */}
      <Card>
        <CardHeader>
          <h2 className="text-lg font-semibold text-white">Recent Activity</h2>
        </CardHeader>
        <CardContent>
          <p className="text-slate-400 text-center py-8">
            No recent activity to display
          </p>
        </CardContent>
      </Card>
    </div>
  );
};

export default Dashboard;

