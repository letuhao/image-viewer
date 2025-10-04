using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service interface for MediaItem operations
/// </summary>
public interface IMediaItemService
{
    #region MediaItem Management
    
    Task<MediaItem> CreateMediaItemAsync(ObjectId collectionId, string name, string filename, string path, 
        string type, string format, long fileSize, int width, int height, TimeSpan? duration = null);
    Task<MediaItem> GetMediaItemByIdAsync(ObjectId mediaItemId);
    Task<MediaItem> GetMediaItemByPathAsync(string path);
    Task<IEnumerable<MediaItem>> GetMediaItemsByCollectionIdAsync(ObjectId collectionId);
    Task<IEnumerable<MediaItem>> GetMediaItemsAsync(int page = 1, int pageSize = 20);
    Task<MediaItem> UpdateMediaItemAsync(ObjectId mediaItemId, UpdateMediaItemRequest request);
    Task DeleteMediaItemAsync(ObjectId mediaItemId);
    
    #endregion
    
    #region MediaItem Metadata Management
    
    Task<MediaItem> UpdateMetadataAsync(ObjectId mediaItemId, UpdateMediaItemMetadataRequest request);
    Task<MediaItem> UpdateCacheInfoAsync(ObjectId mediaItemId, UpdateCacheInfoRequest request);
    Task<MediaItem> UpdateStatisticsAsync(ObjectId mediaItemId, UpdateMediaItemStatisticsRequest request);
    
    #endregion
    
    #region MediaItem Status Management
    
    Task<MediaItem> ActivateMediaItemAsync(ObjectId mediaItemId);
    Task<MediaItem> DeactivateMediaItemAsync(ObjectId mediaItemId);
    
    #endregion
    
    #region MediaItem Search and Filtering
    
    Task<IEnumerable<MediaItem>> SearchMediaItemsAsync(string query, int page = 1, int pageSize = 20);
    Task<IEnumerable<MediaItem>> GetMediaItemsByFilterAsync(MediaItemFilterRequest filter, int page = 1, int pageSize = 20);
    Task<IEnumerable<MediaItem>> GetMediaItemsByCollectionAsync(ObjectId collectionId, int page = 1, int pageSize = 20);
    Task<IEnumerable<MediaItem>> GetMediaItemsByTypeAsync(string type, int page = 1, int pageSize = 20);
    Task<IEnumerable<MediaItem>> GetMediaItemsByFormatAsync(string format, int page = 1, int pageSize = 20);
    
    #endregion
    
    #region MediaItem Statistics
    
    Task<MediaItemStatistics> GetMediaItemStatisticsAsync();
    Task<IEnumerable<MediaItem>> GetTopMediaItemsByActivityAsync(int limit = 10);
    Task<IEnumerable<MediaItem>> GetRecentMediaItemsAsync(int limit = 10);
    
    #endregion
}

/// <summary>
/// Request model for updating media item information
/// </summary>
public class UpdateMediaItemRequest
{
    public string? Name { get; set; }
    public string? Filename { get; set; }
    public string? Path { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public TimeSpan? Duration { get; set; }
}

/// <summary>
/// Request model for updating media item metadata
/// </summary>
public class UpdateMediaItemMetadataRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? Categories { get; set; }
    public Dictionary<string, object>? CustomFields { get; set; }
    public Dictionary<string, object>? ExifData { get; set; }
    public string? ColorProfile { get; set; }
    public int? BitDepth { get; set; }
    public string? Compression { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public CameraInfo? CameraInfo { get; set; }
}

/// <summary>
/// Request model for updating cache info
/// </summary>
public class UpdateCacheInfoRequest
{
    public bool? IsCached { get; set; }
    public string? CachePath { get; set; }
    public long? CacheSize { get; set; }
    public string? CacheFormat { get; set; }
    public int? CacheQuality { get; set; }
    public int? CacheWidth { get; set; }
    public int? CacheHeight { get; set; }
    public int? CompressionLevel { get; set; }
}

/// <summary>
/// Request model for updating media item statistics
/// </summary>
public class UpdateMediaItemStatisticsRequest
{
    public long? TotalViews { get; set; }
    public long? TotalDownloads { get; set; }
    public long? TotalShares { get; set; }
    public long? TotalLikes { get; set; }
    public long? TotalComments { get; set; }
    public long? TotalRatings { get; set; }
    public double? AverageRating { get; set; }
    public long? TotalRatingSum { get; set; }
    public DateTime? LastViewed { get; set; }
    public DateTime? LastDownloaded { get; set; }
    public DateTime? LastShared { get; set; }
    public DateTime? LastLiked { get; set; }
    public DateTime? LastCommented { get; set; }
    public DateTime? LastRated { get; set; }
}

/// <summary>
/// Request model for filtering media items
/// </summary>
public class MediaItemFilterRequest
{
    public ObjectId? CollectionId { get; set; }
    public string? Type { get; set; }
    public string? Format { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public DateTime? LastActivityAfter { get; set; }
    public DateTime? LastActivityBefore { get; set; }
    public string? Path { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? Categories { get; set; }
    public int? MinWidth { get; set; }
    public int? MaxWidth { get; set; }
    public int? MinHeight { get; set; }
    public int? MaxHeight { get; set; }
    public long? MinFileSize { get; set; }
    public long? MaxFileSize { get; set; }
}
