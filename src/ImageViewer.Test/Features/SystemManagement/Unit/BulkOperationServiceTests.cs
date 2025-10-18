using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.BulkOperations;
using MongoDB.Bson;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Test.Features.SystemManagement.Unit;

/// <summary>
/// Unit tests for BulkOperationService - Bulk Operations Management features
/// </summary>
public class BulkOperationServiceTests
{
    private readonly Mock<ILogger<BulkOperationService>> _mockLogger;
    private readonly BulkOperationService _bulkOperationService;

    public BulkOperationServiceTests()
    {
        _mockLogger = new Mock<ILogger<BulkOperationService>>();
        _bulkOperationService = new BulkOperationService(_mockLogger.Object);
    }

    [Fact]
    public void BulkOperationService_ShouldExist()
    {
        // Arrange & Act
        var service = _bulkOperationService;

        // Assert
        service.Should().NotBeNull();
        service.Should().BeOfType<BulkOperationService>();
    }

    [Fact]
    public async Task BulkImport_WithValidRequest_ShouldImportSuccessfully()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var request = new BulkImportRequest
        {
            UserId = userId,
            SourcePath = "/source/path",
            DestinationPath = "/destination/path",
            FileTypes = new List<string> { "jpg", "png", "gif" },
            OverwriteExisting = false,
            CreateThumbnails = true,
            GenerateMetadata = true,
            BatchSize = 50
        };

