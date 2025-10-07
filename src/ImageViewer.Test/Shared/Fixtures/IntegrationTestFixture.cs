using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Infrastructure.Services;
using ImageViewer.Domain.Entities;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImageViewer.Test.Shared.Fixtures;

/// <summary>
/// Base integration test fixture for setting up test environment without Docker
/// </summary>
    public class IntegrationTestFixture : IAsyncLifetime
    {
        private IServiceProvider _serviceProvider = null!;
        private readonly ObjectId _testUserId = ObjectId.Parse("507f1f77bcf86cd799439011");
        private List<User> _testUsers = null!;
        private List<Domain.Entities.NotificationTemplate> _testTemplates = null!;

        public IServiceProvider ServiceProvider => _serviceProvider;
        public ObjectId TestUserId => _testUserId;

    public async Task InitializeAsync()
    {
        // Initialize test data
        _testUsers = CreateTestUsers();
        _testTemplates = new List<Domain.Entities.NotificationTemplate>();

        // Create service collection for testing
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

               // Add mocked repositories with basic setup
               services.AddSingleton<IUserRepository>(CreateMockUserRepository().Object);
               services.AddSingleton<ILibraryRepository>(CreateMockLibraryRepository().Object);
               services.AddSingleton<ICollectionRepository>(CreateMockCollectionRepository().Object);
               services.AddSingleton<IMediaItemRepository>(CreateMockMediaItemRepository().Object);
               services.AddSingleton<IImageRepository>(CreateMockImageRepository().Object);
               services.AddSingleton<ITagRepository>(CreateMockTagRepository().Object);
               services.AddSingleton<INotificationTemplateRepository>(CreateMockNotificationTemplateRepository().Object);
               services.AddSingleton<IPerformanceMetricRepository>(CreateMockPerformanceMetricRepository().Object);
               services.AddSingleton<ICacheInfoRepository>(CreateMockCacheInfoRepository().Object);
               services.AddSingleton<IMediaProcessingJobRepository>(CreateMockMediaProcessingJobRepository().Object);
               services.AddSingleton<ICacheFolderRepository>(CreateMockCacheFolderRepository().Object);
               services.AddSingleton<IBackgroundJobRepository>(CreateMockBackgroundJobRepository().Object);
               services.AddSingleton<IUserSettingRepository>(CreateMockUserSettingRepository().Object);
               services.AddSingleton<INotificationQueueRepository>(CreateMockNotificationQueueRepository().Object);
               services.AddSingleton<IUnitOfWork>(CreateMockUnitOfWork().Object);

        // Add application services
        services.AddScoped<ISystemHealthService, SystemHealthService>();
        services.AddScoped<IBulkOperationService, BulkOperationService>();
        services.AddScoped<IBackgroundJobService, ImageViewer.Application.Services.BackgroundJobService>();
        services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IUserService, UserService>();
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

           public async Task CleanupTestDataAsync()
           {
               // Reset the test data to initial state
               // This ensures each test starts with a clean slate
               _testUsers.Clear();
               _testUsers.AddRange(CreateTestUsers());
               _testTemplates.Clear();
               await Task.CompletedTask;
           }

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

    #region Mock Repository Creation Methods

           private Mock<IUserRepository> CreateMockUserRepository()
           {
               var mock = new Mock<IUserRepository>();

               // Setup GetByIdAsync
               mock.Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>()))
                   .ReturnsAsync((ObjectId id) => _testUsers.FirstOrDefault(u => u.Id == id));

               // Setup GetByUsernameAsync
               mock.Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
                   .ReturnsAsync((string username) => _testUsers.FirstOrDefault(u => u.Username == username));

               // Setup GetByEmailAsync
               mock.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                   .ReturnsAsync((string email) => _testUsers.FirstOrDefault(u => u.Email == email));

               // Setup GetAllAsync
               mock.Setup(x => x.GetAllAsync())
                   .ReturnsAsync(_testUsers);

               // Setup CreateAsync - add the new user to the list and return it
               mock.Setup(x => x.CreateAsync(It.IsAny<User>()))
                   .ReturnsAsync((User user) => {
                       _testUsers.Add(user);
                       return user;
                   });

               // Setup UpdateAsync - update the user in the list and return it
               mock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
                   .ReturnsAsync((User user) => {
                       var existingUser = _testUsers.FirstOrDefault(u => u.Id == user.Id);
                       if (existingUser != null)
                       {
                           var index = _testUsers.IndexOf(existingUser);
                           _testUsers[index] = user;
                       }
                       return user;
                   });

               // Setup DeleteAsync - remove the user from the list
               mock.Setup(x => x.DeleteAsync(It.IsAny<ObjectId>()))
                   .Returns((ObjectId id) => {
                       var userToRemove = _testUsers.FirstOrDefault(u => u.Id == id);
                       if (userToRemove != null)
                       {
                           _testUsers.Remove(userToRemove);
                       }
                       return Task.CompletedTask;
                   });

               // Setup GetUserStatisticsAsync
               mock.Setup(x => x.GetUserStatisticsAsync())
                   .ReturnsAsync(() => new ImageViewer.Domain.ValueObjects.UserStatistics
                   {
                       TotalUsers = _testUsers.Count,
                       ActiveUsers = _testUsers.Count(u => u.IsActive),
                       VerifiedUsers = _testUsers.Count(u => u.IsEmailVerified),
                       NewUsersThisMonth = _testUsers.Count(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-30)),
                       NewUsersThisWeek = _testUsers.Count(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-7)),
                       NewUsersToday = _testUsers.Count(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-1))
                   });

               return mock;
           }

    private Mock<ILibraryRepository> CreateMockLibraryRepository()
    {
        var mock = new Mock<ILibraryRepository>();
        var testLibraries = CreateTestLibraries();

        mock.Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>()))
            .ReturnsAsync((ObjectId id) => testLibraries.FirstOrDefault(l => l.Id == id));

        mock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(testLibraries);

        return mock;
    }

    private Mock<ICollectionRepository> CreateMockCollectionRepository()
    {
        var mock = new Mock<ICollectionRepository>();
        var testCollections = CreateTestCollections();

        mock.Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>()))
            .ReturnsAsync((ObjectId id) => testCollections.FirstOrDefault(c => c.Id == id));

        mock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(testCollections);

        return mock;
    }

    private Mock<IMediaItemRepository> CreateMockMediaItemRepository()
    {
        var mock = new Mock<IMediaItemRepository>();
        var testMediaItems = CreateTestMediaItems();

        mock.Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>()))
            .ReturnsAsync((ObjectId id) => testMediaItems.FirstOrDefault(m => m.Id == id));

        mock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(testMediaItems);

        return mock;
    }

    private Mock<IImageRepository> CreateMockImageRepository()
    {
        var mock = new Mock<IImageRepository>();
        var testImages = CreateTestImages();

        mock.Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>()))
            .ReturnsAsync((ObjectId id) => testImages.FirstOrDefault(i => i.Id == id));

        mock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(testImages);

        return mock;
    }

    private Mock<ITagRepository> CreateMockTagRepository()
    {
        var mock = new Mock<ITagRepository>();
        var testTags = CreateTestTags();

        mock.Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>()))
            .ReturnsAsync((ObjectId id) => testTags.FirstOrDefault(t => t.Id == id));

        mock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(testTags);

        return mock;
    }

    private Mock<INotificationTemplateRepository> CreateMockNotificationTemplateRepository()
    {
        var mock = new Mock<INotificationTemplateRepository>();

        mock.Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>()))
            .ReturnsAsync((ObjectId id) => _testTemplates.FirstOrDefault(t => t.Id == id));

        mock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(_testTemplates);

        mock.Setup(x => x.GetByTemplateTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string templateType, CancellationToken ct) => _testTemplates.Where(t => t.TemplateType == templateType));

        mock.Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.NotificationTemplate>()))
            .ReturnsAsync((Domain.Entities.NotificationTemplate template) => {
                _testTemplates.Add(template);
                return template;
            });

        mock.Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.NotificationTemplate>()))
            .ReturnsAsync((Domain.Entities.NotificationTemplate template) => {
                var existing = _testTemplates.FirstOrDefault(t => t.Id == template.Id);
                if (existing != null)
                {
                    var index = _testTemplates.IndexOf(existing);
                    _testTemplates[index] = template;
                }
                return template;
            });

        mock.Setup(x => x.DeleteAsync(It.IsAny<ObjectId>()))
            .Returns((ObjectId id) => {
                var template = _testTemplates.FirstOrDefault(t => t.Id == id);
                if (template != null)
                {
                    _testTemplates.Remove(template);
                }
                return Task.CompletedTask;
            });


        return mock;
    }

    private Mock<IPerformanceMetricRepository> CreateMockPerformanceMetricRepository()
    {
        var mock = new Mock<IPerformanceMetricRepository>();
        var testMetrics = CreateTestPerformanceMetrics();

        mock.Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>()))
            .ReturnsAsync((ObjectId id) => testMetrics.FirstOrDefault(m => m.Id == id));

        mock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(testMetrics);

        return mock;
    }

    private Mock<ICacheInfoRepository> CreateMockCacheInfoRepository()
    {
        var mock = new Mock<ICacheInfoRepository>();
        var testCacheInfos = CreateTestCacheInfos();

        mock.Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>()))
            .ReturnsAsync((ObjectId id) => testCacheInfos.FirstOrDefault(c => c.Id == id));

        mock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(testCacheInfos);

        return mock;
    }

    private Mock<IMediaProcessingJobRepository> CreateMockMediaProcessingJobRepository()
    {
        var mock = new Mock<IMediaProcessingJobRepository>();
        var testJobs = CreateTestMediaProcessingJobs();

        mock.Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>()))
            .ReturnsAsync((ObjectId id) => testJobs.FirstOrDefault(j => j.Id == id));

        mock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(testJobs);

        return mock;
    }

    private Mock<ICacheFolderRepository> CreateMockCacheFolderRepository()
    {
        var mock = new Mock<ICacheFolderRepository>();
        var testCacheFolders = CreateTestCacheFolders();

        mock.Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>()))
            .ReturnsAsync((ObjectId id) => testCacheFolders.FirstOrDefault(c => c.Id == id));

        mock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(testCacheFolders);

        return mock;
    }

    private Mock<IBackgroundJobRepository> CreateMockBackgroundJobRepository()
    {
        var mock = new Mock<IBackgroundJobRepository>();
        var testJobs = CreateTestBackgroundJobs();

        mock.Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>()))
            .ReturnsAsync((ObjectId id) => testJobs.FirstOrDefault(j => j.Id == id));

        mock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(testJobs);

        return mock;
    }

    private Mock<IUserSettingRepository> CreateMockUserSettingRepository()
    {
        var mock = new Mock<IUserSettingRepository>();
        var testSettings = CreateTestUserSettings();

        mock.Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>()))
            .ReturnsAsync((ObjectId id) => testSettings.FirstOrDefault(s => s.Id == id));

        mock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(testSettings);

        return mock;
    }

    private Mock<INotificationQueueRepository> CreateMockNotificationQueueRepository()
    {
        var mock = new Mock<INotificationQueueRepository>();
        var testQueues = CreateTestNotificationQueues();

        mock.Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>()))
            .ReturnsAsync((ObjectId id) => testQueues.FirstOrDefault(q => q.Id == id));

        mock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(testQueues);

        mock.Setup(x => x.GetPendingNotificationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(testQueues.Where(q => q.Status == "Pending"));

        mock.Setup(x => x.GetByUserIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ObjectId userId, CancellationToken ct) => testQueues.Where(q => q.UserId == userId));

        mock.Setup(x => x.GetByStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string status, CancellationToken ct) => testQueues.Where(q => q.Status == status));

        mock.Setup(x => x.GetByChannelAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string channel, CancellationToken ct) => testQueues.Where(q => q.NotificationType == channel));

        return mock;
    }

    private Mock<IUnitOfWork> CreateMockUnitOfWork()
    {
        var mock = new Mock<IUnitOfWork>();
        mock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return mock;
    }

    #endregion

    #region Test Data Creation Methods

           private List<User> CreateTestUsers()
           {
               var users = new List<User>();
               
               // Create a test user with a fixed ID for testing
               var testUser = new User("testuser", "test@example.com", "Test User", "hashedpassword");
               // Use reflection to set the ID since it's read-only
               var idProperty = typeof(User).GetProperty("Id");
               idProperty?.SetValue(testUser, _testUserId);
               users.Add(testUser);
               
               return users;
           }

    private List<Library> CreateTestLibraries()
    {
        return new List<Library>();
    }

    private List<Collection> CreateTestCollections()
    {
        return new List<Collection>();
    }

    private List<MediaItem> CreateTestMediaItems()
    {
        return new List<MediaItem>();
    }

    private List<Image> CreateTestImages()
    {
        return new List<Image>();
    }

    private List<Tag> CreateTestTags()
    {
        return new List<Tag>();
    }

    private List<Domain.Entities.NotificationTemplate> CreateTestNotificationTemplates()
    {
        return new List<Domain.Entities.NotificationTemplate>();
    }

    private List<PerformanceMetric> CreateTestPerformanceMetrics()
    {
        return new List<PerformanceMetric>();
    }

    private List<ImageCacheInfo> CreateTestCacheInfos()
    {
        return new List<ImageCacheInfo>();
    }

    private List<MediaProcessingJob> CreateTestMediaProcessingJobs()
    {
        return new List<MediaProcessingJob>();
    }

    private List<CacheFolder> CreateTestCacheFolders()
    {
        return new List<CacheFolder>();
    }

    private List<BackgroundJob> CreateTestBackgroundJobs()
    {
        return new List<BackgroundJob>();
    }

    private List<UserSetting> CreateTestUserSettings()
    {
        return new List<UserSetting>();
    }

    private List<NotificationQueue> CreateTestNotificationQueues()
    {
        return new List<NotificationQueue>();
    }

    #endregion
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
