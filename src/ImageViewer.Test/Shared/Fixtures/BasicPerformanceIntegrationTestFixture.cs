using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Infrastructure.Services;
using ImageViewer.Test.Shared.TestData;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Test.Shared.Fixtures;

/// <summary>
/// Basic integration test fixture for Performance tests
/// Uses minimal mocking and focuses on essential functionality
/// </summary>
public class BasicPerformanceIntegrationTestFixture : IAsyncLifetime
{
    private IServiceProvider _serviceProvider = null!;

    public IServiceProvider ServiceProvider => _serviceProvider;

    public async Task InitializeAsync()
    {
        // Create service collection for testing
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Create basic mock repositories
        var mockRepositories = CreateBasicMockRepositories();

        // Register mocked repositories
        services.AddSingleton(mockRepositories.UserRepository.Object);
        services.AddSingleton(mockRepositories.LibraryRepository.Object);
        services.AddSingleton(mockRepositories.CollectionRepository.Object);
        services.AddSingleton(mockRepositories.MediaItemRepository);
        services.AddSingleton(mockRepositories.ImageRepository.Object);
        services.AddSingleton(mockRepositories.TagRepository);
        services.AddSingleton(mockRepositories.NotificationTemplateRepository);
        services.AddSingleton(mockRepositories.PerformanceMetricRepository.Object);
        // services.AddSingleton(mockRepositories.CacheInfoRepository.Object); // Removed
        services.AddSingleton(mockRepositories.MediaProcessingJobRepository.Object);
        services.AddSingleton(mockRepositories.CacheFolderRepository.Object);
        services.AddSingleton(mockRepositories.BackgroundJobRepository);
        services.AddSingleton(mockRepositories.UserSettingRepository);
        services.AddSingleton(mockRepositories.UnitOfWork);

        // Add application services
        services.AddScoped<ISystemHealthService, SystemHealthService>();
        services.AddScoped<IBulkOperationService, BulkOperationService>();
        services.AddScoped<IBackgroundJobService, ImageViewer.Application.Services.BackgroundJobService>();
        services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IBulkService, BulkService>();
        // services.AddScoped<IPerformanceService, PerformanceService>(); // Removed
        // services.AddScoped<ICacheService, CacheService>(); // Removed
        services.AddScoped<IImageProcessingService, SkiaSharpImageProcessingService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationTemplateService, NotificationTemplateService>();
        services.AddScoped<IRealTimeNotificationService, RealTimeNotificationService>();
        services.AddScoped<IDiscoveryService, DiscoveryService>();
        services.AddScoped<ISearchService, SearchService>();

        _serviceProvider = services.BuildServiceProvider();

        // Generate test images
        await GenerateTestImagesAsync();
    }

