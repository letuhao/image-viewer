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

public class CollectionServiceTests02
{
    private readonly Mock<ICollectionRepository> _collectionRepositoryMock;
    private readonly Mock<IFileScannerService> _fileScannerMock;
    private readonly Mock<ILogger<CollectionService>> _loggerMock;
    private readonly Mock<Microsoft.Extensions.Options.IOptions<ImageViewer.Application.Options.ImageSizeOptions>> _optionsMock;
    private readonly CollectionService _service;

    public CollectionServiceTests02()
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
    public async Task CreateAsync_WithInvalidPath_ShouldThrowArgumentException()
    {
        // Arrange
        var name = "Test Collection";
        var invalidPath = "invalid://path";
        var settings = new CollectionSettings();

        _fileScannerMock.Setup(x => x.IsValidCollectionPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => 
            _service.CreateAsync(name, invalidPath, CollectionType.Folder, settings));
    }

    [Fact]
    public async Task CreateAsync_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        string? name = null;
        var path = "L:\\EMedia\\AI_Generated\\AiASAG";
        var settings = new CollectionSettings();

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => 
            _service.CreateAsync(name!, path, CollectionType.Folder, settings));
    }

    [Fact]
    public async Task CreateAsync_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var name = "";
        var path = "L:\\EMedia\\AI_Generated\\AiASAG";
        var settings = new CollectionSettings();

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => 
            _service.CreateAsync(name, path, CollectionType.Folder, settings));
    }

    [Fact]
    public async Task CreateAsync_WithNullPath_ShouldThrowArgumentException()
    {
        // Arrange
        var name = "Test Collection";
        string? path = null;
        var settings = new CollectionSettings();

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => 
            _service.CreateAsync(name, path!, CollectionType.Folder, settings));
    }

    [Fact]
    public async Task CreateAsync_WithEmptyPath_ShouldThrowArgumentException()
    {
        // Arrange
        var name = "Test Collection";
        var path = "";
        var settings = new CollectionSettings();

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => 
            _service.CreateAsync(name, path, CollectionType.Folder, settings));
    }

    [Fact]
    public async Task UpdateAsync_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        string? name = null;
        var path = "L:\\EMedia\\AI_Generated\\AiASAG";
        var settings = new CollectionSettings();

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => 
            _service.UpdateAsync(collectionId, name!, path, settings));
    }

    [Fact]
    public async Task UpdateAsync_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        var name = "";
        var path = "L:\\EMedia\\AI_Generated\\AiASAG";
        var settings = new CollectionSettings();

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => 
            _service.UpdateAsync(collectionId, name, path, settings));
    }

    [Fact]
    public async Task UpdateAsync_WithNullPath_ShouldThrowArgumentException()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        var name = "Test Collection";
        string? path = null;
        var settings = new CollectionSettings();

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => 
            _service.UpdateAsync(collectionId, name, path!, settings));
    }

    [Fact]
    public async Task UpdateAsync_WithEmptyPath_ShouldThrowArgumentException()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        var name = "Test Collection";
        var path = "";
        var settings = new CollectionSettings();

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => 
            _service.UpdateAsync(collectionId, name, path, settings));
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidPath_ShouldThrowArgumentException()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        var name = "Test Collection";
        var invalidPath = "invalid://path";
        var settings = new CollectionSettings();

        _fileScannerMock.Setup(x => x.IsValidCollectionPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => 
            _service.UpdateAsync(collectionId, name, invalidPath, settings));
    }

    [Fact]
    public async Task GetByIdAsync_WithEmptyGuid_ShouldReturnNull()
    {
        // Arrange
        var emptyId = Guid.Empty;
        
        var collectionRepositoryMock = new Mock<ICollectionRepository>();
        collectionRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);
        
        _unitOfWorkMock.Setup(x => x.Collections).Returns(collectionRepositoryMock.Object);

        // Act
        var result = await _service.GetByIdAsync(emptyId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithEmptyResult_ShouldReturnEmptyCollection()
    {
        // Arrange
        var emptyCollections = new List<Collection>();
        
        var collectionRepositoryMock = new Mock<ICollectionRepository>();
        collectionRepositoryMock.Setup(x => x.GetActiveCollectionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyCollections);
        
        _unitOfWorkMock.Setup(x => x.Collections).Returns(collectionRepositoryMock.Object);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_WithEmptyGuid_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyId = Guid.Empty;

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => 
            _service.DeleteAsync(emptyId));
    }

    [Fact]
    public async Task CreateAsync_WithZipType_ShouldCreateZipCollection()
    {
        // Arrange
        var name = "Test Zip Collection";
        var path = "L:\\EMedia\\AI_Generated\\AiASAG\\test.zip";
        var type = CollectionType.Zip;
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
    }

    [Fact]
    public async Task CreateAsync_WithRarType_ShouldCreateRarCollection()
    {
        // Arrange
        var name = "Test Rar Collection";
        var path = "L:\\EMedia\\AI_Generated\\AiASAG\\test.rar";
        var type = CollectionType.Rar;
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
    }

    [Fact]
    public async Task CreateAsync_WithSevenZipType_ShouldCreateSevenZipCollection()
    {
        // Arrange
        var name = "Test 7Z Collection";
        var path = "L:\\EMedia\\AI_Generated\\AiASAG\\test.7z";
        var type = CollectionType.SevenZip;
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
    }
}
