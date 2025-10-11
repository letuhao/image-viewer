using ImageViewer.Domain.Entities;

namespace ImageViewer.Scheduler.Jobs;

/// <summary>
/// Base interface for scheduled job type handlers
/// 定时任务类型处理器接口 - Interface handler cho loại công việc định kỳ
/// </summary>
public interface IScheduledJobTypeHandler
{
    /// <summary>
    /// Execute the job logic
    /// 执行任务逻辑 - Thực thi logic công việc
    /// </summary>
    /// <returns>Result dictionary with execution details</returns>
    Task<Dictionary<string, object>> ExecuteAsync(ScheduledJob job, CancellationToken cancellationToken);
}

/// <summary>
/// Handler for library scan jobs
/// 库扫描任务处理器 - Handler quét thư viện
/// </summary>
public interface ILibraryScanJobHandler : IScheduledJobTypeHandler
{
}

/// <summary>
/// Handler for cache cleanup jobs
/// 缓存清理任务处理器 - Handler dọn dẹp cache
/// </summary>
public interface ICacheCleanupJobHandler : IScheduledJobTypeHandler
{
}

/// <summary>
/// Handler for stale job recovery
/// 停滞任务恢复处理器 - Handler khôi phục công việc treo
/// </summary>
public interface IStaleJobRecoveryHandler : IScheduledJobTypeHandler
{
}

/// <summary>
/// Handler for thumbnail cleanup jobs
/// 缩略图清理任务处理器 - Handler dọn dẹp thumbnail
/// </summary>
public interface IThumbnailCleanupJobHandler : IScheduledJobTypeHandler
{
}

