using Serilog;
using Serilog.Events;
using RabbitMQ.Client;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using ImageViewer.Worker;
using ImageViewer.Worker.Services;
using ImageViewer.Infrastructure.Data;
using ImageViewer.Infrastructure.Extensions;
using ImageViewer.Application.Services;
using ImageViewer.Infrastructure.Services;
using ImageViewer.Domain.Interfaces;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog - READ FROM appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration) // Read all settings from appsettings.json
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .CreateLogger();

// Use Serilog for all logging
builder.Services.AddSerilog();

// Configure MongoDB
builder.Services.AddMongoDb(builder.Configuration);

// Configure RabbitMQ
builder.Services.Configure<RabbitMQOptions>(builder.Configuration.GetSection("RabbitMQ"));

// Register RabbitMQ connection
builder.Services.AddSingleton<IConnection>(provider =>
{
    var options = provider.GetRequiredService<IOptions<RabbitMQOptions>>().Value;
    var factory = new ConnectionFactory
    {
        HostName = options.HostName,
        Port = options.Port,
        UserName = options.UserName,
        Password = options.Password,
        VirtualHost = options.VirtualHost,
        RequestedConnectionTimeout = options.ConnectionTimeout,
        RequestedHeartbeat = TimeSpan.FromSeconds(60)
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

// Register message queue service
builder.Services.AddScoped<ImageViewer.Domain.Interfaces.IMessageQueueService, ImageViewer.Infrastructure.Services.RabbitMQMessageQueueService>();

// Add Application Services
builder.Services.AddScoped<CollectionService>();
builder.Services.AddScoped<ICollectionService>(provider =>
{
    var collectionService = provider.GetRequiredService<CollectionService>();
    var messageQueueService = provider.GetRequiredService<IMessageQueueService>();
    var logger = provider.GetRequiredService<ILogger<QueuedCollectionService>>();
    return new QueuedCollectionService(collectionService, messageQueueService, logger);
});
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<ICacheService, CacheService>(); // Refactored to use embedded design
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IBackgroundJobService, ImageViewer.Application.Services.BackgroundJobService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>(); // Refactored to use embedded design
builder.Services.AddScoped<IBulkService, BulkService>();
builder.Services.AddScoped<IImageProcessingSettingsService, ImageProcessingSettingsService>();
builder.Services.AddScoped<ICacheFolderSelectionService, CacheFolderSelectionService>();

// Add Infrastructure Services
// builder.Services.AddScoped<IFileScannerService, FileScannerService>(); // Removed - needs refactoring
builder.Services.AddScoped<IImageProcessingService, SkiaSharpImageProcessingService>();
builder.Services.AddScoped<IAdvancedThumbnailService, AdvancedThumbnailService>(); // Refactored to use embedded design
builder.Services.AddScoped<ICompressedFileService, CompressedFileService>();

// Note: UserContextService and JwtService are designed for web applications
// For Worker project, we'll use mock implementations
builder.Services.AddScoped<IUserContextService, MockUserContextService>();
// builder.Services.AddScoped<IJwtService, JwtService>(); // Not needed for Worker

// Register RabbitMQ setup service (runs first to create queues)
builder.Services.AddScoped<RabbitMQSetupService>();
builder.Services.AddHostedService<RabbitMQStartupHostedService>();

// Register DLQ recovery service (runs on startup to recover failed messages)
builder.Services.AddHostedService<DlqRecoveryService>();

// Register centralized job monitoring service (runs every 5 seconds)
builder.Services.AddHostedService<JobMonitoringService>();

// Register file processing job recovery service (runs on startup)
builder.Services.AddHostedService<FileProcessingJobRecoveryHostedService>();

// Register consumers
builder.Services.AddHostedService<LibraryScanConsumer>();
builder.Services.AddHostedService<CollectionScanConsumer>();
builder.Services.AddHostedService<ImageProcessingConsumer>();
builder.Services.AddHostedService<ThumbnailGenerationConsumer>();
builder.Services.AddHostedService<CacheGenerationConsumer>();
builder.Services.AddHostedService<BulkOperationConsumer>();

var host = builder.Build();

// Initialize MongoDB indexes on startup
using (var scope = host.Services.CreateScope())
{
    try
    {
        var mongoInitService = scope.ServiceProvider.GetRequiredService<ImageViewer.Infrastructure.Services.MongoDbInitializationService>();
        await mongoInitService.InitializeAsync();
        Log.Information("✅ MongoDB indexes initialized");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "❌ Failed to initialize MongoDB indexes");
        // Continue startup even if index creation fails
    }
}

try
{
    Log.Information("Starting ImageViewer Worker Service");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
