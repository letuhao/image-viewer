using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace ImageViewer.Infrastructure.Services;

/// <summary>
/// Long path handler for Windows systems
/// </summary>
public class LongPathHandler
{
    private readonly ILogger<LongPathHandler> _logger;
    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public LongPathHandler(ILogger<LongPathHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Check if path exists safely, handling long paths on Windows
    /// </summary>
    public static bool PathExistsSafe(string path)
    {
        try
        {
            if (IsWindows && path.Length > 260)
            {
                // Use UNC path for long paths on Windows
                var uncPath = path.StartsWith("\\\\") ? path : $"\\\\?\\{path}";
                return Directory.Exists(uncPath) || File.Exists(uncPath);
            }
            
            return Directory.Exists(path) || File.Exists(path);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Get long path version of a path for Windows
    /// </summary>
    public static string GetLongPath(string path)
    {
        if (IsWindows && path.Length > 260 && !path.StartsWith("\\\\?\\"))
        {
            return $"\\\\?\\{path}";
        }
        return path;
    }

    /// <summary>
    /// Get directory info safely for long paths
    /// </summary>
    public static DirectoryInfo? GetDirectoryInfoSafe(string path)
    {
        try
        {
            var longPath = GetLongPath(path);
            return new DirectoryInfo(longPath);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Get file info safely for long paths
    /// </summary>
    public static FileInfo? GetFileInfoSafe(string path)
    {
        try
        {
            var longPath = GetLongPath(path);
            return new FileInfo(longPath);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Get directories safely for long paths
    /// </summary>
    public static string[] GetDirectoriesSafe(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        try
        {
            var longPath = GetLongPath(path);
            return Directory.GetDirectories(longPath, searchPattern, searchOption);
        }
        catch (Exception ex)
        {
            throw new DirectoryNotFoundException($"Error accessing directory {path}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get files safely for long paths
    /// </summary>
    public static string[] GetFilesSafe(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        try
        {
            var longPath = GetLongPath(path);
            return Directory.GetFiles(longPath, searchPattern, searchOption);
        }
        catch (Exception ex)
        {
            throw new DirectoryNotFoundException($"Error accessing directory {path}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Create directory safely for long paths
    /// </summary>
    public static DirectoryInfo CreateDirectorySafe(string path)
    {
        try
        {
            var longPath = GetLongPath(path);
            return Directory.CreateDirectory(longPath);
        }
        catch (Exception ex)
        {
            throw new DirectoryNotFoundException($"Error creating directory {path}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Delete directory safely for long paths
    /// </summary>
    public static void DeleteDirectorySafe(string path, bool recursive = false)
    {
        try
        {
            var longPath = GetLongPath(path);
            Directory.Delete(longPath, recursive);
        }
        catch (Exception ex)
        {
            throw new DirectoryNotFoundException($"Error deleting directory {path}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Delete file safely for long paths
    /// </summary>
    public static void DeleteFileSafe(string path)
    {
        try
        {
            var longPath = GetLongPath(path);
            File.Delete(longPath);
        }
        catch (Exception ex)
        {
            throw new FileNotFoundException($"Error deleting file {path}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Read all bytes safely for long paths
    /// </summary>
    public static byte[] ReadAllBytesSafe(string path)
    {
        try
        {
            var longPath = GetLongPath(path);
            return File.ReadAllBytes(longPath);
        }
        catch (Exception ex)
        {
            throw new FileNotFoundException($"Error reading file {path}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Write all bytes safely for long paths
    /// </summary>
    public static void WriteAllBytesSafe(string path, byte[] bytes)
    {
        try
        {
            var longPath = GetLongPath(path);
            File.WriteAllBytes(longPath, bytes);
        }
        catch (Exception ex)
        {
            throw new FileNotFoundException($"Error writing file {path}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Copy file safely for long paths
    /// </summary>
    public static void CopyFileSafe(string sourcePath, string destPath, bool overwrite = false)
    {
        try
        {
            var longSourcePath = GetLongPath(sourcePath);
            var longDestPath = GetLongPath(destPath);
            File.Copy(longSourcePath, longDestPath, overwrite);
        }
        catch (Exception ex)
        {
            throw new FileNotFoundException($"Error copying file from {sourcePath} to {destPath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Move file safely for long paths
    /// </summary>
    public static void MoveFileSafe(string sourcePath, string destPath)
    {
        try
        {
            var longSourcePath = GetLongPath(sourcePath);
            var longDestPath = GetLongPath(destPath);
            File.Move(longSourcePath, longDestPath);
        }
        catch (Exception ex)
        {
            throw new FileNotFoundException($"Error moving file from {sourcePath} to {destPath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get file size safely for long paths
    /// </summary>
    public static long GetFileSizeSafe(string path)
    {
        try
        {
            var fileInfo = GetFileInfoSafe(path);
            return fileInfo?.Length ?? 0;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>
    /// Get last write time safely for long paths
    /// </summary>
    public static DateTime GetLastWriteTimeSafe(string path)
    {
        try
        {
            var fileInfo = GetFileInfoSafe(path);
            return fileInfo?.LastWriteTime ?? DateTime.MinValue;
        }
        catch (Exception)
        {
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// Check if path is a long path on Windows
    /// </summary>
    public static bool IsLongPath(string path)
    {
        return IsWindows && path.Length > 260;
    }

    /// <summary>
    /// Normalize path for current platform
    /// </summary>
    public static string NormalizePath(string path)
    {
        if (IsWindows)
        {
            return path.Replace('/', '\\');
        }
        return path.Replace('\\', '/');
    }
}
