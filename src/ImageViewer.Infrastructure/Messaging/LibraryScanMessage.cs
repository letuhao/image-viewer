using ImageViewer.Domain.Events;

namespace ImageViewer.Infrastructure.Messaging;

/// <summary>
/// Message for library scan operations
/// Published by Scheduler, consumed by Worker
/// </summary>
public class LibraryScanMessage : MessageEvent
{
    public string LibraryId { get; set; } = string.Empty;
    public string LibraryPath { get; set; } = string.Empty;
    public string ScheduledJobId { get; set; } = string.Empty;
    public string JobRunId { get; set; } = string.Empty;
    public string ScanType { get; set; } = "Full"; // Full or Incremental
    public bool IncludeSubfolders { get; set; } = true;

    public LibraryScanMessage()
    {
        MessageType = "LibraryScan";
    }
}

