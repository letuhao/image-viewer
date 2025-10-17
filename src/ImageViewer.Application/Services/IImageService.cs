using ImageViewer.Domain.Entities;
using ImageViewer.Domain.ValueObjects;
using MongoDB.Bson;

namespace ImageViewer.Application.Services;

/// <summary>
/// Image service interface - Updated for embedded design
/// </summary>
public interface IImageService
{
    // Embedded image operations
    Task<ImageEmbedded?> GetEmbeddedImageByIdAsync(string imageId, ObjectId collectionId, CancellationToken cancellationToken = default);
    Task<ImageEmbedded?> GetEmbeddedImageByFilenameAsync(string filename, ObjectId collectionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ImageEmbedded>> GetEmbeddedImagesByCollectionAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ImageEmbedded>> GetDisplayableImagesByCollectionAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ImageEmbedded>> GetEmbeddedImagesByFormatAsync(string format, CancellationToken cancellationToken = default);
    Task<IEnumerable<ImageEmbedded>> GetEmbeddedImagesBySizeRangeAsync(int minWidth, int minHeight, CancellationToken cancellationToken = default);
    Task<IEnumerable<ImageEmbedded>> GetLargeEmbeddedImagesAsync(long minSizeBytes, CancellationToken cancellationToken = default);
    Task<IEnumerable<ImageEmbedded>> GetHighResolutionEmbeddedImagesAsync(int minWidth, int minHeight, CancellationToken cancellationToken = default);
    
    // Random and navigation operations
    Task<ImageEmbedded?> GetRandomEmbeddedImageAsync(CancellationToken cancellationToken = default);
    Task<ImageEmbedded?> GetRandomEmbeddedImageByCollectionAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
    Task<ImageEmbedded?> GetNextEmbeddedImageAsync(string currentImageId, ObjectId collectionId, CancellationToken cancellationToken = default);
    Task<ImageEmbedded?> GetPreviousEmbeddedImageAsync(string currentImageId, ObjectId collectionId, CancellationToken cancellationToken = default);
    
    // Cross-collection navigation operations
    Task<CrossCollectionNavigationResult> GetCrossCollectionNavigationAsync(string currentImageId, ObjectId collectionId, string direction, string sortBy = "updatedAt", string sortDirection = "desc", CancellationToken cancellationToken = default);
    
    // File operations
    Task<byte[]?> GetImageFileAsync(string imageId, ObjectId collectionId, CancellationToken cancellationToken = default);
    Task<byte[]?> GetThumbnailAsync(string imageId, ObjectId collectionId, int? width = null, int? height = null, CancellationToken cancellationToken = default);
    Task<ThumbnailEmbedded?> GetThumbnailInfoAsync(string imageId, ObjectId collectionId, int? width = null, int? height = null, CancellationToken cancellationToken = default);
    Task<byte[]?> GetCachedImageAsync(string imageId, ObjectId collectionId, int? width = null, int? height = null, CancellationToken cancellationToken = default);
    
    // CRUD operations on embedded images
    Task<ImageEmbedded> CreateEmbeddedImageAsync(ObjectId collectionId, string filename, string relativePath, long fileSize, int width, int height, string format, ArchiveEntryInfo? archiveEntry = null, CancellationToken cancellationToken = default);
    Task UpdateEmbeddedImageMetadataAsync(string imageId, ObjectId collectionId, int width, int height, long fileSize, CancellationToken cancellationToken = default);
    Task DeleteEmbeddedImageAsync(string imageId, ObjectId collectionId, CancellationToken cancellationToken = default);
    Task RestoreEmbeddedImageAsync(string imageId, ObjectId collectionId, CancellationToken cancellationToken = default);
    
    // Statistics
    Task<long> GetTotalSizeByCollectionAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
    Task<int> GetCountByCollectionAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
    
    // Thumbnail and cache operations
    Task<ThumbnailEmbedded> GenerateThumbnailAsync(string imageId, ObjectId collectionId, int width, int height, CancellationToken cancellationToken = default);
    Task<ImageCacheInfoEmbedded> GenerateCacheAsync(string imageId, ObjectId collectionId, int width, int height, CancellationToken cancellationToken = default);
    Task CleanupExpiredThumbnailsAsync(CancellationToken cancellationToken = default);
    
}

/// <summary>
/// Result of cross-collection navigation
/// </summary>
public class CrossCollectionNavigationResult
{
    public string? TargetImageId { get; set; }
    public string? TargetCollectionId { get; set; }
    public bool IsCrossCollection { get; set; }
    public string? TargetCollectionName { get; set; }
    public int TargetImagePosition { get; set; }
    public int TotalImagesInTargetCollection { get; set; }
    public bool HasTarget { get; set; }
    public string? ErrorMessage { get; set; }
}

