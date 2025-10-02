using ImageViewer.Domain.Enums;

namespace ImageViewer.Application.DTOs.Collections;

/// <summary>
/// Create collection request DTO
/// </summary>
public class CreateCollectionDto
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public CollectionType Type { get; set; } = CollectionType.Folder;
    public string? Description { get; set; }
}
