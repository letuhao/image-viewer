using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// Cache folder repository implementation
/// </summary>
public class CacheFolderRepository : Repository<CacheFolder>, ICacheFolderRepository
{
    public CacheFolderRepository(ImageViewerDbContext context, ILogger<CacheFolderRepository> logger) : base(context, logger)
    {
    }

    public async Task<CacheFolder?> GetByPathAsync(string path)
    {
        try
        {
            return await _dbSet
                .FirstOrDefaultAsync(cf => cf.Path == path && cf.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache folder by path {Path}", path);
            throw;
        }
    }

    public async Task<IEnumerable<CacheFolder>> GetActiveOrderedByPriorityAsync()
    {
        try
        {
            return await _dbSet
                .Where(cf => cf.IsActive)
                .OrderBy(cf => cf.Priority)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active cache folders ordered by priority");
            throw;
        }
    }

    public async Task<IEnumerable<CacheFolder>> GetByPriorityRangeAsync(int minPriority, int maxPriority)
    {
        try
        {
            return await _dbSet
                .Where(cf => cf.Priority >= minPriority && cf.Priority <= maxPriority && cf.IsActive)
                .OrderBy(cf => cf.Priority)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache folders by priority range {MinPriority}-{MaxPriority}", minPriority, maxPriority);
            throw;
        }
    }
}
