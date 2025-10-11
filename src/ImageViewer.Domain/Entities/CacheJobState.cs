using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// CacheJobState entity - 缓存任务状态 - Trạng thái công việc cache
/// Tracks cache generation job state for resumption after interruption
/// 跟踪缓存生成任务状态以便在中断后恢复 - Theo dõi trạng thái công việc tạo cache để tiếp tục sau khi gián đoạn
/// </summary>
public class CacheJobState : BaseEntity
{
    [BsonElement("jobId")]
    public string JobId { get; private set; } // Background job ID
    
    [BsonElement("collectionId")]
    public string CollectionId { get; private set; }
    
    [BsonElement("collectionName")]
    public string? CollectionName { get; private set; }
    
    [BsonElement("status")]
    public string Status { get; private set; } // Pending, Running, Paused, Completed, Failed
    
    [BsonElement("totalImages")]
    public int TotalImages { get; private set; }
    
    [BsonElement("completedImages")]
    public int CompletedImages { get; private set; }
    
    [BsonElement("failedImages")]
    public int FailedImages { get; private set; }
    
    [BsonElement("skippedImages")]
    public int SkippedImages { get; private set; } // Already cached
    
    [BsonElement("processedImageIds")]
    public List<string> ProcessedImageIds { get; private set; } = new(); // Track which images are done
    
    [BsonElement("failedImageIds")]
    public List<string> FailedImageIds { get; private set; } = new(); // Track which images failed
    
    [BsonElement("cacheFolderId")]
    public string? CacheFolderId { get; private set; } // Which cache folder is being used
    
    [BsonElement("cacheFolderPath")]
    public string? CacheFolderPath { get; private set; }
    
    [BsonElement("totalSizeBytes")]
    public long TotalSizeBytes { get; private set; } // Total size of cache generated so far
    
    [BsonElement("cacheWidth")]
    public int CacheWidth { get; private set; }
    
    [BsonElement("cacheHeight")]
    public int CacheHeight { get; private set; }
    
    [BsonElement("quality")]
    public int Quality { get; private set; }
    
    [BsonElement("format")]
    public string Format { get; private set; } = "jpeg";
    
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
    private CacheJobState() 
    { 
        JobId = string.Empty;
        CollectionId = string.Empty;
        Status = "Pending";
    }

    public CacheJobState(
        string jobId,
        string collectionId,
        string? collectionName,
        int totalImages,
        string? cacheFolderId,
        string? cacheFolderPath,
        int cacheWidth,
        int cacheHeight,
        int quality,
        string format)
    {
        JobId = jobId ?? throw new ArgumentNullException(nameof(jobId));
        CollectionId = collectionId ?? throw new ArgumentNullException(nameof(collectionId));
        CollectionName = collectionName;
        TotalImages = totalImages;
        CacheFolderId = cacheFolderId;
        CacheFolderPath = cacheFolderPath;
        CacheWidth = cacheWidth;
        CacheHeight = cacheHeight;
        Quality = quality;
        Format = format ?? "jpeg";
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

