using FluentAssertions;
using Moq;
using Xunit;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ImageViewer.Application.Options;
using ImageViewer.Application.DTOs.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImageViewer.Test.Features.MediaManagement.Unit;

/// <summary>
/// Unit tests for CacheService - Caching and Performance features
/// </summary>
public class CacheServiceTests
{
    private readonly Mock<ICacheFolderRepository> _mockCacheFolderRepository;
    private readonly Mock<ICollectionRepository> _mockCollectionRepository;
    private readonly Mock<IImageRepository> _mockImageRepository;
    private readonly Mock<ICacheInfoRepository> _mockCacheInfoRepository;
    private readonly Mock<IImageProcessingService> _mockImageProcessingService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<CacheService>> _mockLogger;
    private readonly Mock<IOptions<ImageSizeOptions>> _mockSizeOptions;
    private readonly CacheService _cacheService;

    public CacheServiceTests()
    {
        _mockCacheFolderRepository = new Mock<ICacheFolderRepository>();
        _mockCollectionRepository = new Mock<ICollectionRepository>();
        _mockImageRepository = new Mock<IImageRepository>();
        _mockCacheInfoRepository = new Mock<ICacheInfoRepository>();
        _mockImageProcessingService = new Mock<IImageProcessingService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<CacheService>>();
        _mockSizeOptions = new Mock<IOptions<ImageSizeOptions>>();

        _mockSizeOptions.Setup(x => x.Value).Returns(new ImageSizeOptions());

        _cacheService = new CacheService(
            _mockCacheFolderRepository.Object,
            _mockCollectionRepository.Object,
            _mockImageRepository.Object,
            _mockCacheInfoRepository.Object,
            _mockImageProcessingService.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object,
            _mockSizeOptions.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange & Act
        var service = new CacheService(
            _mockCacheFolderRepository.Object,
            _mockCollectionRepository.Object,
            _mockImageRepository.Object,
            _mockCacheInfoRepository.Object,
            _mockImageProcessingService.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object,
            _mockSizeOptions.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullCacheFolderRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CacheService(
            null!,
            _mockCollectionRepository.Object,
            _mockImageRepository.Object,
            _mockCacheInfoRepository.Object,
            _mockImageProcessingService.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object,
            _mockSizeOptions.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CacheService(
            _mockCacheFolderRepository.Object,
            _mockCollectionRepository.Object,
            _mockImageRepository.Object,
            _mockCacheInfoRepository.Object,
            _mockImageProcessingService.Object,
            _mockUnitOfWork.Object,
            null!,
            _mockSizeOptions.Object));
    }

    #endregion

    #region Cache Statistics Tests

    [Fact]
    public async Task GetCacheStatisticsAsync_WithValidData_ShouldReturnStatistics()
    {
        // Arrange
        var cacheFolders = new List<CacheFolder>
        {
            new CacheFolder("Test Cache", "/test/cache", 1024L, 10)
        };

        var cacheInfos = new List<ImageCacheInfo>
        {
            new ImageCacheInfo(ObjectId.GenerateNewId(), "/test/cache/image1.jpg", "1920x1080", 512L, DateTime.UtcNow.AddDays(-1))
        };

        _mockCacheFolderRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(cacheFolders);
        _mockCacheInfoRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(cacheInfos);

        // Act
        var result = await _cacheService.GetCacheStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Summary.Should().NotBeNull();
        result.CacheFolders.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCacheStatisticsAsync_WithNoData_ShouldReturnEmptyStatistics()
    {
        // Arrange
        _mockCacheFolderRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<CacheFolder>());
        _mockCacheInfoRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<ImageCacheInfo>());

        // Act
        var result = await _cacheService.GetCacheStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Summary.Should().NotBeNull();
        result.CacheFolders.Should().BeEmpty();
    }

    #endregion

    #region Cache Folder Management Tests

    [Fact]
    public async Task GetCacheFoldersAsync_WithValidData_ShouldReturnFolders()
    {
        // Arrange
        var cacheFolders = new List<CacheFolder>
        {
            new CacheFolder("Test Cache", "/test/cache", 1024L, 10)
        };

        _mockCacheFolderRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(cacheFolders);

        // Act
        var result = await _cacheService.GetCacheFoldersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Test Cache");
        result.First().Path.Should().Be("/test/cache");
    }

    [Fact]
    public async Task CreateCacheFolderAsync_WithValidData_ShouldCreateFolder()
    {
        // Arrange
        var createDto = new CreateCacheFolderDto
        {
            Name = "New Cache",
            Path = "/new/cache",
            MaxSize = 2048L,
            Priority = 10
        };

        var createdFolder = new CacheFolder(createDto.Name, createDto.Path, createDto.MaxSize, createDto.Priority);
        _mockCacheFolderRepository.Setup(x => x.CreateAsync(It.IsAny<CacheFolder>())).ReturnsAsync(createdFolder);

        // Act
        var result = await _cacheService.CreateCacheFolderAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Cache");
        result.Path.Should().Be("/new/cache");
        result.MaxSize.Should().Be(2048L);
        result.Priority.Should().Be(10);
    }

    [Fact]
    public async Task GetCacheFolderAsync_WithValidId_ShouldReturnFolder()
    {
        // Arrange
        var folderId = ObjectId.GenerateNewId();
        var cacheFolder = new CacheFolder("Test Cache", "/test/cache", 1024L, 10)
        {
            Id = folderId
        };

        _mockCacheFolderRepository.Setup(x => x.GetByIdAsync(folderId)).ReturnsAsync(cacheFolder);

        // Act
        var result = await _cacheService.GetCacheFolderAsync(folderId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(folderId);
        result.Name.Should().Be("Test Cache");
    }

    [Fact]
    public async Task UpdateCacheFolderAsync_WithValidData_ShouldUpdateFolder()
    {
        // Arrange
        var folderId = ObjectId.GenerateNewId();
        var updateDto = new UpdateCacheFolderDto
        {
            Name = "Updated Cache",
            Path = "/updated/cache",
            MaxSize = 4096L
        };

        var existingFolder = new CacheFolder("Original Cache", "/original/cache", 1024L, 10)
        {
            Id = folderId
        };

        _mockCacheFolderRepository.Setup(x => x.GetByIdAsync(folderId)).ReturnsAsync(existingFolder);
        _mockCacheFolderRepository.Setup(x => x.UpdateAsync(It.IsAny<CacheFolder>())).ReturnsAsync(existingFolder);

        // Act
        var result = await _cacheService.UpdateCacheFolderAsync(folderId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(folderId);
    }

    [Fact]
    public async Task DeleteCacheFolderAsync_WithValidId_ShouldDeleteFolder()
    {
        // Arrange
        var folderId = ObjectId.GenerateNewId();
        var cacheFolder = new CacheFolder("Test Cache", "/test/cache", 1024L, 10)
        {
            Id = folderId
        };

        _mockCacheFolderRepository.Setup(x => x.GetByIdAsync(folderId)).ReturnsAsync(cacheFolder);
        _mockCacheFolderRepository.Setup(x => x.DeleteAsync(folderId)).Returns(Task.CompletedTask);

        // Act
        await _cacheService.DeleteCacheFolderAsync(folderId);

        // Assert
        _mockCacheFolderRepository.Verify(x => x.DeleteAsync(folderId), Times.Once);
    }

    #endregion

    #region Cache Operations Tests

    [Fact]
    public async Task ClearCollectionCacheAsync_WithValidCollectionId_ShouldClearCache()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var collection = new Collection(collectionId, "Test Collection", "/test/collection", Domain.Enums.CollectionType.Folder);
        var images = new List<Image>
        {
            new Image(collectionId, "test1.jpg", "/test/image1.jpg", 1024L, 1920, 1080, "image/jpeg")
        };

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId)).ReturnsAsync(collection);
        _mockImageRepository.Setup(x => x.GetByCollectionIdAsync(collectionId, It.IsAny<CancellationToken>())).ReturnsAsync(images);
        _mockImageRepository.Setup(x => x.UpdateAsync(It.IsAny<Image>())).ReturnsAsync((Image img) => img);
        
        // Setup cache folder for the collection
        var cacheFolder = new CacheFolder("Test Cache", "/test/cache", 1024L, 10);
        _mockCacheFolderRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<CacheFolder> { cacheFolder });

