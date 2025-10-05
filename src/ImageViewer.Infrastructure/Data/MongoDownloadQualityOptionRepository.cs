using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoDownloadQualityOptionRepository : MongoRepository<DownloadQualityOption>, IDownloadQualityOptionRepository
{
    public MongoDownloadQualityOptionRepository(IMongoDatabase database, ILogger<MongoDownloadQualityOptionRepository> logger)
        : base(database.GetCollection<DownloadQualityOption>("downloadQualityOptions"), logger)
    {
    }

    public async Task<IEnumerable<DownloadQualityOption>> GetByQualityAsync(string quality, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(option => option.Quality == quality)
                .SortBy(option => option.Bitrate)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get download quality options for quality {Quality}", quality);
            throw;
        }
    }

    public async Task<IEnumerable<DownloadQualityOption>> GetByCollectionIdAsync(ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(option => option.CollectionId == collectionId)
                .SortBy(option => option.Bitrate)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get download quality options for collection {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<IEnumerable<DownloadQualityOption>> GetActiveOptionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(option => option.IsActive == true)
                .SortBy(option => option.Bitrate)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get active download quality options");
            throw;
        }
    }

    public async Task<IEnumerable<DownloadQualityOption>> GetByBandwidthAsync(long maxBandwidth, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(option => option.Bitrate <= maxBandwidth)
                .SortBy(option => option.Bitrate)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get download quality options for bandwidth {MaxBandwidth}", maxBandwidth);
            throw;
        }
    }
}
