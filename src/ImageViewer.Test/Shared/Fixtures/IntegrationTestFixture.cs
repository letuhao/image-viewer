using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Infrastructure.Services;

namespace ImageViewer.Test.Shared.Fixtures;

/// <summary>
/// Base integration test fixture for setting up test environment without Docker
/// </summary>
public class IntegrationTestFixture : IAsyncLifetime
{
    private IServiceProvider _serviceProvider = null!;

    public IServiceProvider ServiceProvider => _serviceProvider;

    public async Task InitializeAsync()
    {
        // Create service collection for testing
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

               // Add mocked repositories
               services.AddSingleton(Mock.Of<IUserRepository>());
               services.AddSingleton(Mock.Of<ILibraryRepository>());
               services.AddSingleton(Mock.Of<ICollectionRepository>());
               services.AddSingleton(Mock.Of<IMediaItemRepository>());
               services.AddSingleton(Mock.Of<IImageRepository>());
               services.AddSingleton(Mock.Of<ITagRepository>());
               services.AddSingleton(Mock.Of<INotificationTemplateRepository>());
               services.AddSingleton(Mock.Of<IPerformanceMetricRepository>());
               services.AddSingleton(Mock.Of<ICacheInfoRepository>());
               services.AddSingleton(Mock.Of<IMediaProcessingJobRepository>());
               services.AddSingleton(Mock.Of<ICacheFolderRepository>());
               services.AddSingleton(Mock.Of<IBackgroundJobRepository>());
               services.AddSingleton(Mock.Of<IUnitOfWork>());

        // Add application services
        services.AddScoped<ISystemHealthService, SystemHealthService>();
        services.AddScoped<IBulkOperationService, BulkOperationService>();
        services.AddScoped<IBackgroundJobService, ImageViewer.Application.Services.BackgroundJobService>();
        services.AddScoped<IBulkService, BulkService>();
        services.AddScoped<IPerformanceService, PerformanceService>();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IImageProcessingService, SkiaSharpImageProcessingService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationTemplateService, NotificationTemplateService>();
        services.AddScoped<IRealTimeNotificationService, RealTimeNotificationService>();
        services.AddScoped<IDiscoveryService, DiscoveryService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<ISecurityService, SecurityService>();
        services.AddScoped<ICollectionService, CollectionService>();
        services.AddScoped<IMediaItemService, MediaItemService>();
        services.AddScoped<IImageService, ImageService>();

        // Also register concrete types for integration tests
        services.AddScoped<SystemHealthService>();
        services.AddScoped<BulkOperationService>();
        services.AddScoped<ImageViewer.Application.Services.BackgroundJobService>();
        services.AddScoped<BulkService>();
        services.AddScoped<PerformanceService>();
        services.AddScoped<CacheService>();
        services.AddScoped<SkiaSharpImageProcessingService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<NotificationTemplateService>();
        services.AddScoped<RealTimeNotificationService>();
        services.AddScoped<DiscoveryService>();
        services.AddScoped<SearchService>();
        services.AddScoped<UserService>();
        services.AddScoped<UserPreferencesService>();
        services.AddScoped<UserProfileService>();
        services.AddScoped<SecurityService>();
        services.AddScoped<CollectionService>();
        services.AddScoped<MediaItemService>();
        services.AddScoped<ImageService>();

        _serviceProvider = services.BuildServiceProvider();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// Get a service from the DI container
    /// </summary>
    public T GetService<T>() where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Get a scoped service from the DI container
    /// </summary>
    public T GetScopedService<T>() where T : notnull
    {
        using var scope = _serviceProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Clean up test data (no-op for mocked services)
    /// </summary>
    public async Task CleanupTestDataAsync()
    {
        // No cleanup needed for mocked services
        await Task.CompletedTask;
    }
}

/// <summary>
/// Collection fixture for sharing the integration test fixture across multiple test classes
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
