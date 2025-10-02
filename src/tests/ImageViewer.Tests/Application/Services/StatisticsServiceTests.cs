using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Tests.Application.Services;

public class StatisticsServiceTests
{
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
}
