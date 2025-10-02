using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Tests.Application.Services;

public class CollectionServiceTests
{
    [Fact]
    public void CollectionService_ShouldBeCreated()
    {
        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var fileScannerMock = new Mock<IFileScannerService>();
        var loggerMock = new Mock<ILogger<CollectionService>>();
        var optionsMock = new Mock<Microsoft.Extensions.Options.IOptions<ImageViewer.Application.Options.ImageSizeOptions>>();
        
        var sizeOptions = new ImageViewer.Application.Options.ImageSizeOptions
        {
            ThumbnailWidth = 300,
            ThumbnailHeight = 300,
            CacheWidth = 1920,
            CacheHeight = 1080
        };
        optionsMock.Setup(x => x.Value).Returns(sizeOptions);

        // Act
        var service = new CollectionService(
            unitOfWorkMock.Object,
            fileScannerMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        // Assert
        service.Should().NotBeNull();
    }
}
