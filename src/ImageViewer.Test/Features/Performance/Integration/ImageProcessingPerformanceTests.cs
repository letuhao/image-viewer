using ImageViewer.Domain.Interfaces;
using ImageViewer.Test.Shared.Fixtures;
using ImageViewer.Test.Shared.TestData;
using System.Diagnostics;

namespace ImageViewer.Test.Features.Performance.Integration;

/// <summary>
/// Integration tests for Image Processing Performance - End-to-end image processing performance scenarios
/// </summary>
[Collection("Integration")]
public class ImageProcessingPerformanceTests : IClassFixture<BasicPerformanceIntegrationTestFixture>
{
    private readonly BasicPerformanceIntegrationTestFixture _fixture;
    private readonly IImageProcessingService _imageProcessingService;

    public ImageProcessingPerformanceTests(BasicPerformanceIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _imageProcessingService = _fixture.GetService<IImageProcessingService>();
    }

    [Fact]
    public async Task ImageProcessingPerformance_ResizePerformance_ShouldResizeEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var imagePath = TestImageGenerator.GetTestImagePath("test-image.jpg");
        var targetWidth = 800;
        var targetHeight = 600;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _imageProcessingService.ResizeImageAsync(imagePath, targetWidth, targetHeight);

        // Assert
        stopwatch.Stop();
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
    }

    [Fact]
    public async Task ImageProcessingPerformance_FormatConversionPerformance_ShouldConvertEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var sourcePath = TestImageGenerator.GetTestImagePath("test-image.jpg");
        var targetFormat = "png";
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _imageProcessingService.ConvertImageFormatAsync(sourcePath, targetFormat);

        // Assert
        stopwatch.Stop();
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000); // Should complete within 3 seconds
    }

    [Fact]
    public async Task ImageProcessingPerformance_ThumbnailGenerationPerformance_ShouldGenerateThumbnailsEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var imagePath = TestImageGenerator.GetTestImagePath("test-image.jpg");
        var thumbnailWidth = 200;
        var thumbnailHeight = 200;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _imageProcessingService.GenerateThumbnailAsync(imagePath, thumbnailWidth, thumbnailHeight);

        // Assert
        stopwatch.Stop();
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Should complete within 2 seconds
    }

    [Fact]
    public async Task ImageProcessingPerformance_MetadataExtraction_ShouldExtractMetadataEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var imagePath = TestImageGenerator.GetTestImagePath("test-image.jpg");
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _imageProcessingService.ExtractMetadataAsync(imagePath);

        // Assert
        stopwatch.Stop();
        result.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }

    [Fact]
    public async Task ImageProcessingPerformance_ImageValidation_ShouldValidateImagesEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var imagePath = TestImageGenerator.GetTestImagePath("test-image.jpg");
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _imageProcessingService.IsImageFileAsync(imagePath);

        // Assert
        stopwatch.Stop();
        result.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500); // Should complete within 500ms
    }

    [Fact]
    public async Task ImageProcessingPerformance_Concurrency_ShouldHandleConcurrentProcessing()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var imagePaths = new[] { 
            TestImageGenerator.GetTestImagePath("test1.jpg"), 
            TestImageGenerator.GetTestImagePath("test2.jpg"), 
            TestImageGenerator.GetTestImagePath("test3.jpg"), 
            TestImageGenerator.GetTestImagePath("test4.jpg"), 
            TestImageGenerator.GetTestImagePath("test5.jpg") 
        };
        var targetWidth = 400;
        var targetHeight = 300;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = imagePaths.Select(path => 
            _imageProcessingService.ResizeImageAsync(path, targetWidth, targetHeight));
        var results = await Task.WhenAll(tasks);

        // Assert
        stopwatch.Stop();
        results.Should().NotBeNull();
        results.Should().HaveCount(imagePaths.Length);
        results.All(r => r.Length > 0).Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(8000); // Should complete within 8 seconds
    }

    [Fact]
    public async Task ImageProcessingPerformance_SupportedFormats_ShouldReturnSupportedFormats()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _imageProcessingService.GetSupportedFormatsAsync();

        // Assert
        stopwatch.Stop();
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100); // Should complete within 100ms
    }

    [Fact]
    public async Task ImageProcessingPerformance_ImageDimensions_ShouldGetDimensionsEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var imagePath = TestImageGenerator.GetTestImagePath("test-image.jpg");
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _imageProcessingService.GetImageDimensionsAsync(imagePath);

        // Assert
        stopwatch.Stop();
        result.Should().NotBeNull();
        result.Width.Should().BeGreaterThan(0);
        result.Height.Should().BeGreaterThan(0);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }

    [Fact]
    public async Task ImageProcessingPerformance_FileSize_ShouldGetFileSizeEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var imagePath = TestImageGenerator.GetTestImagePath("test-image.jpg");
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _imageProcessingService.GetImageFileSizeAsync(imagePath);

        // Assert
        stopwatch.Stop();
        result.Should().BeGreaterThan(0);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500); // Should complete within 500ms
    }
}