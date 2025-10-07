using SkiaSharp;
using System.IO;

namespace ImageViewer.Test.Shared.TestData;

/// <summary>
/// Utility class for generating test images for integration tests
/// </summary>
public static class TestImageGenerator
{
    private static readonly string TestImagesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestImages");

    /// <summary>
    /// Ensures the test images directory exists
    /// </summary>
    public static void EnsureTestImagesDirectory()
    {
        if (!Directory.Exists(TestImagesDirectory))
        {
            Directory.CreateDirectory(TestImagesDirectory);
        }
    }

    /// <summary>
    /// Creates a test JPEG image with specified dimensions
    /// </summary>
    public static string CreateTestJpeg(string fileName, int width = 800, int height = 600, int quality = 90)
    {
        EnsureTestImagesDirectory();
        var filePath = Path.Combine(TestImagesDirectory, fileName);

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        using var canvas = surface.Canvas;
        
        // Create a gradient background
        using var paint = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(width, height),
                new[] { SKColors.Blue, SKColors.Red, SKColors.Green },
                new[] { 0f, 0.5f, 1f },
                SKShaderTileMode.Clamp)
        };
        
        canvas.DrawRect(0, 0, width, height, paint);
        
        // Add some text
        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 24,
            IsAntialias = true
        };
        
        canvas.DrawText($"Test Image {width}x{height}", 50, height / 2, textPaint);
        
        // Save as JPEG
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        using var stream = File.Create(filePath);
        data.SaveTo(stream);

        return filePath;
    }

    /// <summary>
    /// Creates a test PNG image with specified dimensions
    /// </summary>
    public static string CreateTestPng(string fileName, int width = 800, int height = 600)
    {
        EnsureTestImagesDirectory();
        var filePath = Path.Combine(TestImagesDirectory, fileName);

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        using var canvas = surface.Canvas;
        
        // Create a solid color background
        using var paint = new SKPaint
        {
            Color = SKColors.Purple
        };
        
        canvas.DrawRect(0, 0, width, height, paint);
        
        // Add some shapes
        using var circlePaint = new SKPaint
        {
            Color = SKColors.Yellow,
            IsAntialias = true
        };
        
        canvas.DrawCircle(width / 2, height / 2, Math.Min(width, height) / 4, circlePaint);
        
        // Save as PNG
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(filePath);
        data.SaveTo(stream);

        return filePath;
    }

    /// <summary>
    /// Creates a test WebP image with specified dimensions
    /// </summary>
    public static string CreateTestWebP(string fileName, int width = 800, int height = 600, int quality = 90)
    {
        EnsureTestImagesDirectory();
        var filePath = Path.Combine(TestImagesDirectory, fileName);

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        using var canvas = surface.Canvas;
        
        // Create a pattern background
        using var paint = new SKPaint
        {
            Color = SKColors.Orange
        };
        
        canvas.DrawRect(0, 0, width, height, paint);
        
        // Add diagonal lines
        using var linePaint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 2,
            IsAntialias = true
        };
        
        for (int i = 0; i < width; i += 20)
        {
            canvas.DrawLine(i, 0, i, height, linePaint);
        }
        
        // Save as WebP
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Webp, quality);
        using var stream = File.Create(filePath);
        data.SaveTo(stream);

        return filePath;
    }

    /// <summary>
    /// Creates multiple test images for batch processing tests
    /// </summary>
    public static string[] CreateMultipleTestImages(int count = 5)
    {
        var imagePaths = new string[count];
        
        for (int i = 0; i < count; i++)
        {
            var fileName = $"test{i + 1}.jpg";
            imagePaths[i] = CreateTestJpeg(fileName, 400 + i * 100, 300 + i * 50);
        }
        
        return imagePaths;
    }

    /// <summary>
    /// Creates a large test image for memory testing
    /// </summary>
    public static string CreateLargeTestImage(string fileName = "large-test-image.jpg", int width = 4000, int height = 3000)
    {
        return CreateTestJpeg(fileName, width, height);
    }

    /// <summary>
    /// Creates a high-quality test image
    /// </summary>
    public static string CreateHighQualityTestImage(string fileName = "high-quality-test.jpg", int width = 1920, int height = 1080)
    {
        return CreateTestJpeg(fileName, width, height, 95);
    }

    /// <summary>
    /// Cleans up test images
    /// </summary>
    public static void CleanupTestImages()
    {
        if (Directory.Exists(TestImagesDirectory))
        {
            try
            {
                Directory.Delete(TestImagesDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Gets the path to a test image file
    /// </summary>
    public static string GetTestImagePath(string fileName)
    {
        return Path.Combine(TestImagesDirectory, fileName);
    }
}
