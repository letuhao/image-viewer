using ImageViewer.Domain.Entities;
using MongoDB.Bson;

namespace ImageViewer.Application.Services;

/// <summary>
/// Image service interface
/// </summary>
public interface IImageService
{
    Task<Image?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default);
    Task<Image?> GetByCollectionIdAndFilenameAsync(ObjectId collectionId, string filename, CancellationToken cancellationToken = default);
    Task<IEnumerable<Image>> GetByCollectionIdAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Image>> GetByFormatAsync(string format, CancellationToken cancellationToken = default);
    Task<IEnumerable<Image>> GetBySizeRangeAsync(int minWidth, int minHeight, CancellationToken cancellationToken = default);
    Task<IEnumerable<Image>> GetLargeImagesAsync(long minSizeBytes, CancellationToken cancellationToken = default);
    Task<IEnumerable<Image>> GetHighResolutionImagesAsync(int minWidth, int minHeight, CancellationToken cancellationToken = default);
    
    Task<Image?> GetRandomImageAsync(CancellationToken cancellationToken = default);
    Task<Image?> GetRandomImageByCollectionAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
    Task<Image?> GetNextImageAsync(ObjectId currentImageId, CancellationToken cancellationToken = default);
    Task<Image?> GetPreviousImageAsync(ObjectId currentImageId, CancellationToken cancellationToken = default);
    
    Task<byte[]?> GetImageFileAsync(ObjectId id, CancellationToken cancellationToken = default);
    Task<byte[]?> GetThumbnailAsync(ObjectId id, int? width = null, int? height = null, CancellationToken cancellationToken = default);
    Task<ThumbnailInfo?> GetThumbnailInfoAsync(ObjectId id, int? width = null, int? height = null, CancellationToken cancellationToken = default);
    Task<byte[]?> GetCachedImageAsync(ObjectId id, int? width = null, int? height = null, CancellationToken cancellationToken = default);
    
    Task DeleteAsync(ObjectId id, CancellationToken cancellationToken = default);
    Task RestoreAsync(ObjectId id, CancellationToken cancellationToken = default);
    
    Task<long> GetTotalSizeByCollectionAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
    Task<int> GetCountByCollectionAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
    
    Task GenerateThumbnailAsync(ObjectId id, int width, int height, CancellationToken cancellationToken = default);
    Task GenerateCacheAsync(ObjectId id, int width, int height, CancellationToken cancellationToken = default);
    Task CleanupExpiredThumbnailsAsync(CancellationToken cancellationToken = default);
}

