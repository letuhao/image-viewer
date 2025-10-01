using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// File scanner service interface
/// </summary>
public interface IFileScannerService
{
    Task<IEnumerable<Image>> ScanFolderAsync(string folderPath, CancellationToken cancellationToken = default);
    Task<IEnumerable<Image>> ScanArchiveAsync(string archivePath, CollectionType archiveType, CancellationToken cancellationToken = default);
    Task<bool> IsValidCollectionPathAsync(string path, CancellationToken cancellationToken = default);
    Task<CollectionType> DetectCollectionTypeAsync(string path, CancellationToken cancellationToken = default);
    Task<long> GetCollectionSizeAsync(string path, CollectionType type, CancellationToken cancellationToken = default);
    Task<int> GetImageCountAsync(string path, CollectionType type, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetSupportedArchiveFormatsAsync(CancellationToken cancellationToken = default);
}
