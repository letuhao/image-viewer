using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// Collection tag repository implementation
/// </summary>
public class CollectionTagRepository : Repository<CollectionTag>, ICollectionTagRepository
{
    public CollectionTagRepository(ImageViewerDbContext context, ILogger<CollectionTagRepository> logger) : base(context, logger)
    {
    }

    public async Task<IEnumerable<CollectionTag>> GetByCollectionIdAsync(Guid collectionId)
    {
        try
        {
            return await _dbSet
                .Where(ct => ct.CollectionId == collectionId)
                .Include(ct => ct.Tag)
                .Include(ct => ct.Collection)
                .OrderBy(ct => ct.Tag.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection tags by collection ID {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<IEnumerable<CollectionTag>> GetByTagIdAsync(Guid tagId)
    {
        try
        {
            return await _dbSet
                .Where(ct => ct.TagId == tagId)
                .Include(ct => ct.Tag)
                .Include(ct => ct.Collection)
                .OrderBy(ct => ct.Collection.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection tags by tag ID {TagId}", tagId);
            throw;
        }
    }

    public async Task<CollectionTag?> GetByCollectionAndTagAsync(Guid collectionId, Guid tagId)
    {
        try
        {
            return await _dbSet
                .Where(ct => ct.CollectionId == collectionId && ct.TagId == tagId)
                .Include(ct => ct.Tag)
                .Include(ct => ct.Collection)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collection tag by collection ID {CollectionId} and tag ID {TagId}", collectionId, tagId);
            throw;
        }
    }

    public async Task<bool> HasTagAsync(Guid collectionId, Guid tagId)
    {
        try
        {
            return await _dbSet
                .AnyAsync(ct => ct.CollectionId == collectionId && ct.TagId == tagId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if collection {CollectionId} has tag {TagId}", collectionId, tagId);
            throw;
        }
    }

    public async Task<Dictionary<Guid, int>> GetTagUsageCountsAsync()
    {
        try
        {
            return await _dbSet
                .GroupBy(ct => ct.TagId)
                .Select(g => new { TagId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TagId, x => x.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag usage counts");
            throw;
        }
    }

    public async Task<IEnumerable<CollectionTag>> GetCollectionsByTagIdAsync(Guid tagId)
    {
        try
        {
            return await _dbSet
                .Where(ct => ct.TagId == tagId)
                .Include(ct => ct.Collection)
                .Include(ct => ct.Tag)
                .OrderBy(ct => ct.Collection.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collections by tag ID {TagId}", tagId);
            throw;
        }
    }
}
