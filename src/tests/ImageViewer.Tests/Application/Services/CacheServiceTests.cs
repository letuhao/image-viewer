using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using FluentAssertions;
using ImageViewer.Application.Services;
using ImageViewer.Application.Options;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Tests.Application.Services;

public class CacheServiceTests
{
    [Fact]
    public void CacheService_ShouldBeCreated()
    {
        // Arrange
        var cacheFolderRepositoryMock = new Mock<ICacheFolderRepository>();
        var collectionRepositoryMock = new Mock<ICollectionRepository>();
        var imageRepositoryMock = new Mock<IImageRepository>();
        var cacheInfoRepositoryMock = new Mock<ICacheInfoRepository>();
        var imageProcessingServiceMock = new Mock<IImageProcessingService>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<ILogger<CacheService>>();
        var optionsMock = new Mock<IOptions<ImageSizeOptions>>();
        
        var sizeOptions = new ImageSizeOptions
        {
            ThumbnailWidth = 300,
            ThumbnailHeight = 300,
            CacheWidth = 1920,
            CacheHeight = 1080
        };
        optionsMock.Setup(x => x.Value).Returns(sizeOptions);

        // Act
        var service = new CacheService(
            cacheFolderRepositoryMock.Object,
            collectionRepositoryMock.Object,
            imageRepositoryMock.Object,
            cacheInfoRepositoryMock.Object,
            imageProcessingServiceMock.Object,
            unitOfWorkMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        // Assert
        service.Should().NotBeNull();
    }
}
