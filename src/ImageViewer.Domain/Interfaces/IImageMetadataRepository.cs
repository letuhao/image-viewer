using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for image metadata
/// </summary>
public interface IImageMetadataRepository : IRepository<ImageMetadataEntity>
{
}
