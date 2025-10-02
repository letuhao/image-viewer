using FluentAssertions;
using ImageViewer.Domain.Entities;
using ImageViewer.IntegrationTests.Common;
using Microsoft.Extensions.Logging;

namespace ImageViewer.IntegrationTests.Performance;

/// <summary>
/// Performance tests for collection operations
/// </summary>
public class CollectionPerformanceTests : PerformanceTestBase
{
    public CollectionPerformanceTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task BuildCollections_ShouldBeFast()
    {
        // Arrange
        await CleanupTestDataAsync();
        
        // Act - Test collection building performance
        var (collections, elapsed) = await MeasureTimeAsync(async () =>
        {
            var createdCollections = new List<Collection>();
            
            // Create multiple collections to test performance
            for (int i = 0; i < 5; i++)
            {
                var collection = await CreateTestCollectionAsync($"PerfTest_{i}");
                createdCollections.Add(collection);
            }
            
            return createdCollections;
        });

        // Assert
        var metrics = GetPerformanceMetrics(elapsed, collections.Count);
        AssertPerformance(metrics, TARGET_LOAD_TIME_MS * 2, "Build Collections");
        
        collections.Should().HaveCount(5);
        metrics.ItemsPerSecond.Should().BeGreaterThan(2); // At least 2 collections per second
        
        _logger.LogInformation($"Built {collections.Count} collections in {elapsed.TotalMilliseconds:F2}ms " +
                             $"({metrics.ItemsPerSecond:F2} collections/sec)");
    }

    [Fact]
    public async Task ScanRealImageFolder_ShouldBeEfficient()
    {
        // Arrange
        await CleanupTestDataAsync();
        
        // Act - Test scanning real image folder
        var (scanResult, elapsed) = await MeasureTimeAsync(async () =>
        {
            if (!Directory.Exists(REAL_IMAGE_FOLDER))
            {
                return new { ImageCount = 0, TotalSize = 0L, SupportedFormats = new string[0] };
            }
            
            var imageFiles = Directory.GetFiles(REAL_IMAGE_FOLDER, "*", SearchOption.AllDirectories)
                .Where(f => IsImageFile(f))
                .ToArray();
            
            var totalSize = imageFiles.Sum(f => new FileInfo(f).Length);
            var supportedFormats = imageFiles
                .Select(f => Path.GetExtension(f).ToLowerInvariant())
                .Distinct()
                .ToArray();
            
            await Task.Delay(1); // Simulate async operation
            return new { 
                ImageCount = imageFiles.Length, 
                TotalSize = totalSize,
                SupportedFormats = supportedFormats
            };
        });

        // Assert
        var metrics = GetPerformanceMetrics(elapsed, scanResult.ImageCount);
        AssertPerformance(metrics, TARGET_LOAD_TIME_MS * 5, "Scan Real Image Folder");
        
        scanResult.ImageCount.Should().BeGreaterThan(0, "Real image folder should contain images");
        scanResult.TotalSize.Should().BeGreaterThan(0);
        scanResult.SupportedFormats.Should().NotBeEmpty();
        
        _logger.LogInformation($"Scanned {scanResult.ImageCount} images ({scanResult.TotalSize / (1024 * 1024)}MB) " +
                             $"in {elapsed.TotalMilliseconds:F2}ms ({metrics.ItemsPerSecond:F2} images/sec)");
        _logger.LogInformation($"Supported formats: {string.Join(", ", scanResult.SupportedFormats)}");
    }

    [Fact]
    public async Task LoadCollection_ShouldBeInstant()
    {
        // Arrange
        await CleanupTestDataAsync();
        var collection = await CreateTestCollectionAsync();
        
        // Act - Test collection loading performance
        var (loadedCollection, elapsed) = await MeasureTimeAsync(async () =>
        {
            var result = await _collectionService.GetByIdAsync(collection.Id);
            return result;
        });

        // Assert
        var metrics = GetPerformanceMetrics(elapsed);
        AssertPerformance(metrics, TARGET_LOAD_TIME_MS, "Load Collection");
        
        loadedCollection.Should().NotBeNull();
        loadedCollection.Id.Should().Be(collection.Id);
        
        _logger.LogInformation($"Loaded collection '{loadedCollection.Name}' in {elapsed.TotalMilliseconds:F2}ms");
    }

