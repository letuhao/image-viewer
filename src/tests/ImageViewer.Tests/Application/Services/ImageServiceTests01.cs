using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Application.DTOs.Collections;

namespace ImageViewer.Tests.Application.Services;

public class ImageServiceTests01
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IImageProcessingService> _imageProcessingMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<ImageService>> _loggerMock;
    private readonly Mock<Microsoft.Extensions.Options.IOptions<ImageViewer.Application.Options.ImageSizeOptions>> _optionsMock;
    private readonly ImageService _service;

    public ImageServiceTests01()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _imageProcessingMock = new Mock<IImageProcessingService>();
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<ImageService>>();
        _optionsMock = new Mock<Microsoft.Extensions.Options.IOptions<ImageViewer.Application.Options.ImageSizeOptions>>();

        var sizeOptions = new ImageViewer.Application.Options.ImageSizeOptions
        {
            ThumbnailWidth = 300,
            ThumbnailHeight = 300,
            CacheWidth = 1920,
            CacheHeight = 1080
        };
        _optionsMock.Setup(x => x.Value).Returns(sizeOptions);

        _service = new ImageService(
            _unitOfWorkMock.Object,
            _imageProcessingMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object,
            _optionsMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnImage()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var image = new Image(imageId, "test.jpg", "L:\\EMedia\\AI_Generated\\AiASAG\\test.jpg", 1024L, 1920, 1080, "jpg");

        var imageRepositoryMock = new Mock<IImageRepository>();
        imageRepositoryMock.Setup(x => x.GetByIdAsync(imageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);
        _unitOfWorkMock.Setup(x => x.Images).Returns(imageRepositoryMock.Object);

        // Act
        var result = await _service.GetByIdAsync(imageId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(image);
        imageRepositoryMock.Verify(x => x.GetByIdAsync(imageId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var imageRepositoryMock = new Mock<IImageRepository>();
        imageRepositoryMock.Setup(x => x.GetByIdAsync(imageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Image?)null);
        _unitOfWorkMock.Setup(x => x.Images).Returns(imageRepositoryMock.Object);

        // Act
        var result = await _service.GetByIdAsync(imageId);

        // Assert
        result.Should().BeNull();
        imageRepositoryMock.Verify(x => x.GetByIdAsync(imageId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByCollectionIdAsync_WithValidCollectionId_ShouldReturnImages()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        var images = new List<Image>
        {
            new Image(Guid.NewGuid(), "test1.jpg", "L:\\EMedia\\AI_Generated\\AiASAG\\test1.jpg", 1024L, 1920, 1080, "jpg"),
            new Image(Guid.NewGuid(), "test2.jpg", "L:\\EMedia\\AI_Generated\\AiASAG\\test2.jpg", 2048L, 1920, 1080, "jpg")
        };

        var imageRepositoryMock = new Mock<IImageRepository>();
        imageRepositoryMock.Setup(x => x.GetByCollectionIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(images);
        _unitOfWorkMock.Setup(x => x.Images).Returns(imageRepositoryMock.Object);

        // Act
        var result = await _service.GetByCollectionIdAsync(collectionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(images);
        imageRepositoryMock.Verify(x => x.GetByCollectionIdAsync(collectionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByCollectionIdAsync_WithEmptyCollection_ShouldReturnEmptyList()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        var imageRepositoryMock = new Mock<IImageRepository>();
        imageRepositoryMock.Setup(x => x.GetByCollectionIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Image>());
        _unitOfWorkMock.Setup(x => x.Images).Returns(imageRepositoryMock.Object);

        // Act
        var result = await _service.GetByCollectionIdAsync(collectionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        imageRepositoryMock.Verify(x => x.GetByCollectionIdAsync(collectionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByFormatAsync_WithValidFormat_ShouldReturnImages()
    {
        // Arrange
        var format = "jpg";
        var images = new List<Image>
        {
            new Image(Guid.NewGuid(), "test1.jpg", "L:\\EMedia\\AI_Generated\\AiASAG\\test1.jpg", 1024L, 1920, 1080, "jpg"),
            new Image(Guid.NewGuid(), "test2.jpg", "L:\\EMedia\\AI_Generated\\AiASAG\\test2.jpg", 2048L, 1920, 1080, "jpg")
        };

        var imageRepositoryMock = new Mock<IImageRepository>();
        imageRepositoryMock.Setup(x => x.GetByFormatAsync(format, It.IsAny<CancellationToken>()))
            .ReturnsAsync(images);
        _unitOfWorkMock.Setup(x => x.Images).Returns(imageRepositoryMock.Object);

        // Act
        var result = await _service.GetByFormatAsync(format);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(images);
        imageRepositoryMock.Verify(x => x.GetByFormatAsync(format, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRandomImageAsync_ShouldReturnRandomImage()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "L:\\EMedia\\AI_Generated\\AiASAG\\test.jpg", 1024L, 1920, 1080, "jpg");

        var imageRepositoryMock = new Mock<IImageRepository>();
        imageRepositoryMock.Setup(x => x.GetRandomImageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);
        _unitOfWorkMock.Setup(x => x.Images).Returns(imageRepositoryMock.Object);

        // Act
        var result = await _service.GetRandomImageAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(image);
        imageRepositoryMock.Verify(x => x.GetRandomImageAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldDeleteImage()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var image = new Image(imageId, "test.jpg", "L:\\EMedia\\AI_Generated\\AiASAG\\test.jpg", 1024L, 1920, 1080, "jpg");
        
        var imageRepositoryMock = new Mock<IImageRepository>();
        imageRepositoryMock.Setup(x => x.GetByIdAsync(imageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);
        imageRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Image>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.Images).Returns(imageRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.DeleteAsync(imageId);

        // Assert
        imageRepositoryMock.Verify(x => x.GetByIdAsync(imageId, It.IsAny<CancellationToken>()), Times.Once);
        imageRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Image>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
