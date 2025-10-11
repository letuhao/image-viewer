using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ImageViewer.Infrastructure.Data;
using ImageViewer.Infrastructure.Services;
using ImageViewer.Infrastructure.Configuration;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Entities;
using ImageViewer.Application.Services;
using MongoDB.Driver;

namespace ImageViewer.Infrastructure.Extensions;

/// <summary>
/// Service collection extensions for MongoDB and Application Services
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure MongoDB options
        services.Configure<MongoDbOptions>(options =>
        {
            options.ConnectionString = configuration["MongoDb:ConnectionString"] ?? "mongodb://localhost:27017";
            options.DatabaseName = configuration["MongoDb:DatabaseName"] ?? "image_viewer";
        });
        
        // Register MongoDB client and database
        services.AddSingleton<IMongoClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            return new MongoClient(options.ConnectionString);
        });
        
        services.AddScoped<IMongoDatabase>(provider =>
        {
            var client = provider.GetRequiredService<IMongoClient>();
            var options = provider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            return client.GetDatabase(options.DatabaseName);
        });

        // Register MongoDB collections
        services.AddScoped<IMongoCollection<Library>>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return database.GetCollection<Library>("libraries");
        });
        
        services.AddScoped<IMongoCollection<Collection>>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return database.GetCollection<Collection>("collections");
        });
        
        services.AddScoped<IMongoCollection<MediaItem>>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return database.GetCollection<MediaItem>("media_items");
        });
        
        services.AddScoped<IMongoCollection<User>>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return database.GetCollection<User>("users");
        });
        
        services.AddScoped<IMongoCollection<RefreshToken>>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return database.GetCollection<RefreshToken>("refresh_tokens");
        });
        
        services.AddScoped<IMongoCollection<Session>>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return database.GetCollection<Session>("sessions");
        });
        
        services.AddScoped<IMongoCollection<ImageViewer.Domain.Entities.Tag>>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return database.GetCollection<ImageViewer.Domain.Entities.Tag>("tags");
        });
        
        services.AddScoped<IMongoCollection<CacheFolder>>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return database.GetCollection<CacheFolder>("cache_folders");
        });
        
        services.AddScoped<IMongoCollection<BackgroundJob>>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return database.GetCollection<BackgroundJob>("background_jobs");
        });
        
        services.AddScoped<IMongoCollection<ViewSession>>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return database.GetCollection<ViewSession>("view_sessions");
        });

        // Register MongoDB context
        services.AddScoped<MongoDbContext>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return new MongoDbContext(database);
        });

        // Register core repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, MongoRefreshTokenRepository>();
        services.AddScoped<ISessionRepository, MongoSessionRepository>();
        services.AddScoped<ILibraryRepository, LibraryRepository>();
        services.AddScoped<ICollectionRepository, MongoCollectionRepository>();
        services.AddScoped<IMediaItemRepository, MediaItemRepository>();
        // services.AddScoped<IImageRepository, MongoImageRepository>(); // Removed - use embedded ImageEmbedded
        services.AddScoped<ITagRepository, MongoTagRepository>();
        services.AddScoped<ICollectionTagRepository, MongoCollectionTagRepository>();
        services.AddScoped<ICacheFolderRepository, MongoCacheFolderRepository>();
        // Unified file processing job state (cache, thumbnail, compression, etc.)
        services.AddScoped<IFileProcessingJobStateRepository, FileProcessingJobStateRepository>();
        // services.AddScoped<IThumbnailInfoRepository, MongoThumbnailInfoRepository>(); // Removed - use embedded ThumbnailEmbedded
        services.AddScoped<IViewSessionRepository, MongoViewSessionRepository>();
        services.AddScoped<IBackgroundJobRepository, MongoBackgroundJobRepository>();
        services.AddScoped<ICollectionStatisticsRepository, MongoCollectionStatisticsRepository>();

        // Register Priority 1 (Core System) repositories
        services.AddScoped<ICollectionRatingRepository, MongoCollectionRatingRepository>();
        services.AddScoped<IFavoriteListRepository, MongoFavoriteListRepository>();
        services.AddScoped<ISearchHistoryRepository, MongoSearchHistoryRepository>();
        services.AddScoped<IUserSettingRepository, MongoUserSettingRepository>();
        services.AddScoped<IAuditLogRepository, MongoAuditLogRepository>();
        services.AddScoped<IErrorLogRepository, MongoErrorLogRepository>();
        services.AddScoped<IPerformanceMetricRepository, MongoPerformanceMetricRepository>();

        // Register Priority 2 (Advanced Features) repositories
        services.AddScoped<IConversationRepository, MongoConversationRepository>();
        services.AddScoped<INotificationQueueRepository, MongoNotificationQueueRepository>();
        services.AddScoped<INotificationTemplateRepository, MongoNotificationTemplateRepository>();
        services.AddScoped<IUserGroupRepository, MongoUserGroupRepository>();
        services.AddScoped<IUserActivityLogRepository, MongoUserActivityLogRepository>();
        services.AddScoped<ISystemSettingRepository, MongoSystemSettingRepository>();
        services.AddScoped<ISystemMaintenanceRepository, MongoSystemMaintenanceRepository>();
        services.AddScoped<ISystemHealthRepository, MongoSystemHealthRepository>();

        // Register Priority 3 (Storage & File Management) repositories
        services.AddScoped<IStorageLocationRepository, MongoStorageLocationRepository>();
        services.AddScoped<IFileStorageMappingRepository, MongoFileStorageMappingRepository>();
        services.AddScoped<IBackupHistoryRepository, MongoBackupHistoryRepository>();

        // Register Priority 3 (Distribution Features) repositories
        services.AddScoped<ITorrentRepository, MongoTorrentRepository>();
        services.AddScoped<IDownloadLinkRepository, MongoDownloadLinkRepository>();
        services.AddScoped<ITorrentStatisticsRepository, MongoTorrentStatisticsRepository>();
        services.AddScoped<ILinkHealthCheckerRepository, MongoLinkHealthCheckerRepository>();
        services.AddScoped<IDownloadQualityOptionRepository, MongoDownloadQualityOptionRepository>();
        services.AddScoped<IDistributionNodeRepository, MongoDistributionNodeRepository>();
        services.AddScoped<INodePerformanceMetricsRepository, MongoNodePerformanceMetricsRepository>();

        // Register Priority 4 (Premium Features) repositories
        services.AddScoped<IRewardAchievementRepository, MongoRewardAchievementRepository>();
        services.AddScoped<IRewardBadgeRepository, MongoRewardBadgeRepository>();
        services.AddScoped<IPremiumFeatureRepository, MongoPremiumFeatureRepository>();
        services.AddScoped<IUserPremiumFeatureRepository, MongoUserPremiumFeatureRepository>();

        // Register Priority 5 (File Management) repositories
        services.AddScoped<IFilePermissionRepository, MongoFilePermissionRepository>();

        // Register Priority 6 (Advanced Analytics) repositories
        services.AddScoped<IContentSimilarityRepository, MongoContentSimilarityRepository>();
        services.AddScoped<IMediaProcessingJobRepository, MongoMediaProcessingJobRepository>();
        services.AddScoped<ICustomReportRepository, MongoCustomReportRepository>();

        // Register Security repositories
        services.AddScoped<ISecurityAlertRepository, MongoSecurityAlertRepository>();

        // Register application services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ILibraryService, LibraryService>();
        services.AddScoped<ICollectionService, CollectionService>();
        services.AddScoped<IMediaItemService, MediaItemService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        services.AddScoped<ICacheService, CacheService>(); // Refactored to use embedded design
        services.AddScoped<ICacheCleanupService, CacheCleanupService>(); // Cache and thumbnail cleanup
        services.AddScoped<IJobFailureAlertService, JobFailureAlertService>(); // Job failure monitoring and alerts
        // Unified file processing job recovery (cache, thumbnail, etc.)
        services.AddScoped<IFileProcessingJobRecoveryService, FileProcessingJobRecoveryService>();
        services.AddScoped<IStatisticsService, StatisticsService>(); // Refactored to use embedded design
        services.AddScoped<IPerformanceService, PerformanceService>(); // Stub implementation
        services.AddScoped<ISecurityService, SecurityService>();
        services.AddScoped<IWindowsDriveService, WindowsDriveService>();
        services.AddScoped<ISystemSettingService, SystemSettingService>();

        // Register infrastructure services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordService, PasswordService>();

        // Register unit of work
        services.AddScoped<IUnitOfWork, MongoUnitOfWork>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            var logger = provider.GetRequiredService<ILogger<MongoUnitOfWork>>();
            var unitOfWork = new MongoUnitOfWork(database, logger);
            unitOfWork.Initialize();
            return unitOfWork;
        });

        return services;
    }
}
