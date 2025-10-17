import { useDashboardStats, useDashboardActivity } from '../hooks/useCollections';
import { Card, CardHeader, CardContent } from '../components/ui/Card';
import LoadingSpinner from '../components/ui/LoadingSpinner';
import { Database, HardDrive, Image as ImageIcon, Activity, TrendingUp, Clock, AlertCircle } from 'lucide-react';

/**
 * Dashboard Page
 * 
 * Overview of system stats and recent activity
 * Body scrolls if content exceeds viewport
 */
const Dashboard: React.FC = () => {
  const { data: stats, isLoading: statsLoading } = useDashboardStats();
  const { data: activity, isLoading: activityLoading } = useDashboardActivity(5);

  const formatBytes = (bytes: number): string => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  };

  if (statsLoading) {
    return <LoadingSpinner text="Loading dashboard..." />;
  }

  const statCards = [
    {
      title: 'Collections',
      value: stats?.totalCollections ?? 0,
      subtitle: `${stats?.activeCollections ?? 0} active`,
      icon: Database,
      color: 'text-blue-500',
      bgColor: 'bg-blue-500/10',
    },
    {
      title: 'Images',
      value: stats?.totalImages ?? 0,
      subtitle: `${formatBytes(stats?.totalSize ?? 0)} total`,
      icon: ImageIcon,
      color: 'text-green-500',
      bgColor: 'bg-green-500/10',
    },
    {
      title: 'Thumbnails',
      value: stats?.totalThumbnails ?? 0,
      subtitle: `${formatBytes(stats?.totalThumbnailSize ?? 0)} cached`,
      icon: HardDrive,
      color: 'text-purple-500',
      bgColor: 'bg-purple-500/10',
    },
    {
      title: 'Active Jobs',
      value: stats?.activeJobs ?? 0,
      subtitle: `${stats?.completedJobsToday ?? 0} completed today`,
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
                <div className="flex-1">
                  <p className="text-sm text-slate-400">{stat.title}</p>
                  <p className="text-2xl font-bold text-white">
                    {stat.value.toLocaleString()}
                  </p>
                  <p className="text-xs text-slate-500">{stat.subtitle}</p>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {/* Additional Stats Row */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {/* Cache Folders */}
        <Card>
          <CardHeader>
            <h3 className="text-lg font-semibold text-white flex items-center">
              <HardDrive className="h-5 w-5 mr-2 text-purple-500" />
              Cache Folders
            </h3>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {stats?.cacheFolderStats?.slice(0, 3).map((folder: any) => (
                <div key={folder.id} className="flex justify-between items-center">
                  <div>
                    <p className="text-sm text-white">{folder.name}</p>
                    <p className="text-xs text-slate-400">{folder.path}</p>
                  </div>
                  <div className="text-right">
                    <p className="text-sm text-white">{formatBytes(folder.currentSizeBytes)}</p>
                    <p className="text-xs text-slate-400">{folder.usagePercentage.toFixed(1)}% used</p>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        {/* System Health */}
        <Card>
          <CardHeader>
            <h3 className="text-lg font-semibold text-white flex items-center">
              <Activity className="h-5 w-5 mr-2 text-green-500" />
              System Health
            </h3>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              <div className="flex justify-between items-center">
                <span className="text-sm text-slate-400">Redis</span>
                <span className={`text-sm ${stats?.systemHealth?.redisStatus === 'Connected' ? 'text-green-500' : 'text-red-500'}`}>
                  {stats?.systemHealth?.redisStatus || 'Unknown'}
                </span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm text-slate-400">MongoDB</span>
                <span className={`text-sm ${stats?.systemHealth?.mongoDbStatus === 'Connected' ? 'text-green-500' : 'text-red-500'}`}>
                  {stats?.systemHealth?.mongoDbStatus || 'Unknown'}
                </span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm text-slate-400">Worker</span>
                <span className={`text-sm ${stats?.systemHealth?.workerStatus === 'Running' ? 'text-green-500' : 'text-red-500'}`}>
                  {stats?.systemHealth?.workerStatus || 'Unknown'}
                </span>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Top Collections */}
        <Card>
          <CardHeader>
            <h3 className="text-lg font-semibold text-white flex items-center">
              <TrendingUp className="h-5 w-5 mr-2 text-blue-500" />
              Top Collections
            </h3>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {stats?.topCollections?.slice(0, 3).map((collection: any) => (
                <div key={collection.id} className="flex justify-between items-center">
                  <div className="flex-1 min-w-0">
                    <p className="text-sm text-white truncate">{collection.name}</p>
                    <p className="text-xs text-slate-400">{collection.imageCount} images</p>
                  </div>
                  <div className="text-right">
                    <p className="text-sm text-white">{collection.viewCount}</p>
                    <p className="text-xs text-slate-400">views</p>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Recent Activity */}
      <Card>
        <CardHeader>
          <h2 className="text-lg font-semibold text-white flex items-center">
            <Clock className="h-5 w-5 mr-2 text-blue-500" />
            Recent Activity
          </h2>
        </CardHeader>
        <CardContent>
          {activityLoading ? (
            <div className="text-center py-4">
              <LoadingSpinner text="Loading activity..." />
            </div>
          ) : activity && activity.length > 0 ? (
            <div className="space-y-3">
              {activity.map((item: any, index: number) => (
                <div key={index} className="flex items-start space-x-3 p-3 rounded-lg bg-slate-800/50">
                  <div className="flex-shrink-0">
                    {item.type === 'collection_created' && <Database className="h-4 w-4 text-green-500" />}
                    {item.type === 'scan_completed' && <Activity className="h-4 w-4 text-blue-500" />}
                    {item.type === 'job_failed' && <AlertCircle className="h-4 w-4 text-red-500" />}
                    {!['collection_created', 'scan_completed', 'job_failed'].includes(item.type) && 
                     <Activity className="h-4 w-4 text-slate-500" />}
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm text-white">{item.message || item.Message || 'Activity'}</p>
                    <p className="text-xs text-slate-400">
                      {new Date(item.timestamp || item.Timestamp).toLocaleString()}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-slate-400 text-center py-8">
              No recent activity to display
            </p>
          )}
        </CardContent>
      </Card>
    </div>
  );
};

export default Dashboard;

