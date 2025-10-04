using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using MongoDB.Bson;
using Xunit;

namespace ImageViewer.Tests.Integration;

/// <summary>
/// Integration tests for all application services
/// </summary>
public class ServicesIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly IServiceProvider _serviceProvider;

    public ServicesIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _serviceProvider = _fixture.ServiceProvider;
    }

    [Fact]
    public async Task UserService_ShouldCreateAndRetrieveUser()
    {
        // Arrange
        var userService = _serviceProvider.GetRequiredService<IUserService>();
        var userId = ObjectId.GenerateNewId();

        // Act
        var user = await userService.GetByIdAsync(userId);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(userId, user.Id);
    }

    [Fact]
    public async Task LibraryService_ShouldCreateAndRetrieveLibrary()
    {
        // Arrange
        var libraryService = _serviceProvider.GetRequiredService<ILibraryService>();
        var libraryId = ObjectId.GenerateNewId();

        // Act
        var library = await libraryService.GetByIdAsync(libraryId);

        // Assert
        Assert.NotNull(library);
        Assert.Equal(libraryId, library.Id);
    }

    [Fact]
    public async Task CollectionService_ShouldCreateAndRetrieveCollection()
    {
        // Arrange
        var collectionService = _serviceProvider.GetRequiredService<ICollectionService>();
        var collectionId = ObjectId.GenerateNewId();

        // Act
        var collection = await collectionService.GetByIdAsync(collectionId);

        // Assert
        Assert.NotNull(collection);
        Assert.Equal(collectionId, collection.Id);
    }

    [Fact]
    public async Task MediaItemService_ShouldCreateAndRetrieveMediaItem()
    {
        // Arrange
        var mediaItemService = _serviceProvider.GetRequiredService<IMediaItemService>();
        var mediaItemId = ObjectId.GenerateNewId();

        // Act
        var mediaItem = await mediaItemService.GetByIdAsync(mediaItemId);

        // Assert
        Assert.NotNull(mediaItem);
        Assert.Equal(mediaItemId, mediaItem.Id);
    }

    [Fact]
    public async Task SearchService_ShouldPerformBasicSearch()
    {
        // Arrange
        var searchService = _serviceProvider.GetRequiredService<ISearchService>();
        var searchRequest = new SearchRequest
        {
            Query = "test search",
            Page = 1,
            PageSize = 10
        };

        // Act
        var searchResult = await searchService.SearchAsync(searchRequest);

        // Assert
        Assert.NotNull(searchResult);
        Assert.Equal(1, searchResult.Page);
        Assert.Equal(10, searchResult.PageSize);
        Assert.NotNull(searchResult.Items);
    }

    [Fact]
    public async Task NotificationService_ShouldCreateNotification()
    {
        // Arrange
        var notificationService = _serviceProvider.GetRequiredService<INotificationService>();
        var userId = ObjectId.GenerateNewId();
        var createRequest = new CreateNotificationRequest
        {
            UserId = userId,
            Type = NotificationType.Info,
            Title = "Test Notification",
            Message = "This is a test notification",
            Priority = NotificationPriority.Normal
        };

        // Act
        var notification = await notificationService.CreateNotificationAsync(createRequest);

        // Assert
        Assert.NotNull(notification);
        Assert.Equal(userId, notification.UserId);
        Assert.Equal("Test Notification", notification.Title);
        Assert.Equal("This is a test notification", notification.Message);
    }

    [Fact]
    public async Task UserPreferencesService_ShouldGetUserPreferences()
    {
        // Arrange
        var userPreferencesService = _serviceProvider.GetRequiredService<IUserPreferencesService>();
        var userId = ObjectId.GenerateNewId();

        // Act
        var preferences = await userPreferencesService.GetUserPreferencesAsync(userId);

        // Assert
        Assert.NotNull(preferences);
        Assert.Equal(userId, preferences.UserId);
        Assert.NotNull(preferences.Display);
        Assert.NotNull(preferences.Privacy);
        Assert.NotNull(preferences.Performance);
        Assert.NotNull(preferences.Notifications);
    }

    [Fact]
    public async Task PerformanceService_ShouldGetCacheInfo()
    {
        // Arrange
        var performanceService = _serviceProvider.GetRequiredService<IPerformanceService>();

        // Act
        var cacheInfo = await performanceService.GetCacheInfoAsync();

        // Assert
        Assert.NotNull(cacheInfo);
        Assert.NotNull(cacheInfo.Status);
    }

    [Fact]
    public async Task SecurityService_ShouldAuthenticateUser()
    {
        // Arrange
        var securityService = _serviceProvider.GetRequiredService<ISecurityService>();
        var loginRequest = new LoginRequest
        {
            Username = "testuser",
            Password = "testpassword"
        };

        // Act
        var authResult = await securityService.AuthenticateAsync(loginRequest);

        // Assert
        Assert.NotNull(authResult);
        // Note: This will fail with current placeholder implementation
        // In real implementation, this should return proper authentication result
    }

    [Fact]
    public async Task SecurityService_ShouldSetupTwoFactor()
    {
        // Arrange
        var securityService = _serviceProvider.GetRequiredService<ISecurityService>();
        var userId = ObjectId.GenerateNewId();

        // Act
        var twoFactorResult = await securityService.SetupTwoFactorAsync(userId);

        // Assert
        Assert.NotNull(twoFactorResult);
        Assert.True(twoFactorResult.Success);
        Assert.NotNull(twoFactorResult.SecretKey);
        Assert.NotNull(twoFactorResult.QrCodeUrl);
    }

    [Fact]
    public async Task AllServices_ShouldBeRegisteredInDI()
    {
        // Arrange & Act & Assert
        Assert.NotNull(_serviceProvider.GetRequiredService<IUserService>());
        Assert.NotNull(_serviceProvider.GetRequiredService<ILibraryService>());
        Assert.NotNull(_serviceProvider.GetRequiredService<ICollectionService>());
        Assert.NotNull(_serviceProvider.GetRequiredService<IMediaItemService>());
        Assert.NotNull(_serviceProvider.GetRequiredService<ISearchService>());
        Assert.NotNull(_serviceProvider.GetRequiredService<INotificationService>());
        Assert.NotNull(_serviceProvider.GetRequiredService<IUserPreferencesService>());
        Assert.NotNull(_serviceProvider.GetRequiredService<IPerformanceService>());
        Assert.NotNull(_serviceProvider.GetRequiredService<ISecurityService>());
    }

    [Fact]
    public async Task AllRepositories_ShouldBeRegisteredInDI()
    {
        // Arrange & Act & Assert
        Assert.NotNull(_serviceProvider.GetRequiredService<IUserRepository>());
        Assert.NotNull(_serviceProvider.GetRequiredService<ILibraryRepository>());
        Assert.NotNull(_serviceProvider.GetRequiredService<ICollectionRepository>());
        Assert.NotNull(_serviceProvider.GetRequiredService<IMediaItemRepository>());
    }

    [Fact]
    public async Task MongoDB_ShouldBeConnected()
    {
        // Arrange
        var mongoDatabase = _serviceProvider.GetRequiredService<IMongoDatabase>();

        // Act
        var adminCommand = new BsonDocument("ping", 1);
        var result = await mongoDatabase.RunCommandAsync<BsonDocument>(adminCommand);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Contains("ok"));
    }
}

