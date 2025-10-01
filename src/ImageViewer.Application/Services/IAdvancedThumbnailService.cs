using ImageViewer.Domain.Entities;

namespace ImageViewer.Application.Services;

/// <summary>
/// Advanced thumbnail service interface
/// </summary>
public interface IAdvancedThumbnailService
{
    Task<string?> GenerateCollectionThumbnailAsync(Guid collectionId, CancellationToken cancellationToken = default);
    Task<BatchThumbnailResult> BatchRegenerateThumbnailsAsync(IEnumerable<Guid> collectionIds, CancellationToken cancellationToken = default);
    Task<byte[]?> GetCollectionThumbnailAsync(Guid collectionId, int? width = null, int? height = null, CancellationToken cancellationToken = default);
    Task DeleteCollectionThumbnailAsync(Guid collectionId, CancellationToken cancellationToken = default);
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
    public List<Guid> SuccessfulCollections { get; set; } = new();
    public List<Guid> FailedCollections { get; set; } = new();
}
