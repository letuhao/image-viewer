using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// Collection repository implementation
/// </summary>
public class CollectionRepository : Repository<Collection>, ICollectionRepository
{
    public CollectionRepository(ImageViewerDbContext context, ILogger<CollectionRepository> logger) : base(context, logger)
    {
    }

    public override async Task<Collection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet
                .Include(c => c.Images)
                .Include(c => c.Tags)
                .Include(c => c.Statistics)
                .Include(c => c.Settings)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection by ID {Id}", id);
            throw;
        }
    }

    public async Task<Collection?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet
                .Include(c => c.Images)
                .Include(c => c.Tags)
                .Include(c => c.Statistics)
                .FirstOrDefaultAsync(c => c.Name == name && !c.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection by name {Name}", name);
            throw;
        }
    }

    public async Task<Collection?> GetByPathAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet
                .Include(c => c.Images)
                .Include(c => c.Tags)
                .Include(c => c.Statistics)
                .FirstOrDefaultAsync(c => c.Path == path && !c.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection by path {Path}", path);
            throw;
        }
    }

    public async Task<IEnumerable<Collection>> GetByTypeAsync(CollectionType type, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet
                .Include(c => c.Images)
                .Include(c => c.Tags)
                .Include(c => c.Statistics)
                .Where(c => c.Type == type && !c.IsDeleted)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collections by type {Type}", type);
            throw;
        }
    }

    public async Task<IEnumerable<Collection>> GetActiveCollectionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet
                .Include(c => c.Images)
                .Include(c => c.Tags)
                .Include(c => c.Statistics)
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active collections");
            throw;
        }
    }

    public async Task<IEnumerable<Collection>> GetActiveCollectionsQueryableAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Images)
            .Include(c => c.Tags)
            .Include(c => c.Statistics)
            .Where(c => !c.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Collection>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet
                .Include(c => c.Images)
                .Include(c => c.Tags)
                .Include(c => c.Statistics)
                .Where(c => c.Name.Contains(searchTerm) && !c.IsDeleted)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching collections by name {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<IEnumerable<Collection>> GetCollectionsWithImagesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet
                .Include(c => c.Images)
                .Include(c => c.Tags)
                .Include(c => c.Statistics)
                .Where(c => !c.IsDeleted && c.Images.Any())
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collections with images");
            throw;
        }
    }

    public async Task<IEnumerable<Collection>> GetCollectionsByTagAsync(string tagName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet
                .Include(c => c.Images)
                .Include(c => c.Tags)
                .Include(c => c.Statistics)
                .Where(c => !c.IsDeleted && c.Tags.Any(t => t.Tag.Name == tagName))
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collections by tag {TagName}", tagName);
            throw;
        }
    }

    public async Task<long> GetTotalSizeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet
                .Where(c => !c.IsDeleted)
                .SelectMany(c => c.Images)
                .SumAsync(i => i.FileSize, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total size");
            throw;
        }
    }

    public async Task<int> GetTotalImageCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet
                .Where(c => !c.IsDeleted)
                .SelectMany(c => c.Images)
                .CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total image count");
            throw;
        }
    }
}
