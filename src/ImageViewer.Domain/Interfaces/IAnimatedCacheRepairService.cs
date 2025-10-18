using System.Threading;
using System.Threading.Tasks;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Service interface for finding and repairing incorrectly cached animated files
/// </summary>
public interface IAnimatedCacheRepairService
{
    /// <summary>
    /// Find all animated files that have been incorrectly cached as static images
    /// </summary>
    Task<AnimatedCacheRepairResult> FindIncorrectlyCachedAnimatedFilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Repair all incorrectly cached animated files by queuing them for regeneration
    /// </summary>
    /// <param name="forceRegenerate">Force regeneration even if cache exists</param>
    Task<AnimatedCacheRepairResult> RepairIncorrectlyCachedAnimatedFilesAsync(bool forceRegenerate = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerate all animated file caches (GIF, WebP, etc.)
    /// </summary>
    Task<int> RegenerateAllAnimatedCachesAsync(CancellationToken cancellationToken = default);
}

// Forward declaration to avoid circular dependency
public class AnimatedCacheRepairResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public System.DateTime StartTime { get; set; }
    public System.DateTime EndTime { get; set; }
    public System.DateTime? RepairStartTime { get; set; }
    public System.DateTime? RepairEndTime { get; set; }
    public int TotalCollections { get; set; }
    public int TotalImages { get; set; }
    public int AnimatedFilesFound { get; set; }
    public int IncorrectlyCachedFiles { get; set; }
    public int FilesQueuedForRepair { get; set; }
    public int RepairErrors { get; set; }
    public System.Collections.Generic.List<IncorrectCacheFileInfo> IncorrectFiles { get; set; } = new();
    public System.TimeSpan ScanDuration => EndTime - StartTime;
    public System.TimeSpan? RepairDuration => RepairEndTime.HasValue && RepairStartTime.HasValue 
        ? RepairEndTime.Value - RepairStartTime.Value 
        : null;
}

public class IncorrectCacheFileInfo
{
    public MongoDB.Bson.ObjectId CollectionId { get; set; }
    public string CollectionName { get; set; } = string.Empty;
    public string ImageId { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public string OriginalFormat { get; set; } = string.Empty;
    public System.Collections.Generic.List<string> CacheFormats { get; set; } = new();
}

