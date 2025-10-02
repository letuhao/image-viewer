using FluentAssertions;
using ImageViewer.Application.Services;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Enums;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Infrastructure.Data;
using ImageViewer.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ImageViewer.IntegrationTests.Performance;

/// <summary>
/// Base class for performance tests with real data and optimized settings
/// </summary>
public abstract class PerformanceTestBase : IntegrationTestBase
{
    protected readonly ICollectionService _collectionService;
    protected readonly IImageService _imageService;
    protected readonly ICacheService _cacheService;
    protected readonly IAdvancedThumbnailService _thumbnailService;
    protected readonly IFileScannerService _fileScannerService;
    protected readonly IImageProcessingService _imageProcessingService;
    protected readonly ILogger<PerformanceTestBase> _logger;

    // Real test data paths
    protected new const string REAL_IMAGE_FOLDER = @"L:\EMedia\AI_Generated\AiASAG";
    protected static readonly string[] CACHE_FOLDERS = {
        @"L:\Image_Cache",
        @"K:\Image_Cache", 
        @"J:\Image_Cache",
        @"I:\Image_Cache"
    };

    // Performance thresholds (optimized for 25 MB/s network)
    protected const int MAX_THUMBNAIL_SIZE = 300; // pixels
    protected const int MAX_CACHE_SIZE_MB = 100; // MB per cache folder
    protected const int TARGET_LOAD_TIME_MS = 200; // milliseconds
    protected const int TARGET_THUMBNAIL_TIME_MS = 100; // milliseconds
    protected const int TARGET_CACHE_TIME_MS = 50; // milliseconds

    protected PerformanceTestBase(IntegrationTestFixture fixture) : base(fixture)
    {
        _collectionService = ServiceProvider.GetRequiredService<ICollectionService>();
        _imageService = ServiceProvider.GetRequiredService<IImageService>();
        _cacheService = ServiceProvider.GetRequiredService<ICacheService>();
        _thumbnailService = ServiceProvider.GetRequiredService<IAdvancedThumbnailService>();
        _fileScannerService = ServiceProvider.GetRequiredService<IFileScannerService>();
        _imageProcessingService = ServiceProvider.GetRequiredService<IImageProcessingService>();
        _logger = ServiceProvider.GetRequiredService<ILogger<PerformanceTestBase>>();
    }

    /// <summary>
    /// Measure execution time of an operation
    /// </summary>
    protected async Task<TimeSpan> MeasureTimeAsync(Func<Task> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        await operation();
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    /// <summary>
    /// Measure execution time of an operation with result
    /// </summary>
    protected async Task<(T Result, TimeSpan Elapsed)> MeasureTimeAsync<T>(Func<Task<T>> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await operation();
        stopwatch.Stop();
        return (result, stopwatch.Elapsed);
    }

    /// <summary>
    /// Create test collection with real image folder
    /// </summary>
    protected async Task<Collection> CreateTestCollectionAsync(string name = "PerformanceTest")
    {
        var collectionName = $"{name}_{Guid.NewGuid():N}";
        var collection = new Collection(collectionName, REAL_IMAGE_FOLDER, CollectionType.Folder);

        await _collectionService.CreateAsync(collectionName, REAL_IMAGE_FOLDER, CollectionType.Folder, null, CancellationToken.None);
        return collection;
    }

    /// <summary>
    /// Get performance metrics for an operation
    /// </summary>
    protected PerformanceMetrics GetPerformanceMetrics(TimeSpan elapsed, int itemCount = 1)
    {
        return new PerformanceMetrics
        {
            ElapsedTime = elapsed,
            ItemsProcessed = itemCount,
            ItemsPerSecond = itemCount / elapsed.TotalSeconds,
            AverageTimePerItem = elapsed.TotalMilliseconds / itemCount
        };
    }

    /// <summary>
    /// Assert performance meets requirements
    /// </summary>
    protected void AssertPerformance(PerformanceMetrics metrics, int maxTimeMs, string operation)
    {
        metrics.ElapsedTime.TotalMilliseconds.Should().BeLessThan(maxTimeMs, 
            $"{operation} should complete within {maxTimeMs}ms, but took {metrics.ElapsedTime.TotalMilliseconds:F2}ms");
        
        _logger.LogInformation($"{operation} Performance: {metrics.ElapsedTime.TotalMilliseconds:F2}ms, " +
                             $"{metrics.ItemsPerSecond:F2} items/sec, " +
                             $"{metrics.AverageTimePerItem:F2}ms per item");
    }

    /// <summary>
    /// Clean up test data
    /// </summary>
    protected async Task CleanupTestDataAsync()
    {
        try
        {
            // Remove test collections
            var testCollections = await DbContext.Collections
                .Where(c => c.Name.Contains("PerformanceTest"))
                .ToListAsync();

            if (testCollections.Any())
            {
                DbContext.Collections.RemoveRange(testCollections);
                await DbContext.SaveChangesAsync();
            }

            // Clear cache folders
            foreach (var cacheFolder in CACHE_FOLDERS)
            {
                if (Directory.Exists(cacheFolder))
                {
                    var files = Directory.GetFiles(cacheFolder, "*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to delete cache file {file}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup test data");
        }
    }
}

/// <summary>
/// Performance metrics for operations
/// </summary>
public class PerformanceMetrics
{
    public TimeSpan ElapsedTime { get; set; }
    public int ItemsProcessed { get; set; }
    public double ItemsPerSecond { get; set; }
    public double AverageTimePerItem { get; set; }
}
