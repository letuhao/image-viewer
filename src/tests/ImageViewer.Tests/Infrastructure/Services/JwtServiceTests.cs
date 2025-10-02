using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using FluentAssertions;
using ImageViewer.Infrastructure.Services;

namespace ImageViewer.Tests.Infrastructure.Services;

public class JwtServiceTests
{
    [Fact]
    public void JwtService_ShouldBeCreated()
    {
        // Arrange
        var configurationMock = new Mock<IConfiguration>();
        var loggerMock = new Mock<ILogger<JwtService>>();

        // Act
        var service = new JwtService(configurationMock.Object, loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
    }
}
