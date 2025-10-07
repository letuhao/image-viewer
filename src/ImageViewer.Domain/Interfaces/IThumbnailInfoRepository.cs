using ImageViewer.Domain.Entities;
using MongoDB.Bson;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for ThumbnailInfo entities
/// </summary>
public interface IThumbnailInfoRepository : IRepository<ThumbnailInfo>
{
    Task<ThumbnailInfo?> GetByImageIdAsync(ObjectId imageId, CancellationToken cancellationToken = default);
    Task<ThumbnailInfo?> GetByImageIdAndDimensionsAsync(ObjectId imageId, int width, int height, CancellationToken cancellationToken = default);
    Task<IEnumerable<ThumbnailInfo>> GetByImageIdsAsync(IEnumerable<ObjectId> imageIds, CancellationToken cancellationToken = default);
    Task<IEnumerable<ThumbnailInfo>> GetExpiredThumbnailsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ThumbnailInfo>> GetStaleThumbnailsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);
    Task<long> GetTotalSizeAsync(CancellationToken cancellationToken = default);
    Task<int> GetCountByImageIdAsync(ObjectId imageId, CancellationToken cancellationToken = default);
    Task DeleteByImageIdAsync(ObjectId imageId, CancellationToken cancellationToken = default);
    Task DeleteExpiredThumbnailsAsync(CancellationToken cancellationToken = default);
}
