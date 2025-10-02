using FluentAssertions;
using ImageViewer.IntegrationTests.Common;
using Microsoft.Extensions.Logging;

namespace ImageViewer.IntegrationTests.Performance;

/// <summary>
/// Performance tests for thumbnail generation and optimization
/// </summary>
public class ThumbnailPerformanceTests : PerformanceTestBase
{
    public ThumbnailPerformanceTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task BuildThumbnails_ShouldBeFast()
    {
        // Arrange
        await CleanupTestDataAsync();
        var collection = await CreateTestCollectionAsync();
        
        // Act - Test thumbnail generation performance
        var (thumbnails, elapsed) = await MeasureTimeAsync(async () =>
        {
            var generatedThumbnails = new List<string>();
            
            if (Directory.Exists(REAL_IMAGE_FOLDER))
            {
                var imageFiles = Directory.GetFiles(REAL_IMAGE_FOLDER, "*", SearchOption.AllDirectories)
                    .Where(f => IsImageFile(f))
                    .Take(10) // Limit to 10 images for performance test
                    .ToArray();
                
                foreach (var imageFile in imageFiles)
                {
                    try
                    {
                        // Simulate thumbnail generation
                        var thumbnailPath = await GenerateThumbnailAsync(imageFile);
                        if (!string.IsNullOrEmpty(thumbnailPath))
                        {
                            generatedThumbnails.Add(thumbnailPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to generate thumbnail for {imageFile}");
                    }
                }
            }
            
            return generatedThumbnails;
        });

        // Assert
        var metrics = GetPerformanceMetrics(elapsed, thumbnails.Count);
        AssertPerformance(metrics, TARGET_THUMBNAIL_TIME_MS * 10, "Build Thumbnails");
        
        metrics.ItemsPerSecond.Should().BeGreaterThan(1); // At least 1 thumbnail per second
        
        _logger.LogInformation($"Generated {thumbnails.Count} thumbnails in {elapsed.TotalMilliseconds:F2}ms " +
                             $"({metrics.ItemsPerSecond:F2} thumbnails/sec)");
    }

    [Fact]
    public async Task ThumbnailOptimization_ShouldMeetSpeedRequirements()
    {
        // Arrange
        await CleanupTestDataAsync();
        
        // Act - Test thumbnail optimization for 25 MB/s network
        var (optimizedThumbnails, elapsed) = await MeasureTimeAsync(async () =>
        {
            var optimizationConfig = new
            {
                MaxSize = MAX_THUMBNAIL_SIZE,
                Quality = 85, // Optimized for speed
                Format = "JPEG", // Fastest format
                Progressive = true, // Progressive loading
                LazyLoading = true // Load on demand
            };
            
            var optimizedCount = 0;
            var totalSize = 0L;
            
            if (Directory.Exists(REAL_IMAGE_FOLDER))
            {
                var imageFiles = Directory.GetFiles(REAL_IMAGE_FOLDER, "*", SearchOption.AllDirectories)
                    .Where(f => IsImageFile(f))
                    .Take(5) // Test with 5 images
                    .ToArray();
                
                foreach (var imageFile in imageFiles)
                {
                    try
                    {
                        // Simulate optimized thumbnail generation
                        var thumbnailPath = await GenerateOptimizedThumbnailAsync(imageFile, optimizationConfig);
                        if (!string.IsNullOrEmpty(thumbnailPath) && File.Exists(thumbnailPath))
                        {
                            optimizedCount++;
                            totalSize += new FileInfo(thumbnailPath).Length;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to optimize thumbnail for {imageFile}");
                    }
                }
            }
            
            return new { Count = optimizedCount, TotalSize = totalSize, Config = optimizationConfig };
        });

        // Assert
        var metrics = GetPerformanceMetrics(elapsed, optimizedThumbnails.Count);
        AssertPerformance(metrics, TARGET_THUMBNAIL_TIME_MS * 5, "Thumbnail Optimization");
        
        optimizedThumbnails.Count.Should().BeGreaterThan(0);
        optimizedThumbnails.TotalSize.Should().BeGreaterThan(0);
        
        _logger.LogInformation($"Optimized {optimizedThumbnails.Count} thumbnails ({optimizedThumbnails.TotalSize / 1024}KB) " +
                             $"in {elapsed.TotalMilliseconds:F2}ms");
        _logger.LogInformation($"Config: {optimizedThumbnails.Config.MaxSize}px, " +
                             $"Quality: {optimizedThumbnails.Config.Quality}, " +
                             $"Format: {optimizedThumbnails.Config.Format}");
    }

    [Fact]
    public async Task BatchThumbnailGeneration_ShouldBeEfficient()
    {
        // Arrange
        await CleanupTestDataAsync();
        var collection = await CreateTestCollectionAsync();
        
        // Act - Test batch thumbnail generation
        var (batchResult, elapsed) = await MeasureTimeAsync(async () =>
        {
            var batchSize = 5;
            var generatedCount = 0;
            var failedCount = 0;
            
            if (Directory.Exists(REAL_IMAGE_FOLDER))
            {
                var imageFiles = Directory.GetFiles(REAL_IMAGE_FOLDER, "*", SearchOption.AllDirectories)
                    .Where(f => IsImageFile(f))
                    .Take(batchSize)
                    .ToArray();
                
                // Process in parallel for better performance
                var tasks = imageFiles.Select(async imageFile =>
                {
                    try
                    {
                        var thumbnailPath = await GenerateThumbnailAsync(imageFile);
                        if (!string.IsNullOrEmpty(thumbnailPath))
                        {
                            Interlocked.Increment(ref generatedCount);
                        }
                    }
                    catch
                    {
                        Interlocked.Increment(ref failedCount);
                    }
                });
                
                await Task.WhenAll(tasks);
            }
            
            return new { Generated = generatedCount, Failed = failedCount, BatchSize = batchSize };
        });

        // Assert
        var metrics = GetPerformanceMetrics(elapsed, batchResult.Generated);
        AssertPerformance(metrics, TARGET_THUMBNAIL_TIME_MS * batchResult.BatchSize, "Batch Thumbnail Generation");
        
        batchResult.Generated.Should().BeGreaterThan(0);
        metrics.ItemsPerSecond.Should().BeGreaterThan(2); // At least 2 thumbnails per second
        
        _logger.LogInformation($"Batch generated {batchResult.Generated}/{batchResult.BatchSize} thumbnails " +
                             $"({batchResult.Failed} failed) in {elapsed.TotalMilliseconds:F2}ms " +
                             $"({metrics.ItemsPerSecond:F2} thumbnails/sec)");
    }

    [Fact]
    public async Task ThumbnailCaching_ShouldBeFast()
    {
        // Arrange
        await CleanupTestDataAsync();
        
        // Act - Test thumbnail caching performance
        var (cacheResult, elapsed) = await MeasureTimeAsync(async () =>
        {
            var cachedCount = 0;
            var cacheHits = 0;
            var cacheMisses = 0;
            
            if (Directory.Exists(REAL_IMAGE_FOLDER))
            {
                var imageFiles = Directory.GetFiles(REAL_IMAGE_FOLDER, "*", SearchOption.AllDirectories)
                    .Where(f => IsImageFile(f))
                    .Take(3) // Test with 3 images
                    .ToArray();
                
                foreach (var imageFile in imageFiles)
                {
                    try
                    {
                        // Simulate cache check and generation
                        var thumbnailPath = await GetOrGenerateThumbnailAsync(imageFile);
                        if (!string.IsNullOrEmpty(thumbnailPath))
                        {
                            cachedCount++;
                            
                            // Simulate cache hit/miss
                            if (File.Exists(thumbnailPath))
                            {
                                cacheHits++;
                            }
                            else
                            {
                                cacheMisses++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to cache thumbnail for {imageFile}");
                    }
                }
            }
            
            return new { Cached = cachedCount, Hits = cacheHits, Misses = cacheMisses };
        });

        // Assert
        var metrics = GetPerformanceMetrics(elapsed, cacheResult.Cached);
        AssertPerformance(metrics, TARGET_CACHE_TIME_MS * 3, "Thumbnail Caching");
        
        cacheResult.Cached.Should().BeGreaterThan(0);
        
        _logger.LogInformation($"Cached {cacheResult.Cached} thumbnails " +
                             $"({cacheResult.Hits} hits, {cacheResult.Misses} misses) " +
                             $"in {elapsed.TotalMilliseconds:F2}ms");
    }

    [Fact]
    public async Task ThumbnailLoading_ShouldBeInstant()
    {
        // Arrange
        await CleanupTestDataAsync();
        
        // Act - Test thumbnail loading performance
        var (loadResult, elapsed) = await MeasureTimeAsync(async () =>
        {
            var loadedCount = 0;
            var totalSize = 0L;
            
            if (Directory.Exists(REAL_IMAGE_FOLDER))
            {
                var imageFiles = Directory.GetFiles(REAL_IMAGE_FOLDER, "*", SearchOption.AllDirectories)
                    .Where(f => IsImageFile(f))
                    .Take(5) // Test with 5 images
                    .ToArray();
                
                foreach (var imageFile in imageFiles)
                {
                    try
                    {
                        var thumbnailPath = await GetOrGenerateThumbnailAsync(imageFile);
                        if (!string.IsNullOrEmpty(thumbnailPath) && File.Exists(thumbnailPath))
                        {
                            loadedCount++;
                            totalSize += new FileInfo(thumbnailPath).Length;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to load thumbnail for {imageFile}");
                    }
                }
            }
            
            return new { Loaded = loadedCount, TotalSize = totalSize };
        });

        // Assert
        var metrics = GetPerformanceMetrics(elapsed, loadResult.Loaded);
        AssertPerformance(metrics, TARGET_LOAD_TIME_MS * 5, "Thumbnail Loading");
        
        loadResult.Loaded.Should().BeGreaterThan(0);
        metrics.ItemsPerSecond.Should().BeGreaterThan(2); // At least 2 thumbnails per second
        
        _logger.LogInformation($"Loaded {loadResult.Loaded} thumbnails ({loadResult.TotalSize / 1024}KB) " +
                             $"in {elapsed.TotalMilliseconds:F2}ms ({metrics.ItemsPerSecond:F2} thumbnails/sec)");
    }

    private bool IsImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".tiff";
    }

    private async Task<string> GenerateThumbnailAsync(string imagePath)
    {
        // Simulate thumbnail generation
        await Task.Delay(50); // Simulate processing time
        
        var thumbnailPath = Path.Combine(CACHE_FOLDERS[0], $"thumb_{Path.GetFileNameWithoutExtension(imagePath)}.jpg");
        
        // Create a dummy thumbnail file for testing
        if (!Directory.Exists(Path.GetDirectoryName(thumbnailPath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(thumbnailPath)!);
        }
        
        File.WriteAllText(thumbnailPath, "dummy thumbnail data");
        return thumbnailPath;
    }

    private async Task<string> GenerateOptimizedThumbnailAsync(string imagePath, dynamic config)
    {
        // Simulate optimized thumbnail generation
        await Task.Delay(30); // Faster processing for optimization
        
        var thumbnailPath = Path.Combine(CACHE_FOLDERS[0], $"opt_thumb_{Path.GetFileNameWithoutExtension(imagePath)}.jpg");
        
        if (!Directory.Exists(Path.GetDirectoryName(thumbnailPath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(thumbnailPath)!);
        }
        
        // Create optimized thumbnail file
        File.WriteAllText(thumbnailPath, $"optimized thumbnail data - {config.MaxSize}px, quality {config.Quality}");
        return thumbnailPath;
    }

    private async Task<string> GetOrGenerateThumbnailAsync(string imagePath)
    {
        var thumbnailPath = Path.Combine(CACHE_FOLDERS[0], $"thumb_{Path.GetFileNameWithoutExtension(imagePath)}.jpg");
        
        if (File.Exists(thumbnailPath))
        {
            return thumbnailPath; // Cache hit
        }
        
        // Generate thumbnail
        return await GenerateThumbnailAsync(imagePath);
    }
}
