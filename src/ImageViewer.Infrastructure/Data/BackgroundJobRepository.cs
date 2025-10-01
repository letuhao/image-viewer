using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// Background job repository implementation
/// </summary>
public class BackgroundJobRepository : Repository<BackgroundJob>, IBackgroundJobRepository
{
    public BackgroundJobRepository(ImageViewerDbContext context, ILogger<BackgroundJobRepository> logger) : base(context, logger)
    {
    }

    public async Task<IEnumerable<BackgroundJob>> GetByStatusAsync(JobStatus status)
    {
        try
        {
            return await _dbSet
                .Where(j => j.Status == status.ToString())
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs by status {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<BackgroundJob>> GetByTypeAsync(string jobType)
    {
        try
        {
            return await _dbSet
                .Where(j => j.JobType == jobType)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs by type {JobType}", jobType);
            throw;
        }
    }

    public async Task<IEnumerable<BackgroundJob>> GetRunningJobsAsync()
    {
        try
        {
            return await _dbSet
                .Where(j => j.Status == JobStatus.Running.ToString())
                .OrderBy(j => j.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting running jobs");
            throw;
        }
    }

    public async Task<IEnumerable<BackgroundJob>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            return await _dbSet
                .Where(j => j.CreatedAt >= fromDate && j.CreatedAt <= toDate)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs by date range {FromDate} to {ToDate}", fromDate, toDate);
            throw;
        }
    }

    public async Task<IEnumerable<BackgroundJob>> GetOlderThanAsync(DateTime date)
    {
        try
        {
            return await _dbSet
                .Where(j => j.CreatedAt < date)
                .OrderBy(j => j.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs older than {Date}", date);
            throw;
        }
    }

    public async Task<Dictionary<JobStatus, int>> GetJobCountsByStatusAsync()
    {
        try
        {
            var statusCounts = await _dbSet
                .GroupBy(j => j.Status)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            var result = new Dictionary<JobStatus, int>();
            foreach (var kvp in statusCounts)
            {
                if (Enum.TryParse<JobStatus>(kvp.Key, out var status))
                {
                    result[status] = kvp.Value;
                }
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job counts by status");
            throw;
        }
    }
}
