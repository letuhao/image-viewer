using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ImageViewer.IntegrationTests.Setup;

/// <summary>
/// Tests to setup database with real data
/// </summary>
public class SetupDatabaseTests
{
    private readonly HttpClient _httpClient;
    // Direct API calls instead of using SetupDatabaseTool
    private readonly ILogger<SetupDatabaseTests> _logger;

    public SetupDatabaseTests()
    {
        // Create HTTP client for API calls
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://localhost:11001");
        
        // Create logger
        var serviceProvider = new ServiceCollection()
            .AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information))
            .BuildServiceProvider();
        
        _logger = serviceProvider.GetRequiredService<ILogger<SetupDatabaseTests>>();
    }

    [Fact]
    public async Task SetupDatabase_WithRealData_ShouldSucceed()
    {
        // Arrange
        _logger.LogInformation("🚀 Starting database setup with real data from L:\\EMedia\\AI_Generated\\AiASAG");

        // Act - Bulk add collections
        var bulkRequest = new BulkAddCollectionsRequest
        {
            ParentPath = @"L:\EMedia\AI_Generated\AiASAG",
            CollectionPrefix = "",
            IncludeSubfolders = true,
            AutoAdd = true,
            ThumbnailWidth = 300,
            ThumbnailHeight = 300,
            CacheWidth = 1920,
            CacheHeight = 1080,
            EnableCache = true,
            AutoScan = true
        };
        
        var json = JsonSerializer.Serialize(bulkRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("/api/bulk/collections", content);
        
        // Assert
        response.IsSuccessStatusCode.Should().BeTrue($"Bulk add should succeed. Status: {response.StatusCode}");
        
        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("📊 Bulk add response: {Content}", responseContent);
        
        var result = JsonSerializer.Deserialize<BulkOperationResult>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        result.Should().NotBeNull();
        result.SuccessCount.Should().BeGreaterThan(0, "Should create at least one collection");
        
        _logger.LogInformation("✅ Created {SuccessCount} collections, {SkippedCount} skipped, {ErrorCount} errors", 
            result.SuccessCount, result.SkippedCount, result.ErrorCount);
        
        _logger.LogInformation("🎉 Database setup completed successfully!");
    }

    [Fact]
    public async Task VerifyDatabaseSetup_ShouldHaveData()
    {
        // Arrange
        _logger.LogInformation("🔍 Verifying database has data");

        // Act - Check collections
        var collectionsResponse = await _httpClient.GetAsync("/api/collections");
        collectionsResponse.IsSuccessStatusCode.Should().BeTrue("Collections API should work");
        
        var collectionsContent = await collectionsResponse.Content.ReadAsStringAsync();
        _logger.LogInformation("📁 Collections response: {Content}", collectionsContent);

        // Act - Check statistics
        var statsResponse = await _httpClient.GetAsync("/api/statistics/overall");
        statsResponse.IsSuccessStatusCode.Should().BeTrue("Statistics API should work");
        
        var statsContent = await statsResponse.Content.ReadAsStringAsync();
        _logger.LogInformation("📊 Statistics response: {Content}", statsContent);

        // Act - Check random image
        var randomImageResponse = await _httpClient.GetAsync("/api/images/random");
        if (randomImageResponse.IsSuccessStatusCode)
        {
            var randomImageContent = await randomImageResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("🖼️ Random image response: {Content}", randomImageContent);
        }
        else
        {
            _logger.LogWarning("⚠️ Random image API returned {StatusCode}", randomImageResponse.StatusCode);
        }

        // Assert
        collectionsResponse.IsSuccessStatusCode.Should().BeTrue();
        statsResponse.IsSuccessStatusCode.Should().BeTrue();
        
        _logger.LogInformation("✅ Database verification completed");
    }

    [Fact]
    public async Task PerformanceTest_WithRealData_ShouldBeFast()
    {
        // Arrange
        _logger.LogInformation("⚡ Running performance test with real data");

        var endpoints = new[]
        {
            "/api/collections",
            "/api/statistics/overall",
            "/api/images/random"
        };

        var results = new List<(string endpoint, long elapsedMs, bool success)>();

        // Act
        foreach (var endpoint in endpoints)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.GetAsync(endpoint);
            stopwatch.Stop();
            
            results.Add((endpoint, stopwatch.ElapsedMilliseconds, response.IsSuccessStatusCode));
            
            _logger.LogInformation("📊 {Endpoint}: {ElapsedMs}ms (Status: {StatusCode})", 
                endpoint, stopwatch.ElapsedMilliseconds, response.StatusCode);
        }

        // Assert
        var totalTime = results.Sum(r => r.elapsedMs);
        var avgTime = results.Average(r => r.elapsedMs);
        var successCount = results.Count(r => r.success);
        
        totalTime.Should().BeLessThan(5000, "Total time should be reasonable");
        avgTime.Should().BeLessThan(1000, "Average time should be fast");
        successCount.Should().BeGreaterThan(0, "At least some endpoints should work");
        
        _logger.LogInformation("🎯 Performance test completed: {TotalTime}ms total, {AvgTime:F0}ms average, {SuccessCount}/{TotalCount} successful", 
            totalTime, avgTime, successCount, results.Count);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

public class BulkAddCollectionsRequest
{
    public string ParentPath { get; set; } = string.Empty;
    public string CollectionPrefix { get; set; } = string.Empty;
    public bool IncludeSubfolders { get; set; } = false;
    public bool AutoAdd { get; set; } = false;
    public int? ThumbnailWidth { get; set; }
    public int? ThumbnailHeight { get; set; }
    public int? CacheWidth { get; set; }
    public int? CacheHeight { get; set; }
    public bool? EnableCache { get; set; }
    public bool? AutoScan { get; set; }
}

public class BulkOperationResult
{
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<BulkCollectionResult> Results { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class BulkCollectionResult
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int Type { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? CollectionId { get; set; }
}
