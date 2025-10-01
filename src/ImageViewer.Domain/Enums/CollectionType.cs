namespace ImageViewer.Domain.Enums;

/// <summary>
/// Collection type enumeration
/// </summary>
public enum CollectionType
{
    /// <summary>
    /// Regular folder containing images
    /// </summary>
    Folder = 0,

    /// <summary>
    /// ZIP archive containing images
    /// </summary>
    Zip = 1,

    /// <summary>
    /// 7-Zip archive containing images
    /// </summary>
    SevenZip = 2,

    /// <summary>
    /// RAR archive containing images
    /// </summary>
    Rar = 3,

    /// <summary>
    /// TAR archive containing images
    /// </summary>
    Tar = 4
}
