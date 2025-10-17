using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System.Diagnostics;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service for recalculating metadata for cache folders and libraries
/// </summary>
public class MetadataRecalculationService : IMetadataRecalculationService
{
    private readonly ICacheFolderRepository _cacheFolderRepository;
    private readonly ILibraryRepository _libraryRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly ILogger<MetadataRecalculationService> _logger;

    public MetadataRecalculationService(
        ICacheFolderRepository cacheFolderRepository,
        ILibraryRepository libraryRepository,
        ICollectionRepository collectionRepository,
        ILogger<MetadataRecalculationService> logger)
    {
        _cacheFolderRepository = cacheFolderRepository;
        _libraryRepository = libraryRepository;
        _collectionRepository = collectionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Recalculates all cache folder metadata based on actual files and collection data
    /// </summary>
    public async Task<MetadataRecalculationResult> RecalculateCacheFolderMetadataAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MetadataRecalculationResult
        {
            OperationType = "Cache Folder Metadata Recalculation",
            StartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("🔄 Starting cache folder metadata recalculation...");

            // Get all cache folders
            var cacheFolders = await _cacheFolderRepository.GetAllAsync();
            result.TotalItems = cacheFolders.Count();

            foreach (var cacheFolder in cacheFolders)
            {
                try
                {
                    _logger.LogDebug("📁 Recalculating metadata for cache folder: {Name} ({Path})", 
                        cacheFolder.Name, cacheFolder.Path);

                    // Calculate actual disk usage
                    var diskUsage = await CalculateDiskUsageAsync(cacheFolder.Path);
                    
                    // Get collections that have cache in this folder
                    var collectionsWithCache = await GetCollectionsWithCacheInFolderAsync(cacheFolder.Id);
                    
                    // Calculate statistics from collections
                    var totalFiles = 0;
                    var totalSize = 0L;
                    var collectionIds = new List<string>();

                    foreach (var collection in collectionsWithCache)
                    {
                        // Count cache files and thumbnails for this collection
                        // Since cache images and thumbnails don't have direct cache folder IDs,
                        // we'll count all cache files for collections that are bound to this cache folder
                        var cacheFiles = collection.CacheImages?.Count ?? 0;
                        var thumbnailFiles = collection.Thumbnails?.Count ?? 0;
                        var totalCollectionFiles = cacheFiles + thumbnailFiles;
                        
                        // Calculate total size for all cache files in this collection
                        var cacheSize = collection.CacheImages?.Sum(c => c.FileSize) ?? 0L;
                        var thumbnailSize = collection.Thumbnails?.Sum(t => t.FileSize) ?? 0L;
                        var totalCollectionSize = cacheSize + thumbnailSize;

                        totalFiles += totalCollectionFiles;
                        totalSize += totalCollectionSize;
                        collectionIds.Add(collection.Id.ToString());
                    }

                    // Update cache folder with recalculated data
                    cacheFolder.UpdateStatistics(totalSize, totalFiles);
                    cacheFolder.CachedCollectionIds.Clear();
                    cacheFolder.CachedCollectionIds.AddRange(collectionIds);
                    // TotalCollections is calculated automatically when CachedCollectionIds is updated

                    await _cacheFolderRepository.UpdateAsync(cacheFolder);
                    
                    result.ProcessedItems++;
                    _logger.LogDebug("✅ Updated cache folder {Name}: {Files} files, {Size} bytes", 
                        cacheFolder.Name, totalFiles, totalSize);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Error processing cache folder {cacheFolder.Name}: {ex.Message}");
                    _logger.LogError(ex, "❌ Error processing cache folder {Name}", cacheFolder.Name);
                }
            }

            result.CompletedAt = DateTime.UtcNow;
            result.Duration = stopwatch.Elapsed;
            result.Success = result.Errors.Count == 0;

            _logger.LogInformation("✅ Cache folder metadata recalculation completed in {Duration}ms. " +
                "Processed: {Processed}/{Total}, Errors: {ErrorCount}",
                result.Duration.TotalMilliseconds, result.ProcessedItems, result.TotalItems, result.Errors.Count);

            return result;
        }
        catch (Exception ex)
        {
            result.CompletedAt = DateTime.UtcNow;
            result.Duration = stopwatch.Elapsed;
            result.Success = false;
            result.Errors.Add($"Fatal error: {ex.Message}");
            
            _logger.LogError(ex, "❌ Fatal error during cache folder metadata recalculation");
            return result;
        }
    }

