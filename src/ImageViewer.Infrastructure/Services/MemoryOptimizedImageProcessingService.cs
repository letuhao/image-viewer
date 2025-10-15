using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using System.Collections.Concurrent;
using System.Buffers;

namespace ImageViewer.Infrastructure.Services;

/// <summary>
/// High-performance memory-optimized image processing service
/// Processes images entirely in memory to maximize performance
/// </summary>
public class MemoryOptimizedImageProcessingService : IImageProcessingService
{
    private readonly ILogger<MemoryOptimizedImageProcessingService> _logger;
    private readonly MemoryOptimizationOptions _options;
    private readonly ConcurrentQueue<byte[]> _memoryPool;
    private readonly SemaphoreSlim _memorySemaphore;
    private long _totalMemoryUsage = 0;
    private readonly object _memoryLock = new object();

    public MemoryOptimizedImageProcessingService(
        ILogger<MemoryOptimizedImageProcessingService> logger,
        IOptions<MemoryOptimizationOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        
        // Initialize memory pool for efficient byte array reuse
        _memoryPool = new ConcurrentQueue<byte[]>();
        _memorySemaphore = new SemaphoreSlim(_options.MaxConcurrentProcessing, _options.MaxConcurrentProcessing);
        
        // Pre-allocate memory buffers
        InitializeMemoryPool();
        
        _logger.LogInformation("🚀 MemoryOptimizedImageProcessingService initialized with {MaxMemory}MB memory limit, {MaxConcurrent} concurrent processing",
            _options.MaxMemoryUsageMB, _options.MaxConcurrentProcessing);
    }

    public async Task<ImageMetadata> ExtractMetadataAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Extracting metadata from {ImagePath}", imagePath);

