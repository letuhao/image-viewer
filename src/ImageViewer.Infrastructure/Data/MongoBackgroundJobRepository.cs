using MongoDB.Driver;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Enums;
using MongoDB.Bson;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB implementation of background job repository
/// </summary>
public class MongoBackgroundJobRepository : MongoRepository<BackgroundJob>, IBackgroundJobRepository
{
    public MongoBackgroundJobRepository(IMongoDatabase database) : base(database, "background_jobs")
    {
    }

    /// <summary>
    /// Get jobs by status
    /// </summary>
    public async Task<IEnumerable<BackgroundJob>> GetByStatusAsync(JobStatus status)
    {
        var filter = Builders<BackgroundJob>.Filter.Eq(x => x.Status, status.ToString());
        var sort = Builders<BackgroundJob>.Sort.Descending(x => x.CreatedAt);
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }

    /// <summary>
    /// Get jobs by type
    /// </summary>
    public async Task<IEnumerable<BackgroundJob>> GetByTypeAsync(string jobType)
    {
        var filter = Builders<BackgroundJob>.Filter.Eq(x => x.JobType, jobType);
        var sort = Builders<BackgroundJob>.Sort.Descending(x => x.CreatedAt);
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }

    /// <summary>
    /// Get running jobs
    /// </summary>
    public async Task<IEnumerable<BackgroundJob>> GetRunningJobsAsync()
    {
        var filter = Builders<BackgroundJob>.Filter.Eq(x => x.Status, JobStatus.Running.ToString());
        var sort = Builders<BackgroundJob>.Sort.Ascending(x => x.CreatedAt);
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }

    /// <summary>
    /// Get jobs by date range
    /// </summary>
    public async Task<IEnumerable<BackgroundJob>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        var filter = Builders<BackgroundJob>.Filter.And(
            Builders<BackgroundJob>.Filter.Gte(x => x.CreatedAt, fromDate),
            Builders<BackgroundJob>.Filter.Lte(x => x.CreatedAt, toDate)
        );
        var sort = Builders<BackgroundJob>.Sort.Descending(x => x.CreatedAt);
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }

    /// <summary>
    /// Get jobs older than specified date
    /// </summary>
    public async Task<IEnumerable<BackgroundJob>> GetOlderThanAsync(DateTime date)
    {
        var filter = Builders<BackgroundJob>.Filter.Lt(x => x.CreatedAt, date);
        var sort = Builders<BackgroundJob>.Sort.Ascending(x => x.CreatedAt);
        return await _collection.Find(filter).Sort(sort).ToListAsync();
    }

    /// <summary>
    /// Get job statistics
    /// </summary>
    public async Task<Dictionary<JobStatus, int>> GetJobCountsByStatusAsync()
    {
        var pipeline = new BsonDocument[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$Status" },
                { "count", new BsonDocument("$sum", 1) }
            })
        };

        var results = await _collection.Aggregate<BsonDocument>(pipeline).ToListAsync();
        
        var counts = new Dictionary<JobStatus, int>();
        foreach (var result in results)
        {
            if (result.TryGetValue("_id", out var statusValue) && 
                result.TryGetValue("count", out var countValue))
            {
                if (Enum.TryParse<JobStatus>(statusValue.ToString(), out var status))
                {
                    counts[status] = countValue.ToInt32();
                }
            }
        }

        return counts;
    }
}
