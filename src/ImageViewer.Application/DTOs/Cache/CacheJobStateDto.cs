namespace ImageViewer.Application.DTOs.Cache;

/// <summary>
/// DTO for cache job state information
/// 缓存任务状态DTO - DTO trạng thái công việc cache
/// </summary>
public class CacheJobStateDto
{
    public string Id { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string CollectionId { get; set; } = string.Empty;
    public string? CollectionName { get; set; }
    public string Status { get; set; } = string.Empty; // Pending, Running, Paused, Completed, Failed
    
    // Progress statistics
    public int TotalImages { get; set; }
    public int CompletedImages { get; set; }
    public int FailedImages { get; set; }
    public int SkippedImages { get; set; }
    public int RemainingImages { get; set; }
    public int Progress { get; set; } // 0-100%
    
    // Cache configuration
    public string? CacheFolderId { get; set; }
    public string? CacheFolderPath { get; set; }
    public long TotalSizeBytes { get; set; }
    public int CacheWidth { get; set; }
    public int CacheHeight { get; set; }
    public int Quality { get; set; }
    public string Format { get; set; } = "jpeg";
    
    // Timestamps
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? LastProgressAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Error information
    public string? ErrorMessage { get; set; }
    public bool CanResume { get; set; }
    
    // Detailed tracking (optional, can be excluded for list views)
    public List<string>? ProcessedImageIds { get; set; }
    public List<string>? FailedImageIds { get; set; }
}

