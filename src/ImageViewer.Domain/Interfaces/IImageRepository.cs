using ImageViewer.Domain.Entities;
using MongoDB.Bson;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Image repository interface
/// </summary>
public interface IImageRepository : IRepository<Image>
{
    Task<IEnumerable<Image>> GetByCollectionIdAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
    Task<Image?> GetByCollectionIdAndFilenameAsync(ObjectId collectionId, string filename, CancellationToken cancellationToken = default);
    Task<IEnumerable<Image>> GetByFormatAsync(string format, CancellationToken cancellationToken = default);
    Task<IEnumerable<Image>> GetBySizeRangeAsync(int minWidth, int minHeight, CancellationToken cancellationToken = default);
    Task<IEnumerable<Image>> GetLargeImagesAsync(long minSizeBytes, CancellationToken cancellationToken = default);
    Task<IEnumerable<Image>> GetHighResolutionImagesAsync(int minWidth, int minHeight, CancellationToken cancellationToken = default);
    Task<Image?> GetRandomImageAsync(CancellationToken cancellationToken = default);
    Task<Image?> GetRandomImageByCollectionAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
    Task<Image?> GetNextImageAsync(ObjectId currentImageId, CancellationToken cancellationToken = default);
    Task<Image?> GetPreviousImageAsync(ObjectId currentImageId, CancellationToken cancellationToken = default);
    Task<long> GetTotalSizeByCollectionAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
    Task<int> GetCountByCollectionAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
}
