using Serilog;
using Serilog.Events;
using ImageViewer.Infrastructure.Data;
using ImageViewer.Application.Services;
using ImageViewer.Infrastructure.Services;
using ImageViewer.Domain.Interfaces;
using Microsoft.AspNetCore.Hosting;
using ImageViewer.Application.Options;
using Microsoft.Extensions.Options;
using ImageViewer.Infrastructure.Extensions;
using ImageViewer.Scheduler.Extensions;
using ImageViewer.Scheduler.Configuration;
using Hangfire;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog - READ FROM appsettings.json ONLY
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration) // Read all settings from appsettings.json
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Add response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ImageViewer API",
        Version = "v1",
        Description = "API for Image Viewer application"
    });
});
// Bind Image size options
builder.Services.Configure<ImageSizeOptions>(builder.Configuration.GetSection("ImageSizes"));
builder.Services.Configure<ImageCachePresetsOptions>(builder.Configuration.GetSection("ImageCachePresets"));

// Add MongoDB
builder.Services.AddMongoDb(builder.Configuration);

// Add RabbitMQ
builder.Services.Configure<ImageViewer.Infrastructure.Data.RabbitMQOptions>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddScoped<ImageViewer.Domain.Interfaces.IMessageQueueService, ImageViewer.Infrastructure.Services.RabbitMQMessageQueueService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add session support for user tracking
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

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
builder.Services.AddScoped<IMessageQueueService, RabbitMQMessageQueueService>();

// Configure Redis
builder.Services.Configure<ImageViewer.Application.Options.RedisOptions>(builder.Configuration.GetSection("Redis"));

// Register Redis distributed cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    var redisConfig = builder.Configuration.GetSection("Redis");
    options.Configuration = redisConfig["ConnectionString"];
    options.InstanceName = redisConfig["InstanceName"];
});

// Register Redis ConnectionMultiplexer (for advanced operations)
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(provider =>
{
    var redisConfig = builder.Configuration.GetSection("Redis");
    var connectionString = redisConfig["ConnectionString"];
    return StackExchange.Redis.ConnectionMultiplexer.Connect(connectionString!);
});

// Register Redis Image Cache Service
builder.Services.AddScoped<ImageViewer.Domain.Interfaces.IImageCacheService, ImageViewer.Infrastructure.Services.RedisImageCacheService>();

// Register Thumbnail Cache Service (for Base64 encoding with Redis)
builder.Services.AddScoped<ImageViewer.Application.Services.IThumbnailCacheService, ImageViewer.Application.Services.ThumbnailCacheService>();

// Add Hangfire Scheduler with Dashboard
builder.Services.AddHangfireDashboard(builder.Configuration);

// Add Application Services - New MongoDB-based services are registered in AddMongoDb extension
// The following services are registered in ServiceCollectionExtensions.AddMongoDb():
// - IUserService, UserService
// - ILibraryService, LibraryService  
// - ICollectionService, CollectionService
// - IMediaItemService, MediaItemService

// Legacy services (to be removed in future phases)
builder.Services.AddScoped<ILibraryService, LibraryService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<ICacheService, CacheService>(); // Refactored to use embedded design
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IBackgroundJobService, ImageViewer.Application.Services.BackgroundJobService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>(); // Refactored to use embedded design
builder.Services.AddScoped<IBulkService, BulkService>();
builder.Services.AddScoped<IImageProcessingSettingsService, ImageProcessingSettingsService>();
builder.Services.AddScoped<ICacheFolderSelectionService, CacheFolderSelectionService>();
builder.Services.AddScoped<IScheduledJobManagementService, ScheduledJobManagementService>();

// Add Infrastructure Services
// builder.Services.AddScoped<IFileScannerService, FileScannerService>(); // Removed - needs refactoring
builder.Services.AddScoped<IImageProcessingService, SkiaSharpImageProcessingService>();
builder.Services.AddScoped<IAdvancedThumbnailService, AdvancedThumbnailService>(); // Refactored to use embedded design
builder.Services.AddScoped<ICompressedFileService, CompressedFileService>();
builder.Services.AddScoped<IUserContextService, ImageViewer.Infrastructure.Services.UserContextService>();
builder.Services.AddScoped<IJwtService, ImageViewer.Infrastructure.Services.JwtService>();

// Add JWT Authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"];
        var jwtIssuer = builder.Configuration["Jwt:Issuer"];
        var jwtAudience = builder.Configuration["Jwt:Audience"];
        
        if (string.IsNullOrWhiteSpace(jwtKey))
            throw new InvalidOperationException("JWT:Key is not configured. Please set it in appsettings.json or environment variables.");
        
        if (string.IsNullOrWhiteSpace(jwtIssuer))
            throw new InvalidOperationException("JWT:Issuer is not configured. Please set it in appsettings.json or environment variables.");
        
        if (string.IsNullOrWhiteSpace(jwtAudience))
            throw new InvalidOperationException("JWT:Audience is not configured. Please set it in appsettings.json or environment variables.");
        
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Add HttpContextAccessor for UserContextService
builder.Services.AddHttpContextAccessor();

// Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure server to use port 11000
app.Urls.Add("http://localhost:11000");
app.Urls.Add("https://localhost:11001");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ImageViewer API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseRouting();
app.UseSession();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Add Hangfire Dashboard (accessible at /hangfire)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
    DashboardTitle = "ImageViewer Scheduler Dashboard",
    StatsPollingInterval = 2000 // 2 seconds
});

app.MapControllers();
app.MapHealthChecks("/health");

// Set up RabbitMQ queues and exchanges on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var connection = scope.ServiceProvider.GetRequiredService<IConnection>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<RabbitMQOptions>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<RabbitMQSetupService>>();
        
        var setupService = new RabbitMQSetupService(connection, options, logger);
        
        // Check if queues already exist
        var queuesExist = await setupService.CheckQueuesExistAsync();
        
        if (!queuesExist)
        {
            logger.LogInformation("Queues do not exist, creating them...");
            await setupService.SetupQueuesAndExchangesAsync();
        }
        else
        {
            logger.LogInformation("All required queues already exist");
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to set up RabbitMQ queues and exchanges");
        // Don't throw - let the API start even if RabbitMQ setup fails
    }
}

// Initialize system settings with defaults if not exists
using (var scope = app.Services.CreateScope())
{
    try
    {
        var systemSettingService = scope.ServiceProvider.GetRequiredService<ISystemSettingService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("🔧 Checking system settings...");
        await systemSettingService.InitializeDefaultSettingsAsync();
        logger.LogInformation("✅ System settings initialized successfully");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ Failed to initialize system settings");
        // Don't throw - let the API start even if settings initialization fails
    }
}

// MongoDB doesn't require database creation - it creates collections automatically

app.Run();

// Make Program class accessible for testing
public partial class Program { }