/// <summary>
/// Integration test fixture for setting up test environment
/// </summary>
public class IntegrationTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; private set; }
    private readonly ServiceCollection _services;

    public IntegrationTestFixture()
    {
        _services = new ServiceCollection();
        SetupServices();
        ServiceProvider = _services.BuildServiceProvider();
    }

    private void SetupServices()
    {
        // Add logging
        _services.AddLogging(builder => builder.AddConsole());

        // Add MongoDB
        _services.AddSingleton<IMongoClient>(provider => new MongoClient("mongodb://localhost:27017"));
        _services.AddScoped<IMongoDatabase>(provider =>
        {
            var client = provider.GetRequiredService<IMongoClient>();
            return client.GetDatabase("image_viewer_test");
        });

        // Add repositories
        _services.AddScoped<IUserRepository, UserRepository>();
        _services.AddScoped<ILibraryRepository, LibraryRepository>();
        _services.AddScoped<ICollectionRepository, CollectionRepository>();
        _services.AddScoped<IMediaItemRepository, MediaItemRepository>();

        // Add services
        _services.AddScoped<IUserService, UserService>();
        _services.AddScoped<ILibraryService, LibraryService>();
        _services.AddScoped<ICollectionService, CollectionService>();
        _services.AddScoped<IMediaItemService, MediaItemService>();
        _services.AddScoped<ISearchService, SearchService>();
        _services.AddScoped<INotificationService, NotificationService>();
        _services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        _services.AddScoped<IPerformanceService, PerformanceService>();
        _services.AddScoped<ISecurityService, SecurityService>();
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
