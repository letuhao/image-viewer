using FluentAssertions;
using Moq;
using Xunit;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.BackgroundJobs;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Enums;
using MongoDB.Bson;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Test.Features.SystemManagement.Unit;

/// <summary>
/// Unit tests for BackgroundJobService - Background Job Management features
/// </summary>
public class BackgroundJobServiceTests
{
    private readonly Mock<IBackgroundJobRepository> _mockBackgroundJobRepository;
    private readonly Mock<ICollectionRepository> _mockCollectionRepository;
    private readonly Mock<IBulkService> _mockBulkService;
    private readonly Mock<IMessageQueueService> _mockMessageQueueService;
    private readonly Mock<ILogger<BackgroundJobService>> _mockLogger;
    private readonly BackgroundJobService _backgroundJobService;

    public BackgroundJobServiceTests()
    {
        _mockBackgroundJobRepository = new Mock<IBackgroundJobRepository>();
        _mockCollectionRepository = new Mock<ICollectionRepository>();
        _mockBulkService = new Mock<IBulkService>();
        _mockMessageQueueService = new Mock<IMessageQueueService>();
        _mockLogger = new Mock<ILogger<BackgroundJobService>>();
        _backgroundJobService = new BackgroundJobService(
            _mockBackgroundJobRepository.Object,
            _mockCollectionRepository.Object,
            _mockBulkService.Object,
            _mockMessageQueueService.Object,
            _mockLogger.Object);
    }

    #region GetJobAsync Tests

    [Fact]
    public async Task GetJobAsync_WithValidJobId_ShouldReturnJob()
    {
        // Arrange
        var jobId = ObjectId.GenerateNewId();
        var job = new BackgroundJob("test-job", "Test job description", new Dictionary<string, object>());

        _mockBackgroundJobRepository.Setup(x => x.GetByIdAsync(jobId))
            .ReturnsAsync(job);

        // Act
        var result = await _backgroundJobService.GetJobAsync(jobId);

        // Assert
        result.Should().NotBeNull();
        result.JobId.Should().Be(job.Id); // Use the job's actual ID, not the input parameter
        result.Type.Should().Be("test-job");
        result.Status.Should().Be("Pending");
        result.Progress.Should().NotBeNull();
        result.Progress.Total.Should().Be(0);
        result.Progress.Completed.Should().Be(0);
        result.Progress.Percentage.Should().Be(0);
        _mockBackgroundJobRepository.Verify(x => x.GetByIdAsync(jobId), Times.Once);
    }

    [Fact]
    public async Task GetJobAsync_WithNonExistentJobId_ShouldThrowArgumentException()
    {
        // Arrange
        var jobId = ObjectId.GenerateNewId();

        _mockBackgroundJobRepository.Setup(x => x.GetByIdAsync(jobId))
            .ReturnsAsync((BackgroundJob)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _backgroundJobService.GetJobAsync(jobId));
        exception.Message.Should().Contain($"Job with ID {jobId} not found");
    }

    #endregion

    #region GetJobsAsync Tests

