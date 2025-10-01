using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// Unit of Work implementation
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ImageViewerDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(ImageViewerDbContext context, ILogger<UnitOfWork> logger, ILoggerFactory loggerFactory)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    // Repository properties
    public ICollectionRepository Collections => new CollectionRepository(_context, _loggerFactory.CreateLogger<CollectionRepository>());
    public IImageRepository Images => new ImageRepository(_context, _loggerFactory.CreateLogger<ImageRepository>());
    public IRepository<CacheFolder> CacheFolders => new Repository<CacheFolder>(_context, _loggerFactory.CreateLogger<Repository<CacheFolder>>());
    public IRepository<Tag> Tags => new Repository<Tag>(_context, _loggerFactory.CreateLogger<Repository<Tag>>());
    public IRepository<CollectionTag> CollectionTags => new Repository<CollectionTag>(_context, _loggerFactory.CreateLogger<Repository<CollectionTag>>());
    public IRepository<ImageCacheInfo> ImageCacheInfos => new Repository<ImageCacheInfo>(_context, _loggerFactory.CreateLogger<Repository<ImageCacheInfo>>());
    public IRepository<CollectionCacheBinding> CollectionCacheBindings => new Repository<CollectionCacheBinding>(_context, _loggerFactory.CreateLogger<Repository<CollectionCacheBinding>>());
    public IRepository<CollectionStatistics> CollectionStatistics => new Repository<CollectionStatistics>(_context, _loggerFactory.CreateLogger<Repository<CollectionStatistics>>());
    public IRepository<ViewSession> ViewSessions => new Repository<ViewSession>(_context, _loggerFactory.CreateLogger<Repository<ViewSession>>());
            public IRepository<BackgroundJob> BackgroundJobs => new Repository<BackgroundJob>(_context, _loggerFactory.CreateLogger<Repository<BackgroundJob>>());
            public IRepository<CollectionSettingsEntity> CollectionSettings => new Repository<CollectionSettingsEntity>(_context, _loggerFactory.CreateLogger<Repository<CollectionSettingsEntity>>());
            public IRepository<ImageMetadataEntity> ImageMetadata => new Repository<ImageMetadataEntity>(_context, _loggerFactory.CreateLogger<Repository<ImageMetadataEntity>>());

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Saved {Count} changes to database", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes to database");
            throw;
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("Transaction already started");
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        _logger.LogDebug("Database transaction started");
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction to commit");
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
            _logger.LogDebug("Database transaction committed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing transaction");
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction to rollback");
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
            _logger.LogDebug("Database transaction rolled back");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back transaction");
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
