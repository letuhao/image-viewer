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

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ImageViewer.Worker")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/imageviewer-worker.log", rollingInterval: RollingInterval.Day)
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

// Register centralized job monitoring service (runs every 5 seconds)
builder.Services.AddHostedService<JobMonitoringService>();

// Register consumers
builder.Services.AddHostedService<CollectionScanConsumer>();
builder.Services.AddHostedService<ImageProcessingConsumer>();
builder.Services.AddHostedService<ThumbnailGenerationConsumer>();
builder.Services.AddHostedService<CacheGenerationConsumer>();
builder.Services.AddHostedService<BulkOperationConsumer>();

var host = builder.Build();

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
