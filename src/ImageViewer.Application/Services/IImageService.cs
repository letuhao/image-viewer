using ImageViewer.Domain.Entities;

namespace ImageViewer.Application.Services;

/// <summary>
/// Image service interface
/// </summary>
public interface IImageService
{
    Task<Image?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Image?> GetByCollectionIdAndFilenameAsync(Guid collectionId, string filename, CancellationToken cancellationToken = default);
    Task<IEnumerable<Image>> GetByCollectionIdAsync(Guid collectionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Image>> GetByFormatAsync(string format, CancellationToken cancellationToken = default);
    Task<IEnumerable<Image>> GetBySizeRangeAsync(int minWidth, int minHeight, CancellationToken cancellationToken = default);
    Task<IEnumerable<Image>> GetLargeImagesAsync(long minSizeBytes, CancellationToken cancellationToken = default);
    Task<IEnumerable<Image>> GetHighResolutionImagesAsync(int minWidth, int minHeight, CancellationToken cancellationToken = default);
    
    Task<Image?> GetRandomImageAsync(CancellationToken cancellationToken = default);
    Task<Image?> GetRandomImageByCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default);
    Task<Image?> GetNextImageAsync(Guid currentImageId, CancellationToken cancellationToken = default);
    Task<Image?> GetPreviousImageAsync(Guid currentImageId, CancellationToken cancellationToken = default);
    
    Task<byte[]?> GetImageFileAsync(Guid id, CancellationToken cancellationToken = default);
    Task<byte[]?> GetThumbnailAsync(Guid id, int? width = null, int? height = null, CancellationToken cancellationToken = default);
    Task<byte[]?> GetCachedImageAsync(Guid id, int? width = null, int? height = null, CancellationToken cancellationToken = default);
    
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task RestoreAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<long> GetTotalSizeByCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default);
    Task<int> GetCountByCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default);
    
    Task GenerateThumbnailAsync(Guid id, int width, int height, CancellationToken cancellationToken = default);
    Task GenerateCacheAsync(Guid id, int width, int height, CancellationToken cancellationToken = default);
}

