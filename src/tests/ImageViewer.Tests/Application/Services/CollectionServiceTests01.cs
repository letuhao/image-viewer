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

public class CollectionServiceTests01
{
    private readonly Mock<ICollectionRepository> _collectionRepositoryMock;
    private readonly Mock<IFileScannerService> _fileScannerMock;
    private readonly Mock<ILogger<CollectionService>> _loggerMock;
    private readonly Mock<Microsoft.Extensions.Options.IOptions<ImageViewer.Application.Options.ImageSizeOptions>> _optionsMock;
    private readonly CollectionService _service;

    public CollectionServiceTests01()
    {
        _collectionRepositoryMock = new Mock<ICollectionRepository>();
        _fileScannerMock = new Mock<IFileScannerService>();
        _loggerMock = new Mock<ILogger<CollectionService>>();
        _optionsMock = new Mock<Microsoft.Extensions.Options.IOptions<ImageViewer.Application.Options.ImageSizeOptions>>();
        
        var sizeOptions = new ImageViewer.Application.Options.ImageSizeOptions
        {
            ThumbnailWidth = 300,
            ThumbnailHeight = 300,
            CacheWidth = 1920,
            CacheHeight = 1080
        };
        _optionsMock.Setup(x => x.Value).Returns(sizeOptions);

        _service = new CollectionService(
            _collectionRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnCollection()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        var collection = new Collection("Test Collection", "C:\\test\\path", CollectionType.Folder);
        
        var collectionRepositoryMock = new Mock<ICollectionRepository>();
        collectionRepositoryMock.Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);
        
        _unitOfWorkMock.Setup(x => x.Collections).Returns(collectionRepositoryMock.Object);

        // Act
        var result = await _service.GetByIdAsync(collectionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(collection);
        collectionRepositoryMock.Verify(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        
        var collectionRepositoryMock = new Mock<ICollectionRepository>();
        collectionRepositoryMock.Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);
        
        _unitOfWorkMock.Setup(x => x.Collections).Returns(collectionRepositoryMock.Object);

        // Act
        var result = await _service.GetByIdAsync(collectionId);

        // Assert
        result.Should().BeNull();
        collectionRepositoryMock.Verify(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllCollections()
    {
        // Arrange
        var collections = new List<Collection>
        {
            new Collection("Collection 1", "C:\\path1", CollectionType.Folder),
            new Collection("Collection 2", "C:\\path2", CollectionType.Zip)
        };
        
        var collectionRepositoryMock = new Mock<ICollectionRepository>();
        collectionRepositoryMock.Setup(x => x.GetActiveCollectionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(collections);
        
        _unitOfWorkMock.Setup(x => x.Collections).Returns(collectionRepositoryMock.Object);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(collections);
        collectionRepositoryMock.Verify(x => x.GetActiveCollectionsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateCollection()
    {
        // Arrange
        var name = "Test Collection";
        var path = "L:\\EMedia\\AI_Generated\\AiASAG";
        var type = CollectionType.Folder;
        var settings = new CollectionSettings();
        
        var collectionRepositoryMock = new Mock<ICollectionRepository>();
        collectionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Collection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(It.IsAny<Collection>());
        
        _unitOfWorkMock.Setup(x => x.Collections).Returns(collectionRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        
        _fileScannerMock.Setup(x => x.IsValidCollectionPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateAsync(name, path, type, settings);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(name);
        result.Path.Should().Be(path);
        result.Type.Should().Be(type);
        collectionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Collection>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldUpdateCollection()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        var newName = "Updated Collection";
        var newPath = "C:\\updated\\path";
        
        var existingCollection = new Collection("Original Collection", "C:\\original\\path", CollectionType.Folder);
        
        var collectionRepositoryMock = new Mock<ICollectionRepository>();
        collectionRepositoryMock.Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCollection);
        
        _unitOfWorkMock.Setup(x => x.Collections).Returns(collectionRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.UpdateAsync(collectionId, newName, newPath);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(newName);
        result.Path.Should().Be(newPath);
        collectionRepositoryMock.Verify(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldDeleteCollection()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        var collection = new Collection("Test Collection", "C:\\test\\path", CollectionType.Folder);
        
        var collectionRepositoryMock = new Mock<ICollectionRepository>();
        collectionRepositoryMock.Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);
        
        _unitOfWorkMock.Setup(x => x.Collections).Returns(collectionRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.DeleteAsync(collectionId);

        // Assert
        collection.IsDeleted.Should().BeTrue();
        collectionRepositoryMock.Verify(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ShouldThrowNotFoundException()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        
        var collectionRepositoryMock = new Mock<ICollectionRepository>();
        collectionRepositoryMock.Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);
        
        _unitOfWorkMock.Setup(x => x.Collections).Returns(collectionRepositoryMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(collectionId));
        collectionRepositoryMock.Verify(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
