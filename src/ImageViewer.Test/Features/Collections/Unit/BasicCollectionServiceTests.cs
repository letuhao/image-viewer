using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.BackgroundJobs;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Exceptions;
using ImageViewer.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace ImageViewer.Test.Features.Collections.Unit;

/// <summary>
/// Unit tests for CollectionService - Collection Management features
/// </summary>
public class BasicCollectionServiceTests
{
    private readonly Mock<ICollectionRepository> _mockCollectionRepository;
    private readonly Mock<IMessageQueueService> _mockMessageQueueService;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly Mock<ILogger<CollectionService>> _mockLogger;
    private readonly CollectionService _collectionService;

        public BasicCollectionServiceTests()
        {
            _mockCollectionRepository = new Mock<ICollectionRepository>();
            _mockMessageQueueService = new Mock<IMessageQueueService>();
            _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
            _mockLogger = new Mock<ILogger<CollectionService>>();

            // Setup IServiceScopeFactory to return IBackgroundJobService
            var mockBackgroundJobService = new Mock<IBackgroundJobService>();
            mockBackgroundJobService.Setup(s => s.CreateJobAsync(It.IsAny<CreateBackgroundJobDto>()))
                .ReturnsAsync(new BackgroundJobDto { JobId = ObjectId.GenerateNewId() });
            
            var mockScope = new Mock<IServiceScope>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(sp => sp.GetService(typeof(IBackgroundJobService)))
                .Returns(mockBackgroundJobService.Object);
            mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
            _mockServiceScopeFactory.Setup(sf => sf.CreateScope()).Returns(mockScope.Object);

            var mockCollectionArchiveRepository = new Mock<ICollectionArchiveRepository>();
            _collectionService = new CollectionService(
                _mockCollectionRepository.Object,
                mockCollectionArchiveRepository.Object,
                _mockMessageQueueService.Object,
                _mockServiceScopeFactory.Object,
                _mockLogger.Object);
        }

    [Fact]
    public async Task CreateCollectionAsync_WithValidData_ShouldReturnCreatedCollection()
    {
        // Arrange
        var libraryId = ObjectId.GenerateNewId();
        var collectionId = ObjectId.GenerateNewId();
        var name = "Test Collection";
        var path = "/test/path";
        var type = CollectionType.Folder;

        var expectedCollection = new Collection(libraryId, name, path, type)
        {
            Id = collectionId
        };

        _mockCollectionRepository.Setup(x => x.GetByPathAsync(path))
            .ReturnsAsync((Collection?)null);
        _mockCollectionRepository.Setup(x => x.CreateAsync(It.IsAny<Collection>()))
            .ReturnsAsync(expectedCollection);

        // Act
        var result = await _collectionService.CreateCollectionAsync(libraryId, name, path, type);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(collectionId);
        result.Name.Should().Be(name);
        result.Path.Should().Be(path);
        result.Type.Should().Be(type);
        result.LibraryId.Should().Be(libraryId);
    }

    [Fact]
    public async Task CreateCollectionAsync_WithEmptyName_ShouldThrowValidationException()
    {
        // Arrange
        var libraryId = ObjectId.GenerateNewId();
        var name = "";
        var path = "/test/path";
        var type = CollectionType.Folder;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _collectionService.CreateCollectionAsync(libraryId, name, path, type));
        