            // Read file into memory buffer
            var imageBytes = await ReadFileToMemoryAsync(imagePath, cancellationToken);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                throw new InvalidOperationException($"Failed to read image file: {imagePath}");
            }

            using var data = SKData.CreateCopy(imageBytes);
            using var codec = SKCodec.Create(data);
            
            if (codec == null)
            {
                throw new InvalidOperationException($"Failed to decode image: {imagePath}");
            }

            var info = codec.Info;
            var metadata = new ImageMetadata(
                quality: 95,
                colorSpace: info.ColorSpace?.ToString(),
                compression: "Unknown",
                createdDate: File.GetCreationTime(imagePath),
                modifiedDate: File.GetLastWriteTime(imagePath)
            );

            // Return buffer to pool
            ReturnBufferToPool(imageBytes);

            _logger.LogDebug("Successfully extracted metadata from {ImagePath}", imagePath);
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting metadata from {ImagePath}", imagePath);
            throw;
        }
    }

    public async Task<byte[]> GenerateThumbnailFromBytesAsync(byte[] imageData, int width, int height, string format = "jpeg", int quality = 90, CancellationToken cancellationToken = default)
    {
        await _memorySemaphore.WaitAsync(cancellationToken);
        
        try
        {
            _logger.LogDebug("🎨 Generating thumbnail from bytes: {Width}x{Height}, format {Format}, quality {Quality}", width, height, format, quality);

            // Check memory usage before processing
            if (!CanProcessInMemory(imageData.Length))
            {
                _logger.LogWarning("⚠️ Insufficient memory for processing {Size}MB image, forcing garbage collection", 
                    imageData.Length / 1024.0 / 1024.0);
                await ForceGarbageCollectionAsync();
                
                if (!CanProcessInMemory(imageData.Length))
                {
                    throw new OutOfMemoryException($"Insufficient memory to process {imageData.Length / 1024.0 / 1024.0:F2}MB image");
                }
            }

            // Track memory usage
            IncrementMemoryUsage(imageData.Length);

            try
            {
                using var data = SKData.CreateCopy(imageData);
                using var originalImage = SKImage.FromEncodedData(data);
                
                var originalInfo = originalImage.Info;
                var scaleX = (float)width / originalInfo.Width;
                var scaleY = (float)height / originalInfo.Height;
                var scale = Math.Min(scaleX, scaleY);

                var thumbnailWidth = (int)(originalInfo.Width * scale);
                var thumbnailHeight = (int)(originalInfo.Height * scale);

                // Use high-performance settings
                var imageInfo = new SKImageInfo(thumbnailWidth, thumbnailHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var surface = SKSurface.Create(imageInfo);
                using var canvas = surface.Canvas;
                
                // Optimized paint settings for speed
                using var paint = new SKPaint
                {
                    FilterQuality = SKFilterQuality.Medium, // Faster than High
                    IsAntialias = false, // Disable for speed
                    IsDither = false // Disable for speed
                };

                canvas.Clear(SKColors.White); // Faster than transparent
                canvas.DrawImage(originalImage,
                    new SKRect(0, 0, originalInfo.Width, originalInfo.Height),
                    new SKRect(0, 0, thumbnailWidth, thumbnailHeight),
                    paint);
                
                using var image = surface.Snapshot();
                var encodedFormat = ParseImageFormat(format);
                using var encoded = image.Encode(encodedFormat, quality);
                
                var result = encoded.ToArray();
                
                _logger.LogDebug("✅ Generated thumbnail: {Size} bytes, format {Format}", result.Length, format);
                return result;
            }
            finally
            {
                // Release memory usage
                DecrementMemoryUsage(imageData.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error generating thumbnail from bytes");
            throw;
        }
        finally
        {
            _memorySemaphore.Release();
        }
    }

    public async Task<byte[]> GenerateThumbnailAsync(string imagePath, int width, int height, string format = "jpeg", int quality = 95, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("🎨 Generating thumbnail from file: {ImagePath}, {Width}x{Height}", imagePath, width, height);

            // Read entire file into memory for processing
            var imageBytes = await ReadFileToMemoryAsync(imagePath, cancellationToken);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                throw new InvalidOperationException($"Failed to read image file: {imagePath}");
            }

            // Process in memory
            var result = await GenerateThumbnailFromBytesAsync(imageBytes, width, height, format, quality, cancellationToken);
            
            // Return buffer to pool
            ReturnBufferToPool(imageBytes);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error generating thumbnail from file {ImagePath}", imagePath);
            throw;
        }
    }

    public async Task<ImageDimensions?> GetImageDimensionsAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("📏 Getting image dimensions from {ImagePath}", imagePath);

            var imageBytes = await ReadFileToMemoryAsync(imagePath, cancellationToken);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return null;
            }

            using var data = SKData.CreateCopy(imageBytes);
            using var codec = SKCodec.Create(data);
            
            if (codec == null)
            {
                return null;
            }

            var info = codec.Info;
            var dimensions = new ImageDimensions(info.Width, info.Height);
            
            // Return buffer to pool
            ReturnBufferToPool(imageBytes);

            _logger.LogDebug("📏 Image dimensions: {Width}x{Height}", dimensions.Width, dimensions.Height);
            return dimensions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting image dimensions from {ImagePath}", imagePath);
            return null;
        }
    }

    /// <summary>
    /// Batch process multiple images in memory for maximum efficiency
    /// </summary>
    public async Task<List<ProcessedImageResult>> ProcessBatchInMemoryAsync(
        List<ImageProcessingRequest> requests, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🚀 Starting batch processing of {Count} images", requests.Count);

        var results = new List<ProcessedImageResult>();
        var tasks = new List<Task<ProcessedImageResult>>();

        // Process images in parallel (limited by semaphore)
        foreach (var request in requests)
        {
            tasks.Add(ProcessSingleImageInMemoryAsync(request, cancellationToken));
        }

        // Wait for all processing to complete
        var completedTasks = await Task.WhenAll(tasks);
        results.AddRange(completedTasks);

        // Force garbage collection after batch
        await ForceGarbageCollectionAsync();

        _logger.LogInformation("✅ Completed batch processing of {Count} images", results.Count);
        return results;
    }

    private async Task<ProcessedImageResult> ProcessSingleImageInMemoryAsync(
        ImageProcessingRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var imageBytes = await ReadFileToMemoryAsync(request.ImagePath, cancellationToken);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return new ProcessedImageResult
                {
                    ImageId = request.ImageId,
                    Success = false,
                    ErrorMessage = "Failed to read image file"
                };
            }

            var thumbnailData = await GenerateThumbnailFromBytesAsync(
                imageBytes, 
                request.Width, 
                request.Height, 
                request.Format, 
                request.Quality, 
                cancellationToken);

            // Return buffer to pool
            ReturnBufferToPool(imageBytes);

            return new ProcessedImageResult
            {
                ImageId = request.ImageId,
                Success = true,
                ThumbnailData = thumbnailData,
                ThumbnailSize = thumbnailData.Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing image {ImageId}", request.ImageId);
            return new ProcessedImageResult
            {
                ImageId = request.ImageId,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<byte[]> ReadFileToMemoryAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            // Get buffer from pool or allocate new one
            var buffer = GetBufferFromPool();
            
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 
                bufferSize: 65536, useAsync: true); // 64KB buffer for faster reads
            
            var totalBytesRead = 0;
            var bytesRead = 0;
            
            do
            {
                bytesRead = await fileStream.ReadAsync(buffer, totalBytesRead, buffer.Length - totalBytesRead, cancellationToken);
                totalBytesRead += bytesRead;
                
                // If buffer is full, expand it
                if (totalBytesRead >= buffer.Length && bytesRead > 0)
                {
                    var newBuffer = new byte[buffer.Length * 2];
                    Array.Copy(buffer, newBuffer, buffer.Length);
                    ReturnBufferToPool(buffer);
                    buffer = newBuffer;
                }
            } while (bytesRead > 0);

            // Trim buffer to actual size
            if (totalBytesRead < buffer.Length)
            {
                var trimmedBuffer = new byte[totalBytesRead];
                Array.Copy(buffer, trimmedBuffer, totalBytesRead);
                ReturnBufferToPool(buffer);
                return trimmedBuffer;
            }

            return buffer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error reading file {FilePath} to memory", filePath);
            throw;
        }
    }

    private void InitializeMemoryPool()
    {
        // Pre-allocate memory buffers for common image sizes
        for (int i = 0; i < _options.MemoryPoolSize; i++)
        {
            _memoryPool.Enqueue(new byte[_options.DefaultBufferSize]);
        }
        
        _logger.LogDebug("📦 Initialized memory pool with {Count} buffers of {Size}KB each", 
            _options.MemoryPoolSize, _options.DefaultBufferSize / 1024);
    }

    private byte[] GetBufferFromPool()
    {
        if (_memoryPool.TryDequeue(out var buffer))
        {
            return buffer;
        }
        
        // Pool exhausted, allocate new buffer
        return new byte[_options.DefaultBufferSize];
    }

    private void ReturnBufferToPool(byte[] buffer)
    {
        if (buffer != null && _memoryPool.Count < _options.MemoryPoolSize * 2)
        {
            Array.Clear(buffer, 0, buffer.Length); // Clear for security
            _memoryPool.Enqueue(buffer);
        }
    }

    private bool CanProcessInMemory(long imageSize)
    {
        lock (_memoryLock)
        {
            var currentUsageMB = _totalMemoryUsage / 1024.0 / 1024.0;
            var maxUsageMB = _options.MaxMemoryUsageMB;
            var projectedUsageMB = (currentUsageMB + imageSize / 1024.0 / 1024.0);
            
            return projectedUsageMB <= maxUsageMB * 0.9; // Use 90% of max to leave buffer
        }
    }

    private void IncrementMemoryUsage(long bytes)
    {
        lock (_memoryLock)
        {
            _totalMemoryUsage += bytes;
        }
    }

    private void DecrementMemoryUsage(long bytes)
    {
        lock (_memoryLock)
        {
            _totalMemoryUsage = Math.Max(0, _totalMemoryUsage - bytes);
        }
    }

    private async Task ForceGarbageCollectionAsync()
    {
        _logger.LogDebug("🗑️ Forcing garbage collection to free memory");
        
        await Task.Run(() =>
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        });
        
        // Reset memory usage tracking after GC
        lock (_memoryLock)
        {
            _totalMemoryUsage = GC.GetTotalMemory(false);
        }
    }

    private SKEncodedImageFormat ParseImageFormat(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "jpeg" or "jpg" => SKEncodedImageFormat.Jpeg,
            "png" => SKEncodedImageFormat.Png,
            "webp" => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Jpeg
        };
    }

    public async Task<byte[]> GenerateCacheAsync(string imagePath, int width, int height, string format = "jpeg", int quality = 95, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("🎨 Generating cache image from {ImagePath} ({Width}x{Height}, {Format}, Quality: {Quality})", 
                imagePath, width, height, format, quality);

            var imageBytes = await ReadFileToMemoryAsync(imagePath, cancellationToken);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return Array.Empty<byte>();
            }

            var result = await GenerateCacheFromBytesAsync(imageBytes, width, height, format, quality, cancellationToken);
            
            // Return buffer to pool
            ReturnBufferToPool(imageBytes);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error generating cache from {ImagePath}", imagePath);
            return Array.Empty<byte>();
        }
    }

    public async Task<byte[]> GenerateCacheFromBytesAsync(byte[] imageData, int width, int height, string format = "jpeg", int quality = 90, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("🎨 Generating cache image from bytes ({Width}x{Height}, {Format}, Quality: {Quality})", 
                width, height, format, quality);

            using var data = SKData.CreateCopy(imageData);
            using var codec = SKCodec.Create(data);
            
            if (codec == null)
            {
                _logger.LogWarning("⚠️ Failed to create codec from image data");
                return Array.Empty<byte>();
            }

            var info = codec.Info;
            
            // Calculate scale factor to maintain aspect ratio
            var scaleX = (float)width / info.Width;
            var scaleY = (float)height / info.Height;
            var scale = Math.Min(scaleX, scaleY);
            
            var scaledWidth = (int)(info.Width * scale);
            var scaledHeight = (int)(info.Height * scale);
            
            // Decode image
            using var originalBitmap = SKBitmap.Decode(codec);
            if (originalBitmap == null)
            {
                _logger.LogWarning("⚠️ Failed to decode image");
                return Array.Empty<byte>();
            }
            
            // Create scaled bitmap
            using var scaledBitmap = new SKBitmap(scaledWidth, scaledHeight);
            using var canvas = new SKCanvas(scaledBitmap);
            
            // Draw scaled image
            canvas.Clear(SKColors.White);
            canvas.DrawBitmap(originalBitmap, new SKRect(0, 0, scaledWidth, scaledHeight));
            
            // Encode to desired format
            var imageFormat = format.ToLowerInvariant() switch
            {
                "jpeg" or "jpg" => SKEncodedImageFormat.Jpeg,
                "png" => SKEncodedImageFormat.Png,
                "webp" => SKEncodedImageFormat.Webp,
                _ => SKEncodedImageFormat.Jpeg
            };
            
            using var encodedData = scaledBitmap.Encode(imageFormat, quality);
            if (encodedData == null)
            {
                _logger.LogWarning("⚠️ Failed to encode cache image");
                return Array.Empty<byte>();
            }
            
            var result = encodedData.ToArray();
            _logger.LogDebug("✅ Generated cache image: {Size} bytes", result.Length);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error generating cache from bytes");
            return Array.Empty<byte>();
        }
    }

    public async Task<byte[]> ResizeImageAsync(string imagePath, int width, int height, string format = "jpeg", int quality = 95, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("🔄 Resizing image from {ImagePath} ({Width}x{Height}, {Format}, Quality: {Quality})", 
                imagePath, width, height, format, quality);

            var imageBytes = await ReadFileToMemoryAsync(imagePath, cancellationToken);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return Array.Empty<byte>();
            }

            var result = await ResizeImageFromBytesAsync(imageBytes, width, height, format, quality, cancellationToken);
            
            // Return buffer to pool
            ReturnBufferToPool(imageBytes);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error resizing image from {ImagePath}", imagePath);
            return Array.Empty<byte>();
        }
    }

    public async Task<byte[]> ResizeImageFromBytesAsync(byte[] imageData, int width, int height, string format = "jpeg", int quality = 95, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("🔄 Resizing image from bytes ({Width}x{Height}, {Format}, Quality: {Quality})", 
                width, height, format, quality);

            using var data = SKData.CreateCopy(imageData);
            using var codec = SKCodec.Create(data);
            
            if (codec == null)
            {
                _logger.LogWarning("⚠️ Failed to create codec from image data");
                return Array.Empty<byte>();
            }

            // Decode image
            using var originalBitmap = SKBitmap.Decode(codec);
            if (originalBitmap == null)
            {
                _logger.LogWarning("⚠️ Failed to decode image");
                return Array.Empty<byte>();
            }
            
            // Create resized bitmap
            using var resizedBitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(resizedBitmap);
            
            // Draw resized image
            canvas.Clear(SKColors.White);
            canvas.DrawBitmap(originalBitmap, new SKRect(0, 0, width, height));
            
            // Encode to desired format
            var imageFormat = format.ToLowerInvariant() switch
            {
                "jpeg" or "jpg" => SKEncodedImageFormat.Jpeg,
                "png" => SKEncodedImageFormat.Png,
                "webp" => SKEncodedImageFormat.Webp,
                _ => SKEncodedImageFormat.Jpeg
            };
            
            using var encodedData = resizedBitmap.Encode(imageFormat, quality);
            if (encodedData == null)
            {
                _logger.LogWarning("⚠️ Failed to encode resized image");
                return Array.Empty<byte>();
            }
            
            var result = encodedData.ToArray();
            _logger.LogDebug("✅ Resized image: {Size} bytes", result.Length);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error resizing image from bytes");
            return Array.Empty<byte>();
        }
    }

    public async Task<byte[]> ConvertImageFormatAsync(string imagePath, string targetFormat, int quality = 95, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("🔄 Converting image format from {ImagePath} to {TargetFormat}", 
                imagePath, targetFormat);

            var imageBytes = await ReadFileToMemoryAsync(imagePath, cancellationToken);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return Array.Empty<byte>();
            }

            using var data = SKData.CreateCopy(imageBytes);
            using var codec = SKCodec.Create(data);
            
            if (codec == null)
            {
                _logger.LogWarning("⚠️ Failed to create codec from image data");
                return Array.Empty<byte>();
            }

            // Decode image
            using var originalBitmap = SKBitmap.Decode(codec);
            if (originalBitmap == null)
            {
                _logger.LogWarning("⚠️ Failed to decode image");
                return Array.Empty<byte>();
            }
            
            // Encode to target format
            var imageFormat = targetFormat.ToLowerInvariant() switch
            {
                "jpeg" or "jpg" => SKEncodedImageFormat.Jpeg,
                "png" => SKEncodedImageFormat.Png,
                "webp" => SKEncodedImageFormat.Webp,
                _ => SKEncodedImageFormat.Jpeg
            };
            
            using var encodedData = originalBitmap.Encode(imageFormat, quality);
            if (encodedData == null)
            {
                _logger.LogWarning("⚠️ Failed to encode converted image");
                return Array.Empty<byte>();
            }
            
            var result = encodedData.ToArray();
            _logger.LogDebug("✅ Converted image: {Size} bytes", result.Length);
            
            // Return buffer to pool
            ReturnBufferToPool(imageBytes);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error converting image format from {ImagePath}", imagePath);
            return Array.Empty<byte>();
        }
    }

    public async Task<bool> IsImageFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif" };
            
            return supportedExtensions.Contains(extension);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error checking if file is image: {FilePath}", filePath);
            return false;
        }
    }

    public async Task<string[]> GetSupportedFormatsAsync(CancellationToken cancellationToken = default)
    {
        return new[] { "JPEG", "PNG", "WEBP" };
    }

    public async Task<ImageDimensions> GetImageDimensionsFromBytesAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        try
        {
            using var data = SKData.CreateCopy(imageData);
            using var codec = SKCodec.Create(data);
            
            if (codec == null)
            {
                return new ImageDimensions(0, 0);
            }

            var info = codec.Info;
            var dimensions = new ImageDimensions(info.Width, info.Height);
            
            _logger.LogDebug("📏 Image dimensions from bytes: {Width}x{Height}", dimensions.Width, dimensions.Height);
            return dimensions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting image dimensions from bytes");
            return new ImageDimensions(0, 0);
        }
    }

    public async Task<long> GetImageFileSizeAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(imagePath))
            {
                return 0;
            }

            var fileInfo = new FileInfo(imagePath);
            return fileInfo.Length;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting image file size from {ImagePath}", imagePath);
            return 0;
        }
    }

    public void Dispose()
    {
        _memorySemaphore?.Dispose();
        _logger.LogInformation("🧹 MemoryOptimizedImageProcessingService disposed");
    }
}

/// <summary>
/// Configuration options for memory optimization
/// </summary>
public class MemoryOptimizationOptions
{
    public int MaxMemoryUsageMB { get; set; } = 2048; // 2GB default
    public int MaxConcurrentProcessing { get; set; } = Environment.ProcessorCount;
    public int MemoryPoolSize { get; set; } = 50;
    public int DefaultBufferSize { get; set; } = 1024 * 1024; // 1MB default buffer
}

/// <summary>
/// Request for processing a single image
/// </summary>
public class ImageProcessingRequest
{
    public string ImageId { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string Format { get; set; } = "jpeg";
    public int Quality { get; set; } = 90;
}

/// <summary>
/// Result of processing a single image
/// </summary>
public class ProcessedImageResult
{
    public string ImageId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public byte[]? ThumbnailData { get; set; }
    public long ThumbnailSize { get; set; }
    public string? ErrorMessage { get; set; }
}
