namespace ImageViewer.Domain.Enums;

/// <summary>
/// Schedule type enumeration
/// 调度类型枚举 - Kiểu lịch trình
/// </summary>
public enum ScheduleType
{
    /// <summary>
    /// Cron-based schedule (e.g., "0 */30 * * * *" for every 30 minutes)
    /// </summary>
    Cron = 1,
    
    /// <summary>
    /// Interval-based schedule (e.g., every 30 minutes)
    /// </summary>
    Interval = 2,
    
    /// <summary>
    /// One-time execution at specific time
    /// </summary>
    Once = 3,
    
    /// <summary>
    /// Manual execution only (via API or UI)
    /// </summary>
    Manual = 4
}