    [Fact]
    public async Task GetJobsAsync_WithNoFilters_ShouldReturnAllJobs()
    {
        // Arrange
        var jobs = new List<BackgroundJob>
        {
            new BackgroundJob("job1", "Job 1", new Dictionary<string, object>()),
            new BackgroundJob("job2", "Job 2", new Dictionary<string, object>())
        };

        _mockBackgroundJobRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(jobs);

        // Act
        var result = await _backgroundJobService.GetJobsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Type.Should().Be("job1");
        result.Last().Type.Should().Be("job2");
        _mockBackgroundJobRepository.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetJobsAsync_WithStatusFilter_ShouldReturnFilteredJobs()
    {
        // Arrange
        var jobs = new List<BackgroundJob>
        {
            new BackgroundJob("job1", "Job 1", new Dictionary<string, object>()),
            new BackgroundJob("job2", "Job 2", new Dictionary<string, object>())
        };
        jobs[0].Start(); // Set status to Running

        _mockBackgroundJobRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(jobs);

        // Act
        var result = await _backgroundJobService.GetJobsAsync(status: "Running");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Status.Should().Be("Running");
        result.First().Type.Should().Be("job1");
    }

    [Fact]
    public async Task GetJobsAsync_WithTypeFilter_ShouldReturnFilteredJobs()
    {
        // Arrange
        var jobs = new List<BackgroundJob>
        {
            new BackgroundJob("cache-generation", "Cache job", new Dictionary<string, object>()),
            new BackgroundJob("thumbnail-generation", "Thumbnail job", new Dictionary<string, object>())
        };

        _mockBackgroundJobRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(jobs);

        // Act
        var result = await _backgroundJobService.GetJobsAsync(type: "cache-generation");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Type.Should().Be("cache-generation");
    }

    [Fact]
    public async Task GetJobsAsync_WithBothFilters_ShouldReturnFilteredJobs()
    {
        // Arrange
        var jobs = new List<BackgroundJob>
        {
            new BackgroundJob("cache-generation", "Cache job", new Dictionary<string, object>()),
            new BackgroundJob("cache-generation", "Cache job 2", new Dictionary<string, object>()),
            new BackgroundJob("thumbnail-generation", "Thumbnail job", new Dictionary<string, object>())
        };
        jobs[0].Start(); // Set status to Running
        jobs[1].Start(); // Set status to Running
        jobs[2].Start(); // Set status to Running

        _mockBackgroundJobRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(jobs);

        // Act
        var result = await _backgroundJobService.GetJobsAsync(status: "Running", type: "cache-generation");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(j => j.Type == "cache-generation").Should().BeTrue();
        result.All(j => j.Status == "Running").Should().BeTrue();
    }

    #endregion

    #region CreateJobAsync Tests

    [Fact]
    public async Task CreateJobAsync_WithValidRequest_ShouldCreateJob()
    {
        // Arrange
        var request = new CreateBackgroundJobDto
        {
            Type = "test-job",
            Description = "Test job description"
        };

        _mockBackgroundJobRepository.Setup(x => x.CreateAsync(It.IsAny<BackgroundJob>()))
            .ReturnsAsync((BackgroundJob job) => job);

        // Act
        var result = await _backgroundJobService.CreateJobAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be("test-job");
        result.Status.Should().Be("Pending");
        result.Progress.Should().NotBeNull();
        result.Progress.Total.Should().Be(0);
        result.Progress.Completed.Should().Be(0);
        _mockBackgroundJobRepository.Verify(x => x.CreateAsync(It.IsAny<BackgroundJob>()), Times.Once);
    }

    [Fact]
    public async Task CreateJobAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _backgroundJobService.CreateJobAsync(null!));
    }

    #endregion

    #region UpdateJobStatusAsync Tests

    [Fact]
    public async Task UpdateJobStatusAsync_WithValidJobId_ShouldUpdateStatus()
    {
        // Arrange
        var jobId = ObjectId.GenerateNewId();
        var job = new BackgroundJob("test-job", "Test job", new Dictionary<string, object>());

        _mockBackgroundJobRepository.Setup(x => x.GetByIdAsync(jobId))
            .ReturnsAsync(job);
        _mockBackgroundJobRepository.Setup(x => x.UpdateAsync(It.IsAny<BackgroundJob>()))
            .ReturnsAsync((BackgroundJob j) => j);

        // Act
        var result = await _backgroundJobService.UpdateJobStatusAsync(jobId, "Running", "Job started");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Running");
        result.Message.Should().Be("Job started");
        _mockBackgroundJobRepository.Verify(x => x.UpdateAsync(It.IsAny<BackgroundJob>()), Times.Once);
    }

    [Fact]
    public async Task UpdateJobStatusAsync_WithNonExistentJobId_ShouldThrowArgumentException()
    {
        // Arrange
        var jobId = ObjectId.GenerateNewId();

        _mockBackgroundJobRepository.Setup(x => x.GetByIdAsync(jobId))
            .ReturnsAsync((BackgroundJob)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _backgroundJobService.UpdateJobStatusAsync(jobId, "Running"));
        exception.Message.Should().Contain($"Job with ID {jobId} not found");
    }

    [Fact]
    public async Task UpdateJobStatusAsync_WithInvalidStatus_ShouldThrowArgumentException()
    {
        // Arrange
        var jobId = ObjectId.GenerateNewId();
        var job = new BackgroundJob("test-job", "Test job", new Dictionary<string, object>());

        _mockBackgroundJobRepository.Setup(x => x.GetByIdAsync(jobId))
            .ReturnsAsync(job);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _backgroundJobService.UpdateJobStatusAsync(jobId, "InvalidStatus"));
        exception.Message.Should().Contain("Invalid job status: InvalidStatus");
    }

    [Fact]
    public async Task UpdateJobStatusAsync_WithValidStatusAndNoMessage_ShouldUpdateStatusOnly()
    {
        // Arrange
        var jobId = ObjectId.GenerateNewId();
        var job = new BackgroundJob("test-job", "Test job", new Dictionary<string, object>());

        _mockBackgroundJobRepository.Setup(x => x.GetByIdAsync(jobId))
            .ReturnsAsync(job);
        _mockBackgroundJobRepository.Setup(x => x.UpdateAsync(It.IsAny<BackgroundJob>()))
            .ReturnsAsync((BackgroundJob j) => j);

        // Act
        var result = await _backgroundJobService.UpdateJobStatusAsync(jobId, "Completed");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Completed");
        result.Message.Should().BeNull();
        _mockBackgroundJobRepository.Verify(x => x.UpdateAsync(It.IsAny<BackgroundJob>()), Times.Once);
    }

    #endregion

    #region UpdateJobProgressAsync Tests

    [Fact]
    public async Task UpdateJobProgressAsync_WithValidJobId_ShouldUpdateProgress()
    {
        // Arrange
        var jobId = ObjectId.GenerateNewId();
        var job = new BackgroundJob("test-job", "Test job", new Dictionary<string, object>());
        job.Start(); // Set status to Running

        _mockBackgroundJobRepository.Setup(x => x.GetByIdAsync(jobId))
            .ReturnsAsync(job);
        _mockBackgroundJobRepository.Setup(x => x.UpdateAsync(It.IsAny<BackgroundJob>()))
            .ReturnsAsync((BackgroundJob j) => j);

        // Act
        var result = await _backgroundJobService.UpdateJobProgressAsync(jobId, 5, 10, "Processing item 5");

        // Assert
        result.Should().NotBeNull();
        result.Progress.Completed.Should().Be(5);
        result.Progress.Total.Should().Be(10);
        result.Progress.CurrentItem.Should().Be("Processing item 5");
        result.Progress.Percentage.Should().Be(50.0);
        _mockBackgroundJobRepository.Verify(x => x.UpdateAsync(It.IsAny<BackgroundJob>()), Times.Once);
    }

    [Fact]
    public async Task UpdateJobProgressAsync_WithNonExistentJobId_ShouldThrowArgumentException()
    {
        // Arrange
        var jobId = ObjectId.GenerateNewId();

        _mockBackgroundJobRepository.Setup(x => x.GetByIdAsync(jobId))
            .ReturnsAsync((BackgroundJob)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _backgroundJobService.UpdateJobProgressAsync(jobId, 5, 10));
        exception.Message.Should().Contain($"Job with ID {jobId} not found");
    }

    [Fact]
    public async Task UpdateJobProgressAsync_WithValidJobIdAndNoCurrentItem_ShouldUpdateProgressOnly()
    {
        // Arrange
        var jobId = ObjectId.GenerateNewId();
        var job = new BackgroundJob("test-job", "Test job", new Dictionary<string, object>());
        job.Start(); // Set status to Running

        _mockBackgroundJobRepository.Setup(x => x.GetByIdAsync(jobId))
            .ReturnsAsync(job);
        _mockBackgroundJobRepository.Setup(x => x.UpdateAsync(It.IsAny<BackgroundJob>()))
            .ReturnsAsync((BackgroundJob j) => j);

        // Act
        var result = await _backgroundJobService.UpdateJobProgressAsync(jobId, 3, 7);

        // Assert
        result.Should().NotBeNull();
        result.Progress.Completed.Should().Be(3);
        result.Progress.Total.Should().Be(7);
        result.Progress.CurrentItem.Should().BeNull();
        _mockBackgroundJobRepository.Verify(x => x.UpdateAsync(It.IsAny<BackgroundJob>()), Times.Once);
    }

    #endregion

    #region CancelJobAsync Tests

    [Fact]
    public async Task CancelJobAsync_WithValidJobId_ShouldCancelJob()
    {
        // Arrange
        var jobId = ObjectId.GenerateNewId();
        var job = new BackgroundJob("test-job", "Test job", new Dictionary<string, object>());

        _mockBackgroundJobRepository.Setup(x => x.GetByIdAsync(jobId))
            .ReturnsAsync(job);
        _mockBackgroundJobRepository.Setup(x => x.UpdateAsync(It.IsAny<BackgroundJob>()))
            .ReturnsAsync((BackgroundJob j) => j);

        // Act
        await _backgroundJobService.CancelJobAsync(jobId);

        // Assert
        _mockBackgroundJobRepository.Verify(x => x.UpdateAsync(It.IsAny<BackgroundJob>()), Times.Once);
    }

    [Fact]
    public async Task CancelJobAsync_WithNonExistentJobId_ShouldThrowArgumentException()
    {
        // Arrange
        var jobId = ObjectId.GenerateNewId();

        _mockBackgroundJobRepository.Setup(x => x.GetByIdAsync(jobId))
            .ReturnsAsync((BackgroundJob)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _backgroundJobService.CancelJobAsync(jobId));
        exception.Message.Should().Contain($"Job with ID {jobId} not found");
    }

    #endregion

    #region DeleteJobAsync Tests

    [Fact]
    public async Task DeleteJobAsync_WithValidJobId_ShouldDeleteJob()
    {
        // Arrange
        var jobId = ObjectId.GenerateNewId();
        var job = new BackgroundJob("test-job", "Test job", new Dictionary<string, object>());

        _mockBackgroundJobRepository.Setup(x => x.GetByIdAsync(jobId))
            .ReturnsAsync(job);
        _mockBackgroundJobRepository.Setup(x => x.DeleteAsync(job.Id))
            .Returns(Task.CompletedTask);

        // Act
        await _backgroundJobService.DeleteJobAsync(jobId);

        // Assert
        _mockBackgroundJobRepository.Verify(x => x.DeleteAsync(job.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteJobAsync_WithNonExistentJobId_ShouldThrowArgumentException()
    {
        // Arrange
        var jobId = ObjectId.GenerateNewId();

        _mockBackgroundJobRepository.Setup(x => x.GetByIdAsync(jobId))
            .ReturnsAsync((BackgroundJob)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _backgroundJobService.DeleteJobAsync(jobId));
        exception.Message.Should().Contain($"Job with ID {jobId} not found");
    }

    #endregion

    #region GetJobStatisticsAsync Tests

    [Fact]
    public async Task GetJobStatisticsAsync_ShouldReturnStatistics()
    {
        // Arrange
        var jobs = new List<BackgroundJob>
        {
            new BackgroundJob("job1", "Job 1", new Dictionary<string, object>()),
            new BackgroundJob("job2", "Job 2", new Dictionary<string, object>()),
            new BackgroundJob("job3", "Job 3", new Dictionary<string, object>()),
            new BackgroundJob("job4", "Job 4", new Dictionary<string, object>())
        };
        jobs[0].Start(); // Running
        jobs[1].Start();
        jobs[1].Complete(); // Completed
        jobs[2].Start();
        jobs[2].Fail("Test error"); // Failed
        jobs[3].Cancel(); // Cancelled

        _mockBackgroundJobRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(jobs);

        // Act
        var result = await _backgroundJobService.GetJobStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalJobs.Should().Be(4);
        result.RunningJobs.Should().Be(1);
        result.CompletedJobs.Should().Be(1);
        result.FailedJobs.Should().Be(1);
        result.CancelledJobs.Should().Be(1);
        result.SuccessRate.Should().Be(25.0); // 1 completed out of 4 total
    }

    [Fact]
    public async Task GetJobStatisticsAsync_WithNoJobs_ShouldReturnZeroStatistics()
    {
        // Arrange
        var jobs = new List<BackgroundJob>();

        _mockBackgroundJobRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(jobs);

        // Act
        var result = await _backgroundJobService.GetJobStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalJobs.Should().Be(0);
        result.RunningJobs.Should().Be(0);
        result.CompletedJobs.Should().Be(0);
        result.FailedJobs.Should().Be(0);
        result.CancelledJobs.Should().Be(0);
        result.SuccessRate.Should().Be(0);
    }

    #endregion

    #region StartCacheGenerationJobAsync Tests

    [Fact]
    public async Task StartCacheGenerationJobAsync_WithValidCollectionId_ShouldCreateJob()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var collection = new Collection(ObjectId.GenerateNewId(), "Test Collection", "/test/path", CollectionType.Folder);

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync(collection);
        _mockBackgroundJobRepository.Setup(x => x.CreateAsync(It.IsAny<BackgroundJob>()))
            .ReturnsAsync((BackgroundJob job) => job);

        // Act
        var result = await _backgroundJobService.StartCacheGenerationJobAsync(collectionId);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be("cache-generation");
        result.Status.Should().Be("Pending");
        result.Parameters.Should().ContainKey("collectionId");
        result.Parameters.Should().ContainKey("collectionName");
        _mockBackgroundJobRepository.Verify(x => x.CreateAsync(It.IsAny<BackgroundJob>()), Times.Once);
    }

    [Fact]
    public async Task StartCacheGenerationJobAsync_WithNonExistentCollectionId_ShouldThrowArgumentException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync((Collection)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _backgroundJobService.StartCacheGenerationJobAsync(collectionId));
        exception.Message.Should().Contain($"Collection with ID {collectionId} not found");
    }

    #endregion

    #region StartThumbnailGenerationJobAsync Tests

    [Fact]
    public async Task StartThumbnailGenerationJobAsync_WithValidCollectionId_ShouldCreateJob()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();
        var collection = new Collection(ObjectId.GenerateNewId(), "Test Collection", "/test/path", CollectionType.Folder);

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync(collection);
        _mockBackgroundJobRepository.Setup(x => x.CreateAsync(It.IsAny<BackgroundJob>()))
            .ReturnsAsync((BackgroundJob job) => job);

        // Act
        var result = await _backgroundJobService.StartThumbnailGenerationJobAsync(collectionId);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be("thumbnail-generation");
        result.Status.Should().Be("Pending");
        result.Parameters.Should().ContainKey("collectionId");
        result.Parameters.Should().ContainKey("collectionName");
        _mockBackgroundJobRepository.Verify(x => x.CreateAsync(It.IsAny<BackgroundJob>()), Times.Once);
    }

    [Fact]
    public async Task StartThumbnailGenerationJobAsync_WithNonExistentCollectionId_ShouldThrowArgumentException()
    {
        // Arrange
        var collectionId = ObjectId.GenerateNewId();

        _mockCollectionRepository.Setup(x => x.GetByIdAsync(collectionId))
            .ReturnsAsync((Collection)null!);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _backgroundJobService.StartThumbnailGenerationJobAsync(collectionId));
        exception.Message.Should().Contain($"Collection with ID {collectionId} not found");
    }

    #endregion

    #region StartBulkOperationJobAsync Tests

    [Fact]
    public async Task StartBulkOperationJobAsync_WithValidRequest_ShouldCreateJob()
    {
        // Arrange
        var request = new BulkOperationDto
        {
            OperationType = "bulk-delete",
            TargetIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            Parameters = new Dictionary<string, object> { { "confirm", true } }
        };

        _mockBackgroundJobRepository.Setup(x => x.CreateAsync(It.IsAny<BackgroundJob>()))
            .ReturnsAsync((BackgroundJob job) => job);

        // Act
        var result = await _backgroundJobService.StartBulkOperationJobAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be("bulk-operation");
        result.Status.Should().Be("Pending");
        result.Parameters.Should().ContainKey("operationType");
        result.Parameters.Should().ContainKey("targetIds");
        result.Parameters.Should().ContainKey("parameters");
        _mockBackgroundJobRepository.Verify(x => x.CreateAsync(It.IsAny<BackgroundJob>()), Times.Once);
    }

    [Fact]
    public async Task StartBulkOperationJobAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _backgroundJobService.StartBulkOperationJobAsync(null!));
    }

    #endregion
}