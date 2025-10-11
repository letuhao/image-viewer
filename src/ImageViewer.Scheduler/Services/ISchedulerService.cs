using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using MongoDB.Bson;

namespace ImageViewer.Scheduler.Services;

/// <summary>
/// Scheduler service interface
/// 调度服务接口 - Interface dịch vụ lập lịch
/// </summary>
public interface ISchedulerService
{
    /// <summary>
    /// Create a new scheduled job
    /// 创建新的定时任务 - Tạo công việc định kỳ mới
    /// </summary>
    Task<ScheduledJob> CreateScheduledJobAsync(ScheduledJob job);
    
    /// <summary>
    /// Enable a scheduled job (registers with Hangfire)
    /// 启用定时任务 - Kích hoạt công việc định kỳ
    /// </summary>
    Task<bool> EnableJobAsync(ObjectId jobId);
    
    /// <summary>
    /// Disable a scheduled job (removes from Hangfire)
    /// 禁用定时任务 - Vô hiệu hóa công việc định kỳ
    /// </summary>
    Task<bool> DisableJobAsync(ObjectId jobId);
    
    /// <summary>
    /// Calculate next run time based on cron expression or interval
    /// 计算下次运行时间 - Tính thời gian chạy tiếp theo
    /// </summary>
    Task<DateTime?> CalculateNextRunTimeAsync(string? cronExpression, int? intervalMinutes = null, ScheduleType scheduleType = ScheduleType.Cron);
    
    /// <summary>
    /// Get all active (enabled) scheduled jobs
    /// 获取所有活动的定时任务 - Lấy tất cả công việc đang hoạt động
    /// </summary>
    Task<IEnumerable<ScheduledJob>> GetActiveScheduledJobsAsync();
    
    /// <summary>
    /// Execute a job immediately (manual trigger)
    /// 立即执行任务 - Thực thi công việc ngay lập tức
    /// </summary>
    Task ExecuteJobAsync(ObjectId jobId);
}

