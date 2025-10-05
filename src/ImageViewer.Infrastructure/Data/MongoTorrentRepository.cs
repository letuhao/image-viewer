using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoTorrentRepository : MongoRepository<Torrent>, ITorrentRepository
{
    public MongoTorrentRepository(IMongoDatabase database, ILogger<MongoTorrentRepository> logger)
        : base(database.GetCollection<Torrent>("torrents"), logger)
    {
    }

    public async Task<IEnumerable<Torrent>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(torrent => torrent.Status == status)
                .SortByDescending(torrent => torrent.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get torrents for status {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<Torrent>> GetByTypeAsync(string torrentType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(torrent => torrent.Status == torrentType)
                .SortByDescending(torrent => torrent.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get torrents for type {TorrentType}", torrentType);
            throw;
        }
    }

    public async Task<IEnumerable<Torrent>> GetByCollectionIdAsync(ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(torrent => torrent.CollectionId == collectionId)
                .SortByDescending(torrent => torrent.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get torrents for collection {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<IEnumerable<Torrent>> GetActiveTorrentsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(torrent => torrent.Status == "Active")
                .SortByDescending(torrent => torrent.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get active torrents");
            throw;
        }
    }

    public async Task<IEnumerable<Torrent>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<Torrent>.Filter.And(
                Builders<Torrent>.Filter.Gte(torrent => torrent.CreatedAt, startDate),
                Builders<Torrent>.Filter.Lte(torrent => torrent.CreatedAt, endDate)
            );

            return await _collection.Find(filter)
                .SortByDescending(torrent => torrent.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get torrents for date range {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }
}
