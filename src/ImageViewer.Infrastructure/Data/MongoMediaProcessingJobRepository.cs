using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

public class MongoMediaProcessingJobRepository : MongoRepository<MediaProcessingJob>, IMediaProcessingJobRepository
{
    public MongoMediaProcessingJobRepository(IMongoDatabase database, ILogger<MongoMediaProcessingJobRepository> logger)
        : base(database.GetCollection<MediaProcessingJob>("mediaProcessingJobs"), logger)
    {
    }

    public async Task<IEnumerable<MediaProcessingJob>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(job => job.Status == status)
                .SortByDescending(job => job.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get media processing jobs for status {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<MediaProcessingJob>> GetByJobTypeAsync(string jobType, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(job => job.JobType == jobType)
                .SortByDescending(job => job.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get media processing jobs for type {JobType}", jobType);
            throw;
        }
    }

    public async Task<IEnumerable<MediaProcessingJob>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(job => job.UserId == userId)
                .SortByDescending(job => job.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get media processing jobs for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<MediaProcessingJob>> GetByPriorityAsync(int priority, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(job => job.Priority == priority)
                .SortByDescending(job => job.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get media processing jobs for priority {Priority}", priority);
            throw;
        }
    }

    public async Task<IEnumerable<MediaProcessingJob>> GetPendingJobsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(job => job.Status == "Pending")
                .SortBy(job => job.Priority)
                .ThenBy(job => job.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get pending media processing jobs");
            throw;
        }
    }

    public async Task<IEnumerable<MediaProcessingJob>> GetFailedJobsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _collection.Find(job => job.Status == "Failed")
                .SortByDescending(job => job.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to get failed media processing jobs");
            throw;
        }
    }
}
