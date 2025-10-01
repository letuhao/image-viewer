using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Events;
using FluentAssertions;
using Xunit;

namespace ImageViewer.Tests.Domain.Entities;

public class CollectionTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateCollection()
    {
        // Arrange
        var name = "Test Collection";
        var path = "/test/path";
        var type = CollectionType.Folder;

        // Act
        var collection = new Collection(name, path, type);

        // Assert
        collection.Should().NotBeNull();
        collection.Id.Should().NotBeEmpty();
        collection.Name.Should().Be(name);
        collection.Path.Should().Be(path);
        collection.Type.Should().Be(type);
        collection.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        collection.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        collection.IsDeleted.Should().BeFalse();
        collection.DeletedAt.Should().BeNull();
        collection.Images.Should().BeEmpty();
        collection.Tags.Should().BeEmpty();
        collection.CacheBindings.Should().BeEmpty();
        collection.Statistics.Should().BeNull();
        collection.Settings.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullName_ShouldThrowArgumentNullException()
    {
        // Arrange
        string name = null!;
        var path = "/test/path";
        var type = CollectionType.Folder;

        // Act & Assert
        var action = () => new Collection(name, path, type);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Constructor_WithNullPath_ShouldThrowArgumentNullException()
    {
        // Arrange
        var name = "Test Collection";
        string path = null!;
        var type = CollectionType.Folder;

        // Act & Assert
        var action = () => new Collection(name, path, type);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("path");
    }

    [Fact]
    public void Constructor_ShouldAddCollectionCreatedEvent()
    {
        // Arrange
        var name = "Test Collection";
        var path = "/test/path";
        var type = CollectionType.Folder;

        // Act
        var collection = new Collection(name, path, type);

        // Assert
        collection.DomainEvents.Should().HaveCount(1);
        collection.DomainEvents.First().Should().BeOfType<CollectionCreatedEvent>();
    }

    [Fact]
    public void UpdateName_WithValidName_ShouldUpdateName()
    {
        // Arrange
        var collection = new Collection("Old Name", "/test/path", CollectionType.Folder);
        var newName = "New Name";
        var originalUpdatedAt = collection.UpdatedAt;

        // Act
        collection.UpdateName(newName);

        // Assert
        collection.Name.Should().Be(newName);
        collection.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateName_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);

        // Act & Assert
        var action = () => collection.UpdateName(null!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot be null or empty*")
            .WithParameterName("name");
    }

    [Fact]
    public void UpdateName_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);

        // Act & Assert
        var action = () => collection.UpdateName("");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot be null or empty*")
            .WithParameterName("name");
    }

    [Fact]
    public void UpdateName_WithWhitespaceName_ShouldThrowArgumentException()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);

        // Act & Assert
        var action = () => collection.UpdateName("   ");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot be null or empty*")
            .WithParameterName("name");
    }

    [Fact]
    public void UpdatePath_WithValidPath_ShouldUpdatePath()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/old/path", CollectionType.Folder);
        var newPath = "/new/path";
        var originalUpdatedAt = collection.UpdatedAt;

        // Act
        collection.UpdatePath(newPath);

        // Assert
        collection.Path.Should().Be(newPath);
        collection.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdatePath_WithNullPath_ShouldThrowArgumentException()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);

        // Act & Assert
        var action = () => collection.UpdatePath(null!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Path cannot be null or empty*")
            .WithParameterName("path");
    }

    [Fact]
    public void UpdatePath_WithEmptyPath_ShouldThrowArgumentException()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);

        // Act & Assert
        var action = () => collection.UpdatePath("");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Path cannot be null or empty*")
            .WithParameterName("path");
    }

    [Fact]
    public void SetSettings_WithValidSettings_ShouldSetSettings()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);
        var settings = new CollectionSettingsEntity(Guid.NewGuid());
        var originalUpdatedAt = collection.UpdatedAt;

        // Act
        collection.SetSettings(settings);

        // Assert
        collection.Settings.Should().Be(settings);
        collection.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void SetSettings_WithNullSettings_ShouldThrowArgumentNullException()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);

        // Act & Assert
        var action = () => collection.SetSettings(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("settings");
    }

    [Fact]
    public void AddImage_WithValidImage_ShouldAddImage()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1000L, 1024, 768, "jpg");
        var originalUpdatedAt = collection.UpdatedAt;

        // Act
        collection.AddImage(image);

        // Assert
        collection.Images.Should().HaveCount(1);
        collection.Images.Should().Contain(image);
        collection.UpdatedAt.Should().BeAfter(originalUpdatedAt);
        collection.DomainEvents.Should().HaveCount(2); // CollectionCreatedEvent + ImageAddedEvent
        collection.DomainEvents.Last().Should().BeOfType<ImageAddedEvent>();
    }

    [Fact]
    public void AddImage_WithNullImage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);

        // Act & Assert
        var action = () => collection.AddImage(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("image");
    }

    [Fact]
    public void AddImage_WithDuplicateFilename_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);
        var image1 = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1000L, 1024, 768, "jpg");
        var image2 = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test2.jpg", 1000L, 1024, 768, "jpg");
        collection.AddImage(image1);

        // Act & Assert
        var action = () => collection.AddImage(image2);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Image with filename 'test.jpg' already exists in collection");
    }

    [Fact]
    public void RemoveImage_WithValidImageId_ShouldRemoveImage()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1000L, 1024, 768, "jpg");
        collection.AddImage(image);
        var originalUpdatedAt = collection.UpdatedAt;

        // Act
        collection.RemoveImage(image.Id);

        // Assert
        collection.Images.Should().BeEmpty();
        collection.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void RemoveImage_WithInvalidImageId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);
        var invalidId = Guid.NewGuid();

        // Act & Assert
        var action = () => collection.RemoveImage(invalidId);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"Image with ID '{invalidId}' not found in collection");
    }

    [Fact]
    public void AddTag_WithValidTag_ShouldAddTag()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);
        var tag = new CollectionTag(Guid.NewGuid(), Guid.NewGuid());
        var originalUpdatedAt = collection.UpdatedAt;

        // Act
        collection.AddTag(tag);

        // Assert
        collection.Tags.Should().HaveCount(1);
        collection.Tags.Should().Contain(tag);
        collection.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void AddTag_WithNullTag_ShouldThrowArgumentNullException()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);

        // Act & Assert
        var action = () => collection.AddTag(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("tag");
    }

    [Fact]
    public void AddTag_WithDuplicateTagId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);
        var tagId = Guid.NewGuid();
        var tag1 = new CollectionTag(Guid.NewGuid(), tagId);
        var tag2 = new CollectionTag(Guid.NewGuid(), tagId);
        collection.AddTag(tag1);

        // Act & Assert
        var action = () => collection.AddTag(tag2);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"Tag with ID '{tagId}' already exists in collection");
    }

    [Fact]
    public void RemoveTag_WithValidTagId_ShouldRemoveTag()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);
        var tag = new CollectionTag(Guid.NewGuid(), Guid.NewGuid());
        collection.AddTag(tag);
        var originalUpdatedAt = collection.UpdatedAt;

        // Act
        collection.RemoveTag(tag.TagId);

        // Assert
        collection.Tags.Should().BeEmpty();
        collection.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void RemoveTag_WithInvalidTagId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);
        var invalidTagId = Guid.NewGuid();

        // Act & Assert
        var action = () => collection.RemoveTag(invalidTagId);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"Tag with ID '{invalidTagId}' not found in collection");
    }

    [Fact]
    public void SoftDelete_ShouldMarkAsDeleted()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);
        var originalUpdatedAt = collection.UpdatedAt;

        // Act
        collection.SoftDelete();

        // Assert
        collection.IsDeleted.Should().BeTrue();
        collection.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        collection.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Restore_ShouldMarkAsNotDeleted()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);
        collection.SoftDelete();
        var originalUpdatedAt = collection.UpdatedAt;

        // Act
        collection.Restore();

        // Assert
        collection.IsDeleted.Should().BeFalse();
        collection.DeletedAt.Should().BeNull();
        collection.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void GetImageCount_WithNoImages_ShouldReturnZero()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);

        // Act
        var count = collection.GetImageCount();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void GetImageCount_WithImages_ShouldReturnCorrectCount()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);
        var image1 = new Image(Guid.NewGuid(), "test1.jpg", "/test/path/test1.jpg", 1000L, 1024, 768, "jpg");
        var image2 = new Image(Guid.NewGuid(), "test2.jpg", "/test/path/test2.jpg", 2000L, 1024, 768, "jpg");
        collection.AddImage(image1);
        collection.AddImage(image2);

        // Act
        var count = collection.GetImageCount();

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public void GetTotalSize_WithNoImages_ShouldReturnZero()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);

        // Act
        var totalSize = collection.GetTotalSize();

        // Assert
        totalSize.Should().Be(0);
    }

    [Fact]
    public void GetTotalSize_WithImages_ShouldReturnCorrectTotal()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);
        var image1 = new Image(Guid.NewGuid(), "test1.jpg", "/test/path/test1.jpg", 1000L, 1024, 768, "jpg");
        var image2 = new Image(Guid.NewGuid(), "test2.jpg", "/test/path/test2.jpg", 2000L, 1024, 768, "jpg");
        collection.AddImage(image1);
        collection.AddImage(image2);

        // Act
        var totalSize = collection.GetTotalSize();

        // Assert
        totalSize.Should().Be(3000);
    }

    [Fact]
    public void GetImagesByFormat_WithMatchingFormat_ShouldReturnMatchingImages()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);
        var jpgImage = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1000L, 1024, 768, "jpg");
        var pngImage = new Image(Guid.NewGuid(), "test.png", "/test/path/test.png", 2000L, 1024, 768, "png");
        collection.AddImage(jpgImage);
        collection.AddImage(pngImage);

        // Act
        var jpgImages = collection.GetImagesByFormat("jpg");

        // Assert
        jpgImages.Should().HaveCount(1);
        jpgImages.Should().Contain(jpgImage);
    }

    [Fact]
    public void GetImagesByFormat_WithCaseInsensitiveFormat_ShouldReturnMatchingImages()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);
        var jpgImage = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1000L, 1024, 768, "jpg");
        collection.AddImage(jpgImage);

        // Act
        var jpgImages = collection.GetImagesByFormat("JPG");

        // Assert
        jpgImages.Should().HaveCount(1);
        jpgImages.Should().Contain(jpgImage);
    }

    [Fact]
    public void GetImagesBySizeRange_WithMatchingSize_ShouldReturnMatchingImages()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);
        var largeImage = new Image(Guid.NewGuid(), "large.jpg", "/test/path/large.jpg", 1000L, 1920, 1080, "jpg");
        var smallImage = new Image(Guid.NewGuid(), "small.jpg", "/test/path/small.jpg", 1000L, 800, 600, "jpg");
        collection.AddImage(largeImage);
        collection.AddImage(smallImage);

        // Act
        var largeImages = collection.GetImagesBySizeRange(1024, 768);

        // Assert
        largeImages.Should().HaveCount(1);
        largeImages.Should().Contain(largeImage);
    }

    [Fact]
    public void GetImagesBySizeRange_WithNoMatchingSize_ShouldReturnEmptyCollection()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);
        var smallImage = new Image(Guid.NewGuid(), "small.jpg", "/test/path/small.jpg", 1000L, 800, 600, "jpg");
        collection.AddImage(smallImage);

        // Act
        var largeImages = collection.GetImagesBySizeRange(1024, 768);

        // Assert
        largeImages.Should().BeEmpty();
    }
}