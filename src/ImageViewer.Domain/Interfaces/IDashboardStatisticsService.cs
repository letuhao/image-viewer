using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Interface for dashboard statistics service with Redis caching
/// 中文：仪表板统计服务接口，使用Redis缓存
/// Tiếng Việt: Giao diện dịch vụ thống kê bảng điều khiển với bộ nhớ đệm Redis
/// </summary>
public interface IDashboardStatisticsService
{
    /// <summary>
    /// Get dashboard statistics with Redis caching for ultra-fast loading
    /// </summary>
    Task<DashboardStatistics> GetDashboardStatisticsAsync();

    /// <summary>
    /// Update dashboard statistics when collections change (real-time updates)
    /// </summary>
    Task UpdateDashboardStatisticsAsync(string updateType, object updateData);

    /// <summary>
    /// Get recent dashboard activity
    /// </summary>
    Task<List<object>> GetRecentActivityAsync(int limit = 10);

    /// <summary>
    /// Check if dashboard statistics are fresh
    /// </summary>
    Task<bool> IsStatisticsFreshAsync();
}
