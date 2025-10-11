using ImageViewer.Domain.Entities;
using MongoDB.Bson;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for ScheduledJobRun entity
/// 定时任务运行历史仓储接口 - Interface repository cho lịch sử chạy công việc
/// </summary>
public interface IScheduledJobRunRepository : IRepository<ScheduledJobRun>
{
    /// <summary>
    /// Get job run history for a scheduled job
    /// 获取定时任务的运行历史 - Lấy lịch sử chạy của công việc
    /// </summary>
    Task<IEnumerable<ScheduledJobRun>> GetJobRunHistoryAsync(ObjectId scheduledJobId, int limit = 50);
    
    /// <summary>
    /// Get recent job runs (all jobs)
    /// 获取最近的任务运行记录 - Lấy các lần chạy gần đây
    /// </summary>
    Task<IEnumerable<ScheduledJobRun>> GetRecentRunsAsync(int limit = 100);
    
    /// <summary>
    /// Get failed job runs for a specific job
    /// 获取失败的任务运行记录 - Lấy các lần chạy thất bại
    /// </summary>
    Task<IEnumerable<ScheduledJobRun>> GetFailedRunsAsync(ObjectId scheduledJobId, int limit = 20);
    
    /// <summary>
    /// Get currently running jobs
    /// 获取正在运行的任务 - Lấy công việc đang chạy
    /// </summary>
    Task<IEnumerable<ScheduledJobRun>> GetRunningJobsAsync();
    
    /// <summary>
    /// Delete old job run history (cleanup)
    /// 删除旧的运行历史 - Xóa lịch sử cũ
    /// </summary>
    Task<int> DeleteOldRunsAsync(DateTime olderThan);
}

