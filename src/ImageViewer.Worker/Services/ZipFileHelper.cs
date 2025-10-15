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
public static class ZipFileHelper
{
    /// <summary>
    /// Extract a file from a compressed archive to byte array
    /// Path format: archive.zip#entry.png
    /// Supports: ZIP, 7Z, RAR, TAR, CBZ, CBR, and more
    /// </summary>
    public static async Task<byte[]?> ExtractZipEntryBytes(string archiveEntryPath, ILogger? logger = null, CancellationToken cancellationToken = default)
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

            // Check if the entry is a __MACOSX metadata file before processing
            if (!MacOSXFilterHelper.IsSafeToProcess(entryName, "ZIP entry extraction"))
            {
                logger?.LogDebug("Skipping __MACOSX metadata entry: {Entry}", entryName);
                return null;
            }

            // Use SharpCompress to support multiple archive formats
            using var archive = ArchiveFactory.Open(archivePath);
            var entry = archive.Entries.FirstOrDefault(e => 
                !e.IsDirectory && 
                MacOSXFilterHelper.IsSafeToProcess(e.Key, "archive entry lookup") &&
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
    /// Check if a path is a ZIP entry (contains #)
    /// </summary>
    public static bool IsZipEntryPath(string path)
    {
        return !string.IsNullOrEmpty(path) && (path.Contains("::") || path.Contains("#"));
    }

    /// <summary>
    /// Split ZIP entry path into ZIP file path and entry name
    /// </summary>
    public static (string zipPath, string entryName) SplitZipEntryPath(string zipEntryPath)
    {
        // Try :: separator first, then fallback to # for backward compatibility
        var parts = zipEntryPath.Split(new[] { "::" }, 2, StringSplitOptions.None);
        if (parts.Length == 2)
        {
            return (parts[0], parts[1]);
        }
        
        parts = zipEntryPath.Split('#', 2);
        if (parts.Length == 2)
        {
            return (parts[0], parts[1]);
        }
        
        return (zipEntryPath, string.Empty);
    }
}