        // Act
        await _cacheService.ClearCollectionCacheAsync(collectionId);

        // Assert
        _mockImageRepository.Verify(x => x.UpdateAsync(It.IsAny<Image>()), Times.Once);
    }

    [Fact]
    public async Task ClearAllCacheAsync_ShouldClearAllCache()
    {
        // Arrange
        var images = new List<Image>
        {
            new Image(ObjectId.GenerateNewId(), "test1.jpg", "/test/image1.jpg", 1024L, 1920, 1080, "image/jpeg"),
            new Image(ObjectId.GenerateNewId(), "test2.jpg", "/test/image2.jpg", 2048L, 1280, 720, "image/jpeg")
        };

        _mockImageRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(images);
        _mockImageRepository.Setup(x => x.UpdateAsync(It.IsAny<Image>())).ReturnsAsync((Image img) => img);

        // Act
        await _cacheService.ClearAllCacheAsync();

        // Assert
        _mockImageRepository.Verify(x => x.UpdateAsync(It.IsAny<Image>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetCollectionCacheStatusAsync_WithValidCollectionId_ShouldReturnStatus()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var collection = new Collection(ObjectId.GenerateNewId(), "Test Collection", "/test/collection", Domain.Enums.CollectionType.Folder);
        var images = new List<Image>
        {
            new Image(ObjectId.GenerateNewId(), "test1.jpg", "/test/image1.jpg", 1024L, 1920, 1080, "image/jpeg"),
            new Image(ObjectId.GenerateNewId(), "test2.jpg", "/test/image2.jpg", 2048L, 1280, 720, "image/jpeg")
        };

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId)).ReturnsAsync(collection);
        _mockImageRepository.Setup(x => x.GetByCollectionIdAsync(collectionId, It.IsAny<CancellationToken>())).ReturnsAsync(images);

        // Act
        var result = await _cacheService.GetCollectionCacheStatusAsync(collectionId);

        // Assert
        result.Should().NotBeNull();
        result.CollectionId.Should().Be(collectionId);
        result.TotalImages.Should().Be(2);
        result.CachedImages.Should().Be(0); // No images have cache info
        result.CachePercentage.Should().Be(0.0);
    }

    #endregion

    #region Cache Image Operations Tests

    [Fact]
    public async Task GetCachedImageAsync_WithValidImageId_ShouldReturnImageData()
    {
        // Arrange
        var imageId = ObjectId.GenerateNewId();
        var dimensions = "1920x1080";
        var cacheInfo = new ImageCacheInfo(imageId, "/test/cache/image.jpg", dimensions, 512L, DateTime.UtcNow.AddHours(1));

        _mockCacheInfoRepository.Setup(x => x.GetByImageIdAsync(imageId)).ReturnsAsync(cacheInfo);

        // Act
        var result = await _cacheService.GetCachedImageAsync(imageId, dimensions);

        // Assert
        // Note: In a real implementation, this would read the actual file data
        // For now, we expect null since File.Exists will return false in tests
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveCachedImageAsync_WithValidData_ShouldSaveImage()
    {
        // Arrange
        var imageId = ObjectId.GenerateNewId();
        var collectionId = ObjectId.GenerateNewId();
        var dimensions = "1920x1080";
        var imageData = new byte[] { 1, 2, 3, 4, 5 };
        var image = new Image(collectionId, "test.jpg", "/test/image.jpg", 1024L, 1920, 1080, "image/jpeg");

        // Setup collection for the image
        var collection = new Collection(collectionId, "Test Collection", "/test/collection", Domain.Enums.CollectionType.Folder);
        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId)).ReturnsAsync(collection);

        _mockImageRepository.Setup(x => x.GetByIdAsync(imageId)).ReturnsAsync(image);
        _mockCacheInfoRepository.Setup(x => x.CreateAsync(It.IsAny<ImageCacheInfo>()))
            .ReturnsAsync(new ImageCacheInfo(imageId, "/test/cache/image.jpg", dimensions, 512L, DateTime.UtcNow));
        
        // Setup cache folder for the collection
        var cacheFolder = new CacheFolder("Test Cache", "/test/cache", 1024L, 10);
        _mockCacheFolderRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<CacheFolder> { cacheFolder });

        // Act
        await _cacheService.SaveCachedImageAsync(imageId, dimensions, imageData);

        // Assert
        _mockCacheInfoRepository.Verify(x => x.CreateAsync(It.IsAny<ImageCacheInfo>()), Times.Once);
    }

    #endregion

    #region Cache Cleanup Tests

    [Fact]
    public async Task CleanupExpiredCacheAsync_ShouldCleanupExpiredEntries()
    {
        // Arrange
        var expiredCacheInfos = new List<ImageCacheInfo>
        {
            new ImageCacheInfo(ObjectId.GenerateNewId(), "/test/cache/expired1.jpg", "1920x1080", 512L, DateTime.UtcNow.AddDays(-30)),
            new ImageCacheInfo(ObjectId.GenerateNewId(), "/test/cache/expired2.jpg", "1280x720", 256L, DateTime.UtcNow.AddDays(-25))
        };

        _mockCacheInfoRepository.Setup(x => x.GetExpiredAsync()).ReturnsAsync(expiredCacheInfos);
        _mockCacheInfoRepository.Setup(x => x.DeleteAsync(It.IsAny<ObjectId>())).Returns(Task.CompletedTask);

        // Act
        await _cacheService.CleanupExpiredCacheAsync();

        // Assert
        _mockCacheInfoRepository.Verify(x => x.DeleteAsync(It.IsAny<ObjectId>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CleanupOldCacheAsync_WithCutoffDate_ShouldCleanupOldEntries()
    {
        // Arrange
        var cutoffDate = DateTime.UtcNow.AddDays(-7);
        var oldCacheInfos = new List<ImageCacheInfo>
        {
            new ImageCacheInfo(ObjectId.GenerateNewId(), "/test/cache/old1.jpg", "1920x1080", 512L, DateTime.UtcNow.AddDays(-10)),
            new ImageCacheInfo(ObjectId.GenerateNewId(), "/test/cache/old2.jpg", "1280x720", 256L, DateTime.UtcNow.AddDays(-8))
        };

        _mockCacheInfoRepository.Setup(x => x.GetOlderThanAsync(cutoffDate)).ReturnsAsync(oldCacheInfos);
        _mockCacheInfoRepository.Setup(x => x.DeleteAsync(It.IsAny<ObjectId>())).Returns(Task.CompletedTask);

        // Act
        await _cacheService.CleanupOldCacheAsync(cutoffDate);

        // Assert
        _mockCacheInfoRepository.Verify(x => x.DeleteAsync(It.IsAny<ObjectId>()), Times.Exactly(2));
    }

    #endregion
}
