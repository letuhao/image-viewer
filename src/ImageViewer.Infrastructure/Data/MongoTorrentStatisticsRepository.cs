using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoTorrentStatisticsRepository : MongoRepository<TorrentStatistics>, ITorrentStatisticsRepository
{
    public MongoTorrentStatisticsRepository(IMongoDatabase database, ILogger<MongoTorrentStatisticsRepository> logger)
        : base(database.GetCollection<TorrentStatistics>("torrentStatistics"), logger)
    {
    }

    public async Task<IEnumerable<TorrentStatistics>> GetByTorrentIdAsync(ObjectId torrentId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(stats => stats.TorrentId == torrentId)
                .SortByDescending(stats => stats.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get torrent statistics for torrent {TorrentId}", torrentId);
            throw;
        }
    }

    public async Task<IEnumerable<TorrentStatistics>> GetByCollectionIdAsync(ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(stats => stats.TorrentId == collectionId)
                .SortByDescending(stats => stats.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get torrent statistics for collection {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<IEnumerable<TorrentStatistics>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<TorrentStatistics>.Filter.And(
                Builders<TorrentStatistics>.Filter.Gte(stats => stats.CreatedAt, startDate),
                Builders<TorrentStatistics>.Filter.Lte(stats => stats.CreatedAt, endDate)
            );

            return await _collection.Find(filter)
                .SortByDescending(stats => stats.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get torrent statistics for date range {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }

    public async Task<IEnumerable<TorrentStatistics>> GetTopPerformersAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(_ => true)
                .SortByDescending(stats => stats.TotalDownloaded)
                .Limit(limit)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get top performing torrents");
            throw;
        }
    }
}
