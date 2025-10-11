using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// FileProcessingJobState entity - 文件处理任务状态 - Trạng thái công việc xử lý file
/// Unified job state tracking for all file processing operations (cache, thumbnail, compression, etc.)
/// 统一跟踪所有文件处理操作的任务状态 - Theo dõi trạng thái thống nhất cho tất cả thao tác xử lý file
/// </summary>
public class FileProcessingJobState : BaseEntity
{
    [BsonElement("jobId")]
    public string JobId { get; private set; } // Background job ID
    
    [BsonElement("jobType")]
    public string JobType { get; private set; } // "cache", "thumbnail", "both", "compression", etc.
    
    [BsonElement("collectionId")]
    public string CollectionId { get; private set; }
    
    [BsonElement("collectionName")]
    public string? CollectionName { get; private set; }
    
    [BsonElement("status")]
    public string Status { get; private set; } // Pending, Running, Paused, Completed, Failed
    
    // Progress tracking (common for all job types)
    [BsonElement("totalImages")]
    public int TotalImages { get; private set; }
    
    [BsonElement("completedImages")]
    public int CompletedImages { get; private set; }
    
    [BsonElement("failedImages")]
    public int FailedImages { get; private set; }
    
    [BsonElement("skippedImages")]
    public int SkippedImages { get; private set; } // Already processed
    
    [BsonElement("processedImageIds")]
    public List<string> ProcessedImageIds { get; private set; } = new(); // Track which images are done
    
    [BsonElement("failedImageIds")]
    public List<string> FailedImageIds { get; private set; } = new(); // Track which images failed
    
    // Output tracking (common for all job types)
    [BsonElement("outputFolderId")]
    public string? OutputFolderId { get; private set; } // Cache folder or thumbnail folder ID
    
    [BsonElement("outputFolderPath")]
    public string? OutputFolderPath { get; private set; }
    
    [BsonElement("totalSizeBytes")]
    public long TotalSizeBytes { get; private set; } // Total size of files generated so far
    
    // Job-specific settings stored as JSON (flexible for different job types)
    [BsonElement("jobSettings")]
    public string JobSettings { get; private set; } = "{}"; // JSON: { width, height, quality, format, etc. }
    
    // Timestamps
    [BsonElement("startedAt")]
    public DateTime? StartedAt { get; private set; }
    
    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; private set; }
    
    [BsonElement("lastProgressAt")]
    public DateTime? LastProgressAt { get; private set; } // Last time progress was updated
    
    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; private set; }
    
    [BsonElement("canResume")]
    public bool CanResume { get; private set; } = true; // Whether this job can be resumed

    // Private constructor for MongoDB
    private FileProcessingJobState() 
    { 
        JobId = string.Empty;
        JobType = "cache";
        CollectionId = string.Empty;
        Status = "Pending";
    }

    public FileProcessingJobState(
        string jobId,
        string jobType,
        string collectionId,
        string? collectionName,
        int totalImages,
        string? outputFolderId,
        string? outputFolderPath,
        string jobSettings)
    {
        JobId = jobId ?? throw new ArgumentNullException(nameof(jobId));
        JobType = jobType ?? throw new ArgumentNullException(nameof(jobType));
        CollectionId = collectionId ?? throw new ArgumentNullException(nameof(collectionId));
        CollectionName = collectionName;
        TotalImages = totalImages;
        OutputFolderId = outputFolderId;
        OutputFolderPath = outputFolderPath;
        JobSettings = jobSettings ?? "{}";
        Status = "Pending";
        CompletedImages = 0;
        FailedImages = 0;
        SkippedImages = 0;
        TotalSizeBytes = 0;
        CanResume = true;
    }

    public void Start()
    {
        Status = "Running";
        StartedAt = DateTime.UtcNow;
        LastProgressAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void Pause()
    {
        Status = "Paused";
        LastProgressAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void Resume()
    {
        Status = "Running";
        LastProgressAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void Complete()
    {
        Status = "Completed";
        CompletedAt = DateTime.UtcNow;
        LastProgressAt = DateTime.UtcNow;
        CanResume = false;
        UpdateTimestamp();
    }

    public void Fail(string errorMessage)
    {
        Status = "Failed";
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
        LastProgressAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void IncrementCompleted(string imageId, long sizeBytes)
    {
        if (!ProcessedImageIds.Contains(imageId))
        {
            ProcessedImageIds.Add(imageId);
            CompletedImages++;
            TotalSizeBytes += sizeBytes;
            LastProgressAt = DateTime.UtcNow;
            UpdateTimestamp();
        }
    }

    public void IncrementFailed(string imageId)
    {
        if (!FailedImageIds.Contains(imageId))
        {
            FailedImageIds.Add(imageId);
            FailedImages++;
            LastProgressAt = DateTime.UtcNow;
            UpdateTimestamp();
        }
    }

    public void IncrementSkipped(string imageId)
    {
        if (!ProcessedImageIds.Contains(imageId))
        {
            ProcessedImageIds.Add(imageId);
            SkippedImages++;
            LastProgressAt = DateTime.UtcNow;
            UpdateTimestamp();
        }
    }

    public bool IsImageProcessed(string imageId)
    {
        return ProcessedImageIds.Contains(imageId) || FailedImageIds.Contains(imageId);
    }

    public int GetProgress()
    {
        if (TotalImages == 0) return 0;
        return (int)((double)(CompletedImages + SkippedImages + FailedImages) / TotalImages * 100);
    }

    public int GetRemainingImages()
    {
        return TotalImages - (CompletedImages + SkippedImages + FailedImages);
    }

    public void DisableResume()
    {
        CanResume = false;
        UpdateTimestamp();
    }
}

