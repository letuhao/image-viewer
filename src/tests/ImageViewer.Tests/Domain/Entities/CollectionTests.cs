using FluentAssertions;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Tests.Common;

namespace ImageViewer.Tests.Domain.Entities;

public class CollectionTests : TestBase
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateCollection()
    {
        // Arrange
        var name = "Test Collection";
        var path = "C:\\Test\\Path";
        var type = CollectionType.Folder;

        // Act
        var collection = new Collection(name, path, type);

        // Assert
        collection.Should().NotBeNull();
        collection.Name.Should().Be(name);
        collection.Path.Should().Be(path);
        collection.Type.Should().Be(type);
        collection.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        collection.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        collection.IsDeleted.Should().BeFalse();
        collection.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        var path = "C:\\Test\\Path";
        var type = CollectionType.Folder;

        // Act & Assert
        var action = () => new Collection(null!, path, type);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Constructor_WithNullPath_ShouldThrowArgumentException()
    {
        // Arrange
        var name = "Test Collection";
        var type = CollectionType.Folder;

        // Act & Assert
        var action = () => new Collection(name, null!, type);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("path");
    }


    [Fact]
    public void UpdateName_WithValidName_ShouldUpdateName()
    {
        // Arrange
        var collection = CreateValidCollection();
        var newName = "Updated Collection Name";

        // Act
        collection.UpdateName(newName);

        // Assert
        collection.Name.Should().Be(newName);
        collection.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateName_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        var collection = CreateValidCollection();

        // Act & Assert
        var action = () => collection.UpdateName(null!);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void UpdateName_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var collection = CreateValidCollection();

        // Act & Assert
        var action = () => collection.UpdateName("");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void UpdateName_WithWhitespaceName_ShouldThrowArgumentException()
    {
        // Arrange
        var collection = CreateValidCollection();

        // Act & Assert
        var action = () => collection.UpdateName("   ");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void UpdatePath_WithValidPath_ShouldUpdatePath()
    {
        // Arrange
        var collection = CreateValidCollection();
        var newPath = "C:\\New\\Path";

        // Act
        collection.UpdatePath(newPath);

        // Assert
        collection.Path.Should().Be(newPath);
        collection.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdatePath_WithNullPath_ShouldThrowArgumentException()
    {
        // Arrange
        var collection = CreateValidCollection();

        // Act & Assert
        var action = () => collection.UpdatePath(null!);
        action.Should().Throw<ArgumentException>()
            .WithParameterName("path");
    }

    [Fact]
    public void UpdatePath_WithEmptyPath_ShouldThrowArgumentException()
    {
        // Arrange
        var collection = CreateValidCollection();

        // Act & Assert
        var action = () => collection.UpdatePath("");
        action.Should().Throw<ArgumentException>()
            .WithParameterName("path");
    }

    [Fact]
    public void SetSettings_WithValidSettings_ShouldSetSettings()
    {
        // Arrange
        var collection = CreateValidCollection();
        var newSettings = new CollectionSettingsEntity(
            collection.Id,
            100, // totalImages
            1024 * 1024 * 100, // totalSizeBytes
            300, 300, // thumbnail size
            1920, 1080, // cache size
            true, true, // auto generate
            TimeSpan.FromDays(30),
            "{}"
        );

        // Act
        collection.SetSettings(newSettings);

        // Assert
        collection.Settings.Should().Be(newSettings);
        collection.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SetSettings_WithNullSettings_ShouldThrowArgumentException()
    {
        // Arrange
        var collection = CreateValidCollection();

        // Act & Assert
        var action = () => collection.SetSettings(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("settings");
    }

    [Fact]
    public void SoftDelete_ShouldMarkAsDeleted()
    {
        // Arrange
        var collection = CreateValidCollection();

        // Act
        collection.SoftDelete();

        // Assert
        collection.IsDeleted.Should().BeTrue();
        collection.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        collection.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Restore_ShouldMarkAsNotDeleted()
    {
        // Arrange
        var collection = CreateValidCollection();
        collection.SoftDelete();

        // Act
        collection.Restore();

        // Assert
        collection.IsDeleted.Should().BeFalse();
        collection.DeletedAt.Should().BeNull();
        collection.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetImageCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var collection = CreateValidCollection();
        var image1 = new Image(Guid.NewGuid(), "test1.jpg", "test1.jpg", 1024, 1920, 1080, "jpeg");
        var image2 = new Image(Guid.NewGuid(), "test2.jpg", "test2.jpg", 1024, 1920, 1080, "jpeg");
        
        collection.AddImage(image1);
        collection.AddImage(image2);

        // Act
        var count = collection.GetImageCount();

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public void GetTotalSize_ShouldReturnCorrectSize()
    {
        // Arrange
        var collection = CreateValidCollection();
        var image1 = new Image(Guid.NewGuid(), "test1.jpg", "test1.jpg", 1024 * 1024, 1920, 1080, "jpeg");
        var image2 = new Image(Guid.NewGuid(), "test2.jpg", "test2.jpg", 2048 * 1024, 1920, 1080, "jpeg");
        
        collection.AddImage(image1);
        collection.AddImage(image2);

        // Act
        var totalSize = collection.GetTotalSize();

        // Assert
        totalSize.Should().Be(3 * 1024 * 1024); // 3MB total
    }

    [Fact]
    public void GetImagesByFormat_ShouldReturnCorrectImages()
    {
        // Arrange
        var collection = CreateValidCollection();
        var jpegImage = new Image(Guid.NewGuid(), "test.jpg", "test.jpg", 1024, 1920, 1080, "jpeg");
        var pngImage = new Image(Guid.NewGuid(), "test.png", "test.png", 1024, 1920, 1080, "png");
        
        collection.AddImage(jpegImage);
        collection.AddImage(pngImage);

        // Act
        var jpegImages = collection.GetImagesByFormat("jpeg");

        // Assert
        jpegImages.Should().HaveCount(1);
        jpegImages.First().Filename.Should().Be("test.jpg");
    }

    private Collection CreateValidCollection()
    {
        return new Collection(
            "Test Collection",
            "C:\\Test\\Path",
            CollectionType.Folder
        );
    }
}
