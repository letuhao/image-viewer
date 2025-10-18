using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using ImageViewer.Scheduler.Configuration;
using ImageViewer.Scheduler.Services;
using ImageViewer.Scheduler.Jobs;
using MongoDB.Driver;

namespace ImageViewer.Scheduler.Extensions;

/// <summary>
/// Extension methods for registering Hangfire services
/// Hangfire服务注册扩展方法 - Phương thức mở rộng đăng ký dịch vụ Hangfire
/// </summary>
public static class HangfireServiceExtensions
{
    /// <summary>
    /// Add Hangfire scheduler services
    /// 添加Hangfire调度服务 - Thêm dịch vụ lập lịch Hangfire
    /// </summary>
    public static IServiceCollection AddHangfireScheduler(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind Hangfire options
        var hangfireOptions = new HangfireOptions();
        configuration.GetSection("Hangfire").Bind(hangfireOptions);
        services.Configure<HangfireOptions>(configuration.GetSection("Hangfire"));

        // Configure Hangfire to use MongoDB
        var mongoUrlBuilder = new MongoUrlBuilder(hangfireOptions.ConnectionString);
        var mongoClient = new MongoClient(hangfireOptions.ConnectionString);

        var storageOptions = new MongoStorageOptions
        {
            MigrationOptions = new MongoMigrationOptions
            {
                MigrationStrategy = new MigrateMongoMigrationStrategy(),
                BackupStrategy = new CollectionMongoBackupStrategy()
            },
            Prefix = "hangfire",
            CheckConnection = true,
            CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.TailNotificationsCollection
        };

        GlobalConfiguration.Configuration
            .UseMongoStorage(mongoClient, hangfireOptions.DatabaseName, storageOptions)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings();

        // Add Hangfire services
        services.AddHangfire((serviceProvider, config) =>
        {
            config.UseMongoStorage(mongoClient, hangfireOptions.DatabaseName, storageOptions);
        });

        // Add Hangfire server (background job processing)
        services.AddHangfireServer((serviceProvider, options) =>
        {
            options.ServerName = hangfireOptions.ServerName;
            options.WorkerCount = hangfireOptions.WorkerCount;
            options.Queues = hangfireOptions.Queues;
        });

        // Register scheduler services
        services.AddScoped<ISchedulerService, HangfireSchedulerService>();
        services.AddScoped<IScheduledJobExecutor, ScheduledJobExecutor>();

        return services;
    }

    /// <summary>
    /// Add Hangfire dashboard UI (for API project)
    /// 添加Hangfire仪表板UI - Thêm giao diện dashboard Hangfire
    /// </summary>
    public static IServiceCollection AddHangfireDashboard(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var hangfireOptions = new HangfireOptions();
        configuration.GetSection("Hangfire").Bind(hangfireOptions);

        if (hangfireOptions.EnableDashboard)
        {
            services.AddHangfire((serviceProvider, config) =>
            {
                var mongoClient = new MongoClient(hangfireOptions.ConnectionString);
                var storageOptions = new MongoStorageOptions
                {
                    MigrationOptions = new MongoMigrationOptions
                    {
                        MigrationStrategy = new MigrateMongoMigrationStrategy(),
                        BackupStrategy = new CollectionMongoBackupStrategy()
                    },
                    Prefix = "hangfire"
                };
                
                config.UseMongoStorage(mongoClient, hangfireOptions.DatabaseName, storageOptions);
            });
        }

        return services;
    }
}

