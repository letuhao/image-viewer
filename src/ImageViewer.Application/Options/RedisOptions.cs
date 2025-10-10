namespace ImageViewer.Application.Options;

/// <summary>
/// Redis caching configuration options
/// </summary>
public class RedisOptions
{
    public const string SectionName = "Redis";

    /// <summary>
    /// Redis connection string
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Instance name prefix for Redis keys
    /// </summary>
    public string InstanceName { get; set; } = "ImageViewer:";

    /// <summary>
    /// Default expiration time in minutes
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Image cache expiration time in minutes (longer than default)
    /// </summary>
    public int ImageCacheExpirationMinutes { get; set} = 120;

    /// <summary>
    /// Enable compression for cached images
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// Maximum cache size in bytes (48GB = 51539607552 bytes)
    /// </summary>
    public long MaxCacheSizeBytes { get; set; } = 51539607552; // 48GB

    /// <summary>
    /// Absolute expiration relative to now in hours
    /// </summary>
    public int AbsoluteExpirationRelativeToNowHours { get; set; } = 24;

    /// <summary>
    /// Sliding expiration in minutes (reset on access)
    /// </summary>
    public int SlidingExpirationMinutes { get; set; } = 30;
}

