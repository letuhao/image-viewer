using MongoDB.Bson;

namespace ImageViewer.Application.Services;

/// <summary>
/// Advanced thumbnail service interface
/// </summary>
public interface IAdvancedThumbnailService
{
    Task<string?> GenerateCollectionThumbnailAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
    Task<BatchThumbnailResult> BatchRegenerateThumbnailsAsync(IEnumerable<ObjectId> collectionIds, CancellationToken cancellationToken = default);
    Task<byte[]?> GetCollectionThumbnailAsync(ObjectId collectionId, int? width = null, int? height = null, CancellationToken cancellationToken = default);
    Task DeleteCollectionThumbnailAsync(ObjectId collectionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Batch thumbnail generation result
/// </summary>
public class BatchThumbnailResult
{
    public int Total { get; set; }
    public int Success { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<ObjectId> SuccessfulCollections { get; set; } = new();
    public List<ObjectId> FailedCollections { get; set; } = new();
}
