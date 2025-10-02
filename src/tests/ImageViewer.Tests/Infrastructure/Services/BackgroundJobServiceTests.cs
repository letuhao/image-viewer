using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using ImageViewer.Infrastructure.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Tests.Infrastructure.Services;

public class BackgroundJobServiceTests
{
    [Fact]
    public void BackgroundJobService_ShouldBeCreated()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        var loggerMock = new Mock<ILogger<BackgroundJobService>>();

        // Act
        var service = new BackgroundJobService(serviceProviderMock.Object, loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<BackgroundJobService>>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new BackgroundJobService(null!, loggerMock.Object));

        exception.ParamName.Should().Be("serviceProvider");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new BackgroundJobService(serviceProviderMock.Object, null!));

        exception.ParamName.Should().Be("logger");
    }
}
