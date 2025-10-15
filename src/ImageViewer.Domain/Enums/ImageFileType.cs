using MongoDB.Bson.Serialization.Attributes;

namespace ImageViewer.Domain.Enums;

/// <summary>
/// Types of image files in the system
/// 中文：系统中图片文件的类型
/// Tiếng Việt: Các loại tệp hình ảnh trong hệ thống
/// </summary>
public enum ImageFileType
{
    /// <summary>
    /// Regular file in a folder
    /// 中文：文件夹中的常规文件
    /// Tiếng Việt: Tệp thông thường trong thư mục
    /// </summary>
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    RegularFile = 0,

    /// <summary>
    /// Archive file itself (ZIP, 7Z, RAR, etc.)
    /// 中文：存档文件本身
    /// Tiếng Việt: Tệp lưu trữ chính
    /// </summary>
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    ArchiveFile = 1,

    /// <summary>
    /// File entry inside an archive
    /// 中文：存档文件内部的条目
    /// Tiếng Việt: Mục tin bên trong tệp lưu trữ
    /// </summary>
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    ArchiveEntry = 2
}
