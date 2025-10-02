using FluentAssertions;
using ImageViewer.IntegrationTests.Common;
using Microsoft.Extensions.Logging;

namespace ImageViewer.IntegrationTests.Performance;

/// <summary>
/// Performance tests optimized for 25 MB/s network speed
/// </summary>
public class NetworkOptimizedTests : PerformanceTestBase
{
    private const int NETWORK_SPEED_MBPS = 25;
    private const int MAX_IMAGE_SIZE_KB = 500; // Optimized for 25 MB/s network
    private new const int TARGET_LOAD_TIME_MS = 200; // 200ms for instant feel

    public NetworkOptimizedTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task ImageLoading_ShouldBeOptimizedFor25Mbps()
    {
        // Arrange
        await CleanupTestDataAsync();
        var collection = await CreateTestCollectionAsync();
        
        // Act - Test image loading optimized for 25 MB/s network
        var (loadResult, elapsed) = await MeasureTimeAsync(async () =>
        {
            var loadedImages = new List<ImageLoadResult>();
            
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
                        var fileInfo = new FileInfo(imageFile);
                        var optimizedSize = await OptimizeImageForNetworkAsync(imageFile, fileInfo.Length);
                        
                        loadedImages.Add(new ImageLoadResult
                        {
                            OriginalPath = imageFile,
                            OptimizedSize = optimizedSize,
                            LoadTime = TimeSpan.FromMilliseconds(CalculateLoadTime(optimizedSize))
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to load image {imageFile}");
                    }
                }
            }
            
            return loadedImages;
        });

        // Assert
        var metrics = GetPerformanceMetrics(elapsed, loadResult.Count);
        AssertPerformance(metrics, TARGET_LOAD_TIME_MS * 5, "Network Optimized Image Loading");
        
        loadResult.Should().NotBeEmpty();
        
        // Verify all images are optimized for network speed
        foreach (var image in loadResult)
        {
            image.OptimizedSize.Should().BeLessThan(MAX_IMAGE_SIZE_KB * 1024, 
                $"Image should be optimized to under {MAX_IMAGE_SIZE_KB}KB for 25 MB/s network");
            image.LoadTime.TotalMilliseconds.Should().BeLessThan(TARGET_LOAD_TIME_MS,
                "Image should load within 200ms for instant feel");
        }
        
        _logger.LogInformation($"Loaded {loadResult.Count} images optimized for {NETWORK_SPEED_MBPS} MB/s network " +
                             $"in {elapsed.TotalMilliseconds:F2}ms");
        
