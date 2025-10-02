using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using FluentAssertions;
using ImageViewer.Infrastructure.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Application.Options;

namespace ImageViewer.Tests.Infrastructure.Services;

public class AdvancedThumbnailServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IImageProcessingService> _imageProcessingServiceMock;
    private readonly Mock<ILogger<AdvancedThumbnailService>> _loggerMock;
    private readonly Mock<IOptions<ImageSizeOptions>> _optionsMock;
    private readonly AdvancedThumbnailService _service;

    public AdvancedThumbnailServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _imageProcessingServiceMock = new Mock<IImageProcessingService>();
        _loggerMock = new Mock<ILogger<AdvancedThumbnailService>>();
        _optionsMock = new Mock<IOptions<ImageSizeOptions>>();

        var sizeOptions = new ImageSizeOptions
        {
            ThumbnailWidth = 300,
            ThumbnailHeight = 300,
            CacheWidth = 1920,
            CacheHeight = 1080
        };
        _optionsMock.Setup(x => x.Value).Returns(sizeOptions);

        _service = new AdvancedThumbnailService(_unitOfWorkMock.Object, _imageProcessingServiceMock.Object, _loggerMock.Object, _optionsMock.Object);
    }

    [Fact]
    public void AdvancedThumbnailService_ShouldBeCreated()
    {
        // Arrange & Act
        var service = new AdvancedThumbnailService(_unitOfWorkMock.Object, _imageProcessingServiceMock.Object, _loggerMock.Object, _optionsMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new AdvancedThumbnailService(null!, _imageProcessingServiceMock.Object, _loggerMock.Object, _optionsMock.Object));
        
        exception.ParamName.Should().Be("unitOfWork");
    }

    [Fact]
    public void Constructor_WithNullImageProcessingService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new AdvancedThumbnailService(_unitOfWorkMock.Object, null!, _loggerMock.Object, _optionsMock.Object));
        
        exception.ParamName.Should().Be("imageProcessingService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new AdvancedThumbnailService(_unitOfWorkMock.Object, _imageProcessingServiceMock.Object, null!, _optionsMock.Object));
        
        exception.ParamName.Should().Be("logger");
    }

    [Fact]
    public async Task GenerateCollectionThumbnailAsync_WithNonExistentCollection_ShouldReturnNull()
    {
        // Arrange
        var nonExistentCollectionId = Guid.NewGuid();
        var collectionRepositoryMock = new Mock<ICollectionRepository>();
        collectionRepositoryMock.Setup(x => x.GetByIdAsync(nonExistentCollectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);
        _unitOfWorkMock.Setup(x => x.Collections).Returns(collectionRepositoryMock.Object);

        // Act
        var result = await _service.GenerateCollectionThumbnailAsync(nonExistentCollectionId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCollectionThumbnailAsync_WithNonExistentCollection_ShouldReturnNull()
    {
        // Arrange
        var nonExistentCollectionId = Guid.NewGuid();
        var collectionRepositoryMock = new Mock<ICollectionRepository>();
        collectionRepositoryMock.Setup(x => x.GetByIdAsync(nonExistentCollectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);
        _unitOfWorkMock.Setup(x => x.Collections).Returns(collectionRepositoryMock.Object);

        // Act
        var result = await _service.GetCollectionThumbnailAsync(nonExistentCollectionId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCollectionThumbnailAsync_WithNonExistentCollection_ShouldComplete()
    {
        // Arrange
        var nonExistentCollectionId = Guid.NewGuid();
        var collectionRepositoryMock = new Mock<ICollectionRepository>();
        collectionRepositoryMock.Setup(x => x.GetByIdAsync(nonExistentCollectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);
        _unitOfWorkMock.Setup(x => x.Collections).Returns(collectionRepositoryMock.Object);

        // Act & Assert
        await _service.DeleteCollectionThumbnailAsync(nonExistentCollectionId);
        // Should not throw exception
    }

    [Fact]
    public async Task BatchRegenerateThumbnailsAsync_WithEmptyCollectionList_ShouldReturnEmptyResult()
    {
        // Arrange
        var emptyCollectionIds = new List<Guid>();

        // Act
        var result = await _service.BatchRegenerateThumbnailsAsync(emptyCollectionIds);

        // Assert
        result.Should().NotBeNull();
        result.Total.Should().Be(0);
        result.Success.Should().Be(0);
        result.Failed.Should().Be(0);
        result.SuccessfulCollections.Should().BeEmpty();
        result.FailedCollections.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task BatchRegenerateThumbnailsAsync_WithNullCollectionList_ShouldReturnEmptyResult()
    {
        // Act
        var result = await _service.BatchRegenerateThumbnailsAsync(null!);

        // Assert
        result.Should().NotBeNull();
        result.Total.Should().Be(0);
        result.Success.Should().Be(0);
        result.Failed.Should().Be(0);
        result.SuccessfulCollections.Should().BeEmpty();
        result.FailedCollections.Should().BeEmpty();
        result.Errors.Should().BeEmpty();
    }
}
