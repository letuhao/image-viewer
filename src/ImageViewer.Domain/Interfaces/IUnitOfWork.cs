using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Unit of work interface
/// TODO: Refactor to remove legacy Image/ThumbnailInfo repositories
/// </summary>
[Obsolete("IUnitOfWork contains obsolete Image/ThumbnailInfo repositories. Use ICollectionRepository directly for embedded design.")]
public interface IUnitOfWork : IDisposable
{
    ICollectionRepository Collections { get; }
    IRepository<CacheFolder> CacheFolders { get; }
    IRepository<Tag> Tags { get; }
    IRepository<CollectionTag> CollectionTags { get; }
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
