using ImageViewer.Domain.Entities;
using ImageViewer.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace ImageViewer.Tests.Domain.Entities;

public class TagTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateTag()
    {
        // Arrange
        var name = "Test Tag";
        var description = "Test Description";
        var color = new TagColor("#FF0000", "Red");

        // Act
        var tag = new Tag(name, description, color);

        // Assert
        tag.Should().NotBeNull();
        tag.Id.Should().NotBeEmpty();
        tag.Name.Should().Be(name);
        tag.Description.Should().Be(description);
        tag.Color.Should().Be(color);
        tag.UsageCount.Should().Be(0);
        tag.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        tag.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        tag.CollectionTags.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithMinimalParameters_ShouldCreateTag()
    {
        // Arrange
        var name = "Test Tag";

        // Act
        var tag = new Tag(name);

        // Assert
        tag.Should().NotBeNull();
        tag.Name.Should().Be(name);
        tag.Description.Should().Be("");
        tag.Color.Should().Be(TagColor.Default);
        tag.UsageCount.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithNullName_ShouldThrowArgumentNullException()
    {
        // Arrange
        string name = null!;

        // Act & Assert
        var action = () => new Tag(name);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("name");
    }

    [Fact]
    public void UpdateName_WithValidName_ShouldUpdateName()
    {
        // Arrange
        var tag = new Tag("Old Name");
        var newName = "New Name";
        var originalUpdatedAt = tag.UpdatedAt;

        // Act
        tag.UpdateName(newName);

        // Assert
        tag.Name.Should().Be(newName);
        tag.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateName_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        var tag = new Tag("Test Tag");

        // Act & Assert
        var action = () => tag.UpdateName(null!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot be null or empty*")
            .WithParameterName("name");
    }

    [Fact]
    public void UpdateName_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var tag = new Tag("Test Tag");

        // Act & Assert
        var action = () => tag.UpdateName("");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot be null or empty*")
            .WithParameterName("name");
    }

    [Fact]
    public void UpdateName_WithWhitespaceName_ShouldThrowArgumentException()
    {
        // Arrange
        var tag = new Tag("Test Tag");

        // Act & Assert
        var action = () => tag.UpdateName("   ");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot be null or empty*")
            .WithParameterName("name");
    }

    [Fact]
    public void UpdateDescription_WithValidDescription_ShouldUpdateDescription()
    {
        // Arrange
        var tag = new Tag("Test Tag");
        var newDescription = "New Description";
        var originalUpdatedAt = tag.UpdatedAt;

        // Act
        tag.UpdateDescription(newDescription);

        // Assert
        tag.Description.Should().Be(newDescription);
        tag.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateDescription_WithNullDescription_ShouldSetEmptyString()
    {
        // Arrange
        var tag = new Tag("Test Tag", "Original Description");
        var originalUpdatedAt = tag.UpdatedAt;

        // Act
        tag.UpdateDescription(null!);

        // Assert
        tag.Description.Should().Be("");
        tag.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateColor_WithValidColor_ShouldUpdateColor()
    {
        // Arrange
        var tag = new Tag("Test Tag");
        var newColor = new TagColor("#00FF00", "Green");
        var originalUpdatedAt = tag.UpdatedAt;

        // Act
        tag.UpdateColor(newColor);

        // Assert
        tag.Color.Should().Be(newColor);
        tag.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateColor_WithNullColor_ShouldThrowArgumentNullException()
    {
        // Arrange
        var tag = new Tag("Test Tag");

        // Act & Assert
        var action = () => tag.UpdateColor(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("color");
    }

    [Fact]
    public void IncrementUsage_ShouldIncrementUsageCount()
    {
        // Arrange
        var tag = new Tag("Test Tag");
        var originalUsageCount = tag.UsageCount;
        var originalUpdatedAt = tag.UpdatedAt;

        // Act
        tag.IncrementUsage();

        // Assert
        tag.UsageCount.Should().Be(originalUsageCount + 1);
        tag.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void DecrementUsage_WithPositiveUsageCount_ShouldDecrementUsageCount()
    {
        // Arrange
        var tag = new Tag("Test Tag");
        tag.IncrementUsage();
        tag.IncrementUsage();
        var originalUsageCount = tag.UsageCount;
        var originalUpdatedAt = tag.UpdatedAt;

        // Act
        tag.DecrementUsage();

        // Assert
        tag.UsageCount.Should().Be(originalUsageCount - 1);
        tag.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void DecrementUsage_WithZeroUsageCount_ShouldNotGoBelowZero()
    {
        // Arrange
        var tag = new Tag("Test Tag");
        var originalUpdatedAt = tag.UpdatedAt;

        // Act
        tag.DecrementUsage();

        // Assert
        tag.UsageCount.Should().Be(0);
        tag.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void AddCollectionTag_WithValidCollectionTag_ShouldAddCollectionTag()
    {
        // Arrange
        var tag = new Tag("Test Tag");
        var collectionId = Guid.NewGuid();
        var collectionTag = new CollectionTag(Guid.NewGuid(), collectionId);

        // Act
        tag.AddCollectionTag(collectionTag);

        // Assert
        tag.CollectionTags.Should().HaveCount(1);
        tag.CollectionTags.Should().Contain(collectionTag);
        tag.UsageCount.Should().Be(1);
    }

    [Fact]
    public void AddCollectionTag_WithNullCollectionTag_ShouldThrowArgumentNullException()
    {
        // Arrange
        var tag = new Tag("Test Tag");

        // Act & Assert
        var action = () => tag.AddCollectionTag(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("collectionTag");
    }

    [Fact]
    public void AddCollectionTag_WithDuplicateCollectionId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var tag = new Tag("Test Tag");
        var collectionId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var collectionTag1 = new CollectionTag(collectionId, tagId);
        var collectionTag2 = new CollectionTag(collectionId, tagId);
        tag.AddCollectionTag(collectionTag1);

        // Act & Assert
        var action = () => tag.AddCollectionTag(collectionTag2);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"Collection '{collectionId}' already has this tag");
    }

    [Fact]
    public void RemoveCollectionTag_WithValidCollectionId_ShouldRemoveCollectionTag()
    {
        // Arrange
        var tag = new Tag("Test Tag");
        var collectionId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var collectionTag = new CollectionTag(collectionId, tagId);
        tag.AddCollectionTag(collectionTag);
        var originalUsageCount = tag.UsageCount;

        // Act
        tag.RemoveCollectionTag(collectionId);

        // Assert
        tag.CollectionTags.Should().BeEmpty();
        tag.UsageCount.Should().Be(originalUsageCount - 1);
    }

    [Fact]
    public void RemoveCollectionTag_WithInvalidCollectionId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var tag = new Tag("Test Tag");
        var invalidCollectionId = Guid.NewGuid();

        // Act & Assert
        var action = () => tag.RemoveCollectionTag(invalidCollectionId);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"Collection '{invalidCollectionId}' does not have this tag");
    }

    [Fact]
    public void IsPopular_WithUsageCountAboveThreshold_ShouldReturnTrue()
    {
        // Arrange
        var tag = new Tag("Test Tag");
        for (int i = 0; i < 15; i++)
        {
            tag.IncrementUsage();
        }

        // Act
        var isPopular = tag.IsPopular(10);

        // Assert
        isPopular.Should().BeTrue();
    }

    [Fact]
    public void IsPopular_WithUsageCountBelowThreshold_ShouldReturnFalse()
    {
        // Arrange
        var tag = new Tag("Test Tag");
        for (int i = 0; i < 5; i++)
        {
            tag.IncrementUsage();
        }

        // Act
        var isPopular = tag.IsPopular(10);

        // Assert
        isPopular.Should().BeFalse();
    }

    [Fact]
    public void IsPopular_WithUsageCountEqualToThreshold_ShouldReturnTrue()
    {
        // Arrange
        var tag = new Tag("Test Tag");
        for (int i = 0; i < 10; i++)
        {
            tag.IncrementUsage();
        }

        // Act
        var isPopular = tag.IsPopular(10);

        // Assert
        isPopular.Should().BeTrue();
    }

    [Fact]
    public void IsUnused_WithZeroUsageCount_ShouldReturnTrue()
    {
        // Arrange
        var tag = new Tag("Test Tag");

        // Act
        var isUnused = tag.IsUnused();

        // Assert
        isUnused.Should().BeTrue();
    }

    [Fact]
    public void IsUnused_WithPositiveUsageCount_ShouldReturnFalse()
    {
        // Arrange
        var tag = new Tag("Test Tag");
        tag.IncrementUsage();

        // Act
        var isUnused = tag.IsUnused();

        // Assert
        isUnused.Should().BeFalse();
    }
}
