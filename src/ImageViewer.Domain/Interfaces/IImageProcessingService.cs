using ImageViewer.Domain.Entities;
using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Image processing service interface
/// </summary>
public interface IImageProcessingService
{
    Task<ImageMetadata> ExtractMetadataAsync(string imagePath, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateThumbnailAsync(string imagePath, int width, int height, CancellationToken cancellationToken = default);
    Task<byte[]> ResizeImageAsync(string imagePath, int width, int height, int quality = 95, CancellationToken cancellationToken = default);
    Task<byte[]> ConvertImageFormatAsync(string imagePath, string targetFormat, int quality = 95, CancellationToken cancellationToken = default);
    Task<bool> IsImageFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<string[]> GetSupportedFormatsAsync(CancellationToken cancellationToken = default);
    Task<ImageDimensions> GetImageDimensionsAsync(string imagePath, CancellationToken cancellationToken = default);
    Task<ImageDimensions> GetImageDimensionsFromBytesAsync(byte[] imageData, CancellationToken cancellationToken = default);
    Task<long> GetImageFileSizeAsync(string imagePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Image dimensions value object
/// </summary>
public record ImageDimensions(int Width, int Height);
