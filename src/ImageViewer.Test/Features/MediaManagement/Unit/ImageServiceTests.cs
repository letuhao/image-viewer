using FluentAssertions;
using Moq;
using Xunit;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ImageViewer.Application.Options;

namespace ImageViewer.Test.Features.MediaManagement.Unit;

/// <summary>
/// Unit tests for ImageService with embedded design
/// </summary>
public class ImageServiceTests
{
    private readonly Mock<ICollectionRepository> _mockCollectionRepository;
    private readonly Mock<IImageProcessingService> _mockImageProcessingService;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ILogger<ImageService>> _mockLogger;
    private readonly Mock<IOptions<ImageSizeOptions>> _mockImageSizeOptions;
    private readonly ImageService _imageService;

    public ImageServiceTests()
    {
        _mockCollectionRepository = new Mock<ICollectionRepository>();
        _mockImageProcessingService = new Mock<IImageProcessingService>();
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<ImageService>>();
        _mockImageSizeOptions = new Mock<IOptions<ImageSizeOptions>>();
        
        var imageSizeOptions = new ImageSizeOptions
        {
            ThumbnailWidth = 300,
            ThumbnailHeight = 300,
            CacheWidth = 1920,
            CacheHeight = 1080,
            JpegQuality = 95
        };
        
        _mockImageSizeOptions.Setup(x => x.Value).Returns(imageSizeOptions);

        var mockCollectionService = new Mock<ICollectionService>();
        _imageService = new ImageService(
            _mockCollectionRepository.Object,
            _mockImageProcessingService.Object,
            _mockCacheService.Object,
            _mockLogger.Object,
            _mockImageSizeOptions.Object,
            mockCollectionService.Object);
    }

    [Fact]
    public async Task CreateEmbeddedImageAsync_WithValidData_ShouldCreateAndReturnImage()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var collection = new Collection(
            ObjectId.Empty, // libraryId
            "Test Collection", // name
            "/path/to/collection", // path
            CollectionType.Folder); // type

        _mockCollectionRepository
            .Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync(collection);

        _mockCollectionRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Collection>()))
            .ReturnsAsync((Collection c) => c);

        // Act
        var result = await _imageService.CreateEmbeddedImageAsync(
            collectionId,
            "test.jpg",
            "/images/test.jpg",
            1024000,
            1920,
            1080,
            ".jpg");

        // Assert
        result.Should().NotBeNull();
        result!.Filename.Should().Be("test.jpg");
        result.GetDisplayPath().Should().Be("/images/test.jpg");
        result.FileSize.Should().Be(1024000);
        result.Width.Should().Be(1920);
        result.Height.Should().Be(1080);
        result.Format.Should().Be(".jpg");
        
        _mockCollectionRepository.Verify(x => x.UpdateAsync(It.IsAny<Collection>()), Times.Once);
    }

    [Fact]
    public async Task GetEmbeddedImageByIdAsync_WithExistingImage_ShouldReturnImage()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var collection = new Collection(
            ObjectId.Empty, // libraryId
            "Test Collection", // name
            "/path/to/collection", // path
            CollectionType.Folder); // type

        var embeddedImage = new ImageEmbedded("test.jpg", "/images/test.jpg", 1024000, 1920, 1080, ".jpg");
        collection.AddImage(embeddedImage);

        _mockCollectionRepository
            .Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync(collection);

        // Act
        var result = await _imageService.GetEmbeddedImageByIdAsync(embeddedImage.Id, collectionId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(embeddedImage.Id);
        result.Filename.Should().Be("test.jpg");
    }

    [Fact]
    public async Task GetEmbeddedImagesByCollectionAsync_WithImages_ShouldReturnAllActiveImages()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var collection = new Collection(
            ObjectId.Empty, // libraryId
            "Test Collection", // name
            "/path/to/collection", // path
            CollectionType.Folder); // type

        var image1 = new ImageEmbedded("test1.jpg", "/images/test1.jpg", 1024000, 1920, 1080, ".jpg");
        var image2 = new ImageEmbedded("test2.jpg", "/images/test2.jpg", 2048000, 3840, 2160, ".jpg");
        collection.AddImage(image1);
        collection.AddImage(image2);

        _mockCollectionRepository
            .Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync(collection);

        // Act
        var result = await _imageService.GetEmbeddedImagesByCollectionAsync(collectionId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(img => img.Filename == "test1.jpg");
        result.Should().Contain(img => img.Filename == "test2.jpg");
    }

    [Fact]
    public async Task DeleteEmbeddedImageAsync_WithExistingImage_ShouldMarkAsDeleted()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var collection = new Collection(
            ObjectId.Empty, // libraryId
            "Test Collection", // name
            "/path/to/collection", // path
            CollectionType.Folder); // type

        var embeddedImage = new ImageEmbedded("test.jpg", "/images/test.jpg", 1024000, 1920, 1080, ".jpg");
        collection.AddImage(embeddedImage);

        _mockCollectionRepository
            .Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync(collection);

        _mockCollectionRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Collection>()))
            .ReturnsAsync((Collection c) => c);

        // Act
        await _imageService.DeleteEmbeddedImageAsync(embeddedImage.Id, collectionId);

        // Assert
        var deletedImages = collection.GetDeletedImages();
        deletedImages.Should().HaveCount(1);
        deletedImages.First().Id.Should().Be(embeddedImage.Id);
        
        _mockCollectionRepository.Verify(x => x.UpdateAsync(It.IsAny<Collection>()), Times.Once);
    }

    [Fact]
    public async Task GetCountByCollectionAsync_WithImages_ShouldReturnCorrectCount()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var collection = new Collection(
            ObjectId.Empty, // libraryId
            "Test Collection", // name
            "/path/to/collection", // path
            CollectionType.Folder); // type

        for (int i = 0; i < 10; i++)
        {
            var image = new ImageEmbedded($"test{i}.jpg", $"/images/test{i}.jpg", 1024000, 1920, 1080, ".jpg");
            collection.AddImage(image);
        }

        _mockCollectionRepository
            .Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync(collection);

        // Act
        var result = await _imageService.GetCountByCollectionAsync(collectionId);

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    public async Task GetTotalSizeByCollectionAsync_WithImages_ShouldReturnCorrectSize()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var collection = new Collection(
            ObjectId.Empty, // libraryId
            "Test Collection", // name
            "/path/to/collection", // path
            CollectionType.Folder); // type

        collection.AddImage(new ImageEmbedded("test1.jpg", "/images/test1.jpg", 1024000, 1920, 1080, ".jpg"));
        collection.AddImage(new ImageEmbedded("test2.jpg", "/images/test2.jpg", 2048000, 1920, 1080, ".jpg"));
        collection.AddImage(new ImageEmbedded("test3.jpg", "/images/test3.jpg", 3072000, 1920, 1080, ".jpg"));

        _mockCollectionRepository
            .Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync(collection);

        // Act
        var result = await _imageService.GetTotalSizeByCollectionAsync(collectionId);

        // Assert
        result.Should().Be(1024000 + 2048000 + 3072000);
    }
}