        exception.Message.Should().Be("Collection name cannot be null or empty");
    }

    [Fact]
    public async Task CreateCollectionAsync_WithEmptyPath_ShouldThrowValidationException()
    {
        // Arrange
        var libraryId = ObjectId.GenerateNewId();
        var name = "Test Collection";
        var path = "";
        var type = CollectionType.Folder;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _collectionService.CreateCollectionAsync(libraryId, name, path, type));
        
        exception.Message.Should().Be("Collection path cannot be null or empty");
    }

    [Fact]
    public async Task CreateCollectionAsync_WithExistingPath_ShouldThrowDuplicateEntityException()
    {
        // Arrange
        var libraryId = ObjectId.GenerateNewId();
        var name = "Test Collection";
        var path = "/existing/path";
        var type = CollectionType.Folder;

        var existingCollection = new Collection(libraryId, "Existing", path, type);

        _mockCollectionRepository.Setup(x => x.GetByPathAsync(path))
            .ReturnsAsync(existingCollection);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DuplicateEntityException>(
            () => _collectionService.CreateCollectionAsync(libraryId, name, path, type));
        
        exception.Message.Should().Be("Collection at path '/existing/path' already exists");
    }

    [Fact]
    public async Task GetCollectionByIdAsync_WithValidId_ShouldReturnCollection()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var libraryId = ObjectId.GenerateNewId();
        var expectedCollection = new Collection(libraryId, "Test Collection", "/test/path", CollectionType.Folder)
        {
            Id = collectionId
        };

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync(expectedCollection);

        // Act
        var result = await _collectionService.GetCollectionByIdAsync(collectionId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(collectionId);
        result.Name.Should().Be("Test Collection");
    }

    [Fact]
    public async Task GetCollectionByIdAsync_WithNonExistentId_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync((Collection?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _collectionService.GetCollectionByIdAsync(collectionId));
        
        exception.Message.Should().Be($"Collection with ID '{collectionId}' not found");
    }

    [Fact]
    public async Task GetCollectionByPathAsync_WithValidPath_ShouldReturnCollection()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var libraryId = ObjectId.GenerateNewId();
        var path = "/test/path";
        var expectedCollection = new Collection(libraryId, "Test Collection", path, CollectionType.Folder)
        {
            Id = collectionId
        };

        _mockCollectionRepository.Setup(x => x.GetByPathAsync(path))
            .ReturnsAsync(expectedCollection);

        // Act
        var result = await _collectionService.GetCollectionByPathAsync(path);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(collectionId);
        result.Path.Should().Be(path);
    }

    [Fact]
    public async Task GetCollectionByPathAsync_WithEmptyPath_ShouldThrowValidationException()
    {
        // Arrange
        var path = "";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _collectionService.GetCollectionByPathAsync(path));
        
        exception.Message.Should().Be("Collection path cannot be null or empty");
    }

    [Fact]
    public async Task GetCollectionByPathAsync_WithNonExistentPath_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var path = "/nonexistent/path";

        _mockCollectionRepository.Setup(x => x.GetByPathAsync(path))
            .ReturnsAsync((Collection?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _collectionService.GetCollectionByPathAsync(path));
        
        exception.Message.Should().Be($"Collection at path '{path}' not found");
    }

    [Fact]
    public async Task GetCollectionsByLibraryIdAsync_WithValidLibraryId_ShouldReturnCollections()
    {
        // Arrange
        var libraryId = ObjectId.GenerateNewId();
        var collections = new List<Collection>
        {
            new Collection(libraryId, "Collection 1", "/path1", CollectionType.Folder),
            new Collection(libraryId, "Collection 2", "/path2", CollectionType.Zip)
        };

        _mockCollectionRepository.Setup(x => x.GetByLibraryIdAsync(libraryId))
            .ReturnsAsync(collections);

        // Act
        var result = await _collectionService.GetCollectionsByLibraryIdAsync(libraryId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(c => c.LibraryId.Should().Be(libraryId));
    }

    [Fact]
    public async Task UpdateCollectionAsync_WithValidData_ShouldUpdateCollection()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var libraryId = ObjectId.GenerateNewId();
        var existingCollection = new Collection(libraryId, "Old Name", "/old/path", CollectionType.Folder)
        {
            Id = collectionId
        };

        var updatedCollection = new Collection(libraryId, "New Name", "/new/path", CollectionType.Zip)
        {
            Id = collectionId
        };

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync(existingCollection);
        _mockCollectionRepository.Setup(x => x.UpdateAsync(It.IsAny<Collection>()))
            .ReturnsAsync(updatedCollection);

        // Act
        var updateRequest = new UpdateCollectionRequest
        {
            Name = "New Name",
            Path = "/new/path",
            Type = CollectionType.Zip
        };
        var result = await _collectionService.UpdateCollectionAsync(collectionId, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Name");
        result.Path.Should().Be("/new/path");
        result.Type.Should().Be(CollectionType.Zip);
    }

    [Fact]
    public async Task DeleteCollectionAsync_WithValidId_ShouldDeleteCollection()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var libraryId = ObjectId.GenerateNewId();
        var collection = new Collection(libraryId, "Test Collection", "/test/path", CollectionType.Folder)
        {
            Id = collectionId
        };

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync(collection);
        _mockCollectionRepository.Setup(x => x.DeleteAsync(collectionId))
            .Returns(Task.CompletedTask);

        // Act
        await _collectionService.DeleteCollectionAsync(collectionId);

        // Assert
        _mockCollectionRepository.Verify(x => x.DeleteAsync(collectionId), Times.Once);
    }

    [Fact]
    public async Task DeleteCollectionAsync_WithNonExistentId_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync((Collection?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _collectionService.DeleteCollectionAsync(collectionId));
        
        exception.Message.Should().Be($"Collection with ID '{collectionId}' not found");
    }
}