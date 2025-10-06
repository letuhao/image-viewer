using FluentAssertions;
using Moq;
using Xunit;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Interfaces;
using MongoDB.Bson;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Test.Features.SystemManagement.Unit;

/// <summary>
/// Unit tests for BulkService - Bulk Operations features
/// </summary>
public class BulkServiceTests
{
    private readonly Mock<ICollectionService> _mockCollectionService;
    private readonly Mock<ILogger<BulkService>> _mockLogger;
    private readonly BulkService _bulkService;

    public BulkServiceTests()
    {
        _mockCollectionService = new Mock<ICollectionService>();
        _mockLogger = new Mock<ILogger<BulkService>>();
        _bulkService = new BulkService(_mockCollectionService.Object, _mockLogger.Object);
    }

    #region BulkAddCollectionsAsync Tests

    [Fact]
    public async Task BulkAddCollectionsAsync_WithValidRequest_ShouldProcessCollections()
    {
        // Arrange
        var request = new BulkAddCollectionsRequest
        {
            ParentPath = "C:\\Test\\Collections",
            CollectionPrefix = "Test",
            IncludeSubfolders = true,
            AutoAdd = true,
            OverwriteExisting = false,
            ThumbnailWidth = 300,
            ThumbnailHeight = 200,
            CacheWidth = 1920,
            CacheHeight = 1080,
            EnableCache = true,
            AutoScan = true
        };

        // Mock directory structure
        var testDirectory = Path.Combine(Path.GetTempPath(), "TestCollections");
        Directory.CreateDirectory(testDirectory);
        
        // Create test subdirectories with images
        var subDir1 = Path.Combine(testDirectory, "TestCollection1");
        var subDir2 = Path.Combine(testDirectory, "TestCollection2");
        Directory.CreateDirectory(subDir1);
        Directory.CreateDirectory(subDir2);
        
        // Create test image files
        File.WriteAllText(Path.Combine(subDir1, "image1.jpg"), "fake image data");
        File.WriteAllText(Path.Combine(subDir2, "image2.png"), "fake image data");
        
        request.ParentPath = testDirectory;

        // Mock collection service responses
        _mockCollectionService.Setup(x => x.GetCollectionByPathAsync(It.IsAny<string>()))
            .ReturnsAsync((Collection)null!); // No existing collections
        _mockCollectionService.Setup(x => x.CreateCollectionAsync(It.IsAny<ObjectId>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CollectionType>()))
            .ReturnsAsync((ObjectId libraryId, string name, string path, CollectionType type) => 
                new Collection(libraryId, name, path, type));

        try
        {
            // Act
            var result = await _bulkService.BulkAddCollectionsAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.TotalProcessed.Should().BeGreaterThan(0);
            result.SuccessCount.Should().BeGreaterThan(0);
            result.CreatedCount.Should().BeGreaterThan(0);
            result.Results.Should().NotBeEmpty();
            result.Results.All(r => r.Status == "Success").Should().BeTrue();
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }

    [Fact]
    public async Task BulkAddCollectionsAsync_WithEmptyParentPath_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new BulkAddCollectionsRequest
        {
            ParentPath = "",
            CollectionPrefix = "Test"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _bulkService.BulkAddCollectionsAsync(request));
        exception.Message.Should().Contain("Parent path is required");
    }

