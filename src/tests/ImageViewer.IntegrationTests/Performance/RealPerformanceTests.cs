using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ImageViewer.IntegrationTests.Performance;

/// <summary>
/// Real Performance Tests using HTTP requests to running service
/// </summary>
public class RealPerformanceTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ILogger<RealPerformanceTests> _logger;

    public RealPerformanceTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        
        // Get logger from service provider
        using var scope = _factory.Services.CreateScope();
        _logger = scope.ServiceProvider.GetRequiredService<ILogger<RealPerformanceTests>>();
    }

    [Fact]
    public async Task HealthCheck_ShouldBeFast()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/health");
        stopwatch.Stop();

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, "Health check should be very fast");
        
        _logger.LogInformation($"Health check took: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task CollectionsAPI_ShouldBeFast()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/api/collections");
        stopwatch.Stop();

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "Collections API should be fast");
        
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Collections API took: {stopwatch.ElapsedMilliseconds}ms, Response length: {content.Length} chars");
    }

    [Fact]
    public async Task ImagesAPI_ShouldBeFast()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act - Test random image endpoint (actual endpoint)
        var response = await _client.GetAsync("/api/images/random");
        stopwatch.Stop();

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Random image API should be fast");
        
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Random image API took: {stopwatch.ElapsedMilliseconds}ms, Response length: {content.Length} chars");
    }

    [Fact]
    public async Task RandomImageAPI_ShouldBeFast()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/api/images/random");
        stopwatch.Stop();

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Random image API should be fast");
        
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Random image API took: {stopwatch.ElapsedMilliseconds}ms, Response length: {content.Length} chars");
    }

    [Fact]
    public async Task StatisticsAPI_ShouldBeFast()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act - Test overall statistics endpoint (actual endpoint)
        var response = await _client.GetAsync("/api/statistics/overall");
        stopwatch.Stop();

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Overall statistics API should be fast");
        
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Overall statistics API took: {stopwatch.ElapsedMilliseconds}ms, Response length: {content.Length} chars");
    }

    [Fact]
    public async Task ConcurrentRequests_ShouldHandleLoad()
    {
        // Arrange
        var tasks = new List<Task<(HttpResponseMessage response, long elapsedMs)>>();
        var stopwatch = Stopwatch.StartNew();

        // Act - Send 10 concurrent requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(ExecuteRequestAsync("/api/collections"));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(10);
        results.All(r => r.response.IsSuccessStatusCode).Should().BeTrue("All concurrent requests should succeed");
        
        var avgResponseTime = results.Average(r => r.elapsedMs);
        var maxResponseTime = results.Max(r => r.elapsedMs);
        
        avgResponseTime.Should().BeLessThan(1000, "Average response time should be reasonable");
        maxResponseTime.Should().BeLessThan(2000, "Max response time should be acceptable");
        
        _logger.LogInformation($"Concurrent requests - Total time: {stopwatch.ElapsedMilliseconds}ms, Avg: {avgResponseTime:F0}ms, Max: {maxResponseTime}ms");
    }

    [Fact]
    public async Task DatabaseHeavyOperations_ShouldBeOptimized()
    {
        // Arrange
        var operations = new[]
        {
            "/api/collections",
            "/api/images/random", 
            "/api/statistics/overall",
            "/api/tags"
        };

        var results = new List<(string endpoint, long elapsedMs)>();

        // Act
        foreach (var operation in operations)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await _client.GetAsync(operation);
            stopwatch.Stop();
            
            response.IsSuccessStatusCode.Should().BeTrue($"Operation {operation} should succeed");
            results.Add((operation, stopwatch.ElapsedMilliseconds));
        }

        // Assert
        var totalTime = results.Sum(r => r.elapsedMs);
        var avgTime = results.Average(r => r.elapsedMs);
        
        totalTime.Should().BeLessThan(5000, "All database operations combined should be fast");
        avgTime.Should().BeLessThan(1000, "Average database operation should be fast");
        
        _logger.LogInformation($"Database operations - Total: {totalTime}ms, Avg: {avgTime:F0}ms");
        foreach (var (endpoint, elapsedMs) in results)
        {
            _logger.LogInformation($"  {endpoint}: {elapsedMs}ms");
        }
    }

    [Fact]
    public async Task ImageProcessing_ShouldBeOptimized()
    {
        // Arrange
        var imageEndpoints = new[]
        {
            "/api/images/random",
            "/api/thumbnails/collections/00000000-0000-0000-0000-000000000000" // Test with dummy collection ID
        };

        var results = new List<(string endpoint, long elapsedMs)>();

        // Act
        foreach (var endpoint in imageEndpoints)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await _client.GetAsync(endpoint);
            stopwatch.Stop();
            
            response.IsSuccessStatusCode.Should().BeTrue($"Image endpoint {endpoint} should succeed");
            results.Add((endpoint, stopwatch.ElapsedMilliseconds));
        }

        // Assert
        var totalTime = results.Sum(r => r.elapsedMs);
        var avgTime = results.Average(r => r.elapsedMs);
        
        totalTime.Should().BeLessThan(3000, "Image processing operations should be fast");
        avgTime.Should().BeLessThan(1500, "Average image processing should be fast");
        
        _logger.LogInformation($"Image processing - Total: {totalTime}ms, Avg: {avgTime:F0}ms");
        foreach (var (endpoint, elapsedMs) in results)
        {
            _logger.LogInformation($"  {endpoint}: {elapsedMs}ms");
        }
    }

    [Fact]
    public async Task PerformanceReport_ShouldGenerateComprehensiveMetrics()
    {
        // Arrange
        var report = new List<string>();
        var totalStopwatch = Stopwatch.StartNew();

        // Test 1: Health Check
        var healthStopwatch = Stopwatch.StartNew();
        var healthResponse = await _client.GetAsync("/health");
        healthStopwatch.Stop();
        report.Add($"Health Check: {healthStopwatch.ElapsedMilliseconds}ms (Status: {healthResponse.StatusCode})");

        // Test 2: Collections API
        var collectionsStopwatch = Stopwatch.StartNew();
        var collectionsResponse = await _client.GetAsync("/api/collections");
        collectionsStopwatch.Stop();
        var collectionsContent = await collectionsResponse.Content.ReadAsStringAsync();
        report.Add($"Collections API: {collectionsStopwatch.ElapsedMilliseconds}ms (Status: {collectionsResponse.StatusCode}, Size: {collectionsContent.Length} chars)");

        // Test 3: Random Images API
        var imagesStopwatch = Stopwatch.StartNew();
        var imagesResponse = await _client.GetAsync("/api/images/random");
        imagesStopwatch.Stop();
        var imagesContent = await imagesResponse.Content.ReadAsStringAsync();
        report.Add($"Random Images API: {imagesStopwatch.ElapsedMilliseconds}ms (Status: {imagesResponse.StatusCode}, Size: {imagesContent.Length} chars)");

        // Test 4: Random Image
        var randomStopwatch = Stopwatch.StartNew();
        var randomResponse = await _client.GetAsync("/api/images/random");
        randomStopwatch.Stop();
        var randomContent = await randomResponse.Content.ReadAsStringAsync();
        report.Add($"Random Image: {randomStopwatch.ElapsedMilliseconds}ms (Status: {randomResponse.StatusCode}, Size: {randomContent.Length} chars)");

        // Test 5: Overall Statistics
        var statsStopwatch = Stopwatch.StartNew();
        var statsResponse = await _client.GetAsync("/api/statistics/overall");
        statsStopwatch.Stop();
        var statsContent = await statsResponse.Content.ReadAsStringAsync();
        report.Add($"Overall Statistics: {statsStopwatch.ElapsedMilliseconds}ms (Status: {statsResponse.StatusCode}, Size: {statsContent.Length} chars)");

        // Test 6: Concurrent Load Test
        var concurrentStopwatch = Stopwatch.StartNew();
        var concurrentTasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 5; i++)
        {
            concurrentTasks.Add(_client.GetAsync("/api/collections"));
        }
        var concurrentResponses = await Task.WhenAll(concurrentTasks);
        concurrentStopwatch.Stop();
        report.Add($"Concurrent Load (5 requests): {concurrentStopwatch.ElapsedMilliseconds}ms (All successful: {concurrentResponses.All(r => r.IsSuccessStatusCode)})");

        totalStopwatch.Stop();
        report.Add($"Total Test Time: {totalStopwatch.ElapsedMilliseconds}ms");

        // Assert
        healthResponse.IsSuccessStatusCode.Should().BeTrue();
        collectionsResponse.IsSuccessStatusCode.Should().BeTrue();
        imagesResponse.IsSuccessStatusCode.Should().BeTrue();
        randomResponse.IsSuccessStatusCode.Should().BeTrue();
        statsResponse.IsSuccessStatusCode.Should().BeTrue();
        concurrentResponses.All(r => r.IsSuccessStatusCode).Should().BeTrue();

        // Output comprehensive report
        _logger.LogInformation("=== REAL PERFORMANCE TEST REPORT ===");
        foreach (var line in report)
        {
            _logger.LogInformation(line);
        }
        _logger.LogInformation("=====================================");
    }

    private async Task<(HttpResponseMessage response, long elapsedMs)> ExecuteRequestAsync(string endpoint)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await _client.GetAsync(endpoint);
        stopwatch.Stop();
        return (response, stopwatch.ElapsedMilliseconds);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
