namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Interface for metadata recalculation service
/// </summary>
public interface IMetadataRecalculationService
{
    /// <summary>
    /// Recalculates all cache folder metadata based on actual files and collection data
    /// </summary>
    Task<MetadataRecalculationResult> RecalculateCacheFolderMetadataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculates all library metadata based on collection statistics
    /// </summary>
    Task<MetadataRecalculationResult> RecalculateLibraryMetadataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculates metadata for both cache folders and libraries
    /// </summary>
    Task<MetadataRecalculationResult> RecalculateAllMetadataAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of metadata recalculation operation
/// </summary>
public class MetadataRecalculationResult
{
    public string OperationType { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public List<string> Errors { get; set; } = new();
    public string? Message { get; set; }
}
