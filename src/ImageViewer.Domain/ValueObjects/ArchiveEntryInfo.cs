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
    [BsonElement("archivePath")]
    public string ArchivePath { get; set; } = string.Empty;

    [BsonElement("entryName")]
    public string EntryName { get; set; } = string.Empty;

    [BsonElement("entryPath")]
    public string EntryPath { get; set; } = string.Empty;

    [BsonElement("isDirectory")]
    public bool IsDirectory { get; set; }

    [BsonElement("compressedSize")]
    public long CompressedSize { get; set; }

    [BsonElement("uncompressedSize")]
    public long UncompressedSize { get; set; }

    [BsonElement("fileType")]
    public ImageFileType FileType { get; set; } = ImageFileType.ArchiveEntry;

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

    ///// <summary>
    ///// Create an ArchiveEntryInfo from a full path string
    ///// Supports both old format (archive.zip#entry.jpg) and new format (archive.zip::entry.jpg)
    ///// </summary>
    //public static ArchiveEntryInfo? FromPath(string fullPath)
    //{
    //    if (string.IsNullOrEmpty(fullPath))
    //        return null;

    //    // Try new format first (::)
    //    var parts = fullPath.Split(new[] { "::" }, 2, StringSplitOptions.None);
    //    if (parts.Length == 2)
    //    {
    //        return new ArchiveEntryInfo
    //        {
    //            ArchivePath = parts[0],
    //            EntryName = parts[1],
    //            EntryPath = parts[1]
    //        };
    //    }

    //    // Fallback to old format (#) for backward compatibility
    //    parts = fullPath.Split('#', 2);
    //    if (parts.Length == 2)
    //    {
    //        return new ArchiveEntryInfo
    //        {
    //            ArchivePath = parts[0],
    //            EntryName = parts[1],
    //            EntryPath = parts[1]
    //        };
    //    }

    //    return null;
    //}

    ///// <summary>
    ///// Convert to the old string format for backward compatibility
    ///// </summary>
    //public string ToLegacyString() => $"{ArchivePath}#{EntryName}";
}
