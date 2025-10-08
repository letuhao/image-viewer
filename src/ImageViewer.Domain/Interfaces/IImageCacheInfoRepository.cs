using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for image cache information
/// OBSOLETE: Use embedded ImageCacheInfoEmbedded in Collection entity instead. This interface is kept only for backward compatibility.
/// </summary>
[Obsolete("Use embedded ImageCacheInfoEmbedded in Collection entity instead. Will be removed in future version.")]
public interface IImageCacheInfoRepository : IRepository<ImageCacheInfo>
{
}
