using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using System.IO;

namespace ImageViewer.Tools.ZipRecover;

/// <summary>
/// Parser for extracting archive file paths from log input
/// </summary>
public class InputParser : IInputParser
{
    private readonly ILogger<InputParser> _logger;
    private readonly ZipRecoveryOptions _options;

    public InputParser(ILogger<InputParser> logger, IOptions<ZipRecoveryOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Lines that didn't match the regex pattern - for manual review
    /// </summary>
    public List<string> UnmatchedLines { get; private set; } = new();

    /// <summary>
    /// Parse input.txt to extract archive file paths from log entries
    /// </summary>
    public async Task<List<string>> ParseArchiveFilePathsAsync(string inputFilePath)
    {
        _logger.LogInformation("Parsing input file: {InputFilePath}", inputFilePath);
        
        if (!File.Exists(inputFilePath))
        {
            _logger.LogError("Input file not found: {InputFilePath}", inputFilePath);
            throw new FileNotFoundException($"Input file not found: {inputFilePath}");
        }

        var archivePaths = new List<string>();
        var lines = await File.ReadAllLinesAsync(inputFilePath);
        
        // Create regex pattern for all supported extensions
        var extensionsPattern = string.Join("|", _options.SupportedExtensions.Select(ext => ext.TrimStart('.').Replace(".", "\\.")));
        
        // Permissive regex pattern - catches any path that looks like an archive file
        // We'll rely on File.Exists() to filter out invalid paths
        // This pattern is intentionally broad to catch various path formats and languages
        // Uses non-greedy match (.+?) to capture the full path including the extension
        var archivePathPattern = $@"Error checking images in compressed file\s+(.+?\.({extensionsPattern}))\.\s+File will be skipped\.";
        
        _logger.LogDebug("Using regex pattern: {Pattern}", archivePathPattern);
        
        var totalMatches = 0;
        var validFiles = 0;
        var invalidPaths = 0;
        var unmatchedLines = new List<string>();
        
        foreach (var line in lines)
        {
            // Use permissive regex matching - catches any path that looks like an archive
            var match = Regex.Match(line, archivePathPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
            {
                totalMatches++;
                var archivePath = match.Groups[1].Value;
                var extension = Path.GetExtension(archivePath).ToLowerInvariant();
                
                // Log the matched path for debugging
                _logger.LogDebug("Regex matched path: {ArchivePath} (Extension: {Extension})", archivePath, extension);
                
                // File.Exists() is our filter - only add files that actually exist
                if (File.Exists(archivePath))
                {
                    archivePaths.Add(archivePath);
                    validFiles++;
                    _logger.LogDebug("✅ Valid archive file: {ArchivePath} (Type: {Extension})", archivePath, extension);
                }
                else
                {
                    invalidPaths++;
                    _logger.LogDebug("❌ File not found (filtered out): {ArchivePath}", archivePath);
                }
            }
            else
            {
                // Collect unmatched lines for manual review
                unmatchedLines.Add(line);
            }
        }
        
        _logger.LogInformation("Regex matching results: {TotalMatches} matches, {ValidFiles} valid files, {InvalidPaths} invalid paths, {UnmatchedLines} unmatched lines", 
            totalMatches, validFiles, invalidPaths, unmatchedLines.Count);

        // Remove duplicates while preserving order
        var uniqueArchivePaths = archivePaths.Distinct().ToList();
        
        _logger.LogInformation("Found {Count} unique archive files to process", uniqueArchivePaths.Count);
        
        // Log breakdown by extension
        var extensionGroups = uniqueArchivePaths
            .GroupBy(path => Path.GetExtension(path).ToLowerInvariant())
            .OrderBy(g => g.Key);
            
        foreach (var group in extensionGroups)
        {
            _logger.LogInformation("  {Extension}: {Count} files", group.Key, group.Count());
        }
        
        // Store unmatched lines for manual review
        UnmatchedLines = unmatchedLines;
        
        return uniqueArchivePaths;
    }

    /// <summary>
    /// Legacy method for backward compatibility
    /// </summary>
    public async Task<List<string>> ParseZipFilePathsAsync(string inputFilePath)
    {
        return await ParseArchiveFilePathsAsync(inputFilePath);
    }

    /// <summary>
    /// Test method to verify permissive regex pattern works with various path formats
    /// </summary>
    public void TestPermissiveRegexPattern()
    {
        var extensionsPattern = string.Join("|", _options.SupportedExtensions.Select(ext => ext.TrimStart('.').Replace(".", "\\.")));
        var archivePathPattern = $@"Error checking images in compressed file\s+(.+?\.({extensionsPattern}))\.\s+File will be skipped\.";
        
        // Test cases with different languages and path formats
        var testCases = new[]
        {
            // Your actual example
            @"Error checking images in compressed file L:\Downloads\Torrents\_Complete\勤劳的通同志 - Suisei Hoshimachi (Hololive) AI Generated.zip. File will be skipped.",
            // Various path formats
            @"Error checking images in compressed file C:\Users\Documents\file.zip. File will be skipped.",
            @"Error checking images in compressed file \\server\share\file.zip. File will be skipped.",
            @"Error checking images in compressed file D:\My Files\日本語ファイル名.zip. File will be skipped.",
            @"Error checking images in compressed file E:\Downloads\한국어파일명.zip. File will be skipped.",
            @"Error checking images in compressed file F:\Archive\ملف_عربي.zip. File will be skipped.",
            @"Error checking images in compressed file G:\Data\русский_файл.zip. File will be skipped.",
            @"Error checking images in compressed file H:\Mixed_混合_파일.zip. File will be skipped.",
            // Different archive formats
            @"Error checking images in compressed file I:\archive.7z. File will be skipped.",
            @"Error checking images in compressed file J:\archive.rar. File will be skipped.",
            @"Error checking images in compressed file K:\archive.tar.gz. File will be skipped."
        };

        _logger.LogInformation("Testing permissive regex pattern...");
        _logger.LogInformation("Pattern: {Pattern}", archivePathPattern);
        
        foreach (var testCase in testCases)
        {
            var match = Regex.Match(testCase, archivePathPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
            {
                var path = match.Groups[1].Value;
                _logger.LogInformation("✅ SUCCESS: Matched path: {Path}", path);
            }
            else
            {
                _logger.LogWarning("❌ FAILED: Could not match: {TestCase}", testCase);
            }
        }
        
        _logger.LogInformation("Permissive regex test completed. File.Exists() will filter out invalid paths.");
    }

    /// <summary>
    /// Manually extract archive path from a line (for unmatched lines)
    /// </summary>
    public string? ExtractPathFromLine(string line)
    {
        try
        {
            // Try to find any path that ends with a supported extension
            var extensionsPattern = string.Join("|", _options.SupportedExtensions.Select(ext => ext.TrimStart('.').Replace(".", "\\.")));
            var pathPattern = $@"(.+?\.({extensionsPattern}))";
            
            var match = Regex.Match(line, pathPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
            {
                var path = match.Groups[1].Value;
                _logger.LogDebug("Manually extracted path: {Path} from line: {Line}", path, line);
                return path;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting path from line: {Line}", line);
            return null;
        }
    }
}
