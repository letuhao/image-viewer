namespace ImageViewer.Application.DTOs.Files;

/// <summary>
/// Directory information DTO
/// </summary>
public class DirectoryInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string ParentPath { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public int FileCount { get; set; }
    public int SubdirectoryCount { get; set; }
    public long TotalSize { get; set; }
}
