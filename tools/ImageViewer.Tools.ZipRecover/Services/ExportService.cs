using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.IO;
using ImageViewer.Tools.ZipRecover.Models;

namespace ImageViewer.Tools.ZipRecover.Services;

/// <summary>
/// Service for exporting data to various formats
/// </summary>
public interface IExportService
{
    Task ExportZipFilesToCsvAsync(List<ZipFileItem> files, string filePath);
    Task ExportZipFilesToJsonAsync(List<ZipFileItem> files, string filePath);
    Task ExportLogsToCsvAsync(List<LogEntry> logs, string filePath);
    Task ExportLogsToJsonAsync(List<LogEntry> logs, string filePath);
    Task ExportUnmatchedLinesToCsvAsync(List<UnmatchedLineItem> lines, string filePath);
    Task ExportUnmatchedLinesToJsonAsync(List<UnmatchedLineItem> lines, string filePath);
    Task ExportSummaryReportAsync(List<ZipFileItem> files, List<LogEntry> logs, string filePath);
}

/// <summary>
/// Service for exporting data to CSV and JSON formats
/// </summary>
public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;

    public ExportService(ILogger<ExportService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Export ZIP files to CSV format
    /// </summary>
    public async Task ExportZipFilesToCsvAsync(List<ZipFileItem> files, string filePath)
    {
        try
        {
            var csv = new StringBuilder();
            csv.AppendLine("FileName,FilePath,ArchiveType,Status,HealthStatus,OriginalSize,RecoveredSize,ValidFiles,TotalFiles,CurrentStep,Progress,Details,ErrorMessage,IsSelected");

            foreach (var file in files)
            {
                csv.AppendLine($"{EscapeCsv(file.FileName)}," +
                             $"{EscapeCsv(file.FilePath)}," +
                             $"{EscapeCsv(file.ArchiveType)}," +
                             $"{EscapeCsv(file.StatusText)}," +
                             $"{EscapeCsv(file.HealthStatus)}," +
                             $"{file.OriginalSize}," +
                             $"{file.RecoveredSize}," +
                             $"{file.ValidFilesCount}," +
                             $"{file.TotalFilesCount}," +
                             $"{EscapeCsv(file.CurrentStep)}," +
                             $"{file.FileProgressPercentage:F1}," +
                             $"{EscapeCsv(file.ProcessingDetails)}," +
                             $"{EscapeCsv(file.ErrorMessage)}," +
                             $"{file.IsSelected}");
            }

            await File.WriteAllTextAsync(filePath, csv.ToString());
            _logger.LogInformation("Exported {Count} ZIP files to CSV: {FilePath}", files.Count, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export ZIP files to CSV: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Export ZIP files to JSON format
    /// </summary>
    public async Task ExportZipFilesToJsonAsync(List<ZipFileItem> files, string filePath)
    {
        try
        {
            var exportData = files.Select(file => new
            {
                FileName = file.FileName,
                FilePath = file.FilePath,
                ArchiveType = file.ArchiveType,
                Status = file.StatusText,
                HealthStatus = file.HealthStatus,
                OriginalSize = file.OriginalSize,
                RecoveredSize = file.RecoveredSize,
                ValidFiles = file.ValidFilesCount,
                TotalFiles = file.TotalFilesCount,
                CurrentStep = file.CurrentStep,
                Progress = file.FileProgressPercentage,
                Details = file.ProcessingDetails,
                ErrorMessage = file.ErrorMessage,
                IsSelected = file.IsSelected
            }).ToList();

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(filePath, json);
            _logger.LogInformation("Exported {Count} ZIP files to JSON: {FilePath}", files.Count, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export ZIP files to JSON: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Export logs to CSV format
    /// </summary>
    public async Task ExportLogsToCsvAsync(List<LogEntry> logs, string filePath)
    {
        try
        {
            var csv = new StringBuilder();
            csv.AppendLine("Timestamp,Level,Message");

            foreach (var log in logs)
            {
                csv.AppendLine($"{log.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                             $"{EscapeCsv(log.Level)}," +
                             $"{EscapeCsv(log.Message)}");
            }

            await File.WriteAllTextAsync(filePath, csv.ToString());
            _logger.LogInformation("Exported {Count} log entries to CSV: {FilePath}", logs.Count, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export logs to CSV: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Export logs to JSON format
    /// </summary>
    public async Task ExportLogsToJsonAsync(List<LogEntry> logs, string filePath)
    {
        try
        {
            var exportData = logs.Select(log => new
            {
                Timestamp = log.Timestamp,
                Level = log.Level,
                Message = log.Message
            }).ToList();

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(filePath, json);
            _logger.LogInformation("Exported {Count} log entries to JSON: {FilePath}", logs.Count, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export logs to JSON: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Export unmatched lines to CSV format
    /// </summary>
    public async Task ExportUnmatchedLinesToCsvAsync(List<UnmatchedLineItem> lines, string filePath)
    {
        try
        {
            var csv = new StringBuilder();
            csv.AppendLine("Line,ExtractedPath,Status,IsSelected,IsValidFile");

            foreach (var line in lines)
            {
                csv.AppendLine($"{EscapeCsv(line.Line)}," +
                             $"{EscapeCsv(line.ExtractedPath)}," +
                             $"{EscapeCsv(line.Status)}," +
                             $"{line.IsSelected}," +
                             $"{line.IsValidFile}");
            }

            await File.WriteAllTextAsync(filePath, csv.ToString());
            _logger.LogInformation("Exported {Count} unmatched lines to CSV: {FilePath}", lines.Count, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export unmatched lines to CSV: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Export unmatched lines to JSON format
    /// </summary>
    public async Task ExportUnmatchedLinesToJsonAsync(List<UnmatchedLineItem> lines, string filePath)
    {
        try
        {
            var exportData = lines.Select(line => new
            {
                Line = line.Line,
                ExtractedPath = line.ExtractedPath,
                Status = line.Status,
                IsSelected = line.IsSelected,
                IsValidFile = line.IsValidFile
            }).ToList();

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(filePath, json);
            _logger.LogInformation("Exported {Count} unmatched lines to JSON: {FilePath}", lines.Count, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export unmatched lines to JSON: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Export comprehensive summary report
    /// </summary>
    public async Task ExportSummaryReportAsync(List<ZipFileItem> files, List<LogEntry> logs, string filePath)
    {
        try
        {
            var summary = new
            {
                ExportDate = DateTime.Now,
                Summary = new
                {
                    TotalFiles = files.Count,
                    SelectedFiles = files.Count(f => f.IsSelected),
                    SuccessfulFiles = files.Count(f => f.Status == ZipFileStatus.Success),
                    FailedFiles = files.Count(f => f.Status == ZipFileStatus.Failed),
                    SkippedFiles = files.Count(f => f.Status == ZipFileStatus.Skipped),
                    PendingFiles = files.Count(f => f.Status == ZipFileStatus.Pending),
                    ProcessingFiles = files.Count(f => f.Status == ZipFileStatus.Processing)
                },
                FilesByStatus = files.GroupBy(f => f.StatusText)
                    .ToDictionary(g => g.Key, g => g.Count()),
                FilesByType = files.GroupBy(f => f.ArchiveType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                FilesByHealth = files.GroupBy(f => f.HealthStatus)
                    .ToDictionary(g => g.Key, g => g.Count()),
                Logs = logs.Select(log => new
                {
                    Timestamp = log.Timestamp,
                    Level = log.Level,
                    Message = log.Message
                }).ToList(),
                Files = files.Select(file => new
                {
                    FileName = file.FileName,
                    FilePath = file.FilePath,
                    ArchiveType = file.ArchiveType,
                    Status = file.StatusText,
                    HealthStatus = file.HealthStatus,
                    OriginalSize = file.OriginalSize,
                    RecoveredSize = file.RecoveredSize,
                    ValidFiles = file.ValidFilesCount,
                    TotalFiles = file.TotalFilesCount,
                    CurrentStep = file.CurrentStep,
                    Progress = file.FileProgressPercentage,
                    Details = file.ProcessingDetails,
                    ErrorMessage = file.ErrorMessage,
                    IsSelected = file.IsSelected
                }).ToList()
            };

            var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(filePath, json);
            _logger.LogInformation("Exported summary report: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export summary report: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Escape CSV field values
    /// </summary>
    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
