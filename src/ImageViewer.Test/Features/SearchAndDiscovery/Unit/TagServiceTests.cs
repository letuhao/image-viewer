using FluentAssertions;
using Moq;
using Xunit;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.Tags;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImageViewer.Test.Features.SearchAndDiscovery.Unit;

/// <summary>
/// Unit tests for TagService - Tag Management and Discovery features
/// </summary>
public class TagServiceTests
{
    private readonly Mock<ITagRepository> _mockTagRepository;
    private readonly Mock<ICollectionRepository> _mockCollectionRepository;
    private readonly Mock<ICollectionTagRepository> _mockCollectionTagRepository;
    private readonly Mock<IUserContextService> _mockUserContextService;
    private readonly Mock<ILogger<TagService>> _mockLogger;
    private readonly TagService _tagService;

    public TagServiceTests()
    {
        _mockTagRepository = new Mock<ITagRepository>();
        _mockCollectionRepository = new Mock<ICollectionRepository>();
        _mockCollectionTagRepository = new Mock<ICollectionTagRepository>();
        _mockUserContextService = new Mock<IUserContextService>();
        _mockLogger = new Mock<ILogger<TagService>>();

        _tagService = new TagService(
            _mockTagRepository.Object,
            _mockCollectionRepository.Object,
            _mockCollectionTagRepository.Object,
            _mockUserContextService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetCollectionTagsAsync_WithValidCollectionId_ShouldReturnTags()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var collection = new Collection(ObjectId.GenerateNewId(), "Test Collection", "/path", Domain.Enums.CollectionType.Folder);
        var tag = new Tag("test-tag", "Test description", Domain.ValueObjects.TagColor.Default);
        var collectionTag = new CollectionTag(collectionId, tag.Id);
        // Set the Tag navigation property using reflection since it's private set
        typeof(CollectionTag).GetProperty("Tag")!.SetValue(collectionTag, tag);

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync(collection);
        _mockCollectionTagRepository.Setup(x => x.GetByCollectionIdAsync(collectionId))
            .ReturnsAsync(new List<CollectionTag> { collectionTag });
        _mockUserContextService.Setup(x => x.GetCurrentUserName())
            .Returns("testuser");

        // Act
        var result = await _tagService.GetCollectionTagsAsync(collectionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCollectionTagsAsync_WithNonExistentCollectionId_ShouldThrowArgumentException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync((Collection)null!);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _tagService.GetCollectionTagsAsync(collectionId));
    }

    [Fact]
    public async Task AddTagToCollectionAsync_WithValidData_ShouldAddTag()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var collection = new Collection(ObjectId.GenerateNewId(), "Test Collection", "/path", Domain.Enums.CollectionType.Folder);
        var dto = new AddTagToCollectionDto
        {
            TagName = "new-tag",
            Description = "New tag description",
            Color = new TagColorDto { R = 255, G = 0, B = 0 }
        };

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync(collection);
        _mockTagRepository.Setup(x => x.GetByNameAsync(dto.TagName))
            .ReturnsAsync((Tag)null!);
        _mockTagRepository.Setup(x => x.CreateAsync(It.IsAny<Tag>()))
            .ReturnsAsync((Tag tag) => tag);
        _mockCollectionTagRepository.Setup(x => x.CreateAsync(It.IsAny<CollectionTag>()))
            .ReturnsAsync((CollectionTag ct) => ct);
        _mockUserContextService.Setup(x => x.GetCurrentUserName())
            .Returns("testuser");

        // Act
        var result = await _tagService.AddTagToCollectionAsync(collectionId, dto);

        // Assert
        result.Should().NotBeNull();
        result.Tag.Should().Be(dto.TagName);
        result.Count.Should().Be(1);
        result.AddedBy.Should().Be("testuser");
    }

    [Fact]
    public async Task AddTagToCollectionAsync_WithExistingTag_ShouldReuseExistingTag()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var collection = new Collection(ObjectId.GenerateNewId(), "Test Collection", "/path", Domain.Enums.CollectionType.Folder);
        var existingTag = new Tag("existing-tag", "Existing description", Domain.ValueObjects.TagColor.Default);
        var dto = new AddTagToCollectionDto
        {
            TagName = "existing-tag",
            Description = "Updated description",
            Color = new TagColorDto { R = 0, G = 255, B = 0 }
        };

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync(collection);
        _mockTagRepository.Setup(x => x.GetByNameAsync(dto.TagName))
            .ReturnsAsync(existingTag);
        _mockCollectionTagRepository.Setup(x => x.CreateAsync(It.IsAny<CollectionTag>()))
            .ReturnsAsync((CollectionTag ct) => ct);
        _mockUserContextService.Setup(x => x.GetCurrentUserName())
            .Returns("testuser");

