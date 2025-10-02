using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using ImageViewer.Infrastructure.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.ValueObjects;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Tests.Infrastructure.Services;

public class FileScannerServiceTests
{
    private readonly Mock<IImageProcessingService> _imageProcessingServiceMock;
    private readonly Mock<ILogger<FileScannerService>> _loggerMock;
    private readonly FileScannerService _service;

    public FileScannerServiceTests()
    {
        _imageProcessingServiceMock = new Mock<IImageProcessingService>();
        _loggerMock = new Mock<ILogger<FileScannerService>>();
        _service = new FileScannerService(_imageProcessingServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void FileScannerService_ShouldBeCreated()
    {
        // Arrange & Act
        var service = new FileScannerService(_imageProcessingServiceMock.Object, _loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullImageProcessingService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new FileScannerService(null!, _loggerMock.Object));
        
        exception.ParamName.Should().Be("imageProcessingService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new FileScannerService(_imageProcessingServiceMock.Object, null!));
        
        exception.ParamName.Should().Be("logger");
    }

    [Fact]
    public async Task ScanFolderAsync_WithNonExistentFolder_ShouldThrowDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentPath = "C:\\NonExistent\\Folder";

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => 
            _service.ScanFolderAsync(nonExistentPath));
    }

    [Fact]
    public async Task ScanFolderAsync_WithNullPath_ShouldThrowDirectoryNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => 
            _service.ScanFolderAsync(null!));
    }

    [Fact]
    public async Task ScanFolderAsync_WithEmptyPath_ShouldThrowDirectoryNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => 
            _service.ScanFolderAsync(string.Empty));
    }

    [Fact]
    public async Task ScanArchiveAsync_WithNonExistentArchive_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = "C:\\NonExistent\\Archive.zip";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _service.ScanArchiveAsync(nonExistentPath, CollectionType.Zip));
    }

    [Fact]
    public async Task ScanArchiveAsync_WithNullPath_ShouldThrowFileNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _service.ScanArchiveAsync(null!, CollectionType.Zip));
    }

    [Fact]
    public async Task ScanArchiveAsync_WithEmptyPath_ShouldThrowFileNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _service.ScanArchiveAsync(string.Empty, CollectionType.Zip));
    }

    [Fact]
    public async Task IsValidCollectionPathAsync_WithNullPath_ShouldReturnFalse()
    {
        // Act
        var result = await _service.IsValidCollectionPathAsync(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidCollectionPathAsync_WithEmptyPath_ShouldReturnFalse()
    {
        // Act
        var result = await _service.IsValidCollectionPathAsync(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsValidCollectionPathAsync_WithNonExistentPath_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentPath = "C:\\NonExistent\\Folder";

        // Act
        var result = await _service.IsValidCollectionPathAsync(nonExistentPath);

        // Assert
        result.Should().BeFalse();
    }
}
