using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Domain.Enums;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for Collection operations
/// </summary>
public interface ICollectionRepository : IRepository<Collection>
{
    #region Query Methods
    
    Task<Collection> GetByPathAsync(string path);
    Task<IEnumerable<Collection>> GetByLibraryIdAsync(ObjectId libraryId);
    Task<IEnumerable<Collection>> GetActiveCollectionsAsync();
    Task<IEnumerable<Collection>> GetCollectionsByTypeAsync(CollectionType type);
    Task<long> GetCollectionCountAsync();
    Task<long> GetActiveCollectionCountAsync();
    
    #endregion
    
    #region Search Methods
    
    Task<IEnumerable<Collection>> SearchCollectionsAsync(string query);
    Task<IEnumerable<Collection>> GetCollectionsByFilterAsync(CollectionFilter filter);
    
    #endregion
    
    #region Statistics Methods
    
    Task<ValueObjects.CollectionStatistics> GetCollectionStatisticsAsync();
    Task<IEnumerable<Collection>> GetTopCollectionsByActivityAsync(int limit = 10);
    Task<IEnumerable<Collection>> GetRecentCollectionsAsync(int limit = 10);
    
    /// <summary>
    /// Get cache statistics using optimized aggregation pipeline
    /// 使用聚合管道获取缓存统计 - Lấy thống kê cache bằng aggregation
    /// </summary>
    Task<(int totalImages, int cachedImages, long totalCacheSize, int collectionsWithCache)> GetCacheStatisticsAsync();
    
    #endregion
    
    #region Atomic Array Operations (Thread-Safe)
    
    /// <summary>
    /// Atomically adds an image to the collection using MongoDB $push
    /// This is thread-safe and prevents race conditions
    /// </summary>
    Task<bool> AtomicAddImageAsync(ObjectId collectionId, ImageEmbedded image);
    
    /// <summary>
    /// Atomically adds a thumbnail to the collection using MongoDB $push
    /// This is thread-safe and prevents race conditions
    /// </summary>
    Task<bool> AtomicAddThumbnailAsync(ObjectId collectionId, ThumbnailEmbedded thumbnail);
    
    /// <summary>
    /// Atomically adds multiple thumbnails to the collection using MongoDB $push
    /// This is thread-safe and prevents race conditions for batch operations
    /// </summary>
    Task<bool> AtomicAddThumbnailsAsync(ObjectId collectionId, IEnumerable<ThumbnailEmbedded> thumbnails);
    
    /// <summary>
    /// Atomically adds a cache image to the collection using MongoDB $push
    /// This is thread-safe and prevents race conditions
    /// </summary>
    Task<bool> AtomicAddCacheImageAsync(ObjectId collectionId, CacheImageEmbedded cacheImage);
    
    /// <summary>
    /// Atomically adds multiple cache images to the collection using MongoDB $pushEach
    /// This is thread-safe and prevents race conditions for batch operations
    /// </summary>
    Task<bool> AtomicAddCacheImagesAsync(ObjectId collectionId, IEnumerable<CacheImageEmbedded> cacheImages);
    
    /// <summary>
    /// Clears all image arrays (Images, Thumbnails, CacheImages) for a collection
    /// Used for force rescan to start fresh
    /// </summary>
    Task ClearImageArraysAsync(ObjectId collectionId);
    
    /// <summary>
    /// Recalculates statistics for a specific collection based on its actual images
    /// </summary>
    Task RecalculateCollectionStatisticsAsync(ObjectId collectionId);
    
    /// <summary>
    /// Recalculates statistics for all collections
    /// </summary>
    Task RecalculateAllCollectionStatisticsAsync();
    
    #endregion
}

/// <summary>
/// Collection filter for advanced queries
/// </summary>
public class CollectionFilter
{
    public ObjectId? LibraryId { get; set; }
    public CollectionType? Type { get; set; }
    public bool? IsActive { get; set; }
    public string? Name { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public DateTime? LastActivityAfter { get; set; }
    public DateTime? LastActivityBefore { get; set; }
    public string? Path { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? Categories { get; set; }
}

/// <summary>
/// Collection statistics for reporting
/// </summary>
public class CollectionStatistics
{
    public long TotalCollections { get; set; }
    public long ActiveCollections { get; set; }
    public long NewCollectionsThisMonth { get; set; }
    public long NewCollectionsThisWeek { get; set; }
    public long NewCollectionsToday { get; set; }
    public Dictionary<ObjectId, long> CollectionsByLibrary { get; set; } = new();
    public Dictionary<CollectionType, long> CollectionsByType { get; set; } = new();
    public Dictionary<string, long> CollectionsByTag { get; set; } = new();
    public Dictionary<string, long> CollectionsByCategory { get; set; } = new();
}