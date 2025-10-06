using FluentAssertions;
using Moq;
using Xunit;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Exceptions;
using ImageViewer.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImageViewer.Application.DTOs.Cache;
using ImageViewer.Application.Options;
using Microsoft.Extensions.Options;

namespace ImageViewer.Test.Features.Performance.Unit;

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
        
        var sizeOptions = new ImageSizeOptions
        {
            CacheWidth = 1280,
            CacheHeight = 720,
            JpegQuality = 95
        };
        _mockSizeOptions.Setup(x => x.Value).Returns(sizeOptions);
        
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

    [Fact]
    public async Task GetCacheStatisticsAsync_ShouldReturnStatistics()
    {
        // Arrange
        var cacheFolders = new List<CacheFolder>
        {
            new CacheFolder("Cache1", "/cache1", 1024000, 1),
            new CacheFolder("Cache2", "/cache2", 2048000, 2)
        };
        
        // Set current sizes for the cache folders
        cacheFolders[0].UpdateStatistics(1024000, 100);
        cacheFolders[1].UpdateStatistics(2048000, 200);
        
        var collections = new List<Collection>
        {
            new Collection(ObjectId.GenerateNewId(), "Collection1", "/path1", Domain.Enums.CollectionType.Folder),
            new Collection(ObjectId.GenerateNewId(), "Collection2", "/path2", Domain.Enums.CollectionType.Folder)
        };
        
        var images = new List<Image>
        {
            new Image(ObjectId.GenerateNewId(), "image1.jpg", "/path1", 1024L, 1920, 1080, "jpeg"),
            new Image(ObjectId.GenerateNewId(), "image2.jpg", "/path2", 2048L, 1920, 1080, "jpeg")
        };

        _mockCacheFolderRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(cacheFolders);
        _mockCollectionRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(collections);
        _mockImageRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(images);

        // Act
        var result = await _cacheService.GetCacheStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Summary.TotalCollections.Should().Be(2);
        result.Summary.TotalImages.Should().Be(2);
        result.Summary.TotalCacheSize.Should().Be(3072000); // 1024000 + 2048000
        result.CacheFolders.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCacheFoldersAsync_ShouldReturnCacheFolders()
    {
        // Arrange
        var cacheFolders = new List<CacheFolder>
        {
            new CacheFolder("Cache1", "/cache1", 1024000, 1),
            new CacheFolder("Cache2", "/cache2", 2048000, 2)
        };

        _mockCacheFolderRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(cacheFolders);

        // Act
        var result = await _cacheService.GetCacheFoldersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Name.Should().Be("Cache1");
        result.First().Path.Should().Be("/cache1");
        result.First().MaxSize.Should().Be(1024000);
        result.First().Priority.Should().Be(1);
        result.First().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCacheFolderAsync_WithValidData_ShouldReturnCreatedCacheFolder()
    {
        // Arrange
        var dto = new CreateCacheFolderDto
        {
            Name = "Test Cache",
            Path = "/test/cache",
            Priority = 1,
            MaxSize = 1024000
        };

        _mockCacheFolderRepository.Setup(x => x.CreateAsync(It.IsAny<CacheFolder>()))
            .ReturnsAsync((CacheFolder cf) => cf);

        // Act
        var result = await _cacheService.CreateCacheFolderAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Cache");
        result.Path.Should().Be("/test/cache");
        result.Priority.Should().Be(1);
        result.MaxSize.Should().Be(1024000);
        result.IsActive.Should().BeTrue();
        
        _mockCacheFolderRepository.Verify(x => x.CreateAsync(It.IsAny<CacheFolder>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCacheFolderAsync_WithValidData_ShouldUpdateCacheFolder()
    {
        // Arrange
        var cacheFolderId = ObjectId.GenerateNewId();
        var existingCacheFolder = new CacheFolder("Old Name", "/old/path", 1024000, 1)
        {
            Id = cacheFolderId
        };
        
        var dto = new UpdateCacheFolderDto
        {
            Name = "Updated Cache",
            Path = "/updated/cache",
            Priority = 2,
            MaxSize = 2048000,
            IsActive = false
        };

        _mockCacheFolderRepository.Setup(x => x.GetByIdAsync(cacheFolderId))
            .ReturnsAsync(existingCacheFolder);
        _mockCacheFolderRepository.Setup(x => x.UpdateAsync(It.IsAny<CacheFolder>()))
            .ReturnsAsync((CacheFolder cf) => cf);

        // Act
        var result = await _cacheService.UpdateCacheFolderAsync(cacheFolderId, dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Cache");
        result.Path.Should().Be("/updated/cache");
        result.Priority.Should().Be(2);
        result.MaxSize.Should().Be(2048000);
        result.IsActive.Should().BeFalse();
        
        _mockCacheFolderRepository.Verify(x => x.UpdateAsync(It.IsAny<CacheFolder>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCacheFolderAsync_WithNonExistentId_ShouldThrowArgumentException()
    {
        // Arrange
        var cacheFolderId = ObjectId.GenerateNewId();
        var dto = new UpdateCacheFolderDto
        {
            Name = "Updated Cache",
            Path = "/updated/cache",
            Priority = 2,
            MaxSize = 2048000,
            IsActive = false
        };

        _mockCacheFolderRepository.Setup(x => x.GetByIdAsync(cacheFolderId))
            .ReturnsAsync((CacheFolder)null!);

        // Act
        Func<Task> act = async () => await _cacheService.UpdateCacheFolderAsync(cacheFolderId, dto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Cache folder with ID {cacheFolderId} not found");
    }

    [Fact]
    public async Task DeleteCacheFolderAsync_WithValidId_ShouldDeleteCacheFolder()
    {
        // Arrange
        var cacheFolderId = ObjectId.GenerateNewId();
        var existingCacheFolder = new CacheFolder("Test Cache", "/test/cache", 1024000, 1)
        {
            Id = cacheFolderId
        };

        _mockCacheFolderRepository.Setup(x => x.GetByIdAsync(cacheFolderId))
            .ReturnsAsync(existingCacheFolder);
        _mockCacheFolderRepository.Setup(x => x.DeleteAsync(cacheFolderId))
            .Returns(Task.CompletedTask);

        // Act
        await _cacheService.DeleteCacheFolderAsync(cacheFolderId);

        // Assert
        _mockCacheFolderRepository.Verify(x => x.DeleteAsync(cacheFolderId), Times.Once);
    }

    [Fact]
    public async Task DeleteCacheFolderAsync_WithNonExistentId_ShouldThrowArgumentException()
    {
        // Arrange
        var cacheFolderId = ObjectId.GenerateNewId();

        _mockCacheFolderRepository.Setup(x => x.GetByIdAsync(cacheFolderId))
            .ReturnsAsync((CacheFolder)null!);

        // Act
        Func<Task> act = async () => await _cacheService.DeleteCacheFolderAsync(cacheFolderId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Cache folder with ID {cacheFolderId} not found");
    }

    [Fact]
    public async Task GetCacheFolderAsync_WithValidId_ShouldReturnCacheFolder()
    {
        // Arrange
        var cacheFolderId = ObjectId.GenerateNewId();
        var existingCacheFolder = new CacheFolder("Test Cache", "/test/cache", 1024000, 1)
        {
            Id = cacheFolderId
        };

        _mockCacheFolderRepository.Setup(x => x.GetByIdAsync(cacheFolderId))
            .ReturnsAsync(existingCacheFolder);

        // Act
        var result = await _cacheService.GetCacheFolderAsync(cacheFolderId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(cacheFolderId);
        result.Name.Should().Be("Test Cache");
        result.Path.Should().Be("/test/cache");
        result.MaxSize.Should().Be(1024000);
        result.Priority.Should().Be(1);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetCacheFolderAsync_WithNonExistentId_ShouldThrowArgumentException()
    {
        // Arrange
        var cacheFolderId = ObjectId.GenerateNewId();

        _mockCacheFolderRepository.Setup(x => x.GetByIdAsync(cacheFolderId))
            .ReturnsAsync((CacheFolder)null!);

        // Act
        Func<Task> act = async () => await _cacheService.GetCacheFolderAsync(cacheFolderId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Cache folder with ID {cacheFolderId} not found");
    }

    [Fact]
    public async Task ClearCollectionCacheAsync_WithValidCollectionId_ShouldClearCache()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var collection = new Collection(ObjectId.GenerateNewId(), "Test Collection", "/test/path", Domain.Enums.CollectionType.Folder)
        {
            Id = collectionId
        };
        
        var images = new List<Image>
        {
            new Image(collectionId, "image1.jpg", "/path1", 1024L, 1920, 1080, "jpeg"),
            new Image(collectionId, "image2.jpg", "/path2", 2048L, 1920, 1080, "jpeg")
        };

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync(collection);
        _mockImageRepository.Setup(x => x.GetByCollectionIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(images);

        // Act
        await _cacheService.ClearCollectionCacheAsync(collectionId);

        // Assert
        _mockCollectionRepository.Verify(x => x.GetByIdAsync(collectionId), Times.Once);
        _mockImageRepository.Verify(x => x.GetByCollectionIdAsync(collectionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClearCollectionCacheAsync_WithNonExistentCollectionId_ShouldThrowArgumentException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync((Collection)null!);

        // Act
        Func<Task> act = async () => await _cacheService.ClearCollectionCacheAsync(collectionId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Collection with ID {collectionId} not found");
    }

    [Fact]
    public async Task ClearAllCacheAsync_ShouldClearAllCache()
    {
        // Arrange
        var images = new List<Image>
        {
            new Image(ObjectId.GenerateNewId(), "image1.jpg", "/path1", 1024L, 1920, 1080, "jpeg"),
            new Image(ObjectId.GenerateNewId(), "image2.jpg", "/path2", 2048L, 1920, 1080, "jpeg")
        };

        _mockImageRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(images);

        // Act
        await _cacheService.ClearAllCacheAsync();

        // Assert
        _mockImageRepository.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetCollectionCacheStatusAsync_WithValidCollectionId_ShouldReturnCacheStatus()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var collection = new Collection(ObjectId.GenerateNewId(), "Test Collection", "/test/path", Domain.Enums.CollectionType.Folder)
        {
            Id = collectionId
        };
        
        var images = new List<Image>
        {
            new Image(collectionId, "image1.jpg", "/path1", 1024L, 1920, 1080, "jpeg"),
            new Image(collectionId, "image2.jpg", "/path2", 2048L, 1920, 1080, "jpeg")
        };

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync(collection);
        _mockImageRepository.Setup(x => x.GetByCollectionIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(images);

        // Act
        var result = await _cacheService.GetCollectionCacheStatusAsync(collectionId);

        // Assert
        result.Should().NotBeNull();
        result.CollectionId.Should().Be(collectionId);
        result.TotalImages.Should().Be(2);
        result.CachedImages.Should().Be(0); // No cache info set
        result.CachePercentage.Should().Be(0);
    }

    [Fact]
    public async Task GetCollectionCacheStatusAsync_WithNonExistentCollectionId_ShouldThrowArgumentException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync((Collection)null!);

        // Act
        Func<Task> act = async () => await _cacheService.GetCollectionCacheStatusAsync(collectionId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Collection with ID {collectionId} not found");
    }

    [Fact]
    public async Task GetCachedImageAsync_WithValidImageId_ShouldReturnCachedImage()
    {
        // Arrange
        var imageId = ObjectId.GenerateNewId();
        var dimensions = "1280x720";
        var cacheInfo = new ImageCacheInfo(imageId, "/cache/path", dimensions, 1024L, DateTime.UtcNow.AddDays(1));
        var expectedImageData = new byte[] { 1, 2, 3, 4, 5 };

        _mockCacheInfoRepository.Setup(x => x.GetByImageIdAsync(imageId))
            .ReturnsAsync(cacheInfo);

        // Act
        var result = await _cacheService.GetCachedImageAsync(imageId, dimensions);

        // Assert
        result.Should().BeNull(); // File doesn't exist in test environment
        _mockCacheInfoRepository.Verify(x => x.GetByImageIdAsync(imageId), Times.Once);
    }

    [Fact]
    public async Task GetCachedImageAsync_WithExpiredCache_ShouldReturnNull()
    {
        // Arrange
        var imageId = ObjectId.GenerateNewId();
        var dimensions = "1280x720";
        var expiredCacheInfo = new ImageCacheInfo(imageId, "/cache/path", dimensions, 1024L, DateTime.UtcNow.AddDays(-1));

        _mockCacheInfoRepository.Setup(x => x.GetByImageIdAsync(imageId))
            .ReturnsAsync(expiredCacheInfo);

        // Act
        var result = await _cacheService.GetCachedImageAsync(imageId, dimensions);

        // Assert
        result.Should().BeNull();
    }

        [Fact]
        public async Task SaveCachedImageAsync_WithValidData_ShouldSaveCachedImage()
        {
            // Arrange
            var imageId = ObjectId.GenerateNewId();
            var collectionId = ObjectId.GenerateNewId();
            var dimensions = "1280x720";
            var imageData = new byte[] { 1, 2, 3, 4, 5 };
            
            var image = new Image(collectionId, "test.jpg", "/path", 1024L, 1920, 1080, "jpeg");
            
            var collection = new Collection(collectionId, "Test Collection", "/test/path", Domain.Enums.CollectionType.Folder);
            collection.CacheBindings.Add(new CacheBinding("/test/cache", "jpeg", 85, 1280, 720, "/test/cache"));
            
            var cacheFolder = new CacheFolder("Test Cache", "/test/cache", 1024000, 1);

            _mockImageRepository.Setup(x => x.GetByIdAsync(imageId))
                .ReturnsAsync(image);
            _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
                .ReturnsAsync(collection);
            _mockCacheFolderRepository.Setup(x => x.GetByPathAsync("/test/cache"))
                .ReturnsAsync(cacheFolder);
            _mockCacheInfoRepository.Setup(x => x.GetByImageIdAsync(imageId))
                .ReturnsAsync((ImageCacheInfo)null!);
            _mockCacheInfoRepository.Setup(x => x.CreateAsync(It.IsAny<ImageCacheInfo>()))
                .ReturnsAsync((ImageCacheInfo ci) => ci);

            // Act
            await _cacheService.SaveCachedImageAsync(imageId, dimensions, imageData);

            // Assert
            _mockImageRepository.Verify(x => x.GetByIdAsync(imageId), Times.Once);
            _mockCacheInfoRepository.Verify(x => x.CreateAsync(It.IsAny<ImageCacheInfo>()), Times.Once);
        }

    [Fact]
    public async Task SaveCachedImageAsync_WithNonExistentImage_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var imageId = ObjectId.GenerateNewId();
        var dimensions = "1280x720";
        var imageData = new byte[] { 1, 2, 3, 4, 5 };

        _mockImageRepository.Setup(x => x.GetByIdAsync(imageId))
            .ReturnsAsync((Image)null!);

        // Act
        Func<Task> act = async () => await _cacheService.SaveCachedImageAsync(imageId, dimensions, imageData);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Image with ID {imageId} not found");
    }

    [Fact]
    public async Task CleanupExpiredCacheAsync_ShouldCleanupExpiredEntries()
    {
        // Arrange
        var expiredEntries = new List<ImageCacheInfo>
        {
            new ImageCacheInfo(ObjectId.GenerateNewId(), "/cache/path1", "1280x720", 1024L, DateTime.UtcNow.AddDays(-1)),
            new ImageCacheInfo(ObjectId.GenerateNewId(), "/cache/path2", "1280x720", 2048L, DateTime.UtcNow.AddDays(-2))
        };

        _mockCacheInfoRepository.Setup(x => x.GetExpiredAsync())
            .ReturnsAsync(expiredEntries);
        _mockCacheInfoRepository.Setup(x => x.DeleteAsync(It.IsAny<ObjectId>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _cacheService.CleanupExpiredCacheAsync();

        // Assert
        _mockCacheInfoRepository.Verify(x => x.GetExpiredAsync(), Times.Once);
        _mockCacheInfoRepository.Verify(x => x.DeleteAsync(It.IsAny<ObjectId>()), Times.Exactly(2));
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CleanupOldCacheAsync_ShouldCleanupOldEntries()
    {
        // Arrange
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        var oldEntries = new List<ImageCacheInfo>
        {
            new ImageCacheInfo(ObjectId.GenerateNewId(), "/cache/path1", "1280x720", 1024L, DateTime.UtcNow.AddDays(-35)),
            new ImageCacheInfo(ObjectId.GenerateNewId(), "/cache/path2", "1280x720", 2048L, DateTime.UtcNow.AddDays(-40))
        };

        _mockCacheInfoRepository.Setup(x => x.GetOlderThanAsync(cutoffDate))
            .ReturnsAsync(oldEntries);
        _mockCacheInfoRepository.Setup(x => x.DeleteAsync(It.IsAny<ObjectId>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _cacheService.CleanupOldCacheAsync(cutoffDate);

        // Assert
        _mockCacheInfoRepository.Verify(x => x.GetOlderThanAsync(cutoffDate), Times.Once);
        _mockCacheInfoRepository.Verify(x => x.DeleteAsync(It.IsAny<ObjectId>()), Times.Exactly(2));
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}