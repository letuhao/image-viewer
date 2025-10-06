using FluentAssertions;
using Moq;
using Xunit;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImageViewer.Test.Features.MediaManagement.Unit;

/// <summary>
/// Unit tests for MediaItemService - Media Management features
/// </summary>
public class MediaItemServiceTests
{
    private readonly Mock<IMediaItemRepository> _mockMediaItemRepository;
    private readonly Mock<ILogger<MediaItemService>> _mockLogger;
    private readonly MediaItemService _mediaItemService;

    public MediaItemServiceTests()
    {
        _mockMediaItemRepository = new Mock<IMediaItemRepository>();
        _mockLogger = new Mock<ILogger<MediaItemService>>();

        _mediaItemService = new MediaItemService(
            _mockMediaItemRepository.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CreateMediaItemAsync_WithValidData_ShouldReturnCreatedMediaItem()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var name = "Test Image";
        var filename = "test.jpg";
        var path = "/path/to/test.jpg";
        var type = "image";
        var format = "jpeg";
        var fileSize = 1024L;
        var width = 1920;
        var height = 1080;

        var expectedMediaItem = new MediaItem(collectionId, name, filename, path, type, format, fileSize, width, height);
        _mockMediaItemRepository.Setup(r => r.CreateAsync(It.IsAny<MediaItem>())).ReturnsAsync(expectedMediaItem);

        // Act
        var result = await _mediaItemService.CreateMediaItemAsync(collectionId, name, filename, path, type, format, fileSize, width, height);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(name);
        result.Filename.Should().Be(filename);
        result.Path.Should().Be(path);
        result.Type.Should().Be(type);
        result.Format.Should().Be(format);
        result.FileSize.Should().Be(fileSize);
        result.Width.Should().Be(width);
        result.Height.Should().Be(height);
        _mockMediaItemRepository.Verify(r => r.CreateAsync(It.IsAny<MediaItem>()), Times.Once);
    }

    [Fact]
    public async Task CreateMediaItemAsync_WithEmptyName_ShouldThrowValidationException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var name = "";
        var filename = "test.jpg";
        var path = "/path/to/test.jpg";
        var type = "image";
        var format = "jpeg";
        var fileSize = 1024L;
        var width = 1920;
        var height = 1080;

        // Act
        Func<Task> act = async () => await _mediaItemService.CreateMediaItemAsync(collectionId, name, filename, path, type, format, fileSize, width, height);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Media item name cannot be null or empty");
    }

    [Fact]
    public async Task CreateMediaItemAsync_WithEmptyFilename_ShouldThrowValidationException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var name = "Test Image";
        var filename = "";
        var path = "/path/to/test.jpg";
        var type = "image";
        var format = "jpeg";
        var fileSize = 1024L;
        var width = 1920;
        var height = 1080;

