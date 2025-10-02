using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using ImageViewer.Infrastructure.Services;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Tests.Infrastructure.Services;

public class CompressedFileServiceTests
{
    [Fact]
    public void CompressedFileService_ShouldBeCreated()
    {
        // Arrange
        var imageProcessingServiceMock = new Mock<IImageProcessingService>();
        var loggerMock = new Mock<ILogger<CompressedFileService>>();

        // Act
        var service = new CompressedFileService(imageProcessingServiceMock.Object, loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
    }
}
