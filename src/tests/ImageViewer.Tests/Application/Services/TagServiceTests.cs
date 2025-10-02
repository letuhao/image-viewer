using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Tests.Application.Services;

public class TagServiceTests
{
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
}
