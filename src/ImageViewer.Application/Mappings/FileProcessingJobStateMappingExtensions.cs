using ImageViewer.Application.DTOs.Cache;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Application.Mappings;

/// <summary>
/// Mapping extensions for FileProcessingJobState entity
/// </summary>
public static class FileProcessingJobStateMappingExtensions
{
    public static FileProcessingJobStateDto ToDto(this FileProcessingJobState entity, bool includeDetailedTracking = false)
    {
        var dto = new FileProcessingJobStateDto
        {
            Id = entity.Id.ToString(),
            JobId = entity.JobId,
            JobType = entity.JobType,
            CollectionId = entity.CollectionId,
            CollectionName = entity.CollectionName,
            Status = entity.Status,
            TotalImages = entity.TotalImages,
            CompletedImages = entity.CompletedImages,
            FailedImages = entity.FailedImages,
            SkippedImages = entity.SkippedImages,
            RemainingImages = entity.GetRemainingImages(),
            Progress = entity.GetProgress(),
            OutputFolderId = entity.OutputFolderId,
            OutputFolderPath = entity.OutputFolderPath,
            TotalSizeBytes = entity.TotalSizeBytes,
            JobSettings = entity.JobSettings,
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

    public static IEnumerable<FileProcessingJobStateDto> ToDtoList(this IEnumerable<FileProcessingJobState> entities, bool includeDetailedTracking = false)
    {
        return entities.Select(e => e.ToDto(includeDetailedTracking));
    }
}

