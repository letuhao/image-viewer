namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Cache statistics value object
/// </summary>
public class CacheStatistics
{
    public int TotalCacheEntries { get; set; }
    public long TotalCacheSize { get; set; }
    public int ValidCacheEntries { get; set; }
    public int ExpiredCacheEntries { get; set; }
    public double AverageCacheSize { get; set; }
}
