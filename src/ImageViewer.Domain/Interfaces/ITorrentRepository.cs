using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for Torrent entity
/// </summary>
public interface ITorrentRepository : IRepository<Torrent>
{
    Task<IEnumerable<Torrent>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Torrent>> GetByTypeAsync(string torrentType, CancellationToken cancellationToken = default);
    Task<IEnumerable<Torrent>> GetByCollectionIdAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Torrent>> GetActiveTorrentsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Torrent>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
