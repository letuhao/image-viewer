using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Tests.Application.Services;

public class ImageServiceTests
{
    [Fact]
    public void ImageService_ShouldBeCreated()
    {
        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var imageProcessingMock = new Mock<IImageProcessingService>();
        var cacheServiceMock = new Mock<ICacheService>();
        var loggerMock = new Mock<ILogger<ImageService>>();
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
        var service = new ImageService(
            unitOfWorkMock.Object,
            imageProcessingMock.Object,
            cacheServiceMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        // Assert
        service.Should().NotBeNull();
    }
}
