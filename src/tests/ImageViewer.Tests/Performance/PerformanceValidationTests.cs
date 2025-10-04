using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using ImageViewer.Api;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;
using System.Net;
using System.Diagnostics;

namespace ImageViewer.Tests.Performance;

/// <summary>
/// Performance validation tests for the ImageViewer system
/// </summary>
public class PerformanceValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PerformanceValidationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task API_ResponseTime_ShouldMeetRequirements()
    {
        // Test that API response times meet performance requirements
        
        var endpoints = new[]
        {
            "/api/v1/performance/cache",
            "/api/v1/performance/database",
            "/api/v1/performance/image-processing",
            "/api/v1/performance/lazy-loading"
        };
        
        foreach (var endpoint in endpoints)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await _client.GetAsync(endpoint);
            stopwatch.Stop();
            
            // API should respond within 500ms
            Assert.True(stopwatch.ElapsedMilliseconds < 500, 
                $"Endpoint {endpoint} took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task Search_Performance_ShouldBeAcceptable()
    {
        // Test search performance with various query sizes
        
        var searchQueries = new[]
        {
            "test",
            "test search query",
            "very long search query that should still perform well within acceptable limits",
            new string('a', 1000) // 1KB query
        };
        
        foreach (var query in searchQueries)
        {
            var searchRequest = new
            {
                Query = query,
                Page = 1,
                PageSize = 10
            };
            
            var searchJson = JsonSerializer.Serialize(searchRequest);
            var searchContent = new StringContent(searchJson, Encoding.UTF8, "application/json");
            
            var stopwatch = Stopwatch.StartNew();
            var response = await _client.PostAsync("/api/v1/search", searchContent);
            stopwatch.Stop();
            
            // Search should complete within 2 seconds
            Assert.True(stopwatch.ElapsedMilliseconds < 2000, 
                $"Search with query length {query.Length} took {stopwatch.ElapsedMilliseconds}ms, expected < 2000ms");
            
            Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task ConcurrentRequests_ShouldBeHandledEfficiently()
    {
        // Test concurrent request handling
        
        var concurrentRequests = 50;
        var tasks = new List<Task<(HttpResponseMessage response, long elapsedMs)>>();
        
        var stopwatch = Stopwatch.StartNew();
        
        // Send concurrent requests
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(GetPerformanceWithTiming("/api/v1/performance/cache"));
        }
        
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // All requests should complete within reasonable time
        Assert.True(stopwatch.ElapsedMilliseconds < 10000, 
            $"50 concurrent requests took {stopwatch.ElapsedMilliseconds}ms, expected < 10000ms");
        
        // Calculate performance metrics
        var responseTimes = results.Select(r => r.elapsedMs).ToList();
        var averageResponseTime = responseTimes.Average();
        var maxResponseTime = responseTimes.Max();
        var minResponseTime = responseTimes.Min();
        
        // Performance requirements
        Assert.True(averageResponseTime < 1000, 
            $"Average response time {averageResponseTime}ms, expected < 1000ms");
        Assert.True(maxResponseTime < 3000, 
            $"Max response time {maxResponseTime}ms, expected < 3000ms");
        Assert.True(minResponseTime > 0, 
            $"Min response time {minResponseTime}ms, expected > 0ms");
        
        // All requests should succeed
        foreach (var (response, _) in results)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task MemoryUsage_ShouldRemainStable()
    {
        // Test memory usage stability
        
        var initialMemory = GC.GetTotalMemory(true);
        
        // Perform multiple operations
        for (int i = 0; i < 100; i++)
        {
            var response = await _client.GetAsync("/api/v1/performance/cache");
            response.Dispose();
            
            if (i % 10 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        
        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var finalMemory = GC.GetTotalMemory(false);
        var memoryIncrease = finalMemory - initialMemory;
        
        // Memory increase should be reasonable (< 50MB)
        Assert.True(memoryIncrease < 50 * 1024 * 1024, 
            $"Memory increased by {memoryIncrease / 1024 / 1024}MB, expected < 50MB");
    }

    [Fact]
    public async Task DatabasePerformance_ShouldBeOptimal()
    {
        // Test database performance
        
        var stopwatch = Stopwatch.StartNew();
        var response = await _client.GetAsync("/api/v1/performance/database");
        stopwatch.Stop();
        
        // Database operations should be fast
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Database performance check took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Verify response contains performance data
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task CachePerformance_ShouldBeOptimal()
    {
        // Test cache performance
        
        var stopwatch = Stopwatch.StartNew();
        var response = await _client.GetAsync("/api/v1/performance/cache");
        stopwatch.Stop();
        
        // Cache operations should be very fast
        Assert.True(stopwatch.ElapsedMilliseconds < 100, 
            $"Cache performance check took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Verify response contains cache data
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task ImageProcessingPerformance_ShouldBeAcceptable()
    {
        // Test image processing performance
        
        var stopwatch = Stopwatch.StartNew();
        var response = await _client.GetAsync("/api/v1/performance/image-processing");
        stopwatch.Stop();
        
        // Image processing operations should complete within reasonable time
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, 
            $"Image processing performance check took {stopwatch.ElapsedMilliseconds}ms, expected < 2000ms");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LazyLoadingPerformance_ShouldBeOptimal()
    {
        // Test lazy loading performance
        
        var stopwatch = Stopwatch.StartNew();
        var response = await _client.GetAsync("/api/v1/performance/lazy-loading");
        stopwatch.Stop();
        
        // Lazy loading should be fast
        Assert.True(stopwatch.ElapsedMilliseconds < 500, 
            $"Lazy loading performance check took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Scalability_ShouldBeDemonstrated()
    {
        // Test system scalability with increasing load
        
        var userCounts = new[] { 10, 25, 50 };
        var requestsPerUser = 5;
        
        foreach (var userCount in userCounts)
        {
            var tasks = new List<Task<List<HttpResponseMessage>>>();
            
            // Simulate concurrent users
            for (int user = 0; user < userCount; user++)
            {
                tasks.Add(SimulateUserLoad(requestsPerUser));
            }
            
            var stopwatch = Stopwatch.StartNew();
            var allResults = await Task.WhenAll(tasks);
            stopwatch.Stop();
            
            var allResponses = allResults.SelectMany(r => r).ToList();
            var totalRequests = userCount * requestsPerUser;
            
            // All requests should complete
            Assert.Equal(totalRequests, allResponses.Count);
            
            // Performance should degrade gracefully
            var averageResponseTime = stopwatch.ElapsedMilliseconds / (double)totalRequests;
            Assert.True(averageResponseTime < 100, 
                $"Average response time per request {averageResponseTime}ms for {userCount} users, expected < 100ms");
            
            // Success rate should remain high
            var successCount = allResponses.Count(r => r.IsSuccessStatusCode);
            var successRate = (double)successCount / totalRequests;
            Assert.True(successRate > 0.95, 
                $"Success rate {successRate:P} for {userCount} users, expected > 95%");
        }
    }

    [Fact]
    public async Task ResourceUtilization_ShouldBeEfficient()
    {
        // Test resource utilization efficiency
        
        var endpoints = new[]
        {
            "/api/v1/performance/cache",
            "/api/v1/performance/database",
            "/api/v1/performance/image-processing",
            "/api/v1/performance/lazy-loading"
        };
        
        var totalRequests = 100;
        var tasks = new List<Task<(HttpResponseMessage response, long elapsedMs)>>();
        
        // Send requests to different endpoints
        for (int i = 0; i < totalRequests; i++)
        {
            var endpoint = endpoints[i % endpoints.Length];
            tasks.Add(GetPerformanceWithTiming(endpoint));
        }
        
        var results = await Task.WhenAll(tasks);
        
        // Calculate resource utilization metrics
        var responseTimes = results.Select(r => r.elapsedMs).ToList();
        var averageResponseTime = responseTimes.Average();
        var throughput = totalRequests / (responseTimes.Sum() / 1000.0); // requests per second
        
        // Throughput should be reasonable
        Assert.True(throughput > 10, 
            $"Throughput {throughput:F2} requests/second, expected > 10");
        
        // Average response time should be acceptable
        Assert.True(averageResponseTime < 500, 
            $"Average response time {averageResponseTime}ms, expected < 500ms");
    }

    [Fact]
    public async Task PerformanceMetrics_ShouldBeAccurate()
    {
        // Test that performance metrics are accurate and consistent
        
        var metricsEndpoints = new[]
        {
            "/api/v1/performance/metrics",
            "/api/v1/performance/statistics"
        };
        
        foreach (var endpoint in metricsEndpoints)
        {
            var response = await _client.GetAsync(endpoint);
            
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                Assert.NotEmpty(content);
                
                // Should return valid JSON
                var json = JsonSerializer.Deserialize<JsonElement>(content);
                Assert.True(json.ValueKind == JsonValueKind.Object);
            }
        }
    }

    private async Task<(HttpResponseMessage response, long elapsedMs)> GetPerformanceWithTiming(string endpoint)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await _client.GetAsync(endpoint);
        stopwatch.Stop();
        return (response, stopwatch.ElapsedMilliseconds);
    }

    private async Task<List<HttpResponseMessage>> SimulateUserLoad(int requestCount)
    {
        var responses = new List<HttpResponseMessage>();
        var endpoints = new[]
        {
            "/api/v1/performance/cache",
            "/api/v1/performance/database",
            "/api/v1/performance/image-processing",
            "/api/v1/performance/lazy-loading"
        };
        
        for (int i = 0; i < requestCount; i++)
        {
            var endpoint = endpoints[i % endpoints.Length];
            var response = await _client.GetAsync(endpoint);
            responses.Add(response);
            
            // Small delay to simulate real user behavior
            await Task.Delay(10);
        }
        
        return responses;
    }
}
