using ImageViewer.Domain.Entities;
using MongoDB.Bson;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for ScheduledJob entity
/// 定时任务仓储接口 - Interface repository cho công việc định kỳ
/// </summary>
public interface IScheduledJobRepository : IRepository<ScheduledJob>
{
    /// <summary>
    /// Get all enabled scheduled jobs
    /// 获取所有已启用的定时任务 - Lấy tất cả công việc định kỳ đã bật
    /// </summary>
    Task<IEnumerable<ScheduledJob>> GetEnabledJobsAsync();
    
    /// <summary>
    /// Get scheduled jobs by type
    /// 按类型获取定时任务 - Lấy công việc theo loại
    /// </summary>
    Task<IEnumerable<ScheduledJob>> GetJobsByTypeAsync(string jobType);
    
    /// <summary>
    /// Get jobs due for execution (NextRunAt <= now AND IsEnabled = true)
    /// 获取到期需要执行的任务 - Lấy công việc cần thực thi
    /// </summary>
    Task<IEnumerable<ScheduledJob>> GetDueJobsAsync(DateTime now);
    
    /// <summary>
    /// Get scheduled job by Hangfire job ID
    /// 通过Hangfire任务ID获取定时任务 - Lấy công việc theo Hangfire ID
    /// </summary>
    Task<ScheduledJob?> GetByHangfireJobIdAsync(string hangfireJobId);
    
    /// <summary>
    /// Update next run time for a job
    /// 更新下次运行时间 - Cập nhật thời gian chạy tiếp theo
    /// </summary>
    Task<bool> UpdateNextRunTimeAsync(ObjectId jobId, DateTime nextRunTime);
    
    /// <summary>
    /// Record job execution result
    /// 记录任务执行结果 - Ghi lại kết quả thực thi
    /// </summary>
    Task<bool> RecordJobRunAsync(ObjectId jobId, DateTime startTime, DateTime endTime, string status, string? errorMessage = null);
}

