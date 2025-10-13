using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;
using System.Windows;
using System.IO;
using System.Linq;
using System.Text;
using ImageViewer.Tools.ZipRecover.Models;
using ImageViewer.Tools.ZipRecover.Services;

namespace ImageViewer.Tools.ZipRecover.ViewModels;

/// <summary>
/// Main ViewModel for the ZIP Recovery Tool
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly ZipRecoveryOptions _options;
    private readonly IInputParser _inputParser;
    private readonly IZipProcessor _zipProcessor;
    private readonly IArchiveHealthValidator _healthValidator;
    private readonly IExportService _exportService;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _currentStatus = "Ready";

    [ObservableProperty]
    private int _totalFiles;

    [ObservableProperty]
    private int _processedFiles;

    [ObservableProperty]
    private int _successfulFiles;

    [ObservableProperty]
    private int _failedFiles;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _sevenZipPath;

    [ObservableProperty]
    private string _inputFilePath = "data/input.txt";

    [ObservableProperty]
    private string _backupDirectory = "backup_corrupted";

    [ObservableProperty]
    private string _tempDirectory = "temp_recovery";

    [ObservableProperty]
    private bool _validateFiles = true;

    [ObservableProperty]
    private bool _skipCorruptedFiles = true;

    [ObservableProperty]
    private bool _skipHealthyArchives = true;

    [ObservableProperty]
    private int _healthCheckTimeoutSeconds = 30;

    [ObservableProperty]
    private bool _showBatchWindows = false;

    [ObservableProperty]
    private string _recoveryReport = string.Empty;

    public ObservableCollection<ZipFileItem> ZipFiles { get; } = new();
    public ObservableCollection<LogEntry> LogEntries { get; } = new();
    public ObservableCollection<UnmatchedLineItem> UnmatchedLines { get; } = new();

    /// <summary>
    /// Number of files selected for processing
    /// </summary>
    public int SelectedFilesCount => ZipFiles.Count(f => f.IsSelected);

    public MainViewModel(
        ILogger<MainViewModel> logger,
        IOptions<ZipRecoveryOptions> options,
        IInputParser inputParser,
        IZipProcessor zipProcessor,
        IArchiveHealthValidator healthValidator,
        IExportService exportService)
    {
        _logger = logger;
        _options = options.Value;
        _inputParser = inputParser;
        _zipProcessor = zipProcessor;
        _healthValidator = healthValidator;
        _exportService = exportService;

        // Set up UI logging callback for ZipProcessor
        _zipProcessor.UiLogCallback = (level, message) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                AddLogEntry(level, message);
            });
        };

        SevenZipPath = _options.SevenZipPath;
        BackupDirectory = _options.BackupDirectory;
        TempDirectory = _options.TempDirectory;
        ValidateFiles = _options.ValidateExtractedFiles;
        SkipCorruptedFiles = _options.SkipCorruptedFiles;
        SkipHealthyArchives = _options.SkipHealthyArchives;
        HealthCheckTimeoutSeconds = _options.HealthCheckTimeoutSeconds;
        ShowBatchWindows = _options.ShowBatchWindows;
    }

    [RelayCommand]
    private async Task LoadInputFileAsync()
    {
        try
        {
            CurrentStatus = "Loading input file...";
            IsProcessing = true;

            var zipPaths = await _inputParser.ParseArchiveFilePathsAsync(InputFilePath);
            
            ZipFiles.Clear();
            UnmatchedLines.Clear();
            
            // Populate unmatched lines for manual review
            foreach (var line in _inputParser.UnmatchedLines)
            {
                UnmatchedLines.Add(new UnmatchedLineItem { Line = line });
            }
            
            foreach (var path in zipPaths)
            {
                var fileInfo = new FileInfo(path);
                ZipFiles.Add(new ZipFileItem
                {
                    FilePath = path,
                    Status = ZipFileStatus.Pending,
                    FileName = Path.GetFileName(path),
                    ArchiveType = Path.GetExtension(path).ToUpperInvariant(),
                    OriginalSize = fileInfo.Exists ? fileInfo.Length : 0,
                    HealthStatus = "Not Checked"
                });
            }

            TotalFiles = zipPaths.Count;
            ProcessedFiles = 0;
            SuccessfulFiles = 0;
            FailedFiles = 0;
            UpdateProgress();

            AddLogEntry("Information", $"Loaded {zipPaths.Count} archive files from input file");
            CurrentStatus = $"Loaded {zipPaths.Count} files - Ready to process";
        }
        catch (Exception ex)
        {
            AddLogEntry("Error", $"Failed to load input file: {ex.Message}");
            CurrentStatus = "Error loading input file";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task StartRecoveryAsync()
    {
        if (ZipFiles.Count == 0)
        {
            MessageBox.Show("Please load input file first.", "No Files", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Get only selected files for processing
        var selectedFiles = ZipFiles.Where(f => f.IsSelected).ToList();
        
        if (!selectedFiles.Any())
        {
            MessageBox.Show("No files selected for processing. Please select files to process.", "No Files Selected", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsProcessing = true;
            CurrentStatus = "Starting recovery process...";
            
            // Update options with current UI values
            _options.SevenZipPath = SevenZipPath;
            _options.BackupDirectory = BackupDirectory;
            _options.TempDirectory = TempDirectory;
            _options.ValidateExtractedFiles = ValidateFiles;
            _options.SkipCorruptedFiles = SkipCorruptedFiles;
            _options.SkipHealthyArchives = SkipHealthyArchives;
            _options.HealthCheckTimeoutSeconds = HealthCheckTimeoutSeconds;

            AddLogEntry("Information", $"Starting ZIP recovery process for {selectedFiles.Count} selected files out of {ZipFiles.Count} total files");

            // Process each selected file
            for (int i = 0; i < selectedFiles.Count; i++)
            {
                var zipFile = selectedFiles[i];
                
                try
                {
                    zipFile.Status = ZipFileStatus.Processing;
                    CurrentStatus = $"Processing {i + 1}/{selectedFiles.Count}: {zipFile.FileName}";
                    
                    AddLogEntry("Information", $"Processing: {zipFile.FilePath}");

                    var success = await ProcessSingleFileAsync(zipFile);
                    
                    if (success)
                    {
                        zipFile.Status = ZipFileStatus.Success;
                        SuccessfulFiles++;
                        AddLogEntry("Success", $"✅ Recovered: {zipFile.FileName}");
                    }
                    else
                    {
                        zipFile.Status = ZipFileStatus.Failed;
                        FailedFiles++;
                        AddLogEntry("Warning", $"❌ Failed: {zipFile.FileName}");
                    }
                }
                catch (Exception ex)
                {
                    zipFile.Status = ZipFileStatus.Failed;
                    FailedFiles++;
                    AddLogEntry("Error", $"❌ Error processing {zipFile.FileName}: {ex.Message}");
                }

                ProcessedFiles++;
                UpdateProgress();
                
                // Small delay to allow UI updates
                await Task.Delay(100);
            }

            CurrentStatus = $"Recovery completed - {SuccessfulFiles} successful, {FailedFiles} failed";
            AddLogEntry("Information", $"Recovery completed: {SuccessfulFiles} successful, {FailedFiles} failed");
            
            // Auto-generate report when recovery is completed
            GenerateRecoveryReport();
            AddLogEntry("Information", "Recovery report auto-generated");
        }
        catch (Exception ex)
        {
            AddLogEntry("Error", $"Recovery process failed: {ex.Message}");
            CurrentStatus = "Recovery failed";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private void BrowseInputFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Input File",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            DefaultExt = "txt"
        };

        if (dialog.ShowDialog() == true)
        {
            InputFilePath = dialog.FileName;
        }
    }

    [RelayCommand]
    private void BrowseSevenZipPath()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select 7-Zip Executable",
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            DefaultExt = "exe"
        };

        if (dialog.ShowDialog() == true)
        {
            SevenZipPath = dialog.FileName;
        }
    }

    [RelayCommand]
    private void BrowseBackupDirectory()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Backup Directory",
            CheckFileExists = false,
            CheckPathExists = true,
            FileName = "Select Folder",
            Filter = "Folders|*.thisisnotafile"
        };

        if (dialog.ShowDialog() == true)
        {
            var directory = Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(directory))
            {
                BackupDirectory = directory;
            }
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        LogEntries.Clear();
    }

    [RelayCommand]
    private async Task CheckHealthStatusAsync()
    {
        if (ZipFiles.Count == 0)
        {
            MessageBox.Show("Please load input file first.", "No Files", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsProcessing = true;
            CurrentStatus = "Checking health status...";
            
            AddLogEntry("Information", "Starting health check for all archives");

            for (int i = 0; i < ZipFiles.Count; i++)
            {
                var zipFile = ZipFiles[i];
                
                try
                {
                    zipFile.HealthStatus = "Checking...";
                    CurrentStatus = $"Checking health {i + 1}/{ZipFiles.Count}: {zipFile.FileName}";
                    
                    var healthStatus = await _healthValidator.CheckArchiveHealthAsync(zipFile.FilePath);
                    zipFile.HealthStatus = GetHealthStatusText(healthStatus);
                    
                    AddLogEntry("Information", $"Health check: {zipFile.FileName} - {zipFile.HealthStatus}");
                }
                catch (Exception ex)
                {
                    zipFile.HealthStatus = "❓ Error";
                    AddLogEntry("Error", $"Health check failed for {zipFile.FileName}: {ex.Message}");
                }

                // Small delay to allow UI updates
                await Task.Delay(50);
            }

            CurrentStatus = "Health check completed";
            AddLogEntry("Information", "Health check completed for all archives");
        }
        catch (Exception ex)
        {
            AddLogEntry("Error", $"Health check process failed: {ex.Message}");
            CurrentStatus = "Health check failed";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private void TestPermissiveRegex()
    {
        try
        {
            AddLogEntry("Information", "Testing permissive regex pattern...");
            _inputParser.TestPermissiveRegexPattern();
            AddLogEntry("Information", "Permissive regex test completed. Check logs for results.");
        }
        catch (Exception ex)
        {
            AddLogEntry("Error", $"Permissive regex test failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ExtractPathFromSelectedLine()
    {
        var selectedLine = UnmatchedLines.FirstOrDefault(x => x.IsSelected);
        if (selectedLine != null)
        {
            var extractedPath = _inputParser.ExtractPathFromLine(selectedLine.Line);
            if (!string.IsNullOrEmpty(extractedPath))
            {
                selectedLine.ExtractedPath = extractedPath;
                selectedLine.Status = File.Exists(extractedPath) ? "Valid" : "Invalid";
            }
            else
            {
                selectedLine.Status = "No Path Found";
            }
        }
    }

    [RelayCommand]
    private void AddSelectedLinesToProcessing()
    {
        var selectedLines = UnmatchedLines.Where(x => x.IsSelected && x.IsValidFile).ToList();
        foreach (var line in selectedLines)
        {
            var fileInfo = new FileInfo(line.ExtractedPath);
            ZipFiles.Add(new ZipFileItem
            {
                FilePath = line.ExtractedPath,
                Status = ZipFileStatus.Pending,
                FileName = Path.GetFileName(line.ExtractedPath),
                ArchiveType = Path.GetExtension(line.ExtractedPath).ToUpperInvariant(),
                OriginalSize = fileInfo.Exists ? fileInfo.Length : 0,
                HealthStatus = "Not Checked"
            });
            
            line.Status = "Added to Processing";
        }
        
        TotalFiles = ZipFiles.Count;
    }

    [RelayCommand]
    private void SelectAllUnmatchedLines()
    {
        foreach (var line in UnmatchedLines)
        {
            line.IsSelected = true;
        }
    }

    [RelayCommand]
    private void DeselectAllUnmatchedLines()
    {
        foreach (var line in UnmatchedLines)
        {
            line.IsSelected = false;
        }
    }

    /// <summary>
    /// Update progress for a specific file
    /// </summary>
    public void UpdateFileProgress(ZipFileItem file, string step, double percentage, string details = "")
    {
        file.CurrentStep = step;
        file.FileProgressPercentage = percentage;
        file.ProcessingDetails = details;
        
        // Trigger property change notifications
        OnPropertyChanged(nameof(SelectedFilesCount));
    }

    /// <summary>
    /// Reset file progress
    /// </summary>
    public void ResetFileProgress(ZipFileItem file)
    {
        file.CurrentStep = "Ready";
        file.FileProgressPercentage = 0;
        file.ProcessingDetails = string.Empty;
    }

    /// <summary>
    /// Generate comprehensive recovery report
    /// </summary>
    public void GenerateRecoveryReport()
    {
        var report = new StringBuilder();
        
        // Header
        report.AppendLine("=== ZIP RECOVERY REPORT ===");
        report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"Application: ImageViewer ZIP Recovery Tool");
        report.AppendLine();
        
        // Summary Statistics
        report.AppendLine("=== SUMMARY STATISTICS ===");
        report.AppendLine($"Total Files: {ZipFiles.Count}");
        report.AppendLine($"Selected Files: {ZipFiles.Count(f => f.IsSelected)}");
        report.AppendLine($"Successful: {ZipFiles.Count(f => f.Status == ZipFileStatus.Success)}");
        report.AppendLine($"Failed: {ZipFiles.Count(f => f.Status == ZipFileStatus.Failed)}");
        report.AppendLine($"Skipped: {ZipFiles.Count(f => f.Status == ZipFileStatus.Skipped)}");
        report.AppendLine($"Pending: {ZipFiles.Count(f => f.Status == ZipFileStatus.Pending)}");
        report.AppendLine($"Processing: {ZipFiles.Count(f => f.Status == ZipFileStatus.Processing)}");
        report.AppendLine();
        
        // Configuration
        report.AppendLine("=== CONFIGURATION ===");
        report.AppendLine($"7-Zip Path: {SevenZipPath}");
        report.AppendLine($"Temp Directory: {TempDirectory}");
        report.AppendLine($"Backup Directory: {BackupDirectory}");
        report.AppendLine($"Skip Healthy Archives: {SkipHealthyArchives}");
        report.AppendLine($"Health Check Timeout: {HealthCheckTimeoutSeconds} seconds");
        report.AppendLine($"Show Batch Windows: {ShowBatchWindows}");
        report.AppendLine($"Validate Files: {ValidateFiles}");
        report.AppendLine($"Skip Corrupted Files: {SkipCorruptedFiles}");
        report.AppendLine();
        
        // Files by Status
        report.AppendLine("=== FILES BY STATUS ===");
        var statusGroups = ZipFiles.GroupBy(f => f.StatusText);
        foreach (var group in statusGroups.OrderBy(g => g.Key))
        {
            report.AppendLine($"{group.Key}: {group.Count()} files");
        }
        report.AppendLine();
        
        // Files by Type
        report.AppendLine("=== FILES BY ARCHIVE TYPE ===");
        var typeGroups = ZipFiles.GroupBy(f => f.ArchiveType);
        foreach (var group in typeGroups.OrderBy(g => g.Key))
        {
            report.AppendLine($"{group.Key}: {group.Count()} files");
        }
        report.AppendLine();
        
        // Files by Health Status
        report.AppendLine("=== FILES BY HEALTH STATUS ===");
        var healthGroups = ZipFiles.GroupBy(f => f.HealthStatus);
        foreach (var group in healthGroups.OrderBy(g => g.Key))
        {
            report.AppendLine($"{group.Key}: {group.Count()} files");
        }
        report.AppendLine();
        
        // Detailed File List
        report.AppendLine("=== DETAILED FILE LIST ===");
        foreach (var file in ZipFiles.OrderBy(f => f.FileName))
        {
            report.AppendLine($"File: {file.FileName}");
            report.AppendLine($"  Path: {file.FilePath}");
            report.AppendLine($"  Type: {file.ArchiveType}");
            report.AppendLine($"  Status: {file.StatusText}");
            report.AppendLine($"  Health: {file.HealthStatus}");
            report.AppendLine($"  Selected: {file.IsSelected}");
            report.AppendLine($"  Original Size: {file.OriginalSize:N0} bytes");
            report.AppendLine($"  Recovered Size: {file.RecoveredSize:N0} bytes");
            report.AppendLine($"  Valid Files: {file.ValidFilesCount}");
            report.AppendLine($"  Total Files: {file.TotalFilesCount}");
            report.AppendLine($"  Current Step: {file.CurrentStep}");
            report.AppendLine($"  Progress: {file.FileProgressPercentage:F1}%");
            report.AppendLine($"  Details: {file.ProcessingDetails}");
            if (!string.IsNullOrEmpty(file.ErrorMessage))
            {
                report.AppendLine($"  Error: {file.ErrorMessage}");
            }
            report.AppendLine();
        }
        
        // Recent Logs
        report.AppendLine("=== RECENT LOG ENTRIES ===");
        var recentLogs = LogEntries.TakeLast(50).ToList();
        foreach (var log in recentLogs)
        {
            report.AppendLine($"[{log.Timestamp:yyyy-MM-dd HH:mm:ss}] [{log.Level}] {log.Message}");
        }
        report.AppendLine();
        
        // Footer
        report.AppendLine("=== END OF REPORT ===");
        
        RecoveryReport = report.ToString();
    }

    [RelayCommand]
    private void GenerateReport()
    {
        GenerateRecoveryReport();
        AddLogEntry("Information", "Recovery report generated successfully");
    }

    [RelayCommand]
    private void SelectAllFiles()
    {
        foreach (var file in ZipFiles)
        {
            file.IsSelected = true;
        }
    }

    [RelayCommand]
    private void DeselectAllFiles()
    {
        foreach (var file in ZipFiles)
        {
            file.IsSelected = false;
        }
    }

    [RelayCommand]
    private void SelectOnlyHealthyFiles()
    {
        foreach (var file in ZipFiles)
        {
            file.IsSelected = file.HealthStatus == "Healthy";
        }
    }

    [RelayCommand]
    private void SelectOnlyCorruptedFiles()
    {
        foreach (var file in ZipFiles)
        {
            file.IsSelected = file.HealthStatus == "Corrupted";
        }
    }

    [RelayCommand]
    private void SelectOnlyUnknownFiles()
    {
        foreach (var file in ZipFiles)
        {
            file.IsSelected = file.HealthStatus == "Unknown" || file.HealthStatus == "Not Checked";
        }
    }

    [RelayCommand]
    private void ExportLogs()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Logs",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            DefaultExt = "txt",
            FileName = $"zip_recovery_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var logContent = string.Join(Environment.NewLine, 
                    LogEntries.Select(log => $"[{log.Timestamp:yyyy-MM-dd HH:mm:ss}] [{log.Level}] {log.Message}"));
                
                File.WriteAllText(dialog.FileName, logContent);
                AddLogEntry("Information", $"Logs exported to: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                AddLogEntry("Error", $"Failed to export logs: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task ExportZipFilesToCsv()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export ZIP Files to CSV",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            DefaultExt = "csv",
            FileName = $"zip_files_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportZipFilesToCsvAsync(ZipFiles.ToList(), dialog.FileName);
                AddLogEntry("Information", $"ZIP files exported to CSV: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                AddLogEntry("Error", $"Failed to export ZIP files to CSV: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task ExportZipFilesToJson()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export ZIP Files to JSON",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = "json",
            FileName = $"zip_files_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportZipFilesToJsonAsync(ZipFiles.ToList(), dialog.FileName);
                AddLogEntry("Information", $"ZIP files exported to JSON: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                AddLogEntry("Error", $"Failed to export ZIP files to JSON: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task ExportLogsToCsv()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Logs to CSV",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            DefaultExt = "csv",
            FileName = $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportLogsToCsvAsync(LogEntries.ToList(), dialog.FileName);
                AddLogEntry("Information", $"Logs exported to CSV: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                AddLogEntry("Error", $"Failed to export logs to CSV: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task ExportLogsToJson()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Logs to JSON",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = "json",
            FileName = $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportLogsToJsonAsync(LogEntries.ToList(), dialog.FileName);
                AddLogEntry("Information", $"Logs exported to JSON: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                AddLogEntry("Error", $"Failed to export logs to JSON: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task ExportUnmatchedLinesToCsv()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Unmatched Lines to CSV",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            DefaultExt = "csv",
            FileName = $"unmatched_lines_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportUnmatchedLinesToCsvAsync(UnmatchedLines.ToList(), dialog.FileName);
                AddLogEntry("Information", $"Unmatched lines exported to CSV: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                AddLogEntry("Error", $"Failed to export unmatched lines to CSV: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task ExportUnmatchedLinesToJson()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Unmatched Lines to JSON",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = "json",
            FileName = $"unmatched_lines_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportUnmatchedLinesToJsonAsync(UnmatchedLines.ToList(), dialog.FileName);
                AddLogEntry("Information", $"Unmatched lines exported to JSON: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                AddLogEntry("Error", $"Failed to export unmatched lines to JSON: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task ExportSummaryReport()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Summary Report",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = "json",
            FileName = $"summary_report_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportSummaryReportAsync(ZipFiles.ToList(), LogEntries.ToList(), dialog.FileName);
                AddLogEntry("Information", $"Summary report exported: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                AddLogEntry("Error", $"Failed to export summary report: {ex.Message}");
            }
        }
    }

    private async Task<bool> ProcessSingleFileAsync(ZipFileItem zipFile)
    {
        try
        {
            // Reset progress
            ResetFileProgress(zipFile);
            
            // Check health status first if enabled
            if (_options.SkipHealthyArchives)
            {
                UpdateFileProgress(zipFile, "Checking Health", 10, "Validating archive integrity...");
                zipFile.HealthStatus = "Checking...";
                var healthStatus = await _healthValidator.CheckArchiveHealthAsync(zipFile.FilePath);
                zipFile.HealthStatus = GetHealthStatusText(healthStatus);
                
                if (healthStatus == ArchiveHealthStatus.Healthy)
                {
                    zipFile.Status = ZipFileStatus.Skipped;
                    UpdateFileProgress(zipFile, "Skipped", 100, "Archive is healthy, no recovery needed");
                    AddLogEntry("Information", $"Skipped healthy archive: {zipFile.FileName}");
                    return true; // Consider skipped healthy files as success
                }
                
                if (healthStatus == ArchiveHealthStatus.UnsupportedFormat)
                {
                    zipFile.Status = ZipFileStatus.Failed;
                    UpdateFileProgress(zipFile, "Failed", 100, "Unsupported archive format");
                    zipFile.ErrorMessage = "Unsupported archive format";
                    AddLogEntry("Warning", $"Unsupported format: {zipFile.FileName}");
                    return false;
                }
            }

            // Start recovery process
            UpdateFileProgress(zipFile, "Starting Recovery", 20, "Initializing recovery process...");
            zipFile.Status = ZipFileStatus.Processing;
            
            UpdateFileProgress(zipFile, "Extracting", 40, "Extracting archive contents...");
            var success = await _zipProcessor.RecoverZipFileAsync(zipFile.FilePath);
            
            if (success)
            {
                UpdateFileProgress(zipFile, "Completed", 100, "Recovery completed successfully");
                zipFile.Status = ZipFileStatus.Success;
                AddLogEntry("Success", $"✅ Recovered: {zipFile.FileName}");
            }
            else
            {
                UpdateFileProgress(zipFile, "Failed", 100, "Recovery failed");
                zipFile.Status = ZipFileStatus.Failed;
                zipFile.ErrorMessage = "Recovery failed";
                AddLogEntry("Warning", $"❌ Failed: {zipFile.FileName}");
            }
            
            return success;
        }
        catch (Exception ex)
        {
            UpdateFileProgress(zipFile, "Error", 100, $"Error: {ex.Message}");
            zipFile.Status = ZipFileStatus.Failed;
            zipFile.ErrorMessage = ex.Message;
            AddLogEntry("Error", $"❌ Error processing {zipFile.FileName}: {ex.Message}");
            return false;
        }
    }

    private string GetHealthStatusText(ArchiveHealthStatus status)
    {
        return status switch
        {
            ArchiveHealthStatus.Healthy => "✅ Healthy",
            ArchiveHealthStatus.PartiallyCorrupted => "⚠️ Partially Corrupted",
            ArchiveHealthStatus.Corrupted => "❌ Corrupted",
            ArchiveHealthStatus.UnsupportedFormat => "🚫 Unsupported",
            ArchiveHealthStatus.FileNotFound => "❓ Not Found",
            ArchiveHealthStatus.AccessDenied => "🚫 Access Denied",
            ArchiveHealthStatus.Unknown => "❓ Unknown",
            _ => "❓ Unknown"
        };
    }

    private void UpdateProgress()
    {
        if (TotalFiles > 0)
        {
            ProgressPercentage = (double)ProcessedFiles / TotalFiles * 100;
        }
    }

    public void AddLogEntry(string level, string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LogEntries.Add(new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message
            });
        });
    }
}
