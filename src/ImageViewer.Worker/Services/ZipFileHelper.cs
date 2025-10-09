using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Worker.Services;

/// <summary>
/// Helper class for extracting files from ZIP archives
/// </summary>
public static class ZipFileHelper
{
    /// <summary>
    /// Extract a file from a ZIP archive to byte array
    /// Path format: zipfile.zip#entry.png
    /// </summary>
    public static async Task<byte[]?> ExtractZipEntryBytes(string zipEntryPath, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var parts = zipEntryPath.Split('#', 2);
            if (parts.Length != 2)
            {
                logger?.LogWarning("Invalid ZIP entry path format: {Path}", zipEntryPath);
                return null;
            }

            var zipPath = parts[0];
            var entryName = parts[1];

            if (!File.Exists(zipPath))
            {
                logger?.LogWarning("ZIP file not found: {Path}", zipPath);
                return null;
            }

            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.GetEntry(entryName);
            if (entry == null)
            {
                logger?.LogWarning("Entry {Entry} not found in ZIP {Zip}", entryName, zipPath);
                return null;
            }

            using var entryStream = entry.Open();
            using var memoryStream = new MemoryStream();
            await entryStream.CopyToAsync(memoryStream, cancellationToken);
            
            var bytes = memoryStream.ToArray();
            logger?.LogDebug("Extracted {Size} bytes from ZIP entry {Entry}", bytes.Length, entryName);
            
            return bytes;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error extracting ZIP entry: {Path}", zipEntryPath);
            return null;
        }
    }

    /// <summary>
    /// Check if a path is a ZIP entry (contains #)
    /// </summary>
    public static bool IsZipEntryPath(string path)
    {
        return !string.IsNullOrEmpty(path) && path.Contains("#");
    }

    /// <summary>
    /// Split ZIP entry path into ZIP file path and entry name
    /// </summary>
    public static (string zipPath, string entryName) SplitZipEntryPath(string zipEntryPath)
    {
        var parts = zipEntryPath.Split('#', 2);
        if (parts.Length == 2)
        {
            return (parts[0], parts[1]);
        }
        return (zipEntryPath, string.Empty);
    }
}

