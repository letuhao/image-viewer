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

    public Task<ImageMetadata> ExtractMetadataAsync(ArchiveEntryInfo archiveEntry, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Extracting metadata from {ImagePath}", archiveEntry.GetPhysicalFileFullPath());

            using var stream = File.OpenRead(archiveEntry.GetPhysicalFileFullPath());
            using var codec = SKCodec.Create(stream);
            using var image = SKImage.FromEncodedData(stream);

            var info = codec.Info;
            var metadata = new ImageMetadata(
                quality: 95,
                colorSpace: info.ColorSpace?.ToString(),
                compression: "Unknown", // SkiaSharp doesn't expose compression info directly
                createdDate: File.GetCreationTime(archiveEntry.GetPhysicalFileFullPath()),
                modifiedDate: File.GetLastWriteTime(archiveEntry.GetPhysicalFileFullPath())
            );

            _logger.LogDebug("Successfully extracted metadata from {ImagePath}", archiveEntry.GetPhysicalFileFullPath());
            return Task.FromResult(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting metadata from {ImagePath}", archiveEntry.GetPhysicalFileFullPath());
            throw;
        }
    }

    public Task<byte[]> GenerateThumbnailFromBytesAsync(byte[] imageData, int width, int height, string format = "jpeg", int quality = 90, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating thumbnail from bytes with size {Width}x{Height}, format {Format}, quality {Quality}", width, height, format, quality);

            using var data = SKData.CreateCopy(imageData);
            using var originalImage = SKImage.FromEncodedData(data);
            
            var originalInfo = originalImage.Info;
            var scaleX = (float)width / originalInfo.Width;
            var scaleY = (float)height / originalInfo.Height;
            var scale = Math.Min(scaleX, scaleY);

            var thumbnailWidth = (int)(originalInfo.Width * scale);
            var thumbnailHeight = (int)(originalInfo.Height * scale);

            var imageInfo = new SKImageInfo(thumbnailWidth, thumbnailHeight);
            using var surface = SKSurface.Create(imageInfo);
            using var canvas = surface.Canvas;
            using var paint = new SKPaint
            {
                FilterQuality = SKFilterQuality.High,
                IsAntialias = true
            };

            canvas.Clear(SKColors.Transparent);
            // DrawImage(image, source rect, dest rect, paint) - CORRECT ORDER!
            canvas.DrawImage(originalImage,
                new SKRect(0, 0, originalInfo.Width, originalInfo.Height), // Source: entire original
                new SKRect(0, 0, thumbnailWidth, thumbnailHeight), // Dest: thumbnail size
                paint);
            
            using var image = surface.Snapshot();
            var encodedFormat = ParseImageFormat(format);
            using var encoded = image.Encode(encodedFormat, quality);
            
            var result = encoded.ToArray();
            _logger.LogDebug("Generated thumbnail from bytes: {Size} bytes, format {Format}", result.Length, format);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail from bytes");
            throw;
        }
    }

    public Task<byte[]> GenerateThumbnailAsync(ArchiveEntryInfo archiveEntry, int width, int height, string format = "jpeg", int quality = 95, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating thumbnail for {ImagePath} with size {Width}x{Height}, format {Format}, quality {Quality}", archiveEntry.GetPhysicalFileFullPath(), width, height, format, quality);

            using var stream = File.OpenRead(archiveEntry.GetPhysicalFileFullPath());
            using var originalImage = SKImage.FromEncodedData(stream);
            
            var originalInfo = originalImage.Info;
            var scaleX = (float)width / originalInfo.Width;
            var scaleY = (float)height / originalInfo.Height;
            var scale = Math.Min(scaleX, scaleY);

            var thumbnailWidth = (int)(originalInfo.Width * scale);
            var thumbnailHeight = (int)(originalInfo.Height * scale);

            using var thumbnailBitmap = new SKBitmap(thumbnailWidth, thumbnailHeight);
            using var canvas = new SKCanvas(thumbnailBitmap);
            
            // Use high-quality interpolation for better thumbnail quality
            using var paint = new SKPaint
            {
                FilterQuality = SKFilterQuality.High,
                IsAntialias = true
            };
            
            canvas.Clear(SKColors.White);
            // DrawImage(image, source rect, dest rect, paint) - CORRECT ORDER!
            canvas.DrawImage(originalImage,
                new SKRect(0, 0, originalInfo.Width, originalInfo.Height), // Source: entire original
                new SKRect(0, 0, thumbnailWidth, thumbnailHeight), // Dest: thumbnail size
                paint);

            using var thumbnailImage = SKImage.FromBitmap(thumbnailBitmap);
            var encodedFormat = ParseImageFormat(format);
            using var thumbnailStream = thumbnailImage.Encode(encodedFormat, quality);
            
            var result = thumbnailStream.ToArray();
            
            _logger.LogDebug("Successfully generated thumbnail for {ImagePath}, format {Format}", archiveEntry.GetPhysicalFileFullPath(), format);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail for {ImagePath}", archiveEntry.GetPhysicalFileFullPath());
            throw;
        }
    }

    public Task<byte[]> ResizeImageFromBytesAsync(byte[] imageData, int width, int height, string format = "jpeg", int quality = 95, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Resizing image from bytes to {Width}x{Height} with format {Format}, quality {Quality}", width, height, format, quality);

            using var data = SKData.CreateCopy(imageData);
            using var originalImage = SKImage.FromEncodedData(data);
            
            if (originalImage == null)
            {
                _logger.LogError("Failed to decode image from byte array. Data may be corrupted or unsupported format. Size: {Size} bytes", imageData.Length);
                throw new InvalidOperationException($"Failed to decode image from byte array (size: {imageData.Length} bytes)");
            }
            
            var originalInfo = originalImage.Info;
            var scaleX = (float)width / originalInfo.Width;
            var scaleY = (float)height / originalInfo.Height;
            var scale = Math.Min(scaleX, scaleY);

            var newWidth = (int)(originalInfo.Width * scale);
            var newHeight = (int)(originalInfo.Height * scale);

            var imageInfo = new SKImageInfo(newWidth, newHeight);
            using var surface = SKSurface.Create(imageInfo);
            using var canvas = surface.Canvas;
            using var paint = new SKPaint
            {
                FilterQuality = SKFilterQuality.High,
                IsAntialias = true
            };

            canvas.Clear(SKColors.White);
            // DrawImage(image, source rect, dest rect, paint) - CORRECT ORDER!
            canvas.DrawImage(originalImage, 
                new SKRect(0, 0, originalInfo.Width, originalInfo.Height), // Source: entire original
                new SKRect(0, 0, newWidth, newHeight), // Dest: resized canvas
                paint);
            
            using var image = surface.Snapshot();
            var encodedFormat = ParseImageFormat(format);
            using var encoded = image.Encode(encodedFormat, quality);
            
            var result = encoded.ToArray();
            _logger.LogDebug("Resized image from bytes: {Size} bytes, format {Format}", result.Length, format);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resizing image from bytes");
            throw;
        }
    }

    public Task<byte[]> ResizeImageAsync(ArchiveEntryInfo archiveEntry, int width, int height, string format = "jpeg", int quality = 95, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Resizing image {ImagePath} to {Width}x{Height} with format {Format}, quality {Quality}", archiveEntry.GetPhysicalFileFullPath(), width, height, format, quality);

            using var stream = File.OpenRead(archiveEntry.GetPhysicalFileFullPath());
            using var originalImage = SKImage.FromEncodedData(stream);
            
            if (originalImage == null)
            {
                _logger.LogError("Failed to decode image: {ImagePath}. File may be corrupted or unsupported format.", archiveEntry.GetPhysicalFileFullPath());
                throw new InvalidOperationException($"Failed to decode image: {archiveEntry.GetPhysicalFileFullPath()}");
            }
            
            var originalInfo = originalImage.Info;
            var scaleX = (float)width / originalInfo.Width;
            var scaleY = (float)height / originalInfo.Height;
            var scale = Math.Min(scaleX, scaleY);

            var newWidth = (int)(originalInfo.Width * scale);
            var newHeight = (int)(originalInfo.Height * scale);

            using var resizedBitmap = new SKBitmap(newWidth, newHeight);
            using var canvas = new SKCanvas(resizedBitmap);
            using var paint = new SKPaint
            {
                FilterQuality = SKFilterQuality.High,
                IsAntialias = true
            };
            
            canvas.Clear(SKColors.White);
            // DrawImage(image, source rect, dest rect, paint) - CORRECT ORDER!
            canvas.DrawImage(originalImage, 
                new SKRect(0, 0, originalInfo.Width, originalInfo.Height), // Source: entire original
                new SKRect(0, 0, newWidth, newHeight), // Dest: resized canvas
                paint);

            using var resizedImage = SKImage.FromBitmap(resizedBitmap);
            var encodedFormat = ParseImageFormat(format);
            using var resizedStream = resizedImage.Encode(encodedFormat, quality);
            
            var result = resizedStream.ToArray();
            
            _logger.LogDebug("Successfully resized image {ImagePath}, format {Format}", archiveEntry.GetPhysicalFileFullPath(), format);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resizing image {ImagePath}", archiveEntry.GetPhysicalFileFullPath());
            throw;
        }
    }

    public Task<byte[]> ConvertImageFormatAsync(ArchiveEntryInfo archiveEntry, string targetFormat, int quality = 95, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Converting image {ImagePath} to format {TargetFormat}", archiveEntry.GetPhysicalFileFullPath(), targetFormat);

            using var stream = File.OpenRead(archiveEntry.GetPhysicalFileFullPath());
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
            
            _logger.LogDebug("Successfully converted image {ImagePath} to {TargetFormat}", archiveEntry.GetPhysicalFileFullPath(), targetFormat);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting image {ImagePath} to {TargetFormat}", archiveEntry.GetPhysicalFileFullPath(), targetFormat);
            throw;
        }
    }

    public Task<bool> IsImageFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking if {FilePath} is an image file", filePath);

            if (!File.Exists(filePath))
            {
                return Task.FromResult(false);
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif" };
            
            if (!supportedExtensions.Contains(extension))
            {
                return Task.FromResult(false);
            }

            // Try to decode the image to verify it's valid
            using var stream = File.OpenRead(filePath);
            using var codec = SKCodec.Create(stream);
            
            var result = codec != null;
            _logger.LogDebug("File {FilePath} is {Result} an image file", filePath, result ? "" : "not");
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking if {FilePath} is an image file", filePath);
            return Task.FromResult(false);
        }
    }

    public Task<string[]> GetSupportedFormatsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new[] { "jpg", "jpeg", "png", "gif", "bmp", "webp", "tiff" });
    }

    public Task<ImageDimensions> GetImageDimensionsAsync(ArchiveEntryInfo archiveEntry, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting dimensions for {ImagePath}", archiveEntry.GetPhysicalFileFullPath());

            using var stream = File.OpenRead(archiveEntry.GetPhysicalFileFullPath());
            using var codec = SKCodec.Create(stream);
            
            var info = codec.Info;
            var dimensions = new ImageDimensions(info.Width, info.Height);
            
            _logger.LogDebug("Image {ImagePath} dimensions: {Width}x{Height}", archiveEntry.GetPhysicalFileFullPath(), dimensions.Width, dimensions.Height);
            return Task.FromResult(dimensions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dimensions for {ImagePath}", archiveEntry.GetPhysicalFileFullPath());
            throw;
        }
    }

    public Task<ImageDimensions> GetImageDimensionsFromBytesAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        try
        {
            using var data = SKData.CreateCopy(imageData);
            using var codec = SKCodec.Create(data);
            var info = codec.Info;
            return Task.FromResult(new ImageDimensions(info.Width, info.Height));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dimensions from image bytes");
            throw;
        }
    }

    public Task<long> GetImageFileSizeAsync(ArchiveEntryInfo archiveEntry, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting file size for {ImagePath}", archiveEntry.GetPhysicalFileFullPath());

            var fileInfo = new FileInfo(archiveEntry.GetPhysicalFileFullPath());
            var size = fileInfo.Length;
            
            _logger.LogDebug("Image {ImagePath} file size: {Size} bytes", archiveEntry.GetPhysicalFileFullPath(), size);
            return Task.FromResult(size);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file size for {ImagePath}", archiveEntry.GetPhysicalFileFullPath());
            throw;
        }
    }

    public Task<byte[]> GenerateCacheAsync(ArchiveEntryInfo archiveEntry, int width, int height, string format = "jpeg", int quality = 95, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("🎨 Generating cache image from {ImagePath} ({Width}x{Height}, {Format}, Quality: {Quality})",
                archiveEntry.GetPhysicalFileFullPath(), width, height, format, quality);

            using var stream = File.OpenRead(archiveEntry.GetPhysicalFileFullPath());
            using var originalBitmap = SKBitmap.Decode(stream);
            
            if (originalBitmap == null)
            {
                _logger.LogWarning("⚠️ Failed to decode image from {ImagePath}", archiveEntry.GetPhysicalFileFullPath());
                return Task.FromResult(Array.Empty<byte>());
            }

            // Calculate scale factor to maintain aspect ratio
            var scaleX = (float)width / originalBitmap.Width;
            var scaleY = (float)height / originalBitmap.Height;
            var scale = Math.Min(scaleX, scaleY);
            
            var scaledWidth = (int)(originalBitmap.Width * scale);
            var scaledHeight = (int)(originalBitmap.Height * scale);
            
            // Create scaled bitmap
            using var scaledBitmap = new SKBitmap(scaledWidth, scaledHeight);
            using var canvas = new SKCanvas(scaledBitmap);
            
            // Draw scaled image
            canvas.Clear(SKColors.White);
            canvas.DrawBitmap(originalBitmap, new SKRect(0, 0, scaledWidth, scaledHeight));
            
            // Encode to desired format
            var imageFormat = ParseImageFormat(format);
            using var encodedData = scaledBitmap.Encode(imageFormat, quality);
            
            if (encodedData == null)
            {
                _logger.LogWarning("⚠️ Failed to encode cache image");
                return Task.FromResult(Array.Empty<byte>());
            }
            
            var result = encodedData.ToArray();
            _logger.LogDebug("✅ Generated cache image: {Size} bytes", result.Length);
            
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error generating cache from {ImagePath}", archiveEntry.GetPhysicalFileFullPath());
            return Task.FromResult(Array.Empty<byte>());
        }
    }

    public Task<byte[]> GenerateCacheFromBytesAsync(byte[] imageData, int width, int height, string format = "jpeg", int quality = 90, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("🎨 Generating cache image from bytes ({Width}x{Height}, {Format}, Quality: {Quality})", 
                width, height, format, quality);

            using var data = SKData.CreateCopy(imageData);
            using var originalBitmap = SKBitmap.Decode(data);
            
            if (originalBitmap == null)
            {
                _logger.LogWarning("⚠️ Failed to decode image from bytes");
                return Task.FromResult(Array.Empty<byte>());
            }

            // Calculate scale factor to maintain aspect ratio
            var scaleX = (float)width / originalBitmap.Width;
            var scaleY = (float)height / originalBitmap.Height;
            var scale = Math.Min(scaleX, scaleY);
            
            var scaledWidth = (int)(originalBitmap.Width * scale);
            var scaledHeight = (int)(originalBitmap.Height * scale);
            
            // Create scaled bitmap
            using var scaledBitmap = new SKBitmap(scaledWidth, scaledHeight);
            using var canvas = new SKCanvas(scaledBitmap);
            
            // Draw scaled image
            canvas.Clear(SKColors.White);
            canvas.DrawBitmap(originalBitmap, new SKRect(0, 0, scaledWidth, scaledHeight));
            
            // Encode to desired format
            var imageFormat = ParseImageFormat(format);
            using var encodedData = scaledBitmap.Encode(imageFormat, quality);
            
            if (encodedData == null)
            {
                _logger.LogWarning("⚠️ Failed to encode cache image");
                return Task.FromResult(Array.Empty<byte>());
            }
            
            var result = encodedData.ToArray();
            _logger.LogDebug("✅ Generated cache image: {Size} bytes", result.Length);
            
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error generating cache from bytes");
            return Task.FromResult(Array.Empty<byte>());
        }
    }

    /// <summary>
    /// Parse image format string to SKEncodedImageFormat enum
    /// </summary>
    private SKEncodedImageFormat ParseImageFormat(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "jpg" or "jpeg" => SKEncodedImageFormat.Jpeg,
            "png" => SKEncodedImageFormat.Png,
            "webp" => SKEncodedImageFormat.Webp,
            "bmp" => SKEncodedImageFormat.Bmp,
            _ => SKEncodedImageFormat.Jpeg // Default to JPEG
        };
    }
}
