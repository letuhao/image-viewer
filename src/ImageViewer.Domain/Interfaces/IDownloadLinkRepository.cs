using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for DownloadLink entity
/// </summary>
public interface IDownloadLinkRepository : IRepository<DownloadLink>
{
    Task<IEnumerable<DownloadLink>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<DownloadLink>> GetByTypeAsync(string linkType, CancellationToken cancellationToken = default);
    Task<IEnumerable<DownloadLink>> GetByCollectionIdAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<DownloadLink>> GetActiveLinksAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DownloadLink>> GetExpiredLinksAsync(CancellationToken cancellationToken = default);
}
