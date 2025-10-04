using FluentAssertions;
using ImageViewer.Api.Controllers;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.Tags;
using ImageViewer.Tests.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MongoDB.Bson;

namespace ImageViewer.Tests.Api.Controllers;

public class TagsControllerTests : TestBase
{
    private readonly Mock<ITagService> _tagServiceMock;
    private readonly Mock<ILogger<TagsController>> _loggerMock;
    private readonly TagsController _controller;

    public TagsControllerTests()
    {
        _tagServiceMock = CreateMock<ITagService>();
        _loggerMock = CreateMock<ILogger<TagsController>>();
        _controller = new TagsController(_tagServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllTags_ShouldReturnOkResult()
    {
        // Arrange
        var expectedTags = new List<TagDto>
        {
            new() { Id = ObjectId.GenerateNewId(), Name = "manga", Description = "Manga collection" },
            new() { Id = ObjectId.GenerateNewId(), Name = "anime", Description = "Anime collection" }
        };
        _tagServiceMock
            .Setup(x => x.GetAllTagsAsync())
            .ReturnsAsync(expectedTags);

        // Act
        var result = await _controller.GetAllTags();

        // Assert
        result.Should().BeOfType<ActionResult<IEnumerable<TagDto>>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var tags = okResult!.Value as IEnumerable<TagDto>;
        tags.Should().NotBeNull();
        tags.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllTags_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        _tagServiceMock
            .Setup(x => x.GetAllTagsAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAllTags();

        // Assert
        result.Should().BeOfType<ActionResult<IEnumerable<TagDto>>>();
        var statusResult = result.Result as ObjectResult;
        statusResult.Should().NotBeNull();
        statusResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetCollectionTags_WithValidCollectionId_ShouldReturnOkResult()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var expectedTags = new List<CollectionTagDto>
        {
            new() { Tag = "manga", Count = 5, AddedBy = "user1", AddedAt = DateTime.UtcNow },
            new() { Tag = "anime", Count = 3, AddedBy = "user2", AddedAt = DateTime.UtcNow }
        };
        _tagServiceMock
            .Setup(x => x.GetCollectionTagsAsync(collectionId))
            .ReturnsAsync(expectedTags);

        // Act
        var result = await _controller.GetCollectionTags(collectionId);

        // Assert
        result.Should().BeOfType<ActionResult<IEnumerable<CollectionTagDto>>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var tags = okResult!.Value as IEnumerable<CollectionTagDto>;
        tags.Should().NotBeNull();
        tags.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCollectionTags_WhenCollectionNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        _tagServiceMock
            .Setup(x => x.GetCollectionTagsAsync(collectionId))
            .ThrowsAsync(new KeyNotFoundException("Collection not found"));

        // Act
        var result = await _controller.GetCollectionTags(collectionId);

        // Assert
        result.Should().BeOfType<ActionResult<IEnumerable<CollectionTagDto>>>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCollectionTags_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        _tagServiceMock
            .Setup(x => x.GetCollectionTagsAsync(collectionId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetCollectionTags(collectionId);

        // Assert
        result.Should().BeOfType<ActionResult<IEnumerable<CollectionTagDto>>>();
        var statusResult = result.Result as ObjectResult;
        statusResult.Should().NotBeNull();
        statusResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task AddTagToCollection_WithValidRequest_ShouldReturnCreatedResult()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var request = new AddTagToCollectionDto
        {
            TagName = "new-tag",
            Description = "A new tag",
            Color = new TagColorDto { R = 255, G = 0, B = 0 }
        };

        var createdTag = new CollectionTagDto
        {
            Tag = request.TagName,
            Count = 1,
            AddedBy = "user1",
            AddedAt = DateTime.UtcNow
        };

        _tagServiceMock
            .Setup(x => x.AddTagToCollectionAsync(collectionId, request))
            .ReturnsAsync(createdTag);

        // Act
        var result = await _controller.AddTagToCollection(collectionId, request);

        // Assert
        result.Should().BeOfType<ActionResult<CollectionTagDto>>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.Value.Should().BeEquivalentTo(createdTag);
    }

    [Fact]
    public async Task AddTagToCollection_WhenCollectionNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var request = new AddTagToCollectionDto { TagName = "new-tag" };

        _tagServiceMock
            .Setup(x => x.AddTagToCollectionAsync(collectionId, request))
            .ThrowsAsync(new KeyNotFoundException("Collection not found"));

        // Act
        var result = await _controller.AddTagToCollection(collectionId, request);

        // Assert
        result.Should().BeOfType<ActionResult<CollectionTagDto>>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
    }

    [Fact]
    public async Task AddTagToCollection_WhenInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var request = new AddTagToCollectionDto { TagName = "" };

        _tagServiceMock
            .Setup(x => x.AddTagToCollectionAsync(collectionId, request))
            .ThrowsAsync(new ArgumentException("Invalid tag data"));

        // Act
        var result = await _controller.AddTagToCollection(collectionId, request);

        // Assert
        result.Should().BeOfType<ActionResult<CollectionTagDto>>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateTag_WithValidRequest_ShouldReturnOkResult()
    {
        // Arrange
        var tagId = ObjectId.GenerateNewId();
        var request = new UpdateTagDto
        {
            Name = "updated-tag",
            Description = "Updated description",
            Color = new TagColorDto { R = 0, G = 255, B = 0 }
        };

        var updatedTag = new TagDto
        {
            Id = tagId,
            Name = request.Name,
            Description = request.Description,
            Color = request.Color
        };

        _tagServiceMock
            .Setup(x => x.UpdateTagAsync(tagId, request))
            .ReturnsAsync(updatedTag);

        // Act
        var result = await _controller.UpdateTag(tagId, request);

        // Assert
        result.Should().BeOfType<ActionResult<TagDto>>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(updatedTag);
    }

    [Fact]
    public async Task UpdateTag_WhenTagNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var tagId = ObjectId.GenerateNewId();
        var request = new UpdateTagDto { Name = "updated-tag" };

        _tagServiceMock
            .Setup(x => x.UpdateTagAsync(tagId, request))
            .ThrowsAsync(new KeyNotFoundException("Tag not found"));

        // Act
        var result = await _controller.UpdateTag(tagId, request);

        // Assert
        result.Should().BeOfType<ActionResult<TagDto>>();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateTag_WhenInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        var tagId = ObjectId.GenerateNewId();
        var request = new UpdateTagDto { Name = "" };

        _tagServiceMock
            .Setup(x => x.UpdateTagAsync(tagId, request))
            .ThrowsAsync(new ArgumentException("Invalid tag data"));

        // Act
        var result = await _controller.UpdateTag(tagId, request);

        // Assert
        result.Should().BeOfType<ActionResult<TagDto>>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
    }

    [Fact]
    public async Task RemoveTagFromCollection_WithValidParameters_ShouldReturnNoContent()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var tagName = "test-tag";

        _tagServiceMock
            .Setup(x => x.RemoveTagFromCollectionAsync(collectionId, tagName))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RemoveTagFromCollection(collectionId, tagName);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemoveTagFromCollection_WhenTagOrCollectionNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var tagName = "test-tag";

        _tagServiceMock
            .Setup(x => x.RemoveTagFromCollectionAsync(collectionId, tagName))
            .ThrowsAsync(new KeyNotFoundException("Tag or collection not found"));

        // Act
        var result = await _controller.RemoveTagFromCollection(collectionId, tagName);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteTag_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var tagId = ObjectId.GenerateNewId();
        _tagServiceMock
            .Setup(x => x.DeleteTagAsync(tagId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteTag(tagId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteTag_WhenTagNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var tagId = ObjectId.GenerateNewId();
        _tagServiceMock
            .Setup(x => x.DeleteTagAsync(tagId))
            .ThrowsAsync(new KeyNotFoundException("Tag not found"));

        // Act
        var result = await _controller.DeleteTag(tagId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteTag_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var tagId = ObjectId.GenerateNewId();
        _tagServiceMock
            .Setup(x => x.DeleteTagAsync(tagId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.DeleteTag(tagId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusResult = result as ObjectResult;
        statusResult!.StatusCode.Should().Be(500);
    }
}
