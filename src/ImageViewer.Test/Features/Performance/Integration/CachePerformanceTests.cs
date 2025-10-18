using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.Cache;
using ImageViewer.Test.Shared.Fixtures;
using MongoDB.Bson;
using System.Diagnostics;

namespace ImageViewer.Test.Features.Performance.Integration;

/// <summary>
/// Integration tests for Cache Performance - End-to-end cache performance scenarios
/// </summary>
[Collection("Integration")]
public class CachePerformanceTests : IClassFixture<BasicPerformanceIntegrationTestFixture>
{
    private readonly BasicPerformanceIntegrationTestFixture _fixture;
    private readonly ICacheService _cacheService;

    public CachePerformanceTests(BasicPerformanceIntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _cacheService = _fixture.GetService<ICacheService>();
    }

    [Fact(Skip = "SaveCachedImageAsync is not supported - use IImageService.GenerateCacheAsync instead")]
    public async Task CachePerformance_StoreAndRetrieve_ShouldPerformEfficiently()
    {
        // This test is skipped because SaveCachedImageAsync is intentionally not supported
        // Cache is now saved via IImageService.GenerateCacheAsync which uses embedded design
        await Task.CompletedTask;
    }

    [Fact]
    public async Task CachePerformance_Statistics_ShouldGetStatisticsEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var statistics = await _cacheService.GetCacheStatisticsAsync();

        // Assert
        stopwatch.Stop();
        statistics.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500); // Should complete within 500ms
    }

    [Fact]
    public async Task CachePerformance_CacheFolders_ShouldGetCacheFoldersEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var folders = await _cacheService.GetCacheFoldersAsync();

        // Assert
        stopwatch.Stop();
        folders.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }

    [Fact]
    public async Task CachePerformance_CreateCacheFolder_ShouldCreateFolderEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var createRequest = new CreateCacheFolderDto
        {
            Name = "Test Cache Folder",
            Path = "/test/cache/folder",
            Priority = 1,
            MaxSize = 1024 * 1024 * 100 // 100MB
        };
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _cacheService.CreateCacheFolderAsync(createRequest);

        // Assert
        stopwatch.Stop();
        result.Should().NotBeNull();
        result.Name.Should().Be(createRequest.Name);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Should complete within 2 seconds
    }

    [Fact]
    public async Task CachePerformance_CollectionCacheStatus_ShouldGetStatusEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var collectionId = ObjectId.GenerateNewId();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var status = await _cacheService.GetCollectionCacheStatusAsync(collectionId);

        // Assert
        stopwatch.Stop();
        status.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    }

    [Fact]
    public async Task CachePerformance_RegenerateCollectionCache_ShouldRegenerateEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var collectionId = ObjectId.GenerateNewId();
        var stopwatch = Stopwatch.StartNew();

        // Act
        await _cacheService.RegenerateCollectionCacheAsync(collectionId);

        // Assert
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
    }

    [Fact]
    public async Task CachePerformance_ClearCollectionCache_ShouldClearEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var collectionId = ObjectId.GenerateNewId();
        var stopwatch = Stopwatch.StartNew();

        // Act
        await _cacheService.ClearCollectionCacheAsync(collectionId);

        // Assert
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Should complete within 2 seconds
    }

    [Fact]
    public async Task CachePerformance_ClearAllCache_ShouldClearAllEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var stopwatch = Stopwatch.StartNew();

        // Act
        await _cacheService.ClearAllCacheAsync();

        // Assert
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // Should complete within 10 seconds
    }

    [Fact]
    public async Task CachePerformance_CleanupExpiredCache_ShouldCleanupEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var stopwatch = Stopwatch.StartNew();

        // Act
        await _cacheService.CleanupExpiredCacheAsync();

        // Assert
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
    }

    [Fact]
    public async Task CachePerformance_CleanupOldCache_ShouldCleanupOldEfficiently()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        var stopwatch = Stopwatch.StartNew();

        // Act
        await _cacheService.CleanupOldCacheAsync(cutoffDate);

        // Assert
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
    }

    [Fact(Skip = "SaveCachedImageAsync is not supported - use IImageService.GenerateCacheAsync instead")]
    public async Task CachePerformance_ConcurrentOperations_ShouldHandleConcurrency()
    {
        // This test is skipped because SaveCachedImageAsync is intentionally not supported
        // Cache is now saved via IImageService.GenerateCacheAsync which uses embedded design
        await Task.CompletedTask;
    }
}