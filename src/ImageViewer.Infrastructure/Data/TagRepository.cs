using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// Tag repository implementation
/// </summary>
public class TagRepository : Repository<Tag>, ITagRepository
{
    public TagRepository(ImageViewerDbContext context, ILogger<TagRepository> logger) : base(context, logger)
    {
    }

    public async Task<Tag?> GetByNameAsync(string name)
    {
        try
        {
            return await _dbSet
                .FirstOrDefaultAsync(t => t.Name == name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag by name {Name}", name);
            throw;
        }
    }

    public async Task<IEnumerable<Tag>> SearchByNameAsync(string query)
    {
        try
        {
            return await _dbSet
                .Where(t => t.Name.Contains(query))
                .OrderBy(t => t.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching tags by name {Query}", query);
            throw;
        }
    }

    public async Task<IEnumerable<Tag>> GetPopularTagsAsync(int limit = 20)
    {
        try
        {
            return await _dbSet
                .OrderByDescending(t => t.UsageCount)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular tags with limit {Limit}", limit);
            throw;
        }
    }

    public async Task<IEnumerable<Tag>> GetByCollectionIdAsync(Guid collectionId)
    {
        try
        {
            return await _dbSet
                .Where(t => t.CollectionTags.Any(ct => ct.CollectionId == collectionId))
                .OrderBy(t => t.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tags by collection ID {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<int> GetUsageCountAsync(Guid tagId)
    {
        try
        {
            return await _dbSet
                .Where(t => t.Id == tagId)
                .Select(t => t.UsageCount)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage count for tag {TagId}", tagId);
            throw;
        }
    }
}
