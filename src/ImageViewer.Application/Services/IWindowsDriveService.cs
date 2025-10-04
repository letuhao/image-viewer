using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service for managing Windows drive access and file operations
/// </summary>
public interface IWindowsDriveService
{
    /// <summary>
    /// Get available Windows drives
    /// </summary>
    Task<IEnumerable<DriveInfo>> GetAvailableDrivesAsync();

    /// <summary>
    /// Check if a drive is accessible
    /// </summary>
    Task<bool> IsDriveAccessibleAsync(string driveLetter);

    /// <summary>
    /// Get drive information
    /// </summary>
    Task<DriveInfo> GetDriveInfoAsync(string driveLetter);

    /// <summary>
    /// Scan drive for media files
    /// </summary>
    Task<IEnumerable<MediaFileInfo>> ScanDriveForMediaAsync(string driveLetter, string[]? extensions = null);

    /// <summary>
    /// Get directory structure
    /// </summary>
    Task<IEnumerable<DirectoryInfo>> GetDirectoryStructureAsync(string driveLetter, string? path = null);

    /// <summary>
    /// Create library from drive
    /// </summary>
    Task<string> CreateLibraryFromDriveAsync(string driveLetter, string libraryName, string? description = null);

    /// <summary>
    /// Monitor drive for changes
    /// </summary>
    Task StartDriveMonitoringAsync(string driveLetter);

    /// <summary>
    /// Stop drive monitoring
    /// </summary>
    Task StopDriveMonitoringAsync(string driveLetter);

    /// <summary>
    /// Get drive statistics
    /// </summary>
    Task<DriveStatistics> GetDriveStatisticsAsync(string driveLetter);
}

/// <summary>
/// Drive information model
/// </summary>
public class DriveInfo
{
    public string Letter { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string FileSystem { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public long FreeSpace { get; set; }
    public long UsedSpace { get; set; }
    public bool IsReady { get; set; }
    public string DriveType { get; set; } = string.Empty;
}

/// <summary>
/// Media file information model
/// </summary>
public class MediaFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public int? Width { get; set; }
    public int? Height { get; set; }
    public TimeSpan? Duration { get; set; }
}

/// <summary>
/// Directory information model
/// </summary>
public class DirectoryInfo
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string ParentPath { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public int FileCount { get; set; }
    public int SubdirectoryCount { get; set; }
    public long TotalSize { get; set; }
}

/// <summary>
/// Drive statistics model
/// </summary>
public class DriveStatistics
{
    public string DriveLetter { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public int TotalDirectories { get; set; }
    public long TotalSize { get; set; }
    public int MediaFiles { get; set; }
    public int ImageFiles { get; set; }
    public int VideoFiles { get; set; }
    public DateTime LastScanned { get; set; }
    public TimeSpan ScanDuration { get; set; }
}
