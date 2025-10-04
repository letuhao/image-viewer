using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ImageViewer.Infrastructure.Data;
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

        // Register MongoDB context
        services.AddScoped<MongoDbContext>(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            return new MongoDbContext(database);
        });

        // Register new repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILibraryRepository, LibraryRepository>();
        services.AddScoped<ICollectionRepository, CollectionRepository>();
        services.AddScoped<IMediaItemRepository, MediaItemRepository>();

        // Register application services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ILibraryService, LibraryService>();
        services.AddScoped<ICollectionService, CollectionService>();
        services.AddScoped<IMediaItemService, MediaItemService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        services.AddScoped<IPerformanceService, PerformanceService>();
        services.AddScoped<ISecurityService, SecurityService>();

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