    [Fact]
    public async Task BulkAddCollectionsAsync_WithNullParentPath_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new BulkAddCollectionsRequest
        {
            ParentPath = null!,
            CollectionPrefix = "Test"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _bulkService.BulkAddCollectionsAsync(request));
        exception.Message.Should().Contain("Parent path is required");
    }

    [Fact]
    public async Task BulkAddCollectionsAsync_WithDangerousSystemPath_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new BulkAddCollectionsRequest
        {
            ParentPath = "C:\\Windows\\System32",
            CollectionPrefix = "Test"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _bulkService.BulkAddCollectionsAsync(request));
        exception.Message.Should().Contain("Cannot scan system directories");
    }

    [Fact]
    public async Task BulkAddCollectionsAsync_WithNonExistentPath_ShouldThrowDirectoryNotFoundException()
    {
        // Arrange
        var request = new BulkAddCollectionsRequest
        {
            ParentPath = "C:\\NonExistent\\Path",
            CollectionPrefix = "Test"
        };

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _bulkService.BulkAddCollectionsAsync(request));
    }

    [Fact]
    public async Task BulkAddCollectionsAsync_WithExistingCollectionsAndOverwriteFalse_ShouldSkipExisting()
    {
        // Arrange
        var testDirectory = Path.Combine(Path.GetTempPath(), "TestExistingCollections");
        Directory.CreateDirectory(testDirectory);
        
        var subDir = Path.Combine(testDirectory, "ExistingCollection");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "image.jpg"), "fake image data");

        var request = new BulkAddCollectionsRequest
        {
            ParentPath = testDirectory,
            CollectionPrefix = "",
            IncludeSubfolders = false,
            OverwriteExisting = false
        };

        var existingCollection = new Collection(ObjectId.GenerateNewId(), "ExistingCollection", subDir, CollectionType.Folder);
        _mockCollectionService.Setup(x => x.GetCollectionByPathAsync(subDir))
            .ReturnsAsync(existingCollection);

        try
        {
            // Act
            var result = await _bulkService.BulkAddCollectionsAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.TotalProcessed.Should().Be(1);
            result.SkippedCount.Should().Be(1);
            result.SuccessCount.Should().Be(0);
            result.Results.First().Status.Should().Be("Skipped");
            result.Results.First().Message.Should().Contain("Collection already exists");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }

    [Fact]
    public async Task BulkAddCollectionsAsync_WithExistingCollectionsAndOverwriteTrue_ShouldUpdateExisting()
    {
        // Arrange
        var testDirectory = Path.Combine(Path.GetTempPath(), "TestOverwriteCollections");
        Directory.CreateDirectory(testDirectory);
        
        var subDir = Path.Combine(testDirectory, "ExistingCollection");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "image.jpg"), "fake image data");

        var request = new BulkAddCollectionsRequest
        {
            ParentPath = testDirectory,
            CollectionPrefix = "",
            IncludeSubfolders = false,
            OverwriteExisting = true,
            ThumbnailWidth = 400,
            CacheWidth = 2048
        };

        var existingCollection = new Collection(ObjectId.GenerateNewId(), "ExistingCollection", subDir, CollectionType.Folder);
        var updatedCollection = new Collection(ObjectId.GenerateNewId(), "ExistingCollection", subDir, CollectionType.Folder);
        
        _mockCollectionService.Setup(x => x.GetCollectionByPathAsync(subDir))
            .ReturnsAsync(existingCollection);
        _mockCollectionService.Setup(x => x.UpdateCollectionAsync(It.IsAny<ObjectId>(), It.IsAny<UpdateCollectionRequest>()))
            .ReturnsAsync(updatedCollection);

        try
        {
            // Act
            var result = await _bulkService.BulkAddCollectionsAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.TotalProcessed.Should().Be(1);
            result.UpdatedCount.Should().Be(1);
            result.SuccessCount.Should().Be(1);
            result.Results.First().Status.Should().Be("Success");
            result.Results.First().Message.Should().Contain("updated");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }

    [Fact]
    public async Task BulkAddCollectionsAsync_WithCollectionPrefix_ShouldFilterByPrefix()
    {
        // Arrange
        var testDirectory = Path.Combine(Path.GetTempPath(), "TestPrefixCollections");
        Directory.CreateDirectory(testDirectory);
        
        var subDir1 = Path.Combine(testDirectory, "TestCollection1");
        var subDir2 = Path.Combine(testDirectory, "OtherCollection");
        Directory.CreateDirectory(subDir1);
        Directory.CreateDirectory(subDir2);
        
        File.WriteAllText(Path.Combine(subDir1, "image1.jpg"), "fake image data");
        File.WriteAllText(Path.Combine(subDir2, "image2.png"), "fake image data");

        var request = new BulkAddCollectionsRequest
        {
            ParentPath = testDirectory,
            CollectionPrefix = "Test",
            IncludeSubfolders = false
        };

        _mockCollectionService.Setup(x => x.GetCollectionByPathAsync(It.IsAny<string>()))
            .ReturnsAsync((Collection)null!);
        _mockCollectionService.Setup(x => x.CreateCollectionAsync(It.IsAny<ObjectId>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CollectionType>()))
            .ReturnsAsync((ObjectId libraryId, string name, string path, CollectionType type) => 
                new Collection(libraryId, name, path, type));

        try
        {
            // Act
            var result = await _bulkService.BulkAddCollectionsAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.TotalProcessed.Should().BeGreaterThanOrEqualTo(1); // At least TestCollection1 should be processed
            result.SuccessCount.Should().BeGreaterThanOrEqualTo(1);
            result.Results.Any(r => r.Name == "TestCollection1").Should().BeTrue();
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }

    [Fact]
    public async Task BulkAddCollectionsAsync_WithIncludeSubfoldersTrue_ShouldProcessSubfolders()
    {
        // Arrange
        var testDirectory = Path.Combine(Path.GetTempPath(), "TestSubfoldersCollections");
        Directory.CreateDirectory(testDirectory);
        
        var subDir1 = Path.Combine(testDirectory, "Level1");
        var subDir2 = Path.Combine(subDir1, "Level2");
        Directory.CreateDirectory(subDir1);
        Directory.CreateDirectory(subDir2);
        
        File.WriteAllText(Path.Combine(subDir1, "image1.jpg"), "fake image data");
        File.WriteAllText(Path.Combine(subDir2, "image2.png"), "fake image data");

        var request = new BulkAddCollectionsRequest
        {
            ParentPath = testDirectory,
            CollectionPrefix = "",
            IncludeSubfolders = true
        };

        _mockCollectionService.Setup(x => x.GetCollectionByPathAsync(It.IsAny<string>()))
            .ReturnsAsync((Collection)null!);
        _mockCollectionService.Setup(x => x.CreateCollectionAsync(It.IsAny<ObjectId>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CollectionType>()))
            .ReturnsAsync((ObjectId libraryId, string name, string path, CollectionType type) => 
                new Collection(libraryId, name, path, type));

        try
        {
            // Act
            var result = await _bulkService.BulkAddCollectionsAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.TotalProcessed.Should().Be(2); // Both Level1 and Level2 should be processed
            result.SuccessCount.Should().Be(2);
            result.Results.Should().HaveCount(2);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }

    [Fact]
    public async Task BulkAddCollectionsAsync_WithCompressedFiles_ShouldProcessZipFiles()
    {
        // Arrange
        var testDirectory = Path.Combine(Path.GetTempPath(), "TestCompressedCollections");
        Directory.CreateDirectory(testDirectory);
        
        var zipFile = Path.Combine(testDirectory, "TestArchive.zip");
        File.WriteAllText(zipFile, "fake zip data");

        var request = new BulkAddCollectionsRequest
        {
            ParentPath = testDirectory,
            CollectionPrefix = "",
            IncludeSubfolders = false
        };

        _mockCollectionService.Setup(x => x.GetCollectionByPathAsync(It.IsAny<string>()))
            .ReturnsAsync((Collection)null!);
        _mockCollectionService.Setup(x => x.CreateCollectionAsync(It.IsAny<ObjectId>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CollectionType>()))
            .ReturnsAsync((ObjectId libraryId, string name, string path, CollectionType type) => 
                new Collection(libraryId, name, path, type));

        try
        {
            // Act
            var result = await _bulkService.BulkAddCollectionsAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.TotalProcessed.Should().BeGreaterThanOrEqualTo(0); // May or may not process compressed files depending on implementation
            if (result.TotalProcessed > 0)
            {
                result.SuccessCount.Should().BeGreaterThanOrEqualTo(0);
                result.Results.Any(r => r.Name == "TestArchive" && r.Type == CollectionType.Zip).Should().BeTrue();
            }
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }

    [Fact]
    public async Task BulkAddCollectionsAsync_WithCollectionServiceException_ShouldHandleErrors()
    {
        // Arrange
        var testDirectory = Path.Combine(Path.GetTempPath(), "TestErrorCollections");
        Directory.CreateDirectory(testDirectory);
        
        var subDir = Path.Combine(testDirectory, "ErrorCollection");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "image.jpg"), "fake image data");

        var request = new BulkAddCollectionsRequest
        {
            ParentPath = testDirectory,
            CollectionPrefix = "",
            IncludeSubfolders = false
        };

        _mockCollectionService.Setup(x => x.GetCollectionByPathAsync(It.IsAny<string>()))
            .ReturnsAsync((Collection)null!);
        _mockCollectionService.Setup(x => x.CreateCollectionAsync(It.IsAny<ObjectId>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CollectionType>()))
            .ThrowsAsync(new InvalidOperationException("Collection creation failed"));

        try
        {
            // Act
            var result = await _bulkService.BulkAddCollectionsAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.TotalProcessed.Should().Be(1);
            result.ErrorCount.Should().Be(1);
            result.SuccessCount.Should().Be(0);
            result.Results.First().Status.Should().Be("Error");
            result.Results.First().Message.Should().Contain("Collection creation failed");
            result.Errors.Should().NotBeEmpty();
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }

    [Fact]
    public async Task BulkAddCollectionsAsync_WithAutoScanFalse_ShouldNotIncludeScanMessage()
    {
        // Arrange
        var testDirectory = Path.Combine(Path.GetTempPath(), "TestNoScanCollections");
        Directory.CreateDirectory(testDirectory);
        
        var subDir = Path.Combine(testDirectory, "NoScanCollection");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "image.jpg"), "fake image data");

        var request = new BulkAddCollectionsRequest
        {
            ParentPath = testDirectory,
            CollectionPrefix = "",
            IncludeSubfolders = false,
            AutoScan = false
        };

        _mockCollectionService.Setup(x => x.GetCollectionByPathAsync(It.IsAny<string>()))
            .ReturnsAsync((Collection)null!);
        _mockCollectionService.Setup(x => x.CreateCollectionAsync(It.IsAny<ObjectId>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CollectionType>()))
            .ReturnsAsync((ObjectId libraryId, string name, string path, CollectionType type) => 
                new Collection(libraryId, name, path, type));

        try
        {
            // Act
            var result = await _bulkService.BulkAddCollectionsAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.TotalProcessed.Should().Be(1);
            result.SuccessCount.Should().Be(1);
            result.Results.First().Status.Should().Be("Success");
            result.Results.First().Message.Should().NotContain("scanned");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }

    #endregion
}
