using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using MongoDB.Bson;

namespace ImageViewer.Test.Features.Cache.Unit;

/// <summary>
/// Unit tests for stale job detection and recovery
/// 停滞任务恢复单元测试 - Kiểm thử đơn vị khôi phục công việc bị treo
/// </summary>
public class StaleJobRecoveryTests
{
    private readonly Mock<IFileProcessingJobStateRepository> _mockJobStateRepository;
    private readonly Mock<ICollectionRepository> _mockCollectionRepository;
    private readonly Mock<IMessageQueueService> _mockMessageQueue;
    private readonly Mock<IImageProcessingSettingsService> _mockSettingsService;
    private readonly Mock<ICacheFolderRepository> _mockCacheFolderRepository;
    private readonly Mock<ILogger<FileProcessingJobRecoveryService>> _mockLogger;
    private readonly FileProcessingJobRecoveryService _service;

    public StaleJobRecoveryTests()
    {
        _mockJobStateRepository = new Mock<IFileProcessingJobStateRepository>();
        _mockCollectionRepository = new Mock<ICollectionRepository>();
        _mockMessageQueue = new Mock<IMessageQueueService>();
        _mockSettingsService = new Mock<IImageProcessingSettingsService>();
        _mockCacheFolderRepository = new Mock<ICacheFolderRepository>();
        _mockLogger = new Mock<ILogger<FileProcessingJobRecoveryService>>();

        _service = new FileProcessingJobRecoveryService(
            _mockJobStateRepository.Object,
            _mockCollectionRepository.Object,
            _mockMessageQueue.Object,
            _mockSettingsService.Object,
            _mockCacheFolderRepository.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task RecoverStaleJobs_WhenNoStaleJobs_ReturnsZero()
    {
        // Arrange
        var timeout = TimeSpan.FromMinutes(30);
        _mockJobStateRepository
            .Setup(x => x.GetStaleJobsAsync(timeout))
            .ReturnsAsync(new List<FileProcessingJobState>());

        // Act
        var result = await _service.RecoverStaleJobsAsync(timeout);

        // Assert
        Assert.Equal(0, result);
        _mockJobStateRepository.Verify(x => x.GetStaleJobsAsync(timeout), Times.Once);
    }

    [Fact]
    public async Task RecoverStaleJobs_WhenStuckLessThan3xTimeout_TriesResume()
    {
        // Arrange: Job stuck for 45 minutes (timeout = 30, 3x = 90)
        var timeout = TimeSpan.FromMinutes(30);
        var staleJob = CreateStaleJob("job-001", DateTime.UtcNow.AddMinutes(-45));
        
        _mockJobStateRepository
            .Setup(x => x.GetStaleJobsAsync(timeout))
            .ReturnsAsync(new List<FileProcessingJobState> { staleJob });
        
        _mockJobStateRepository
            .Setup(x => x.GetByJobIdAsync("job-001"))
            .ReturnsAsync(staleJob);
        
        var mockCollection = CreateMockCollection();
        _mockCollectionRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>()))
            .ReturnsAsync(mockCollection);

        // Act
        var result = await _service.RecoverStaleJobsAsync(timeout);

        // Assert
        Assert.Equal(1, result); // Should recover
        _mockJobStateRepository.Verify(x => x.UpdateStatusAsync("job-001", "Failed", It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RecoverStaleJobs_WhenStuckMoreThan3xTimeout_MarksFailed()
    {
        // Arrange: Job stuck for 120 minutes (timeout = 30, 3x = 90)
        var timeout = TimeSpan.FromMinutes(30);
        var staleJob = CreateStaleJob("job-002", DateTime.UtcNow.AddMinutes(-120));
        
        _mockJobStateRepository
            .Setup(x => x.GetStaleJobsAsync(timeout))
            .ReturnsAsync(new List<FileProcessingJobState> { staleJob });

        // Act
        var result = await _service.RecoverStaleJobsAsync(timeout);

        // Assert
        Assert.Equal(0, result); // Should NOT recover
        _mockJobStateRepository.Verify(
            x => x.UpdateStatusAsync("job-002", "Failed", It.Is<string>(s => s.Contains("stuck without progress"))),
            Times.Once
        );
    }

    [Fact]
    public async Task GetStaleJobCount_ReturnsCorrectCount()
    {
        // Arrange
        var timeout = TimeSpan.FromMinutes(30);
        var staleJobs = new List<FileProcessingJobState>
        {
            CreateStaleJob("job-001", DateTime.UtcNow.AddMinutes(-45)),
            CreateStaleJob("job-002", DateTime.UtcNow.AddMinutes(-60)),
            CreateStaleJob("job-003", DateTime.UtcNow.AddMinutes(-90))
        };
        
        _mockJobStateRepository
            .Setup(x => x.GetStaleJobsAsync(timeout))
            .ReturnsAsync(staleJobs);

        // Act
        var count = await _service.GetStaleJobCountAsync(timeout);

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task RecoverStaleJobs_WithMultipleJobs_HandlesEachCorrectly()
    {
        // Arrange
        var timeout = TimeSpan.FromMinutes(30);
        var jobs = new List<FileProcessingJobState>
        {
            CreateStaleJob("job-short", DateTime.UtcNow.AddMinutes(-40)),  // Will resume
            CreateStaleJob("job-medium", DateTime.UtcNow.AddMinutes(-60)), // Will resume
            CreateStaleJob("job-long", DateTime.UtcNow.AddMinutes(-120))   // Will fail
        };
        
        _mockJobStateRepository
            .Setup(x => x.GetStaleJobsAsync(timeout))
            .ReturnsAsync(jobs);

        foreach (var job in jobs.Where(j => j.LastProgressAt > DateTime.UtcNow.AddMinutes(-90)))
        {
            _mockJobStateRepository
                .Setup(x => x.GetByJobIdAsync(job.JobId))
                .ReturnsAsync(job);
            
            var mockCollection = CreateMockCollection();
            _mockCollectionRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<ObjectId>()))
                .ReturnsAsync(mockCollection);
        }

        // Act
        var result = await _service.RecoverStaleJobsAsync(timeout);

        // Assert
        Assert.Equal(2, result); // 2 resumed, 1 failed
        _mockJobStateRepository.Verify(
            x => x.UpdateStatusAsync("job-long", "Failed", It.IsAny<string>()),
            Times.Once,
            "Long stuck job should be marked as failed"
        );
    }

    private FileProcessingJobState CreateStaleJob(string jobId, DateTime lastProgress)
    {
        var job = new FileProcessingJobState(
            jobId: jobId,
            jobType: "cache",
            collectionId: ObjectId.GenerateNewId().ToString(),
            collectionName: "Test Collection",
            totalImages: 100,
            outputFolderId: null,
            outputFolderPath: "/test/cache",
            jobSettings: "{\"width\":1920,\"height\":1080,\"quality\":85}"
        );
        
        job.Start();
        
        // Use reflection to set LastProgressAt (private setter)
        var lastProgressProp = typeof(FileProcessingJobState).GetProperty("LastProgressAt");
        lastProgressProp?.SetValue(job, lastProgress);
        
        return job;
    }

    private Collection CreateMockCollection()
    {
        return new Collection(
            libraryId: ObjectId.GenerateNewId(),
            name: "Test Collection",
            path: "/test/path",
            type: Domain.Enums.CollectionType.Folder,
            description: "Test",
            createdBy: "test-user"
        );
    }
}

