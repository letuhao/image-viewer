using System.IO.Compression;
using SharpCompress.Archives;
using SharpCompress.Common;
using Microsoft.Extensions.Logging;
using ImageViewer.Application.Helpers;

namespace ImageViewer.Worker.Services;

/// <summary>
/// Helper class for extracting files from compressed archives (ZIP, 7Z, RAR, TAR, CBZ, CBR)
/// Uses SharpCompress for multi-format support
/// </summary>
public static class ArchiveFileHelper
{
    /// <summary>
    /// Extract a file from a compressed archive to byte array
    /// Path format: archive.zip#entry.png
    /// Supports: ZIP, 7Z, RAR, TAR, CBZ, CBR, and more
    /// </summary>
    public static async Task<byte[]?> ExtractArchiveEntryBytes(string archiveEntryPath, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try :: separator first, then fallback to # for backward compatibility
            var parts = archiveEntryPath.Split(new[] { "::" }, 2, StringSplitOptions.None);
            if (parts.Length != 2)
            {
                parts = archiveEntryPath.Split('#', 2);
                if (parts.Length != 2)
                {
                    logger?.LogWarning("Invalid archive entry path format: {Path}", archiveEntryPath);
                    return null;
                }
            }

            var archivePath = parts[0];
            var entryName = parts[1];

            if (!File.Exists(archivePath))
            {
                logger?.LogWarning("Archive file not found: {Path}", archivePath);
                return null;
            }

            // Use SharpCompress to support multiple archive formats
            using var archive = ArchiveFactory.Open(archivePath);
            var entry = archive.Entries.FirstOrDefault(e => 
                !e.IsDirectory && 
                MacOSXFilterHelper.IsSafeToProcess(e.Key, "archive entry extraction") &&
                (e.Key == entryName || e.Key.Replace('\\', '/') == entryName.Replace('\\', '/')));
            
            if (entry == null)
            {
                logger?.LogWarning("Entry {Entry} not found in archive {Archive}", entryName, archivePath);
                return null;
            }

            using var entryStream = entry.OpenEntryStream();
            using var memoryStream = new MemoryStream();
            await entryStream.CopyToAsync(memoryStream, cancellationToken);
            
            var bytes = memoryStream.ToArray();
            logger?.LogDebug("Extracted {Size} bytes from archive entry {Entry}", bytes.Length, entryName);
            
            return bytes;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error extracting archive entry: {Path}", archiveEntryPath);
            return null;
        }
    }

    /// <summary>
    /// Check if a path is an archive entry (contains #)
    /// </summary>
    public static bool IsArchiveEntryPath(string path)
    {
        return !string.IsNullOrEmpty(path) && (path.Contains("::") || path.Contains("#"));
    }

    /// <summary>
    /// Get the uncompressed size of an archive entry without extracting it
    /// Path format: archive.zip#entry.png
    /// </summary>
    public static long GetArchiveEntrySize(string archiveEntryPath, ILogger? logger = null)
    {
        try
        {
            // Try :: separator first, then fallback to # for backward compatibility
            var parts = archiveEntryPath.Split(new[] { "::" }, 2, StringSplitOptions.None);
            if (parts.Length != 2)
            {
                parts = archiveEntryPath.Split('#', 2);
                if (parts.Length != 2)
                {
                    logger?.LogWarning("Invalid archive entry path format: {Path}", archiveEntryPath);
                    return 0;
                }
            }

            var archivePath = parts[0];
            var entryName = parts[1];

            if (!File.Exists(archivePath))
            {
                logger?.LogWarning("Archive file not found: {Path}", archivePath);
                return 0;
            }

            // Use SharpCompress to get entry metadata without extraction
            using var archive = ArchiveFactory.Open(archivePath);
            var entry = archive.Entries.FirstOrDefault(e => 
                !e.IsDirectory && 
                MacOSXFilterHelper.IsSafeToProcess(e.Key, "archive entry size check") &&
                (e.Key == entryName || e.Key.Replace('\\', '/') == entryName.Replace('\\', '/')));
            
            if (entry == null)
            {
                logger?.LogWarning("Entry {Entry} not found in archive {Archive}", entryName, archivePath);
                return 0;
            }

            // Return uncompressed size
            return entry.Size;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error getting archive entry size: {Path}", archiveEntryPath);
            return 0;
        }
    }

    /// <summary>
    /// Split archive entry path into archive file path and entry name
    /// </summary>
    public static (string archivePath, string entryName) SplitArchiveEntryPath(string archiveEntryPath)
    {
        // Use :: as separator to avoid conflicts with # in filenames
        var parts = archiveEntryPath.Split(new[] { "::" }, 2, StringSplitOptions.None);
        if (parts.Length == 2)
        {
            return (parts[0], parts[1]);
        }
        
        // Fallback to # for backward compatibility
        var hashParts = archiveEntryPath.Split('#', 2);
        if (hashParts.Length == 2)
        {
            return (hashParts[0], hashParts[1]);
        }
        
        return (archiveEntryPath, string.Empty);
    }

    // Keep old method names for backward compatibility
    public static bool IsZipEntryPath(string path) => IsArchiveEntryPath(path);
    public static (string zipPath, string entryName) SplitZipEntryPath(string zipEntryPath) => SplitArchiveEntryPath(zipEntryPath);
    public static Task<byte[]?> ExtractZipEntryBytes(string archiveEntryPath, ILogger? logger = null, CancellationToken cancellationToken = default) 
        => ExtractArchiveEntryBytes(archiveEntryPath, logger, cancellationToken);
}

