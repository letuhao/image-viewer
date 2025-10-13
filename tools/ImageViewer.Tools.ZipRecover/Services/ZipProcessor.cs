using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;

namespace ImageViewer.Tools.ZipRecover;

/// <summary>
/// Service for processing archive files - extraction, validation, and re-zipping
/// </summary>
public class ZipProcessor : IZipProcessor
{
    private readonly ILogger<ZipProcessor> _logger;
    private readonly ZipRecoveryOptions _options;
    private readonly IArchiveHealthValidator _healthValidator;
    
    // Callback for UI logging
    public Action<string, string>? UiLogCallback { get; set; }

    public ZipProcessor(ILogger<ZipProcessor> logger, IOptions<ZipRecoveryOptions> options, IArchiveHealthValidator healthValidator)
    {
        _logger = logger;
        _options = options.Value;
        _healthValidator = healthValidator;
    }
    
    /// <summary>
    /// Log to both standard logger and UI
    /// </summary>
    private void LogToUi(string level, string message)
    {
        _logger.LogInformation(message);
        UiLogCallback?.Invoke(level, message);
    }

    /// <summary>
    /// Recover a corrupted archive file by extracting valid content and re-zipping
    /// </summary>
    public async Task<bool> RecoverZipFileAsync(string archiveFilePath)
    {
        _logger.LogInformation("Starting recovery for archive file: {ArchiveFilePath}", archiveFilePath);
        
        if (!File.Exists(archiveFilePath))
        {
            _logger.LogError("Archive file not found: {ArchiveFilePath}", archiveFilePath);
            return false;
        }

        // Check archive health first
        if (_options.SkipHealthyArchives)
        {
            var healthStatus = await _healthValidator.CheckArchiveHealthAsync(archiveFilePath);
            
            if (healthStatus == ArchiveHealthStatus.Healthy)
            {
                _logger.LogInformation("Archive is healthy, skipping: {ArchiveFilePath}", archiveFilePath);
                return true; // Consider healthy archives as "successfully processed"
            }
            
            if (healthStatus == ArchiveHealthStatus.UnsupportedFormat)
            {
                _logger.LogWarning("Unsupported archive format, skipping: {ArchiveFilePath}", archiveFilePath);
                return false;
            }
            
            _logger.LogInformation("Archive health status: {Status} for {ArchiveFilePath}", healthStatus, archiveFilePath);
        }

        var originalFileName = Path.GetFileNameWithoutExtension(archiveFilePath);
        var originalDirectory = Path.GetDirectoryName(archiveFilePath) ?? Path.GetTempPath();
        var extension = Path.GetExtension(archiveFilePath).ToLowerInvariant();
        var tempExtractPath = Path.Combine(_options.TempDirectory, originalFileName);
        var backupPath = Path.Combine(_options.BackupDirectory, Path.GetFileName(archiveFilePath));
        var newArchivePath = Path.Combine(originalDirectory, $"{originalFileName}_recovered{extension}");

        try
        {
            // Step 1: Create directories
            Directory.CreateDirectory(_options.TempDirectory);
            Directory.CreateDirectory(_options.BackupDirectory);
            Directory.CreateDirectory(tempExtractPath);

            // Step 2: Extract using 7-Zip (supports many more formats than .NET ZipFile)
            var extractionSuccess = await ExtractWithSevenZipAsync(archiveFilePath, tempExtractPath);
            if (!extractionSuccess)
            {
                _logger.LogError("Failed to extract archive file: {ArchiveFilePath}", archiveFilePath);
                return false;
            }

            // Step 3: Validate extracted files
            var validFiles = await ValidateExtractedFilesAsync(tempExtractPath);
            if (validFiles.Count == 0)
            {
                _logger.LogWarning("No valid files found in archive: {ArchiveFilePath}", archiveFilePath);
                return false;
            }

            _logger.LogInformation("Found {Count} valid files in archive: {ArchiveFilePath}", validFiles.Count, archiveFilePath);

            // Step 4: Create new archive with valid files
            var newArchiveCreated = await CreateNewArchiveAsync(validFiles, newArchivePath, extension);
            if (!newArchiveCreated)
            {
                _logger.LogError("Failed to create new archive file: {NewArchivePath}", newArchivePath);
                return false;
            }

            // Step 5: Move original to backup (recycle bin simulation)
            await MoveToBackupAsync(archiveFilePath, backupPath);

            // Optional: validate recovered archive before replacing original
            if (_options.ValidateRecoveredArchive)
            {
                var recoveredOk = await ValidateRecoveredArchiveAsync(newArchivePath);
                if (!recoveredOk)
                {
                    _logger.LogWarning("Recovered archive failed validation: {NewArchivePath}", newArchivePath);
                    return false;
                }
            }

            // Step 6: Replace original with recovered archive
            File.Move(newArchivePath, archiveFilePath);

            _logger.LogInformation("Successfully recovered archive file: {ArchiveFilePath}", archiveFilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recovering archive file: {ArchiveFilePath}", archiveFilePath);
            return false;
        }
        finally
        {
            // Cleanup temp directory
            if (Directory.Exists(tempExtractPath))
            {
                try
                {
                    Directory.Delete(tempExtractPath, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cleanup temp directory: {TempPath}", tempExtractPath);
                }
            }
        }
    }

    /// <summary>
    /// Validate the recovered archive by attempting to list its contents with 7-Zip
    /// </summary>
    private async Task<bool> ValidateRecoveredArchiveAsync(string archivePath)
    {
        try
        {
            if (!File.Exists(archivePath))
            {
                return false;
            }

            // Prefer 7-Zip 't' (test) if available; fallback to ZipFile open for .zip
            if (File.Exists(_options.SevenZipPath))
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = _options.SevenZipPath,
                    Arguments = $"t -y -spf \"{archivePath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    return false;
                }
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    return true;
                }

                // If non-zero, still consider valid if 7z could open the archive and found entries
                var stdOut = await process.StandardOutput.ReadToEndAsync();
                if (!string.IsNullOrWhiteSpace(stdOut) && stdOut.Contains("Testing archive:"))
                {
                    return true;
                }

                return false;
            }

            // Fallback for .zip: open and enumerate entries
            if (Path.GetExtension(archivePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                using var archive = ZipFile.OpenRead(archivePath);
                return archive.Entries.Count >= 0; // will throw if invalid
            }

            return true; // best effort
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extract ZIP file using 7-Zip command line tool
    /// </summary>
    private async Task<bool> ExtractWithSevenZipAsync(string zipFilePath, string extractPath)
    {
        string? listFilePath = null;
        try
        {
            if (!File.Exists(_options.SevenZipPath))
            {
                _logger.LogWarning("7-Zip not found at {SevenZipPath}, falling back to .NET ZipFile", _options.SevenZipPath);
                return await ExtractWithDotNetZipFileAsync(zipFilePath, extractPath);
            }

            // Pass the archive path exactly as-is (quoted); rely on 7-Zip handling of brackets
            LogToUi("Information", $"🔍 Original path: {zipFilePath}");
            
            ProcessStartInfo processInfo;
            
            if (_options.ShowBatchWindows)
            {
                // Create a batch file to keep the window open for debugging
                var batchContent = $@"@echo off
echo Starting 7-Zip extraction...
echo Command: ""{_options.SevenZipPath}"" x -y -spf -scsUTF-8 -o""{extractPath}"" ""{zipFilePath}""
echo.
""{_options.SevenZipPath}"" x -y -spf -scsUTF-8 -o""{extractPath}"" ""{zipFilePath}""
echo.
echo 7-Zip finished with exit code: %ERRORLEVEL%
echo.
pause
";
                var batchFile = Path.Combine(Path.GetTempPath(), $"zip_recovery_debug_{Guid.NewGuid():N}.bat");
                await File.WriteAllTextAsync(batchFile, batchContent);
                
                processInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{batchFile}\"",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };
                
                LogToUi("Information", $"🪟 Created debug batch file: {batchFile}");
            }
            else
            {
                processInfo = new ProcessStartInfo
                {
                    FileName = _options.SevenZipPath,
                    Arguments = $"x -y -spf -scsUTF-8 -o\"{extractPath}\" \"{zipFilePath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
            }

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                _logger.LogError("Failed to start 7-Zip process");
                return false;
            }

            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                LogToUi("Success", $"✅ 7-Zip extraction successful for: {zipFilePath}");
                return true;
            }
            else
            {
                var errorOutput = await process.StandardError.ReadToEndAsync();
                var standardOutput = await process.StandardOutput.ReadToEndAsync();
                LogToUi("Error", $"❌ 7-Zip extraction failed with exit code {process.ExitCode}");
                LogToUi("Error", $"🔍 7-Zip Error Output: {errorOutput}");
                LogToUi("Error", $"📋 7-Zip Standard Output: {standardOutput}");
                LogToUi("Error", $"⚙️ 7-Zip Command: {processInfo.Arguments}");

                // If some files were extracted despite the error, continue with partial extraction
                try
                {
                    if (Directory.Exists(extractPath))
                    {
                        var anyExtracted = Directory.EnumerateFiles(extractPath, "*", SearchOption.AllDirectories).Any();
                        if (anyExtracted)
                        {
                            LogToUi("Warning", $"⚠️ 7-Zip returned exit code {process.ExitCode} but extracted files were found; continuing with partial extraction.");
                            return true;
                        }
                    }
                }
                catch { }

                // Try with different charset options as fallback
                if (await TryExtractionWithDifferentCharsets(zipFilePath, extractPath))
                {
                    return true;
                }

                // Try .NET ZipFile as final fallback
                return await ExtractWithDotNetZipFileAsync(zipFilePath, extractPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during 7-Zip extraction: {ZipFilePath}", zipFilePath);
            return await ExtractWithDotNetZipFileAsync(zipFilePath, extractPath);
        }
        finally
        {
        }
    }

    /// <summary>
    /// Try extraction with different charset options
    /// </summary>
    private async Task<bool> TryExtractionWithDifferentCharsets(string zipFilePath, string extractPath)
    {
        // Try different charset options
        var charsetOptions = new[] { "-scsUTF-8" };
        
        foreach (var charset in charsetOptions)
        {
            try
            {
                LogToUi("Information", $"🔄 Trying extraction with charset: {charset}");
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = _options.SevenZipPath,
                    Arguments = $"x -y -spf {charset} -o\"{extractPath}\" \"{zipFilePath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    continue;
                }

                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    LogToUi("Success", $"✅ 7-Zip extraction successful with charset {charset}");
                    return true;
                }
                else
                {
                    var errorOutput = await process.StandardError.ReadToEndAsync();
                    LogToUi("Warning", $"⚠️ Charset {charset} failed: {errorOutput.Trim()}");

                    // If some files were extracted despite the error, continue with partial extraction
                    try
                    {
                        if (Directory.Exists(extractPath))
                        {
                            var anyExtracted = Directory.EnumerateFiles(extractPath, "*", SearchOption.AllDirectories).Any();
                            if (anyExtracted)
                            {
                                LogToUi("Warning", $"⚠️ 7-Zip returned exit code {process.ExitCode} but extracted files were found; continuing with partial extraction.");
                                return true;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                LogToUi("Warning", $"⚠️ Error trying charset {charset}: {ex.Message}");
            }
        }
        
        LogToUi("Warning", "⚠️ All charset options failed, falling back to .NET ZipFile");
        return false;
    }

    /// <summary>
    /// Extract ZIP file using .NET ZipFile (fallback method)
    /// </summary>
    private async Task<bool> ExtractWithDotNetZipFileAsync(string zipFilePath, string extractPath)
    {
        try
        {
            await Task.Run(() =>
            {
                ZipFile.ExtractToDirectory(zipFilePath, extractPath, overwriteFiles: true);
            });
            
            _logger.LogDebug(".NET ZipFile extraction successful for: {ZipFilePath}", zipFilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ".NET ZipFile extraction failed for: {ZipFilePath}", zipFilePath);
            return false;
        }
    }

    /// <summary>
    /// Validate extracted files and return list of valid files
    /// </summary>
    private async Task<List<string>> ValidateExtractedFilesAsync(string extractPath)
    {
        var validFiles = new List<string>();
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

        try
        {
            var allFiles = Directory.GetFiles(extractPath, "*", SearchOption.AllDirectories);
            
            foreach (var file in allFiles)
            {
                var isValid = await ValidateFileAsync(file, imageExtensions);
                if (isValid)
                {
                    validFiles.Add(file);
                }
                else
                {
                    _logger.LogDebug("Skipping invalid file: {FilePath}", file);
                }
            }

            return validFiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating extracted files in: {ExtractPath}", extractPath);
            return validFiles;
        }
    }

    /// <summary>
    /// Validate individual file
    /// </summary>
    private async Task<bool> ValidateFileAsync(string filePath, string[] validExtensions)
    {
        try
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            // Check if file has valid extension
            if (!validExtensions.Contains(extension))
            {
                return false;
            }

            // Check if file can be read
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                return false;
            }

            // Try to read file header to validate it's not corrupted
            using var fileStream = File.OpenRead(filePath);
            var buffer = new byte[Math.Min(1024, fileInfo.Length)];
            var bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);
            
            if (bytesRead == 0)
            {
                return false;
            }

            // Basic validation based on file extension
            return ValidateFileHeader(buffer, extension);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "File validation failed for: {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Validate file header based on extension
    /// </summary>
    private bool ValidateFileHeader(byte[] header, string extension)
    {
        try
        {
            return extension switch
            {
                ".jpg" or ".jpeg" => header.Length >= 2 && header[0] == 0xFF && header[1] == 0xD8,
                ".png" => header.Length >= 8 && 
                          header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
                ".gif" => header.Length >= 6 && 
                          header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46,
                ".bmp" => header.Length >= 2 && header[0] == 0x42 && header[1] == 0x4D,
                ".webp" => header.Length >= 12 && 
                           header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                           header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
                _ => true // For unknown extensions, assume valid if file can be read
            };
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Create new archive with valid files
    /// </summary>
    private async Task<bool> CreateNewArchiveAsync(List<string> validFiles, string newArchivePath, string extension)
    {
        try
        {
            // For ZIP files, use .NET ZipFile for better compatibility
            if (extension == ".zip")
            {
                return await CreateNewZipAsync(validFiles, newArchivePath);
            }
            
            // For other formats, use 7-Zip
            return await CreateNewArchiveWithSevenZipAsync(validFiles, newArchivePath, extension);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating new archive file: {NewArchivePath}", newArchivePath);
            return false;
        }
    }

    /// <summary>
    /// Create new ZIP file with valid files using .NET ZipFile
    /// </summary>
    private async Task<bool> CreateNewZipAsync(List<string> validFiles, string newZipPath)
    {
        try
        {
            await Task.Run(() =>
            {
                using var archive = ZipFile.Open(newZipPath, ZipArchiveMode.Create);
                
                foreach (var file in validFiles)
                {
                    var relativePath = Path.GetRelativePath(Path.GetDirectoryName(validFiles.First())!, file);
                    archive.CreateEntryFromFile(file, relativePath);
                }
            });

            _logger.LogDebug("Created new ZIP file: {NewZipPath} with {Count} files", newZipPath, validFiles.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating new ZIP file: {NewZipPath}", newZipPath);
            return false;
        }
    }

    /// <summary>
    /// Create new archive with valid files using 7-Zip
    /// </summary>
    private async Task<bool> CreateNewArchiveWithSevenZipAsync(List<string> validFiles, string newArchivePath, string extension)
    {
        try
        {
            if (!File.Exists(_options.SevenZipPath))
            {
                _logger.LogError("7-Zip not found, cannot create {Extension} archive", extension);
                return false;
            }

            // Create a temporary directory with the files to archive
            var tempArchiveDir = Path.Combine(_options.TempDirectory, $"archive_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempArchiveDir);

            try
            {
                // Copy files to temp directory maintaining structure
                foreach (var file in validFiles)
                {
                    var relativePath = Path.GetRelativePath(Path.GetDirectoryName(validFiles.First())!, file);
                    var destPath = Path.Combine(tempArchiveDir, relativePath);
                    var destDir = Path.GetDirectoryName(destPath);
                    
                    if (!string.IsNullOrEmpty(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }
                    
                    File.Copy(file, destPath, true);
                }

                // Determine archive format for 7-Zip
                var archiveFormat = GetSevenZipFormat(extension);
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = _options.SevenZipPath,
                    Arguments = $"a \"{newArchivePath}\" \"{tempArchiveDir}\\*\" -t{archiveFormat} -y",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = !_options.ShowBatchWindows
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    _logger.LogError("Failed to start 7-Zip process for archive creation");
                    return false;
                }

                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    _logger.LogDebug("Created new {Extension} archive: {NewArchivePath} with {Count} files", 
                        extension, newArchivePath, validFiles.Count);
                    return true;
                }
                else
                {
                    var errorOutput = await process.StandardError.ReadToEndAsync();
                    _logger.LogError("7-Zip archive creation failed with exit code {ExitCode}: {Error}", 
                        process.ExitCode, errorOutput);
                    return false;
                }
            }
            finally
            {
                // Cleanup temp archive directory
                if (Directory.Exists(tempArchiveDir))
                {
                    try
                    {
                        Directory.Delete(tempArchiveDir, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to cleanup temp archive directory: {TempPath}", tempArchiveDir);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating new archive with 7-Zip: {NewArchivePath}", newArchivePath);
            return false;
        }
    }

    /// <summary>
    /// Get 7-Zip format parameter for archive creation
    /// </summary>
    private string GetSevenZipFormat(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".7z" => "7z",
            ".zip" => "zip",
            ".tar" => "tar",
            ".gz" => "gzip",
            ".bz2" => "bzip2",
            ".xz" => "xz",
            ".rar" => "rar",
            ".cab" => "cab",
            ".iso" => "iso",
            ".msi" => "msi",
            ".deb" => "deb",
            ".rpm" => "rpm",
            ".dmg" => "dmg",
            ".pkg" => "pkg",
            ".arj" => "arj",
            ".lzh" or ".lha" => "lzh",
            ".ace" => "ace",
            ".z" => "z",
            ".cpio" => "cpio",
            ".swm" => "swm",
            ".wim" => "wim",
            ".esd" => "esd",
            ".chm" => "chm",
            ".hfs" or ".hfsx" => "hfs",
            ".apk" => "zip", // APK is essentially a ZIP
            ".ipa" => "zip", // IPA is essentially a ZIP
            ".jar" or ".war" or ".ear" => "zip", // Java archives are ZIP-based
            ".cbz" => "zip", // CBZ is ZIP
            ".cbr" => "rar", // CBR is RAR
            ".cbt" => "tar", // CBT is TAR
            ".cb7" => "7z",  // CB7 is 7Z
            _ => "zip" // Default to ZIP for unknown formats
        };
    }

    /// <summary>
    /// Move original ZIP to backup directory (simulate recycle bin)
    /// </summary>
    private async Task MoveToBackupAsync(string originalPath, string backupPath)
    {
        try
        {
            await Task.Run(() =>
            {
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
                File.Move(originalPath, backupPath);
            });

            _logger.LogDebug("Moved original ZIP to backup: {BackupPath}", backupPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving ZIP to backup: {OriginalPath} -> {BackupPath}", originalPath, backupPath);
            throw;
        }
    }
}
