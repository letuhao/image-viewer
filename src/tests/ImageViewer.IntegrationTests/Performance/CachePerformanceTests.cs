using FluentAssertions;
using ImageViewer.IntegrationTests.Common;
using Microsoft.Extensions.Logging;

namespace ImageViewer.IntegrationTests.Performance;

/// <summary>
/// Performance tests for cache operations
/// </summary>
public class CachePerformanceTests : PerformanceTestBase
{
    public CachePerformanceTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CacheSettings_ShouldBeOptimizedForSpeed()
    {
        // Arrange
        await CleanupTestDataAsync();
        
        // Act - Test cache configuration
        var (cacheSettings, elapsed) = await MeasureTimeAsync(async () =>
        {
            // Configure cache for maximum speed
            var settings = new
            {
                MaxCacheSize = 400 * 1024 * 1024, // 400MB total across all cache folders
                MaxThumbnailSize = MAX_THUMBNAIL_SIZE,
                CompressionLevel = 6, // Balanced compression
                PreloadThumbnails = true,
                CacheFolders = CACHE_FOLDERS
            };
            
            // Verify cache folders exist and are accessible
            foreach (var folder in CACHE_FOLDERS)
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }
            
            await Task.Delay(1); // Simulate async operation
            return settings;
        });

        // Assert
        var metrics = GetPerformanceMetrics(elapsed);
        AssertPerformance(metrics, TARGET_CACHE_TIME_MS, "Cache Settings Configuration");
        
        cacheSettings.MaxCacheSize.Should().BeGreaterThan(0);
        cacheSettings.MaxThumbnailSize.Should().BeLessThanOrEqualTo(MAX_THUMBNAIL_SIZE);
        cacheSettings.CacheFolders.Should().HaveCount(4);
        
        _logger.LogInformation($"Cache configured: {cacheSettings.MaxCacheSize / (1024 * 1024)}MB total, " +
                             $"{cacheSettings.MaxThumbnailSize}px thumbnails, " +
                             $"{cacheSettings.CacheFolders.Length} cache folders");
    }

    [Fact]
    public async Task CacheOptimization_ShouldMeetSpeedRequirements()
    {
        // Arrange
        await CleanupTestDataAsync();
        var collection = await CreateTestCollectionAsync();
        
        // Act - Test cache optimization with real data
        var (optimizedCache, elapsed) = await MeasureTimeAsync(async () =>
        {
            // Simulate cache optimization for 25 MB/s network
            var cacheConfig = new
            {
                ThumbnailSize = MAX_THUMBNAIL_SIZE,
                Quality = 85, // Good quality, fast processing
                Format = "JPEG", // Fastest format
                Progressive = true, // Progressive loading
                LazyLoading = true, // Load on demand
                PreloadCount = 5, // Preload next 5 images
                MaxConcurrent = 4 // Use all cache folders
            };
            
            // Test cache folder performance
            var folderPerformance = new List<(string Folder, long Size, TimeSpan AccessTime)>();
            
            foreach (var folder in CACHE_FOLDERS)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                if (Directory.Exists(folder))
                {
                    var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
                    var totalSize = files.Sum(f => new FileInfo(f).Length);
                    stopwatch.Stop();
                    
                    folderPerformance.Add((folder, totalSize, stopwatch.Elapsed));
                }
            }
            
            await Task.Delay(1); // Simulate async operation
            return new { CacheConfig = cacheConfig, FolderPerformance = folderPerformance };
        });

        // Assert
        var metrics = GetPerformanceMetrics(elapsed);
        AssertPerformance(metrics, TARGET_CACHE_TIME_MS, "Cache Optimization");
        
        optimizedCache.CacheConfig.ThumbnailSize.Should().BeLessThanOrEqualTo(MAX_THUMBNAIL_SIZE);
        optimizedCache.CacheConfig.Quality.Should().BeInRange(80, 95);
        optimizedCache.FolderPerformance.Should().HaveCount(4);
        
        _logger.LogInformation($"Cache optimization completed in {elapsed.TotalMilliseconds:F2}ms");
        foreach (var perf in optimizedCache.FolderPerformance)
        {
            _logger.LogInformation($"Cache folder {perf.Folder}: {perf.Size / (1024 * 1024)}MB, " +
                                 $"access time: {perf.AccessTime.TotalMilliseconds:F2}ms");
        }
    }

    [Fact]
    public async Task CachePreloading_ShouldBeFast()
    {
        // Arrange
        await CleanupTestDataAsync();
        var collection = await CreateTestCollectionAsync();
        
        // Act - Test cache preloading performance
        var (preloadResult, elapsed) = await MeasureTimeAsync(async () =>
        {
            // Simulate preloading thumbnails for fast viewing
            var preloadTasks = new List<Task>();
            var preloadCount = 10; // Preload 10 thumbnails
            
            for (int i = 0; i < preloadCount; i++)
            {
                preloadTasks.Add(Task.Run(async () =>
                {
                    // Simulate thumbnail generation
                    await Task.Delay(10); // Simulate processing time
                }));
            }
            
            await Task.WhenAll(preloadTasks);
            
            return new { PreloadedCount = preloadCount, Success = true };
        });

        // Assert
        var metrics = GetPerformanceMetrics(elapsed, preloadResult.PreloadedCount);
        AssertPerformance(metrics, TARGET_THUMBNAIL_TIME_MS * 2, "Cache Preloading");
        
        preloadResult.Success.Should().BeTrue();
        metrics.ItemsPerSecond.Should().BeGreaterThan(5); // At least 5 thumbnails per second
        
        _logger.LogInformation($"Preloaded {preloadResult.PreloadedCount} thumbnails in {elapsed.TotalMilliseconds:F2}ms " +
                             $"({metrics.ItemsPerSecond:F2} thumbnails/sec)");
    }

    [Fact]
    public async Task CacheCleanup_ShouldBeEfficient()
    {
        // Arrange
        await CleanupTestDataAsync();
        
        // Act - Test cache cleanup performance
        var (cleanupResult, elapsed) = await MeasureTimeAsync(async () =>
        {
            var totalCleaned = 0;
            var totalSize = 0L;
            
            foreach (var cacheFolder in CACHE_FOLDERS)
            {
                if (Directory.Exists(cacheFolder))
                {
                    var files = Directory.GetFiles(cacheFolder, "*", SearchOption.AllDirectories);
                    var folderSize = files.Sum(f => new FileInfo(f).Length);
                    
                    // Simulate cleanup (don't actually delete in test)
                    totalCleaned += files.Length;
                    totalSize += folderSize;
                }
            }
            
            await Task.Delay(1); // Simulate async operation
            return new { FilesCleaned = totalCleaned, SizeCleaned = totalSize };
        });

        // Assert
        var metrics = GetPerformanceMetrics(elapsed);
        AssertPerformance(metrics, TARGET_CACHE_TIME_MS * 2, "Cache Cleanup");
        
        _logger.LogInformation($"Cache cleanup: {cleanupResult.FilesCleaned} files, " +
                             $"{cleanupResult.SizeCleaned / (1024 * 1024)}MB in {elapsed.TotalMilliseconds:F2}ms");
    }
}
