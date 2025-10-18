namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Helper class for migrating existing ImageEmbedded objects to use the new ArchiveEntry structure
/// 中文：迁移现有ImageEmbedded对象到新ArchiveEntry结构的辅助类
/// Tiếng Việt: Lớp trợ giúp để di chuyển các đối tượng ImageEmbedded hiện có sang cấu trúc ArchiveEntry mới
/// </summary>
public static class ImageEmbeddedMigrationHelper
{
    /// <summary>
    /// Migrate an existing ImageEmbedded object to use the new ArchiveEntry structure
    /// This method should be called when loading existing data from MongoDB
    /// </summary>
    //public static void MigrateToArchiveEntry(ImageEmbedded image)
    //{
    //    if (image.ArchiveEntry != null)
    //    {
    //        // Already migrated
    //        return;
    //    }

    //    // Check if LegacyRelativePath contains archive entry information
    //    if (image.LegacyRelativePath.Contains("::") || image.LegacyRelativePath.Contains("#"))
    //    {
    //        var archiveEntry = ArchiveEntryInfo.FromPath(image.LegacyRelativePath);
    //        if (archiveEntry != null)
    //        {
    //            // Use reflection to set the ArchiveEntry property (since it's private set)
    //            var archiveEntryProperty = typeof(ImageEmbedded).GetProperty(nameof(ImageEmbedded.ArchiveEntry));
    //            archiveEntryProperty?.SetValue(image, archiveEntry);
    //        }
    //    }
    //}

    ///// <summary>
    ///// Create a new ImageEmbedded for a regular file
    ///// </summary>
    //public static ImageEmbedded CreateRegularFile(string filename, string relativePath, 
    //    long fileSize, int width, int height, string format)
    //{
    //    return new ImageEmbedded(filename, relativePath, fileSize, width, height, format);
    //}

    ///// <summary>
    ///// Create a new ImageEmbedded for an archive file itself
    ///// </summary>
    //public static ImageEmbedded CreateArchiveFile(string filename, string relativePath, 
    //    long fileSize, int width, int height, string format)
    //{
    //    return new ImageEmbedded(filename, relativePath, ImageFileType.ArchiveFile, fileSize, width, height, format);
    //}

    ///// <summary>
    ///// Create a new ImageEmbedded for an archive entry
    ///// </summary>
    //public static ImageEmbedded CreateArchiveEntry(string filename, string relativePath, 
    //    string archivePath, string entryName, long fileSize, int width, int height, string format)
    //{
    //    var archiveEntry = new ArchiveEntryInfo
    //    {
    //        ArchivePath = archivePath,
    //        EntryName = entryName,
    //        EntryPath = entryName,
    //        FileType = ImageFileType.ArchiveEntry
    //    };

    //    return new ImageEmbedded(filename, relativePath, archiveEntry, fileSize, width, height, format);
    //}

    ///// <summary>
    ///// Create a new ImageEmbedded for an archive entry using ArchiveEntryInfo
    ///// </summary>
    //public static ImageEmbedded CreateArchiveEntry(string filename, string relativePath, 
    //    ArchiveEntryInfo archiveEntry, long fileSize, int width, int height, string format)
    //{
    //    return new ImageEmbedded(filename, relativePath, archiveEntry, fileSize, width, height, format);
    //}

    ///// <summary>
    ///// Get the display path for an ImageEmbedded (handles both regular files and archive entries)
    ///// </summary>
    //public static string GetDisplayPath(ImageEmbedded image)
    //{
    //    if (image.IsArchiveEntry())
    //    {
    //        return $"{image.GetArchivePath()}::{image.GetEntryName()}";
    //    }
        
    //    return image.GetDisplayPath();
    //}

    ///// <summary>
    ///// Check if an ImageEmbedded represents an archive entry
    ///// </summary>
    //public static bool IsArchiveEntry(ImageEmbedded image)
    //{
    //    return image.IsArchiveEntry() || 
    //           image.LegacyRelativePath.Contains("::") || 
    //           image.LegacyRelativePath.Contains("#");
    //}

    ///// <summary>
    ///// Detect the file type based on the relative path and filename
    ///// </summary>
    //public static ImageFileType DetectFileType(string relativePath, string filename)
    //{
    //    // Check if it's an archive entry (contains separator)
    //    if (relativePath.Contains("::") || relativePath.Contains("#"))
    //    {
    //        return ImageFileType.ArchiveEntry;
    //    }

    //    // Check if it's an archive file (common archive extensions)
    //    var extension = Path.GetExtension(filename).ToLowerInvariant();
    //    var archiveExtensions = new[] { ".zip", ".7z", ".rar", ".tar", ".gz", ".bz2", ".xz", ".cbz", ".cbr" };
        
    //    if (archiveExtensions.Contains(extension))
    //    {
    //        return ImageFileType.ArchiveFile;
    //    }

    //    // Default to regular file
    //    return ImageFileType.RegularFile;
    //}

    ///// <summary>
    ///// Migrate an existing ImageEmbedded and set the correct file type
    ///// </summary>
    //public static void MigrateFileType(ImageEmbedded image)
    //{
    //    if (image.FileType != ImageFileType.RegularFile)
    //    {
    //        // Already has a file type set
    //        return;
    //    }

    //    var detectedType = DetectFileType(image.LegacyRelativePath, image.Filename);
        
    //    // Use reflection to set the FileType property
    //    var fileTypeProperty = typeof(ImageEmbedded).GetProperty(nameof(ImageEmbedded.FileType));
    //    fileTypeProperty?.SetValue(image, detectedType);

    //    // If it's an archive entry, also migrate the ArchiveEntry property
    //    if (detectedType == ImageFileType.ArchiveEntry)
    //    {
    //        MigrateToArchiveEntry(image);
    //    }
    //}
}
