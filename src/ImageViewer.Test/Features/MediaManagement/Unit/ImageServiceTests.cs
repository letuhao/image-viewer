using FluentAssertions;
using Moq;
using Xunit;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace ImageViewer.Test.Features.MediaManagement.Unit;

/// <summary>
/// Unit tests for ImageService - Image Management features
/// </summary>
public class ImageServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IImageProcessingService> _mockImageProcessingService;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ILogger<ImageService>> _mockLogger;
    private readonly Mock<IOptions<ImageSizeOptions>> _mockSizeOptions;
    private readonly ImageService _imageService;

    public ImageServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockImageProcessingService = new Mock<IImageProcessingService>();
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<ImageService>>();
        _mockSizeOptions = new Mock<IOptions<ImageSizeOptions>>();

        _mockSizeOptions.Setup(x => x.Value).Returns(new ImageSizeOptions());

        _imageService = new ImageService(
            _mockUnitOfWork.Object,
            _mockImageProcessingService.Object,
            _mockCacheService.Object,
            _mockLogger.Object,
            _mockSizeOptions.Object
        );
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnImage()
    {
        // Arrange
        var imageId = ObjectId.GenerateNewId();
        var collectionId = ObjectId.GenerateNewId();
        var expectedImage = new Image(collectionId, "test.jpg", "/path/to/test.jpg", 1024L, 1920, 1080, "jpeg")
        {
            Id = imageId
        };

        _mockUnitOfWork.Setup(x => x.Images.GetByIdAsync(imageId))
            .ReturnsAsync(expectedImage);

        // Act
        var result = await _imageService.GetByIdAsync(imageId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(imageId);
        result.Filename.Should().Be(expectedImage.Filename);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var imageId = ObjectId.GenerateNewId();
        _mockUnitOfWork.Setup(x => x.Images.GetByIdAsync(imageId))
            .ReturnsAsync((Image?)null);

        // Act
        var result = await _imageService.GetByIdAsync(imageId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByCollectionIdAndFilenameAsync_WithValidData_ShouldReturnImage()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var filename = "test.jpg";
        var expectedImage = new Image(collectionId, filename, "/path/to/test.jpg", 1024L, 1920, 1080, "jpeg");

        _mockUnitOfWork.Setup(x => x.Images.GetByCollectionIdAndFilenameAsync(collectionId, filename, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedImage);

        // Act
        var result = await _imageService.GetByCollectionIdAndFilenameAsync(collectionId, filename);

        // Assert
        result.Should().NotBeNull();
        result.Filename.Should().Be(filename);
        result.CollectionId.Should().Be(collectionId);
    }

    [Fact]
    public async Task GetByCollectionIdAndFilenameAsync_WithNonExistentData_ShouldReturnNull()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var filename = "nonexistent.jpg";
        _mockUnitOfWork.Setup(x => x.Images.GetByCollectionIdAndFilenameAsync(collectionId, filename, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Image?)null);

        // Act
        var result = await _imageService.GetByCollectionIdAndFilenameAsync(collectionId, filename);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByCollectionIdAsync_WithValidCollectionId_ShouldReturnImages()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var images = new List<Image>
        {
            new Image(collectionId, "image1.jpg", "/path1", 1024L, 1920, 1080, "jpeg"),
            new Image(collectionId, "image2.jpg", "/path2", 2048L, 1920, 1080, "jpeg")
        };

        _mockUnitOfWork.Setup(x => x.Images.GetByCollectionIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(images);

        // Act
        var result = await _imageService.GetByCollectionIdAsync(collectionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(i => i.Filename == "image1.jpg");
        result.Should().Contain(i => i.Filename == "image2.jpg");
    }

    [Fact]
    public async Task GetByFormatAsync_WithValidFormat_ShouldReturnImages()
    {
        // Arrange
        var format = "jpeg";
        var collectionId = ObjectId.GenerateNewId();
        var images = new List<Image>
        {
            new Image(collectionId, "image1.jpg", "/path1", 1024L, 1920, 1080, format),
            new Image(collectionId, "image2.jpg", "/path2", 2048L, 1920, 1080, format)
        };

        _mockUnitOfWork.Setup(x => x.Images.GetByFormatAsync(format, It.IsAny<CancellationToken>()))
            .ReturnsAsync(images);

        // Act
        var result = await _imageService.GetByFormatAsync(format);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(i => i.Format.Should().Be(format));
    }

    [Fact]
    public async Task GetBySizeRangeAsync_WithValidRange_ShouldReturnImages()
    {
        // Arrange
        var minWidth = 1920;
        var minHeight = 1080;
        var collectionId = ObjectId.GenerateNewId();
        var images = new List<Image>
        {
            new Image(collectionId, "image1.jpg", "/path1", 1024L, 1920, 1080, "jpeg"),
            new Image(collectionId, "image2.jpg", "/path2", 2048L, 2560, 1440, "jpeg")
        };

        _mockUnitOfWork.Setup(x => x.Images.GetBySizeRangeAsync(minWidth, minHeight, It.IsAny<CancellationToken>()))
            .ReturnsAsync(images);

        // Act
        var result = await _imageService.GetBySizeRangeAsync(minWidth, minHeight);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(i => i.Width.Should().BeGreaterOrEqualTo(minWidth));
        result.Should().AllSatisfy(i => i.Height.Should().BeGreaterOrEqualTo(minHeight));
    }

    [Fact]
    public async Task GetLargeImagesAsync_WithValidSize_ShouldReturnImages()
    {
        // Arrange
        var minSizeBytes = 1024L;
        var collectionId = ObjectId.GenerateNewId();
        var images = new List<Image>
        {
            new Image(collectionId, "image1.jpg", "/path1", 2048L, 1920, 1080, "jpeg"),
            new Image(collectionId, "image2.jpg", "/path2", 4096L, 1920, 1080, "jpeg")
        };

        _mockUnitOfWork.Setup(x => x.Images.GetLargeImagesAsync(minSizeBytes, It.IsAny<CancellationToken>()))
            .ReturnsAsync(images);

        // Act
        var result = await _imageService.GetLargeImagesAsync(minSizeBytes);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(i => i.FileSize.Should().BeGreaterOrEqualTo(minSizeBytes));
    }

    [Fact]
    public async Task GetHighResolutionImagesAsync_WithValidResolution_ShouldReturnImages()
    {
        // Arrange
        var minWidth = 1920;
        var minHeight = 1080;
        var collectionId = ObjectId.GenerateNewId();
        var images = new List<Image>
        {
            new Image(collectionId, "image1.jpg", "/path1", 1024L, 1920, 1080, "jpeg"),
            new Image(collectionId, "image2.jpg", "/path2", 2048L, 2560, 1440, "jpeg")
        };

        _mockUnitOfWork.Setup(x => x.Images.GetHighResolutionImagesAsync(minWidth, minHeight, It.IsAny<CancellationToken>()))
            .ReturnsAsync(images);

        // Act
        var result = await _imageService.GetHighResolutionImagesAsync(minWidth, minHeight);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(i => i.Width.Should().BeGreaterOrEqualTo(minWidth));
        result.Should().AllSatisfy(i => i.Height.Should().BeGreaterOrEqualTo(minHeight));
    }

    [Fact]
    public async Task GetRandomImageAsync_ShouldReturnRandomImage()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var expectedImage = new Image(collectionId, "random.jpg", "/path", 1024L, 1920, 1080, "jpeg");

        _mockUnitOfWork.Setup(x => x.Images.GetRandomImageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedImage);

        // Act
        var result = await _imageService.GetRandomImageAsync();

        // Assert
        result.Should().NotBeNull();
        result.Filename.Should().Be("random.jpg");
    }

    [Fact]
    public async Task GetRandomImageByCollectionAsync_WithValidCollectionId_ShouldReturnRandomImage()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var expectedImage = new Image(collectionId, "random.jpg", "/path", 1024L, 1920, 1080, "jpeg");

        _mockUnitOfWork.Setup(x => x.Images.GetRandomImageByCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedImage);

        // Act
        var result = await _imageService.GetRandomImageByCollectionAsync(collectionId);

        // Assert
        result.Should().NotBeNull();
        result.Filename.Should().Be("random.jpg");
        result.CollectionId.Should().Be(collectionId);
    }

    [Fact]
    public async Task GetNextImageAsync_WithValidCurrentImageId_ShouldReturnNextImage()
    {
        // Arrange
        var currentImageId = ObjectId.GenerateNewId();
        var collectionId = ObjectId.GenerateNewId();
        var nextImage = new Image(collectionId, "next.jpg", "/path", 1024L, 1920, 1080, "jpeg");

        _mockUnitOfWork.Setup(x => x.Images.GetNextImageAsync(currentImageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nextImage);

        // Act
        var result = await _imageService.GetNextImageAsync(currentImageId);

        // Assert
        result.Should().NotBeNull();
        result.Filename.Should().Be("next.jpg");
    }

    [Fact]
    public async Task GetPreviousImageAsync_WithValidCurrentImageId_ShouldReturnPreviousImage()
    {
        // Arrange
        var currentImageId = ObjectId.GenerateNewId();
        var collectionId = ObjectId.GenerateNewId();
        var previousImage = new Image(collectionId, "previous.jpg", "/path", 1024L, 1920, 1080, "jpeg");

        _mockUnitOfWork.Setup(x => x.Images.GetPreviousImageAsync(currentImageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousImage);

        // Act
        var result = await _imageService.GetPreviousImageAsync(currentImageId);

        // Assert
        result.Should().NotBeNull();
        result.Filename.Should().Be("previous.jpg");
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldDeleteImage()
    {
        // Arrange
        var imageId = ObjectId.GenerateNewId();
        var collectionId = ObjectId.GenerateNewId();
        var image = new Image(collectionId, "test.jpg", "/path", 1024L, 1920, 1080, "jpeg")
        {
            Id = imageId
        };

        _mockUnitOfWork.Setup(x => x.Images.GetByIdAsync(imageId))
            .ReturnsAsync(image);
        _mockUnitOfWork.Setup(x => x.Images.UpdateAsync(It.IsAny<Image>()))
            .ReturnsAsync((Image img) => img);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _imageService.DeleteAsync(imageId);

        // Assert
        _mockUnitOfWork.Verify(x => x.Images.UpdateAsync(It.IsAny<Image>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}