        // Act
        Func<Task> act = async () => await _mediaItemService.CreateMediaItemAsync(collectionId, name, filename, path, type, format, fileSize, width, height);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Media item filename cannot be null or empty");
    }

    [Fact]
    public async Task CreateMediaItemAsync_WithEmptyPath_ShouldThrowValidationException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var name = "Test Image";
        var filename = "test.jpg";
        var path = "";
        var type = "image";
        var format = "jpeg";
        var fileSize = 1024L;
        var width = 1920;
        var height = 1080;

        // Act
        Func<Task> act = async () => await _mediaItemService.CreateMediaItemAsync(collectionId, name, filename, path, type, format, fileSize, width, height);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Media item path cannot be null or empty");
    }

    [Fact]
    public async Task CreateMediaItemAsync_WithEmptyType_ShouldThrowValidationException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var name = "Test Image";
        var filename = "test.jpg";
        var path = "/path/to/test.jpg";
        var type = "";
        var format = "jpeg";
        var fileSize = 1024L;
        var width = 1920;
        var height = 1080;

        // Act
        Func<Task> act = async () => await _mediaItemService.CreateMediaItemAsync(collectionId, name, filename, path, type, format, fileSize, width, height);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Media item type cannot be null or empty");
    }

    [Fact]
    public async Task CreateMediaItemAsync_WithEmptyFormat_ShouldThrowValidationException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var name = "Test Image";
        var filename = "test.jpg";
        var path = "/path/to/test.jpg";
        var type = "image";
        var format = "";
        var fileSize = 1024L;
        var width = 1920;
        var height = 1080;

        // Act
        Func<Task> act = async () => await _mediaItemService.CreateMediaItemAsync(collectionId, name, filename, path, type, format, fileSize, width, height);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Media item format cannot be null or empty");
    }

    [Fact]
    public async Task CreateMediaItemAsync_WithZeroFileSize_ShouldThrowValidationException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var name = "Test Image";
        var filename = "test.jpg";
        var path = "/path/to/test.jpg";
        var type = "image";
        var format = "jpeg";
        var fileSize = 0L;
        var width = 1920;
        var height = 1080;

        // Act
        Func<Task> act = async () => await _mediaItemService.CreateMediaItemAsync(collectionId, name, filename, path, type, format, fileSize, width, height);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("File size must be greater than 0");
    }

    [Fact]
    public async Task CreateMediaItemAsync_WithZeroWidth_ShouldThrowValidationException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var name = "Test Image";
        var filename = "test.jpg";
        var path = "/path/to/test.jpg";
        var type = "image";
        var format = "jpeg";
        var fileSize = 1024L;
        var width = 0;
        var height = 1080;

        // Act
        Func<Task> act = async () => await _mediaItemService.CreateMediaItemAsync(collectionId, name, filename, path, type, format, fileSize, width, height);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Width must be greater than 0");
    }

    [Fact]
    public async Task CreateMediaItemAsync_WithZeroHeight_ShouldThrowValidationException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var name = "Test Image";
        var filename = "test.jpg";
        var path = "/path/to/test.jpg";
        var type = "image";
        var format = "jpeg";
        var fileSize = 1024L;
        var width = 1920;
        var height = 0;

        // Act
        Func<Task> act = async () => await _mediaItemService.CreateMediaItemAsync(collectionId, name, filename, path, type, format, fileSize, width, height);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Height must be greater than 0");
    }

    [Fact]
    public async Task GetMediaItemByIdAsync_WithValidId_ShouldReturnMediaItem()
    {
        // Arrange
        var mediaItemId = ObjectId.GenerateNewId();
        var collectionId = ObjectId.GenerateNewId();
        var expectedMediaItem = new MediaItem(collectionId, "Test Image", "test.jpg", "/path/to/test.jpg", "image", "jpeg", 1024L, 1920, 1080)
        {
            Id = mediaItemId
        };

        _mockMediaItemRepository.Setup(x => x.GetByIdAsync(mediaItemId))
            .ReturnsAsync(expectedMediaItem);

        // Act
        var result = await _mediaItemService.GetMediaItemByIdAsync(mediaItemId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(mediaItemId);
        result.Name.Should().Be(expectedMediaItem.Name);
    }

    [Fact]
    public async Task GetMediaItemByIdAsync_WithNonExistentId_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var mediaItemId = ObjectId.GenerateNewId();
        _mockMediaItemRepository.Setup(x => x.GetByIdAsync(mediaItemId))
            .ReturnsAsync((MediaItem?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _mediaItemService.GetMediaItemByIdAsync(mediaItemId));

        exception.Message.Should().Be($"Media item with ID '{mediaItemId}' not found");
    }

    [Fact]
    public async Task GetMediaItemByPathAsync_WithValidPath_ShouldReturnMediaItem()
    {
        // Arrange
        var mediaItemId = ObjectId.GenerateNewId();
        var collectionId = ObjectId.GenerateNewId();
        var path = "/path/to/test.jpg";
        var expectedMediaItem = new MediaItem(collectionId, "Test Image", "test.jpg", path, "image", "jpeg", 1024L, 1920, 1080)
        {
            Id = mediaItemId
        };

        _mockMediaItemRepository.Setup(x => x.GetByPathAsync(path))
            .ReturnsAsync(expectedMediaItem);

        // Act
        var result = await _mediaItemService.GetMediaItemByPathAsync(path);

        // Assert
        result.Should().NotBeNull();
        result.Path.Should().Be(path);
    }

    [Fact]
    public async Task GetMediaItemByPathAsync_WithEmptyPath_ShouldThrowValidationException()
    {
        // Arrange
        var path = "";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _mediaItemService.GetMediaItemByPathAsync(path));
        
        exception.Message.Should().Be("Media item path cannot be null or empty");
    }

    [Fact]
    public async Task GetMediaItemByPathAsync_WithNonExistentPath_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var path = "/nonexistent/path.jpg";

        _mockMediaItemRepository.Setup(x => x.GetByPathAsync(path))
            .ReturnsAsync((MediaItem?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _mediaItemService.GetMediaItemByPathAsync(path));
        
        exception.Message.Should().Be($"Media item at path '{path}' not found");
    }

    [Fact]
    public async Task GetMediaItemsByCollectionIdAsync_WithValidCollectionId_ShouldReturnMediaItems()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var mediaItems = new List<MediaItem>
        {
            new MediaItem(collectionId, "Image 1", "image1.jpg", "/path1", "image", "jpeg", 1024L, 1920, 1080),
            new MediaItem(collectionId, "Image 2", "image2.jpg", "/path2", "image", "jpeg", 2048L, 1920, 1080)
        };

        _mockMediaItemRepository.Setup(x => x.GetByCollectionIdAsync(collectionId))
            .ReturnsAsync(mediaItems);

        // Act
        var result = await _mediaItemService.GetMediaItemsByCollectionIdAsync(collectionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(m => m.Name == "Image 1");
        result.Should().Contain(m => m.Name == "Image 2");
    }

    [Fact]
    public async Task UpdateMediaItemAsync_WithValidData_ShouldUpdateMediaItem()
    {
        // Arrange
        var mediaItemId = ObjectId.GenerateNewId();
        var collectionId = ObjectId.GenerateNewId();
        var existingMediaItem = new MediaItem(collectionId, "Old Name", "old.jpg", "/old/path", "image", "jpeg", 1024L, 1920, 1080)
        {
            Id = mediaItemId
        };

        var updatedMediaItem = new MediaItem(collectionId, "New Name", "new.jpg", "/new/path", "image", "jpeg", 2048L, 1920, 1080)
        {
            Id = mediaItemId
        };

        _mockMediaItemRepository.Setup(x => x.GetByIdAsync(mediaItemId))
            .ReturnsAsync(existingMediaItem);
        _mockMediaItemRepository.Setup(x => x.UpdateAsync(It.IsAny<MediaItem>()))
            .ReturnsAsync(updatedMediaItem);

        // Act
        var updateRequest = new UpdateMediaItemRequest
        {
            Name = "New Name",
            Filename = "new.jpg",
            Path = "/new/path",
            Width = 1920,
            Height = 1080
        };
        var result = await _mediaItemService.UpdateMediaItemAsync(mediaItemId, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Name");
        result.Filename.Should().Be("new.jpg");
        result.Path.Should().Be("/new/path");
        _mockMediaItemRepository.Verify(x => x.UpdateAsync(It.IsAny<MediaItem>()), Times.Once);
    }

    [Fact]
    public async Task DeleteMediaItemAsync_WithValidId_ShouldDeleteMediaItem()
    {
        // Arrange
        var mediaItemId = ObjectId.GenerateNewId();
        var collectionId = ObjectId.GenerateNewId();
        var mediaItem = new MediaItem(collectionId, "Test Image", "test.jpg", "/test/path", "image", "jpeg", 1024L, 1920, 1080)
        {
            Id = mediaItemId
        };

        _mockMediaItemRepository.Setup(x => x.GetByIdAsync(mediaItemId))
            .ReturnsAsync(mediaItem);
        _mockMediaItemRepository.Setup(x => x.DeleteAsync(mediaItemId))
            .Returns(Task.CompletedTask);

        // Act
        await _mediaItemService.DeleteMediaItemAsync(mediaItemId);

        // Assert
        _mockMediaItemRepository.Verify(x => x.DeleteAsync(mediaItemId), Times.Once);
    }

    [Fact]
    public async Task DeleteMediaItemAsync_WithNonExistentId_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var mediaItemId = ObjectId.GenerateNewId();
        _mockMediaItemRepository.Setup(x => x.GetByIdAsync(mediaItemId))
            .ReturnsAsync((MediaItem?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _mediaItemService.DeleteMediaItemAsync(mediaItemId));

        exception.Message.Should().Be($"Media item with ID '{mediaItemId}' not found");
    }
}