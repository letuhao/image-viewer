using ImageViewer.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ImageViewer.Tests.Domain.Entities;

public class ImageTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateImage()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        var filename = "test.jpg";
        var relativePath = "/test/path/test.jpg";
        var fileSize = 1024L;
        var width = 1920;
        var height = 1080;
        var format = "jpg";

        // Act
        var image = new Image(collectionId, filename, relativePath, fileSize, width, height, format);

        // Assert
        image.Should().NotBeNull();
        image.Id.Should().NotBeEmpty();
        image.CollectionId.Should().Be(collectionId);
        image.Filename.Should().Be(filename);
        image.RelativePath.Should().Be(relativePath);
        image.FileSize.Should().Be(fileSize);
        image.FileSizeBytes.Should().Be(fileSize);
        image.Width.Should().Be(width);
        image.Height.Should().Be(height);
        image.Format.Should().Be(format);
        image.ViewCount.Should().Be(0);
        image.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        image.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        image.IsDeleted.Should().BeFalse();
        image.DeletedAt.Should().BeNull();
        image.CacheInfo.Should().BeNull();
        image.Metadata.Should().BeNull();
        image.CacheInfoCollection.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullFilename_ShouldThrowArgumentNullException()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        string filename = null!;
        var relativePath = "/test/path/test.jpg";
        var fileSize = 1024L;
        var width = 1920;
        var height = 1080;
        var format = "jpg";

        // Act & Assert
        var action = () => new Image(collectionId, filename, relativePath, fileSize, width, height, format);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("filename");
    }

    [Fact]
    public void Constructor_WithNullRelativePath_ShouldThrowArgumentNullException()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        var filename = "test.jpg";
        string relativePath = null!;
        var fileSize = 1024L;
        var width = 1920;
        var height = 1080;
        var format = "jpg";

        // Act & Assert
        var action = () => new Image(collectionId, filename, relativePath, fileSize, width, height, format);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("relativePath");
    }

    [Fact]
    public void Constructor_WithNullFormat_ShouldThrowArgumentNullException()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        var filename = "test.jpg";
        var relativePath = "/test/path/test.jpg";
        var fileSize = 1024L;
        var width = 1920;
        var height = 1080;
        string format = null!;

        // Act & Assert
        var action = () => new Image(collectionId, filename, relativePath, fileSize, width, height, format);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("format");
    }

    [Fact]
    public void SetMetadata_WithValidMetadata_ShouldSetMetadata()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024L, 1920, 1080, "jpg");
        var metadata = new ImageMetadataEntity(Guid.NewGuid());
        var originalUpdatedAt = image.UpdatedAt;

        // Act
        image.SetMetadata(metadata);

        // Assert
        image.Metadata.Should().Be(metadata);
        image.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void SetMetadata_WithNullMetadata_ShouldThrowArgumentNullException()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024L, 1920, 1080, "jpg");

        // Act & Assert
        var action = () => image.SetMetadata(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("metadata");
    }

    [Fact]
    public void UpdateDimensions_WithValidDimensions_ShouldUpdateDimensions()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024L, 1920, 1080, "jpg");
        var newWidth = 2560;
        var newHeight = 1440;
        var originalUpdatedAt = image.UpdatedAt;

        // Act
        image.UpdateDimensions(newWidth, newHeight);

        // Assert
        image.Width.Should().Be(newWidth);
        image.Height.Should().Be(newHeight);
        image.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateDimensions_WithZeroWidth_ShouldThrowArgumentException()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024L, 1920, 1080, "jpg");

        // Act & Assert
        var action = () => image.UpdateDimensions(0, 1080);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Width must be greater than 0*")
            .WithParameterName("width");
    }

    [Fact]
    public void UpdateDimensions_WithNegativeWidth_ShouldThrowArgumentException()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024L, 1920, 1080, "jpg");

        // Act & Assert
        var action = () => image.UpdateDimensions(-1, 1080);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Width must be greater than 0*")
            .WithParameterName("width");
    }

    [Fact]
    public void UpdateDimensions_WithZeroHeight_ShouldThrowArgumentException()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024L, 1920, 1080, "jpg");

        // Act & Assert
        var action = () => image.UpdateDimensions(1920, 0);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Height must be greater than 0*")
            .WithParameterName("height");
    }

    [Fact]
    public void UpdateDimensions_WithNegativeHeight_ShouldThrowArgumentException()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024L, 1920, 1080, "jpg");

        // Act & Assert
        var action = () => image.UpdateDimensions(1920, -1);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Height must be greater than 0*")
            .WithParameterName("height");
    }

    [Fact]
    public void UpdateFileSize_WithValidFileSize_ShouldUpdateFileSize()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024L, 1920, 1080, "jpg");
        var newFileSize = 2048L;
        var originalUpdatedAt = image.UpdatedAt;

        // Act
        image.UpdateFileSize(newFileSize);

        // Assert
        image.FileSize.Should().Be(newFileSize);
        image.FileSizeBytes.Should().Be(newFileSize);
        image.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateFileSize_WithNegativeFileSize_ShouldThrowArgumentException()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024L, 1920, 1080, "jpg");

        // Act & Assert
        var action = () => image.UpdateFileSize(-1);
        action.Should().Throw<ArgumentException>()
            .WithMessage("File size cannot be negative*")
            .WithParameterName("fileSize");
    }

    [Fact]
    public void SoftDelete_ShouldMarkAsDeleted()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024L, 1920, 1080, "jpg");
        var originalUpdatedAt = image.UpdatedAt;

        // Act
        image.SoftDelete();

        // Assert
        image.IsDeleted.Should().BeTrue();
        image.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        image.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Restore_ShouldMarkAsNotDeleted()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024L, 1920, 1080, "jpg");
        image.SoftDelete();
        var originalUpdatedAt = image.UpdatedAt;

        // Act
        image.Restore();

        // Assert
        image.IsDeleted.Should().BeFalse();
        image.DeletedAt.Should().BeNull();
        image.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void SetCacheInfo_WithValidCacheInfo_ShouldSetCacheInfo()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024L, 1920, 1080, "jpg");
        var cacheInfo = new ImageCacheInfo(Guid.NewGuid(), "cache.jpg", "1920x1080", 512L, DateTime.UtcNow.AddDays(1));
        var originalUpdatedAt = image.UpdatedAt;

        // Act
        image.SetCacheInfo(cacheInfo);

        // Assert
        image.CacheInfo.Should().Be(cacheInfo);
        image.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void SetCacheInfo_WithNullCacheInfo_ShouldThrowArgumentNullException()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024L, 1920, 1080, "jpg");

        // Act & Assert
        var action = () => image.SetCacheInfo(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("cacheInfo");
    }

    [Fact]
    public void ClearCacheInfo_ShouldRemoveCacheInfo()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024L, 1920, 1080, "jpg");
        var cacheInfo = new ImageCacheInfo(Guid.NewGuid(), "cache.jpg", "1920x1080", 512L, DateTime.UtcNow.AddDays(1));
        image.SetCacheInfo(cacheInfo);
        var originalUpdatedAt = image.UpdatedAt;

        // Act
        image.ClearCacheInfo();

        // Assert
        image.CacheInfo.Should().BeNull();
        image.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void IncrementViewCount_ShouldIncrementViewCount()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024L, 1920, 1080, "jpg");
        var originalViewCount = image.ViewCount;
        var originalUpdatedAt = image.UpdatedAt;

        // Act
        image.IncrementViewCount();

        // Assert
        image.ViewCount.Should().Be(originalViewCount + 1);
        image.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void GetAspectRatio_WithValidDimensions_ShouldReturnCorrectRatio()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024L, 1920, 1080, "jpg");

        // Act
        var aspectRatio = image.GetAspectRatio();

        // Assert
        aspectRatio.Should().BeApproximately(1.777, 0.001); // 1920/1080
    }

    [Fact]
    public void GetAspectRatio_WithZeroHeight_ShouldReturnZero()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024, 1920, 0, "jpg");

        // Act
        var aspectRatio = image.GetAspectRatio();

        // Assert
        aspectRatio.Should().Be(0);
    }

    [Fact]
    public void IsLandscape_WithLandscapeImage_ShouldReturnTrue()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024L, 1920, 1080, "jpg");

        // Act
        var isLandscape = image.IsLandscape();

        // Assert
        isLandscape.Should().BeTrue();
    }

    [Fact]
    public void IsLandscape_WithPortraitImage_ShouldReturnFalse()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024, 1080, 1920, "jpg");

        // Act
        var isLandscape = image.IsLandscape();

        // Assert
        isLandscape.Should().BeFalse();
    }

    [Fact]
    public void IsPortrait_WithPortraitImage_ShouldReturnTrue()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024, 1080, 1920, "jpg");

        // Act
        var isPortrait = image.IsPortrait();

        // Assert
        isPortrait.Should().BeTrue();
    }

    [Fact]
    public void IsPortrait_WithLandscapeImage_ShouldReturnFalse()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024L, 1920, 1080, "jpg");

        // Act
        var isPortrait = image.IsPortrait();

        // Assert
        isPortrait.Should().BeFalse();
    }

    [Fact]
    public void IsSquare_WithSquareImage_ShouldReturnTrue()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024, 1024, 1024, "jpg");

        // Act
        var isSquare = image.IsSquare();

        // Assert
        isSquare.Should().BeTrue();
    }

    [Fact]
    public void IsSquare_WithNonSquareImage_ShouldReturnFalse()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024L, 1920, 1080, "jpg");

        // Act
        var isSquare = image.IsSquare();

        // Assert
        isSquare.Should().BeFalse();
    }

    [Fact]
    public void GetResolution_ShouldReturnCorrectResolution()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024L, 1920, 1080, "jpg");

        // Act
        var resolution = image.GetResolution();

        // Assert
        resolution.Should().Be("1920x1080");
    }

    [Fact]
    public void IsHighResolution_WithHighResolutionImage_ShouldReturnTrue()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024L, 1920, 1080, "jpg");

        // Act
        var isHighResolution = image.IsHighResolution();

        // Assert
        isHighResolution.Should().BeTrue();
    }

    [Fact]
    public void IsHighResolution_WithLowResolutionImage_ShouldReturnFalse()
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024, 800, 600, "jpg");

        // Act
        var isHighResolution = image.IsHighResolution();

        // Assert
        isHighResolution.Should().BeFalse();
    }

    [Fact]
    public void IsLargeFile_WithLargeFile_ShouldReturnTrue()
    {
        // Arrange
        var largeFileSize = 11 * 1024 * 1024; // 11MB
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", largeFileSize, 1920, 1080, "jpg");

        // Act
        var isLargeFile = image.IsLargeFile();

        // Assert
        isLargeFile.Should().BeTrue();
    }

    [Fact]
    public void IsLargeFile_WithSmallFile_ShouldReturnFalse()
    {
        // Arrange
        var smallFileSize = 5 * 1024 * 1024; // 5MB
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", smallFileSize, 1920, 1080, "jpg");

        // Act
        var isLargeFile = image.IsLargeFile();

        // Assert
        isLargeFile.Should().BeFalse();
    }

    [Theory]
    [InlineData("jpg", true)]
    [InlineData("jpeg", true)]
    [InlineData("png", true)]
    [InlineData("gif", true)]
    [InlineData("bmp", true)]
    [InlineData("webp", true)]
    [InlineData("tiff", true)]
    [InlineData("txt", false)]
    [InlineData("doc", false)]
    [InlineData("pdf", false)]
    public void IsSupportedFormat_WithVariousFormats_ShouldReturnCorrectResult(string format, bool expected)
    {
        // Arrange
        var image = new Image(Guid.NewGuid(), "test.jpg", "/test/path/test.jpg", 1024, 1920, 1080, format);

        // Act
        var isSupported = image.IsSupportedFormat();

        // Assert
        isSupported.Should().Be(expected);
    }
}
