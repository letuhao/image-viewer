using Microsoft.Extensions.Logging;
using SkiaSharp;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Infrastructure.Services;

/// <summary>
/// SkiaSharp-based image processing service
/// </summary>
public class SkiaSharpImageProcessingService : IImageProcessingService
{
    private readonly ILogger<SkiaSharpImageProcessingService> _logger;

    public SkiaSharpImageProcessingService(ILogger<SkiaSharpImageProcessingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ImageMetadata> ExtractMetadataAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Extracting metadata from {ImagePath}", imagePath);

            using var stream = File.OpenRead(imagePath);
            using var codec = SKCodec.Create(stream);
            using var image = SKImage.FromEncodedData(stream);

            var info = codec.Info;
            var metadata = new ImageMetadata(
                quality: 95,
                colorSpace: info.ColorSpace?.ToString(),
                compression: "Unknown", // SkiaSharp doesn't expose compression info directly
                createdDate: File.GetCreationTime(imagePath),
                modifiedDate: File.GetLastWriteTime(imagePath)
            );

            _logger.LogDebug("Successfully extracted metadata from {ImagePath}", imagePath);
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting metadata from {ImagePath}", imagePath);
            throw;
        }
    }

    public async Task<byte[]> GenerateThumbnailAsync(string imagePath, int width, int height, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating thumbnail for {ImagePath} with size {Width}x{Height}", imagePath, width, height);

            using var stream = File.OpenRead(imagePath);
            using var originalImage = SKImage.FromEncodedData(stream);
            
            var originalInfo = originalImage.Info;
            var scaleX = (float)width / originalInfo.Width;
            var scaleY = (float)height / originalInfo.Height;
            var scale = Math.Min(scaleX, scaleY);

            var thumbnailWidth = (int)(originalInfo.Width * scale);
            var thumbnailHeight = (int)(originalInfo.Height * scale);

            using var thumbnailBitmap = new SKBitmap(thumbnailWidth, thumbnailHeight);
            using var canvas = new SKCanvas(thumbnailBitmap);
            
            canvas.Clear(SKColors.White);
            canvas.DrawImage(originalImage, new SKRect(0, 0, thumbnailWidth, thumbnailHeight), new SKRect(0, 0, originalInfo.Width, originalInfo.Height));

            using var thumbnailImage = SKImage.FromBitmap(thumbnailBitmap);
            using var thumbnailStream = thumbnailImage.Encode(SKEncodedImageFormat.Jpeg, 90);
            
            var result = thumbnailStream.ToArray();
            
            _logger.LogDebug("Successfully generated thumbnail for {ImagePath}", imagePath);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail for {ImagePath}", imagePath);
            throw;
        }
    }

    public async Task<byte[]> ResizeImageAsync(string imagePath, int width, int height, int quality = 95, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Resizing image {ImagePath} to {Width}x{Height} with quality {Quality}", imagePath, width, height, quality);

            using var stream = File.OpenRead(imagePath);
            using var originalImage = SKImage.FromEncodedData(stream);
            
            var originalInfo = originalImage.Info;
            var scaleX = (float)width / originalInfo.Width;
            var scaleY = (float)height / originalInfo.Height;
            var scale = Math.Min(scaleX, scaleY);

            var newWidth = (int)(originalInfo.Width * scale);
            var newHeight = (int)(originalInfo.Height * scale);

            using var resizedBitmap = new SKBitmap(newWidth, newHeight);
            using var canvas = new SKCanvas(resizedBitmap);
            
            canvas.Clear(SKColors.White);
            canvas.DrawImage(originalImage, new SKRect(0, 0, newWidth, newHeight), new SKRect(0, 0, originalInfo.Width, originalInfo.Height));

            using var resizedImage = SKImage.FromBitmap(resizedBitmap);
            using var resizedStream = resizedImage.Encode(SKEncodedImageFormat.Jpeg, quality);
            
            var result = resizedStream.ToArray();
            
            _logger.LogDebug("Successfully resized image {ImagePath}", imagePath);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resizing image {ImagePath}", imagePath);
            throw;
        }
    }

    public async Task<byte[]> ConvertImageFormatAsync(string imagePath, string targetFormat, int quality = 95, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Converting image {ImagePath} to format {TargetFormat}", imagePath, targetFormat);

            using var stream = File.OpenRead(imagePath);
            using var originalImage = SKImage.FromEncodedData(stream);
            
            var format = targetFormat.ToLowerInvariant() switch
            {
                "jpg" or "jpeg" => SKEncodedImageFormat.Jpeg,
                "png" => SKEncodedImageFormat.Png,
                "webp" => SKEncodedImageFormat.Webp,
                "bmp" => SKEncodedImageFormat.Bmp,
                _ => throw new ArgumentException($"Unsupported target format: {targetFormat}")
            };

            using var convertedStream = originalImage.Encode(format, quality);
            var result = convertedStream.ToArray();
            
            _logger.LogDebug("Successfully converted image {ImagePath} to {TargetFormat}", imagePath, targetFormat);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting image {ImagePath} to {TargetFormat}", imagePath, targetFormat);
            throw;
        }
    }

    public async Task<bool> IsImageFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking if {FilePath} is an image file", filePath);

            if (!File.Exists(filePath))
            {
                return false;
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif" };
            
            if (!supportedExtensions.Contains(extension))
            {
                return false;
            }

            // Try to decode the image to verify it's valid
            using var stream = File.OpenRead(filePath);
            using var codec = SKCodec.Create(stream);
            
            var result = codec != null;
            _logger.LogDebug("File {FilePath} is {Result} an image file", filePath, result ? "" : "not");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking if {FilePath} is an image file", filePath);
            return false;
        }
    }

    public async Task<string[]> GetSupportedFormatsAsync(CancellationToken cancellationToken = default)
    {
        return new[] { "jpg", "jpeg", "png", "gif", "bmp", "webp", "tiff" };
    }

    public async Task<ImageDimensions> GetImageDimensionsAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting dimensions for {ImagePath}", imagePath);

            using var stream = File.OpenRead(imagePath);
            using var codec = SKCodec.Create(stream);
            
            var info = codec.Info;
            var dimensions = new ImageDimensions(info.Width, info.Height);
            
            _logger.LogDebug("Image {ImagePath} dimensions: {Width}x{Height}", imagePath, dimensions.Width, dimensions.Height);
            return dimensions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dimensions for {ImagePath}", imagePath);
            throw;
        }
    }

    public async Task<ImageDimensions> GetImageDimensionsFromBytesAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        try
        {
            using var data = SKData.CreateCopy(imageData);
            using var codec = SKCodec.Create(data);
            var info = codec.Info;
            return new ImageDimensions(info.Width, info.Height);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dimensions from image bytes");
            throw;
        }
    }

    public async Task<long> GetImageFileSizeAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting file size for {ImagePath}", imagePath);

            var fileInfo = new FileInfo(imagePath);
            var size = fileInfo.Length;
            
            _logger.LogDebug("Image {ImagePath} file size: {Size} bytes", imagePath, size);
            return size;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file size for {ImagePath}", imagePath);
            throw;
        }
    }
}
