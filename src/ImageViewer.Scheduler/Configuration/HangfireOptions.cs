namespace ImageViewer.Scheduler.Configuration;

/// <summary>
/// Hangfire configuration options
/// Hangfire配置选项 - Tùy chọn cấu hình Hangfire
/// </summary>
public class HangfireOptions
{
    /// <summary>
    /// MongoDB connection string for Hangfire storage
    /// </summary>
    public string ConnectionString { get; set; } = "mongodb://localhost:27017/image_viewer_hangfire";
    
    /// <summary>
    /// Database name for Hangfire collections
    /// </summary>
    public string DatabaseName { get; set; } = "image_viewer_hangfire";
    
    /// <summary>
    /// Server name identifier for distributed Hangfire instances
    /// </summary>
    public string ServerName { get; set; } = "ImageViewer.Scheduler";
    
    /// <summary>
    /// Number of concurrent Hangfire workers
    /// </summary>
    public int WorkerCount { get; set; } = 5;
    
    /// <summary>
    /// Queue names that this server will process
    /// </summary>
    public string[] Queues { get; set; } = new[] { "default", "scheduler", "library-scan" };
    
    /// <summary>
    /// Automatic retry attempts for failed jobs
    /// </summary>
    public int AutomaticRetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Enable Hangfire dashboard UI
    /// </summary>
    public bool EnableDashboard { get; set; } = true;
    
    /// <summary>
    /// Dashboard path (e.g., /hangfire)
    /// </summary>
    public string DashboardPath { get; set; } = "/hangfire";
    
    /// <summary>
    /// Job expiration time (days) - how long to keep completed jobs
    /// </summary>
    public int JobExpirationDays { get; set; } = 7;
    
    /// <summary>
    /// Job synchronization interval (minutes) - how often to check database for new/updated jobs
    /// </summary>
    public int JobSynchronizationInterval { get; set; } = 5;
}

