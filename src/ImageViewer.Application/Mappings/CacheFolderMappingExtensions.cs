using ImageViewer.Application.DTOs.Cache;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Application.Mappings;

/// <summary>
/// Mapping extensions for CacheFolder entity with enhanced statistics
/// </summary>
public static class CacheFolderMappingExtensions
{
    public static CacheFolderStatisticsDto ToStatisticsDto(this CacheFolder entity)
    {
        return new CacheFolderStatisticsDto
        {
            Id = entity.Id.ToString(),
            Name = entity.Name,
            Path = entity.Path,
            MaxSizeBytes = entity.MaxSizeBytes,
            CurrentSizeBytes = entity.CurrentSizeBytes,
            Priority = entity.Priority,
            IsActive = entity.IsActive,
            TotalCollections = entity.TotalCollections,
            TotalFiles = entity.TotalFiles,
            CachedCollectionIds = entity.CachedCollectionIds,
            LastCacheGeneratedAt = entity.LastCacheGeneratedAt,
            LastCleanupAt = entity.LastCleanupAt,
            AvailableSpaceBytes = entity.GetAvailableSpace(),
            UsagePercentage = entity.GetUsagePercentage(),
            IsFull = entity.IsFull(),
            IsNearFull = entity.IsNearFull(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static IEnumerable<CacheFolderStatisticsDto> ToStatisticsDtoList(this IEnumerable<CacheFolder> entities)
    {
        return entities.Select(e => e.ToStatisticsDto());
    }
}

