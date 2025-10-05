using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Entities;

/// <summary>
/// Backup history entity - represents backup tracking and management
/// </summary>
public class BackupHistory : BaseEntity
{
    [BsonElement("backupName")]
    public string BackupName { get; private set; } = string.Empty;

    [BsonElement("backupType")]
    public string BackupType { get; private set; } = "Full"; // Full, Incremental, Differential, Snapshot

    [BsonElement("sourceLocation")]
    public string SourceLocation { get; private set; } = string.Empty;

    [BsonElement("destinationLocation")]
    public string DestinationLocation { get; private set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; private set; } = "Pending"; // Pending, InProgress, Completed, Failed, Cancelled

    [BsonElement("startTime")]
    public DateTime? StartTime { get; private set; }

    [BsonElement("endTime")]
    public DateTime? EndTime { get; private set; }

    [BsonElement("duration")]
    public long? DurationMs { get; private set; }

    [BsonElement("totalFiles")]
    public int TotalFiles { get; private set; } = 0;

    [BsonElement("processedFiles")]
    public int ProcessedFiles { get; private set; } = 0;

    [BsonElement("failedFiles")]
    public int FailedFiles { get; private set; } = 0;

    [BsonElement("totalSizeBytes")]
    public long TotalSizeBytes { get; private set; } = 0;

    [BsonElement("backupSizeBytes")]
    public long BackupSizeBytes { get; private set; } = 0;

    [BsonElement("compressionRatio")]
    public double? CompressionRatio { get; private set; }

    [BsonElement("errorMessage")]
    public string? ErrorMessage { get; private set; }

    [BsonElement("errorDetails")]
    public List<string> ErrorDetails { get; private set; } = new();

    [BsonElement("retentionDays")]
    public int? RetentionDays { get; private set; }

    [BsonElement("expiresAt")]
    public DateTime? ExpiresAt { get; private set; }

    [BsonElement("isEncrypted")]
    public bool IsEncrypted { get; private set; } = false;

    [BsonElement("encryptionKey")]
    public string? EncryptionKey { get; private set; }

    [BsonElement("checksum")]
    public string? Checksum { get; private set; }

    [BsonElement("verificationStatus")]
    public string VerificationStatus { get; private set; } = "Pending"; // Pending, Verified, Failed

    [BsonElement("verificationTime")]
    public DateTime? VerificationTime { get; private set; }

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; private set; } = new();

    [BsonElement("createdBy")]
    public ObjectId? CreatedBy { get; private set; }

    [BsonElement("scheduledBackupId")]
    public ObjectId? ScheduledBackupId { get; private set; }

    // Navigation properties
    [BsonIgnore]
    public User? Creator { get; private set; }

    // Private constructor for EF Core
    private BackupHistory() { }

    public static BackupHistory Create(string backupName, string backupType, string sourceLocation, string destinationLocation, ObjectId? createdBy = null, ObjectId? scheduledBackupId = null, int? retentionDays = null)
    {
        if (string.IsNullOrWhiteSpace(backupName))
            throw new ArgumentException("Backup name cannot be empty", nameof(backupName));

        if (string.IsNullOrWhiteSpace(backupType))
            throw new ArgumentException("Backup type cannot be empty", nameof(backupType));

        if (string.IsNullOrWhiteSpace(sourceLocation))
            throw new ArgumentException("Source location cannot be empty", nameof(sourceLocation));

        if (string.IsNullOrWhiteSpace(destinationLocation))
            throw new ArgumentException("Destination location cannot be empty", nameof(destinationLocation));

        var backup = new BackupHistory
        {
            BackupName = backupName,
            BackupType = backupType,
            SourceLocation = sourceLocation,
            DestinationLocation = destinationLocation,
            CreatedBy = createdBy,
            ScheduledBackupId = scheduledBackupId,
            Status = "Pending",
            TotalFiles = 0,
            ProcessedFiles = 0,
            FailedFiles = 0,
            TotalSizeBytes = 0,
            BackupSizeBytes = 0,
            RetentionDays = retentionDays,
            IsEncrypted = false,
            VerificationStatus = "Pending",
            ErrorDetails = new List<string>(),
            Metadata = new Dictionary<string, object>()
        };

        if (retentionDays.HasValue)
        {
            backup.ExpiresAt = DateTime.UtcNow.AddDays(retentionDays.Value);
        }

        return backup;
    }

    public void StartBackup()
    {
        Status = "InProgress";
        StartTime = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void CompleteBackup()
    {
        Status = "Completed";
        EndTime = DateTime.UtcNow;
        
        if (StartTime.HasValue)
        {
            DurationMs = (long)(EndTime.Value - StartTime.Value).TotalMilliseconds;
        }
        
        UpdateTimestamp();
    }

    public void FailBackup(string errorMessage, List<string>? errorDetails = null)
    {
        Status = "Failed";
        EndTime = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        ErrorDetails = errorDetails ?? new List<string>();
        
        if (StartTime.HasValue)
        {
            DurationMs = (long)(EndTime.Value - StartTime.Value).TotalMilliseconds;
        }
        
        UpdateTimestamp();
    }

    public void CancelBackup()
    {
        Status = "Cancelled";
        EndTime = DateTime.UtcNow;
        
        if (StartTime.HasValue)
        {
            DurationMs = (long)(EndTime.Value - StartTime.Value).TotalMilliseconds;
        }
        
        UpdateTimestamp();
    }

    public void UpdateProgress(int processedFiles, int failedFiles, long totalSizeBytes, long backupSizeBytes)
    {
        ProcessedFiles = processedFiles;
        FailedFiles = failedFiles;
        TotalSizeBytes = totalSizeBytes;
        BackupSizeBytes = backupSizeBytes;
        
        if (totalSizeBytes > 0)
        {
            CompressionRatio = (double)backupSizeBytes / totalSizeBytes;
        }
        
        UpdateTimestamp();
    }

    public void SetTotalFiles(int totalFiles)
    {
        TotalFiles = totalFiles;
        UpdateTimestamp();
    }

    public void SetEncryption(bool isEncrypted, string? encryptionKey = null)
    {
        IsEncrypted = isEncrypted;
        EncryptionKey = encryptionKey;
        UpdateTimestamp();
    }

    public void SetChecksum(string checksum)
    {
        Checksum = checksum;
        UpdateTimestamp();
    }

    public void VerifyBackup(bool isVerified)
    {
        VerificationStatus = isVerified ? "Verified" : "Failed";
        VerificationTime = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void SetRetention(int retentionDays)
    {
        RetentionDays = retentionDays;
        ExpiresAt = DateTime.UtcNow.AddDays(retentionDays);
        UpdateTimestamp();
    }

    public void AddErrorDetail(string errorDetail)
    {
        if (!string.IsNullOrWhiteSpace(errorDetail))
        {
            ErrorDetails.Add(errorDetail);
            UpdateTimestamp();
        }
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }

    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    }

    public double GetProgressPercentage()
    {
        if (TotalFiles == 0) return 0;
        return (double)ProcessedFiles / TotalFiles * 100;
    }

    public double GetCompressionPercentage()
    {
        if (TotalSizeBytes == 0) return 0;
        return (1.0 - (double)BackupSizeBytes / TotalSizeBytes) * 100;
    }

    public bool IsSuccessful()
    {
        return Status == "Completed" && VerificationStatus == "Verified";
    }

    public TimeSpan? GetDuration()
    {
        if (DurationMs.HasValue)
        {
            return TimeSpan.FromMilliseconds(DurationMs.Value);
        }
        return null;
    }
}
