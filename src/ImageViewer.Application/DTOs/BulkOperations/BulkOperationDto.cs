using MongoDB.Bson;

namespace ImageViewer.Application.DTOs.BulkOperations;

/// <summary>
/// Bulk import request
/// </summary>
public class BulkImportRequest
{
    public ObjectId UserId { get; set; }
    public string SourcePath { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
    public List<string> FileTypes { get; set; } = new();
    public bool OverwriteExisting { get; set; } = false;
    public bool CreateThumbnails { get; set; } = true;
    public bool GenerateMetadata { get; set; } = true;
    public int BatchSize { get; set; } = 100;
}

/// <summary>
/// Bulk export request
/// </summary>
public class BulkExportRequest
{
    public ObjectId UserId { get; set; }
    public List<ObjectId> CollectionIds { get; set; } = new();
    public string ExportPath { get; set; } = string.Empty;
    public string ExportFormat { get; set; } = "Original"; // Original, JPEG, PNG, WebP
    public int? MaxWidth { get; set; }
    public int? MaxHeight { get; set; }
    public int Quality { get; set; } = 90;
    public bool IncludeMetadata { get; set; } = true;
    public bool CreateZipArchive { get; set; } = false;
}

/// <summary>
/// Bulk update request
/// </summary>
public class BulkUpdateRequest
{
    public ObjectId UserId { get; set; }
    public List<ObjectId> CollectionIds { get; set; } = new();
    public Dictionary<string, object> UpdateFields { get; set; } = new();
    public bool ValidateUpdates { get; set; } = true;
    public int BatchSize { get; set; } = 100;
}

/// <summary>
/// Bulk delete request
/// </summary>
public class BulkDeleteRequest
{
    public ObjectId UserId { get; set; }
    public List<ObjectId> CollectionIds { get; set; } = new();
    public bool SoftDelete { get; set; } = true;
    public bool DeleteFiles { get; set; } = false;
    public bool ConfirmDeletion { get; set; } = false;
}

/// <summary>
/// Bulk validation request
/// </summary>
public class BulkValidationRequest
{
    public ObjectId UserId { get; set; }
    public List<ObjectId> CollectionIds { get; set; } = new();
    public List<string> ValidationRules { get; set; } = new();
    public bool ValidateFiles { get; set; } = true;
    public bool ValidateMetadata { get; set; } = true;
    public bool ValidatePermissions { get; set; } = true;
}

/// <summary>
/// Bulk operation progress
/// </summary>
public class BulkOperationProgress
{
    public ObjectId OperationId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int SuccessfulItems { get; set; }
    public int FailedItems { get; set; }
    public double ProgressPercentage { get; set; }
    public string CurrentItem { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Running, Completed, Failed, Cancelled
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Bulk operation result
/// </summary>
public class BulkOperationResultDto
{
    public ObjectId OperationId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int SuccessfulItems { get; set; }
    public int FailedItems { get; set; }
    public int SkippedItems { get; set; }
    public double SuccessRate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> Summary { get; set; } = new();
}

/// <summary>
/// Bulk operation error
/// </summary>
public class BulkOperationError
{
    public ObjectId ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string ErrorType { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
}