    public async Task DisposeAsync()
    {
        // Cleanup test images
        TestImageGenerator.CleanupTestImages();
        
        // Cleanup cache directories
        var cacheDirs = Directory.GetDirectories(Path.GetTempPath(), "ImageViewerTestCache*");
        foreach (var cacheDir in cacheDirs)
        {
            try
            {
                Directory.Delete(cacheDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public T GetService<T>() where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    public async Task CleanupTestDataAsync()
    {
        // Regenerate test images if needed
        await GenerateTestImagesAsync();
    }

    private async Task GenerateTestImagesAsync()
    {
        await Task.Run(() =>
        {
            // Generate test images for image processing tests
            TestImageGenerator.CreateTestJpeg("test-image.jpg", 800, 600);
            TestImageGenerator.CreateTestJpeg("test1.jpg", 400, 300);
            TestImageGenerator.CreateTestJpeg("test2.jpg", 500, 400);
            TestImageGenerator.CreateTestJpeg("test3.jpg", 600, 450);
            TestImageGenerator.CreateTestJpeg("test4.jpg", 700, 500);
            TestImageGenerator.CreateTestJpeg("test5.jpg", 800, 600);
            TestImageGenerator.CreateLargeTestImage("large-test-image.jpg", 4000, 3000);
            TestImageGenerator.CreateHighQualityTestImage("high-quality-test.jpg", 1920, 1080);
        });
    }

    private BasicMockRepositories CreateBasicMockRepositories()
    {
        // Create basic mock repositories that return empty collections or null
        // This is sufficient for integration tests that focus on service behavior
        var mockUserRepository = new Mock<IUserRepository>();
        mockUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>())).ReturnsAsync((User?)null);
        mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<User>());

        var mockLibraryRepository = new Mock<ILibraryRepository>();
        mockLibraryRepository.Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>())).ReturnsAsync((Library?)null);
        mockLibraryRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Library>());

        var mockCollectionRepository = new Mock<ICollectionRepository>();
        // Setup to return a mock collection for any ID to support cache tests
        mockCollectionRepository.Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>()))
            .ReturnsAsync((ObjectId id) => new Collection(ObjectId.GenerateNewId(), "Test Collection", "/test/path", Domain.Enums.CollectionType.Folder) { Id = id });
        mockCollectionRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Collection>());

        var mockImageRepository = new Mock<IImageRepository>();
        // Setup to return a mock image for any ID to support cache tests
        mockImageRepository.Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>()))
            .ReturnsAsync((ObjectId id) => new Image(ObjectId.GenerateNewId(), "test.jpg", "/test/image.jpg", 1024L, 800, 600, "jpeg") { Id = id });
        mockImageRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Image>());

        var mockCacheFolderRepository = new Mock<ICacheFolderRepository>();
        mockCacheFolderRepository.Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>())).ReturnsAsync((CacheFolder?)null);
        mockCacheFolderRepository.Setup(x => x.GetByPathAsync(It.IsAny<string>())).ReturnsAsync((CacheFolder?)null);
        mockCacheFolderRepository.Setup(x => x.CreateAsync(It.IsAny<CacheFolder>()))
            .ReturnsAsync((CacheFolder folder) => { folder.Id = ObjectId.GenerateNewId(); return folder; });
        
        // Create a real cache directory for integration tests
        var cacheDir = Path.Combine(Path.GetTempPath(), "ImageViewerTestCache", Guid.NewGuid().ToString());
        Directory.CreateDirectory(cacheDir);
        
        var testCacheFolder = new CacheFolder("Test Cache", cacheDir, 1000L, 1) 
        { 
            Id = ObjectId.GenerateNewId() 
        };
        
        // Setup to return a default cache folder for any collection
        mockCacheFolderRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<CacheFolder> { testCacheFolder });
        mockCacheFolderRepository.Setup(x => x.GetActiveOrderedByPriorityAsync())
            .ReturnsAsync(new List<CacheFolder> { testCacheFolder });

        // Removed: ICacheInfoRepository mocking - interface deleted

        var mockPerformanceMetricRepository = new Mock<IPerformanceMetricRepository>();
        mockPerformanceMetricRepository.Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>())).ReturnsAsync((PerformanceMetric?)null);
        mockPerformanceMetricRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<PerformanceMetric>());

        var mockMediaProcessingJobRepository = new Mock<IMediaProcessingJobRepository>();
        mockMediaProcessingJobRepository.Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>())).ReturnsAsync((MediaProcessingJob?)null);
        mockMediaProcessingJobRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<MediaProcessingJob>());

        return new BasicMockRepositories
        {
            UserRepository = mockUserRepository,
            LibraryRepository = mockLibraryRepository,
            CollectionRepository = mockCollectionRepository,
            MediaItemRepository = Mock.Of<IMediaItemRepository>(),
            ImageRepository = mockImageRepository,
            TagRepository = Mock.Of<ITagRepository>(),
            NotificationTemplateRepository = Mock.Of<INotificationTemplateRepository>(),
            PerformanceMetricRepository = mockPerformanceMetricRepository,
            // CacheInfoRepository = mockCacheInfoRepository, // Removed
            MediaProcessingJobRepository = mockMediaProcessingJobRepository,
            CacheFolderRepository = mockCacheFolderRepository,
            BackgroundJobRepository = Mock.Of<IBackgroundJobRepository>(),
            UserSettingRepository = Mock.Of<IUserSettingRepository>(),
            UnitOfWork = Mock.Of<IUnitOfWork>()
        };
    }

    private class BasicMockRepositories
    {
        public Mock<IUserRepository> UserRepository { get; set; } = null!;
        public Mock<ILibraryRepository> LibraryRepository { get; set; } = null!;
        public Mock<ICollectionRepository> CollectionRepository { get; set; } = null!;
        public IMediaItemRepository MediaItemRepository { get; set; } = null!;
        public Mock<IImageRepository> ImageRepository { get; set; } = null!;
        public ITagRepository TagRepository { get; set; } = null!;
        public INotificationTemplateRepository NotificationTemplateRepository { get; set; } = null!;
        public Mock<IPerformanceMetricRepository> PerformanceMetricRepository { get; set; } = null!;
        // public Mock<ICacheInfoRepository> CacheInfoRepository { get; set; } = null!; // Removed
        public Mock<IMediaProcessingJobRepository> MediaProcessingJobRepository { get; set; } = null!;
        public Mock<ICacheFolderRepository> CacheFolderRepository { get; set; } = null!;
        public IBackgroundJobRepository BackgroundJobRepository { get; set; } = null!;
        public IUserSettingRepository UserSettingRepository { get; set; } = null!;
        public IUnitOfWork UnitOfWork { get; set; } = null!;
    }
}
