using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Image processing service interface
/// </summary>
public interface IImageProcessingService
{
    Task<ImageMetadata> ExtractMetadataAsync(ArchiveEntryInfo archiveEntry, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateThumbnailAsync(ArchiveEntryInfo archiveEntry, int width, int height, string format = "jpeg", int quality = 95, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateThumbnailFromBytesAsync(byte[] imageData, int width, int height, string format = "jpeg", int quality = 90, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateCacheAsync(ArchiveEntryInfo archiveEntry, int width, int height, string format = "jpeg", int quality = 95, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateCacheFromBytesAsync(byte[] imageData, int width, int height, string format = "jpeg", int quality = 90, CancellationToken cancellationToken = default);
    Task<byte[]> ResizeImageAsync(ArchiveEntryInfo archiveEntry, int width, int height, string format = "jpeg", int quality = 95, CancellationToken cancellationToken = default);
    Task<byte[]> ResizeImageFromBytesAsync(byte[] imageData, int width, int height, string format = "jpeg", int quality = 95, CancellationToken cancellationToken = default);
    Task<byte[]> ConvertImageFormatAsync(ArchiveEntryInfo archiveEntry, string targetFormat, int quality = 95, CancellationToken cancellationToken = default);
    Task<bool> IsImageFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<string[]> GetSupportedFormatsAsync(CancellationToken cancellationToken = default);
    Task<ImageDimensions> GetImageDimensionsAsync(ArchiveEntryInfo archiveEntry, CancellationToken cancellationToken = default);
    Task<ImageDimensions> GetImageDimensionsFromBytesAsync(byte[] imageData, CancellationToken cancellationToken = default);
    Task<long> GetImageFileSizeAsync(ArchiveEntryInfo archiveEntry, CancellationToken cancellationToken = default);
}

/// <summary>
/// Image dimensions value object
/// </summary>
public record ImageDimensions(int Width, int Height);
