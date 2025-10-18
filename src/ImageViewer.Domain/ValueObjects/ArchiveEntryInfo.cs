using MongoDB.Bson.Serialization.Attributes;
using ImageViewer.Domain.Enums;

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Information about a file entry inside an archive (ZIP, 7Z, RAR, etc.)
/// 中文：存档文件内部条目信息
/// Tiếng Việt: Thông tin mục tin bên trong tệp lưu trữ
/// </summary>
public class ArchiveEntryInfo
{
    /// <summary>
    /// Safe separator for combining archive path and entry name in display format
    /// Uses :: which is forbidden in both Windows and Linux filenames
    /// </summary>
    public const string DISPLAY_PATH_SEPARATOR = "::";
    [BsonElement("archivePath")]
    public string ArchivePath { get; set; } = string.Empty;

    [BsonElement("entryName")]
    public string EntryName { get; set; } = string.Empty;

    [BsonElement("entryPath")]
    public string EntryPath { get; set; } = string.Empty;

    /// <summary>
    /// DEPRECATED: Use IsArchiveEntry instead. This property is confusing and will be removed in future versions.
    /// Indicates whether this entry should be processed as an archive entry (false) or regular file (true).
    /// </summary>
    [BsonElement("isDirectory")]
    [Obsolete("Use IsArchiveEntry property instead. This naming is confusing.")]
    public bool IsDirectory { get; set; }

    [BsonElement("compressedSize")]
    public long CompressedSize { get; set; }

    [BsonElement("uncompressedSize")]
    public long UncompressedSize { get; set; }

    [BsonElement("fileType")]
    public ImageFileType FileType { get; set; } = ImageFileType.ArchiveEntry;

    /// <summary>
    /// Indicates whether this entry is inside an archive file (true) or a regular file (false)
    /// This is the clearer replacement for the confusing IsDirectory property
    /// </summary>
    public bool IsArchiveEntry => !IsDirectory;

    /// <summary>
    /// Indicates whether this entry is a regular file (not inside an archive)
    /// This is the clearer replacement for the confusing IsDirectory property
    /// </summary>
    public bool IsRegularFile => IsDirectory;

    ///// <summary>
    ///// Get the full path for this archive entry
    ///// Format: "archive.zip::entry.jpg" for display/logging purposes
    ///// </summary>
    //public string GetFullPath() => $"{ArchivePath}::{EntryName}";

    /// <summary>
    /// Get the display name for this entry (just the filename)
    /// </summary>
    public string GetDisplayName() => Path.GetFileName(EntryName);

    /// <summary>
    /// Check if this is a valid archive entry
    /// </summary>
    public bool IsValid() => !string.IsNullOrEmpty(ArchivePath) && !string.IsNullOrEmpty(EntryName);
    
    /// <summary>
    /// Get full path of physical file on the directory
    /// </summary>
    /// <returns></returns>
    public string GetPhysicalFileFullPath() => Path.Combine(ArchivePath, EntryName);

    /// <summary>
    /// Get the full path for this archive entry in display format
    /// Format: "archive.zip::entry.jpg" for display/logging purposes
    /// Uses :: separator which is forbidden in both Windows and Linux filenames
    /// </summary>
    public string GetDisplayPath() => $"{ArchivePath}{DISPLAY_PATH_SEPARATOR}{EntryName}";

    /// <summary>
    /// Parse a display path back into ArchiveEntryInfo
    /// Format: "archive.zip::entry.jpg"
    /// </summary>
    /// <param name="displayPath">Display path with :: separator</param>
    /// <returns>ArchiveEntryInfo or null if format is invalid</returns>
    public static ArchiveEntryInfo? FromDisplayPath(string displayPath)
    {
        if (string.IsNullOrEmpty(displayPath))
            return null;

        // Split on :: separator (safe because :: cannot appear in filenames)
        var parts = displayPath.Split(new[] { DISPLAY_PATH_SEPARATOR }, 2, StringSplitOptions.None);
        if (parts.Length == 2)
        {
            return ForArchiveEntry(parts[0], parts[1]);
        }

        return null;
    }

    /// <summary>
    /// Validate that a path string is safe to use as a display path
    /// Checks that the separator doesn't appear in the path components
    /// </summary>
    /// <param name="archivePath">Path to the archive</param>
    /// <param name="entryName">Name of the entry</param>
    /// <returns>True if safe to combine, false if separator would conflict</returns>
    public static bool IsSafeToCombine(string archivePath, string entryName)
    {
        return !string.IsNullOrEmpty(archivePath) && 
               !string.IsNullOrEmpty(entryName) &&
               !archivePath.Contains(DISPLAY_PATH_SEPARATOR) &&
               !entryName.Contains(DISPLAY_PATH_SEPARATOR);
    }


    #region Static Factory Methods

