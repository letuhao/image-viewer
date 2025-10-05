using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for DownloadQualityOption entity
/// </summary>
public interface IDownloadQualityOptionRepository : IRepository<DownloadQualityOption>
{
    Task<IEnumerable<DownloadQualityOption>> GetByQualityAsync(string quality, CancellationToken cancellationToken = default);
    Task<IEnumerable<DownloadQualityOption>> GetByCollectionIdAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<DownloadQualityOption>> GetActiveOptionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DownloadQualityOption>> GetByBandwidthAsync(long maxBandwidth, CancellationToken cancellationToken = default);
}