    [Fact]
    public async Task LoadCollectionWithImages_ShouldBeFast()
    {
        // Arrange
        await CleanupTestDataAsync();
        var collection = await CreateTestCollectionAsync();
        
        // Act - Test loading collection with image metadata
        var (collectionWithImages, elapsed) = await MeasureTimeAsync(async () =>
        {
            // Simulate loading collection with image metadata
            var result = await _collectionService.GetByIdAsync(collection.Id);
            
            // Simulate loading image metadata (without actual file processing)
            var imageCount = 0;
            if (Directory.Exists(REAL_IMAGE_FOLDER))
            {
                var imageFiles = Directory.GetFiles(REAL_IMAGE_FOLDER, "*", SearchOption.AllDirectories)
                    .Where(f => IsImageFile(f))
                    .Take(20) // Limit to 20 images for performance test
                    .ToArray();
                
                imageCount = imageFiles.Length;
            }
            
            return new { Collection = result, ImageCount = imageCount };
        });

        // Assert
        var metrics = GetPerformanceMetrics(elapsed);
        AssertPerformance(metrics, TARGET_LOAD_TIME_MS * 3, "Load Collection with Images");
        
        collectionWithImages.Collection.Should().NotBeNull();
        collectionWithImages.ImageCount.Should().BeGreaterThan(0);
        
        _logger.LogInformation($"Loaded collection with {collectionWithImages.ImageCount} images " +
                             $"in {elapsed.TotalMilliseconds:F2}ms");
    }

    [Fact]
    public async Task SearchCollections_ShouldBeFast()
    {
        // Arrange
        await CleanupTestDataAsync();
        
        // Create test collections
        var collections = new List<Collection>();
        for (int i = 0; i < 10; i++)
        {
            var collection = await CreateTestCollectionAsync($"SearchTest_{i}");
            collections.Add(collection);
        }
        
        // Act - Test collection search performance
        var (searchResults, elapsed) = await MeasureTimeAsync(async () =>
        {
            var results = await _collectionService.GetAllAsync();
            return results.Where(c => c.Name.Contains("SearchTest")).ToList();
        });

        // Assert
        var metrics = GetPerformanceMetrics(elapsed, searchResults.Count);
        AssertPerformance(metrics, TARGET_LOAD_TIME_MS * 2, "Search Collections");
        
        searchResults.Should().HaveCount(10);
        metrics.ItemsPerSecond.Should().BeGreaterThan(5); // At least 5 collections per second
        
        _logger.LogInformation($"Searched {searchResults.Count} collections in {elapsed.TotalMilliseconds:F2}ms " +
                             $"({metrics.ItemsPerSecond:F2} collections/sec)");
    }

    [Fact]
    public async Task UpdateCollection_ShouldBeFast()
    {
        // Arrange
        await CleanupTestDataAsync();
        var collection = await CreateTestCollectionAsync();
        
        // Act - Test collection update performance
        (Collection updatedCollection, TimeSpan elapsed) = await MeasureTimeAsync(async () =>
        {
            collection.UpdateName("Updated_Performance_Test");
            
            await _collectionService.UpdateAsync(collection.Id, "Updated_Performance_Test", collection.Path, null, CancellationToken.None);
            return collection;
        });

        // Assert
        var metrics = GetPerformanceMetrics(elapsed);
        AssertPerformance(metrics, TARGET_LOAD_TIME_MS, "Update Collection");
        
        updatedCollection.Name.Should().Be("Updated_Performance_Test");
        
        _logger.LogInformation($"Updated collection in {elapsed.TotalMilliseconds:F2}ms");
    }

    private bool IsImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".tiff";
    }
}
