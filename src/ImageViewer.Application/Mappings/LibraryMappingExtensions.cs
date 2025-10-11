using ImageViewer.Application.DTOs;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Application.Mappings;

/// <summary>
/// Extension methods for mapping Library entities to DTOs
/// </summary>
public static class LibraryMappingExtensions
{
    /// <summary>
    /// Convert Library entity to DTO
    /// </summary>
    public static LibraryDto ToDto(this Library library)
    {
        return new LibraryDto
        {
            Id = library.Id.ToString(),
            Name = library.Name,
            Description = library.Description,
            Path = library.Path,
            OwnerId = library.OwnerId.ToString(),
            IsPublic = library.IsPublic,
            IsActive = library.IsActive,
            Settings = new LibrarySettingsDto
            {
                AutoScan = library.Settings.AutoScan,
                ScanInterval = library.Settings.ScanInterval,
                GenerateThumbnails = library.Settings.GenerateThumbnails,
                GenerateCache = library.Settings.GenerateCache,
                EnableWatching = library.Settings.EnableWatching,
                MaxFileSize = library.Settings.MaxFileSize,
                AllowedFormats = library.Settings.AllowedFormats.ToList(),
                ExcludedPaths = library.Settings.ExcludedPaths.ToList(),
                ThumbnailSettings = new ThumbnailSettingsDto
                {
                    Enabled = library.Settings.ThumbnailSettings.Enabled,
                    Width = library.Settings.ThumbnailSettings.Width,
                    Height = library.Settings.ThumbnailSettings.Height,
                    Quality = library.Settings.ThumbnailSettings.Quality,
                    Format = library.Settings.ThumbnailSettings.Format
                },
                CacheSettings = new CacheSettingsDto
                {
                    Enabled = library.Settings.CacheSettings.Enabled,
                    MaxSize = library.Settings.CacheSettings.MaxSize,
                    CompressionLevel = library.Settings.CacheSettings.CompressionLevel,
                    RetentionDays = library.Settings.CacheSettings.RetentionDays
                }
            },
            Metadata = new LibraryMetadataDto
            {
                Description = library.Metadata.Description,
                Tags = library.Metadata.Tags.ToList(),
                Categories = library.Metadata.Categories.ToList(),
                CustomFields = library.Metadata.CustomFields
            },
            Statistics = new LibraryStatisticsDto
            {
                TotalCollections = library.Statistics.TotalCollections,
                TotalMediaItems = library.Statistics.TotalMediaItems,
                TotalSize = library.Statistics.TotalSize,
                TotalViews = library.Statistics.TotalViews,
                TotalDownloads = library.Statistics.TotalDownloads,
                LastScannedAt = library.Statistics.LastScanDate
            },
            WatchInfo = new WatchInfoDto
            {
                IsWatching = library.WatchInfo.IsWatching,
                WatchPath = library.WatchInfo.WatchPath,
                WatchFilters = library.WatchInfo.WatchFilters.ToList(),
                LastWatchEvent = library.WatchInfo.LastWatchDate
            },
            CreatedAt = library.CreatedAt,
            UpdatedAt = library.UpdatedAt,
            IsDeleted = library.IsDeleted,
            CreatedBy = library.CreatedBy,
            UpdatedBy = library.UpdatedBy
        };
    }

    /// <summary>
    /// Convert list of Library entities to DTOs
    /// </summary>
    public static List<LibraryDto> ToDto(this IEnumerable<Library> libraries)
    {
        return libraries.Select(l => l.ToDto()).ToList();
    }
}

