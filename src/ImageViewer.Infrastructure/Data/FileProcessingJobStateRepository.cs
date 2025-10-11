using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB repository implementation for FileProcessingJobState entity
/// </summary>
public class FileProcessingJobStateRepository : MongoRepository<FileProcessingJobState>, IFileProcessingJobStateRepository
{
    public FileProcessingJobStateRepository(
        IMongoDatabase database,
        ILogger<FileProcessingJobStateRepository> logger)
        : base(database, "file_processing_job_states")
    {
        // Create indexes for optimal query performance
        // Note: MongoDB will create these indexes automatically on first use
        // We define them here for documentation and can manually create via MongoDB shell if needed
        
        // Indexes to create manually or via migration:
        // db.file_processing_job_states.createIndex({ "jobId": 1 }, { unique: true })
        // db.file_processing_job_states.createIndex({ "jobType": 1 })
        // db.file_processing_job_states.createIndex({ "collectionId": 1 })
        // db.file_processing_job_states.createIndex({ "status": 1 })
        // db.file_processing_job_states.createIndex({ "lastProgressAt": -1 })
        // db.file_processing_job_states.createIndex({ "jobType": 1, "status": 1 })
        // db.file_processing_job_states.createIndex({ "status": 1, "completedAt": -1 })
    }

