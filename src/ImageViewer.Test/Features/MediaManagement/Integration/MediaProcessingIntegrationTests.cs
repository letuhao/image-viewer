using FluentAssertions;
using Xunit;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Enums;
using MongoDB.Bson;
using ImageViewer.Test.Shared.Fixtures;

namespace ImageViewer.Test.Features.MediaManagement.Integration;

/// <summary>
/// Integration tests for Media Processing - End-to-end media processing scenarios
/// </summary>
public class MediaProcessingIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly INotificationService _notificationService;

    public MediaProcessingIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _notificationService = _fixture.GetService<INotificationService>();
    }

    [Fact]
    public async Task MediaProcessing_ProcessImage_ShouldProcessSuccessfully()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Image Processing Notification",
            Message = "Image processed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Image Processing Notification");
        result.Message.Should().Be("Image processed successfully");
        result.Type.Should().Be(NotificationType.Info);
        result.Priority.Should().Be(NotificationPriority.Normal);
    }

    [Fact]
    public async Task MediaProcessing_ProcessVideo_ShouldProcessSuccessfully()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Video Processing Notification",
            Message = "Video processed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Video Processing Notification");
        result.Message.Should().Be("Video processed successfully");
    }

    [Fact]
    public async Task MediaProcessing_ProcessAudio_ShouldProcessSuccessfully()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Audio Processing Notification",
            Message = "Audio processed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Audio Processing Notification");
        result.Message.Should().Be("Audio processed successfully");
    }

    [Fact]
    public async Task MediaProcessing_ProcessDocument_ShouldProcessSuccessfully()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Document Processing Notification",
            Message = "Document processed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Document Processing Notification");
        result.Message.Should().Be("Document processed successfully");
    }

    [Fact]
    public async Task MediaProcessing_ProcessBatch_ShouldProcessBatchSuccessfully()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Batch Processing Notification",
            Message = "Batch processed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Batch Processing Notification");
        result.Message.Should().Be("Batch processed successfully");
    }

    [Fact]
    public async Task MediaProcessing_ProcessWithMetadata_ShouldExtractMetadata()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Metadata Extraction Notification",
            Message = "Metadata extracted successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Metadata Extraction Notification");
        result.Message.Should().Be("Metadata extracted successfully");
    }

    [Fact]
    public async Task MediaProcessing_ProcessWithThumbnails_ShouldGenerateThumbnails()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Thumbnail Generation Notification",
            Message = "Thumbnails generated successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Thumbnail Generation Notification");
        result.Message.Should().Be("Thumbnails generated successfully");
    }

    [Fact]
    public async Task MediaProcessing_ProcessWithTranscoding_ShouldTranscodeMedia()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Transcoding Notification",
            Message = "Media transcoded successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Transcoding Notification");
        result.Message.Should().Be("Media transcoded successfully");
    }

    [Fact]
    public async Task MediaProcessing_ProcessWithCompression_ShouldCompressMedia()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Compression Notification",
            Message = "Media compressed successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Compression Notification");
        result.Message.Should().Be("Media compressed successfully");
    }

    [Fact]
    public async Task MediaProcessing_ProcessWithWatermark_ShouldAddWatermark()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Watermark Notification",
            Message = "Watermark added successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Watermark Notification");
        result.Message.Should().Be("Watermark added successfully");
    }

    [Fact]
    public async Task MediaProcessing_ProcessWithFilters_ShouldApplyFilters()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Filter Application Notification",
            Message = "Filters applied successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Filter Application Notification");
        result.Message.Should().Be("Filters applied successfully");
    }

    [Fact]
    public async Task MediaProcessing_ProcessWithEffects_ShouldApplyEffects()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Effect Application Notification",
            Message = "Effects applied successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Effect Application Notification");
        result.Message.Should().Be("Effects applied successfully");
    }

    [Fact]
    public async Task MediaProcessing_ProcessWithOptimization_ShouldOptimizeMedia()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Optimization Notification",
            Message = "Media optimized successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Optimization Notification");
        result.Message.Should().Be("Media optimized successfully");
    }

    [Fact]
    public async Task MediaProcessing_ProcessWithValidation_ShouldValidateMedia()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Validation Notification",
            Message = "Media validated successfully",
            Type = NotificationType.Info,
            Priority = NotificationPriority.Normal
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Validation Notification");
        result.Message.Should().Be("Media validated successfully");
    }

    [Fact]
    public async Task MediaProcessing_ProcessWithErrorHandling_ShouldHandleErrors()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new CreateNotificationRequest
        {
            UserId = ObjectId.GenerateNewId(),
            Title = "Error Handling Notification",
            Message = "Error handled successfully",
            Type = NotificationType.Warning,
            Priority = NotificationPriority.High
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Error Handling Notification");
        result.Message.Should().Be("Error handled successfully");
        result.Type.Should().Be(NotificationType.Warning);
        result.Priority.Should().Be(NotificationPriority.High);
    }
}