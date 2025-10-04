using Microsoft.Extensions.Logging;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using MongoDB.Bson;
using System.IO.Compression;

namespace ImageViewer.Infrastructure.Services;

/// <summary>
/// File system scanner service
/// </summary>
public class FileScannerService : IFileScannerService
{
    private readonly IImageProcessingService _imageProcessingService;
    private readonly ILogger<FileScannerService> _logger;

    public FileScannerService(IImageProcessingService imageProcessingService, ILogger<FileScannerService> logger)
    {
        _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<Image>> ScanFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Scanning folder {FolderPath}", folderPath);

            if (!LongPathHandler.PathExistsSafe(folderPath))
            {
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
            }

            var images = new List<Image>();
            var supportedExtensions = await _imageProcessingService.GetSupportedFormatsAsync(cancellationToken);
            var searchPatterns = supportedExtensions.Select(ext => $"*.{ext}").ToArray();

            foreach (var pattern in searchPatterns)
            {
                var files = Directory.GetFiles(folderPath, pattern, SearchOption.AllDirectories);
                
                foreach (var file in files)
                {
                    try
                    {
                        var image = await CreateImageFromFileAsync(file, folderPath, cancellationToken);
                        if (image != null)
                        {
                            images.Add(image);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing file {FilePath}", file);
                    }
                }
            }

            _logger.LogInformation("Scanned folder {FolderPath}, found {ImageCount} images", folderPath, images.Count);
            return images;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning folder {FolderPath}", folderPath);
            throw;
        }
    }