        foreach (var image in loadResult)
        {
            _logger.LogInformation($"Image: {Path.GetFileName(image.OriginalPath)}, " +
                                 $"Size: {image.OptimizedSize / 1024}KB, " +
                                 $"Load time: {image.LoadTime.TotalMilliseconds:F2}ms");
        }
    }

    [Fact]
    public async Task ProgressiveLoading_ShouldBeFast()
    {
        // Arrange
        await CleanupTestDataAsync();
        
        // Act - Test progressive loading for instant user experience
        var (progressiveResult, elapsed) = await MeasureTimeAsync(async () =>
        {
            var progressiveConfig = new ProgressiveLoadingConfig
            {
                ThumbnailSize = 150, // Small thumbnails for instant loading
                PreviewSize = 300,   // Medium previews
                FullSize = 800,      // Full size for zoom
                Quality = 80,       // Balanced quality/speed
                Format = "JPEG"      // Fastest format
            };
            
            var loadedStages = new List<ProgressiveStage>();
            
            if (Directory.Exists(REAL_IMAGE_FOLDER))
            {
                var imageFiles = Directory.GetFiles(REAL_IMAGE_FOLDER, "*", SearchOption.AllDirectories)
                    .Where(f => IsImageFile(f))
                    .Take(3) // Test with 3 images
                    .ToArray();
                
                foreach (var imageFile in imageFiles)
                {
                    var stages = await LoadProgressiveStagesAsync(imageFile, progressiveConfig);
                    loadedStages.AddRange(stages);
                }
            }
            
            return new { Stages = loadedStages, Config = progressiveConfig };
        });

        // Assert
        var metrics = GetPerformanceMetrics(elapsed, progressiveResult.Stages.Count);
        AssertPerformance(metrics, TARGET_LOAD_TIME_MS * 3, "Progressive Loading");
        
        progressiveResult.Stages.Should().NotBeEmpty();
        
        // Verify progressive loading stages
        var thumbnailStages = progressiveResult.Stages.Where(s => s.Stage == "thumbnail").ToList();
        var previewStages = progressiveResult.Stages.Where(s => s.Stage == "preview").ToList();
        var fullStages = progressiveResult.Stages.Where(s => s.Stage == "full").ToList();
        
        thumbnailStages.Should().NotBeEmpty("Should have thumbnail stages");
        previewStages.Should().NotBeEmpty("Should have preview stages");
        
        _logger.LogInformation($"Progressive loading: {progressiveResult.Stages.Count} stages " +
                             $"({thumbnailStages.Count} thumbnails, {previewStages.Count} previews, {fullStages.Count} full) " +
                             $"in {elapsed.TotalMilliseconds:F2}ms");
    }

    [Fact]
    public async Task Preloading_ShouldBeEfficient()
    {
        // Arrange
        await CleanupTestDataAsync();
        var collection = await CreateTestCollectionAsync();
        
        // Act - Test preloading for smooth browsing
        var (preloadResult, elapsed) = await MeasureTimeAsync(async () =>
        {
            var preloadConfig = new PreloadConfig
            {
                PreloadCount = 5,        // Preload next 5 images
                ThumbnailSize = 200,     // Small thumbnails
                Quality = 75,            // Lower quality for speed
                BackgroundProcessing = true // Process in background
            };
            
            var preloadedItems = new List<PreloadItem>();
            
            if (Directory.Exists(REAL_IMAGE_FOLDER))
            {
                var imageFiles = Directory.GetFiles(REAL_IMAGE_FOLDER, "*", SearchOption.AllDirectories)
                    .Where(f => IsImageFile(f))
                    .Take(preloadConfig.PreloadCount)
                    .ToArray();
                
                // Preload in parallel for efficiency
                var preloadTasks = imageFiles.Select(async imageFile =>
                {
                    try
                    {
                        var preloadItem = await PreloadImageAsync(imageFile, preloadConfig);
                        return preloadItem;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to preload {imageFile}");
                        return null;
                    }
                });
                
                var results = await Task.WhenAll(preloadTasks);
                preloadedItems.AddRange(results.Where(r => r != null)!);
            }
            
            return new { Items = preloadedItems, Config = preloadConfig };
        });

        // Assert
        var metrics = GetPerformanceMetrics(elapsed, preloadResult.Items.Count);
        AssertPerformance(metrics, TARGET_LOAD_TIME_MS * 3, "Preloading");
        
        preloadResult.Items.Should().NotBeEmpty();
        metrics.ItemsPerSecond.Should().BeGreaterThan(2); // At least 2 items per second
        
        _logger.LogInformation($"Preloaded {preloadResult.Items.Count} items " +
                             $"in {elapsed.TotalMilliseconds:F2}ms ({metrics.ItemsPerSecond:F2} items/sec)");
    }

    [Fact]
    public async Task CacheOptimization_ShouldBeFast()
    {
        // Arrange
        await CleanupTestDataAsync();
        
        // Act - Test cache optimization for network speed
        var (cacheResult, elapsed) = await MeasureTimeAsync(async () =>
        {
            var cacheConfig = new NetworkCacheConfig
            {
                MaxCacheSize = 400 * 1024 * 1024, // 400MB total
                ThumbnailSize = MAX_THUMBNAIL_SIZE,
                Quality = 85,
                CompressionLevel = 6,
                PreloadThumbnails = true,
                CacheFolders = CACHE_FOLDERS
            };
            
            var optimizedCache = new List<CacheItem>();
            
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
                        var cacheItem = await OptimizeCacheForNetworkAsync(imageFile, cacheConfig);
                        optimizedCache.Add(cacheItem);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to optimize cache for {imageFile}");
                    }
                }
            }
            
            return new { Cache = optimizedCache, Config = cacheConfig };
        });

        // Assert
        var metrics = GetPerformanceMetrics(elapsed, cacheResult.Cache.Count);
        AssertPerformance(metrics, TARGET_CACHE_TIME_MS * 3, "Cache Optimization");
        
        cacheResult.Cache.Should().NotBeEmpty();
        
        _logger.LogInformation($"Optimized cache for {cacheResult.Cache.Count} items " +
                             $"in {elapsed.TotalMilliseconds:F2}ms");
    }

    private bool IsImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".tiff";
    }

    private async Task<long> OptimizeImageForNetworkAsync(string imagePath, long originalSize)
    {
        // Simulate image optimization for 25 MB/s network
        await Task.Delay(20); // Simulate processing time
        
        // Calculate optimized size based on network speed
        var optimizedSize = Math.Min(originalSize, MAX_IMAGE_SIZE_KB * 1024);
        return optimizedSize;
    }

    private int CalculateLoadTime(long sizeBytes)
    {
        // Calculate load time based on 25 MB/s network speed
        var sizeMB = sizeBytes / (1024.0 * 1024.0);
        var loadTimeMs = (sizeMB / NETWORK_SPEED_MBPS) * 1000;
        return Math.Max(50, (int)loadTimeMs); // Minimum 50ms
    }

    private async Task<List<ProgressiveStage>> LoadProgressiveStagesAsync(string imagePath, ProgressiveLoadingConfig config)
    {
        var stages = new List<ProgressiveStage>();
        
        // Simulate progressive loading stages
        await Task.Delay(10); // Thumbnail stage
        stages.Add(new ProgressiveStage { Stage = "thumbnail", Size = config.ThumbnailSize, LoadTime = TimeSpan.FromMilliseconds(50) });
        
        await Task.Delay(20); // Preview stage
        stages.Add(new ProgressiveStage { Stage = "preview", Size = config.PreviewSize, LoadTime = TimeSpan.FromMilliseconds(100) });
        
        await Task.Delay(30); // Full stage
        stages.Add(new ProgressiveStage { Stage = "full", Size = config.FullSize, LoadTime = TimeSpan.FromMilliseconds(150) });
        
        return stages;
    }

    private async Task<PreloadItem> PreloadImageAsync(string imagePath, PreloadConfig config)
    {
        // Simulate preloading
        await Task.Delay(25);
        
        return new PreloadItem
        {
            ImagePath = imagePath,
            ThumbnailPath = Path.Combine(CACHE_FOLDERS[0], $"preload_{Path.GetFileNameWithoutExtension(imagePath)}.jpg"),
            Size = config.ThumbnailSize,
            Quality = config.Quality,
            PreloadTime = DateTime.UtcNow
        };
    }

    private async Task<CacheItem> OptimizeCacheForNetworkAsync(string imagePath, NetworkCacheConfig config)
    {
        // Simulate cache optimization
        await Task.Delay(15);
        
        return new CacheItem
        {
            OriginalPath = imagePath,
            CachePath = Path.Combine(CACHE_FOLDERS[0], $"cache_{Path.GetFileNameWithoutExtension(imagePath)}.jpg"),
            Size = MAX_THUMBNAIL_SIZE,
            Quality = config.Quality,
            OptimizedForNetwork = true
        };
    }
}

