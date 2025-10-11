using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB repository implementation for ScheduledJob entity
/// 定时任务MongoDB仓储实现 - Repository MongoDB cho công việc định kỳ
/// </summary>
public class MongoScheduledJobRepository : MongoRepository<ScheduledJob>, IScheduledJobRepository
{
    public MongoScheduledJobRepository(
        IMongoDatabase database,
        ILogger<MongoScheduledJobRepository> logger)
        : base(database, "scheduled_jobs")
    {
        // Note: Indexes should be created manually or via migration
        // db.scheduled_jobs.createIndex({ "isEnabled": 1, "nextRunAt": 1 })
        // db.scheduled_jobs.createIndex({ "jobType": 1 })
        // db.scheduled_jobs.createIndex({ "hangfireJobId": 1 }, { unique: true, sparse: true })
    }

    public async Task<IEnumerable<ScheduledJob>> GetEnabledJobsAsync()
    {
        var filter = Builders<ScheduledJob>.Filter.And(
            Builders<ScheduledJob>.Filter.Eq(x => x.IsEnabled, true),
            Builders<ScheduledJob>.Filter.Eq(x => x.IsDeleted, false)
        );
        
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<ScheduledJob>> GetJobsByTypeAsync(string jobType)
    {
        var filter = Builders<ScheduledJob>.Filter.And(
            Builders<ScheduledJob>.Filter.Eq(x => x.JobType, jobType),
            Builders<ScheduledJob>.Filter.Eq(x => x.IsDeleted, false)
        );
        
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<ScheduledJob>> GetDueJobsAsync(DateTime now)
    {
        var filter = Builders<ScheduledJob>.Filter.And(
            Builders<ScheduledJob>.Filter.Eq(x => x.IsEnabled, true),
            Builders<ScheduledJob>.Filter.Eq(x => x.IsDeleted, false),
            Builders<ScheduledJob>.Filter.Lte(x => x.NextRunAt, now)
        );
        
        return await _collection.Find(filter)
            .Sort(Builders<ScheduledJob>.Sort.Ascending(x => x.Priority).Descending(x => x.NextRunAt))
            .ToListAsync();
    }

    public async Task<ScheduledJob?> GetByHangfireJobIdAsync(string hangfireJobId)
    {
        var filter = Builders<ScheduledJob>.Filter.Eq(x => x.HangfireJobId, hangfireJobId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateNextRunTimeAsync(ObjectId jobId, DateTime nextRunTime)
    {
        var filter = Builders<ScheduledJob>.Filter.Eq(x => x.Id, jobId);
        var update = Builders<ScheduledJob>.Update
            .Set(x => x.NextRunAt, nextRunTime)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        
        var result = await _collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> RecordJobRunAsync(
        ObjectId jobId, 
        DateTime startTime, 
        DateTime endTime, 
        string status, 
        string? errorMessage = null)
    {
        var filter = Builders<ScheduledJob>.Filter.Eq(x => x.Id, jobId);
        
        var updateBuilder = Builders<ScheduledJob>.Update
            .Set(x => x.LastRunAt, startTime)
            .Set(x => x.LastRunDuration, endTime - startTime)
            .Set(x => x.LastRunStatus, status)
            .Set(x => x.LastErrorMessage, errorMessage)
            .Inc(x => x.RunCount, 1)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        
        // Increment success or failure count
        if (status == "Completed" || status == "Succeeded")
        {
            updateBuilder = updateBuilder.Inc(x => x.SuccessCount, 1);
        }
        else if (status == "Failed")
        {
            updateBuilder = updateBuilder.Inc(x => x.FailureCount, 1);
        }
        
        var result = await _collection.UpdateOneAsync(filter, updateBuilder);
        return result.ModifiedCount > 0;
    }
}

