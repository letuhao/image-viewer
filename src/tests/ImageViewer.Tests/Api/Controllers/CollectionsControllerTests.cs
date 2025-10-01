using FluentAssertions;
using ImageViewer.Api.Controllers;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.Common;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Tests.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ImageViewer.Tests.Api.Controllers;

public class CollectionsControllerTests : TestBase
{
    private readonly Mock<ICollectionService> _collectionServiceMock;
    private readonly Mock<ILogger<CollectionsController>> _loggerMock;
    private readonly CollectionsController _controller;

    public CollectionsControllerTests()
    {
        _collectionServiceMock = CreateMock<ICollectionService>();
        _loggerMock = CreateMock<ILogger<CollectionsController>>();
        _controller = new CollectionsController(_collectionServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetCollections_ShouldReturnOkResult()
    {
        // Arrange
        var collections = new List<Collection>
        {
            new Collection("Test Collection 1", "C:\\Test1", CollectionType.Folder),
            new Collection("Test Collection 2", "C:\\Test2", CollectionType.Folder)
        };
        _collectionServiceMock
            .Setup(x => x.GetCollectionsAsync(It.IsAny<PaginationRequestDto>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginationResponseDto<Collection>
            {
                Data = collections,
                TotalCount = collections.Count,
                Page = 1,
                PageSize = 10,
                TotalPages = 1,
                HasNextPage = false,
                HasPreviousPage = false
            });

        // Act
        var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
        var result = await _controller.GetCollections(pagination);

        // Assert
        result.Should().BeOfType<ActionResult<PaginationResponseDto<Collection>>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var paginatedResult = okResult!.Value as PaginationResponseDto<Collection>;
        paginatedResult.Should().NotBeNull();
        paginatedResult!.Data.Should().BeEquivalentTo(collections);
    }

    [Fact]
    public async Task GetCollections_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        _collectionServiceMock
            .Setup(x => x.GetCollectionsAsync(It.IsAny<PaginationRequestDto>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
        var result = await _controller.GetCollections(pagination);

        // Assert
        result.Should().BeOfType<ActionResult<PaginationResponseDto<Collection>>>();
        var statusResult = result.Result as ObjectResult;
        statusResult.Should().NotBeNull();
        statusResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetCollection_WithValidId_ShouldReturnOkResult()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        var collection = new Collection("Test Collection", "C:\\Test", CollectionType.Folder);
        _collectionServiceMock
            .Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);

        // Act
        var result = await _controller.GetCollection(collectionId);

        // Assert
        result.Should().BeOfType<ActionResult<Collection>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(collection);
    }

    [Fact]
    public async Task GetCollection_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        _collectionServiceMock
            .Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Collection?)null);

        // Act
        var result = await _controller.GetCollection(collectionId);

        // Assert
        result.Should().BeOfType<ActionResult<Collection>>();
        var notFoundResult = result.Result as NotFoundResult;
        notFoundResult.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCollection_WithValidRequest_ShouldReturnCreatedResult()
    {
        // Arrange
        var request = new CreateCollectionRequest
        {
            Name = "Test Collection",
            Path = "C:\\Test\\Path",
            Type = CollectionType.Folder,
            ThumbnailWidth = 200,
            ThumbnailHeight = 200,
            CacheWidth = 1280,
            CacheHeight = 720,
            Quality = 85,
            EnableCache = true,
            AutoScan = true
        };

        var createdCollection = new Collection("New Collection", "C:\\New", CollectionType.Folder);
        _collectionServiceMock
            .Setup(x => x.CreateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CollectionType>(),
                It.IsAny<CollectionSettings>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdCollection);

        // Act
        var result = await _controller.CreateCollection(request);

        // Assert
        result.Should().BeOfType<ActionResult<Collection>>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.Value.Should().BeEquivalentTo(createdCollection);
    }

    [Fact]
    public async Task CreateCollection_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var request = new CreateCollectionRequest
        {
            Name = "Test Collection",
            Path = "C:\\Test\\Path",
            Type = CollectionType.Folder
        };

        _collectionServiceMock
            .Setup(x => x.CreateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CollectionType>(),
                It.IsAny<CollectionSettings>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateCollection(request);

        // Assert
        result.Should().BeOfType<ActionResult<Collection>>();
        var statusResult = result.Result as ObjectResult;
        statusResult.Should().NotBeNull();
        statusResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task UpdateCollection_WithValidRequest_ShouldReturnNoContent()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        var request = new UpdateCollectionRequest
        {
            Name = "Updated Collection",
            Settings = new CollectionSettingsRequest
            {
                ThumbnailWidth = 300,
                ThumbnailHeight = 300
            }
        };

        var updatedCollection = new Collection("Updated Collection", "C:\\Updated", CollectionType.Folder);
        _collectionServiceMock
            .Setup(x => x.UpdateAsync(
                collectionId,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CollectionSettings?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedCollection);

        // Act
        var result = await _controller.UpdateCollection(collectionId, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateCollection_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        var request = new UpdateCollectionRequest
        {
            Name = "Updated Collection"
        };

        _collectionServiceMock
            .Setup(x => x.UpdateAsync(
                collectionId,
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CollectionSettings?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.UpdateCollection(collectionId, request);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusResult = result as ObjectResult;
        statusResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task DeleteCollection_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        _collectionServiceMock
            .Setup(x => x.DeleteAsync(collectionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteCollection(collectionId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteCollection_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        _collectionServiceMock
            .Setup(x => x.DeleteAsync(collectionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.DeleteCollection(collectionId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusResult = result as ObjectResult;
        statusResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task ScanCollection_WithValidId_ShouldReturnOkResult()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        _collectionServiceMock
            .Setup(x => x.ScanCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ScanCollection(collectionId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task ScanCollection_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        _collectionServiceMock
            .Setup(x => x.ScanCollectionAsync(collectionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ScanCollection(collectionId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusResult = result as ObjectResult;
        statusResult!.StatusCode.Should().Be(500);
    }
}
