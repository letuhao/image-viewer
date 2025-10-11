namespace ImageViewer.Application.Services;

/// <summary>
/// Service for monitoring job failures and sending alerts
/// 任务失败警报服务 - Dịch vụ cảnh báo lỗi công việc
/// </summary>
public interface IJobFailureAlertService
{
    /// <summary>
    /// Check job failure rate and send alert if threshold exceeded
    /// 检查任务失败率并在超过阈值时发送警报 - Kiểm tra tỷ lệ lỗi và gửi cảnh báo
    /// </summary>
    Task CheckAndAlertAsync(string jobId, double failureThreshold = 0.1);
    
    /// <summary>
    /// Monitor all running jobs and alert on high failure rates
    /// 监控所有运行中的任务 - Giám sát tất cả công việc đang chạy
    /// </summary>
    Task MonitorAllJobsAsync(double failureThreshold = 0.1);
    
    /// <summary>
    /// Send alert for job failure
    /// 发送任务失败警报 - Gửi cảnh báo lỗi công việc
    /// </summary>
    Task SendJobFailureAlertAsync(string jobId, string jobType, string collectionName, int failedCount, int totalCount, double failureRate);
}