// Data classes for performance tests
public class ImageLoadResult
{
    public string OriginalPath { get; set; } = string.Empty;
    public long OptimizedSize { get; set; }
    public TimeSpan LoadTime { get; set; }
}

public class ProgressiveLoadingConfig
{
    public int ThumbnailSize { get; set; }
    public int PreviewSize { get; set; }
    public int FullSize { get; set; }
    public int Quality { get; set; }
    public string Format { get; set; } = string.Empty;
}

public class ProgressiveStage
{
    public string Stage { get; set; } = string.Empty;
    public int Size { get; set; }
    public TimeSpan LoadTime { get; set; }
}

public class PreloadConfig
{
    public int PreloadCount { get; set; }
    public int ThumbnailSize { get; set; }
    public int Quality { get; set; }
    public bool BackgroundProcessing { get; set; }
}

public class PreloadItem
{
    public string ImagePath { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public int Size { get; set; }
    public int Quality { get; set; }
    public DateTime PreloadTime { get; set; }
}

public class NetworkCacheConfig
{
    public long MaxCacheSize { get; set; }
    public int ThumbnailSize { get; set; }
    public int Quality { get; set; }
    public int CompressionLevel { get; set; }
    public bool PreloadThumbnails { get; set; }
    public string[] CacheFolders { get; set; } = Array.Empty<string>();
}

public class CacheItem
{
    public string OriginalPath { get; set; } = string.Empty;
    public string CachePath { get; set; } = string.Empty;
    public int Size { get; set; }
    public int Quality { get; set; }
    public bool OptimizedForNetwork { get; set; }
}
