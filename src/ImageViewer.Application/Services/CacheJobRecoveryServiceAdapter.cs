namespace ImageViewer.Application.Services;

/// <summary>
/// Adapter to provide backward compatibility for ICacheJobRecoveryService
/// Wraps IFileProcessingJobRecoveryService and filters for JobType="cache"
/// </summary>
public class CacheJobRecoveryServiceAdapter : ICacheJobRecoveryService
{
    private readonly IFileProcessingJobRecoveryService _fileProcessingRecoveryService;
    private const string CacheJobType = "cache";

    public CacheJobRecoveryServiceAdapter(IFileProcessingJobRecoveryService fileProcessingRecoveryService)
    {
        _fileProcessingRecoveryService = fileProcessingRecoveryService;
    }

    public async Task RecoverIncompleteJobsAsync()
    {
        // Recover only cache jobs
        await _fileProcessingRecoveryService.RecoverIncompleteJobsByTypeAsync(CacheJobType);
    }

    public Task<bool> ResumeJobAsync(string jobId)
    {
        return _fileProcessingRecoveryService.ResumeJobAsync(jobId);
    }

    public async Task<IEnumerable<string>> GetResumableJobIdsAsync()
    {
        return await _fileProcessingRecoveryService.GetResumableJobIdsByTypeAsync(CacheJobType);
    }

    public Task DisableJobResumptionAsync(string jobId, string reason)
    {
        return _fileProcessingRecoveryService.DisableJobResumptionAsync(jobId, reason);
    }

    public Task<int> CleanupOldCompletedJobsAsync(int olderThanDays = 30)
    {
        return _fileProcessingRecoveryService.CleanupOldCompletedJobsAsync(olderThanDays);
    }
}

