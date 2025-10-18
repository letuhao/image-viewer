using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ImageViewer.Domain.Enums;

#pragma warning disable CS8618 // MongoDB entities/value objects are initialized by the driver

namespace ImageViewer.Domain.ValueObjects;

/// <summary>
/// Embedded image value object for MongoDB collections
/// </summary>
public class ImageEmbedded
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; private set; } = ObjectId.GenerateNewId().ToString();
    
    [BsonElement("filename")]
    public string Filename { get; private set; }
    
    [BsonElement("relativePath")]
    public string RelativePath { get; private set; }
    
    [BsonElement("legacyRelativePath")]
    public string LegacyRelativePath { get; private set; }
    
    [BsonElement("archiveEntry")]
    public ArchiveEntryInfo? ArchiveEntry { get; private set; }
    
    [BsonElement("fileType")]
    public ImageFileType FileType { get; private set; } = ImageFileType.RegularFile;
    
    [BsonElement("fileSize")]
    public long FileSize { get; private set; }
    
    [BsonElement("width")]
    public int Width { get; private set; }
    
    [BsonElement("height")]
    public int Height { get; private set; }
    
    [BsonElement("format")]
    public string Format { get; private set; }
    
    [BsonElement("viewCount")]
    public int ViewCount { get; private set; }
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; private set; }
    
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; private set; }
    
    [BsonElement("isDeleted")]
    public bool IsDeleted { get; private set; }
    
    [BsonElement("deletedAt")]
    public DateTime? DeletedAt { get; private set; }
    
    [BsonElement("cacheInfo")]
    public ImageCacheInfoEmbedded? CacheInfo { get; private set; }
    
    [BsonElement("metadata")]
    public ImageMetadataEmbedded? Metadata { get; private set; }

    // Private constructor for MongoDB
    private ImageEmbedded() { }

    public ImageEmbedded(string filename, string relativePath, long fileSize, 
        int width, int height, string format)
    {
        Filename = filename ?? throw new ArgumentNullException(nameof(filename));
        RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
        LegacyRelativePath = relativePath; // Keep for backward compatibility
        ArchiveEntry = null; // Regular file
        FileType = ImageFileType.RegularFile;
        FileSize = fileSize;
        Width = width;
        Height = height;
        Format = format ?? throw new ArgumentNullException(nameof(format));
        ViewCount = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    /// <summary>
    /// Constructor for new data and migration - handles both null and non-null ArchiveEntryInfo
    /// </summary>
    public ImageEmbedded(string filename, string relativePath, ArchiveEntryInfo? archiveEntry, 
        long fileSize, int width, int height, string format)
    {
        Filename = filename ?? throw new ArgumentNullException(nameof(filename));
        RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
        LegacyRelativePath = relativePath; // Keep for backward compatibility
        
        // Handle legacy data where ArchiveEntry might be null
        if (archiveEntry != null)
        {
            ArchiveEntry = archiveEntry;
            FileType = archiveEntry.FileType;
        }
        else
        {
            // Create a default ArchiveEntryInfo for legacy data
            // This handles old data that doesn't have ArchiveEntryInfo
            ArchiveEntry = new ArchiveEntryInfo
            {
                ArchivePath = "", // Will be empty for legacy data
                EntryName = filename,
                EntryPath = filename,
                FileType = ImageFileType.RegularFile // Default to regular file
            };
            FileType = ImageFileType.RegularFile;
        }
        
        FileSize = fileSize;
        Width = width;
        Height = height;
        Format = format ?? throw new ArgumentNullException(nameof(format));
        ViewCount = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    public ImageEmbedded(string filename, string relativePath, ImageFileType fileType,
        long fileSize, int width, int height, string format)
    {
        Filename = filename ?? throw new ArgumentNullException(nameof(filename));
        RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
        LegacyRelativePath = relativePath; // Keep for backward compatibility
        ArchiveEntry = null; // Set based on file type
        FileType = fileType;
        FileSize = fileSize;
        Width = width;
        Height = height;
        Format = format ?? throw new ArgumentNullException(nameof(format));
        ViewCount = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    public void UpdateMetadata(int width, int height, long fileSize)
    {
        Width = width;
        Height = height;
        FileSize = fileSize;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementViewCount()
    {
        ViewCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCacheInfo(ImageCacheInfoEmbedded? cacheInfo)
    {
        CacheInfo = cacheInfo;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearCacheInfo()
    {
        CacheInfo = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetMetadata(ImageMetadataEmbedded metadata)
    {
        Metadata = metadata;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsDeleted()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    ///// <summary>
    ///// Get full path for the image (resolves relative paths and handles ZIP entries)
    ///// 获取图片的完整路径 - Lấy đường dẫn đầy đủ
    ///// </summary>
    //public string GetFullPath(string collectionPath)
    //{
    //    if (string.IsNullOrEmpty(collectionPath))
    //    {
    //        return GetDisplayPath();
    //    }

    //    // Handle archive entries using the new ArchiveEntry property
    //    if (ArchiveEntry != null)
    //    {
    //        var archivePath = ArchiveEntry.ArchivePath;
            
    //        // If archive path is not rooted, combine with collection path
    //        if (!Path.IsPathRooted(archivePath))
    //        {
    //            archivePath = Path.Combine(collectionPath, archivePath);
    //        }

    //        return $"{archivePath}::{ArchiveEntry.EntryName}";
    //    }

    //    // Handle legacy ZIP entries in LegacyRelativePath (backward compatibility)
    //    if (LegacyRelativePath.Contains("::") || LegacyRelativePath.Contains("#"))
    //    {
    //        var archiveEntry = ArchiveEntryInfo.FromPath(LegacyRelativePath);
    //        if (archiveEntry != null)
    //        {
    //            var archivePath = archiveEntry.ArchivePath;
                
    //            // If archive path is not rooted, combine with collection path
    //            if (!Path.IsPathRooted(archivePath))
    //            {
    //                archivePath = Path.Combine(collectionPath, archivePath);
    //            }

    //            return $"{archivePath}::{archiveEntry.EntryName}";
    //        }
    //    }

    //    // Handle regular files
    //    if (!Path.IsPathRooted(RelativePath))
    //    {
    //        return Path.Combine(collectionPath, RelativePath);
    //    }

    //    return RelativePath;
    //}

    ///// <summary>
    ///// Get display path for the image (for logging and display purposes)
    ///// </summary>
    //public string GetDisplayPath()
    //{
    //    if (ArchiveEntry != null)
    //    {
    //        return ArchiveEntry.GetFullPath();
    //    }

    //    if (LegacyRelativePath.Contains("::") || LegacyRelativePath.Contains("#"))
    //    {
    //        return LegacyRelativePath;
    //    }

    //    return RelativePath;
    //}

    /// <summary>
    /// Check if this image is inside an archive
    /// </summary>
    public bool IsArchiveEntry() => FileType == ImageFileType.ArchiveEntry && ArchiveEntry != null;

    /// <summary>
    /// Check if this image is an archive file itself
    /// </summary>
    public bool IsArchiveFile() => FileType == ImageFileType.ArchiveFile;

    /// <summary>
    /// Check if this image is a regular file
    /// </summary>
    public bool IsRegularFile() => FileType == ImageFileType.RegularFile;

    /// <summary>
    /// Get the archive path if this is an archive entry, otherwise return null
    /// </summary>
    public string? GetArchivePath() => ArchiveEntry?.ArchivePath;

    /// <summary>
    /// Get the entry name if this is an archive entry, otherwise return null
    /// </summary>
    public string? GetEntryName() => ArchiveEntry?.EntryName;

    /// <summary>
    /// Get the display name based on file type
    /// </summary>
    public string GetDisplayName()
    {
        return FileType switch
        {
            ImageFileType.ArchiveEntry => ArchiveEntry?.GetDisplayName() ?? Filename,
            ImageFileType.ArchiveFile => $"{Filename} (Archive)",
            ImageFileType.RegularFile => Filename,
            _ => Filename
        };
    }
}