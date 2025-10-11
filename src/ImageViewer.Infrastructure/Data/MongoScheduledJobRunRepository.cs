using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB repository implementation for ScheduledJobRun entity
/// 定时任务运行历史MongoDB仓储实现 - Repository MongoDB cho lịch sử công việc
/// </summary>
public class MongoScheduledJobRunRepository : MongoRepository<ScheduledJobRun>, IScheduledJobRunRepository
{
    public MongoScheduledJobRunRepository(
        IMongoDatabase database,
        ILogger<MongoScheduledJobRunRepository> logger)
        : base(database, "scheduled_job_runs")
    {
        // Note: Indexes should be created manually or via migration
        // db.scheduled_job_runs.createIndex({ "scheduledJobId": 1, "startedAt": -1 })
        // db.scheduled_job_runs.createIndex({ "status": 1, "completedAt": -1 })
        // db.scheduled_job_runs.createIndex({ "startedAt": -1 })
    }

    public async Task<IEnumerable<ScheduledJobRun>> GetJobRunHistoryAsync(ObjectId scheduledJobId, int limit = 50)
    {
        var filter = Builders<ScheduledJobRun>.Filter.Eq(x => x.ScheduledJobId, scheduledJobId);
        
        return await _collection.Find(filter)
            .Sort(Builders<ScheduledJobRun>.Sort.Descending(x => x.StartedAt))
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<ScheduledJobRun>> GetRecentRunsAsync(int limit = 100)
    {
        return await _collection.Find(_ => true)
            .Sort(Builders<ScheduledJobRun>.Sort.Descending(x => x.StartedAt))
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<ScheduledJobRun>> GetFailedRunsAsync(ObjectId scheduledJobId, int limit = 20)
    {
        var filter = Builders<ScheduledJobRun>.Filter.And(
            Builders<ScheduledJobRun>.Filter.Eq(x => x.ScheduledJobId, scheduledJobId),
            Builders<ScheduledJobRun>.Filter.Eq(x => x.Status, "Failed")
        );
        
        return await _collection.Find(filter)
            .Sort(Builders<ScheduledJobRun>.Sort.Descending(x => x.StartedAt))
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<ScheduledJobRun>> GetRunningJobsAsync()
    {
        var filter = Builders<ScheduledJobRun>.Filter.Eq(x => x.Status, "Running");
        
        return await _collection.Find(filter)
            .Sort(Builders<ScheduledJobRun>.Sort.Descending(x => x.StartedAt))
            .ToListAsync();
    }

    public async Task<int> DeleteOldRunsAsync(DateTime olderThan)
    {
        var filter = Builders<ScheduledJobRun>.Filter.Lt(x => x.StartedAt, olderThan);
        
        var result = await _collection.DeleteManyAsync(filter);
        return (int)result.DeletedCount;
    }
}

