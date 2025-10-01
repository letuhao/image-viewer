using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// Image repository implementation
/// </summary>
public class ImageRepository : Repository<Image>, IImageRepository
{
    public ImageRepository(ImageViewerDbContext context, ILogger<ImageRepository> logger) : base(context, logger)
    {
    }

    public async Task<IEnumerable<Image>> GetByCollectionIdAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet
                .Include(i => i.CacheInfo)
                .Where(i => i.CollectionId == collectionId && !i.IsDeleted)
                .OrderBy(i => i.Filename)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting images by collection ID {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<Image?> GetByCollectionIdAndFilenameAsync(Guid collectionId, string filename, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet
                .Include(i => i.CacheInfo)
                .FirstOrDefaultAsync(i => i.CollectionId == collectionId && i.Filename == filename && !i.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image by collection ID {CollectionId} and filename {Filename}", collectionId, filename);
            throw;
        }
    }

    public async Task<IEnumerable<Image>> GetByFormatAsync(string format, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet
                .Include(i => i.CacheInfo)
                .Where(i => i.Format.Equals(format, StringComparison.OrdinalIgnoreCase) && !i.IsDeleted)
                .OrderBy(i => i.Filename)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting images by format {Format}", format);
            throw;
        }
    }

    public async Task<IEnumerable<Image>> GetBySizeRangeAsync(int minWidth, int minHeight, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet
                .Include(i => i.CacheInfo)
                .Where(i => i.Width >= minWidth && i.Height >= minHeight && !i.IsDeleted)
                .OrderBy(i => i.Filename)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting images by size range {MinWidth}x{MinHeight}", minWidth, minHeight);
            throw;
        }
    }

    public async Task<IEnumerable<Image>> GetLargeImagesAsync(long minSizeBytes, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet
                .Include(i => i.CacheInfo)
                .Where(i => i.FileSize >= minSizeBytes && !i.IsDeleted)
                .OrderByDescending(i => i.FileSize)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting large images with minimum size {MinSizeBytes}", minSizeBytes);
            throw;
        }
    }

    public async Task<IEnumerable<Image>> GetHighResolutionImagesAsync(int minWidth, int minHeight, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet
                .Include(i => i.CacheInfo)
                .Where(i => i.Width >= minWidth && i.Height >= minHeight && !i.IsDeleted)
                .OrderByDescending(i => i.Width * i.Height)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting high resolution images {MinWidth}x{MinHeight}", minWidth, minHeight);
            throw;
        }
    }

    public async Task<Image?> GetRandomImageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _dbSet.CountAsync(i => !i.IsDeleted, cancellationToken);
            if (count == 0) return null;

            var random = new Random();
            var skip = random.Next(0, count);

            return await _dbSet
                .Include(i => i.CacheInfo)
                .Where(i => !i.IsDeleted)
                .Skip(skip)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting random image");
            throw;
        }
    }

    public async Task<Image?> GetRandomImageByCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _dbSet.CountAsync(i => i.CollectionId == collectionId && !i.IsDeleted, cancellationToken);
            if (count == 0) return null;

            var random = new Random();
            var skip = random.Next(0, count);

            return await _dbSet
                .Include(i => i.CacheInfo)
                .Where(i => i.CollectionId == collectionId && !i.IsDeleted)
                .Skip(skip)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting random image by collection {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<Image?> GetNextImageAsync(Guid currentImageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentImage = await _dbSet
                .FirstOrDefaultAsync(i => i.Id == currentImageId && !i.IsDeleted, cancellationToken);

            if (currentImage == null) return null;

            return await _dbSet
                .Include(i => i.CacheInfo)
                .Where(i => i.CollectionId == currentImage.CollectionId && 
                           i.Filename.CompareTo(currentImage.Filename) > 0 && 
                           !i.IsDeleted)
                .OrderBy(i => i.Filename)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next image for {CurrentImageId}", currentImageId);
            throw;
        }
    }

    public async Task<Image?> GetPreviousImageAsync(Guid currentImageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentImage = await _dbSet
                .FirstOrDefaultAsync(i => i.Id == currentImageId && !i.IsDeleted, cancellationToken);

            if (currentImage == null) return null;

            return await _dbSet
                .Include(i => i.CacheInfo)
                .Where(i => i.CollectionId == currentImage.CollectionId && 
                           i.Filename.CompareTo(currentImage.Filename) < 0 && 
                           !i.IsDeleted)
                .OrderByDescending(i => i.Filename)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting previous image for {CurrentImageId}", currentImageId);
            throw;
        }
    }

    public async Task<long> GetTotalSizeByCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet
                .Where(i => i.CollectionId == collectionId && !i.IsDeleted)
                .SumAsync(i => i.FileSize, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total size by collection {CollectionId}", collectionId);
            throw;
        }
    }

    public async Task<int> GetCountByCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet
                .CountAsync(i => i.CollectionId == collectionId && !i.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting count by collection {CollectionId}", collectionId);
            throw;
        }
    }
}
