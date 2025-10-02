namespace ImageViewer.Application.DTOs.Collections;

/// <summary>
/// Update collection request DTO
/// </summary>
public class UpdateCollectionDto
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? Description { get; set; }
}
