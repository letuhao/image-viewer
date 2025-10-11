using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Infrastructure.Configuration;
using ImageViewer.Infrastructure.Data;
using ImageViewer.Infrastructure.Services;
using ImageViewer.Scheduler.Configuration;
using ImageViewer.Scheduler.Jobs;
using ImageViewer.Scheduler.Services;
using MongoDB.Driver;
using Serilog;

namespace ImageViewer.Scheduler;

public class Program
{
    public static void Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Hangfire", Serilog.Events.LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/scheduler-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                fileSizeLimitBytes: 100 * 1024 * 1024, // 100MB
                retainedFileCountLimit: 7)
            .CreateLogger();

        try
        {
            Log.Information("Starting ImageViewer Scheduler");
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Scheduler terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((hostContext, services) =>
            {
                var configuration = hostContext.Configuration;

                // Register Hangfire options
                services.Configure<HangfireOptions>(configuration.GetSection("Hangfire"));
                var hangfireOptions = configuration.GetSection("Hangfire").Get<HangfireOptions>() 
                    ?? new HangfireOptions();

                // Register MongoDB for Hangfire
                var mongoUrlBuilder = new MongoUrlBuilder(hangfireOptions.ConnectionString)
                {
                    DatabaseName = hangfireOptions.DatabaseName
                };
                var mongoClient = new MongoClient(mongoUrlBuilder.ToMongoUrl());

                // Configure Hangfire to use MongoDB
                var storageOptions = new MongoStorageOptions
                {
                    MigrationOptions = new MongoMigrationOptions
                    {
                        MigrationStrategy = new MigrateMongoMigrationStrategy(),
                        BackupStrategy = new CollectionMongoBackupStrategy()
                    },
                    CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.TailNotificationsCollection,
                    Prefix = "hangfire",
                    CheckConnection = true
                };

                services.AddHangfire(config => config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseMongoStorage(mongoClient, hangfireOptions.DatabaseName, storageOptions));

                // Register Hangfire server
                services.AddHangfireServer(options =>
                {
                    options.ServerName = hangfireOptions.ServerName;
                    options.WorkerCount = hangfireOptions.WorkerCount;
                    options.Queues = hangfireOptions.Queues;
                });

                // Register Infrastructure services
                // MongoDB (manual registration - Scheduler only needs specific services)
                services.Configure<MongoDbOptions>(options =>
                {
                    options.ConnectionString = configuration["MongoDb:ConnectionString"] ?? "mongodb://localhost:27017";
                    options.DatabaseName = configuration["MongoDb:DatabaseName"] ?? "image_viewer";
                });
                
                services.AddSingleton<IMongoClient>(provider =>
                {
                    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbOptions>>().Value;
                    return new MongoClient(options.ConnectionString);
                });
                
                services.AddScoped<IMongoDatabase>(provider =>
                {
                    var client = provider.GetRequiredService<IMongoClient>();
                    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbOptions>>().Value;
                    return client.GetDatabase(options.DatabaseName);
                });
                
                // RabbitMQ Message Queue
                services.Configure<RabbitMQOptions>(configuration.GetSection("RabbitMQ"));
                services.AddSingleton<IMessageQueueService, RabbitMQMessageQueueService>();

                // Register MongoDB repositories for scheduler (only what's needed)
                services.AddScoped<IScheduledJobRepository, MongoScheduledJobRepository>();
                services.AddScoped<IScheduledJobRunRepository, MongoScheduledJobRunRepository>();
                services.AddScoped<ILibraryRepository, LibraryRepository>();

                // Register Scheduler services
                services.AddScoped<ISchedulerService, HangfireSchedulerService>();
                services.AddScoped<IScheduledJobExecutor, ScheduledJobExecutor>();
                
                // Register job handlers
                services.AddScoped<ILibraryScanJobHandler, LibraryScanJobHandler>();

                // Register the background worker
                services.AddHostedService<SchedulerWorker>();
            });
}
