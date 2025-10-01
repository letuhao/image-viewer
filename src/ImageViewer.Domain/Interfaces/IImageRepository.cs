using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Image repository interface
/// </summary>
public interface IImageRepository : IRepository<Image>
{
    Task<IEnumerable<Image>> GetByCollectionIdAsync(Guid collectionId, CancellationToken cancellationToken = default);
    Task<Image?> GetByCollectionIdAndFilenameAsync(Guid collectionId, string filename, CancellationToken cancellationToken = default);
    Task<IEnumerable<Image>> GetByFormatAsync(string format, CancellationToken cancellationToken = default);
    Task<IEnumerable<Image>> GetBySizeRangeAsync(int minWidth, int minHeight, CancellationToken cancellationToken = default);
    Task<IEnumerable<Image>> GetLargeImagesAsync(long minSizeBytes, CancellationToken cancellationToken = default);
    Task<IEnumerable<Image>> GetHighResolutionImagesAsync(int minWidth, int minHeight, CancellationToken cancellationToken = default);
    Task<Image?> GetRandomImageAsync(CancellationToken cancellationToken = default);
    Task<Image?> GetRandomImageByCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default);
    Task<Image?> GetNextImageAsync(Guid currentImageId, CancellationToken cancellationToken = default);
    Task<Image?> GetPreviousImageAsync(Guid currentImageId, CancellationToken cancellationToken = default);
    Task<long> GetTotalSizeByCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default);
    Task<int> GetCountByCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default);
}
