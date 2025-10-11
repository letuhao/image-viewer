using ImageViewer.Application.DTOs.Cache;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Application.Mappings;

/// <summary>
/// Mapping extensions for CacheJobState entity
/// </summary>
public static class CacheJobStateMappingExtensions
{
    public static CacheJobStateDto ToDto(this CacheJobState entity, bool includeDetailedTracking = false)
    {
        var dto = new CacheJobStateDto
        {
            Id = entity.Id.ToString(),
            JobId = entity.JobId,
            CollectionId = entity.CollectionId,
            CollectionName = entity.CollectionName,
            Status = entity.Status,
            TotalImages = entity.TotalImages,
            CompletedImages = entity.CompletedImages,
            FailedImages = entity.FailedImages,
            SkippedImages = entity.SkippedImages,
            RemainingImages = entity.GetRemainingImages(),
            Progress = entity.GetProgress(),
            CacheFolderId = entity.CacheFolderId,
            CacheFolderPath = entity.CacheFolderPath,
            TotalSizeBytes = entity.TotalSizeBytes,
            CacheWidth = entity.CacheWidth,
            CacheHeight = entity.CacheHeight,
            Quality = entity.Quality,
            Format = entity.Format,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            LastProgressAt = entity.LastProgressAt,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            ErrorMessage = entity.ErrorMessage,
            CanResume = entity.CanResume
        };

        // Optionally include detailed tracking (large data)
        if (includeDetailedTracking)
        {
            dto.ProcessedImageIds = entity.ProcessedImageIds;
            dto.FailedImageIds = entity.FailedImageIds;
        }

        return dto;
    }

    public static IEnumerable<CacheJobStateDto> ToDtoList(this IEnumerable<CacheJobState> entities, bool includeDetailedTracking = false)
    {
        return entities.Select(e => e.ToDto(includeDetailedTracking));
    }
}

