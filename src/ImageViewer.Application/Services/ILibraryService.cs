using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service interface for Library operations
/// </summary>
public interface ILibraryService
{
    #region Library Management
    
    Task<Library> CreateLibraryAsync(string name, string path, ObjectId ownerId, string description = "", bool autoScan = false);
    Task<Library> GetLibraryByIdAsync(ObjectId libraryId);
    Task<Library> GetLibraryByPathAsync(string path);
    Task<IEnumerable<Library>> GetLibrariesByOwnerIdAsync(ObjectId ownerId);
    Task<IEnumerable<Library>> GetPublicLibrariesAsync();
    Task<IEnumerable<Library>> GetLibrariesAsync(int page = 1, int pageSize = 20);
    Task<Library> UpdateLibraryAsync(ObjectId libraryId, UpdateLibraryRequest request);
    Task DeleteLibraryAsync(ObjectId libraryId);
    
    #endregion
    
    #region Library Settings Management
    
    Task<Library> UpdateSettingsAsync(ObjectId libraryId, UpdateLibrarySettingsRequest request);
    Task<Library> UpdateMetadataAsync(ObjectId libraryId, UpdateLibraryMetadataRequest request);
    Task<Library> UpdateStatisticsAsync(ObjectId libraryId, UpdateLibraryStatisticsRequest request);
    
    #endregion
    
    #region Library Status Management
    
    Task<Library> ActivateLibraryAsync(ObjectId libraryId);
    Task<Library> DeactivateLibraryAsync(ObjectId libraryId);
    Task<Library> SetPublicAsync(ObjectId libraryId, bool isPublic);
    
    #endregion
    
    #region Library Watching Management
    
    Task<Library> EnableWatchingAsync(ObjectId libraryId);
    Task<Library> DisableWatchingAsync(ObjectId libraryId);
    Task<Library> UpdateWatchSettingsAsync(ObjectId libraryId, UpdateWatchSettingsRequest request);
    
    #endregion
    
    #region Library Search and Filtering
    
    Task<IEnumerable<Library>> SearchLibrariesAsync(string query, int page = 1, int pageSize = 20);
    Task<IEnumerable<Library>> GetLibrariesByFilterAsync(LibraryFilterRequest filter, int page = 1, int pageSize = 20);
    
    #endregion
    
    #region Library Statistics
    
    Task<LibraryStatistics> GetLibraryStatisticsAsync();
    Task<IEnumerable<Library>> GetTopLibrariesByActivityAsync(int limit = 10);
    Task<IEnumerable<Library>> GetRecentLibrariesAsync(int limit = 10);
    Task<IEnumerable<Library>> GetLibrariesByOwnerAsync(ObjectId ownerId, int page = 1, int pageSize = 20);
    
    #endregion
}

/// <summary>
/// Request model for updating library information
/// </summary>
public class UpdateLibraryRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Path { get; set; }
}

/// <summary>
/// Request model for updating library settings
/// </summary>
public class UpdateLibrarySettingsRequest
{
    public bool? AutoScan { get; set; }
    public bool? GenerateThumbnails { get; set; }
    public bool? GenerateCache { get; set; }
    public bool? EnableWatching { get; set; }
    public int? ScanInterval { get; set; }
    public long? MaxFileSize { get; set; }
    public List<string>? AllowedFormats { get; set; }
    public List<string>? ExcludedPaths { get; set; }
    public ThumbnailSettings? ThumbnailSettings { get; set; }
    public CacheSettings? CacheSettings { get; set; }
}

/// <summary>
/// Request model for updating library metadata
/// </summary>
public class UpdateLibraryMetadataRequest
{
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? Categories { get; set; }
    public Dictionary<string, object>? CustomFields { get; set; }
    public string? Version { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// Request model for updating library statistics
/// </summary>
public class UpdateLibraryStatisticsRequest
{
    public long? TotalCollections { get; set; }
    public long? TotalMediaItems { get; set; }
    public long? TotalSize { get; set; }
    public long? TotalViews { get; set; }
    public long? TotalDownloads { get; set; }
    public long? TotalShares { get; set; }
    public long? TotalLikes { get; set; }
    public long? TotalComments { get; set; }
    public DateTime? LastScanDate { get; set; }
    public long? ScanCount { get; set; }
    public DateTime? LastActivity { get; set; }
}

/// <summary>
/// Request model for updating watch settings
/// </summary>
public class UpdateWatchSettingsRequest
{
    public bool? IsWatching { get; set; }
    public string? WatchPath { get; set; }
    public List<string>? WatchFilters { get; set; }
}

/// <summary>
/// Request model for filtering libraries
/// </summary>
public class LibraryFilterRequest
{
    public ObjectId? OwnerId { get; set; }
    public bool? IsPublic { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public DateTime? LastActivityAfter { get; set; }
    public DateTime? LastActivityBefore { get; set; }
    public string? Path { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? Categories { get; set; }
}
