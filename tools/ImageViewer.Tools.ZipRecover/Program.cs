using System.Windows;

namespace ImageViewer.Tools.ZipRecover;

/// <summary>
/// Configuration options for ZIP recovery
/// </summary>
public class ZipRecoveryOptions
{
    public string SevenZipPath { get; set; } = "C:\\Program Files\\7-Zip\\7z.exe";
    public string TempDirectory { get; set; } = "temp_recovery";
    public string BackupDirectory { get; set; } = "backup_corrupted";
    public int MaxRetryAttempts { get; set; } = 3;
    public bool SkipCorruptedFiles { get; set; } = true;
    public bool ValidateExtractedFiles { get; set; } = true;
    public bool SkipHealthyArchives { get; set; } = true;
    public int HealthCheckTimeoutSeconds { get; set; } = 30;
    public string LogLevel { get; set; } = "Information";
    public bool ValidateRecoveredArchive { get; set; } = true;
    public bool ShowBatchWindows { get; set; } = false;
    public string[] SupportedExtensions { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Service for processing corrupted ZIP files
/// </summary>
public interface IZipRecoveryService
{
    Task ProcessCorruptedZipFilesAsync();
}

/// <summary>
/// Service for parsing input file
/// </summary>
public interface IInputParser
{
    Task<List<string>> ParseZipFilePathsAsync(string inputFilePath);
    Task<List<string>> ParseArchiveFilePathsAsync(string inputFilePath);
    void TestPermissiveRegexPattern();
    List<string> UnmatchedLines { get; }
    string? ExtractPathFromLine(string line);
}

/// <summary>
/// Service for processing ZIP files
/// </summary>
public interface IZipProcessor
{
    Task<bool> RecoverZipFileAsync(string zipFilePath);
    Action<string, string>? UiLogCallback { get; set; }
}
