using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for MediaItem operations
/// </summary>
public interface IMediaItemRepository : IRepository<MediaItem>
{
    #region Query Methods
    
    Task<MediaItem> GetByPathAsync(string path);
    Task<IEnumerable<MediaItem>> GetByCollectionIdAsync(ObjectId collectionId);
    Task<IEnumerable<MediaItem>> GetActiveMediaItemsAsync();
    Task<IEnumerable<MediaItem>> GetMediaItemsByTypeAsync(string type);
    Task<IEnumerable<MediaItem>> GetMediaItemsByFormatAsync(string format);
    Task<long> GetMediaItemCountAsync();
    Task<long> GetActiveMediaItemCountAsync();
    
    #endregion
    
    #region Search Methods
    
    Task<IEnumerable<MediaItem>> SearchMediaItemsAsync(string query);
    Task<IEnumerable<MediaItem>> GetMediaItemsByFilterAsync(MediaItemFilter filter);
    
    #endregion
    
    #region Statistics Methods
    
    Task<ValueObjects.MediaItemStatistics> GetMediaItemStatisticsAsync();
    Task<IEnumerable<MediaItem>> GetTopMediaItemsByActivityAsync(int limit = 10);
    Task<IEnumerable<MediaItem>> GetRecentMediaItemsAsync(int limit = 10);
    
    #endregion
}

/// <summary>
/// MediaItem filter for advanced queries
/// </summary>
public class MediaItemFilter
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

/// <summary>
/// MediaItem statistics for reporting
/// </summary>
public class MediaItemStatistics
{
    public long TotalMediaItems { get; set; }
    public long ActiveMediaItems { get; set; }
    public long NewMediaItemsThisMonth { get; set; }
    public long NewMediaItemsThisWeek { get; set; }
    public long NewMediaItemsToday { get; set; }
    public Dictionary<ObjectId, long> MediaItemsByCollection { get; set; } = new();
    public Dictionary<string, long> MediaItemsByType { get; set; } = new();
    public Dictionary<string, long> MediaItemsByFormat { get; set; } = new();
    public Dictionary<string, long> MediaItemsByTag { get; set; } = new();
    public Dictionary<string, long> MediaItemsByCategory { get; set; } = new();
    public long TotalFileSize { get; set; }
    public double AverageFileSize { get; set; }
}
