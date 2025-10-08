using MongoDB.Bson;

namespace ImageViewer.Application.DTOs.Cache;

/// <summary>
/// Cache distribution statistics DTO
/// </summary>
public class CacheDistributionStatisticsDto
{
    public int TotalCacheFolders { get; set; }
    public int TotalFiles { get; set; }
    public long TotalSizeBytes { get; set; }
    public double AverageFilesPerFolder { get; set; }
    public double AverageSizePerFolder { get; set; }
    public DistributionBalanceDto DistributionBalance { get; set; } = null!;
    public IEnumerable<CacheFolderDistributionDto> CacheFolderDistributions { get; set; } = null!;
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Distribution balance metrics DTO
/// </summary>
public class DistributionBalanceDto
{
    public double FileCountVariance { get; set; }
    public double SizeVariance { get; set; }
    public double FileCountStandardDeviation { get; set; }
    public double SizeStandardDeviation { get; set; }
    public bool IsWellBalanced { get; set; }
}

/// <summary>
/// Individual cache folder distribution DTO
/// </summary>
public class CacheFolderDistributionDto
{
    public ObjectId CacheFolderId { get; set; }
    public string CacheFolderName { get; set; } = string.Empty;
    public string CacheFolderPath { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public long MaxSizeBytes { get; set; }
    public double UsagePercentage { get; set; }
    public bool IsActive { get; set; }
}