    public async Task<IEnumerable<Image>> ScanArchiveAsync(string archivePath, CollectionType archiveType, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Scanning archive {ArchivePath} of type {ArchiveType}", archivePath, archiveType);

            if (!LongPathHandler.PathExistsSafe(archivePath))
            {
                throw new FileNotFoundException($"Archive not found: {archivePath}");
            }

            var images = new List<Image>();
            var supportedExtensions = await _imageProcessingService.GetSupportedFormatsAsync(cancellationToken);

            switch (archiveType)
            {
                case CollectionType.Zip:
                    images = await ScanZipArchiveAsync(archivePath, supportedExtensions, cancellationToken);
                    break;
                case CollectionType.SevenZip:
                case CollectionType.Rar:
                case CollectionType.Tar:
                    _logger.LogWarning("Archive type {ArchiveType} not yet supported", archiveType);
                    break;
                default:
                    throw new ArgumentException($"Unsupported archive type: {archiveType}");
            }

            _logger.LogInformation("Scanned archive {ArchivePath}, found {ImageCount} images", archivePath, images.Count);
            return images;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning archive {ArchivePath}", archivePath);
            throw;
        }
    }

    public Task<bool> IsValidCollectionPathAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating collection path {Path}", path);

            if (string.IsNullOrWhiteSpace(path))
            {
                return Task.FromResult(false);
            }

            // Check if it's a directory
            if (LongPathHandler.PathExistsSafe(path))
            {
                return Task.FromResult(true);
            }

            // Check if it's a supported archive file
            if (LongPathHandler.PathExistsSafe(path))
            {
                var extension = Path.GetExtension(path).ToLowerInvariant();
                var supportedArchives = new[] { ".zip", ".7z", ".rar", ".tar" };
                return Task.FromResult(supportedArchives.Contains(extension));
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating collection path {Path}", path);
            return Task.FromResult(false);
        }
    }

    public Task<CollectionType> DetectCollectionTypeAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Detecting collection type for path {Path}", path);

            if (LongPathHandler.PathExistsSafe(path))
            {
                return Task.FromResult(CollectionType.Folder);
            }

            if (LongPathHandler.PathExistsSafe(path))
            {
                var extension = Path.GetExtension(path).ToLowerInvariant();
                var result = extension switch
                {
                    ".zip" => CollectionType.Zip,
                    ".7z" => CollectionType.SevenZip,
                    ".rar" => CollectionType.Rar,
                    ".tar" => CollectionType.Tar,
                    _ => throw new ArgumentException($"Unsupported file extension: {extension}")
                };
                return Task.FromResult(result);
            }

            throw new ArgumentException($"Path does not exist: {path}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting collection type for path {Path}", path);
            throw;
        }
    }

    public Task<long> GetCollectionSizeAsync(string path, CollectionType type, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting collection size for path {Path} of type {Type}", path, type);

            if (type == CollectionType.Folder && Directory.Exists(path))
            {
                return Task.FromResult(GetDirectorySize(path));
            }

            if (LongPathHandler.PathExistsSafe(path))
            {
                var fileInfo = new FileInfo(path);
                return Task.FromResult(fileInfo.Length);
            }

            return Task.FromResult(0L);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection size for path {Path}", path);
            throw;
        }
    }

    public async Task<int> GetImageCountAsync(string path, CollectionType type, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting image count for path {Path} of type {Type}", path, type);

            IEnumerable<Image> images;
            if (type == CollectionType.Folder)
            {
                images = await ScanFolderAsync(path, cancellationToken);
            }
            else
            {
                images = await ScanArchiveAsync(path, type, cancellationToken);
            }

            return images.Count();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image count for path {Path}", path);
            throw;
        }
    }

    public Task<IEnumerable<string>> GetSupportedArchiveFormatsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<string>>(new[] { "zip", "7z", "rar", "tar" });
    }

    private async Task<List<Image>> ScanZipArchiveAsync(string archivePath, string[] supportedExtensions, CancellationToken cancellationToken)
    {
        var images = new List<Image>();
        
        try
        {
            using var archive = ZipFile.OpenRead(archivePath);
            
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.EndsWith("/")) continue; // Skip directories
                
                var extension = Path.GetExtension(entry.Name).ToLowerInvariant().TrimStart('.');
                if (!supportedExtensions.Contains(extension)) continue;

                try
                {
                    using var stream = entry.Open();
                    var tempPath = Path.GetTempFileName();
                    
                    using (var fileStream = File.Create(tempPath))
                    {
                        await stream.CopyToAsync(fileStream, cancellationToken);
                    }

                    var image = await CreateImageFromFileAsync(tempPath, archivePath, entry.FullName, cancellationToken);
                    if (image != null)
                    {
                        images.Add(image);
                    }

                    File.Delete(tempPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing archive entry {EntryName}", entry.FullName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning ZIP archive {ArchivePath}", archivePath);
            throw;
        }

        return images;
    }

    private async Task<Image?> CreateImageFromFileAsync(string filePath, string basePath, CancellationToken cancellationToken)
    {
        return await CreateImageFromFileAsync(filePath, basePath, null, cancellationToken);
    }

    private async Task<Image?> CreateImageFromFileAsync(string filePath, string basePath, string? customRelativePath, CancellationToken cancellationToken)
    {
        try
        {
            // Check if it's a valid image file
            var isValidImage = await _imageProcessingService.IsImageFileAsync(filePath, cancellationToken);
            if (!isValidImage)
            {
                return null;
            }

            // Get image dimensions
            var dimensions = await _imageProcessingService.GetImageDimensionsAsync(filePath, cancellationToken);
            
            // Get file size
            var fileSize = await _imageProcessingService.GetImageFileSizeAsync(filePath, cancellationToken);
            
            // Extract metadata
            var metadata = await _imageProcessingService.ExtractMetadataAsync(filePath, cancellationToken);
            
            // Get relative path
            var relativePath = customRelativePath ?? Path.GetRelativePath(basePath, filePath);
            
            // Create image entity (CollectionId will be set when added to collection)
            var image = new Image(
                ObjectId.Empty, // Will be set when added to collection
                Path.GetFileName(filePath),
                relativePath,
                fileSize,
                dimensions.Width,
                dimensions.Height,
                Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant()
            );
            
            // Set metadata if available
            if (metadata != null)
            {
                var metadataEntity = new ImageMetadataEntity(
                    image.Id,
                    metadata.Quality,
                    metadata.ColorSpace,
                    metadata.Compression,
                    metadata.CreatedDate,
                    metadata.ModifiedDate,
                    metadata.Camera,
                    metadata.Software,
                    System.Text.Json.JsonSerializer.Serialize(metadata.AdditionalMetadata ?? new Dictionary<string, object>())
                );
                image.SetMetadata(metadataEntity);
            }

            return image;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error creating image from file {FilePath}", filePath);
            return null;
        }
    }

    private static long GetDirectorySize(string directoryPath)
    {
        long size = 0;
        try
        {
            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                size += fileInfo.Length;
            }
        }
        catch (Exception)
        {
            // Ignore errors when calculating directory size
        }
        return size;
    }
}
