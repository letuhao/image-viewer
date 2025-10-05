using MongoDB.Bson;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Domain.Interfaces;

/// <summary>
/// Repository interface for MediaProcessingJob entity
/// </summary>
public interface IMediaProcessingJobRepository : IRepository<MediaProcessingJob>
{
    Task<IEnumerable<MediaProcessingJob>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<MediaProcessingJob>> GetByJobTypeAsync(string jobType, CancellationToken cancellationToken = default);
    Task<IEnumerable<MediaProcessingJob>> GetByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<MediaProcessingJob>> GetByPriorityAsync(int priority, CancellationToken cancellationToken = default);
    Task<IEnumerable<MediaProcessingJob>> GetPendingJobsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<MediaProcessingJob>> GetFailedJobsAsync(CancellationToken cancellationToken = default);
}
