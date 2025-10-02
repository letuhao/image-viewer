using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using ImageViewer.Infrastructure.Services;
using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Tests.Infrastructure.Services;

public class SkiaSharpImageProcessingServiceTests
{
    private readonly Mock<ILogger<SkiaSharpImageProcessingService>> _loggerMock;
    private readonly SkiaSharpImageProcessingService _service;

    public SkiaSharpImageProcessingServiceTests()
    {
        _loggerMock = new Mock<ILogger<SkiaSharpImageProcessingService>>();
        _service = new SkiaSharpImageProcessingService(_loggerMock.Object);
    }

    [Fact]
    public void SkiaSharpImageProcessingService_ShouldBeCreated()
    {
        // Arrange & Act
        var service = new SkiaSharpImageProcessingService(_loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new SkiaSharpImageProcessingService(null!));
        
        exception.ParamName.Should().Be("logger");
    }

    [Fact]
    public async Task GetImageDimensionsAsync_WithNonExistentFile_ShouldThrowDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentPath = "C:\\NonExistent\\Image.jpg";

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => 
            _service.GetImageDimensionsAsync(nonExistentPath));
    }

    [Fact]
    public async Task GetImageDimensionsAsync_WithNullPath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.GetImageDimensionsAsync(null!));
    }

    [Fact]
    public async Task GetImageDimensionsAsync_WithEmptyPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GetImageDimensionsAsync(string.Empty));
    }

    [Fact]
    public async Task ResizeImageAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = "C:\\NonExistent\\Image.jpg";
        var width = 300;
        var height = 300;
        var quality = 90;

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => 
            _service.ResizeImageAsync(nonExistentPath, width, height, quality));
    }

    [Fact]
    public async Task ResizeImageAsync_WithNullPath_ShouldThrowArgumentNullException()
    {
        // Arrange
        var width = 300;
        var height = 300;
        var quality = 90;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.ResizeImageAsync(null!, width, height, quality));
    }

    [Fact]
    public async Task ResizeImageAsync_WithInvalidDimensions_ShouldThrowDirectoryNotFoundException()
    {
        // Arrange
        var path = "C:\\Test\\Image.jpg";
        var width = -1;
        var height = 300;
        var quality = 90;

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => 
            _service.ResizeImageAsync(path, width, height, quality));
    }

    [Fact]
    public async Task ResizeImageAsync_WithInvalidQuality_ShouldThrowDirectoryNotFoundException()
    {
        // Arrange
        var path = "C:\\Test\\Image.jpg";
        var width = 300;
        var height = 300;
        var quality = 150; // Invalid quality > 100

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => 
            _service.ResizeImageAsync(path, width, height, quality));
    }

    [Fact]
    public async Task GenerateThumbnailAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = "C:\\NonExistent\\Image.jpg";
        var width = 300;
        var height = 300;

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => 
            _service.GenerateThumbnailAsync(nonExistentPath, width, height));
    }

    [Fact]
    public async Task GenerateThumbnailAsync_WithNullPath_ShouldThrowArgumentNullException()
    {
        // Arrange
        var width = 300;
        var height = 300;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.GenerateThumbnailAsync(null!, width, height));
    }

    [Fact]
    public async Task GenerateThumbnailAsync_WithInvalidDimensions_ShouldThrowDirectoryNotFoundException()
    {
        // Arrange
        var path = "C:\\Test\\Image.jpg";
        var width = -1;
        var height = 300;

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => 
            _service.GenerateThumbnailAsync(path, width, height));
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = "C:\\NonExistent\\Image.jpg";

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => 
            _service.ExtractMetadataAsync(nonExistentPath));
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithNullPath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.ExtractMetadataAsync(null!));
    }
}
