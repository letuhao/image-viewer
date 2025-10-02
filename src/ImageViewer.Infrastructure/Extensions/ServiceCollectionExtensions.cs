using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ImageViewer.Infrastructure.Data;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Entities;
using MongoDB.Driver;

namespace ImageViewer.Infrastructure.Extensions;

/// <summary>
/// Service collection extensions for MongoDB
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

        // Register repositories
        services.AddScoped<ICollectionRepository, MongoCollectionRepository>();
        services.AddScoped<IImageRepository, MongoImageRepository>();
        services.AddScoped<ICacheFolderRepository, MongoCacheFolderRepository>();
        services.AddScoped<ITagRepository, MongoTagRepository>();
        services.AddScoped<IBackgroundJobRepository, MongoBackgroundJobRepository>();
        services.AddScoped<IViewSessionRepository, MongoViewSessionRepository>();
        services.AddScoped<ICacheInfoRepository, MongoCacheInfoRepository>();
        services.AddScoped<ICollectionTagRepository, MongoCollectionTagRepository>();
        services.AddScoped<ICollectionCacheBindingRepository, MongoCollectionCacheBindingRepository>();
        services.AddScoped<ICollectionSettingsRepository, MongoCollectionSettingsRepository>();
        services.AddScoped<ICollectionStatisticsRepository, MongoCollectionStatisticsRepository>();
        services.AddScoped<IImageCacheInfoRepository, MongoImageCacheInfoRepository>();
        services.AddScoped<IImageMetadataRepository, MongoImageMetadataRepository>();
        
        // Register generic repositories with collection names
        services.AddScoped<IRepository<CacheFolder>>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return new MongoRepository<CacheFolder>(database, "cache_folders");
        });
        services.AddScoped<IRepository<ImageViewer.Domain.Entities.Tag>>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return new MongoRepository<ImageViewer.Domain.Entities.Tag>(database, "tags");
        });
        services.AddScoped<IRepository<CollectionTag>>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return new MongoRepository<CollectionTag>(database, "collection_tags");
        });
        services.AddScoped<IRepository<ImageCacheInfo>>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return new MongoRepository<ImageCacheInfo>(database, "image_cache_info");
        });
        services.AddScoped<IRepository<CollectionCacheBinding>>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return new MongoRepository<CollectionCacheBinding>(database, "collection_cache_bindings");
        });
        services.AddScoped<IRepository<CollectionStatistics>>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return new MongoRepository<CollectionStatistics>(database, "collection_statistics");
        });
        services.AddScoped<IRepository<ViewSession>>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return new MongoRepository<ViewSession>(database, "view_sessions");
        });
        services.AddScoped<IRepository<BackgroundJob>>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return new MongoRepository<BackgroundJob>(database, "background_jobs");
        });
        services.AddScoped<IRepository<CollectionSettingsEntity>>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return new MongoRepository<CollectionSettingsEntity>(database, "collection_settings");
        });
        services.AddScoped<IRepository<ImageMetadataEntity>>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return new MongoRepository<ImageMetadataEntity>(database, "image_metadata");
        });

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
