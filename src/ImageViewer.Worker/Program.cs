using Serilog;
using Serilog.Events;
using RabbitMQ.Client;
using ImageViewer.Worker;
using ImageViewer.Worker.Services;
using ImageViewer.Infrastructure.Data;
using ImageViewer.Infrastructure.Extensions;

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
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);
builder.Host.UseSerilog();

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
    return factory.CreateConnection();
});

// Register message queue service
builder.Services.AddScoped<ImageViewer.Domain.Interfaces.IMessageQueueService, ImageViewer.Infrastructure.Services.RabbitMQMessageQueueService>();

// Register consumers
builder.Services.AddHostedService<CollectionScanConsumer>();
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
