using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using ImageViewer.Api;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ImageViewer.Tests.Integration;

/// <summary>
/// Performance tests for API endpoints
/// </summary>
public class PerformanceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PerformanceTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task SearchEndpoint_ShouldRespondWithinAcceptableTime()
    {
        // Arrange
        var searchRequest = new
        {
            Query = "test search",
            Page = 1,
            PageSize = 10
        };
        var json = JsonSerializer.Serialize(searchRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.PostAsync("/api/v1/search", content);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"Search took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UserEndpoint_ShouldRespondWithinAcceptableTime()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync($"/api/v1/users/{userId}");
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 500, $"User endpoint took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PerformanceEndpoint_ShouldRespondWithinAcceptableTime()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/api/v1/performance/cache");
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 500, $"Performance endpoint took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MultipleRequests_ShouldMaintainPerformance()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";
        var tasks = new List<Task<(HttpResponseMessage response, long elapsedMs)>>();
        var stopwatch = Stopwatch.StartNew();

        // Act - Send 50 concurrent requests
        for (int i = 0; i < 50; i++)
        {
            tasks.Add(GetUserWithTiming(userId));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var averageResponseTime = results.Average(r => r.elapsedMs);
        var maxResponseTime = results.Max(r => r.elapsedMs);
        var minResponseTime = results.Min(r => r.elapsedMs);

        Assert.True(averageResponseTime < 1000, $"Average response time {averageResponseTime}ms, expected < 1000ms");
        Assert.True(maxResponseTime < 2000, $"Max response time {maxResponseTime}ms, expected < 2000ms");
        Assert.True(minResponseTime > 0, $"Min response time {minResponseTime}ms, expected > 0ms");

        // All requests should complete successfully
        foreach (var (response, _) in results)
        {
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task SearchWithLargePayload_ShouldHandleEfficiently()
    {
        // Arrange
        var largeSearchRequest = new
        {
            Query = new string('a', 1000), // 1KB search query
            Page = 1,
            PageSize = 100
        };
        var json = JsonSerializer.Serialize(largeSearchRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.PostAsync("/api/v1/search", content);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"Large payload search took {stopwatch.ElapsedMilliseconds}ms, expected < 2000ms");
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task NotificationCreation_ShouldBeFast()
    {
        // Arrange
        var notificationRequest = new
        {
            UserId = "507f1f77bcf86cd799439011",
            Type = "Info",
            Title = "Performance Test Notification",
            Message = "This is a performance test notification",
            Priority = "Normal"
        };
        var json = JsonSerializer.Serialize(notificationRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.PostAsync("/api/v1/notifications", content);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"Notification creation took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SecurityAuthentication_ShouldBeFast()
    {
        // Arrange
        var loginRequest = new
        {
            Username = "testuser",
            Password = "testpassword"
        };
        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.PostAsync("/api/v1/security/authenticate", content);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"Authentication took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DatabaseConnection_ShouldBeFast()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mongoDatabase = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var adminCommand = new MongoDB.Bson.BsonDocument("ping", 1);
        var result = await mongoDatabase.RunCommandAsync<MongoDB.Bson.BsonDocument>(adminCommand);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 100, $"Database ping took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task MemoryUsage_ShouldRemainStable()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        var userId = "507f1f77bcf86cd799439011";

        // Act - Perform multiple operations
        for (int i = 0; i < 100; i++)
        {
            var response = await _client.GetAsync($"/api/v1/users/{userId}");
            response.Dispose();
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);
        var memoryIncrease = finalMemory - initialMemory;

        // Assert
        Assert.True(memoryIncrease < 10 * 1024 * 1024, $"Memory increased by {memoryIncrease / 1024 / 1024}MB, expected < 10MB");
    }

    [Fact]
    public async Task ConcurrentUsers_ShouldBeHandledEfficiently()
    {
        // Arrange
        var userId = "507f1f77bcf86cd799439011";
        var concurrentUsers = 20;
        var requestsPerUser = 10;
        var tasks = new List<Task<List<(HttpResponseMessage response, long elapsedMs)>>>();

        // Act - Simulate concurrent users
        for (int user = 0; user < concurrentUsers; user++)
        {
            tasks.Add(SimulateUserSession(userId, requestsPerUser));
        }

        var allResults = await Task.WhenAll(tasks);
        var allResponses = allResults.SelectMany(r => r).ToList();

        // Assert
        var totalRequests = concurrentUsers * requestsPerUser;
        Assert.Equal(totalRequests, allResponses.Count);

        var averageResponseTime = allResponses.Average(r => r.elapsedMs);
        var maxResponseTime = allResponses.Max(r => r.elapsedMs);
        var successRate = allResponses.Count(r => r.response.IsSuccessStatusCode || r.response.StatusCode == System.Net.HttpStatusCode.NotFound) / (double)totalRequests;

        Assert.True(averageResponseTime < 1000, $"Average response time {averageResponseTime}ms, expected < 1000ms");
        Assert.True(maxResponseTime < 3000, $"Max response time {maxResponseTime}ms, expected < 3000ms");
        Assert.True(successRate > 0.95, $"Success rate {successRate:P}, expected > 95%");
    }

    private async Task<(HttpResponseMessage response, long elapsedMs)> GetUserWithTiming(string userId)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await _client.GetAsync($"/api/v1/users/{userId}");
        stopwatch.Stop();
        return (response, stopwatch.ElapsedMilliseconds);
    }

    private async Task<List<(HttpResponseMessage response, long elapsedMs)>> SimulateUserSession(string userId, int requestCount)
    {
        var results = new List<(HttpResponseMessage response, long elapsedMs)>();
        
        for (int i = 0; i < requestCount; i++)
        {
            var result = await GetUserWithTiming(userId);
            results.Add(result);
            
            // Small delay to simulate real user behavior
            await Task.Delay(10);
        }
        
        return results;
    }
}
