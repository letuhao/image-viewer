namespace ImageViewer.Application.Services;

/// <summary>
/// Compressed file service interface
/// </summary>
public interface ICompressedFileService
{
    /// <summary>
    /// Check if file is a supported compressed format
    /// </summary>
    Task<bool> IsCompressedFileAsync(string filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get supported compressed file extensions
    /// </summary>
    string[] GetSupportedExtensions();
    
    /// <summary>
    /// Extract images from compressed file
    /// </summary>
    Task<IEnumerable<CompressedFileImage>> ExtractImagesAsync(string filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get compressed file information
    /// </summary>
    Task<CompressedFileInfo?> GetFileInfoAsync(string filePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if compressed file contains images
    /// </summary>
    Task<bool> ContainsImagesAsync(string filePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Compressed file image information
/// </summary>
public class CompressedFileImage
{
    public string Filename { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime LastModified { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public string Format { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
}

/// <summary>
/// Compressed file information
/// </summary>
public class CompressedFileInfo
{
    public string Filename { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime LastModified { get; set; }
    public string Format { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public int ImageFiles { get; set; }
    public long TotalImageSize { get; set; }
    public string[] SupportedFormats { get; set; } = Array.Empty<string>();
}
