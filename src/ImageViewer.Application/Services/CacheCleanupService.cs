using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace ImageViewer.Application.Services;

/// <summary>
/// Implementation of cache cleanup service
/// </summary>
public class CacheCleanupService : ICacheCleanupService
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly ICacheFolderRepository _cacheFolderRepository;
    private readonly ILogger<CacheCleanupService> _logger;

    public CacheCleanupService(
        ICollectionRepository collectionRepository,
        ICacheFolderRepository cacheFolderRepository,
        ILogger<CacheCleanupService> logger)
    {
        _collectionRepository = collectionRepository;
        _cacheFolderRepository = cacheFolderRepository;
        _logger = logger;
    }

    public async Task<int> CleanupOrphanedCacheFilesAsync(string cacheFolderPath, int olderThanDays = 7)
    {
        try
        {
            _logger.LogInformation("🧹 Cleaning up orphaned cache files in {CacheFolderPath} (older than {Days} days)", 
                cacheFolderPath, olderThanDays);

            var cacheDir = Path.Combine(cacheFolderPath, "cache");
            if (!Directory.Exists(cacheDir))
            {
                _logger.LogWarning("⚠️ Cache directory does not exist: {Path}", cacheDir);
                return 0;
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
            var deletedCount = 0;
            var totalSize = 0L;

            // Get all collections to check against
            var collections = await _collectionRepository.GetAllAsync();
            var collectionsList = collections.ToList();

            // Scan all cache files
            var cacheFiles = Directory.GetFiles(cacheDir, "*", SearchOption.AllDirectories);
            
            foreach (var cacheFile in cacheFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(cacheFile);
                    
                    // Only cleanup old files
                    if (fileInfo.LastWriteTimeUtc > cutoffDate)
                    {
                        continue;
                    }

                    // Extract imageId from filename (format: {imageId}_cache_{width}x{height}.{ext})
                    var fileName = Path.GetFileNameWithoutExtension(cacheFile);
                    var parts = fileName.Split('_');
                    if (parts.Length < 2)
                    {
                        continue; // Invalid filename format
                    }

                    var imageId = parts[0];

                    // Check if this cache file exists in any collection's CacheImages
                    var isOrphaned = true;
                    foreach (var collection in collectionsList)
                    {
                        if (collection.CacheImages.Any(c => c.ImageId == imageId && c.Path == cacheFile))
                        {
                            isOrphaned = false;
                            break;
                        }
                    }

                    if (isOrphaned)
                    {
                        _logger.LogInformation("🗑️ Deleting orphaned cache file: {Path} (age: {Age} days)", 
                            cacheFile, (DateTime.UtcNow - fileInfo.LastWriteTimeUtc).TotalDays);
                        
                        totalSize += fileInfo.Length;
                        File.Delete(cacheFile);
                        deletedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "❌ Failed to check/delete cache file: {Path}", cacheFile);
                }
            }

            _logger.LogInformation("✅ Cleaned up {Count} orphaned cache files, freed {Size} bytes", 
                deletedCount, totalSize);

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up orphaned cache files");
            return 0;
        }
    }

    public async Task<int> CleanupOrphanedThumbnailFilesAsync(string cacheFolderPath, int olderThanDays = 7)
    {
        try
        {
            _logger.LogInformation("🧹 Cleaning up orphaned thumbnail files in {CacheFolderPath} (older than {Days} days)", 
                cacheFolderPath, olderThanDays);

            var thumbnailDir = Path.Combine(cacheFolderPath, "thumbnails");
            if (!Directory.Exists(thumbnailDir))
            {
                _logger.LogWarning("⚠️ Thumbnail directory does not exist: {Path}", thumbnailDir);
                return 0;
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
            var deletedCount = 0;
            var totalSize = 0L;

            // Get all collections to check against
            var collections = await _collectionRepository.GetAllAsync();
            var collectionsList = collections.ToList();

            // Scan all thumbnail files
            var thumbnailFiles = Directory.GetFiles(thumbnailDir, "*", SearchOption.AllDirectories);
            
            foreach (var thumbnailFile in thumbnailFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(thumbnailFile);
                    
                    // Only cleanup old files
                    if (fileInfo.LastWriteTimeUtc > cutoffDate)
                    {
                        continue;
                    }

                    // Extract imageId from filename (format: {imageId}_{width}x{height}.{ext})
                    var fileName = Path.GetFileNameWithoutExtension(thumbnailFile);
                    var parts = fileName.Split('_');
                    if (parts.Length < 2)
                    {
                        continue; // Invalid filename format
                    }

                    var imageId = parts[0];

                    // Check if this thumbnail exists in any collection's Thumbnails
                    var isOrphaned = true;
                    foreach (var collection in collectionsList)
                    {
                        if (collection.Thumbnails.Any(t => t.ImageId == imageId && t.ThumbnailPath == thumbnailFile))
                        {
                            isOrphaned = false;
                            break;
                        }
                    }

                    if (isOrphaned)
                    {
                        _logger.LogInformation("🗑️ Deleting orphaned thumbnail file: {Path} (age: {Age} days)", 
                            thumbnailFile, (DateTime.UtcNow - fileInfo.LastWriteTimeUtc).TotalDays);
                        
                        totalSize += fileInfo.Length;
                        File.Delete(thumbnailFile);
                        deletedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "❌ Failed to check/delete thumbnail file: {Path}", thumbnailFile);
                }
            }

            _logger.LogInformation("✅ Cleaned up {Count} orphaned thumbnail files, freed {Size} bytes", 
                deletedCount, totalSize);

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up orphaned thumbnail files");
            return 0;
        }
    }

    public async Task<(int cacheFiles, int thumbnailFiles)> CleanupOrphanedFilesAsync(string cacheFolderPath, int olderThanDays = 7)
    {
        var cacheFiles = await CleanupOrphanedCacheFilesAsync(cacheFolderPath, olderThanDays);
        var thumbnailFiles = await CleanupOrphanedThumbnailFilesAsync(cacheFolderPath, olderThanDays);
        
        return (cacheFiles, thumbnailFiles);
    }

    public async Task ReconcileCacheFolderStatisticsAsync(string cacheFolderId)
    {
        try
        {
            _logger.LogInformation("🔄 Reconciling cache folder statistics for {CacheFolderId}", cacheFolderId);

            var cacheFolder = await _cacheFolderRepository.GetByIdAsync(ObjectId.Parse(cacheFolderId));
            if (cacheFolder == null)
            {
                _logger.LogWarning("⚠️ Cache folder {Id} not found", cacheFolderId);
                return;
            }

            // Scan actual disk usage
            var cacheDir = Path.Combine(cacheFolder.Path, "cache");
            var thumbnailDir = Path.Combine(cacheFolder.Path, "thumbnails");

            long actualSize = 0;
            int actualFileCount = 0;

            if (Directory.Exists(cacheDir))
            {
                var cacheFiles = Directory.GetFiles(cacheDir, "*", SearchOption.AllDirectories);
                foreach (var file in cacheFiles)
                {
                    var fileInfo = new FileInfo(file);
                    actualSize += fileInfo.Length;
                    actualFileCount++;
                }
            }

            if (Directory.Exists(thumbnailDir))
            {
                var thumbnailFiles = Directory.GetFiles(thumbnailDir, "*", SearchOption.AllDirectories);
                foreach (var file in thumbnailFiles)
                {
                    var fileInfo = new FileInfo(file);
                    actualSize += fileInfo.Length;
                    actualFileCount++;
                }
            }

            // Update statistics if discrepancy found
            if (actualSize != cacheFolder.CurrentSizeBytes || actualFileCount != cacheFolder.TotalFiles)
            {
                _logger.LogWarning("⚠️ Statistics mismatch for {Name}: " +
                    "Size (DB: {DbSize}, Disk: {ActualSize}), " +
                    "Files (DB: {DbFiles}, Disk: {ActualFiles})",
                    cacheFolder.Name,
                    cacheFolder.CurrentSizeBytes, actualSize,
                    cacheFolder.TotalFiles, actualFileCount);

                cacheFolder.UpdateStatistics(actualSize, actualFileCount);
                await _cacheFolderRepository.UpdateAsync(cacheFolder);

                _logger.LogInformation("✅ Reconciled cache folder {Name} statistics", cacheFolder.Name);
            }
            else
            {
                _logger.LogInformation("✅ Cache folder {Name} statistics are accurate", cacheFolder.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reconciling cache folder statistics");
        }
    }
}

