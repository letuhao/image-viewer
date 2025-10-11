namespace ImageViewer.Application.DTOs.Cache;

/// <summary>
/// DTO for file processing job state information (cache, thumbnail, etc.)
/// 文件处理任务状态DTO - DTO trạng thái công việc xử lý file
/// </summary>
public class FileProcessingJobStateDto
{
    public string Id { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty; // "cache", "thumbnail", "both", etc.
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
    
    // Output information
    public string? OutputFolderId { get; set; }
    public string? OutputFolderPath { get; set; }
    public long TotalSizeBytes { get; set; }
    
    // Job settings (JSON string)
    public string JobSettings { get; set; } = "{}";
    
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

