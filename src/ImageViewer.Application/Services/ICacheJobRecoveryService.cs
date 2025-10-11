namespace ImageViewer.Application.Services;

/// <summary>
/// Service for recovering and resuming interrupted cache generation jobs
/// 缓存任务恢复服务 - Dịch vụ khôi phục công việc cache
/// </summary>
public interface ICacheJobRecoveryService
{
    /// <summary>
    /// Recover all incomplete/stale cache jobs on startup
    /// 恢复所有未完成/停滞的缓存任务 - Khôi phục tất cả công việc cache chưa hoàn thành/bị treo
    /// </summary>
    Task RecoverIncompleteJobsAsync();
    
    /// <summary>
    /// Resume a specific cache job by job ID
    /// 通过任务ID恢复特定的缓存任务 - Tiếp tục công việc cache cụ thể theo ID
    /// </summary>
    Task<bool> ResumeJobAsync(string jobId);
    
    /// <summary>
    /// Get resumable jobs (incomplete with CanResume=true)
    /// 获取可恢复的任务列表 - Lấy danh sách công việc có thể tiếp tục
    /// </summary>
    Task<IEnumerable<string>> GetResumableJobIdsAsync();
    
    /// <summary>
    /// Mark a job as non-resumable (e.g., corrupted data, missing collection)
    /// 标记任务为不可恢复 - Đánh dấu công việc không thể tiếp tục
    /// </summary>
    Task DisableJobResumptionAsync(string jobId, string reason);
    
    /// <summary>
    /// Cleanup old completed jobs (older than specified days)
    /// 清理旧的已完成任务 - Dọn dẹp công việc đã hoàn thành cũ
    /// </summary>
    Task<int> CleanupOldCompletedJobsAsync(int olderThanDays = 30);
}

