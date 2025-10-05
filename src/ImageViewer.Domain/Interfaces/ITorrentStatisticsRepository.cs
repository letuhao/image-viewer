using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for TorrentStatistics entity
/// </summary>
public interface ITorrentStatisticsRepository : IRepository<TorrentStatistics>
{
    Task<IEnumerable<TorrentStatistics>> GetByTorrentIdAsync(ObjectId torrentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TorrentStatistics>> GetByCollectionIdAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TorrentStatistics>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<TorrentStatistics>> GetTopPerformersAsync(int limit = 10, CancellationToken cancellationToken = default);
}
