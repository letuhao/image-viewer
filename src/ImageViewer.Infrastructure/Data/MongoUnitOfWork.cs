using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

#pragma warning disable CS0618 // Type or member is obsolete - MongoUnitOfWork uses legacy repositories for backward compatibility

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB unit of work implementation
/// TODO: Refactor to remove legacy Image/ThumbnailInfo repositories
/// </summary>
public class MongoUnitOfWork : IUnitOfWork
{
    private readonly IMongoDatabase _database;
    private IClientSessionHandle? _session;
    private readonly ILogger<MongoUnitOfWork> _logger;
    private bool _disposed = false;

    public MongoUnitOfWork(IMongoDatabase database, ILogger<MongoUnitOfWork> logger)
    {
        _database = database;
        _logger = logger;
    }

    public ICollectionRepository Collections { get; set; } = null!;
    public IRepository<CacheFolder> CacheFolders { get; set; } = null!;
    public IRepository<Domain.Entities.Tag> Tags { get; set; } = null!;
    public IRepository<CollectionTag> CollectionTags { get; set; } = null!;
    public IRepository<CollectionCacheBinding> CollectionCacheBindings { get; set; } = null!;
    public IRepository<CollectionStatisticsEntity> CollectionStatistics { get; set; } = null!;
    public IRepository<ViewSession> ViewSessions { get; set; } = null!;
    public IRepository<BackgroundJob> BackgroundJobs { get; set; } = null!;
    public IRepository<CollectionSettingsEntity> CollectionSettings { get; set; } = null!;
    public IRepository<ImageMetadataEntity> ImageMetadata { get; set; } = null!;

    public void Initialize()
    {
        Collections = new MongoCollectionRepository(_database);
        CacheFolders = new MongoRepository<CacheFolder>(_database, "cache_folders");
        Tags = new MongoRepository<Domain.Entities.Tag>(_database, "tags");
        CollectionTags = new MongoRepository<CollectionTag>(_database, "collection_tags");
        CollectionCacheBindings = new MongoRepository<CollectionCacheBinding>(_database, "collection_cache_bindings");
        CollectionStatistics = new MongoRepository<CollectionStatisticsEntity>(_database, "collection_statistics");
        ViewSessions = new MongoRepository<ViewSession>(_database, "view_sessions");
        BackgroundJobs = new MongoRepository<BackgroundJob>(_database, "background_jobs");
        CollectionSettings = new MongoRepository<CollectionSettingsEntity>(_database, "collection_settings");
        ImageMetadata = new MongoRepository<ImageMetadataEntity>(_database, "image_metadata");
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // MongoDB doesn't have explicit save changes like EF Core
        // All operations are immediately persisted
        _logger.LogDebug("MongoDB operations are automatically persisted");
        return 1; // Return 1 to indicate success
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_session == null)
        {
            _session = await _database.Client.StartSessionAsync(cancellationToken: cancellationToken);
            _session.StartTransaction();
            _logger.LogDebug("MongoDB transaction started");
        }
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_session != null)
        {
            await _session.CommitTransactionAsync(cancellationToken);
            _logger.LogDebug("MongoDB transaction committed");
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_session != null)
        {
            await _session.AbortTransactionAsync(cancellationToken);
            _logger.LogDebug("MongoDB transaction rolled back");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _session?.Dispose();
            _disposed = true;
        }
    }
}
