using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoContentSimilarityRepository : MongoRepository<ContentSimilarity>, IContentSimilarityRepository
{
    public MongoContentSimilarityRepository(IMongoDatabase database, ILogger<MongoContentSimilarityRepository> logger)
        : base(database.GetCollection<ContentSimilarity>("contentSimilarities"), logger)
    {
    }

    public async Task<IEnumerable<ContentSimilarity>> GetBySourceIdAsync(ObjectId sourceId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(similarity => similarity.SourceId == sourceId)
                .SortByDescending(similarity => similarity.SimilarityScore)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get content similarities for source {SourceId}", sourceId);
            throw;
        }
    }

    public async Task<IEnumerable<ContentSimilarity>> GetByTargetIdAsync(ObjectId targetId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(similarity => similarity.TargetId == targetId)
                .SortByDescending(similarity => similarity.SimilarityScore)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get content similarities for target {TargetId}", targetId);
            throw;
        }
    }

    public async Task<IEnumerable<ContentSimilarity>> GetByAlgorithmAsync(string algorithm, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(similarity => similarity.Algorithm == algorithm)
                .SortByDescending(similarity => similarity.SimilarityScore)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get content similarities for algorithm {Algorithm}", algorithm);
            throw;
        }
    }

    public async Task<IEnumerable<ContentSimilarity>> GetBySimilarityThresholdAsync(double minThreshold, double maxThreshold, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<ContentSimilarity>.Filter.And(
                Builders<ContentSimilarity>.Filter.Gte(similarity => similarity.SimilarityScore, minThreshold),
                Builders<ContentSimilarity>.Filter.Lte(similarity => similarity.SimilarityScore, maxThreshold)
            );

            return await _collection.Find(filter)
                .SortByDescending(similarity => similarity.SimilarityScore)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get content similarities for threshold range {MinThreshold} to {MaxThreshold}", minThreshold, maxThreshold);
            throw;
        }
    }

    public async Task<IEnumerable<ContentSimilarity>> GetHighSimilarityAsync(double threshold = 0.8, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(similarity => similarity.SimilarityScore >= threshold)
                .SortByDescending(similarity => similarity.SimilarityScore)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get high similarity content similarities with threshold {Threshold}", threshold);
            throw;
        }
    }
}
