using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Tests.Application.Services;

public class ApplicationServiceTests
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

    [Fact]
    public void TagService_ShouldBeCreated()
    {
        // Arrange
        var tagRepositoryMock = new Mock<ITagRepository>();
        var collectionRepositoryMock = new Mock<ICollectionRepository>();
        var collectionTagRepositoryMock = new Mock<ICollectionTagRepository>();
        var userContextMock = new Mock<IUserContextService>();
        var loggerMock = new Mock<ILogger<TagService>>();

        // Act
        var service = new TagService(
            tagRepositoryMock.Object,
            collectionRepositoryMock.Object,
            collectionTagRepositoryMock.Object,
            userContextMock.Object,
            loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void StatisticsService_ShouldBeCreated()
    {
        // Arrange
        var collectionRepositoryMock = new Mock<ICollectionRepository>();
        var imageRepositoryMock = new Mock<IImageRepository>();
        var cacheFolderRepositoryMock = new Mock<ICacheFolderRepository>();
        var viewSessionRepositoryMock = new Mock<IViewSessionRepository>();
        var loggerMock = new Mock<ILogger<StatisticsService>>();

        // Act
        var service = new StatisticsService(
            collectionRepositoryMock.Object,
            imageRepositoryMock.Object,
            cacheFolderRepositoryMock.Object,
            viewSessionRepositoryMock.Object,
            loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void CacheService_ShouldBeCreated()
    {
        // Arrange
        var cacheFolderRepositoryMock = new Mock<ICacheFolderRepository>();
        var collectionRepositoryMock = new Mock<ICollectionRepository>();
        var imageRepositoryMock = new Mock<IImageRepository>();
        var cacheInfoRepositoryMock = new Mock<ICacheInfoRepository>();
        var imageProcessingMock = new Mock<IImageProcessingService>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<ILogger<CacheService>>();
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
        var service = new CacheService(
            cacheFolderRepositoryMock.Object,
            collectionRepositoryMock.Object,
            imageRepositoryMock.Object,
            cacheInfoRepositoryMock.Object,
            imageProcessingMock.Object,
            unitOfWorkMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        // Assert
        service.Should().NotBeNull();
    }
}
