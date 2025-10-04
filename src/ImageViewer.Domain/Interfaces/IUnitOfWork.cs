using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Unit of work interface
/// </summary>
public interface IUnitOfWork : IDisposable
{
    ICollectionRepository Collections { get; }
    IImageRepository Images { get; }
    IRepository<CacheFolder> CacheFolders { get; }
    IRepository<Tag> Tags { get; }
    IRepository<CollectionTag> CollectionTags { get; }
    IRepository<ImageCacheInfo> ImageCacheInfos { get; }
    IRepository<CollectionCacheBinding> CollectionCacheBindings { get; }
    IRepository<CollectionStatisticsEntity> CollectionStatistics { get; }
    IRepository<ViewSession> ViewSessions { get; }
    IRepository<BackgroundJob> BackgroundJobs { get; }
    IRepository<CollectionSettingsEntity> CollectionSettings { get; }
    IRepository<ImageMetadataEntity> ImageMetadata { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
