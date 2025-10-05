using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoDownloadLinkRepository : MongoRepository<DownloadLink>, IDownloadLinkRepository
{
    public MongoDownloadLinkRepository(IMongoDatabase database, ILogger<MongoDownloadLinkRepository> logger)
        : base(database.GetCollection<DownloadLink>("downloadLinks"), logger)
    {
    }

    public async Task<IEnumerable<DownloadLink>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(link => link.Status == status)
                .SortByDescending(link => link.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get download links for status {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<DownloadLink>> GetByTypeAsync(string linkType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(link => link.Type == linkType)
                .SortByDescending(link => link.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get download links for type {LinkType}", linkType);
            throw;
        }
    }

    public async Task<IEnumerable<DownloadLink>> GetByCollectionIdAsync(ObjectId collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(link => link.CollectionId == collectionId)
                .SortByDescending(link => link.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get download links for collection {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<IEnumerable<DownloadLink>> GetActiveLinksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _collection.Find(link => 
                link.Status == "Active" && 
                (link.ExpiresAt == null || link.ExpiresAt > now))
                .SortByDescending(link => link.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get active download links");
            throw;
        }
    }

    public async Task<IEnumerable<DownloadLink>> GetExpiredLinksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _collection.Find(link => 
                link.ExpiresAt != null && 
                link.ExpiresAt <= now)
                .SortByDescending(link => link.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get expired download links");
            throw;
        }
    }
}
