using MongoDB.Bson;

namespace ImageViewer.Scheduler.Jobs;

/// <summary>
/// Interface for scheduled job executors
/// 定时任务执行器接口 - Interface executor công việc định kỳ
/// </summary>
public interface IScheduledJobExecutor
{
    /// <summary>
    /// Execute a scheduled job
    /// 执行定时任务 - Thực thi công việc định kỳ
    /// </summary>
    Task ExecuteAsync(ObjectId scheduledJobId, CancellationToken cancellationToken);
}

