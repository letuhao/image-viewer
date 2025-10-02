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
        services.AddScoped<IRepository<CacheFolder>, MongoRepository<CacheFolder>>();
        services.AddScoped<IRepository<ImageViewer.Domain.Entities.Tag>, MongoRepository<ImageViewer.Domain.Entities.Tag>>();
        services.AddScoped<IRepository<CollectionTag>, MongoRepository<CollectionTag>>();
        services.AddScoped<IRepository<ImageCacheInfo>, MongoRepository<ImageCacheInfo>>();
        services.AddScoped<IRepository<CollectionCacheBinding>, MongoRepository<CollectionCacheBinding>>();
        services.AddScoped<IRepository<CollectionStatistics>, MongoRepository<CollectionStatistics>>();
        services.AddScoped<IRepository<ViewSession>, MongoRepository<ViewSession>>();
        services.AddScoped<IRepository<BackgroundJob>, MongoRepository<BackgroundJob>>();
        services.AddScoped<IRepository<CollectionSettingsEntity>, MongoRepository<CollectionSettingsEntity>>();
        services.AddScoped<IRepository<ImageMetadataEntity>, MongoRepository<ImageMetadataEntity>>();

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
