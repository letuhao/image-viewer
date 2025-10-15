namespace ImageViewer.Application.Helpers;

/// <summary>
/// Centralized helper for filtering __MACOSX metadata files
/// Ensures consistent filtering across all components
/// </summary>
public static class MacOSXFilterHelper
{
    /// <summary>
    /// Checks if a path contains __MACOSX metadata
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <returns>True if the path contains __MACOSX metadata</returns>
    public static bool IsMacOSXPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        // Normalize path: convert backslashes to forward slashes and make lowercase
        var normalizedPath = path.Replace('\\', '/').ToLowerInvariant();
        
        // Check for various __MACOSX patterns:
        // - __MACOSX/ (standard folder)
        // - __macosx/ (lowercase variant)
        // - Files that start with __MACOSX
        // - Files in nested __MACOSX folders
        return normalizedPath.Contains("__macosx/") ||
               normalizedPath.StartsWith("__macosx/") ||
               normalizedPath.Contains("/__macosx/");
    }

    /// <summary>
    /// Filters out __MACOSX entries from an enumerable of archive entries
    /// </summary>
    /// <typeparam name="T">Archive entry type</typeparam>
    /// <param name="entries">Archive entries to filter</param>
    /// <param name="pathSelector">Function to get the path from the entry</param>
    /// <returns>Filtered entries without __MACOSX metadata</returns>
    public static IEnumerable<T> FilterMacOSXEntries<T>(
        IEnumerable<T> entries, 
        Func<T, string> pathSelector)
    {
        return entries.Where(entry => !IsMacOSXPath(pathSelector(entry)));
    }

    /// <summary>
    /// Checks if an archive entry is a __MACOSX metadata file
    /// Works with both SharpCompress (entry.Key) and ZipFile (entry.FullName)
    /// </summary>
    /// <param name="entryPath">The entry path (Key or FullName)</param>
    /// <returns>True if the entry is __MACOSX metadata</returns>
    public static bool IsMacOSXEntry(string? entryPath)
    {
        return IsMacOSXPath(entryPath);
    }

    /// <summary>
    /// Validates that a path is safe to process (not __MACOSX metadata)
    /// </summary>
    /// <param name="path">Path to validate</param>
    /// <param name="operation">Operation being performed (for logging)</param>
    /// <returns>True if safe to process</returns>
    public static bool IsSafeToProcess(string? path, string operation = "process")
    {
        if (IsMacOSXPath(path))
        {
            // Log the filtering for debugging
            //Console.WriteLine($"[MacOSXFilter] Filtered out __MACOSX entry during {operation}: {path}");
            return false;
        }
        return true;
    }
}
