using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Application.Services;

/// <summary>
/// Service for selecting cache folders for image storage with smart distribution
/// 中文：缓存文件夹选择服务
/// Tiếng Việt: Dịch vụ chọn thư mục bộ nhớ cache
/// </summary>
public interface ICacheFolderSelectionService
{
    /// <summary>
    /// Select the best cache folder for storing a cache image
    /// Uses hash-based distribution for equal load balancing across folders
    /// </summary>
    /// <param name="collectionId">Collection ID for hash-based distribution</param>
    /// <param name="imageId">Image ID</param>
    /// <param name="cacheWidth">Cache image width</param>
    /// <param name="cacheHeight">Cache image height</param>
    /// <param name="format">Image format (jpeg, webp, png)</param>
    /// <returns>Full cache path where the image should be stored</returns>
    Task<string?> SelectCacheFolderForCacheAsync(ObjectId collectionId, string imageId, int cacheWidth, int cacheHeight, string format);

    /// <summary>
    /// Select the best cache folder for storing a thumbnail
    /// Uses hash-based distribution for equal load balancing across folders
    /// </summary>
    /// <param name="collectionId">Collection ID for hash-based distribution</param>
    /// <param name="imageId">Image ID or filename</param>
    /// <param name="width">Thumbnail width</param>
    /// <param name="height">Thumbnail height</param>
    /// <param name="format">Image format (jpeg, webp, png)</param>
    /// <returns>Full thumbnail path where the image should be stored</returns>
    Task<string?> SelectCacheFolderForThumbnailAsync(ObjectId collectionId, string imageId, int width, int height, string format);
}

