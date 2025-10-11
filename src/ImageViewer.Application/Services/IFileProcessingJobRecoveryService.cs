namespace ImageViewer.Application.Services;

/// <summary>
/// Service for recovering and resuming interrupted file processing jobs (cache, thumbnail, etc.)
/// 文件处理任务恢复服务 - Dịch vụ khôi phục công việc xử lý file
/// </summary>
public interface IFileProcessingJobRecoveryService
{
    /// <summary>
    /// Recover all incomplete file processing jobs on startup
    /// 恢复所有未完成的文件处理任务 - Khôi phục tất cả công việc xử lý file chưa hoàn thành
    /// </summary>
    Task RecoverIncompleteJobsAsync();
    
    /// <summary>
    /// Recover incomplete jobs by job type (cache, thumbnail, etc.)
    /// 按任务类型恢复未完成任务 - Khôi phục công việc chưa hoàn thành theo loại
    /// </summary>
    Task RecoverIncompleteJobsByTypeAsync(string jobType);
    
    /// <summary>
    /// Resume a specific job by job ID
    /// 通过任务ID恢复特定任务 - Tiếp tục công việc cụ thể theo ID
    /// </summary>
    Task<bool> ResumeJobAsync(string jobId);
    
    /// <summary>
    /// Get resumable jobs (incomplete with CanResume=true)
    /// 获取可恢复的任务列表 - Lấy danh sách công việc có thể tiếp tục
    /// </summary>
    Task<IEnumerable<string>> GetResumableJobIdsAsync();
    
    /// <summary>
    /// Get resumable jobs by type
    /// 按类型获取可恢复任务 - Lấy công việc có thể tiếp tục theo loại
    /// </summary>
    Task<IEnumerable<string>> GetResumableJobIdsByTypeAsync(string jobType);
    
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

    /// <summary>
    /// Detect and recover stale jobs (jobs stuck without progress)
    /// 检测并恢复停滞任务 - Phát hiện và khôi phục công việc bị treo
    /// </summary>
    Task<int> RecoverStaleJobsAsync(TimeSpan timeout);

    /// <summary>
    /// Get count of stale jobs
    /// 获取停滞任务数量 - Lấy số lượng công việc bị treo
    /// </summary>
    Task<int> GetStaleJobCountAsync(TimeSpan timeout);
}

