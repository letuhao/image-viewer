using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;

namespace ImageViewer.Tools.ZipRecover;

/// <summary>
/// Main service for coordinating ZIP recovery process
/// </summary>
public class ZipRecoveryService : IZipRecoveryService
{
    private readonly ILogger<ZipRecoveryService> _logger;
    private readonly ZipRecoveryOptions _options;
    private readonly IInputParser _inputParser;
    private readonly IZipProcessor _zipProcessor;

    public ZipRecoveryService(
        ILogger<ZipRecoveryService> logger,
        IOptions<ZipRecoveryOptions> options,
        IInputParser inputParser,
        IZipProcessor zipProcessor)
    {
        _logger = logger;
        _options = options.Value;
        _inputParser = inputParser;
        _zipProcessor = zipProcessor;
    }

    /// <summary>
    /// Process all corrupted ZIP files from input.txt
    /// </summary>
    public async Task ProcessCorruptedZipFilesAsync()
    {
        _logger.LogInformation("Starting ZIP recovery process...");
        
        try
        {
            // Parse input file to get ZIP file paths
            var inputFilePath = Path.Combine("data", "input.txt");
            var zipFilePaths = await _inputParser.ParseZipFilePathsAsync(inputFilePath);
            
            if (zipFilePaths.Count == 0)
            {
                _logger.LogWarning("No ZIP files found in input file");
                return;
            }

            _logger.LogInformation("Processing {Count} ZIP files...", zipFilePaths.Count);

            var successCount = 0;
            var failureCount = 0;

            // Process each ZIP file
            foreach (var zipFilePath in zipFilePaths)
            {
                try
                {
                    _logger.LogInformation("Processing ZIP file {Index}/{Total}: {ZipFilePath}", 
                        zipFilePaths.IndexOf(zipFilePath) + 1, zipFilePaths.Count, zipFilePath);

                    var success = await _zipProcessor.RecoverZipFileAsync(zipFilePath);
                    
                    if (success)
                    {
                        successCount++;
                        _logger.LogInformation("✅ Successfully recovered: {ZipFilePath}", zipFilePath);
                    }
                    else
                    {
                        failureCount++;
                        _logger.LogWarning("❌ Failed to recover: {ZipFilePath}", zipFilePath);
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    _logger.LogError(ex, "❌ Error processing ZIP file: {ZipFilePath}", zipFilePath);
                }

                // Add small delay between processing files
                await Task.Delay(100);
            }

            // Summary
            _logger.LogInformation("ZIP recovery process completed:");
            _logger.LogInformation("  ✅ Successfully recovered: {SuccessCount}", successCount);
            _logger.LogInformation("  ❌ Failed to recover: {FailureCount}", failureCount);
            _logger.LogInformation("  📊 Total processed: {TotalCount}", zipFilePaths.Count);
            
            if (failureCount > 0)
            {
                _logger.LogWarning("Some ZIP files could not be recovered. Check logs for details.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during ZIP recovery process: {Message}", ex.Message);
            throw;
        }
    }
}