        // Act
        var result = await _tagService.AddTagToCollectionAsync(collectionId, dto);

        // Assert
        result.Should().NotBeNull();
        result.Tag.Should().Be(dto.TagName);
        _mockTagRepository.Verify(x => x.CreateAsync(It.IsAny<Tag>()), Times.Never);
    }

    [Fact]
    public async Task AddTagToCollectionAsync_WithNonExistentCollection_ShouldThrowArgumentException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var dto = new AddTagToCollectionDto
        {
            TagName = "test-tag",
            Description = "Test description",
            Color = new TagColorDto { R = 255, G = 0, B = 0 }
        };

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync((Collection)null!);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _tagService.AddTagToCollectionAsync(collectionId, dto));
    }

    [Fact]
    public async Task RemoveTagFromCollectionAsync_WithValidData_ShouldRemoveTag()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var tag = new Tag("test-tag", "Test description", Domain.ValueObjects.TagColor.Default);
        var collectionTag = new CollectionTag(collectionId, tag.Id);

        _mockTagRepository.Setup(x => x.GetByNameAsync("test-tag"))
            .ReturnsAsync(tag);
        _mockCollectionTagRepository.Setup(x => x.GetByCollectionIdAndTagIdAsync(collectionId, tag.Id))
            .ReturnsAsync(collectionTag);
        _mockCollectionTagRepository.Setup(x => x.DeleteAsync(collectionTag.Id))
            .Returns(Task.CompletedTask);

        // Act
        await _tagService.RemoveTagFromCollectionAsync(collectionId, "test-tag");

        // Assert
        _mockCollectionTagRepository.Verify(x => x.DeleteAsync(collectionTag.Id), Times.Once);
    }

    [Fact]
    public async Task RemoveTagFromCollectionAsync_WithNonExistentTag_ShouldThrowArgumentException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        _mockTagRepository.Setup(x => x.GetByNameAsync("non-existent-tag"))
            .ReturnsAsync((Tag)null!);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _tagService.RemoveTagFromCollectionAsync(collectionId, "non-existent-tag"));
    }

    [Fact]
    public async Task RemoveTagFromCollectionAsync_WithTagNotInCollection_ShouldThrowArgumentException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var tagId = ObjectId.GenerateNewId();
        var tag = new Tag("test-tag", "Test description", Domain.ValueObjects.TagColor.Default);

        _mockTagRepository.Setup(x => x.GetByNameAsync("test-tag"))
            .ReturnsAsync(tag);
        _mockCollectionTagRepository.Setup(x => x.GetByCollectionIdAndTagIdAsync(collectionId, tagId))
            .ReturnsAsync((CollectionTag)null!);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _tagService.RemoveTagFromCollectionAsync(collectionId, "test-tag"));
    }

    [Fact]
    public async Task GetAllTagsAsync_ShouldReturnAllTags()
    {
        // Arrange
        var tags = new List<Tag>
        {
            new Tag("tag1", "Description 1", Domain.ValueObjects.TagColor.Default),
            new Tag("tag2", "Description 2", Domain.ValueObjects.TagColor.Default)
        };

        _mockTagRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(tags);

        // Act
        var result = await _tagService.GetAllTagsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTagAsync_WithValidTagId_ShouldReturnTag()
    {
        // Arrange
        var tag = new Tag("test-tag", "Test description", Domain.ValueObjects.TagColor.Default);

        _mockTagRepository.Setup(x => x.GetByIdAsync(tag.Id))
            .ReturnsAsync(tag);

        // Act
        var result = await _tagService.GetTagAsync(tag.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(tag.Id);
        result.Name.Should().Be("test-tag");
        result.Description.Should().Be("Test description");
    }

    [Fact]
    public async Task GetTagAsync_WithNonExistentTagId_ShouldThrowArgumentException()
    {
        // Arrange
        var tagId = ObjectId.GenerateNewId();
        _mockTagRepository.Setup(x => x.GetByIdAsync(tagId))
            .ReturnsAsync((Tag)null!);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _tagService.GetTagAsync(tagId));
    }

    [Fact]
    public async Task CreateTagAsync_WithValidData_ShouldCreateTag()
    {
        // Arrange
        var dto = new CreateTagDto
        {
            Name = "new-tag",
            Description = "New tag description",
            Color = new TagColorDto { R = 255, G = 0, B = 0 }
        };

        _mockTagRepository.Setup(x => x.CreateAsync(It.IsAny<Tag>()))
            .ReturnsAsync((Tag tag) => tag);

        // Act
        var result = await _tagService.CreateTagAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(dto.Name);
        result.Description.Should().Be(dto.Description);
        result.Color.R.Should().Be(dto.Color.R);
        result.Color.G.Should().Be(dto.Color.G);
        result.Color.B.Should().Be(dto.Color.B);
    }

    [Fact]
    public async Task UpdateTagAsync_WithValidData_ShouldUpdateTag()
    {
        // Arrange
        var tagId = ObjectId.GenerateNewId();
        var tag = new Tag("old-name", "Old description", Domain.ValueObjects.TagColor.Default);
        var dto = new UpdateTagDto
        {
            Name = "new-name",
            Description = "New description",
            Color = new TagColorDto { R = 0, G = 255, B = 0 }
        };

        _mockTagRepository.Setup(x => x.GetByIdAsync(tagId))
            .ReturnsAsync(tag);
        _mockTagRepository.Setup(x => x.UpdateAsync(It.IsAny<Tag>()))
            .ReturnsAsync((Tag tag) => tag);

        // Act
        var result = await _tagService.UpdateTagAsync(tagId, dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(dto.Name);
        result.Description.Should().Be(dto.Description);
        result.Color.R.Should().Be(dto.Color.R);
        result.Color.G.Should().Be(dto.Color.G);
        result.Color.B.Should().Be(dto.Color.B);
    }

    [Fact]
    public async Task UpdateTagAsync_WithNonExistentTagId_ShouldThrowArgumentException()
    {
        // Arrange
        var tagId = ObjectId.GenerateNewId();
        var dto = new UpdateTagDto
        {
            Name = "new-name",
            Description = "New description",
            Color = new TagColorDto { R = 0, G = 255, B = 0 }
        };

        _mockTagRepository.Setup(x => x.GetByIdAsync(tagId))
            .ReturnsAsync((Tag)null!);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _tagService.UpdateTagAsync(tagId, dto));
    }

    [Fact]
    public async Task DeleteTagAsync_WithValidTagId_ShouldDeleteTag()
    {
        // Arrange
        var tag = new Tag("test-tag", "Test description", Domain.ValueObjects.TagColor.Default);
        var collectionTags = new List<CollectionTag>();

        _mockTagRepository.Setup(x => x.GetByIdAsync(tag.Id))
            .ReturnsAsync(tag);
        _mockCollectionTagRepository.Setup(x => x.GetByTagIdAsync(tag.Id))
            .ReturnsAsync(collectionTags);
        _mockTagRepository.Setup(x => x.DeleteAsync(tag.Id))
            .Returns(Task.CompletedTask);

        // Act
        await _tagService.DeleteTagAsync(tag.Id);

        // Assert
        _mockTagRepository.Verify(x => x.DeleteAsync(tag.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteTagAsync_WithNonExistentTagId_ShouldThrowArgumentException()
    {
        // Arrange
        var tagId = ObjectId.GenerateNewId();
        _mockTagRepository.Setup(x => x.GetByIdAsync(tagId))
            .ReturnsAsync((Tag)null!);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _tagService.DeleteTagAsync(tagId));
    }

    [Fact]
    public async Task GetTagStatisticsAsync_ShouldReturnStatistics()
    {
        // Arrange
        var tags = new List<Tag>
        {
            new Tag("tag1", "Description 1", Domain.ValueObjects.TagColor.Default),
            new Tag("tag2", "Description 2", Domain.ValueObjects.TagColor.Default)
        };
        var collectionTags = new List<CollectionTag>
        {
            new CollectionTag(ObjectId.GenerateNewId(), tags[0].Id),
            new CollectionTag(ObjectId.GenerateNewId(), tags[1].Id)
        };

        _mockTagRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(tags);
        _mockCollectionTagRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(collectionTags);

        // Act
        var result = await _tagService.GetTagStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalTags.Should().Be(2);
        result.TotalTagUsages.Should().Be(2);
        result.AverageTagsPerCollection.Should().Be(1.0);
    }

    [Fact]
    public async Task SearchTagsAsync_WithValidQuery_ShouldReturnMatchingTags()
    {
        // Arrange
        var query = "test";
        var limit = 10;
        var tags = new List<Tag>
        {
            new Tag("test-tag", "Test description", Domain.ValueObjects.TagColor.Default),
            new Tag("other-tag", "Other description", Domain.ValueObjects.TagColor.Default)
        };

        _mockTagRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(tags);

        // Act
        var result = await _tagService.SearchTagsAsync(query, limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("test-tag");
    }

    [Fact]
    public async Task GetPopularTagsAsync_WithValidLimit_ShouldReturnPopularTags()
    {
        // Arrange
        var limit = 5;
        var tag = new Tag("popular-tag", "Popular description", Domain.ValueObjects.TagColor.Default);
        var collectionTags = new List<CollectionTag>
        {
            new CollectionTag(ObjectId.GenerateNewId(), tag.Id),
            new CollectionTag(ObjectId.GenerateNewId(), tag.Id)
        };
        // Set the Tag navigation property for each CollectionTag
        foreach (var ct in collectionTags)
        {
            typeof(CollectionTag).GetProperty("Tag")!.SetValue(ct, tag);
        }

        _mockCollectionTagRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(collectionTags);

        // Act
        var result = await _tagService.GetPopularTagsAsync(limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountLessOrEqualTo(limit);
    }

    [Fact]
    public async Task GetTagSuggestionsAsync_WithValidCollectionId_ShouldReturnSuggestions()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var collection = new Collection(ObjectId.GenerateNewId(), "Test Collection", "/path", Domain.Enums.CollectionType.Folder);
        var similarCollection = new Collection(ObjectId.GenerateNewId(), "Similar Collection", "/similar", Domain.Enums.CollectionType.Folder);
        var tag = new Tag("suggested-tag", "Suggested description", Domain.ValueObjects.TagColor.Default);
        var collectionTag = new CollectionTag(similarCollection.Id, tag.Id);

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync(collection);
        _mockCollectionRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Collection> { collection, similarCollection });
        _mockCollectionTagRepository.Setup(x => x.GetByCollectionIdAsync(similarCollection.Id))
            .ReturnsAsync(new List<CollectionTag> { collectionTag });

        // Act
        var result = await _tagService.GetTagSuggestionsAsync(collectionId, 10);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountLessOrEqualTo(10);
    }

    [Fact]
    public async Task GetTagSuggestionsAsync_WithNonExistentCollectionId_ShouldThrowArgumentException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync((Collection)null!);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _tagService.GetTagSuggestionsAsync(collectionId, 10));
    }

    [Fact]
    public async Task BulkAddTagsToCollectionAsync_WithValidData_ShouldAddTags()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var collection = new Collection(ObjectId.GenerateNewId(), "Test Collection", "/path", Domain.Enums.CollectionType.Folder);
        var tagNames = new List<string> { "tag1", "tag2" };

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync(collection);
        _mockTagRepository.Setup(x => x.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((Tag)null!);
        _mockTagRepository.Setup(x => x.CreateAsync(It.IsAny<Tag>()))
            .ReturnsAsync((Tag tag) => tag);
        _mockCollectionTagRepository.Setup(x => x.CreateAsync(It.IsAny<CollectionTag>()))
            .ReturnsAsync((CollectionTag ct) => ct);
        _mockUserContextService.Setup(x => x.GetCurrentUserName())
            .Returns("testuser");

        // Act
        var result = await _tagService.BulkAddTagsToCollectionAsync(collectionId, tagNames);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task BulkAddTagsToCollectionAsync_WithNonExistentCollection_ShouldThrowArgumentException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var tagNames = new List<string> { "tag1", "tag2" };

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync((Collection)null!);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _tagService.BulkAddTagsToCollectionAsync(collectionId, tagNames));
    }

    [Fact]
    public async Task BulkRemoveTagsFromCollectionAsync_WithValidData_ShouldRemoveTags()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var collection = new Collection(ObjectId.GenerateNewId(), "Test Collection", "/path", Domain.Enums.CollectionType.Folder);
        var tagNames = new List<string> { "tag1", "tag2" };
        var tag = new Tag("tag1", "Description", Domain.ValueObjects.TagColor.Default);
        var collectionTag = new CollectionTag(collectionId, tag.Id);
        // Set the Tag navigation property using reflection since it's private set
        typeof(CollectionTag).GetProperty("Tag")!.SetValue(collectionTag, tag);

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync(collection);
        _mockTagRepository.Setup(x => x.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(tag);
        _mockCollectionTagRepository.Setup(x => x.GetByCollectionIdAndTagIdAsync(collectionId, tag.Id))
            .ReturnsAsync(collectionTag);
        _mockCollectionTagRepository.Setup(x => x.DeleteAsync(collectionTag.Id))
            .Returns(Task.CompletedTask);

        // Act
        await _tagService.BulkRemoveTagsFromCollectionAsync(collectionId, tagNames);

        // Assert
        _mockCollectionTagRepository.Verify(x => x.DeleteAsync(It.IsAny<ObjectId>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task BulkRemoveTagsFromCollectionAsync_WithNonExistentCollection_ShouldThrowArgumentException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var tagNames = new List<string> { "tag1", "tag2" };

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync((Collection)null!);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _tagService.BulkRemoveTagsFromCollectionAsync(collectionId, tagNames));
    }
}