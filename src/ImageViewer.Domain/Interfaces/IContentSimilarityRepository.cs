using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for ContentSimilarity entity
/// </summary>
public interface IContentSimilarityRepository : IRepository<ContentSimilarity>
{
    Task<IEnumerable<ContentSimilarity>> GetBySourceIdAsync(ObjectId sourceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ContentSimilarity>> GetByTargetIdAsync(ObjectId targetId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ContentSimilarity>> GetByAlgorithmAsync(string algorithm, CancellationToken cancellationToken = default);
    Task<IEnumerable<ContentSimilarity>> GetBySimilarityThresholdAsync(double minThreshold, double maxThreshold, CancellationToken cancellationToken = default);
    Task<IEnumerable<ContentSimilarity>> GetHighSimilarityAsync(double threshold = 0.8, CancellationToken cancellationToken = default);
}
