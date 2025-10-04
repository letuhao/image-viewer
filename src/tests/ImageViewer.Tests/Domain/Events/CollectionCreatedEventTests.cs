using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Events;
using ImageViewer.Domain.Enums;
using FluentAssertions;
using Xunit;
using MongoDB.Bson;

namespace ImageViewer.Tests.Domain.Events;

public class CollectionCreatedEventTests
{
    [Fact]
    public void Constructor_WithValidCollection_ShouldCreateEvent()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var libraryId = ObjectId.GenerateNewId();
        var collectionName = "Test Collection";

        // Act
        var domainEvent = new CollectionCreatedEvent(collectionId, collectionName, libraryId);

        // Assert
        domainEvent.Should().NotBeNull();
        domainEvent.Id.Should().NotBeEmpty();
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        domainEvent.CollectionId.Should().Be(collectionId);
        domainEvent.CollectionName.Should().Be(collectionName);
        domainEvent.LibraryId.Should().Be(libraryId);
    }

    [Fact]
    public void Constructor_WithNullCollection_ShouldThrowArgumentNullException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var libraryId = ObjectId.GenerateNewId();
        string collectionName = null!;

        // Act & Assert
        var action = () => new CollectionCreatedEvent(collectionId, collectionName, libraryId);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("collectionName");
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueId()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);

        // Act
        var event1 = new CollectionCreatedEvent(collection);
        var event2 = new CollectionCreatedEvent(collection);

        // Assert
        event1.Id.Should().NotBe(event2.Id);
    }

    [Fact]
    public void Constructor_ShouldSetOccurredOnToCurrentTime()
    {
        // Arrange
        var collection = new Collection("Test Collection", "/test/path", CollectionType.Folder);
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new CollectionCreatedEvent(collection);

        // Assert
        domainEvent.OccurredOn.Should().BeAfter(beforeCreation);
        domainEvent.OccurredOn.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }
}
