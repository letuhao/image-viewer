using System.IO;

namespace ImageViewer.Application.Helpers;

/// <summary>
/// Helper for repairing truncated ZIP file paths
/// </summary>
public static class PathRepairHelper
{
    /// <summary>
    /// Repair a potentially truncated ZIP file path by finding the actual file on disk
    /// </summary>
    public static string RepairTruncatedZipPath(string collectionPath, string potentiallyTruncatedPath)
    {
        if (string.IsNullOrEmpty(potentiallyTruncatedPath) || string.IsNullOrEmpty(collectionPath))
            return potentiallyTruncatedPath;

        // Check if this looks like a truncated ZIP path
        if (!IsTruncatedZipPath(potentiallyTruncatedPath))
            return potentiallyTruncatedPath;

        try
        {
            var directory = new DirectoryInfo(collectionPath);
            if (!directory.Exists)
                return potentiallyTruncatedPath;

            // Extract the base name from the truncated path
            var baseName = ExtractBaseName(potentiallyTruncatedPath);
            
            // Look for ZIP files that contain this base name
            var zipFiles = directory.GetFiles("*.zip", SearchOption.TopDirectoryOnly);
            
            foreach (var zipFile in zipFiles)
            {
                if (zipFile.Name.Contains(baseName, StringComparison.OrdinalIgnoreCase))
                {
                    // Found a matching ZIP file
                    var relativePath = Path.GetRelativePath(collectionPath, zipFile.FullName);
                    return relativePath;
                }
            }

            // If no ZIP file found, return the original path
            return potentiallyTruncatedPath;
        }
        catch
        {
            // If any error occurs, return the original path
            return potentiallyTruncatedPath;
        }
    }

    /// <summary>
    /// Check if a path looks like a truncated ZIP path
    /// </summary>
    private static bool IsTruncatedZipPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        // Look for patterns like "[Folder] Name" without # or ::
        // This suggests the path was truncated at the # character
        return path.StartsWith("[") && 
               path.Contains("] ") && 
               !path.Contains("#") && 
               !path.Contains("::") &&
               !path.EndsWith(".zip");
    }

    /// <summary>
    /// Extract the base name from a truncated path
    /// </summary>
    private static string ExtractBaseName(string truncatedPath)
    {
        // Extract the part after "] " from patterns like "[Folder] Name"
        var index = truncatedPath.IndexOf("] ");
        if (index > 0)
        {
            return truncatedPath.Substring(index + 2).Trim();
        }
        
        return truncatedPath;
    }
}
