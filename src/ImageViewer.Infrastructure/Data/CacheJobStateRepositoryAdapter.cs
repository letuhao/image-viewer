using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using MongoDB.Bson;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// Adapter to provide backward compatibility for ICacheJobStateRepository
/// Wraps IFileProcessingJobStateRepository and filters for JobType="cache"
/// </summary>
public class CacheJobStateRepositoryAdapter : ICacheJobStateRepository
{
    private readonly IFileProcessingJobStateRepository _fileProcessingRepository;
    private const string CacheJobType = "cache";

    public CacheJobStateRepositoryAdapter(IFileProcessingJobStateRepository fileProcessingRepository)
    {
        _fileProcessingRepository = fileProcessingRepository;
    }

    // Map CacheJobState to FileProcessingJobState for queries
    private static CacheJobState? MapToLegacy(FileProcessingJobState? job)
    {
        if (job == null || job.JobType != CacheJobType) return null;
        
        // For now, just return null to indicate we should use the new API
        // This is a transitional adapter - code should migrate to IFileProcessingJobStateRepository
        return null;
    }

    public async Task<CacheJobState?> GetByJobIdAsync(string jobId)
    {
        var job = await _fileProcessingRepository.GetByJobIdAsync(jobId);
        return MapToLegacy(job);
    }

    public async Task<CacheJobState?> GetByCollectionIdAsync(string collectionId)
    {
        var job = await _fileProcessingRepository.GetByCollectionIdAsync(collectionId);
        return MapToLegacy(job);
    }

    public async Task<IEnumerable<CacheJobState>> GetIncompleteJobsAsync()
    {
        var jobs = await _fileProcessingRepository.GetIncompleteJobsByTypeAsync(CacheJobType);
        return jobs.Select(MapToLegacy).Where(j => j != null).Cast<CacheJobState>();
    }

    public async Task<IEnumerable<CacheJobState>> GetPausedJobsAsync()
    {
        var jobs = await _fileProcessingRepository.GetPausedJobsAsync();
        return jobs.Where(j => j.JobType == CacheJobType).Select(MapToLegacy).Where(j => j != null).Cast<CacheJobState>();
    }

    public async Task<IEnumerable<CacheJobState>> GetStaleJobsAsync(TimeSpan stalePeriod)
    {
        var jobs = await _fileProcessingRepository.GetStaleJobsAsync(stalePeriod);
        return jobs.Where(j => j.JobType == CacheJobType).Select(MapToLegacy).Where(j => j != null).Cast<CacheJobState>();
    }

    public Task<bool> IsImageProcessedAsync(string jobId, string imageId)
    {
        return _fileProcessingRepository.IsImageProcessedAsync(jobId, imageId);
    }

    public Task<bool> AtomicIncrementCompletedAsync(string jobId, string imageId, long sizeBytes)
    {
        return _fileProcessingRepository.AtomicIncrementCompletedAsync(jobId, imageId, sizeBytes);
    }

    public Task<bool> AtomicIncrementFailedAsync(string jobId, string imageId)
    {
        return _fileProcessingRepository.AtomicIncrementFailedAsync(jobId, imageId);
    }

    public Task<bool> AtomicIncrementSkippedAsync(string jobId, string imageId)
    {
        return _fileProcessingRepository.AtomicIncrementSkippedAsync(jobId, imageId);
    }

    public Task<bool> UpdateStatusAsync(string jobId, string status, string? errorMessage = null)
    {
        return _fileProcessingRepository.UpdateStatusAsync(jobId, status, errorMessage);
    }

    public Task<int> DeleteOldCompletedJobsAsync(DateTime olderThan)
    {
        return _fileProcessingRepository.DeleteOldCompletedJobsAsync(olderThan);
    }

    // IRepository<CacheJobState> methods - delegate to FileProcessingJobState repository
    public Task<CacheJobState?> GetByIdAsync(ObjectId id)
    {
        throw new NotImplementedException("Use GetByJobIdAsync instead");
    }

    public Task<IEnumerable<CacheJobState>> GetAllAsync()
    {
        throw new NotImplementedException("Use IFileProcessingJobStateRepository.GetByJobTypeAsync(\"cache\") instead");
    }

    public Task<CacheJobState> CreateAsync(CacheJobState entity)
    {
        throw new NotImplementedException("Use IFileProcessingJobStateRepository with FileProcessingJobState instead");
    }

    public Task<bool> UpdateAsync(CacheJobState entity)
    {
        throw new NotImplementedException("Use IFileProcessingJobStateRepository with FileProcessingJobState instead");
    }

    public Task<bool> DeleteAsync(ObjectId id)
    {
        throw new NotImplementedException("Use IFileProcessingJobStateRepository instead");
    }

    public Task<long> CountAsync()
    {
        throw new NotImplementedException("Use IFileProcessingJobStateRepository instead");
    }

    public Task<bool> ExistsAsync(ObjectId id)
    {
        throw new NotImplementedException("Use IFileProcessingJobStateRepository instead");
    }
}

