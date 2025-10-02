using Microsoft.Extensions.Logging;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Interfaces;
using System.IO.Compression;

namespace ImageViewer.Infrastructure.Services;

/// <summary>
/// Compressed file service implementation
/// </summary>
public class CompressedFileService : ICompressedFileService
{
    private readonly IImageProcessingService _imageProcessingService;
    private readonly ILogger<CompressedFileService> _logger;

    // Supported compressed file extensions
    private readonly string[] _supportedExtensions = 
    {
        ".zip", ".cbz", ".cbr", ".7z", ".rar", ".tar", ".tar.gz", ".tar.bz2"
    };

    // Supported image extensions
    private readonly string[] _imageExtensions = 
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".svg"
    };

    public CompressedFileService(
        IImageProcessingService imageProcessingService,
        ILogger<CompressedFileService> logger)
    {
        _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<bool> IsCompressedFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!LongPathHandler.PathExistsSafe(filePath))
            {
                return Task.FromResult(false);
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return Task.FromResult(_supportedExtensions.Contains(extension));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if file is compressed: {FilePath}", filePath);
            return Task.FromResult(false);
        }
    }

    public string[] GetSupportedExtensions()
    {
        return _supportedExtensions;
    }

    public async Task<IEnumerable<CompressedFileImage>> ExtractImagesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Extracting images from compressed file: {FilePath}", filePath);

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var images = new List<CompressedFileImage>();

            switch (extension)
            {
                case ".zip":
                case ".cbz":
                    images = await ExtractFromZipAsync(filePath, cancellationToken);
                    break;
                case ".7z":
                    images = await ExtractFrom7ZipAsync(filePath, cancellationToken);
                    break;
                case ".rar":
                case ".cbr":
                    images = await ExtractFromRarAsync(filePath, cancellationToken);
                    break;
                case ".tar":
                case ".tar.gz":
                case ".tar.bz2":
                    images = await ExtractFromTarAsync(filePath, cancellationToken);
                    break;
                default:
                    _logger.LogWarning("Unsupported compressed file format: {Extension}", extension);
                    break;
            }

            _logger.LogInformation("Extracted {Count} images from {FilePath}", images.Count, filePath);
            return images;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting images from compressed file: {FilePath}", filePath);
            return Enumerable.Empty<CompressedFileImage>();
        }
    }

    public async Task<CompressedFileInfo?> GetFileInfoAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!LongPathHandler.PathExistsSafe(filePath))
            {
                return null;
            }

            var fileInfo = new FileInfo(filePath);
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (!_supportedExtensions.Contains(extension))
            {
                return null;
            }

            var info = new CompressedFileInfo
            {
                Filename = Path.GetFileName(filePath),
                SizeBytes = fileInfo.Length,
                LastModified = fileInfo.LastWriteTime,
                Format = extension,
                SupportedFormats = _imageExtensions
            };

            // Get detailed information based on file type
            switch (extension)
            {
                case ".zip":
                case ".cbz":
                    await GetZipFileInfoAsync(filePath, info);
                    break;
                case ".7z":
                    await Get7ZipFileInfoAsync(filePath, info);
                    break;
                case ".rar":
                case ".cbr":
                    await GetRarFileInfoAsync(filePath, info);
                    break;
                case ".tar":
                case ".tar.gz":
                case ".tar.bz2":
                    await GetTarFileInfoAsync(filePath, info);
                    break;
            }

            return info;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file info for: {FilePath}", filePath);
            return null;
        }
    }

    public async Task<bool> ContainsImagesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var info = await GetFileInfoAsync(filePath, cancellationToken);
            return info?.ImageFiles > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if compressed file contains images: {FilePath}", filePath);
            return false;
        }
    }

    private async Task<List<CompressedFileImage>> ExtractFromZipAsync(string filePath, CancellationToken cancellationToken)
    {
        var images = new List<CompressedFileImage>();

        try
        {
            using var archive = ZipFile.OpenRead(filePath);
            
            foreach (var entry in archive.Entries)
            {
                if (IsImageFile(entry.Name))
                {
                    var image = await ExtractImageFromZipEntryAsync(entry, cancellationToken);
                    if (image != null)
                    {
                        images.Add(image);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting from ZIP file: {FilePath}", filePath);
        }

        return images;
    }

    private Task<List<CompressedFileImage>> ExtractFrom7ZipAsync(string filePath, CancellationToken cancellationToken)
    {
        var images = new List<CompressedFileImage>();
        
        // Note: 7-Zip extraction requires external library or process
        // For now, return empty list with warning
        _logger.LogWarning("7-Zip extraction not implemented yet for: {FilePath}", filePath);
        
        return Task.FromResult(images);
    }

    private Task<List<CompressedFileImage>> ExtractFromRarAsync(string filePath, CancellationToken cancellationToken)
    {
        var images = new List<CompressedFileImage>();
        
        // Note: RAR extraction requires external library or process
        // For now, return empty list with warning
        _logger.LogWarning("RAR extraction not implemented yet for: {FilePath}", filePath);
        
        return Task.FromResult(images);
    }

    private Task<List<CompressedFileImage>> ExtractFromTarAsync(string filePath, CancellationToken cancellationToken)
    {
        var images = new List<CompressedFileImage>();
        
        // Note: TAR extraction requires additional implementation
        // For now, return empty list with warning
        _logger.LogWarning("TAR extraction not implemented yet for: {FilePath}", filePath);
        
        return Task.FromResult(images);
    }

    private async Task<CompressedFileImage?> ExtractImageFromZipEntryAsync(ZipArchiveEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = entry.Open();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            var imageData = memoryStream.ToArray();

            // Get image dimensions from the actual bytes
            var dimensions = await _imageProcessingService.GetImageDimensionsFromBytesAsync(imageData, cancellationToken);
            
            return new CompressedFileImage
            {
                Filename = Path.GetFileName(entry.Name),
                RelativePath = entry.FullName,
                SizeBytes = entry.Length,
                LastModified = entry.LastWriteTime.DateTime,
                Data = imageData,
                Format = Path.GetExtension(entry.Name).ToLowerInvariant(),
                Width = dimensions.Width,
                Height = dimensions.Height
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting image from ZIP entry: {EntryName}", entry.FullName);
            return null;
        }
    }

    private Task GetZipFileInfoAsync(string filePath, CompressedFileInfo info)
    {
        try
        {
            using var archive = ZipFile.OpenRead(filePath);
            
            info.TotalFiles = archive.Entries.Count;
            info.ImageFiles = archive.Entries.Count(entry => IsImageFile(entry.Name));
            info.TotalImageSize = archive.Entries
                .Where(entry => IsImageFile(entry.Name))
                .Sum(entry => entry.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ZIP file info: {FilePath}", filePath);
        }
        
        return Task.CompletedTask;
    }

    private Task Get7ZipFileInfoAsync(string filePath, CompressedFileInfo info)
    {
        // Note: 7-Zip info requires external library or process
        _logger.LogWarning("7-Zip file info not implemented yet for: {FilePath}", filePath);
        return Task.CompletedTask;
    }

    private Task GetRarFileInfoAsync(string filePath, CompressedFileInfo info)
    {
        // Note: RAR info requires external library or process
        _logger.LogWarning("RAR file info not implemented yet for: {FilePath}", filePath);
        return Task.CompletedTask;
    }

    private Task GetTarFileInfoAsync(string filePath, CompressedFileInfo info)
    {
        // Note: TAR info requires additional implementation
        _logger.LogWarning("TAR file info not implemented yet for: {FilePath}", filePath);
        return Task.CompletedTask;
    }

    private bool IsImageFile(string filename)
    {
        var extension = Path.GetExtension(filename).ToLowerInvariant();
        return _imageExtensions.Contains(extension);
    }
}