    /// <summary>
    /// Recalculates all library metadata based on collection statistics
    /// </summary>
    public async Task<MetadataRecalculationResult> RecalculateLibraryMetadataAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MetadataRecalculationResult
        {
            OperationType = "Library Metadata Recalculation",
            StartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("🔄 Starting library metadata recalculation...");

            // Get all libraries
            var libraries = await _libraryRepository.GetAllAsync();
            result.TotalItems = libraries.Count();

            foreach (var library in libraries)
            {
                try
                {
                    _logger.LogDebug("📚 Recalculating metadata for library: {Name}", library.Name);

                    // Get all collections in this library
                    var collections = await _collectionRepository.GetByLibraryIdAsync(library.Id);
                    
                    // Calculate aggregated statistics
                    var totalCollections = collections.Count();
                    var totalMediaItems = collections.Sum(c => c.Statistics.TotalItems);
                    var totalSize = collections.Sum(c => c.Statistics.TotalSize);
                    var totalViews = collections.Sum(c => c.Statistics.TotalViews);
                    var totalDownloads = collections.Sum(c => c.Statistics.TotalDownloads);
                    var totalShares = collections.Sum(c => c.Statistics.TotalShares);
                    var totalLikes = collections.Sum(c => c.Statistics.TotalLikes);
                    var totalComments = collections.Sum(c => c.Statistics.TotalComments);
                    var lastActivity = collections
                        .Where(c => c.Statistics.LastActivity.HasValue)
                        .Max(c => c.Statistics.LastActivity);

                    // Create new statistics by using the existing methods
                    var newStatistics = new ImageViewer.Domain.ValueObjects.LibraryStatistics();
                    
                    // Update collections count
                    if (totalCollections > 0)
                    {
                        newStatistics.IncrementCollections(totalCollections);
                    }
                    
                    // Update media items count
                    if (totalMediaItems > 0)
                    {
                        newStatistics.IncrementMediaItems(totalMediaItems);
                    }
                    
                    // Update size
                    if (totalSize > 0)
                    {
                        newStatistics.IncrementSize(totalSize);
                    }
                    
                    // Update views
                    if (totalViews > 0)
                    {
                        for (int i = 0; i < totalViews; i++)
                        {
                            newStatistics.IncrementViews();
                        }
                    }
                    
                    // Update downloads
                    if (totalDownloads > 0)
                    {
                        for (int i = 0; i < totalDownloads; i++)
                        {
                            newStatistics.IncrementDownloads();
                        }
                    }
                    
                    // Update shares
                    if (totalShares > 0)
                    {
                        for (int i = 0; i < totalShares; i++)
                        {
                            newStatistics.IncrementShares();
                        }
                    }
                    
                    // Update likes
                    if (totalLikes > 0)
                    {
                        for (int i = 0; i < totalLikes; i++)
                        {
                            newStatistics.IncrementLikes();
                        }
                    }
                    
                    // Update comments
                    if (totalComments > 0)
                    {
                        for (int i = 0; i < totalComments; i++)
                        {
                            newStatistics.IncrementComments();
                        }
                    }
                    
                    // Preserve scan-related data
                    if (library.Statistics.LastScanDate.HasValue)
                    {
                        newStatistics.UpdateLastScanDate(library.Statistics.LastScanDate.Value);
                    }

                    // Update library with recalculated statistics
                    library.UpdateStatistics(newStatistics);
                    await _libraryRepository.UpdateAsync(library);
                    
                    result.ProcessedItems++;
                    _logger.LogDebug("✅ Updated library {Name}: {Collections} collections, {MediaItems} media items, {Size} bytes", 
                        library.Name, totalCollections, totalMediaItems, totalSize);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Error processing library {library.Name}: {ex.Message}");
                    _logger.LogError(ex, "❌ Error processing library {Name}", library.Name);
                }
            }

            result.CompletedAt = DateTime.UtcNow;
            result.Duration = stopwatch.Elapsed;
            result.Success = result.Errors.Count == 0;

            _logger.LogInformation("✅ Library metadata recalculation completed in {Duration}ms. " +
                "Processed: {Processed}/{Total}, Errors: {ErrorCount}",
                result.Duration.TotalMilliseconds, result.ProcessedItems, result.TotalItems, result.Errors.Count);

            return result;
        }
        catch (Exception ex)
        {
            result.CompletedAt = DateTime.UtcNow;
            result.Duration = stopwatch.Elapsed;
            result.Success = false;
            result.Errors.Add($"Fatal error: {ex.Message}");
            
            _logger.LogError(ex, "❌ Fatal error during library metadata recalculation");
            return result;
        }
    }

    /// <summary>
    /// Recalculates metadata for both cache folders and libraries
    /// </summary>
    public async Task<MetadataRecalculationResult> RecalculateAllMetadataAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MetadataRecalculationResult
        {
            OperationType = "Complete Metadata Recalculation",
            StartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("🔄 Starting complete metadata recalculation...");

            // First recalculate cache folder metadata
            var cacheFolderResult = await RecalculateCacheFolderMetadataAsync(cancellationToken);
            result.ProcessedItems += cacheFolderResult.ProcessedItems;
            result.Errors.AddRange(cacheFolderResult.Errors);

            // Then recalculate library metadata
            var libraryResult = await RecalculateLibraryMetadataAsync(cancellationToken);
            result.ProcessedItems += libraryResult.ProcessedItems;
            result.Errors.AddRange(libraryResult.Errors);

            result.TotalItems = cacheFolderResult.TotalItems + libraryResult.TotalItems;
            result.CompletedAt = DateTime.UtcNow;
            result.Duration = stopwatch.Elapsed;
            result.Success = result.Errors.Count == 0;

            _logger.LogInformation("✅ Complete metadata recalculation finished in {Duration}ms. " +
                "Processed: {Processed}/{Total}, Errors: {ErrorCount}",
                result.Duration.TotalMilliseconds, result.ProcessedItems, result.TotalItems, result.Errors.Count);

            return result;
        }
        catch (Exception ex)
        {
            result.CompletedAt = DateTime.UtcNow;
            result.Duration = stopwatch.Elapsed;
            result.Success = false;
            result.Errors.Add($"Fatal error: {ex.Message}");
            
            _logger.LogError(ex, "❌ Fatal error during complete metadata recalculation");
            return result;
        }
    }

    private async Task<long> CalculateDiskUsageAsync(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                _logger.LogWarning("⚠️ Cache folder path does not exist: {Path}", path);
                return 0;
            }

            var totalSize = await Task.Run(() => 
                Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                    .Sum(file => new FileInfo(file).Length));
            
            return totalSize;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error calculating disk usage for path: {Path}", path);
            return 0;
        }
    }

    private async Task<IEnumerable<Collection>> GetCollectionsWithCacheInFolderAsync(ObjectId cacheFolderId)
    {
        try
        {
            // Get the cache folder to get its path
            var cacheFolder = await _cacheFolderRepository.GetByIdAsync(cacheFolderId);
            if (cacheFolder == null)
            {
                _logger.LogWarning("Cache folder {CacheFolderId} not found", cacheFolderId);
                return Enumerable.Empty<Collection>();
            }

            // Get all collections
            var allCollections = await _collectionRepository.GetAllAsync();
            
            // Filter collections that have cache bindings to this specific cache folder path
            return allCollections.Where(c => 
                c.CacheBindings?.Any(cb => cb.CacheFolder == cacheFolder.Path) == true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting collections with cache in folder {CacheFolderId}", cacheFolderId);
            return Enumerable.Empty<Collection>();
        }
    }
}

