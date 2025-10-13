using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ImageViewer.Tools.ZipRecover;

/// <summary>
/// Service for validating archive health
/// </summary>
public interface IArchiveHealthValidator
{
    Task<ArchiveHealthStatus> CheckArchiveHealthAsync(string archivePath);
}

/// <summary>
/// Archive health status
/// </summary>
public enum ArchiveHealthStatus
{
    Unknown,
    Healthy,
    Corrupted,
    PartiallyCorrupted,
    UnsupportedFormat,
    AccessDenied,
    FileNotFound
}

/// <summary>
/// Service for validating archive health using 7-Zip
/// </summary>
public class ArchiveHealthValidator : IArchiveHealthValidator
{
    private readonly ILogger<ArchiveHealthValidator> _logger;
    private readonly ZipRecoveryOptions _options;

    public ArchiveHealthValidator(ILogger<ArchiveHealthValidator> logger, IOptions<ZipRecoveryOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Check the health status of an archive file
    /// </summary>
    public async Task<ArchiveHealthStatus> CheckArchiveHealthAsync(string archivePath)
    {
        _logger.LogDebug("Checking health of archive: {ArchivePath}", archivePath);

        if (!File.Exists(archivePath))
        {
            _logger.LogWarning("Archive file not found: {ArchivePath}", archivePath);
            return ArchiveHealthStatus.FileNotFound;
        }

        var extension = Path.GetExtension(archivePath).ToLowerInvariant();
        if (!_options.SupportedExtensions.Contains(extension))
        {
            _logger.LogWarning("Unsupported archive format: {Extension} for {ArchivePath}", extension, archivePath);
            return ArchiveHealthStatus.UnsupportedFormat;
        }

        try
        {
            // Use 7-Zip to test archive integrity
            var healthStatus = await TestWithSevenZipAsync(archivePath);
            
            _logger.LogDebug("Archive health check result: {Status} for {ArchivePath}", healthStatus, archivePath);
            return healthStatus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking archive health: {ArchivePath}", archivePath);
            return ArchiveHealthStatus.Unknown;
        }
    }

    /// <summary>
    /// Test archive using 7-Zip command line tool
    /// </summary>
    private async Task<ArchiveHealthStatus> TestWithSevenZipAsync(string archivePath)
    {
        try
        {
            if (!File.Exists(_options.SevenZipPath))
            {
                _logger.LogWarning("7-Zip not found at {SevenZipPath}, using basic file validation", _options.SevenZipPath);
                return await BasicFileValidationAsync(archivePath);
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = _options.SevenZipPath,
                Arguments = $"t \"{archivePath}\" -y",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = !_options.ShowBatchWindows
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                _logger.LogError("Failed to start 7-Zip process for health check");
                return ArchiveHealthStatus.Unknown;
            }

            // Set timeout for health check
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_options.HealthCheckTimeoutSeconds));
            var processTask = process.WaitForExitAsync();

            var completedTask = await Task.WhenAny(processTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                _logger.LogWarning("Health check timeout for archive: {ArchivePath}", archivePath);
                try
                {
                    process.Kill();
                }
                catch
                {
                    // Ignore kill errors
                }
                return ArchiveHealthStatus.Unknown;
            }

            await processTask; // Ensure process has actually exited

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            return InterpretSevenZipResult(process.ExitCode, output, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during 7-Zip health check: {ArchivePath}", archivePath);
            return ArchiveHealthStatus.Unknown;
        }
    }

    /// <summary>
    /// Interpret 7-Zip test results
    /// </summary>
    private ArchiveHealthStatus InterpretSevenZipResult(int exitCode, string output, string error)
    {
        // 7-Zip exit codes:
        // 0 = Success (no errors)
        // 1 = Warning (some files could not be processed)
        // 2 = Fatal error
        // 7 = Command line error
        // 8 = Not enough memory
        // 255 = User stopped the process

        return exitCode switch
        {
            0 => ArchiveHealthStatus.Healthy,
            1 => ArchiveHealthStatus.PartiallyCorrupted,
            2 => ArchiveHealthStatus.Corrupted,
            7 => ArchiveHealthStatus.UnsupportedFormat,
            8 => ArchiveHealthStatus.Unknown,
            255 => ArchiveHealthStatus.Unknown,
            _ => ArchiveHealthStatus.Unknown
        };
    }

    /// <summary>
    /// Basic file validation when 7-Zip is not available
    /// </summary>
    private async Task<ArchiveHealthStatus> BasicFileValidationAsync(string archivePath)
    {
        try
        {
            var fileInfo = new FileInfo(archivePath);
            
            // Check if file is empty
            if (fileInfo.Length == 0)
            {
                return ArchiveHealthStatus.Corrupted;
            }

            // Try to read file header
            using var fileStream = File.OpenRead(archivePath);
            var buffer = new byte[Math.Min(1024, fileInfo.Length)];
            var bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);
            
            if (bytesRead == 0)
            {
                return ArchiveHealthStatus.Corrupted;
            }

            // Basic header validation for common formats
            var extension = Path.GetExtension(archivePath).ToLowerInvariant();
            var isValidHeader = ValidateArchiveHeader(buffer, extension);
            
            return isValidHeader ? ArchiveHealthStatus.Healthy : ArchiveHealthStatus.Corrupted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during basic file validation: {ArchivePath}", archivePath);
            return ArchiveHealthStatus.Unknown;
        }
    }

    /// <summary>
    /// Validate archive header based on file extension
    /// </summary>
    private bool ValidateArchiveHeader(byte[] header, string extension)
    {
        try
        {
            return extension switch
            {
                ".zip" => header.Length >= 4 && 
                          (header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04) || // ZIP
                          (header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x05 && header[3] == 0x06) || // ZIP empty
                          (header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x07 && header[3] == 0x08),   // ZIP spanned
                ".7z" => header.Length >= 6 && 
                         header[0] == 0x37 && header[1] == 0x7A && header[2] == 0xBC && 
                         header[3] == 0xAF && header[4] == 0x27 && header[5] == 0x1C,
                ".rar" => header.Length >= 7 && 
                          header[0] == 0x52 && header[1] == 0x61 && header[2] == 0x72 && 
                          header[3] == 0x21 && header[4] == 0x1A && header[5] == 0x07,
                ".tar" => header.Length >= 512, // TAR files have 512-byte headers
                ".gz" => header.Length >= 2 && header[0] == 0x1F && header[1] == 0x8B,
                ".bz2" => header.Length >= 3 && header[0] == 0x42 && header[1] == 0x5A && header[2] == 0x68,
                ".xz" => header.Length >= 6 && 
                         header[0] == 0xFD && header[1] == 0x37 && header[2] == 0x7A && 
                         header[3] == 0x58 && header[4] == 0x5A && header[5] == 0x00,
                ".cab" => header.Length >= 4 && 
                          header[0] == 0x4D && header[1] == 0x53 && header[2] == 0x43 && header[3] == 0x46,
                ".iso" => header.Length >= 2048 && 
                          header[2048] == 0x01 && header[2049] == 0x43 && header[2050] == 0x44 && header[2051] == 0x30,
                ".msi" => header.Length >= 8 && 
                          header[0] == 0xD0 && header[1] == 0xCF && header[2] == 0x11 && header[3] == 0xE0,
                _ => true // For unknown formats, assume valid if file can be read
            };
        }
        catch
        {
            return false;
        }
    }
}