        // Act
        var result = await _bulkOperationService.BulkImportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.OperationType.Should().Be("Import");
        result.Status.Should().Be("Completed");
        result.TotalItems.Should().BeGreaterThan(0);
        result.SuccessfulItems.Should().BeGreaterThan(0);
        result.SuccessRate.Should().BeGreaterThan(0);
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        result.Summary.Should().ContainKey("SourcePath");
        result.Summary.Should().ContainKey("DestinationPath");
        result.Summary.Should().ContainKey("FileTypes");
    }

    [Fact]
    public async Task BulkImport_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        BulkImportRequest request = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _bulkOperationService.BulkImportAsync(request));
    }

    [Fact]
    public async Task BulkExport_WithValidRequest_ShouldExportSuccessfully()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var collectionIds = new List<ObjectId> { ObjectId.GenerateNewId(), ObjectId.GenerateNewId() };
        var request = new BulkExportRequest
        {
            UserId = userId,
            CollectionIds = collectionIds,
            ExportPath = "/export/path",
            ExportFormat = "JPEG",
            MaxWidth = 1920,
            MaxHeight = 1080,
            Quality = 90,
            IncludeMetadata = true,
            CreateZipArchive = true
        };

        // Act
        var result = await _bulkOperationService.BulkExportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.OperationType.Should().Be("Export");
        result.Status.Should().Be("Completed");
        result.TotalItems.Should().Be(collectionIds.Count);
        result.SuccessfulItems.Should().BeGreaterThan(0);
        result.SuccessRate.Should().BeGreaterThan(0);
        result.Summary.Should().ContainKey("ExportPath");
        result.Summary.Should().ContainKey("ExportFormat");
        result.Summary.Should().ContainKey("Quality");
    }

    [Fact]
    public async Task BulkExport_WithEmptyCollectionIds_ShouldHandleGracefully()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var request = new BulkExportRequest
        {
            UserId = userId,
            CollectionIds = new List<ObjectId>(),
            ExportPath = "/export/path",
            ExportFormat = "JPEG"
        };

        // Act
        var result = await _bulkOperationService.BulkExportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.OperationType.Should().Be("Export");
        result.Status.Should().Be("Completed");
        result.TotalItems.Should().Be(0);
        result.SuccessfulItems.Should().Be(0);
        result.SuccessRate.Should().Be(0);
    }

    [Fact]
    public async Task BulkUpdate_WithValidRequest_ShouldUpdateSuccessfully()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var collectionIds = new List<ObjectId> { ObjectId.GenerateNewId(), ObjectId.GenerateNewId() };
        var updateFields = new Dictionary<string, object>
        {
            { "Name", "Updated Name" },
            { "Description", "Updated Description" }
        };
        var request = new BulkUpdateRequest
        {
            UserId = userId,
            CollectionIds = collectionIds,
            UpdateFields = updateFields,
            ValidateUpdates = true,
            BatchSize = 25
        };

        // Act
        var result = await _bulkOperationService.BulkUpdateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.OperationType.Should().Be("Update");
        result.Status.Should().Be("Completed");
        result.TotalItems.Should().Be(collectionIds.Count);
        result.SuccessfulItems.Should().BeGreaterThan(0);
        result.SuccessRate.Should().BeGreaterThan(0);
        result.Summary.Should().ContainKey("UpdateFields");
        result.Summary.Should().ContainKey("ValidateUpdates");
    }

    [Fact]
    public async Task BulkDelete_WithValidRequest_ShouldDeleteSuccessfully()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var collectionIds = new List<ObjectId> { ObjectId.GenerateNewId(), ObjectId.GenerateNewId() };
        var request = new BulkDeleteRequest
        {
            UserId = userId,
            CollectionIds = collectionIds,
            SoftDelete = true,
            DeleteFiles = false,
            ConfirmDeletion = true
        };

        // Act
        var result = await _bulkOperationService.BulkDeleteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.OperationType.Should().Be("Delete");
        result.Status.Should().Be("Completed");
        result.TotalItems.Should().Be(collectionIds.Count);
        result.SuccessfulItems.Should().BeGreaterThan(0);
        result.SuccessRate.Should().BeGreaterThan(0);
        result.Summary.Should().ContainKey("SoftDelete");
        result.Summary.Should().ContainKey("DeleteFiles");
    }

    [Fact]
    public async Task BulkDelete_WithSoftDeleteFalse_ShouldDeletePermanently()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var collectionIds = new List<ObjectId> { ObjectId.GenerateNewId() };
        var request = new BulkDeleteRequest
        {
            UserId = userId,
            CollectionIds = collectionIds,
            SoftDelete = false,
            DeleteFiles = true,
            ConfirmDeletion = true
        };

        // Act
        var result = await _bulkOperationService.BulkDeleteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.OperationType.Should().Be("Delete");
        result.Status.Should().Be("Completed");
        result.Summary["SoftDelete"].Should().Be(false);
        result.Summary["DeleteFiles"].Should().Be(true);
    }

    [Fact]
    public async Task BulkValidation_WithValidRequest_ShouldValidateSuccessfully()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var collectionIds = new List<ObjectId> { ObjectId.GenerateNewId(), ObjectId.GenerateNewId() };
        var validationRules = new List<string> { "FileExists", "MetadataValid", "PermissionsCorrect" };
        var request = new BulkValidationRequest
        {
            UserId = userId,
            CollectionIds = collectionIds,
            ValidationRules = validationRules,
            ValidateFiles = true,
            ValidateMetadata = true,
            ValidatePermissions = true
        };

        // Act
        var result = await _bulkOperationService.BulkValidateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.OperationType.Should().Be("Validation");
        result.Status.Should().Be("Completed");
        result.TotalItems.Should().Be(collectionIds.Count);
        result.SuccessfulItems.Should().BeGreaterThan(0);
        result.SuccessRate.Should().BeGreaterThan(0);
        result.Summary.Should().ContainKey("ValidationRules");
        result.Summary.Should().ContainKey("ValidateFiles");
        result.Summary.Should().ContainKey("ValidateMetadata");
    }

    [Fact]
    public async Task BulkValidation_WithNoValidationRules_ShouldStillValidate()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var collectionIds = new List<ObjectId> { ObjectId.GenerateNewId() };
        var request = new BulkValidationRequest
        {
            UserId = userId,
            CollectionIds = collectionIds,
            ValidationRules = new List<string>(),
            ValidateFiles = false,
            ValidateMetadata = false,
            ValidatePermissions = false
        };

        // Act
        var result = await _bulkOperationService.BulkValidateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.OperationType.Should().Be("Validation");
        result.Status.Should().Be("Completed");
        result.TotalItems.Should().Be(collectionIds.Count);
    }

    [Fact]
    public async Task GetOperationProgress_WithValidOperationId_ShouldReturnProgress()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var request = new BulkImportRequest
        {
            UserId = userId,
            SourcePath = "/source/path",
            DestinationPath = "/destination/path",
            FileTypes = new List<string> { "jpg" },
            BatchSize = 1
        };

        // Start operation
        var result = await _bulkOperationService.BulkImportAsync(request);

        // Act
        var progress = await _bulkOperationService.GetOperationProgressAsync(result.OperationId);

        // Assert
        progress.Should().NotBeNull();
        progress.OperationType.Should().Be("Import");
        progress.Status.Should().BeOneOf("Running", "Completed");
        progress.TotalItems.Should().BeGreaterThan(0);
        progress.ProgressPercentage.Should().BeGreaterThanOrEqualTo(0);
        progress.ProgressPercentage.Should().BeLessThanOrEqualTo(100);

        // Operation is already completed
    }

    [Fact]
    public async Task GetOperationProgress_WithInvalidOperationId_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidOperationId = ObjectId.GenerateNewId();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _bulkOperationService.GetOperationProgressAsync(invalidOperationId));
    }

    [Fact]
    public async Task CancelOperation_WithValidOperationId_ShouldCancelSuccessfully()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var request = new BulkImportRequest
        {
            UserId = userId,
            SourcePath = "/source/path",
            DestinationPath = "/destination/path",
            FileTypes = new List<string> { "jpg" },
            BatchSize = 100 // Large batch to ensure operation runs long enough
        };

        // Start operation
        var operationTask = _bulkOperationService.BulkImportAsync(request);
        
        // Get the operation ID from the task result
        var result = await operationTask;

        // Act
        var cancelled = await _bulkOperationService.CancelOperationAsync(result.OperationId);

        // Assert
        cancelled.Should().BeTrue();

        // Wait for operation to complete
        await operationTask;
    }

    [Fact]
    public async Task CancelOperation_WithInvalidOperationId_ShouldReturnFalse()
    {
        // Arrange
        var invalidOperationId = ObjectId.GenerateNewId();

        // Act
        var cancelled = await _bulkOperationService.CancelOperationAsync(invalidOperationId);

        // Assert
        cancelled.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserOperations_WithValidUserId_ShouldReturnOperations()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var request = new BulkImportRequest
        {
            UserId = userId,
            SourcePath = "/source/path",
            DestinationPath = "/destination/path",
            FileTypes = new List<string> { "jpg" },
            BatchSize = 1
        };

        // Start operation
        var operationTask = _bulkOperationService.BulkImportAsync(request);

        // Act
        var operations = await _bulkOperationService.GetUserOperationsAsync(userId);

        // Assert
        operations.Should().NotBeNull();
        operations.Should().NotBeEmpty();
        operations.Should().Contain(op => op.OperationType == "Import");

        // Wait for operation to complete
        await operationTask;
    }

    [Fact]
    public async Task BulkOperation_WithCancellation_ShouldHandleCancellationGracefully()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var request = new BulkImportRequest
        {
            UserId = userId,
            SourcePath = "/source/path",
            DestinationPath = "/destination/path",
            FileTypes = new List<string> { "jpg" },
            BatchSize = 100
        };

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50); // Cancel after 50ms

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            _bulkOperationService.BulkImportAsync(request, cts.Token));
    }

    [Fact]
    public async Task BulkOperation_WithErrors_ShouldTrackErrorsCorrectly()
    {
        // Arrange
        var userId = ObjectId.GenerateNewId();
        var request = new BulkImportRequest
        {
            UserId = userId,
            SourcePath = "/source/path",
            DestinationPath = "/destination/path",
            FileTypes = new List<string> { "jpg" },
            BatchSize = 20 // Ensure we have some items to process
        };

        // Act
        var result = await _bulkOperationService.BulkImportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Completed");
        result.TotalItems.Should().BeGreaterThan(0);
        result.SuccessfulItems.Should().BeGreaterThan(0);
        result.FailedItems.Should().BeGreaterThan(0); // Some items should fail (every 10th item)
        result.Errors.Should().NotBeEmpty();
        result.SuccessRate.Should().BeGreaterThan(0);
        result.SuccessRate.Should().BeLessThan(100);
    }
}
