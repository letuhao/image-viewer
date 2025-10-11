using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using ImageViewer.Domain.Entities;
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
        // Configure Serilog - READ FROM appsettings.json ONLY
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration) // Read all settings from appsettings.json
            .CreateLogger();

        try
        {
            Log.Information("Starting ImageViewer Scheduler");
            var host = CreateHostBuilder(args).Build();
            
            // Initialize MongoDB indexes on startup
            using (var scope = host.Services.CreateScope())
            {
                try
                {
                    var mongoInitService = scope.ServiceProvider.GetRequiredService<ImageViewer.Infrastructure.Services.MongoDbInitializationService>();
                    mongoInitService.InitializeAsync().GetAwaiter().GetResult();
                    Log.Information("✅ MongoDB indexes initialized");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "❌ Failed to initialize MongoDB indexes");
                    // Continue startup even if index creation fails
                }
            }
            
            host.Run();
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
                
                // Register MongoDB collections needed by Scheduler
                services.AddScoped<IMongoCollection<Library>>(provider =>
                {
                    var database = provider.GetRequiredService<IMongoDatabase>();
                    return database.GetCollection<Library>("libraries");
                });
                
                services.AddScoped<IMongoCollection<ScheduledJob>>(provider =>
                {
                    var database = provider.GetRequiredService<IMongoDatabase>();
                    return database.GetCollection<ScheduledJob>("scheduled_jobs");
                });
                
                services.AddScoped<IMongoCollection<ScheduledJobRun>>(provider =>
                {
                    var database = provider.GetRequiredService<IMongoDatabase>();
                    return database.GetCollection<ScheduledJobRun>("scheduled_job_runs");
                });
                
                // RabbitMQ Message Queue
                services.Configure<RabbitMQOptions>(configuration.GetSection("RabbitMQ"));
                services.AddSingleton<IMessageQueueService, RabbitMQMessageQueueService>();

                // Register MongoDB initialization service (creates indexes on startup)
                services.AddScoped<MongoDbInitializationService>();
                
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
