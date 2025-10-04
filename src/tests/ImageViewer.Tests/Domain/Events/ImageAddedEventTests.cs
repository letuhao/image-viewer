using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Events;
using ImageViewer.Domain.Enums;
using FluentAssertions;
using Xunit;
using MongoDB.Bson;

namespace ImageViewer.Tests.Domain.Events;

public class ImageAddedEventTests
{
    [Fact]
    public void Constructor_WithValidImageAndCollection_ShouldCreateEvent()
    {
        // Arrange
        var collection = new Collection(ObjectId.GenerateNewId(), "Test Collection", "/test/path", CollectionType.Folder);
        var image = new Image(collection.Id, "test.jpg", "/test/path/test.jpg", 1024, 1920, 1080, "jpg");

        // Act
        var domainEvent = new ImageAddedEvent(image, collection);

        // Assert
        domainEvent.Should().NotBeNull();
        domainEvent.Id.Should().NotBeEmpty();
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        domainEvent.Image.Should().Be(image);
        domainEvent.Collection.Should().Be(collection);
    }

    [Fact]
    public void Constructor_WithNullImage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var collection = new Collection(ObjectId.GenerateNewId(), "Test Collection", "/test/path", CollectionType.Folder);
        Image image = null!;

        // Act & Assert
        var action = () => new ImageAddedEvent(image, collection);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("image");
    }

    [Fact]
    public void Constructor_WithNullCollection_ShouldThrowArgumentNullException()
    {
        // Arrange
        var collection = new Collection(ObjectId.GenerateNewId(), "Test Collection", "/test/path", CollectionType.Folder);
        var image = new Image(collection.Id, "test.jpg", "/test/path/test.jpg", 1024, 1920, 1080, "jpg");
        Collection nullCollection = null!;

        // Act & Assert
        var action = () => new ImageAddedEvent(image, nullCollection);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("collection");
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueId()
    {
        // Arrange
        var collection = new Collection(ObjectId.GenerateNewId(), "Test Collection", "/test/path", CollectionType.Folder);
        var image = new Image(collection.Id, "test.jpg", "/test/path/test.jpg", 1024, 1920, 1080, "jpg");

        // Act
        var event1 = new ImageAddedEvent(image, collection);
        var event2 = new ImageAddedEvent(image, collection);

        // Assert
        event1.Id.Should().NotBe(event2.Id);
    }

    [Fact]
    public void Constructor_ShouldSetOccurredOnToCurrentTime()
    {
        // Arrange
        var collection = new Collection(ObjectId.GenerateNewId(), "Test Collection", "/test/path", CollectionType.Folder);
        var image = new Image(collection.Id, "test.jpg", "/test/path/test.jpg", 1024, 1920, 1080, "jpg");
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new ImageAddedEvent(image, collection);

        // Assert
        domainEvent.OccurredOn.Should().BeAfter(beforeCreation);
        domainEvent.OccurredOn.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }
}