    /// <summary>
    /// Create an ArchiveEntryInfo for a regular file (not in an archive)
    /// </summary>
    /// <param name="filePath">Full path to the file</param>
    /// <param name="fileName">Name of the file (optional, will be extracted from path if not provided)</param>
    /// <returns>ArchiveEntryInfo configured for a regular file</returns>
    public static ArchiveEntryInfo ForRegularFile(string filePath, string? fileName = null)
    {
        var directory = Path.GetDirectoryName(filePath) ?? "";
        var name = fileName ?? Path.GetFileName(filePath);
        
        return new ArchiveEntryInfo
        {
            ArchivePath = directory,
            EntryName = name,
            EntryPath = name,
            IsDirectory = true, // Regular file (not archive entry)
            FileType = ImageFileType.RegularFile,
            CompressedSize = 0,
            UncompressedSize = 0
        };
    }

    /// <summary>
    /// Create an ArchiveEntryInfo for an entry inside an archive file
    /// </summary>
    /// <param name="archivePath">Path to the archive file</param>
    /// <param name="entryName">Name of the entry inside the archive</param>
    /// <param name="entryPath">Full path of the entry inside the archive (optional, defaults to entryName)</param>
    /// <param name="compressedSize">Compressed size of the entry (optional)</param>
    /// <param name="uncompressedSize">Uncompressed size of the entry (optional)</param>
    /// <returns>ArchiveEntryInfo configured for an archive entry</returns>
    public static ArchiveEntryInfo ForArchiveEntry(string archivePath, string entryName, 
        string? entryPath = null, long compressedSize = 0, long uncompressedSize = 0)
    {
        return new ArchiveEntryInfo
        {
            ArchivePath = archivePath,
            EntryName = entryName,
            EntryPath = entryPath ?? entryName,
            IsDirectory = false, // Archive entry
            FileType = ImageFileType.ArchiveEntry,
            CompressedSize = compressedSize,
            UncompressedSize = uncompressedSize
        };
    }

    /// <summary>
    /// Create an ArchiveEntryInfo from a collection and media file info
    /// Automatically determines if it's a regular file or archive entry based on collection type
    /// </summary>
    /// <param name="collectionPath">Path to the collection</param>
    /// <param name="collectionType">Type of the collection (Folder or Archive)</param>
    /// <param name="fileName">Name of the file</param>
    /// <param name="fileSize">Size of the file (optional)</param>
    /// <returns>ArchiveEntryInfo configured based on collection type</returns>
    public static ArchiveEntryInfo FromCollection(string collectionPath, CollectionType collectionType, 
        string fileName, long fileSize = 0)
    {
        if (collectionType == CollectionType.Folder)
        {
            // Regular file in a folder collection
            return ForRegularFile(Path.Combine(collectionPath, fileName), fileName);
        }
        else
        {
            // Entry inside an archive collection
            return ForArchiveEntry(collectionPath, fileName, fileName, 0, fileSize);
        }
    }


    /// <summary>
    /// Create a complete ArchiveEntryInfo with all properties set
    /// This is the recommended method for creating ArchiveEntryInfo objects
    /// </summary>
    /// <param name="archivePath">Path to the archive file or directory</param>
    /// <param name="entryName">Name of the entry</param>
    /// <param name="isArchiveEntry">True if this is an entry inside an archive, false if it's a regular file</param>
    /// <param name="entryPath">Full path of the entry inside the archive (optional, defaults to entryName)</param>
    /// <param name="fileType">Type of the file (optional, will be determined automatically if not provided)</param>
    /// <param name="compressedSize">Compressed size of the entry (optional)</param>
    /// <param name="uncompressedSize">Uncompressed size of the entry (optional)</param>
    /// <returns>Complete ArchiveEntryInfo with all properties set</returns>
    public static ArchiveEntryInfo CreateComplete(string archivePath, string entryName, bool isArchiveEntry,
        string? entryPath = null, ImageFileType? fileType = null, long compressedSize = 0, long uncompressedSize = 0)
    {
        var actualFileType = fileType ?? (isArchiveEntry ? ImageFileType.ArchiveEntry : ImageFileType.RegularFile);
        
        return new ArchiveEntryInfo
        {
            ArchivePath = archivePath,
            EntryName = entryName,
            EntryPath = entryPath ?? entryName,
            IsDirectory = !isArchiveEntry, // Invert the logic for backward compatibility
            FileType = actualFileType,
            CompressedSize = compressedSize,
            UncompressedSize = uncompressedSize
        };
    }

    /// <summary>
    /// Create an ArchiveEntryInfo from a display path with validation
    /// Validates that the separator doesn't conflict with path components
    /// </summary>
    /// <param name="displayPath">Display path with :: separator</param>
    /// <param name="isArchiveEntry">True if this is an archive entry, false if regular file</param>
    /// <returns>ArchiveEntryInfo or null if format is invalid or unsafe</returns>
    public static ArchiveEntryInfo? FromDisplayPathSafe(string displayPath, bool isArchiveEntry = true)
    {
        if (string.IsNullOrEmpty(displayPath))
            return null;

        var parts = displayPath.Split(new[] { DISPLAY_PATH_SEPARATOR }, 2, StringSplitOptions.None);
        if (parts.Length != 2)
            return null;

        var archivePath = parts[0];
        var entryName = parts[1];

        // Validate that the separator doesn't appear in the components
        if (!IsSafeToCombine(archivePath, entryName))
            return null;

        return isArchiveEntry 
            ? ForArchiveEntry(archivePath, entryName)
            : ForRegularFile(Path.Combine(archivePath, entryName), entryName);
    }

    #endregion
}
