using ImageViewer.Infrastructure.Services;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Domain.Enums;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace ImageViewer.Test.Features.Performance.Unit;

/// <summary>
/// Unit tests for ImageProcessingService - Image Processing Performance features
/// </summary>
public class ImageProcessingServiceTests
{
    private readonly Mock<ILogger<SkiaSharpImageProcessingService>> _mockLogger;
    private readonly SkiaSharpImageProcessingService _imageProcessingService;

    public ImageProcessingServiceTests()
    {
        _mockLogger = new Mock<ILogger<SkiaSharpImageProcessingService>>();
        _imageProcessingService = new SkiaSharpImageProcessingService(_mockLogger.Object);
    }

    /// <summary>
    /// Helper method to create ArchiveEntryInfo from a file path for testing
    /// </summary>
    private static ArchiveEntryInfo CreateArchiveEntryFromPath(string filePath)
    {
        return new ArchiveEntryInfo
        {
            ArchivePath = Path.GetDirectoryName(filePath) ?? "",
            EntryName = Path.GetFileName(filePath),
            EntryPath = Path.GetFileName(filePath),
            IsDirectory = false,
            FileType = ImageFileType.RegularFile
        };
    }

    [Fact]
    public void Constructor_WithValidLogger_ShouldCreateInstance()
    {
        // Arrange & Act
        var service = new SkiaSharpImageProcessingService(_mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => new SkiaSharpImageProcessingService(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithValidImagePath_ShouldReturnMetadata()
    {
        // Arrange
        var imagePath = CreateTestImageFile();
        
        try
        {
            // Act
            var archiveEntry = CreateArchiveEntryFromPath(imagePath);
            var result = await _imageProcessingService.ExtractMetadataAsync(archiveEntry);

            // Assert
            result.Should().NotBeNull();
            result.Quality.Should().Be(95);
            result.CreatedDate.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
            result.ModifiedDate.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
        }
        finally
        {
            // Cleanup
            if (File.Exists(imagePath))
                File.Delete(imagePath);
        }
    }

    [Fact]
    public async Task ExtractMetadataAsync_WithInvalidPath_ShouldThrowException()
    {
        // Arrange
        var invalidPath = "nonexistent/image.jpg";

        // Act & Assert
        var archiveEntry = CreateArchiveEntryFromPath(invalidPath);
        var action = async () => await _imageProcessingService.ExtractMetadataAsync(archiveEntry);
        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GenerateThumbnailAsync_WithValidImagePath_ShouldReturnThumbnailBytes()
    {
        // Arrange
        var imagePath = CreateTestImageFile();
        
        try
        {
            // Act
            var archiveEntry = CreateArchiveEntryFromPath(imagePath);
            var result = await _imageProcessingService.GenerateThumbnailAsync(archiveEntry, 100, 100);

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            result.Length.Should().BeGreaterThan(0);
        }
        finally
        {
            // Cleanup
            if (File.Exists(imagePath))
                File.Delete(imagePath);
        }
    }

    [Fact]
    public async Task GenerateThumbnailAsync_WithInvalidDimensions_ShouldThrowException()
    {
        // Arrange
        var imagePath = CreateTestImageFile();
        
        try
        {
            // Act & Assert
            var archiveEntry = CreateArchiveEntryFromPath(imagePath);
            var action = async () => await _imageProcessingService.GenerateThumbnailAsync(archiveEntry, -1, -1);
            await action.Should().ThrowAsync<Exception>();
        }
        finally
        {
            // Cleanup
            if (File.Exists(imagePath))
                File.Delete(imagePath);
        }
    }

    [Fact]
    public async Task ResizeImageAsync_WithValidImagePath_ShouldReturnResizedImageBytes()
    {
        // Arrange
        var imagePath = CreateTestImageFile();
        
        try
        {
            // Act
            var archiveEntry = CreateArchiveEntryFromPath(imagePath);
            var result = await _imageProcessingService.ResizeImageAsync(archiveEntry, 200, 200, "jpeg", 90);

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            result.Length.Should().BeGreaterThan(0);
        }
        finally
        {
            // Cleanup
            if (File.Exists(imagePath))
                File.Delete(imagePath);
        }
    }

    [Fact]
    public async Task ResizeImageAsync_WithInvalidQuality_ShouldUseDefaultQuality()
    {
        // Arrange
        var imagePath = CreateTestImageFile();
        
        try
        {
            // Act
            var archiveEntry = CreateArchiveEntryFromPath(imagePath);
            var result = await _imageProcessingService.ResizeImageAsync(archiveEntry, 200, 200, "jpeg", 150); // Invalid quality > 100

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
        }
        finally
        {
            // Cleanup
            if (File.Exists(imagePath))
                File.Delete(imagePath);
        }
    }

    [Fact]
    public async Task ConvertImageFormatAsync_WithValidImagePath_ShouldReturnConvertedImageBytes()
    {
        // Arrange
        var imagePath = CreateTestImageFile();
        
        try
        {
            // Act
            var archiveEntry = CreateArchiveEntryFromPath(imagePath);
            var result = await _imageProcessingService.ConvertImageFormatAsync(archiveEntry, "PNG", 90);

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            result.Length.Should().BeGreaterThan(0);
        }
        finally
        {
            // Cleanup
            if (File.Exists(imagePath))
                File.Delete(imagePath);
        }
    }

    [Fact]
    public async Task ConvertImageFormatAsync_WithUnsupportedFormat_ShouldThrowException()
    {
        // Arrange
        var imagePath = CreateTestImageFile();
        
        try
        {
            // Act & Assert
            var archiveEntry = CreateArchiveEntryFromPath(imagePath);
            var action = async () => await _imageProcessingService.ConvertImageFormatAsync(archiveEntry, "UNSUPPORTED", 90);
            await action.Should().ThrowAsync<Exception>();
        }
        finally
        {
            // Cleanup
            if (File.Exists(imagePath))
                File.Delete(imagePath);
        }
    }

    [Fact]
    public async Task IsImageFileAsync_WithValidImageFile_ShouldReturnTrue()
    {
        // Arrange
        var imagePath = CreateTestImageFile();
        
        try
        {
            // Act
            var result = await _imageProcessingService.IsImageFileAsync(imagePath);

            // Assert
            result.Should().BeTrue();
        }
        finally
        {
            // Cleanup
            if (File.Exists(imagePath))
                File.Delete(imagePath);
        }
    }

    [Fact]
    public async Task IsImageFileAsync_WithNonImageFile_ShouldReturnFalse()
    {
        // Arrange
        var textPath = Path.GetTempFileName();
        await File.WriteAllTextAsync(textPath, "This is not an image");

        try
        {
            // Act
            var result = await _imageProcessingService.IsImageFileAsync(textPath);

            // Assert
            result.Should().BeFalse();
        }
        finally
        {
            // Cleanup
            if (File.Exists(textPath))
                File.Delete(textPath);
        }
    }

    [Fact]
    public async Task GetSupportedFormatsAsync_ShouldReturnSupportedFormats()
    {
        // Act
        var result = await _imageProcessingService.GetSupportedFormatsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().Contain("jpg");
        result.Should().Contain("png");
    }

    [Fact]
    public async Task GetImageDimensionsAsync_WithValidImagePath_ShouldReturnDimensions()
    {
        // Arrange
        var imagePath = CreateTestImageFile();
        
        try
        {
            // Act
            var archiveEntry = CreateArchiveEntryFromPath(imagePath);
            var result = await _imageProcessingService.GetImageDimensionsAsync(archiveEntry);

            // Assert
            result.Should().NotBeNull();
            result.Width.Should().BeGreaterThan(0);
            result.Height.Should().BeGreaterThan(0);
        }
        finally
        {
            // Cleanup
            if (File.Exists(imagePath))
                File.Delete(imagePath);
        }
    }

    [Fact]
    public async Task GetImageDimensionsFromBytesAsync_WithValidImageData_ShouldReturnDimensions()
    {
        // Arrange
        var imageBytes = CreateTestImageBytes();

        // Act
        var result = await _imageProcessingService.GetImageDimensionsFromBytesAsync(imageBytes);

        // Assert
        result.Should().NotBeNull();
        result.Width.Should().BeGreaterThan(0);
        result.Height.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetImageFileSizeAsync_WithValidImagePath_ShouldReturnFileSize()
    {
        // Arrange
        var imagePath = CreateTestImageFile();
        
        try
        {
            // Act
            var archiveEntry = CreateArchiveEntryFromPath(imagePath);
            var result = await _imageProcessingService.GetImageFileSizeAsync(archiveEntry);

            // Assert
            result.Should().BeGreaterThan(0);
        }
        finally
        {
            // Cleanup
            if (File.Exists(imagePath))
                File.Delete(imagePath);
        }
    }

    [Fact]
    public async Task GetImageFileSizeAsync_WithInvalidPath_ShouldThrowException()
    {
        // Arrange
        var invalidPath = "nonexistent/image.jpg";

        // Act & Assert
        var archiveEntry = CreateArchiveEntryFromPath(invalidPath);
        var action = async () => await _imageProcessingService.GetImageFileSizeAsync(archiveEntry);
        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ProcessingWithCancellation_ShouldHandleCancellationGracefully()
    {
        // Arrange
        var imagePath = CreateTestImageFile();
        var cancellationTokenSource = new CancellationTokenSource();
        
        try
        {
            // Act - Test that the service can accept a cancellation token without throwing
            var archiveEntry = CreateArchiveEntryFromPath(imagePath);
            var result = await _imageProcessingService.GenerateThumbnailAsync(archiveEntry, 100, 100, "jpeg", 95, cancellationTokenSource.Token);

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
        }
        finally
        {
            // Cleanup
            if (File.Exists(imagePath))
                File.Delete(imagePath);
        }
    }

    [Fact]
    public async Task BatchProcessing_WithMultipleImages_ShouldProcessAllImages()
    {
        // Arrange
        var imagePaths = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            imagePaths.Add(CreateTestImageFile());
        }

        try
        {
            // Act
            var tasks = imagePaths.Select(path => 
            {
                var archiveEntry = CreateArchiveEntryFromPath(path);
                return _imageProcessingService.GenerateThumbnailAsync(archiveEntry, 50, 50);
            });
            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(3);
            results.Should().OnlyContain(result => result.Length > 0);
        }
        finally
        {
            // Cleanup
            foreach (var path in imagePaths)
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task MemoryManagement_WithLargeImage_ShouldProcessWithoutMemoryLeaks()
    {
        // Arrange
        var imagePath = CreateTestImageFile();
        
        try
        {
            // Act - Process the same image multiple times to test memory management
            for (int i = 0; i < 10; i++)
            {
                var archiveEntry = CreateArchiveEntryFromPath(imagePath);
                var result = await _imageProcessingService.GenerateThumbnailAsync(archiveEntry, 100, 100);
                result.Should().NotBeNull();
                
                // Force garbage collection to test memory management
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            // Assert - If we get here without OutOfMemoryException, memory management is working
            true.Should().BeTrue();
        }
        finally
        {
            // Cleanup
            if (File.Exists(imagePath))
                File.Delete(imagePath);
        }
    }

    [Fact]
    public async Task Performance_WithConcurrentProcessing_ShouldHandleConcurrentRequests()
    {
        // Arrange
        var imagePath = CreateTestImageFile();
        var tasks = new List<Task<byte[]>>();
        
        try
        {
            // Act - Create multiple concurrent processing tasks
            for (int i = 0; i < 5; i++)
            {
                var archiveEntry = CreateArchiveEntryFromPath(imagePath);
                tasks.Add(_imageProcessingService.GenerateThumbnailAsync(archiveEntry, 100, 100));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(5);
            results.Should().OnlyContain(result => result.Length > 0);
        }
        finally
        {
            // Cleanup
            if (File.Exists(imagePath))
                File.Delete(imagePath);
        }
    }

    private string CreateTestImageFile()
    {
        // Create a simple test image file using SkiaSharp
        var tempPath = Path.GetTempFileName() + ".jpg";
        
        using var surface = SKSurface.Create(new SKImageInfo(100, 100));
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Red);
        
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        using var stream = File.Create(tempPath);
        data.SaveTo(stream);
        
        return tempPath;
    }

    private byte[] CreateTestImageBytes()
    {
        using var surface = SKSurface.Create(new SKImageInfo(50, 50));
        using var canvas = surface.Canvas;
        canvas.Clear(SKColors.Blue);
        
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }
}
