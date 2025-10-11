namespace ImageViewer.Application.DTOs.Cache;

/// <summary>
/// DTO for cache folder statistics with detailed information
/// 缓存文件夹统计信息DTO - DTO thống kê thư mục cache chi tiết
/// </summary>
public class CacheFolderStatisticsDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long MaxSizeBytes { get; set; }
    public long CurrentSizeBytes { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    
    // Enhanced statistics
    public int TotalCollections { get; set; }
    public int TotalFiles { get; set; }
    public List<string> CachedCollectionIds { get; set; } = new();
    public DateTime? LastCacheGeneratedAt { get; set; }
    public DateTime? LastCleanupAt { get; set; }
    
    // Calculated fields
    public long AvailableSpaceBytes { get; set; }
    public double UsagePercentage { get; set; }
    public bool IsFull { get; set; }
    public bool IsNearFull { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

