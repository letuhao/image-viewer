using Serilog;
using RabbitMQ.Client;
using Microsoft.Extensions.Options;
using ImageViewer.Worker.Services;
using ImageViewer.Infrastructure.Data;
using ImageViewer.Infrastructure.Extensions;
using ImageViewer.Application.Services;
using ImageViewer.Infrastructure.Services;
using ImageViewer.Domain.Interfaces;

Console.WriteLine("Start worker ...");

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

// Configure Batch Processing
builder.Services.Configure<BatchProcessingOptions>(builder.Configuration.GetSection("BatchProcessing"));

// Configure Memory Optimization
builder.Services.Configure<MemoryOptimizationOptions>(builder.Configuration.GetSection("MemoryOptimization"));

// Register RabbitMQ ConnectionFactory
builder.Services.AddSingleton<IConnectionFactory>(provider =>
{
    var options = provider.GetRequiredService<IOptions<RabbitMQOptions>>().Value;
    return new ConnectionFactory
    {
        HostName = options.HostName,
        Port = options.Port,
        UserName = options.UserName,
        Password = options.Password,
        VirtualHost = options.VirtualHost,
        RequestedConnectionTimeout = options.ConnectionTimeout,
        RequestedHeartbeat = TimeSpan.FromSeconds(60)
    };
});

// Register RabbitMQ connection (for backward compatibility with existing consumers)
builder.Services.AddSingleton(provider =>
{
    var factory = provider.GetRequiredService<IConnectionFactory>();
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

// Register message queue service
builder.Services.AddScoped<IMessageQueueService, RabbitMQMessageQueueService>();

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

// Register consumers - OPTIMIZED BATCH PROCESSING MODE
builder.Services.AddHostedService<LibraryScanConsumer>();           // ✅ KEEP - Library scanning
builder.Services.AddHostedService<CollectionScanConsumer>();        // ✅ KEEP - Collection scanning  
builder.Services.AddHostedService<ImageProcessingConsumer>();       // ✅ KEEP - Creates embedded images + queues messages

// Use batch thumbnail generation consumer (replaces old individual consumer)
builder.Services.AddHostedService<BatchThumbnailGenerationConsumer>(); // ✅ NEW OPTIMIZED - Batch thumbnail processing

// Use batch cache generation consumer (replaces old individual consumer)
builder.Services.AddHostedService<BatchCacheGenerationConsumer>(); // ✅ NEW OPTIMIZED - Batch cache processing

// LEGACY CONSUMERS - COMMENTED OUT TO PREVENT CONFLICTS WITH NEW BATCH LOGIC
// builder.Services.AddHostedService<CacheGenerationConsumer>();    // ❌ COMMENTED - Still uses old individual processing
builder.Services.AddHostedService<BulkOperationConsumer>();        // ✅ KEEP - Bulk operations support

// Register optimized image processing service
builder.Services.AddSingleton<IImageProcessingService, MemoryOptimizedImageProcessingService>();

var host = builder.Build();

Console.WriteLine("Host is building ...");

// Initialize MongoDB indexes on startup
using (var scope = host.Services.CreateScope())
{
    try
    {
        var mongoInitService = scope.ServiceProvider.GetRequiredService<MongoDbInitializationService>();
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
    
    // Start the host (this starts all hosted services)
    await host.StartAsync();
    
    Log.Information("ImageViewer Worker Service started successfully. Press Ctrl+C to stop.");
    
    // Wait for cancellation (Ctrl+C or shutdown signal)
    var cancellationToken = new CancellationTokenSource();
    Console.CancelKeyPress += (sender, eventArgs) =>
    {
        eventArgs.Cancel = true;
        Log.Information("Shutdown signal received. Stopping worker service...");
        cancellationToken.Cancel();
    };
    
    // Keep the service running until cancellation
    await Task.Delay(Timeout.Infinite, cancellationToken.Token);
}
catch (OperationCanceledException)
{
    Log.Information("Worker service shutdown requested");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.Information("Stopping ImageViewer Worker Service...");
    
    // Stop the host gracefully
    if (host is IAsyncDisposable asyncDisposable)
    {
        await asyncDisposable.DisposeAsync();
    }
    else
    {
        host.Dispose();
    }
    
    Log.CloseAndFlush();
}