    public async Task<FileProcessingJobState?> GetByJobIdAsync(string jobId)
    {
        var filter = Builders<FileProcessingJobState>.Filter.Eq(x => x.JobId, jobId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<FileProcessingJobState?> GetByCollectionIdAsync(string collectionId)
    {
        var filter = Builders<FileProcessingJobState>.Filter.Eq(x => x.CollectionId, collectionId);
        return await _collection.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<FileProcessingJobState>> GetByJobTypeAsync(string jobType)
    {
        var filter = Builders<FileProcessingJobState>.Filter.Eq(x => x.JobType, jobType);
        return await _collection.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<FileProcessingJobState>> GetIncompleteJobsAsync()
    {
        var filter = Builders<FileProcessingJobState>.Filter.And(
            Builders<FileProcessingJobState>.Filter.In(x => x.Status, new[] { "Pending", "Running", "Paused" }),
            Builders<FileProcessingJobState>.Filter.Eq(x => x.CanResume, true)
        );
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<FileProcessingJobState>> GetIncompleteJobsByTypeAsync(string jobType)
    {
        var filter = Builders<FileProcessingJobState>.Filter.And(
            Builders<FileProcessingJobState>.Filter.Eq(x => x.JobType, jobType),
            Builders<FileProcessingJobState>.Filter.In(x => x.Status, new[] { "Pending", "Running", "Paused" }),
            Builders<FileProcessingJobState>.Filter.Eq(x => x.CanResume, true)
        );
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<FileProcessingJobState>> GetPausedJobsAsync()
    {
        var filter = Builders<FileProcessingJobState>.Filter.Eq(x => x.Status, "Paused");
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<FileProcessingJobState>> GetStaleJobsAsync(TimeSpan stalePeriod)
    {
        var staleTime = DateTime.UtcNow.Subtract(stalePeriod);
        var filter = Builders<FileProcessingJobState>.Filter.And(
            Builders<FileProcessingJobState>.Filter.Eq(x => x.Status, "Running"),
            Builders<FileProcessingJobState>.Filter.Lt(x => x.LastProgressAt, staleTime)
        );
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<bool> IsImageProcessedAsync(string jobId, string imageId)
    {
        var filter = Builders<FileProcessingJobState>.Filter.And(
            Builders<FileProcessingJobState>.Filter.Eq(x => x.JobId, jobId),
            Builders<FileProcessingJobState>.Filter.Or(
                Builders<FileProcessingJobState>.Filter.AnyEq(x => x.ProcessedImageIds, imageId),
                Builders<FileProcessingJobState>.Filter.AnyEq(x => x.FailedImageIds, imageId)
            )
        );
        return await _collection.CountDocumentsAsync(filter) > 0;
    }

    public async Task<bool> AtomicIncrementCompletedAsync(string jobId, string imageId, long sizeBytes)
    {
        try
        {
            var filter = Builders<FileProcessingJobState>.Filter.And(
                Builders<FileProcessingJobState>.Filter.Eq(x => x.JobId, jobId),
                Builders<FileProcessingJobState>.Filter.Not(
                    Builders<FileProcessingJobState>.Filter.AnyEq(x => x.ProcessedImageIds, imageId)
                )
            );

            var update = Builders<FileProcessingJobState>.Update
                .AddToSet(x => x.ProcessedImageIds, imageId)
                .Inc(x => x.CompletedImages, 1)
                .Inc(x => x.TotalSizeBytes, sizeBytes)
                .Set(x => x.LastProgressAt, DateTime.UtcNow)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error atomically incrementing completed count for job {JobId}, image {ImageId}", jobId, imageId);
            return false;
        }
    }

    public async Task<bool> AtomicIncrementFailedAsync(string jobId, string imageId)
    {
        try
        {
            var filter = Builders<FileProcessingJobState>.Filter.And(
                Builders<FileProcessingJobState>.Filter.Eq(x => x.JobId, jobId),
                Builders<FileProcessingJobState>.Filter.Not(
                    Builders<FileProcessingJobState>.Filter.AnyEq(x => x.FailedImageIds, imageId)
                )
            );

            var update = Builders<FileProcessingJobState>.Update
                .AddToSet(x => x.FailedImageIds, imageId)
                .Inc(x => x.FailedImages, 1)
                .Set(x => x.LastProgressAt, DateTime.UtcNow)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error atomically incrementing failed count for job {JobId}, image {ImageId}", jobId, imageId);
            return false;
        }
    }

    public async Task<bool> AtomicIncrementSkippedAsync(string jobId, string imageId)
    {
        try
        {
            var filter = Builders<FileProcessingJobState>.Filter.And(
                Builders<FileProcessingJobState>.Filter.Eq(x => x.JobId, jobId),
                Builders<FileProcessingJobState>.Filter.Not(
                    Builders<FileProcessingJobState>.Filter.AnyEq(x => x.ProcessedImageIds, imageId)
                )
            );

            var update = Builders<FileProcessingJobState>.Update
                .AddToSet(x => x.ProcessedImageIds, imageId)
                .Inc(x => x.SkippedImages, 1)
                .Set(x => x.LastProgressAt, DateTime.UtcNow)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await _collection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error atomically incrementing skipped count for job {JobId}, image {ImageId}", jobId, imageId);
            return false;
        }
    }

    public async Task<bool> UpdateStatusAsync(string jobId, string status, string? errorMessage = null)
    {
        try
        {
            var filter = Builders<FileProcessingJobState>.Filter.Eq(x => x.JobId, jobId);
            
            var updateBuilder = Builders<FileProcessingJobState>.Update
                .Set(x => x.Status, status)
                .Set(x => x.LastProgressAt, DateTime.UtcNow)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            if (status == "Completed")
            {
                updateBuilder = updateBuilder
                    .Set(x => x.CompletedAt, DateTime.UtcNow)
                    .Set(x => x.CanResume, false);
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                updateBuilder = updateBuilder.Set(x => x.ErrorMessage, errorMessage);
            }

            var result = await _collection.UpdateOneAsync(filter, updateBuilder);
            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for job {JobId}", jobId);
            return false;
        }
    }

    public async Task<int> DeleteOldCompletedJobsAsync(DateTime olderThan)
    {
        try
        {
            var filter = Builders<FileProcessingJobState>.Filter.And(
                Builders<FileProcessingJobState>.Filter.Eq(x => x.Status, "Completed"),
                Builders<FileProcessingJobState>.Filter.Lt(x => x.CompletedAt, olderThan)
            );

            var result = await _collection.DeleteManyAsync(filter);
            return (int)result.DeletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting old completed jobs");
            return 0;
        }
    }
}

