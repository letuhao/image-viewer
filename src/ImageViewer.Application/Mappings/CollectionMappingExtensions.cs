using ImageViewer.Application.DTOs.Collections;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.ValueObjects;

namespace ImageViewer.Application.Mappings;

public static class CollectionMappingExtensions
{
    /// <summary>
    /// Convert Collection entity to lightweight overview DTO (for lists)
    /// Includes thumbnail info for collection card display
    /// NOTE: ThumbnailBase64 must be set separately using IThumbnailCacheService
    /// </summary>
    public static CollectionOverviewDto ToOverviewDto(this Collection collection)
    {
        // Get middle thumbnail (if thumbnails exist)
        var middleThumbnail = GetMiddleThumbnail(collection.Thumbnails);
        
        return new CollectionOverviewDto
        {
            Id = collection.Id.ToString(),
            Name = collection.Name,
            Path = collection.Path,
            Type = collection.Type.ToString().ToLower(),
            IsNested = false, // TODO: Add nested collection support
            Depth = 0, // TODO: Add depth tracking
            ImageCount = collection.Images?.Count ?? 0,
            ThumbnailCount = collection.Thumbnails?.Count ?? 0,
            CacheImageCount = collection.CacheImages?.Count ?? 0,
            TotalSize = collection.Images?.Sum(i => i.FileSize) ?? 0,
            
            // Thumbnail info for collection card display
            ThumbnailPath = middleThumbnail?.ThumbnailPath,
            ThumbnailImageId = middleThumbnail?.Id, // Use Thumbnail ID, not Image ID
            HasThumbnail = middleThumbnail != null,
            ThumbnailBase64 = null, // Will be populated by controller using IThumbnailCacheService
            
            CreatedAt = collection.CreatedAt,
            UpdatedAt = collection.UpdatedAt,
        };
    }
    
    /// <summary>
    /// Get the middle thumbnail for this collection
    /// </summary>
    public static ThumbnailEmbedded? GetCollectionThumbnail(this Collection collection)
    {
        return GetMiddleThumbnail(collection.Thumbnails);
    }

    /// <summary>
    /// Get the middle thumbnail from the collection (for collection card preview)
    /// Returns null if no thumbnails exist or if no valid thumbnails found
    /// </summary>
    private static Domain.ValueObjects.ThumbnailEmbedded? GetMiddleThumbnail(List<Domain.ValueObjects.ThumbnailEmbedded>? thumbnails)
    {
        if (thumbnails == null || thumbnails.Count == 0)
            return null;

        // Filter for valid, generated thumbnails
        var validThumbnails = thumbnails
            .Where(t => t.IsGenerated && t.IsValid && !string.IsNullOrEmpty(t.ThumbnailPath))
            .ToList();

        if (validThumbnails.Count == 0)
            return null;

        // Return the middle thumbnail (or first if only one)
        var middleIndex = validThumbnails.Count / 2;
        return validThumbnails[middleIndex];
    }

    /// <summary>
    /// Convert Collection entity to full detail DTO (for single collection view)
    /// </summary>
    public static CollectionDetailDto ToDetailDto(this Collection collection)
    {
        return new CollectionDetailDto
        {
            Id = collection.Id.ToString(),
            LibraryId = collection.LibraryId.ToString(),
            Name = collection.Name,
            Description = collection.Description,
            Path = collection.Path,
            Type = collection.Type.ToString().ToLower(),
            IsActive = collection.IsActive,
            IsNested = false, // TODO: Add nested collection support
            Depth = 0, // TODO: Add depth tracking
            Settings = new CollectionSettingsDto
            {
                Enabled = collection.Settings.Enabled,
                AutoScan = collection.Settings.AutoScan,
                GenerateThumbnails = collection.Settings.GenerateThumbnails,
                GenerateCache = collection.Settings.GenerateCache,
                EnableWatching = collection.Settings.EnableWatching,
                ScanInterval = collection.Settings.ScanInterval,
                MaxFileSize = collection.Settings.MaxFileSize,
                AllowedFormats = collection.Settings.AllowedFormats.ToList(),
                ExcludedPaths = collection.Settings.ExcludedPaths.ToList(),
                AutoGenerateCache = collection.Settings.AutoGenerateCache,
            },
            Metadata = new CollectionMetadataDto
            {
                Description = collection.Metadata.Description,
                Tags = collection.Metadata.Tags.ToList(),
                Categories = collection.Metadata.Categories.ToList(),
                CustomFields = collection.Metadata.CustomFields.ToDictionary(k => k.Key, k => k.Value?.ToString() ?? string.Empty),
                Version = collection.Metadata.Version,
                LastModified = collection.Metadata.LastModified,
                CreatedBy = collection.Metadata.CreatedBy,
                ModifiedBy = collection.Metadata.ModifiedBy,
            },
            Statistics = new CollectionStatisticsDto
            {
                TotalItems = collection.Statistics.TotalItems,
                TotalSize = collection.Statistics.TotalSize,
                TotalViews = collection.Statistics.TotalViews,
                TotalDownloads = collection.Statistics.TotalDownloads,
                TotalShares = collection.Statistics.TotalShares,
                TotalLikes = collection.Statistics.TotalLikes,
                TotalComments = collection.Statistics.TotalComments,
                LastScanDate = collection.Statistics.LastScanDate,
                ScanCount = collection.Statistics.ScanCount,
                LastActivity = collection.Statistics.LastActivity,
                TotalCollections = collection.Statistics.TotalCollections,
                ActiveCollections = collection.Statistics.ActiveCollections,
                TotalImages = collection.Statistics.TotalImages,
                AverageImagesPerCollection = collection.Statistics.AverageImagesPerCollection,
                AverageSizePerCollection = collection.Statistics.AverageSizePerCollection,
                LastViewed = collection.Statistics.LastViewed,
            },
            WatchInfo = new WatchInfoDto
            {
                IsWatching = collection.WatchInfo.IsWatching,
                WatchPath = collection.WatchInfo.WatchPath,
                WatchFilters = collection.WatchInfo.WatchFilters.ToList(),
                LastWatchDate = collection.WatchInfo.LastWatchDate,
                WatchCount = collection.WatchInfo.WatchCount,
                LastChangeDetected = collection.WatchInfo.LastChangeDetected,
                ChangeCount = collection.WatchInfo.ChangeCount,
            },
            SearchIndex = new SearchIndexDto
            {
                SearchableText = collection.SearchIndex.SearchableText,
                Tags = collection.SearchIndex.Tags.ToList(),
                Categories = collection.SearchIndex.Categories.ToList(),
                Keywords = collection.SearchIndex.Keywords.ToList(),
                LastIndexed = collection.SearchIndex.LastIndexed,
                IndexVersion = collection.SearchIndex.IndexVersion,
            },
            Images = collection.Images.ToList(),
            Thumbnails = collection.Thumbnails.ToList(),
            CacheImages = collection.CacheImages.ToList(),
            CacheBindings = collection.CacheBindings.ToList(),
            CreatedAt = collection.CreatedAt,
            UpdatedAt = collection.UpdatedAt,
        };
    }
}

